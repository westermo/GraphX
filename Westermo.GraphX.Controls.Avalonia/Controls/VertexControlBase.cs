using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls.Avalonia.Models;
using Rect = Westermo.GraphX.Measure.Rect;

namespace Westermo.GraphX.Controls.Avalonia
{
    [TemplatePart(Name = "PART_vertexLabel", Type = typeof(IVertexLabelControl))]
    [TemplatePart(Name = "PART_vcproot", Type = typeof(Panel))]
    public abstract class VertexControlBase : TemplatedControl, IGraphControl
    {
        protected internal IVertexLabelControl? VertexLabelControl;

        /// <summary>
        /// Fires when new label is attached to VertexControl
        /// </summary>
        public event EventHandler<EventArgs?>? LabelAttached;

        protected void OnLabelAttached()
        {
            LabelAttached?.Invoke(this, null);
        }

        /// <summary>
        /// Fires when new label is detached from VertexControl
        /// </summary>
        public event EventHandler<EventArgs?>? LabelDetached;

        protected void OnLabelDetached()
        {
            LabelDetached?.Invoke(this, null);
        }

        /// <summary>
        /// Fires when IsPositionTraceEnabled property set and object changes its coordinates.
        /// </summary>
        public event VertexPositionChangedEH? PositionChanged;

        protected void OnPositionChanged(Point offset, Point pos)
        {
            PositionChanged?.Invoke(this, new VertexPositionEventArgs(offset, pos, this));
        }

        /// <summary>
        /// Hides this control with all related edges
        /// </summary>
        public void HideWithEdges()
        {
            SetCurrentValue(IsVisibleProperty, false);
            SetConnectionPointsVisibility(false);
            RootArea?.GetRelatedControls(this, GraphControlType.Edge, EdgesType.All).ForEach(a =>
            {
                a.IsVisible = false;
            });
        }

        /// <summary>
        /// Shows this control with all related edges
        /// </summary>
        public void ShowWithEdges()
        {
            SetCurrentValue(IsVisibleProperty, true);
            SetConnectionPointsVisibility(true);
            RootArea?.GetRelatedControls(this, GraphControlType.Edge, EdgesType.All).ForEach(a =>
            {
                if (a is EdgeControlBase @base)
                    @base.SetVisibility(true);
                else a.IsVisible = true;
            });
        }

        #region Properties

        /// <summary>
        /// List of found vertex connection points
        /// </summary>
        public List<IVertexConnectionPoint> VertexConnectionPointsList { get; protected set; } = [];

        /// <summary>
        /// Provides settings for event calls within single vertex control
        /// </summary>
        public VertexEventOptions? EventOptions { get; protected set; }

        private double _labelAngle;

        /// <summary>
        /// Gets or sets vertex label angle
        /// </summary>
        public double LabelAngle
        {
            get => VertexLabelControl?.Angle ?? _labelAngle;
            set
            {
                _labelAngle = value;
                if (VertexLabelControl != null)
                    VertexLabelControl.Angle = _labelAngle;
            }
        }

        public static readonly StyledProperty<VertexShape> VertexShapeProperty =
            AvaloniaProperty.Register<VertexControlBase, VertexShape>(nameof(VertexShape));

        /// <summary>
        /// Gets or sets actual shape form of vertex control (affects mostly math calculations such edges connectors)
        /// </summary>
        public VertexShape VertexShape
        {
            get => GetValue(VertexShapeProperty);
            set => SetValue(VertexShapeProperty, value);
        }

        /// <summary>
        /// Gets or sets vertex data object
        /// </summary>
        public object? Vertex
        {
            get => GetValue(VertexProperty);
            set => SetValue(VertexProperty, value);
        }

        public static readonly StyledProperty<object?> VertexProperty =
            AvaloniaProperty.Register<VertexControlBase, object?>(nameof(Vertex));

        /// <summary>
        /// Gets or sets vertex control parent GraphArea object (don't need to be set manualy)
        /// </summary>
        public GraphAreaBase? RootArea
        {
            get => GetValue(RootCanvasProperty);
            set => SetValue(RootCanvasProperty, value);
        }

        public static readonly StyledProperty<GraphAreaBase?> RootCanvasProperty =
            AvaloniaProperty.Register<VertexControlBase, GraphAreaBase?>(nameof(RootArea));

        public static readonly StyledProperty<bool> ShowLabelProperty =
            AvaloniaProperty.Register<VertexControlBase, bool>(
                nameof(ShowLabel));

