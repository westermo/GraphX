using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    /// Cached layout information for positioning an edge pointer after geometry normalization.
    /// Set during <see cref="PrepareEdgeLayout"/> and reused on non-dirty arrange passes
    /// so that pointers survive <c>base.ArrangeOverride()</c> resetting child positions.
    /// </summary>
    private struct EdgePointerLayoutInfo
    {
        /// <summary>World-space pointer position (the endpoint before offset adjustment).</summary>
        public Point Position;

        /// <summary>The adjacent point used to compute rotation direction.</summary>
        public Point DirectionTarget;

        /// <summary>Whether suppress/unsuppress logic is permitted for this pointer.</summary>
        public bool AllowUnsuppress;

        /// <summary>Indicates that valid data has been stored.</summary>
        public bool HasData;
    }

    /// <summary>Cached source pointer layout info, set during geometry rebuild.</summary>
    private EdgePointerLayoutInfo _sourcePointerLayout;

    /// <summary>Cached target pointer layout info, set during geometry rebuild.</summary>
    private EdgePointerLayoutInfo _targetPointerLayout;

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
    /// Computes the endpoint offset needed to prevent the edge line from drawing under the pointer.
    /// Also handles suppress/unsuppress based on edge length and overlap settings.
    /// This is a pure computation — it does not store any state.
    /// </summary>
    /// <param name="pointer">The edge pointer to compute offset for, or null if none.</param>
    /// <param name="from">The edge endpoint where the pointer will be placed.</param>
    /// <param name="to">The adjacent point used to determine direction.</param>
    /// <returns>The offset to subtract from the endpoint so the line stops at the pointer boundary.</returns>
    private static Point ComputeEdgePointerOffset(IEdgePointer? pointer, Point from, Point to)
    {
        if (pointer is not Control { IsVisible: true } ctrl)
            return new Point();

        // Compute direction from endpoint towards its neighbor
        var dir = from == to
            ? new Measure.Vector(0, 0)
            : MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());

        var width = ctrl.DesiredSize.Width;
        var height = ctrl.DesiredSize.Height;

        return new Point(dir.X * width, dir.Y * height);
    }

    public Measure.Point SourcePointerPosition { get; private set; }

    public Measure.Point TargetPointerPosition { get; private set; }

    public Measure.Point GetPointerPosition(IEdgePointer pointer)
    {
        return pointer == EdgePointerForSource ? SourcePointerPosition :
            pointer == EdgePointerForTarget ? TargetPointerPosition : new Measure.Point();
    }

    /// <summary>
    /// Positions an edge pointer control in local coordinates after geometry normalization.
    /// Computes direction, rotation angle, and arranges the pointer within the edge's local space.
    /// Always runs after <c>base.ArrangeOverride()</c>, whether geometry was rebuilt or not.
    /// </summary>
    private static Measure.Point ArrangeEdgePointer(EdgePointerLayoutInfo data, IEdgePointer pointer,
        bool hideEdgePointerOnVertexOverlap)
    {
        var from = data.Position;
        var to = data.DirectionTarget;
        var allowUnsuppress = data.AllowUnsuppress;

        var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
        if (from == to)
        {
            if (hideEdgePointerOnVertexOverlap) pointer.Suppress();
            else dir = new Measure.Vector(0, 0);
        }
        else if (allowUnsuppress) pointer.UnSuppress();

        if (pointer is not Control ctrl) return new Measure.Point();
        var size = ctrl.DesiredSize;
        var (width, height) = GetWidthAndHeight(size, ctrl);

        if (width == 0 || height == 0) return new Measure.Point();


        // Convert to local coordinates using the now-known offset
        var position = new Measure.Point(from.X, from.Y);
        var angle = pointer.NeedRotation
            ? -MathHelper.GetAngleBetweenPoints(from.ToGraphX(), to.ToGraphX()).ToDegrees()
            : 0;

        var vecMove = new Measure.Vector((.5 + dir.X * .5) * width, (.5 + dir.Y * .5) * height);
        position = new Measure.Point(position.X - vecMove.X, position.Y - vecMove.Y);
        if (double.IsNaN(width) || width == 0 || double.IsNaN(position.X)) return position;
        var rect =
            new Rect(position.ToAvalonia(), size);
        ctrl.Arrange(rect);
        SetRotation(ctrl, angle);

        return position;
    }

    private static (double, double) GetWidthAndHeight(Size size, Control ctrl)
    {
        var width = size.Width;
        var height = size.Height;
        if (width == 0 || height == 0)
        {
            // Fallback to explicit Width/Height if DesiredSize not available yet
            width = double.IsNaN(ctrl.Width) ? ctrl.Bounds.Width : ctrl.Width;
            height = double.IsNaN(ctrl.Height) ? ctrl.Bounds.Height : ctrl.Height;
        }

        return (width, height);
    }

    private static void SetRotation(Control ctrl, double angle)
    {
        angle = double.IsNaN(angle) ? 0 : angle;
        if (ctrl.RenderTransform is RotateTransform rot)
        {
            rot.Angle = angle;
            return;
        }

        ctrl.RenderTransform = new RotateTransform { Angle = angle, CenterX = 0, CenterY = 0 };
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
        InvalidateMeasure();
    }

    /// <summary>
    /// Internal method. Detaches label from control.
    /// </summary>
    public void DetachLabels(IEdgeLabelControl? ctrl = null)
    {
        EdgeLabelControls.OfType<IAttachableControl<EdgeControl>>()
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
        _edgeLabelControls.Where(l => l.ShowLabel).ForEach(l => { l.Show(); });
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

    private static Size Union(params ReadOnlySpan<Size> sizes)
    {
        var width = 0.0;
        var height = 0.0;
        foreach (var size in sizes)
        {
            width = Math.Max(double.IsNaN(size.Width) ? 0 : size.Width, width);
            height = Math.Max(double.IsNaN(size.Height) ? 0 : size.Height, height);
        }

        return new Size(width, height);
    }

    // Provide a desired size for layout based on current geometry bounds so edge is not collapsed to 0x0.
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!IsTemplateLoaded)
            ApplyTemplate();

        if (!TryGetSourcePoints(false, out var sourceRect))
            return default;
        if (!TryGetTargetPoints(false, out var targetRect))
            return default;


        var selfLoopSize = IsSelfLooped
            ? new Size(SelfLoopIndicatorRadius * 2 + SelfLoopIndicatorOffset.X,
                SelfLoopIndicatorRadius * 2 + SelfLoopIndicatorOffset.Y)
            : new Size();
        var spanningRect = sourceRect.Union(targetRect);
        var infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        if (SelfLoopIndicator is { } selfLoopIndicator) selfLoopIndicator.Measure(infiniteSize);

        if (Edge is not IRoutingInfo routedEdge)
            throw new GX_InvalidDataException("Edge must implement IRoutingInfo interface");

        //get the route informations
        var routeInformation = routedEdge.RoutingPoints;
        var gEdge = Edge as IGraphXCommonEdge;
        UpdateConnectionPoints(gEdge, routeInformation, sourceRect, targetRect);

        // If the logic above is working correctly, both the source and target connection points will exist.
        if (!SourceConnectionPoint.HasValue || !TargetConnectionPoint.HasValue)
            throw new GX_GeneralException("One or both connection points was not found due to an internal error.");

        var p1 = SourceConnectionPoint.Value;
        var p2 = TargetConnectionPoint.Value;
        _points = GetPoints(p1, p2, routedEdge);
        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var minY = double.MaxValue;
        var maxY = double.MinValue;
        foreach (var point in _points)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        var dx = maxX - minX;
        var dy = maxY - minY;
        var routingBounds = new Size(dx, dy);

        // Cache layout info for pointer arrangement after base.ArrangeOverride
        _sourcePointerLayout = new EdgePointerLayoutInfo
        {
            Position = _points[0],
            DirectionTarget = _points[1],
            HasData = EdgePointerForSource != null,
            AllowUnsuppress = true
        };
        _targetPointerLayout = new EdgePointerLayoutInfo
        {
            Position = _points[^1],
            DirectionTarget = _points[^2],
            HasData = EdgePointerForTarget != null,
            AllowUnsuppress = true
        };
        if (EdgePointerForSource is Control pointerForSource)
        {
            if (ShowArrows) EdgePointerForSource.Show();
            pointerForSource.Measure(infiniteSize);
            var sourceOffset = ComputeEdgePointerOffset(EdgePointerForSource, _points[0], _points[1]);
            _points[0] = _points[0].Subtract(sourceOffset);
        }

        if (EdgePointerForTarget is Control pointerForTarget)
        {
            if (ShowArrows) EdgePointerForTarget.Show();
            pointerForTarget.Measure(infiniteSize);
            var targetOffset = ComputeEdgePointerOffset(EdgePointerForTarget, _points[^1], _points[^2]);
            _points[^1] = _points[^1].Subtract(targetOffset);
        }

        //Measure bounds after edge pointers have been measured.
        _pathBounds = GetBounds(_points);

        // Shift points into local space
        for (var i = 0; i < _points.Length; i++)
            _points[i] = _points[i].Subtract(_pathBounds.TopLeft);
        _sourcePointerLayout.Position = _sourcePointerLayout.Position.Subtract(_pathBounds.TopLeft);
        _sourcePointerLayout.DirectionTarget = _sourcePointerLayout.DirectionTarget.Subtract(_pathBounds.TopLeft);
        _targetPointerLayout.Position = _targetPointerLayout.Position.Subtract(_pathBounds.TopLeft);
        _targetPointerLayout.DirectionTarget = _targetPointerLayout.DirectionTarget.Subtract(_pathBounds.TopLeft);

        foreach (var label in EdgeLabelControls)
        {
            if (IsSelfLooped && !label.DisplayForSelfLoopedEdges) continue;
            if (label is not Control { IsVisible: true } ctrl) continue;
            ctrl.Measure(infiniteSize);
            selfLoopSize = Union(selfLoopSize, ctrl.DesiredSize);
        }


        return Union(spanningRect.Size, selfLoopSize, routingBounds);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Reposition the EdgeControl itself
        SetPosition(_pathBounds.X, _pathBounds.Y);
        Width = Math.Max(1, _pathBounds.Width);
        Height = Math.Max(1, _pathBounds.Height);
        LineGeometry = PrepareEdgeLayout();
        if (LinePathObject == null) return base.ArrangeOverride(finalSize);
        LinePathObject.Data = LineGeometry;
        LinePathObject.StrokeDashArray = StrokeDashArray;

        var result = base.ArrangeOverride(finalSize);

        // Position edge pointers
        if (_sourcePointerLayout.HasData && EdgePointerForSource != null)
            SourcePointerPosition =
                ArrangeEdgePointer(_sourcePointerLayout, EdgePointerForSource, HideEdgePointerOnVertexOverlap);
        if (_targetPointerLayout.HasData && EdgePointerForTarget != null)
            TargetPointerPosition =
                ArrangeEdgePointer(_targetPointerLayout, EdgePointerForTarget, HideEdgePointerOnVertexOverlap);

        // Position labels at edge midpoint
        var midPoint = GetMidpoint(out var angle, out var flipAxis, out var vector);
        if (midPoint == default) return result;
        vector.Normalize();
        foreach (var label in EdgeLabelControls)
        {
            if (label is not Control { IsVisible: true } ctrl) continue;
            var labelSize = ctrl.DesiredSize;
            var offsetX = label.LabelHorizontalOffset * vector;
            var offsetY = label.LabelVerticalOffset * vector.Perpendicular();
            if (label.FlipOnRotation && flipAxis && !IsParallel)
            {
                offsetY = -offsetY;
            }

            var localPoint = midPoint;
            localPoint.X += -labelSize.Width / 2 + offsetX.X + offsetY.X + _pathBounds.X;
            localPoint.Y += -labelSize.Height / 2 + offsetX.Y + offsetY.Y + _pathBounds.Y;
            ctrl.SetCurrentValue(GraphAreaBase.XProperty, localPoint.X);
            ctrl.SetCurrentValue(GraphAreaBase.YProperty, localPoint.Y);
            label.Angle = label.AlignToEdge ? -angle.ToDegrees() + (flipAxis ? 180 : 0) : 0;
            ctrl.Arrange(new Rect(localPoint.X , localPoint.Y, labelSize.Width, labelSize.Height));
        }

        return result;
    }

    private Measure.Point GetMidpoint(out double angle, out bool flipAxis, out Measure.Vector vector)
    {
        angle = 0;
        flipAxis = false;
        vector = new Measure.Vector(1, 1);
        if (IsSelfLooped)
        {
            if (Source is null) return default;
            var pt = Source.GetCenterPosition().ToGraphX();
            pt.Offset(SelfLoopIndicatorOffset.X, SelfLoopIndicatorOffset.Y);
            // For self-loop edges, store angle in radians (45 degrees = π/4) to match other branches.
            angle = Math.PI / 4.0;
            return pt;
        }

        if (Source == null || Target == null) return default;
        var p1 = _points[0].ToGraphX();
        var p2 = _points[^1].ToGraphX();

        var edgeLength = TotalLength(_points);
        var remaining = FindHalfwayPoint(edgeLength, _points, ref p1, ref p2);
        // After FindHalfwayPoint, p1 and p2 represent the segment containing the midpoint.
        // Compute flipAxis based on the updated segment endpoints, consistent with the non-routing branch.
        flipAxis = p1.X > p2.X;
        angle = MathHelper.GetAngleBetweenPoints(p1, p2);
        vector = flipAxis ? p1 - p2 : p2 - p1;
        return new Measure.Point(p1.X + remaining * Math.Cos(angle), p1.Y - remaining * Math.Sin(angle));
    }

    private static double FindHalfwayPoint(double edgeLength, Point[] routingPoints, ref Measure.Point p1,
        ref Measure.Point p2)
    {
        // We now want the midpoint along the entire polyline.
        edgeLength /= 2;
        var newp1 = p1;
        var newp2 = p2;
        var previousPoint = p1;
        var remaining = edgeLength;
        var foundSegment = false;

        // Walk again to find the segment that contains the midpoint.
        for (var index = 1; index < routingPoints.Length; index++)
        {
            var currentPoint = routingPoints[index].ToGraphX();
            var lengthOfSegment = MathHelper.GetDistanceBetweenPoints(previousPoint, currentPoint);
            if (lengthOfSegment >= remaining)
            {
                newp1 = previousPoint;
                newp2 = currentPoint;
                foundSegment = true;
                break;
            }

            remaining -= lengthOfSegment;
            previousPoint = currentPoint;
        }

        // If the midpoint lies on the last segment to p2, handle it here.
        if (!foundSegment)
        {
            newp1 = previousPoint;
            newp2 = p2;
            // 'remaining' is already the distance from newp1 along this last segment.
        }

        p1 = newp1;
        p2 = newp2;
        return remaining;
    }

    private static double TotalLength(Point[] points)
    {
        var result = 0.0;
        for (var index = 0; index < points.Length-1; index++)
        {
            var currentPoint = points[index];
            var nextPoint = points[index + 1];
            var lengthOfSegment = MathHelper.GetDistanceBetweenPoints(currentPoint.ToGraphX(), nextPoint.ToGraphX());
            if (double.IsNaN(lengthOfSegment)) continue;
            result += lengthOfSegment;
        }
        return result;
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

    private Point[] _points = [];
    private Rect _pathBounds;

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
    /// <returns>
    /// A tuple containing the <see cref="StreamGeometry"/> representing the edge path and
    /// the local offset used to transform world-space coordinates into control-local space.
    /// </returns>
    private StreamGeometry BuildNormalizedStreamGeometry(Span<Point> points, bool reverse)
    {
        // Handle edge case gracefully: if fewer than 2 points, return an empty geometry
        // This can occur temporarily during rapid vertex dragging or when edge endpoints
        // are removed due to arrow pointer adjustments on very short edges.
        if (points.Length < 2)
            return new StreamGeometry();

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

    private Rect GetBounds(Span<Point> points)
    {
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
        return new Rect(minX, minY, maxX - minX, maxY - minY);
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
        if (!hasNoTemplate) return null;
        var ellipse = new EllipseGeometry
        {
            // Position ellipse so that (pt) represents its top-left corner
            Center = new Point(pt.X + SelfLoopIndicatorRadius, pt.Y + SelfLoopIndicatorRadius),
            RadiusX = SelfLoopIndicatorRadius,
            RadiusY = SelfLoopIndicatorRadius
        };
        return ellipse;
    }


    /// <summary>
    /// Prepares the complete edge layout: computes connection points, edge pointer offsets,
    /// adjusts path endpoints, builds geometry, and stores pointer layout info for later arrangement.
    /// </summary>
    /// <returns>A tuple of the edge geometry and the local offset for world-to-local coordinate conversion.</returns>
    private Geometry? PrepareEdgeLayout()
    {
        if (!TryGetSourcePoints(false, out var sourceRect))
            return null;
        if (Edge is not IRoutingInfo routedEdge)
            throw new GX_InvalidDataException("Edge must implement IRoutingInfo interface");


        //if self looped edge
        UpdateSelfLoopedEdgeData();
        if (IsSelfLooped)
        {
            // Self-looped edges have no edge pointers to position
            _sourcePointerLayout = default;
            _targetPointerLayout = default;
            return PrepareSelfLoopedEdge(sourceRect.TopLeft);
        }

        return CreateEdgeGeometry(_points, routedEdge is IGraphXCommonEdge { ReversePath: true });
    }

    private Point[] GetPoints(Point p1, Point p2, IRoutingInfo routeInformation)
    {
        if (routeInformation is not { RoutingPoints.Length: > 2 }) return [p1, p2];
        //replace start and end points with accurate ones
        const int stackallocThreshold = 256;
        var pointsLength = routeInformation.RoutingPoints.Length;
        var routePoints = pointsLength <= stackallocThreshold
            ? stackalloc Point[pointsLength]
            : new Point[pointsLength];
        for (var i = 0; i < routeInformation.RoutingPoints.Length; i++)
        {
            routePoints[i] = routeInformation.RoutingPoints[i].ToAvalonia();
        }

        routePoints[0] = p1;
        routePoints[^1] = p2;

        if (RootArea is not { IsEdgeRoutingEnabled: true })
        {
            return [..routePoints];
        }

        return [..GeometryHelper.GetCurveThroughPoints(routePoints, 0.5, RootArea.EdgeCurvingTolerance)];
    }

    /// <summary>
    /// Creates a <see cref="StreamGeometry"/> for the edge path as an optimized alternative to
    /// <c>CreateFigure</c>. Using <see cref="StreamGeometry"/> is more performant than
    /// <see cref="PathGeometry"/> for dynamically generated edge visuals.
    /// </summary>
    /// <param name="points">The points to generate for</param>
    /// <param name="reverse">Whether or not to reverse the produced geometry</param>
    /// <returns>
    /// A tuple of the normalized geometry and the local offset for coordinate conversion.
    /// </returns>
    private StreamGeometry CreateEdgeGeometry(Point[] points, bool reverse)
    {
        return BuildNormalizedStreamGeometry(TransformFinalPath(points), reverse);
    }

    [MemberNotNullWhen(true, nameof(Source))]
    private bool TryGetSourcePoints(bool useCurrentCoords, out Rect result)
    {
        return TryGetPoints(useCurrentCoords, Source, out result);
    }

    [MemberNotNullWhen(true, nameof(Target))]
    private bool TryGetTargetPoints(bool useCurrentCoords, out Rect result)
    {
        if (!OverrideEndpoint.HasValue) return TryGetPoints(useCurrentCoords, Target, out result);
        result = new Rect(OverrideEndpoint.Value, new Size(10, 10));
        return Target is not null;
    }

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
        result = new Rect(new Point(x, y), control.DesiredSize);
        return true;
    }


    private void UpdateConnectionPoints(IGraphXCommonEdge? gEdge, Measure.Point[]? routeInformation, Rect sourceRect,
        Rect targetRect)
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
                if (NeedParallelCalc(routeInformation is { Length: > 1 }))
                {
                    var m = new Point(targetCenter.X - sourceCenter.X, targetCenter.Y - sourceCenter.Y);
                    targetCenter = new Point(sourceCpCenter.X + m.X, sourceCpCenter.Y + m.Y);
                }
                else if (routeInformation is { Length: > 1 })
                {
                    targetCenter = routeInformation[1].ToAvalonia();
                }

                SourceConnectionPoint = GetCpEndPoint(sourceCp, sourceCpCenter, targetCenter);
                TargetConnectionPoint = GeometryHelper.GetEdgeEndpoint(targetCenter, targetRect,
                    routeInformation is { Length: > 1 } ? routeInformation[^2].ToAvalonia() : sourceCpCenter,
                    Target.VertexShape);
                break;
            }
            case { TargetConnectionPointId: { } id }:
            {
                var targetCp = GetTargetCpOrThrow(id);
                var targetCpCenter = targetCp.RectangularSize.Center();

                // In the case of parallel edges, the source direction needs to be found and the correct offset calculated. Otherwise, fall back
                // to route information or simply the center of the source vertex.
                if (NeedParallelCalc(routeInformation is { Length: > 1 }))
                {
                    var m = new Point(sourceCenter.X - targetCenter.X, sourceCenter.Y - targetCenter.Y);
                    sourceCenter = new Point(targetCpCenter.X + m.X, targetCpCenter.Y + m.Y);
                }
                else if (routeInformation is { Length: > 1 })
                {
                    sourceCenter = routeInformation[^2].ToAvalonia();
                }

                SourceConnectionPoint = GeometryHelper.GetEdgeEndpoint(sourceCenter, sourceRect,
                    routeInformation is { Length: > 1 } ? routeInformation[1].ToAvalonia() : targetCpCenter,
                    Source!.VertexShape);
                TargetConnectionPoint = GetCpEndPoint(targetCp, targetCpCenter, sourceCenter);
                break;
            }
            default:
            {
                //calculate source and target edge attach points
                if (NeedParallelCalc(routeInformation is { Length: > 1 }))
                {
                    var origSc = sourceCenter;
                    var origTc = targetCenter;
                    sourceCenter = GetParallelOffset(origSc, origTc, ParallelEdgeOffset);
                    targetCenter = GetParallelOffset(origTc, origSc, -ParallelEdgeOffset);
                }

                var scpEnd = routeInformation is { Length: > 1 } ? routeInformation[1].ToAvalonia() : targetCenter;
                var tcpEnd = routeInformation is { Length: > 1 } ? routeInformation[^2].ToAvalonia() : sourceCenter;
                SourceConnectionPoint =
                    GeometryHelper.GetEdgeEndpoint(sourceCenter, sourceRect, scpEnd, Source!.VertexShape);
                TargetConnectionPoint =
                    GeometryHelper.GetEdgeEndpoint(targetCenter, targetRect, tcpEnd, Target.VertexShape);
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
