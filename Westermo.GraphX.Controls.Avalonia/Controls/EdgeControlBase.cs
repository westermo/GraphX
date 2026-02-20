using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl.Helpers;
using Westermo.GraphX.Controls.Models.Interfaces;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Base class for edge controls that provides common functionality for visual representation of graph edges.
/// </summary>
/// <remarks>
/// This abstract class provides:
/// - Edge geometry generation and caching for performance optimization
/// - Edge pointer (arrow) management for source and target endpoints
/// - Self-loop edge support for edges connecting a vertex to itself
/// - Dash style and stroke customization
/// - Label attachment for edge annotations
/// - Integration with GraphAreaBase for event handling
/// </remarks>
[TemplatePart(Name = "PART_edgePath", Type = typeof(Path))]
[TemplatePart(Name = "PART_SelfLoopedEdge", Type = typeof(Control))]
[TemplatePart(Name = "PART_edgeLabel", Type = typeof(IEdgeLabelControl))] //obsolete, present for exception
[TemplatePart(Name = "PART_EdgePointerForSource", Type = typeof(IEdgePointer))]
[TemplatePart(Name = "PART_EdgePointerForTarget", Type = typeof(IEdgePointer))]
public abstract class EdgeControlBase : TemplatedControl, IGraphControl, IDisposable
{
    /// <summary>
    /// Gets or sets if edge pointer should be hidden when source and target vertices are overlapped making the 0 length edge. Default value is True.
    /// </summary>
    public bool HideEdgePointerOnVertexOverlap { get; set; } = true;

    /// <summary>
    /// Gets or sets the length of the edge to hide the edge pointers if less than or equal to. Default value is 0 (do not hide).
    /// </summary>
    public double HideEdgePointerByEdgeLength { get; set; } = 0.0d;

    /// <summary>
    /// Gets whether this edge is a self-loop (connects a vertex to itself).
    /// </summary>
    public abstract bool IsSelfLooped { get; protected set; }

    /// <summary>
    /// Disposes of this edge control and releases any resources.
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// Cleans up all references and resources held by this edge control.
    /// </summary>
    public abstract void Clean();

    protected AvaloniaList<double>? StrokeDashArray { get; set; }

    /// <summary>
    /// Gets if this edge is parallel (has another edge with the same source and target vertices)
    /// </summary>
    public bool IsParallel { get; internal set; }

    /// <summary>
    /// Element presenting self looped edge
    /// </summary>
    protected Control? SelfLoopIndicator;

    /// <summary>
    /// Used to store last known SLE rect size for proper updates on layout passes
    /// </summary>
    private Rect _selfLoopedEdgeLastKnownRect;

    /// <summary>
    /// Stores the local coordinate offset used when normalizing edge geometry.
    /// Edge pointers need to be positioned relative to this offset.
    /// </summary>
    private Point _localOffset;

    /// <summary>
    /// Stores pending edge pointer data to be applied after geometry is built.
    /// </summary>
    private struct EdgePointerData
    {
        public Point Position;
        public Point DirectionTarget;
        public bool AllowUnsuppress;
        public bool HasData;
    }

    private EdgePointerData _pendingSourcePointer;
    private EdgePointerData _pendingTargetPointer;

    /// <summary>
    /// Calculates the padding needed to prevent edge pointers from being clipped.
    /// </summary>
    private double GetEdgePointerPadding()
    {
        var padding = 0.0;
        if (EdgePointerForSource is Control sourceCtrl)
        {
            padding = Math.Max(padding, Math.Max(sourceCtrl.DesiredSize.Width, sourceCtrl.DesiredSize.Height));
        }

        if (EdgePointerForTarget is Control targetCtrl)
        {
            padding = Math.Max(padding, Math.Max(targetCtrl.DesiredSize.Width, targetCtrl.DesiredSize.Height));
        }

        // Add extra padding to ensure arrows aren't clipped at edges
        return padding > 0 ? padding + 2 : 0;
    }

    /// <summary>
    /// Stores edge pointer target data to be applied after geometry normalization.
    /// Returns the amount to subtract from the edge endpoint to account for the pointer.
    /// </summary>
    private Point StoreSourcePointerData(Point from, Point to, bool allowUnsuppress = true)
    {
        _pendingSourcePointer = new EdgePointerData
        {
            Position = from,
            DirectionTarget = to,
            AllowUnsuppress = allowUnsuppress,
            HasData = true
        };

        // Calculate the offset that should be subtracted from the line endpoint
        var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
        if (from == to)
            dir = new Measure.Vector(0, 0);

        if (EdgePointerForSource is not Control ctrl)
            return new Point();

        var width = double.IsNaN(ctrl.Width) ? ctrl.Bounds.Width : ctrl.Width;
        var height = double.IsNaN(ctrl.Height) ? ctrl.Bounds.Height : ctrl.Height;
        if (width == 0) width = ctrl.DesiredSize.Width;
        if (height == 0) height = ctrl.DesiredSize.Height;

        return new Point(dir.X * width, dir.Y * height);
    }

    /// <summary>
    /// Stores edge pointer target data to be applied after geometry normalization.
    /// Returns the amount to subtract from the edge endpoint to account for the pointer.
    /// </summary>
    private Point StoreTargetPointerData(Point from, Point to, bool allowUnsuppress = true)
    {
        _pendingTargetPointer = new EdgePointerData
        {
            Position = from,
            DirectionTarget = to,
            AllowUnsuppress = allowUnsuppress,
            HasData = true
        };

        // Calculate the offset that should be subtracted from the line endpoint
        var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
        if (from == to)
            dir = new Measure.Vector(0, 0);

        if (EdgePointerForTarget is not Control ctrl)
            return new Point();

        var width = double.IsNaN(ctrl.Width) ? ctrl.Bounds.Width : ctrl.Width;
        var height = double.IsNaN(ctrl.Height) ? ctrl.Bounds.Height : ctrl.Height;
        if (width == 0) width = ctrl.DesiredSize.Width;
        if (height == 0) height = ctrl.DesiredSize.Height;

        return new Point(dir.X * width, dir.Y * height);
    }

