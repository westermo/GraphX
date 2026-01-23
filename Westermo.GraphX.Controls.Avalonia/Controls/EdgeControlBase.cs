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
using Avalonia.Input;
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

    public abstract bool IsSelfLooped { get; protected set; }

    public abstract void Dispose();

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
        SourceProperty.Changed.AddClassHandler<EdgeControlBase>(OnSourceChangedInternal);
        TargetProperty.Changed.AddClassHandler<EdgeControlBase>(OnTargetChangedInternal);
        DashStyleProperty.Changed.AddClassHandler<EdgeControlBase>(DashStyleChanged);
        ShowArrowsProperty.Changed.AddClassHandler<EdgeControlBase>(ShowArrowsChanged);
        ShowSelfLoopIndicatorProperty.Changed.AddClassHandler<EdgeControlBase>(ShowSelfLoopIndicatorChanged);
        SelfLoopIndicatorOffsetProperty.Changed.AddClassHandler<EdgeControlBase>(SelfLoopDataChanged);
        SelfLoopIndicatorRadiusProperty.Changed.AddClassHandler<EdgeControlBase>(SelfLoopDataChanged);
    }

    private static void SelfLoopDataChanged(EdgeControlBase arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.UpdateEdgeRendering();
    }

    private static void ShowSelfLoopIndicatorChanged(EdgeControlBase arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.UpdateSelfLoopedEdgeData();
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
        edgeControlBase.UpdateEdge(false);
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
        _updateLabelPosition = true;
        Loaded += EdgeControlBase_Loaded;
    }


    private void EdgeControlBase_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= EdgeControlBase_Loaded;
    }

    private bool _updateLabelPosition;

    /// <summary>
    /// Gets or sets if label position should be updated on edge update
    /// </summary>
    public bool UpdateLabelPosition
    {
        get => _updateLabelPosition;
        set => _updateLabelPosition = value;
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
        edgeControlBase.UpdateEdge(false);
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
        UpdateEdge();
    }


    internal void SetVisibility(bool value)
    {
        SetCurrentValue(IsVisibleProperty, value);
    }

    internal virtual void InvalidateChildren()
    {
        EdgeLabelControls.ForEach(l => l.UpdateLayout());
        if (LinePathObject != null)
        {
            var pos = Source!.GetPosition();
            Source.SetPosition(pos.X, pos.Y);
        }
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
            SelfLoopIndicator.LayoutUpdated += (_, _) =>
            {
                SelfLoopIndicator?.Arrange(_selfLoopedEdgeLastKnownRect);
            };
        // var x = ShowLabel;
        MeasureChild(EdgePointerForSource as Control);
        MeasureChild(EdgePointerForTarget as Control);
        MeasureChild(SelfLoopIndicator);
        //TODO measure label?

        UpdateSelfLoopedEdgeData();

        UpdateEdge();
    }

    // Re-added after edit: measure template child once with unlimited size so DesiredSize is initialized
    protected void MeasureChild(Control? child)
    {
        child?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
    }

    // Provide a desired size for layout based on current geometry bounds so edge is not collapsed to 0x0.
    protected override Size MeasureOverride(Size availableSize)
    {
        // If we have geometry use its bounds; otherwise fall back to explicitly set Width/Height (may be NaN).
        if (LineGeometry != null)
        {
            var b = LineGeometry.Bounds;
            var w = double.IsNaN(b.Width) || b.Width <= 0 ? 1 : b.Width;
            var h = double.IsNaN(b.Height) || b.Height <= 0 ? 1 : b.Height;
            // Respect MinWidth/MinHeight from styles.
            w = Math.Max(w, MinWidth);
            h = Math.Max(h, MinHeight);
            return new Size(w, h);
        }

        var fw = double.IsNaN(Width) ? 0 : Width;
        var fh = double.IsNaN(Height) ? 0 : Height;
        fw = Math.Max(fw, MinWidth);
        fh = Math.Max(fh, MinHeight);
        if (fw <= 0) fw = 1; // never let it be 0 so Path can render
        if (fh <= 0) fh = 1;
        return new Size(fw, fh);
    }

        /// <summary>
        /// Complete edge update pass. Don't needed to be run manualy until some edge related modifications are done requiring full edge update.
        /// </summary>
        /// <param name="updateLabel">Update label data</param>
        public virtual void UpdateEdge(bool updateLabel = true)
        {
            if (!IsVisible && !IsHiddenEdgesUpdated) return;
            //first show label to get DesiredSize - optimized: avoid LINQ allocation
            foreach (var l in EdgeLabelControls)
            {
                if (l.ShowLabel) l.Show();
                else l.Hide();
            }
            UpdateEdgeRendering(updateLabel);
        }

    /// <summary>
    /// Internal. Update only edge points andge edge line visual
    /// </summary>
    /// <param name="updateLabel"></param>
    internal virtual void UpdateEdgeRendering(bool updateLabel = true)
    {
        if (!IsTemplateLoaded)
            ApplyTemplate();
        if (ShowArrows)
        {
            // Note: Do not override a possible WPF Binding or Converter for the Visibility property.
            if (EdgePointerForSource?.IsVisible == true)
                EdgePointerForSource?.Show();

            // Note: Do not override a possible WPF Binding or Converter for the Visibility property.
            if (EdgePointerForTarget?.IsVisible == true)
                EdgePointerForTarget?.Show();
        }
        else
        {
            EdgePointerForSource?.Hide();
            EdgePointerForTarget?.Hide();
        }

        //use final vertex coordinates (layout results) instead of current to avoid drawing collapsed edges before animation/position commit
        PrepareEdgePath(false, null, updateLabel);
        if (LinePathObject == null) return;
        LinePathObject.Data = LineGeometry;
        LinePathObject.StrokeDashArray = StrokeDashArray;
        InvalidateMeasure(); // ensure layout re-queries our desired size after geometry change
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
                else SelfLoopIndicator!.SetCurrentValue(IsVisibleProperty, true);
            }
            else
            {
                if (HasSelfLoopedEdgeTemplate)
                    SelfLoopIndicator!.SetCurrentValue(IsVisibleProperty, false);
            }
        }

        /// <summary>
        /// Builds a StreamGeometry from normalized points. StreamGeometry is more performant than PathGeometry
        /// as recommended by Avalonia documentation.
        /// </summary>
        private StreamGeometry BuildNormalizedStreamGeometry(Span<Point> points, bool reverse)
        {
            if (points.Length < 2)
                throw new GX_InvalidDataException("At least two points required to build edge path!");

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

            // Reposition the EdgeControl itself
            SetPosition(minX, minY);

            // Shift points into local space
            for (var i = 0; i < points.Length; i++)
                points[i] = new Point(points[i].X - minX, points[i].Y - minY);

            Width = Math.Max(1, maxX - minX);
            Height = Math.Max(1, maxY - minY);

            // Build StreamGeometry - more performant than PathGeometry
            if (reverse) points.Reverse();

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false);
                for (var i = 1; i < points.Length; i++)
                    ctx.LineTo(points[i]);
                ctx.EndFigure(false);
            }

            return geometry;
        }

    private PathFigure BuildNormalizedLineFigure(Span<Point> points, bool reverse)
    {
        if (points.Length < 2)
            throw new GX_InvalidDataException("At least two points required to build edge path!");

        // Collect all points we will actually use
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

        // Reposition the EdgeControl itself
        SetPosition(minX, minY);

        // Shift points into local space
        for (var i = 0; i < points.Length; i++)
            points[i] = new Point(points[i].X - minX, points[i].Y - minY);

        Width = Math.Max(1, maxX - minX);
        Height = Math.Max(1, maxY - minY);

            // Build figure
            if (reverse) points.Reverse();
            var start = points[0];

            // Create new PolyLineSegment with points
            // Note: We create new instances because Avalonia collections don't support Clear()
            // when change tracking is enabled
            var segPoints = new Points();
            for (var i = 1; i < points.Length; i++)
                segPoints.Add(points[i]);

        return new PathFigure
        {
            StartPoint = start,
            Segments = [new PolyLineSegment { Points = segPoints }],
            IsClosed = false
        };
    }


    /// <summary>
    /// Process self looped edge positioning
    /// </summary>
    /// <param name="sourcePos">Left-top vertex position</param>
    protected virtual void PrepareSelfLoopedEdge(Point sourcePos)
    {
        if (!ShowSelfLoopIndicator)
            return;

        var hasNoTemplate = !HasSelfLoopedEdgeTemplate;
        var pt =
            new Point(
                sourcePos.X + SelfLoopIndicatorOffset.X -
                (hasNoTemplate ? SelfLoopIndicatorRadius : SelfLoopIndicator!.DesiredSize.Width),
                sourcePos.Y + SelfLoopIndicatorOffset.Y -
                (hasNoTemplate ? SelfLoopIndicatorRadius : SelfLoopIndicator!.DesiredSize.Height));

        //if we has no self looped edge template defined we'll use default built-in indicator
        if (hasNoTemplate)
        {
            // Generate default ellipse geometry centered relative to calculated top-left point
            if (LineGeometry is not EllipseGeometry ellipse)
            {
                ellipse = new EllipseGeometry();
                LineGeometry = ellipse;
            }

            // Position ellipse so that (pt) represents its top-left corner
            ellipse.Center = new Point(pt.X + SelfLoopIndicatorRadius, pt.Y + SelfLoopIndicatorRadius);
            ellipse.RadiusX = SelfLoopIndicatorRadius;
            ellipse.RadiusY = SelfLoopIndicatorRadius;
        }
        else _selfLoopedEdgeLastKnownRect = new Rect(pt, SelfLoopIndicator!.DesiredSize);
    }

    public virtual void PrepareEdgePathFromMousePointer(PointerEventArgs pointerEventArgs,
        bool useCurrentCoords = false)
    {
        if (RootArea is null) return;
        if (!TryGetSourcePoints(useCurrentCoords, out var sourceRect)) return;


        //get the size of the source
        var sourceSize = sourceRect.Size;

        //get the position center of the source
        var sourcePos = sourceRect.Center;

        //get the size of the target
        var targetSize = new Size(1, 1);

        //get the position center of the target
        var targetPos = pointerEventArgs.GetPosition(RootArea);

        if (Edge is not IRoutingInfo routedEdge)
            throw new GX_InvalidDataException("Edge must implement IRoutingInfo interface");

        //get the route informations
        var routeInformation = routedEdge.RoutingPoints;

        // Get the TopLeft position of the Source Vertex.
        var sourcePos1 = new Point(useCurrentCoords ? GraphAreaBase.GetX(Source) : GraphAreaBase.GetFinalX(Source),
            useCurrentCoords ? GraphAreaBase.GetY(Source) : GraphAreaBase.GetFinalY(Source));
        // Get the TopLeft position of the Target Vertex.

        var hasEpSource = EdgePointerForSource != null;
        var hasEpTarget = EdgePointerForTarget != null;

        //if self looped edge
        if (IsSelfLooped)
        {
            PrepareSelfLoopedEdge(sourcePos1);
            return;
        }

        //check if we have some edge route data
        var hasRouteInfo = routeInformation is { Length: > 1 };

        var gEdge = Edge as IGraphXCommonEdge;
        Point p1;

        //calculate edge source (p1) and target (p2) endpoints based on different settings
        if (gEdge?.SourceConnectionPointId != null)
        {
            var sourceCp = Source.GetConnectionPointById(gEdge.SourceConnectionPointId.Value, true) ??
                           throw new GX_ObjectNotFoundException(string.Format(
                               "Can't find source vertex VCP by edge source connection point Id({1}) : {0}", Source,
                               gEdge.SourceConnectionPointId));
            if (sourceCp.Shape == VertexShape.None) p1 = sourceCp.RectangularSize.Center();
            else
            {
                var targetCpPos = hasRouteInfo ? routeInformation![1].ToAvalonia() : targetPos;
                p1 = GeometryHelper.GetEdgeEndpoint(sourceCp.RectangularSize.Center(), sourceCp.RectangularSize,
                    targetCpPos, sourceCp.Shape);
            }
        }
        else
            p1 = GeometryHelper.GetEdgeEndpoint(sourcePos, new Rect(sourcePos1, sourceSize),
                hasRouteInfo ? routeInformation![1].ToAvalonia() : targetPos, Source.VertexShape);

        var p2 = GeometryHelper.GetEdgeEndpoint(
            targetPos, new Rect(targetPos, targetSize),
            hasRouteInfo ? routeInformation![^2].ToAvalonia() : sourcePos,
            VertexShape.None);

        SourceConnectionPoint = p1;
        TargetConnectionPoint = p2;

        LineGeometry = new PathGeometry();
        PathFigure lineFigure;

        //if we have route and route consist of 2 or more points
        if (hasRouteInfo)
        {
            //replace start and end points with accurate ones
            Span<Point> routePoints = [p1, p2];

            if (routedEdge.RoutingPoints != null)
                routedEdge.RoutingPoints = routePoints.ToArray().ToGraphX();

            if (RootArea.EdgeCurvingEnabled)
            {
                var oPolyLineSegment =
                    GeometryHelper.GetCurveThroughPoints([.. routePoints], 0.5, RootArea.EdgeCurvingTolerance);

                if (hasEpTarget)
                {
                    UpdateTargetEpData(oPolyLineSegment[^1], oPolyLineSegment[^2]);
                    oPolyLineSegment.RemoveAt(oPolyLineSegment.Count - 1);
                }

                if (hasEpSource)
                {
                    UpdateSourceEpData(oPolyLineSegment[0], oPolyLineSegment[1]);
                    oPolyLineSegment.RemoveAt(0);
                }

                lineFigure = BuildNormalizedLineFigure(CollectionsMarshal.AsSpan(oPolyLineSegment),
                    gEdge?.ReversePath ?? false);
            }
            else
            {
                if (hasEpSource)
                    routePoints[0] =
                        routePoints[0].Subtract(UpdateSourceEpData(routePoints[0], routePoints[1]));
                if (hasEpTarget)
                    routePoints[^1] = routePoints[^1]
                        .Subtract(UpdateTargetEpData(p2, routePoints[^2]));


                lineFigure = BuildNormalizedLineFigure(routePoints, gEdge?.ReversePath ?? false);
            }
        }
        else // no route defined
        {
            var remainHidden = false;
            //check for hide only if prop is not 0
            if (HideEdgePointerByEdgeLength != 0d)
            {
                if (MathHelper.GetDistanceBetweenPoints(p1.ToGraphX(), p2.ToGraphX()) <=
                    HideEdgePointerByEdgeLength)
                {
                    EdgePointerForSource?.Hide();
                    EdgePointerForTarget?.Hide();
                    remainHidden = true;
                }
                else
                {
                    EdgePointerForSource?.Show();
                    EdgePointerForTarget?.Show();
                }
            }

            if (hasEpSource)
                p1 = p1.Subtract(UpdateSourceEpData(p1, p2, remainHidden));
            if (hasEpTarget)
                p2 = p2.Subtract(UpdateTargetEpData(p2, p1, remainHidden));

            lineFigure = BuildNormalizedLineFigure(TransformFinalPath([p1, p2]), gEdge?.ReversePath ?? false);
        }

            ((PathGeometry)LineGeometry).Figures!.Add(lineFigure);
            if (_updateLabelPosition)
            {
                // Optimized: avoid LINQ allocation by using foreach
                foreach (var l in EdgeLabelControls)
                {
                    if (l.ShowLabel)
                        l.UpdatePosition();
                }
            }

        if (ShowArrows)
        {
            EdgePointerForSource?.Show();
            EdgePointerForTarget?.Show();
        }
        else
        {
            EdgePointerForSource?.Hide();
            EdgePointerForTarget?.Hide();
        }

        if (LinePathObject == null) return;
        LinePathObject.Data = LineGeometry;
        LinePathObject.StrokeDashArray = StrokeDashArray;
        InvalidateMeasure();
    }


    /// <summary>
    /// Create and apply edge path using calculated ER parameters stored in edge
    /// </summary>
    /// <param name="useCurrentCoords">Use current vertices coordinates or final coorfinates (for.ex if move animation is active final coords will be its destination)</param>
    /// <param name="externalRoutingPoints">Provided custom routing points will be used instead of stored ones.</param>
    /// <param name="updateLabel">Should edge label be updated in this pass</param>
    public virtual void PrepareEdgePath(bool useCurrentCoords = false,
        Measure.Point[]? externalRoutingPoints = null, bool updateLabel = true)
    {
        if (!TryGetSourcePoints(useCurrentCoords, out var sourceRect))
            return;
        if (!TryGetTargetPoints(useCurrentCoords, out var targetRect))
            return;

        if (Edge is not IRoutingInfo routedEdge)
            throw new GX_InvalidDataException("Edge must implement IRoutingInfo interface");

        //get the route informations
        var routeInformation = externalRoutingPoints ?? routedEdge.RoutingPoints;


        //if self looped edge
        if (IsSelfLooped)
        {
            PrepareSelfLoopedEdge(sourceRect.TopLeft);
            return;
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
            LineGeometry = CreateStreamGeometry(externalRoutingPoints, hasRouteInfo, routeInformation, p1, p2, routedEdge,
                gEdge);

            if (_updateLabelPosition && updateLabel)
            {
                // Optimized: avoid LINQ allocation by using foreach
                foreach (var l in EdgeLabelControls)
                {
                    if (l.ShowLabel)
                        l.UpdatePosition();
                }
            }
            InvalidateMeasure();
        }

        /// <summary>
        /// Creates a StreamGeometry for the edge path. StreamGeometry is more performant than PathGeometry.
        /// </summary>
        private Geometry CreateStreamGeometry(Measure.Point[]? externalRoutingPoints, bool hasRouteInfo,
            Measure.Point[]? routeInformation, Point p1,
            Point p2, IRoutingInfo routedEdge, IGraphXCommonEdge? gEdge)
        {
            var hasEpSource = EdgePointerForSource != null;
            var hasEpTarget = EdgePointerForTarget != null;

            //if we have route and route consist of 2 or more points
            if (RootArea != null && hasRouteInfo && routeInformation != null)
            {
                //replace start and end points with accurate ones
                Span<Point> routePoints = stackalloc Point[routeInformation.Length < 2 ? 2 : routeInformation.Length];
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
                    routePoints[0] = hasEpSource switch
                    {
                        true => routePoints[0].Subtract(UpdateSourceEpData(routePoints[0], routePoints[1])),
                        _ => routePoints[0]
                    };
                    routePoints[^1] = hasEpTarget switch
                    {
                        true => routePoints[^1].Subtract(UpdateTargetEpData(p2, routePoints[^2])),
                        _ => routePoints[^1]
                    };

                    return BuildNormalizedStreamGeometry(routePoints, gEdge?.ReversePath ?? false);
                }

                var oPolyLineSegment =
                    GeometryHelper.GetCurveThroughPoints(routePoints, 0.5, RootArea.EdgeCurvingTolerance);

                if (hasEpTarget)
                {
                    UpdateTargetEpData(oPolyLineSegment[^1], oPolyLineSegment[^2]);
                    oPolyLineSegment.RemoveAt(oPolyLineSegment.Count - 1);
                }

                if (hasEpSource)
                {
                    UpdateSourceEpData(oPolyLineSegment[0], oPolyLineSegment[1]);
                    oPolyLineSegment.RemoveAt(0);
                }

                return BuildNormalizedStreamGeometry(CollectionsMarshal.AsSpan(oPolyLineSegment),
                    gEdge?.ReversePath ?? false);
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

            if (hasEpSource)
                p1 = p1.Subtract(UpdateSourceEpData(p1, p2, allowUpdateEpDataToUnsuppress));
            if (hasEpTarget)
                p2 = p2.Subtract(UpdateTargetEpData(p2, p1, allowUpdateEpDataToUnsuppress));

            return BuildNormalizedStreamGeometry(TransformFinalPath([p1, p2]), gEdge?.ReversePath ?? false);
        }

        private PathFigure CreateFigure(Measure.Point[]? externalRoutingPoints, bool hasRouteInfo,
            Measure.Point[]? routeInformation, Point p1,
            Point p2, IRoutingInfo routedEdge, IGraphXCommonEdge? gEdge)
        {
            var hasEpSource = EdgePointerForSource != null;
            var hasEpTarget = EdgePointerForTarget != null;
            //if we have route and route consist of 2 or more points
            if (RootArea != null && hasRouteInfo && routeInformation != null)
            {
                //replace start and end points with accurate ones
                Span<Point> routePoints = stackalloc Point[routeInformation.Length < 2 ? 2 : routeInformation.Length];
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
                routePoints[0] = hasEpSource switch
                {
                    true => routePoints[0].Subtract(UpdateSourceEpData(routePoints[0], routePoints[1])),
                    _ => routePoints[0]
                };
                routePoints[^1] = hasEpTarget switch
                {
                    true => routePoints[^1].Subtract(UpdateTargetEpData(p2, routePoints[^2])),
                    _ => routePoints[^1]
                };

                return BuildNormalizedLineFigure(routePoints, gEdge?.ReversePath ?? false);
            }

            var oPolyLineSegment =
                GeometryHelper.GetCurveThroughPoints(routePoints, 0.5, RootArea.EdgeCurvingTolerance);

            if (hasEpTarget)
            {
                UpdateTargetEpData(oPolyLineSegment[^1],
                    oPolyLineSegment[^2]);
                oPolyLineSegment.RemoveAt(oPolyLineSegment.Count - 1);
            }

            if (hasEpSource)
            {
                UpdateSourceEpData(oPolyLineSegment[0], oPolyLineSegment[1]);
                oPolyLineSegment.RemoveAt(0);
            }

            return BuildNormalizedLineFigure(CollectionsMarshal.AsSpan(oPolyLineSegment),
                gEdge?.ReversePath ?? false);
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

        if (hasEpSource)
            p1 = p1.Subtract(UpdateSourceEpData(p1, p2, allowUpdateEpDataToUnsuppress));
        if (hasEpTarget)
            p2 = p2.Subtract(UpdateTargetEpData(p2, p1, allowUpdateEpDataToUnsuppress));

        return BuildNormalizedLineFigure(TransformFinalPath([p1, p2]), gEdge?.ReversePath ?? false);
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
                    var origSC = sourceCenter;
                    var origTC = targetCenter;
                    sourceCenter = GetParallelOffset(origSC, origTC, ParallelEdgeOffset);
                    targetCenter = GetParallelOffset(origTC, origSC, -ParallelEdgeOffset);
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

    private Point UpdateSourceEpData(Point from, Point to, bool allowUnsuppress = true)
    {
        var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
        if (from == to)
        {
            if (HideEdgePointerOnVertexOverlap) EdgePointerForSource!.Suppress();
            else dir = new Measure.Vector(0, 0);
        }
        else if (allowUnsuppress) EdgePointerForSource!.UnSuppress();

        var result = EdgePointerForSource!.Update(from.ToGraphX(), dir,
            EdgePointerForSource.NeedRotation
                ? -MathHelper.GetAngleBetweenPoints(from.ToGraphX(), to.ToGraphX()).ToDegrees()
                : 0);
        return EdgePointerForSource.IsVisible == IsVisible ? result.ToAvalonia() : new Point();
    }

    private Point UpdateTargetEpData(Point from, Point to, bool allowUnsuppress = true)
    {
        var dir = MathHelper.GetDirection(from.ToGraphX(), to.ToGraphX());
        if (from == to)
        {
            if (HideEdgePointerOnVertexOverlap) EdgePointerForTarget!.Suppress();
            else dir = new Measure.Vector(0, 0);
        }
        else if (allowUnsuppress) EdgePointerForTarget!.UnSuppress();

        var result = EdgePointerForTarget!.Update(from.ToGraphX(), dir,
            EdgePointerForTarget.NeedRotation
                ? -MathHelper.GetAngleBetweenPoints(from.ToGraphX(), to.ToGraphX()).ToDegrees()
                : 0);
        return EdgePointerForTarget.IsVisible == IsVisible ? result.ToAvalonia() : new Point();
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