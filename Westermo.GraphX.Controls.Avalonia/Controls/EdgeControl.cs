using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace Westermo.GraphX.Controls.Avalonia
{
    /// <summary>
    /// Visual edge control
    /// </summary>
    [Serializable]
    public class EdgeControl : EdgeControlBase
    {
        #region Dependency Properties

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<EdgeControl, double>(
                nameof(StrokeThickness), 5);


        /// <summary>
        /// Custom edge thickness
        /// </summary>
        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }


        private static readonly StyledProperty<bool> IsSelfLoopedProperty =
            AvaloniaProperty.Register<EdgeControl, bool>(nameof(IsSelfLooped));

        private bool IsSelfLoopedInternal => Source != null && Target != null && Source.Vertex == Target.Vertex;

        /// <summary>
        /// Gets if this edge is self looped (have same Source and Target)
        /// </summary>
        public sealed override bool IsSelfLooped
        {
            get => IsSelfLoopedInternal;
            protected set => SetValue(IsSelfLoopedProperty, value);
        }

        #endregion


        #region public Clean()

        public override void Clean()
        {
            Source = null;
            Target = null;
            Edge = null;
            RootArea = null!;
            HighlightBehaviour.SetIsHighlightEnabled(this, false);
            DragBehaviour.SetIsDragEnabled(this, false);
            LineGeometry = null;
            LinePathObject = null;
            SelfLoopIndicator = null;
            EdgeLabelControls.ForEach(l => l.Dispose());
            EdgeLabelControls.Clear();

            if (EdgePointerForSource != null)
            {
                EdgePointerForSource.Dispose();
                EdgePointerForSource = null;
            }

            if (EdgePointerForTarget != null)
            {
                EdgePointerForTarget.Dispose();
                EdgePointerForTarget = null;
            }

            EventOptions?.Clean();
        }

        #endregion

        #region Vertex position tracing

        private bool _sourceTrace;
        private bool _targetTrace;
        private VertexControl? _oldSource;
        private VertexControl? _oldTarget;

        protected override void OnSourceChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            SourceChanged();
        }

        protected override void OnTargetChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            TargetChanged();
        }

        private void SourceChanged()
        {
            // Only proceed if not in design mode
            if (EventOptions == null)
                return;

            if (_oldSource != null)
            {
                _oldSource.PositionChanged -= source_PositionChanged;
                _oldSource.SizeChanged -= Source_SizeChanged;
                if (_oldSource.EventOptions != null)
                    _oldSource.EventOptions.PositionChangeNotification = _sourceTrace;
            }

            _oldSource = Source;
            if (Source != null)
            {
                if (Source.EventOptions != null)
                {
                    _sourceTrace = Source.EventOptions.PositionChangeNotification;
                    Source.EventOptions.PositionChangeNotification = true;
                }

                Source.PositionChanged += source_PositionChanged;
                Source.SizeChanged += Source_SizeChanged;
            }

            IsSelfLooped = IsSelfLoopedInternal;
            UpdateSelfLoopedEdgeData();
        }

        private void TargetChanged()
        {
            // Only proceed if not in design mode
            if (EventOptions == null)
                return;

            if (_oldTarget != null)
            {
                _oldTarget.PositionChanged -= source_PositionChanged;
                _oldTarget.SizeChanged -= Source_SizeChanged;
                if (_oldTarget.EventOptions != null)
                    _oldTarget.EventOptions.PositionChangeNotification = _targetTrace;
            }

            _oldTarget = Target;
            if (Target != null)
            {
                if (Target.EventOptions != null)
                {
                    _targetTrace = Target.EventOptions.PositionChangeNotification;
                    Target.EventOptions.PositionChangeNotification = true;
                }

                Target.PositionChanged += source_PositionChanged;
                Target.SizeChanged += Source_SizeChanged;
            }

            IsSelfLooped = IsSelfLoopedInternal;
            UpdateSelfLoopedEdgeData();
        }

        private void source_PositionChanged(object sender, EventArgs e)
        {
            //update edge on any connected vertex position changes
            UpdateEdge();
        }

        private void Source_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateEdge();
        }

        #endregion

        static EdgeControl()
        {
            //override the StyleKey
        }

        private bool _clickTrack;
        private Point _clickTrackPoint;

        internal void UpdateEventhandling(EventType typ)
        {
            switch (typ)
            {
                case EventType.MouseClick:
                    if (EventOptions is { MouseClickEnabled: true })
                    {
                        PointerPressed += EdgeControl_MouseDown;
                        PointerMoved += EdgeControl_PreviewMouseMove;
                    }
                    else
                    {
                        PointerPressed -= EdgeControl_MouseDown;
                        PointerMoved -= EdgeControl_PreviewMouseMove;
                    }

                    break;
                case EventType.MouseDoubleClick:
                    if (EventOptions is { MouseDoubleClickEnabled: true })
                        DoubleTapped += EdgeControl_MouseDoubleClick;
                    else DoubleTapped -= EdgeControl_MouseDoubleClick;
                    break;
                case EventType.MouseEnter:
                    if (EventOptions is { MouseEnterEnabled: true }) PointerEntered += EdgeControl_MouseEnter;
                    else PointerEntered -= EdgeControl_MouseEnter;
                    break;
                case EventType.MouseLeave:
                    if (EventOptions is { MouseLeaveEnabled: true }) PointerExited += EdgeControl_MouseLeave;
                    else PointerExited -= EdgeControl_MouseLeave;
                    break;

                case EventType.MouseMove:
                    if (EventOptions is { MouseMoveEnabled: true }) PointerMoved += EdgeControl_MouseMove;
                    else PointerMoved -= EdgeControl_MouseMove;
                    break;
            }

            PointerReleased -= EdgeControl_MouseUp;
            PointerReleased += EdgeControl_MouseUp;
        }

        public EdgeControl()
            : this(null, null, null)
        {
        }

        public EdgeControl(VertexControl? source, VertexControl? target, object? edge, bool showArrows = true)
        {
            DataContext = edge;
            Source = source;
            Target = target;
            Edge = edge;
            DataContext = edge;
            SetCurrentValue(ShowArrowsProperty, showArrows);
            IsHiddenEdgesUpdated = true;

            if (!CustomHelper.IsInDesignMode(this))
            {
                EventOptions = new EdgeEventOptions(this);
                foreach (var item in Enum.GetValues<EventType>())
                    UpdateEventhandling(item);

                // Trigger initial state
                SourceChanged();
                TargetChanged();
            }

            IsSelfLooped = IsSelfLoopedInternal;
        }

        #region Event handlers

        private void EdgeControl_PreviewMouseMove(object? sender, PointerEventArgs pointerEventArgs)
        {
            if (!_clickTrack)
                return;

            var curPoint = pointerEventArgs.GetPosition(RootArea);

            if (curPoint != _clickTrackPoint)
                _clickTrack = false;
        }

        private void EdgeControl_MouseUp(object? sender, PointerReleasedEventArgs e)
        {
            if (IsVisible)
            {
                if (_clickTrack)
                {
                    RaiseEvent(new RoutedEventArgs(ClickEvent, this));
                    RootArea.OnEdgeClicked(this, e, e.KeyModifiers);
                }
            }

            _clickTrack = false;
            e.Handled = true;
        }

        private void EdgeControl_MouseLeave(object? sender, PointerEventArgs e)
        {
            if (IsVisible)
                RootArea.OnEdgeMouseLeave(this, e, e.KeyModifiers);
            // e.Handled = true;
        }

        private void EdgeControl_MouseEnter(object? sender, PointerEventArgs e)
        {
            if (IsVisible)
                RootArea.OnEdgeMouseEnter(this, e, e.KeyModifiers);
            // e.Handled = true;
        }

        private void EdgeControl_MouseMove(object? sender, PointerEventArgs e)
        {
            if (IsVisible)
                RootArea.OnEdgeMouseMove(this, e, e.KeyModifiers);
            // e.Handled = true;
        }

        private void EdgeControl_MouseDoubleClick(object? sender, TappedEventArgs e)
        {
            if (IsVisible)
                RootArea.OnEdgeDoubleClick(this, e, e.KeyModifiers);
            //e.Handled = true;
        }

        private void EdgeControl_MouseDown(object? sender, PointerPressedEventArgs e)
        {
            if (IsVisible)
                RootArea.OnEdgeSelected(this, e, e.KeyModifiers);
            _clickTrack = true;
            _clickTrackPoint = e.GetPosition(RootArea);
            e.Handled = true;
        }

        #endregion

        #region Click Event

        public static readonly RoutedEvent ClickEvent =
            RoutedEvent.Register<EdgeControl, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        #endregion

        public override void Dispose()
        {
            Clean();
        }

        /// <summary>
        /// Gets Edge data as specified class
        /// </summary>
        /// <typeparam name="T">Class</typeparam>
        public T? GetDataEdge<T>() where T : IGraphXCommonEdge
        {
            return (T?)Edge;
        }
    }
}