    /// <summary>
    /// Applies pending edge pointer positions after geometry has been normalized.
    /// This must be called AFTER BuildNormalizedLineFigure/BuildNormalizedStreamGeometry.
    /// </summary>
    private void ApplyPendingEdgePointers()
    {
        if (_pendingSourcePointer.HasData && EdgePointerForSource != null)
        {
            var from = _pendingSourcePointer.Position;
            var to = _pendingSourcePointer.DirectionTarget;
            var allowUnsuppress = _pendingSourcePointer.AllowUnsuppress;

            var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
            if (from == to)
            {
                if (HideEdgePointerOnVertexOverlap) EdgePointerForSource.Suppress();
                else dir = new Measure.Vector(0, 0);
            }
            else if (allowUnsuppress) EdgePointerForSource.UnSuppress();

            // Convert to local coordinates using the now-known offset
            var localFrom = new Measure.Point(from.X - _localOffset.X, from.Y - _localOffset.Y);
            MeasureChild(EdgePointerForSource as Control);
            EdgePointerForSource.Update(localFrom, dir,
                EdgePointerForSource.NeedRotation
                    ? -MathHelper.GetAngleBetweenPoints(from.ToGraphX(), to.ToGraphX()).ToDegrees()
                    : 0);

            // Show pointer now that it's properly positioned
            if (ShowArrows) EdgePointerForSource.Show();

            _pendingSourcePointer = default;
        }

        if (_pendingTargetPointer.HasData && EdgePointerForTarget != null)
        {
            var from = _pendingTargetPointer.Position;
            var to = _pendingTargetPointer.DirectionTarget;
            var allowUnsuppress = _pendingTargetPointer.AllowUnsuppress;

            var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
            if (from == to)
            {
                if (HideEdgePointerOnVertexOverlap) EdgePointerForTarget.Suppress();
                else dir = new Measure.Vector(0, 0);
            }
            else if (allowUnsuppress) EdgePointerForTarget.UnSuppress();

            // Convert to local coordinates using the now-known offset
            var localFrom = new Measure.Point(from.X - _localOffset.X, from.Y - _localOffset.Y);
            MeasureChild(EdgePointerForTarget as Control);
            EdgePointerForTarget.Update(localFrom, dir,
                EdgePointerForTarget.NeedRotation
                    ? -MathHelper.GetAngleBetweenPoints(from.ToGraphX(), to.ToGraphX()).ToDegrees()
                    : 0);

            // Show pointer now that it's properly positioned
            if (ShowArrows) EdgePointerForTarget.Show();

            _pendingTargetPointer = default;
        }
    }

    protected virtual void OnSourceChanged(Control d, AvaloniaPropertyChangedEventArgs e)
    {
    }

    protected virtual void OnTargetChanged(Control d, AvaloniaPropertyChangedEventArgs e)
    {
    }

    /// <summary>
    /// Gets or sets parent GraphArea visual
    /// </summary>
    public GraphAreaBase? RootArea
    {
        get => GetValue(RootCanvasProperty);
        set => SetValue(RootCanvasProperty, value);
    }

    public static readonly StyledProperty<GraphAreaBase?> RootCanvasProperty =
        AvaloniaProperty.Register<EdgeControlBase, GraphAreaBase?>(nameof(RootArea));

    public static readonly StyledProperty<double> SelfLoopIndicatorRadiusProperty =
        AvaloniaProperty.Register<EdgeControlBase, double>(
            nameof(SelfLoopIndicatorRadius), 5d);

    /// <summary>
    /// Radius of the default self-loop indicator, which is drawn as a circle (when custom template isn't provided). Default is 20.
    /// </summary>
    public double SelfLoopIndicatorRadius
    {
        get => GetValue(SelfLoopIndicatorRadiusProperty);
        set => SetValue(SelfLoopIndicatorRadiusProperty, value);
    }

    public static readonly StyledProperty<Point> SelfLoopIndicatorOffsetProperty =
        AvaloniaProperty.Register<EdgeControlBase, Point>(nameof(SelfLoopIndicatorOffset));

    /// <summary>
    /// Offset from the left-top corner of the vertex. Useful for custom vertex shapes. Default is 10,10.
    /// </summary>
    public Point SelfLoopIndicatorOffset
    {
        get => GetValue(SelfLoopIndicatorOffsetProperty);
        set => SetValue(SelfLoopIndicatorOffsetProperty, value);
    }

    public static readonly StyledProperty<bool> ShowSelfLoopIndicatorProperty =
        AvaloniaProperty.Register<EdgeControlBase, bool>(
            nameof(ShowSelfLoopIndicator), true);

    /// <summary>
    /// Show self looped edge indicator on the vertex top-left corner. Default value is true.
    /// </summary>
    public bool ShowSelfLoopIndicator
    {
        get => GetValue(ShowSelfLoopIndicatorProperty);
        set => SetValue(ShowSelfLoopIndicatorProperty, value);
    }

    public static readonly StyledProperty<VertexControl?> SourceProperty =
        AvaloniaProperty.Register<EdgeControlBase, VertexControl?>(nameof(Source));