        static VertexControlBase()
        {
            ShowLabelProperty.Changed.AddClassHandler<VertexControlBase>(ShowLabelChanged);
        }

        private static void ShowLabelChanged(VertexControlBase obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj.VertexLabelControl == null) return;
            if (e.NewValue is true) obj.VertexLabelControl.Show();
            else obj.VertexLabelControl.Hide();
        }

        public bool ShowLabel
        {
            get => GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        #endregion

        #region Position methods

        /// <summary>
        /// Set attached coordinates X and Y
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="alsoFinal"></param>
        public void SetPosition(Measure.Point pt, bool alsoFinal = true)
        {
            GraphAreaBase.SetX(this, pt.X, alsoFinal);
            GraphAreaBase.SetY(this, pt.Y, alsoFinal);
        }

        public void SetPosition(double x, double y, bool alsoFinal = true)
        {
            GraphAreaBase.SetX(this, x, alsoFinal);
            GraphAreaBase.SetY(this, y, alsoFinal);
        }

        public abstract void Clean();

        /// <summary>
        /// Get control position on the GraphArea panel in attached coords X and Y
        /// </summary>
        /// <param name="final"></param>
        /// <param name="round"></param>
        public Measure.Point GetPosition(bool final = false, bool round = false)
        {
            return round
                ? new Measure.Point(final ? (int)GraphAreaBase.GetFinalX(this) : (int)GraphAreaBase.GetX(this),
                    final ? (int)GraphAreaBase.GetFinalY(this) : (int)GraphAreaBase.GetY(this))
                : new Measure.Point(final ? GraphAreaBase.GetFinalX(this) : GraphAreaBase.GetX(this),
                    final ? GraphAreaBase.GetFinalY(this) : GraphAreaBase.GetY(this));
        }

        /// <summary>
        /// Get control position on the GraphArea panel in attached coords X and Y (GraphX type version)
        /// </summary>
        /// <param name="final"></param>
        /// <param name="round"></param>
        internal Measure.Point GetPositionGraphX(bool final = false, bool round = false)
        {
            return round
                ? new Measure.Point(final ? (int)GraphAreaBase.GetFinalX(this) : (int)GraphAreaBase.GetX(this),
                    final ? (int)GraphAreaBase.GetFinalY(this) : (int)GraphAreaBase.GetY(this))
                : new Measure.Point(final ? GraphAreaBase.GetFinalX(this) : GraphAreaBase.GetX(this),
                    final ? GraphAreaBase.GetFinalY(this) : GraphAreaBase.GetY(this));
        }

        #endregion

        /// <summary>
        /// Get vertex center position
        /// </summary>
        public Point GetCenterPosition(bool final = false)
        {
            var pos = GetPosition();
            return new Point(pos.X + Width * .5, pos.Y + Height * .5);
        }

        /// <summary>
        /// Returns first connection point found with specified ID
        /// </summary>
        /// <param name="id">Connection point identifier</param>
        /// <param name="runUpdate">Update connection point if found</param>
        public IVertexConnectionPoint? GetConnectionPointById(int id, bool runUpdate = false)
        {
            var result = VertexConnectionPointsList.FirstOrDefault(a => a.Id == id);
            result?.Update();
            return result;
        }

        public IVertexConnectionPoint? GetConnectionPointAt(Point position)
        {
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return VertexConnectionPointsList.FirstOrDefault(a =>
            {
                var rect = new Rect(a.RectangularSize.X, a.RectangularSize.Y, a.RectangularSize.Width,
                    a.RectangularSize.Height);
                return rect.Contains(position.ToGraphX());
            });
        }

        /// <summary>
        /// Internal method. Attaches label to control
        /// </summary>
        /// <param name="ctrl">Control</param>
        public void AttachLabel(IVertexLabelControl ctrl)
        {
            VertexLabelControl = ctrl;
            OnLabelAttached();
        }

        /// <summary>
        /// Internal method. Detaches label from control.
        /// </summary>
        public void DetachLabel()
        {
            (VertexLabelControl as IAttachableControl<VertexControl>)?.Detach();
            VertexLabelControl = null;
            OnLabelDetached();
        }

        /// <summary>
        /// Sets visibility of all connection points
        /// </summary>
        /// <param name="isVisible"></param>
        public void SetConnectionPointsVisibility(bool isVisible)
        {
            foreach (var item in VertexConnectionPointsList)
            {
                if (isVisible) item.Show();
                else item.Hide();
            }
        }
    }
}