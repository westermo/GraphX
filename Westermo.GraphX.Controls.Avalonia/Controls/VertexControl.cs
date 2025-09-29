using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree; // added for visual tree traversal without re-applying template on self
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
            DoubleTapped += OnDoubleTapped;
        }

        ~VertexControl()
        {
            DoubleTapped -= OnDoubleTapped;
        }

        public static T? FindDescendant<T>(TemplateAppliedEventArgs templateAppliedEventArgs, string name)
            where T : class
        {
            return templateAppliedEventArgs.NameScope.Find<T>(name);
        }

        #region Event tracing

        private bool _clickTrack;
        private Point _clickTrackPoint;

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_clickTrack)
            {
                var curPoint = RootArea != null ? e.GetPosition(RootArea) : new Point();

                if (curPoint != _clickTrackPoint)
                    _clickTrack = false;
            }

            RootArea?.OnVertexMouseMove(this, e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
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

        private void OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (!IsVisible) return;
            RootArea?.OnVertexDoubleClick(this, e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
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

            // Avoid infinite recursion: the previous implementation called this.FindDescendantsOfType<>()
            // which internally invoked ApplyTemplate() on the control itself while already inside OnApplyTemplate,
            // leading to repeated OnApplyTemplate calls. We now start the search from VCPRoot if available, otherwise
            // only traverse direct visual children of this control (without re-applying this template).
            if (VCPRoot is Visual vcpVisual)
            {
                VertexConnectionPointsList = vcpVisual.FindDescendantsOfType<IVertexConnectionPoint>().ToList();
            }
            else
            {
                var result = new System.Collections.Generic.List<IVertexConnectionPoint>();
                if (this is Visual selfVisual)
                {
                    foreach (var child in selfVisual.GetVisualChildren())
                    {
                        foreach (var item in child.FindDescendantsOfType<IVertexConnectionPoint>())
                            result.Add(item);
                    }
                }

                VertexConnectionPointsList = result;
            }

            if (VertexConnectionPointsList.GroupBy(x => x.Id).Count(group => @group.Count() > 1) > 0)
                throw new GX_InvalidDataException(
                    "Vertex connection points in VertexControl template must have unique Id!");
        }

        #region Events handling

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            if (!IsVisible) return;
            RootArea?.OnVertexMouseEnter(this, e);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (!IsVisible) return;
            RootArea?.OnVertexMouseLeave(this, e);
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
            OnPositionChanged(new Point(), GetPosition());
        }
    }
}