    static EdgeControlBase()
    {
        AffectsMeasure<EdgeControlBase>(SourceProperty, TargetProperty, ShowArrowsProperty, DashStyleProperty,
            ShowSelfLoopIndicatorProperty, SelfLoopIndicatorRadiusProperty, SelfLoopIndicatorOffsetProperty);
        SourceProperty.Changed.AddClassHandler<EdgeControlBase>(OnSourceChangedInternal);
        TargetProperty.Changed.AddClassHandler<EdgeControlBase>(OnTargetChangedInternal);
        DashStyleProperty.Changed.AddClassHandler<EdgeControlBase>(DashStyleChanged);
        ShowArrowsProperty.Changed.AddClassHandler<EdgeControlBase>(ShowArrowsChanged);
    }

    private static void OnSourceChangedInternal(EdgeControlBase control,
        AvaloniaPropertyChangedEventArgs avaloniaPropertyChangedEventArgs)
    {
        control.OnSourceChanged(control, avaloniaPropertyChangedEventArgs);
    }

    public static readonly StyledProperty<VertexControl?> TargetProperty =
        AvaloniaProperty.Register<EdgeControlBase, VertexControl?>(nameof(Target));

    private static void OnTargetChangedInternal(EdgeControlBase control,
        AvaloniaPropertyChangedEventArgs avaloniaPropertyChangedEventArgs)
    {
        control.OnTargetChanged(control, avaloniaPropertyChangedEventArgs);
    }

    public static readonly StyledProperty<object?> EdgeProperty =
        AvaloniaProperty.Register<EdgeControlBase, object?>(nameof(Edge));


    public static readonly StyledProperty<EdgeDashStyle> DashStyleProperty =
        AvaloniaProperty.Register<EdgeControlBase, EdgeDashStyle>(nameof(DashStyle));

    private static void DashStyleChanged(EdgeControlBase edgeControlBase, AvaloniaPropertyChangedEventArgs e)
    {
        edgeControlBase.StrokeDashArray = (EdgeDashStyle?)e.NewValue switch
        {
            EdgeDashStyle.Solid => null,
            EdgeDashStyle.Dash => [4.0, 2.0],
            EdgeDashStyle.Dot => [1.0, 2.0],
            EdgeDashStyle.DashDot => [4.0, 2.0, 1.0, 2.0],
            EdgeDashStyle.DashDotDot => [4.0, 2.0, 1.0, 2.0, 1.0, 2.0],
            _ => null,
        };
    }

    /// <summary>
    /// Gets or sets edge dash style
    /// </summary>
    public EdgeDashStyle DashStyle
    {
        get => GetValue(DashStyleProperty);
        set => SetValue(DashStyleProperty, value);
    }


    /// <summary>
    /// Gets or sets if this edge can be paralellized if GraphArea.EnableParallelEdges is true.
    /// If not it will be drawn by default.
    /// </summary>
    public bool CanBeParallel { get; set; } = true;

    protected EdgeControlBase()
    {
        Loaded += EdgeControlBase_Loaded;
    }


