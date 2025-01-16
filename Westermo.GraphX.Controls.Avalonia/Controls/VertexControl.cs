using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace Westermo.GraphX.Controls.Avalonia
{
    public interface IXYReactive
    {
        void XYChanged(AvaloniaPropertyChangedEventArgs args);
    }

    /// <summary>
    /// Visual vertex control
    /// </summary>
    [Serializable]
    [TemplatePart(Name = "PART_vertexLabel", Type = typeof(IVertexLabelControl))]
    [TemplatePart(Name = "PART_vcproot", Type = typeof(Panel))]
    public class VertexControl : VertexControlBase, IXYReactive
    {
        static VertexControl()
        {
            //override the StyleKey Property
        }

        /// <summary>
        /// Create vertex visual control
        /// </summary>
        /// <param name="vertexData">Vertex data object</param>
        /// <param name="tracePositionChange">Listen for the vertex position changed events and fire corresponding event</param>
        /// <param name="bindToDataObject">Bind DataContext to the Vertex data. True by default. </param>
        public VertexControl(object vertexData, bool tracePositionChange = true, bool bindToDataObject = true)
        {
            if (bindToDataObject) DataContext = vertexData;
            Vertex = vertexData;

            EventOptions = new VertexEventOptions(this) { PositionChangeNotification = tracePositionChange };
            foreach (var item in Enum.GetValues<EventType>())
                UpdateEventhandling(item);
        }

        public T? FindDescendant<T>(TemplateAppliedEventArgs templateAppliedEventArgs, string name) where T : class
        {
            return templateAppliedEventArgs.NameScope.Find<T>(name);
        }

        #region Event tracing

        private bool _clickTrack;
        private Point _clickTrackPoint;

        internal void UpdateEventhandling(EventType typ)
        {
            switch (typ)
            {
                case EventType.MouseClick:
                    if (EventOptions is { MouseClickEnabled: true })
                    {
                        PointerPressed += VertexControl_Down;
                        PointerMoved += VertexControl_PreviewMouseMove;
                    }
                    else
                    {
                        PointerPressed -= VertexControl_Down;
                        PointerMoved -= VertexControl_PreviewMouseMove;
                    }

                    break;

                case EventType.MouseDoubleClick:
                    if (EventOptions is { MouseDoubleClickEnabled: true })
                        DoubleTapped += VertexControl_MouseDoubleClick;
                    else DoubleTapped -= VertexControl_MouseDoubleClick;
                    break;

                case EventType.MouseMove:
                    if (EventOptions is { MouseMoveEnabled: true }) PointerMoved += VertexControl_MouseMove;
                    else PointerMoved -= VertexControl_MouseMove;
                    break;

                case EventType.MouseEnter:
                    if (EventOptions is { MouseEnterEnabled: true }) PointerEntered += VertexControl_MouseEnter;
                    else PointerEntered -= VertexControl_MouseEnter;
                    break;

                case EventType.MouseLeave:
                    if (EventOptions is { MouseLeaveEnabled: true }) PointerExited += VertexControl_MouseLeave;
                    else PointerExited -= VertexControl_MouseLeave;
                    break;
            }

            PointerReleased -= VertexControl_MouseUp;
            PointerReleased += VertexControl_MouseUp;
        }

        private void VertexControl_PreviewMouseMove(object? sender, PointerEventArgs e)
        {
            if (!_clickTrack)
                return;

            var curPoint = RootArea != null ? e.GetPosition(RootArea) : new Point();

            if (curPoint != _clickTrackPoint)
                _clickTrack = false;
        }

        private void VertexControl_MouseUp(object? sender, PointerReleasedEventArgs e)
        {
            if (RootArea != null && IsVisible)
            {
                RootArea.OnVertexMouseUp(this, e, e.KeyModifiers);
                if (_clickTrack)
                {
                    RaiseEvent(new RoutedEventArgs(ClickEvent, this));
                    RootArea.OnVertexClicked(this, e, e.KeyModifiers);
                }
            }

            _clickTrack = false;
            e.Handled = true;
        }

        private void VertexControl_MouseDoubleClick(object? sender, TappedEventArgs e)
        {
            if (RootArea != null && IsVisible)
                RootArea.OnVertexDoubleClick(this, e);
            //e.Handled = true;
        }

        private void VertexControl_Down(object? sender, PointerPressedEventArgs e)
        {
            if (RootArea != null && IsVisible)
                RootArea.OnVertexSelected(this, e, e.KeyModifiers);
            _clickTrack = true;
            _clickTrackPoint = RootArea != null ? e.GetPosition(RootArea) : new Point();
            e.Handled = true;
        }

        #endregion Event tracing

        #region Click Event

        public static readonly RoutedEvent ClickEvent =
            RoutedEvent.Register<VertexControl, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        #endregion Click Event

        /// <summary>
        /// Gets the root element which hosts VCPs so you can add them at runtime. Requires Panel-descendant template item defined named PART_vcproot.
        /// </summary>
        public Panel? VCPRoot { get; protected set; }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (Template == null) return;
            VertexLabelControl ??= FindDescendant<IVertexLabelControl>(e, "PART_vertexLabel");

            VCPRoot ??= FindDescendant<Panel>(e, "PART_vcproot");

            if (VertexLabelControl != null)
            {
                if (ShowLabel) VertexLabelControl.Show();
                else VertexLabelControl.Hide();
                UpdateLayout();
                VertexLabelControl.UpdatePosition();
            }

            VertexConnectionPointsList = this.FindDescendantsOfType<IVertexConnectionPoint>().ToList();
            if (VertexConnectionPointsList.GroupBy(x => x.Id).Count(group => @group.Count() > 1) > 0)
                throw new GX_InvalidDataException(
                    "Vertex connection points in VertexControl template must have unique Id!");
        }

        #region Events handling

        private void VertexControl_MouseLeave(object? sender, PointerEventArgs e)
        {
            if (RootArea != null && IsVisible)
                RootArea.OnVertexMouseLeave(this, e);
        }

        private void VertexControl_MouseEnter(object? sender, PointerEventArgs e)
        {
            if (RootArea != null && IsVisible)
                RootArea.OnVertexMouseEnter(this, e);
        }

        private void VertexControl_MouseMove(object? sender, PointerEventArgs e)
        {
            if (RootArea != null)
                RootArea.OnVertexMouseMove(this, e);
        }

        #endregion Events handling

        /// <summary>
        /// Cleans all potential memory-holding code
        /// </summary>
        public override void Clean()
        {
            Vertex = null;
            RootArea = null!;
            HighlightBehaviour.SetIsHighlightEnabled(this, false);
            DragBehaviour.SetIsDragEnabled(this, false);
            VertexLabelControl = null;

            if (EventOptions != null)
            {
                EventOptions.PositionChangeNotification = false;
                EventOptions.Clean();
            }
        }

        /// <summary>
        /// Gets Vertex data as specified class
        /// </summary>
        /// <typeparam name="T">Class</typeparam>
        public T GetDataVertex<T>() where T : IGraphXVertex
        {
            return (T)Vertex!;
        }

        public void XYChanged(AvaloniaPropertyChangedEventArgs args)
        {
            if (ShowLabel)
                VertexLabelControl?.UpdatePosition();
            OnPositionChanged(new Point(), GetPosition().ToAvaloniaPoint());
        }
    }
}