    private void EdgeControlBase_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= EdgeControlBase_Loaded;
    }


    /// <summary>
    /// Gets or set if hidden edges should be updated when connected vertices positions are changed. Default value is True.
    /// </summary>
    public bool IsHiddenEdgesUpdated { get; set; }

    public static readonly StyledProperty<bool> ShowArrowsProperty =
        AvaloniaProperty.Register<EdgeControlBase, bool>(nameof(ShowArrows), true);

    private static void ShowArrowsChanged(EdgeControlBase edgeControlBase,
        AvaloniaPropertyChangedEventArgs avaloniaPropertyChangedEventArgs)
    {
        if (edgeControlBase.ShowArrows)
        {
            edgeControlBase.EdgePointerForSource?.Show();
            edgeControlBase.EdgePointerForTarget?.Show();
        }
        else
        {
            edgeControlBase.EdgePointerForSource?.Hide();
            edgeControlBase.EdgePointerForTarget?.Hide();
        }
    }

    /// <summary>
    /// Show arrows on the edge ends. Default value is true.
    /// </summary>
    public bool ShowArrows
    {
        get => GetValue(ShowArrowsProperty);
        set => SetValue(ShowArrowsProperty, value);
    }

    /// <summary>
    ///  Gets or Sets that user controls the path geometry object or it is generated automatically
    /// </summary>
    public bool ManualDrawing { get; set; }

    /// <summary>
    /// Geometry object that represents visual edge path. Applied in OnApplyTemplate and OnRender.
    /// </summary>
    protected Geometry? LineGeometry;

    /// <summary>
    /// Gets the bounds of the edge geometry for culling purposes.
    /// Returns null if no geometry has been computed yet.
    /// </summary>
    public Rect? GeometryBounds => LineGeometry?.Bounds;

    /// <summary>
    /// Templated Path object to operate with routed path
    /// </summary>
    protected Path? LinePathObject;

    private IList<IEdgeLabelControl> _edgeLabelControls = [];

    /// <summary>
    /// Templated label control to display labels
    /// </summary>
    protected internal IList<IEdgeLabelControl> EdgeLabelControls
    {
        get => _edgeLabelControls;
        set
        {
            _edgeLabelControls = value;
            OnEdgeLabelUpdated();
        }
    }

    protected IEdgePointer? EdgePointerForSource;
    protected IEdgePointer? EdgePointerForTarget;

    /// <summary>
    /// Returns edge pointer for source if any
    /// </summary>
    public IEdgePointer? GetEdgePointerForSource()
    {
        return EdgePointerForSource;
    }

    /// <summary>
    /// Returns edge pointer for target if any
    /// </summary>
    public IEdgePointer? GetEdgePointerForTarget()
    {
        return EdgePointerForTarget;
    }

    /// <summary>
    /// Source visual vertex object
    /// </summary>
    public VertexControl? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Target visual vertex object
    /// </summary>
    public VertexControl? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>
    /// Data edge object
    /// </summary>
    public object? Edge
    {
        get => GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    /// <summary>
    /// Internal method. Attaches label to control
    /// </summary>
    /// <param name="ctrl"></param>
    public void AttachLabel(IEdgeLabelControl ctrl)
    {
        if (RootArea is null) return;
        EdgeLabelControls.Add(ctrl);
        if (!RootArea.Children.Contains((Control)ctrl))
            RootArea.Children.Add((Control)ctrl);
        ctrl.Show();
        var r = ctrl.GetSize();
        if (!r.IsEmpty()) return;
        ctrl.UpdateLayout();
        ctrl.UpdatePosition();
    }

    /// <summary>
    /// Internal method. Detaches label from control.
    /// </summary>
    public void DetachLabels(IEdgeLabelControl? ctrl = null)
    {
        EdgeLabelControls.Where(l => l is IAttachableControl<EdgeControl>).Cast<IAttachableControl<EdgeControl>>()
            .ForEach(label =>
            {
                label.Detach();
                RootArea?.Children.Remove((Control)label);
            });
        EdgeLabelControls.Clear();
    }

    /// <summary>
    /// Update edge label if any
    /// </summary>
    public void UpdateLabel()
    {
        _edgeLabelControls.Where(l => l.ShowLabel).ForEach(l =>
        {
            l.Show();
            l.UpdateLayout();
            l.UpdatePosition();
        });
    }


    /// <summary>
    /// Set attached coordinates X and Y
    /// </summary>
    /// <param name="pt"></param>
    /// <param name="alsoFinal"></param>
    public void SetPosition(Point pt, bool alsoFinal = true)
    {
        GraphAreaBase.SetX(this, pt.X, alsoFinal);
        GraphAreaBase.SetY(this, pt.Y, alsoFinal);
    }

    public void SetPosition(double x, double y, bool alsoFinal = true)
    {
        GraphAreaBase.SetX(this, x, alsoFinal);
        GraphAreaBase.SetY(this, y, alsoFinal);
    }

    /// <summary>
    /// Get control position on the GraphArea panel in attached coords X and Y
    /// </summary>
    /// <param name="final"></param>
    /// <param name="round"></param>
    public Point GetPosition(bool final = false, bool round = false)
    {
        return new Point(final ? GraphAreaBase.GetFinalX(this) : GraphAreaBase.GetX(this),
            final ? GraphAreaBase.GetFinalY(this) : GraphAreaBase.GetY(this));
    }

    /// <summary>
    /// Get control position on the GraphArea panel in attached coords X and Y
    /// </summary>
    /// <param name="final"></param>
    /// <param name="round"></param>
    internal Point GetPositionGraphX(bool final = false, bool round = false)
    {
        return new Point(final ? GraphAreaBase.GetFinalX(this) : GraphAreaBase.GetX(this),
            final ? GraphAreaBase.GetFinalY(this) : GraphAreaBase.GetY(this));
    }


    /// <summary>
    /// Gets current edge path geometry object
    /// </summary>
    public PathGeometry? GetEdgePathManually()
    {
        if (!ManualDrawing) return null;
        return LineGeometry as PathGeometry;
    }

    /// <summary>
    /// Sets current edge path geometry object
    /// </summary>
    public void SetEdgePathManually(PathGeometry geo)
    {
        if (!ManualDrawing) return;
        LineGeometry = geo;
    }

    /// <summary>
    /// Gets if Template has been loaded and edge can operate at 100%
    /// </summary>
    public bool IsTemplateLoaded => LinePathObject != null;

    protected virtual void OnEdgeLabelUpdated()
    {
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (Template == null) return;

        LinePathObject = GetTemplatePart(e, "PART_edgePath") as Path;
        if (LinePathObject == null)
            throw new GX_ObjectNotFoundException(
                "EdgeControlBase Template -> Edge template must contain 'PART_edgePath' Path object to draw route points!");
        LinePathObject.Data = LineGeometry;

        //EdgeLabelControl = EdgeLabelControl ?? GetTemplatePart("PART_edgeLabel") as IEdgeLabelControl;
        if (GetTemplatePart(e, "PART_edgeLabel") != null)
            throw new GX_ObsoleteException("PART_edgeLabel is obsolete. Please use attachable labels mechanics!");

        EdgePointerForSource = GetTemplatePart(e, "PART_EdgePointerForSource") as IEdgePointer;
        EdgePointerForTarget = GetTemplatePart(e, "PART_EdgePointerForTarget") as IEdgePointer;

        SelfLoopIndicator = GetTemplatePart(e, "PART_SelfLoopedEdge") as Control;
        if (SelfLoopIndicator != null)
            SelfLoopIndicator.LayoutUpdated += (_, _) => { SelfLoopIndicator?.Arrange(_selfLoopedEdgeLastKnownRect); };
        // var x = ShowLabel;
        MeasureChild(EdgePointerForSource as Control);
        MeasureChild(EdgePointerForTarget as Control);
        MeasureChild(SelfLoopIndicator);
    }

    // Re-added after edit: measure template child once with unlimited size so DesiredSize is initialized
    protected void MeasureChild(Control? child)
    {
        if (child == null) return;

        // Ensure the child's template is applied so content is available for measuring
        if (child is TemplatedControl templated)
        {
            templated.ApplyTemplate();
        }

        child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
    }

    private static Size Sum(params ReadOnlySpan<Size> sizes)
    {
        var totalWidth = 0.0;
        var totalHeight = 0.0;
        foreach (var size in sizes)
        {
            totalWidth += double.IsNaN(size.Width) ? 0 : size.Width;
            totalHeight += double.IsNaN(size.Height) ? 0 : size.Height;
        }

        return new Size(totalWidth, totalHeight);
    }

    // Provide a desired size for layout based on current geometry bounds so edge is not collapsed to 0x0.
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!IsTemplateLoaded)
            ApplyTemplate();
        var sourcePos = Source?.GetPosition() ?? new Point();
        var targetPos = OverrideEndpoint ?? Target?.GetPosition() ?? new Point();
        var selfLoopSize = IsSelfLooped
            ? new Size(SelfLoopIndicatorRadius * 2 + SelfLoopIndicatorOffset.X,
                SelfLoopIndicatorRadius * 2 + SelfLoopIndicatorOffset.Y)
            : new Size();
        var sourceSize = Source?.DesiredSize ?? new Size();
        var targetSize = Target?.DesiredSize ?? new Size();
        var sourceIsLeftOfTarget = sourcePos.X < targetPos.X;
        var sourceIsAboveTarget = sourcePos.Y < targetPos.Y;
        var bottom = sourceIsAboveTarget ? targetPos.Y + targetSize.Height : sourcePos.Y + sourceSize.Height;
        var right = sourceIsLeftOfTarget ? targetPos.X + targetSize.Width : sourcePos.X + sourceSize.Width;
        var top = sourceIsAboveTarget ? sourcePos.Y : targetPos.Y;
        var left = sourceIsLeftOfTarget ? sourcePos.X : targetPos.X;
        if (EdgePointerForSource is Control pointerForSource) pointerForSource.Measure(availableSize);
        if (EdgePointerForTarget is Control pointerForTarget) pointerForTarget.Measure(availableSize);
        if (SelfLoopIndicator is { } selfLoopIndicator) selfLoopIndicator.Measure(availableSize);
        return Sum(new Size(right - left, bottom - top), selfLoopSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        //first show label to get DesiredSize - optimized: avoid LINQ allocation
        foreach (var l in EdgeLabelControls)
        {
            switch (l.ShowLabel)
            {
                case true:
                    l.UpdatePosition();
                    l.Show();
                    break;
                default:
                    l.Hide();
                    break;
            }
        }

        //use final vertex coordinates (layout results) instead of current to avoid drawing collapsed edges before animation/position commit
        LineGeometry = PrepareEdgePath();
        foreach (var l in EdgeLabelControls)
        {
            if (l.ShowLabel)
                l.UpdatePosition();
        }

        if (LinePathObject == null) return base.ArrangeOverride(finalSize);
        LinePathObject.Data = LineGeometry;
        LinePathObject.StrokeDashArray = StrokeDashArray;
        return base.ArrangeOverride(finalSize);
    }

    internal int ParallelEdgeOffset;

    /// <summary>
    /// Gets the offset point for edge parallelization
    /// </summary>
    /// <param name="sourceCenter">Source vertex</param>
    /// <param name="targetCenter">Target vertex</param>
    /// <param name="sideDistance">Distance between edges</param>
    protected virtual Point GetParallelOffset(Point sourceCenter, Point targetCenter, int sideDistance)
    {
        var mainVector = new Vector(targetCenter.X - sourceCenter.X, targetCenter.Y - sourceCenter.Y);
        //get new point coordinate
        var joint = new Point(
            sourceCenter.X + sideDistance * (mainVector.Y / mainVector.Length),
            sourceCenter.Y - sideDistance * (mainVector.X / mainVector.Length));
        return joint;
    }

    /// <summary>
    /// Internal value to store last calculated Source vertex connection point
    /// </summary>
    protected internal Point? SourceConnectionPoint;

    /// <summary>
    /// Internal value to store last calculated Target vertex connection point
    /// </summary>
    protected internal Point? TargetConnectionPoint;

    protected Point? OverrideEndpoint
    {
        get;
        set
        {
            field = value;
            InvalidateMeasure();
        }
    }

    // Added for testing & diagnostics: public read-only accessors for connection points
    public Point? SourceEndpoint => SourceConnectionPoint;
    public Point? TargetEndpoint => TargetConnectionPoint;

    /// <summary>
    ///Gets is looped edge indicator template available. Used to pass some heavy cycle checks.
    /// </summary>
    protected bool HasSelfLoopedEdgeTemplate => SelfLoopIndicator != null;

    /// <summary>
    /// Update SLE data such as template, edge pointers visibility
    /// </summary>
    protected virtual void UpdateSelfLoopedEdgeData()
    {
        //generate object if template is present
        if (IsSelfLooped)
        {
            //hide edge pointers
            EdgePointerForSource?.Hide();
            EdgePointerForTarget?.Hide();

            //return if we don't need to show edge loops
            if (!ShowSelfLoopIndicator) return;

            //pregenerate built-in indicator geometry if template PART is absent
            if (!HasSelfLoopedEdgeTemplate)
                LineGeometry = new EllipseGeometry();
            else SelfLoopIndicator?.IsVisible = true;
        }
        else
        {
            if (HasSelfLoopedEdgeTemplate)
                SelfLoopIndicator?.IsVisible = false;
        }
    }

    /// <summary>
    /// Builds a <see cref="StreamGeometry"/> from normalized points. <see cref="StreamGeometry"/> is more performant than <see cref="PathGeometry"/>
    /// as recommended by Avalonia documentation.
    /// </summary>
    /// <param name="points">A span of points, in control coordinates, that define the polyline of the edge. The span may be modified in place (e.g. reversed).</param>
    /// <param name="reverse">If <c>true</c>, reverses the order of <paramref name="points"/> before building the geometry.</param>
    /// <returns>A <see cref="StreamGeometry"/> representing a polyline passing through the specified <paramref name="points"/> in the control's local space.</returns>
    private StreamGeometry BuildNormalizedStreamGeometry(Span<Point> points, bool reverse)
    {
        // Handle edge case gracefully: if fewer than 2 points, return an empty geometry
        // This can occur temporarily during rapid vertex dragging or when edge endpoints
        // are removed due to arrow pointer adjustments on very short edges.
        if (points.Length < 2)
            return new StreamGeometry();

        // Calculate padding needed for edge pointers to prevent clipping
        var pointerPadding = GetEdgePointerPadding();

        // Collect bounds
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        foreach (var point in points)
        {
            if (point.X < minX) minX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.X > maxX) maxX = point.X;
            if (point.Y > maxY) maxY = point.Y;
        }

        // Expand bounds to include edge pointer padding
        minX -= pointerPadding;
        minY -= pointerPadding;
        maxX += pointerPadding;
        maxY += pointerPadding;

        // Reposition the EdgeControl itself
        SetPosition(minX, minY);

        // Store offset for edge pointer positioning (set before edge pointer updates
        // will be pre-computed, but this ensures consistency)
        _localOffset = new Point(minX, minY);

        // Shift points into local space
        for (var i = 0; i < points.Length; i++)
            points[i] = new Point(points[i].X - minX, points[i].Y - minY);

        Width = Math.Max(1, maxX - minX);
        Height = Math.Max(1, maxY - minY);

        // Build StreamGeometry - more performant than PathGeometry
        if (reverse) points.Reverse();

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(points[0], false);
        for (var i = 1; i < points.Length; i++)
            ctx.LineTo(points[i]);
        ctx.EndFigure(false);

        return geometry;
    }

    /// <summary>
    /// Process self looped edge positioning
    /// </summary>
    /// <param name="sourcePos">Left-top vertex position</param>
    protected virtual EllipseGeometry? PrepareSelfLoopedEdge(Point sourcePos)
    {
        if (!ShowSelfLoopIndicator)
            return null;

        var hasNoTemplate = !HasSelfLoopedEdgeTemplate;
        var pt = new Point(
            sourcePos.X + SelfLoopIndicatorOffset.X -
            (hasNoTemplate ? SelfLoopIndicatorRadius : SelfLoopIndicator!.DesiredSize.Width),
            sourcePos.Y + SelfLoopIndicatorOffset.Y -
            (hasNoTemplate ? SelfLoopIndicatorRadius : SelfLoopIndicator!.DesiredSize.Height));

        //if we has no self looped edge template defined we'll use default built-in indicator
        if (hasNoTemplate)
        {
            var ellipse = new EllipseGeometry
            {
                // Position ellipse so that (pt) represents its top-left corner
                Center = new Point(pt.X + SelfLoopIndicatorRadius, pt.Y + SelfLoopIndicatorRadius),
                RadiusX = SelfLoopIndicatorRadius,
                RadiusY = SelfLoopIndicatorRadius
            };
            return ellipse;
        }

        _selfLoopedEdgeLastKnownRect = new Rect(pt, SelfLoopIndicator!.DesiredSize);
        return null;
    }


    /// <summary>
    /// Create and apply edge path using calculated ER parameters stored in edge
    /// </summary>
    /// <param name="useCurrentCoords">Use current vertices coordinates or final coorfinates (for.ex if move animation is active final coords will be its destination)</param>
    /// <param name="externalRoutingPoints">Provided custom routing points will be used instead of stored ones.</param>
    private Geometry? PrepareEdgePath(bool useCurrentCoords = false,
        Measure.Point[]? externalRoutingPoints = null)
    {
        if (!TryGetSourcePoints(useCurrentCoords, out var sourceRect))
            return null;
        if (!TryGetTargetPoints(useCurrentCoords, out var targetRect))
            return null;

        if (Edge is not IRoutingInfo routedEdge)
            throw new GX_InvalidDataException("Edge must implement IRoutingInfo interface");

        //get the route informations
        var routeInformation = externalRoutingPoints ?? routedEdge.RoutingPoints;


        //if self looped edge
        UpdateSelfLoopedEdgeData();
        if (IsSelfLooped)
        {
            return PrepareSelfLoopedEdge(sourceRect.TopLeft);
        }

        //check if we have some edge route data
        var hasRouteInfo = routeInformation is { Length: > 1 };
        var gEdge = Edge as IGraphXCommonEdge;
        UpdateConnectionPoints(gEdge, hasRouteInfo, routeInformation, sourceRect, targetRect);

        // If the logic above is working correctly, both the source and target connection points will exist.
        if (!SourceConnectionPoint.HasValue || !TargetConnectionPoint.HasValue)
            throw new GX_GeneralException("One or both connection points was not found due to an internal error.");

        var p1 = SourceConnectionPoint.Value;
        var p2 = TargetConnectionPoint.Value;
        // Use StreamGeometry for better performance (as recommended by Avalonia docs)
        return CreateStreamGeometry(externalRoutingPoints, hasRouteInfo, routeInformation, p1, p2, routedEdge,
            gEdge);
    }

    /// <summary>
    /// Creates a <see cref="StreamGeometry"/> for the edge path as an optimized alternative to
    /// <c>CreateFigure</c>. Using <see cref="StreamGeometry"/> is more performant than
    /// <see cref="PathGeometry"/> for dynamically generated edge visuals.
    /// </summary>
    /// <param name="externalRoutingPoints">Optional external routing points provided by the routing algorithm, if any.</param>
    /// <param name="hasRouteInfo">Indicates whether valid route information is available in <paramref name="routeInformation"/>.</param>
    /// <param name="routeInformation">The route points describing the edge path when routing is enabled.</param>
    /// <param name="p1">The initial source connection point of the edge before pointer adjustments.</param>
    /// <param name="p2">The initial target connection point of the edge before pointer adjustments.</param>
    /// <param name="routedEdge">The routing metadata associated with the edge.</param>
    /// <param name="gEdge">The graph edge instance used to determine additional edge settings such as path reversal.</param>
    /// <returns>
    /// A normalized <see cref="StreamGeometry"/> instance representing the final edge path, ready to be
    /// rendered by the control.
    /// </returns>
    private StreamGeometry CreateStreamGeometry(Measure.Point[]? externalRoutingPoints, bool hasRouteInfo,
        Measure.Point[]? routeInformation, Point p1,
        Point p2, IRoutingInfo routedEdge, IGraphXCommonEdge? gEdge)
    {
        var hasEpSource = EdgePointerForSource != null;
        var hasEpTarget = EdgePointerForTarget != null;

        //if we have route and route consist of 2 or more points
        if (RootArea != null && hasRouteInfo && routeInformation != null)
        {
            //replace start and end points with accurate ones
            const int stackallocThreshold = 256;
            int pointsLength = routeInformation.Length < 2 ? 2 : routeInformation.Length;
            Span<Point> routePoints = pointsLength <= stackallocThreshold
                ? stackalloc Point[pointsLength]
                : new Point[pointsLength];
            for (var i = 0; i < routeInformation.Length; i++)
            {
                routePoints[i] = routeInformation[i].ToAvalonia();
            }

            routePoints[0] = p1;
            routePoints[^1] = p2;
            if (externalRoutingPoints == null && routedEdge.RoutingPoints != null)
                routedEdge.RoutingPoints = routePoints.ToArray().ToGraphX();

            if (!RootArea.EdgeCurvingEnabled)
            {
                // Store edge pointer data
                Point sourceOffset = default, targetOffset = default;
                if (hasEpSource)
                    sourceOffset = StoreSourcePointerData(routePoints[0], routePoints[1]);
                if (hasEpTarget)
                    targetOffset = StoreTargetPointerData(p2, routePoints[^2]);

                // Adjust endpoints
                if (hasEpSource)
                    routePoints[0] = routePoints[0].Subtract(sourceOffset);
                if (hasEpTarget)
                    routePoints[^1] = routePoints[^1].Subtract(targetOffset);

                var result = BuildNormalizedStreamGeometry(routePoints, gEdge?.ReversePath ?? false);
                ApplyPendingEdgePointers();
                return result;
            }

            var oPolyLineSegment =
                GeometryHelper.GetCurveThroughPoints(routePoints, 0.5, RootArea.EdgeCurvingTolerance);

            // Store edge pointer data
            if (hasEpTarget)
                StoreTargetPointerData(oPolyLineSegment[^1], oPolyLineSegment[^2]);
            if (hasEpSource)
                StoreSourcePointerData(oPolyLineSegment[0], oPolyLineSegment[1]);

            var curvedResult = BuildNormalizedStreamGeometry(CollectionsMarshal.AsSpan(oPolyLineSegment),
                gEdge?.ReversePath ?? false);
            ApplyPendingEdgePointers();
            return curvedResult;
        }

        // no route defined
        var allowUpdateEpDataToUnsuppress = true;
        //check for hide only if prop is not 0
        if (HideEdgePointerByEdgeLength != 0d)
        {
            if (MathHelper.GetDistanceBetweenPoints(p1.ToGraphX(), p2.ToGraphX()) <=
                HideEdgePointerByEdgeLength)
            {
                EdgePointerForSource?.Suppress();
                EdgePointerForTarget?.Suppress();
                allowUpdateEpDataToUnsuppress = false;
            }
            else
            {
                EdgePointerForSource?.UnSuppress();
                EdgePointerForTarget?.UnSuppress();
            }
        }

        // Store edge pointer data
        Point srcOffset = default, tgtOffset = default;
        if (hasEpSource)
            srcOffset = StoreSourcePointerData(p1, p2, allowUpdateEpDataToUnsuppress);
        if (hasEpTarget)
            tgtOffset = StoreTargetPointerData(p2, p1, allowUpdateEpDataToUnsuppress);

        // Adjust endpoints
        p1 = p1.Subtract(srcOffset);
        p2 = p2.Subtract(tgtOffset);

        var unroutedResult = BuildNormalizedStreamGeometry(TransformFinalPath([p1, p2]), gEdge?.ReversePath ?? false);
        ApplyPendingEdgePointers();
        return unroutedResult;
    }

    [MemberNotNullWhen(true, nameof(Source))]
    private bool TryGetSourcePoints(bool useCurrentCoords, out Rect result) =>
        TryGetPoints(useCurrentCoords, Source, out result);

    [MemberNotNullWhen(true, nameof(Target))]
    private bool TryGetTargetPoints(bool useCurrentCoords, out Rect result) =>
        TryGetPoints(useCurrentCoords, Target, out result);

    private static bool TryGetPoints(bool useCurrentCoords, VertexControl? control, out Rect result)
    {
        result = default;
        if (control is null) return false;
        var x = useCurrentCoords ? GraphAreaBase.GetX(control) : GraphAreaBase.GetFinalX(control);
        var y = useCurrentCoords ? GraphAreaBase.GetY(control) : GraphAreaBase.GetFinalY(control);
        if (double.IsNaN(x)) x = GraphAreaBase.GetX(control);
        if (double.IsNaN(y)) y = GraphAreaBase.GetY(control);
        if (double.IsNaN(x) ||
            double.IsNaN(y)) return false;
        var size = Design.IsDesignMode
            ? new Size(80, 20)
            : new Size(control.Bounds.Width, control.Bounds.Height);
        result = new Rect(new Point(x, y), size);
        return true;
    }


    private void UpdateConnectionPoints(IGraphXCommonEdge? gEdge, bool hasRouteInfo,
        Measure.Point[] routeInformation, Rect sourceRect, Rect targetRect)
    {
        if (Target is null) return;
        if (Source is null) return;
        var sourceCenter = sourceRect.Center;
        var targetCenter = targetRect.Center;


        //calculate edge source (p1) and target (p2) endpoints based on different settings
        switch (gEdge)
        {
            case { SourceConnectionPointId: { } sourceId, TargetConnectionPointId: { } targetId }:
            {
                // Get the connection points and their centers
                var sourceCp = GetSourceCpOrThrow(sourceId);
                var targetCp = GetTargetCpOrThrow(targetId);
                var sourceCpCenter = sourceCp.RectangularSize.Center();
                var targetCpCenter = targetCp.RectangularSize.Center();

                SourceConnectionPoint = GetCpEndPoint(sourceCp, sourceCpCenter, targetCpCenter);
                TargetConnectionPoint = GetCpEndPoint(targetCp, targetCpCenter, sourceCpCenter);
                break;
            }
            case { SourceConnectionPointId: { } id }:
            {
                var sourceCp = GetSourceCpOrThrow(id);
                var sourceCpCenter = sourceCp.RectangularSize.Center();

                // In the case of parallel edges, the target direction needs to be found and the correct offset calculated. Otherwise, fall back
                // to route information or simply the center of the target vertex.
                if (NeedParallelCalc(hasRouteInfo))
                {
                    var m = new Point(targetCenter.X - sourceCenter.X, targetCenter.Y - sourceCenter.Y);
                    targetCenter = new Point(sourceCpCenter.X + m.X, sourceCpCenter.Y + m.Y);
                }
                else if (hasRouteInfo)
                {
                    targetCenter = routeInformation[1].ToAvalonia();
                }

                SourceConnectionPoint = GetCpEndPoint(sourceCp, sourceCpCenter, targetCenter);
                TargetConnectionPoint = GeometryHelper.GetEdgeEndpoint(targetCenter, targetRect,
                    hasRouteInfo ? routeInformation[^2].ToAvalonia() : sourceCpCenter,
                    Target.VertexShape);
                break;
            }
            case { TargetConnectionPointId: { } id }:
            {
                var targetCp = GetTargetCpOrThrow(id);
                var targetCpCenter = targetCp.RectangularSize.Center();

                // In the case of parallel edges, the source direction needs to be found and the correct offset calculated. Otherwise, fall back
                // to route information or simply the center of the source vertex.
                if (NeedParallelCalc(hasRouteInfo))
                {
                    var m = new Point(sourceCenter.X - targetCenter.X, sourceCenter.Y - targetCenter.Y);
                    sourceCenter = new Point(targetCpCenter.X + m.X, targetCpCenter.Y + m.Y);
                }
                else if (hasRouteInfo)
                {
                    sourceCenter = routeInformation[^2].ToAvalonia();
                }

                SourceConnectionPoint = GeometryHelper.GetEdgeEndpoint(sourceCenter, sourceRect,
                    hasRouteInfo ? routeInformation[1].ToAvalonia() : targetCpCenter, Source!.VertexShape);
                TargetConnectionPoint = GetCpEndPoint(targetCp, targetCpCenter, sourceCenter);
                break;
            }
            default:
            {
                //calculate source and target edge attach points
                if (NeedParallelCalc(hasRouteInfo))
                {
                    var origSc = sourceCenter;
                    var origTc = targetCenter;
                    sourceCenter = GetParallelOffset(origSc, origTc, ParallelEdgeOffset);
                    targetCenter = GetParallelOffset(origTc, origSc, -ParallelEdgeOffset);
                }

                SourceConnectionPoint = GeometryHelper.GetEdgeEndpoint(sourceCenter,
                    sourceRect,
                    hasRouteInfo ? routeInformation[1].ToAvalonia() : targetCenter, Source!.VertexShape);
                TargetConnectionPoint = GeometryHelper.GetEdgeEndpoint(targetCenter,
                    targetRect,
                    hasRouteInfo ? routeInformation[^2].ToAvalonia() : sourceCenter,
                    Target.VertexShape);
                break;
            }
        }
    }

    private bool NeedParallelCalc(bool hasRouteInfo)
    {
        if (RootArea is null) return false;
        return !hasRouteInfo && RootArea.EnableParallelEdges && IsParallel;
    }

    private static Point GetCpEndPoint(IVertexConnectionPoint cp, Point cpCenter, Point distantEnd)
    {
        // If the connection point (cp) doesn't have any shape, the edge comes from its center, otherwise find the location
        // on its perimeter that the edge should come from.
        var calculatedCp = cp.Shape == VertexShape.None
            ? cpCenter
            : GeometryHelper.GetEdgeEndpoint(cpCenter, cp.RectangularSize, distantEnd, cp.Shape);
        return calculatedCp;
    }

    private IVertexConnectionPoint GetTargetCpOrThrow(int id)
    {
        return Target?.GetConnectionPointById(id, true) ?? throw new GX_ObjectNotFoundException(string.Format(
            "Can't find target vertex VCP by edge target connection point Id({1}) : {0}", Target, id));
    }

    private IVertexConnectionPoint GetSourceCpOrThrow(int id)
    {
        return Source!.GetConnectionPointById(id, true) ?? throw new GX_ObjectNotFoundException(string.Format(
            "Can't find source vertex VCP by edge source connection point Id({1}) : {0}", Source, id));
    }

    protected virtual Span<Point> TransformFinalPath(Span<Point> original)
    {
        return original;
    }

    /// <summary>
    /// Searches and returns template part by name if found
    /// </summary>
    /// <param name="args">The event args for the template application</param>
    /// <param name="name">Template PART name</param>
    /// <returns></returns>
    protected virtual object? GetTemplatePart(TemplateAppliedEventArgs args, string name)
    {
        return args.NameScope.Find(name);
    }

    public virtual IList<Rect> GetLabelSizes()
    {
        return [.. EdgeLabelControls.Select(l => l.GetSize())];
    }

    // Internal test accessor
    internal Geometry? GetLineGeometry() => LineGeometry;

    /// <summary>
    /// Returns all edge controls attached to this entity
    /// </summary>
    public IList<IEdgeLabelControl> GetLabelControls()
    {
        return [.. EdgeLabelControls];
    }
}