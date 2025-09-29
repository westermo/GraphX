using System;
using System.Collections.Generic;
using System.Linq;
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
using Westermo.GraphX.Controls.Avalonia.Models;

namespace Westermo.GraphX.Controls.Avalonia
{
    [TemplatePart(Name = "PART_edgePath", Type = typeof(Path))]
    [TemplatePart(Name = "PART_SelfLoopedEdge", Type = typeof(Control))]
    [TemplatePart(Name = "PART_edgeLabel", Type = typeof(IEdgeLabelControl))] //obsolete, present for exception
    [TemplatePart(Name = "PART_EdgePointerForSource", Type = typeof(IEdgePointer))]
    [TemplatePart(Name = "PART_EdgePointerForTarget", Type = typeof(IEdgePointer))]
    public abstract class EdgeControlBase : TemplatedControl, IGraphControl, IDisposable
    {
        #region Properties & Fields

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
        public GraphAreaBase RootArea
        {
            get => GetValue(RootCanvasProperty);
            set => SetValue(RootCanvasProperty, value);
        }

        public static readonly StyledProperty<GraphAreaBase> RootCanvasProperty =
            AvaloniaProperty.Register<EdgeControlBase, GraphAreaBase>(nameof(RootArea));

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
            DashStyleProperty.Changed.AddClassHandler<EdgeControlBase>(dashstyle_changed);
            ShowArrowsProperty.Changed.AddClassHandler<EdgeControlBase>(showarrows_changed);
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


        #region DashStyle

        public static readonly StyledProperty<EdgeDashStyle> DashStyleProperty =
            AvaloniaProperty.Register<EdgeControlBase, EdgeDashStyle>(nameof(DashStyle));

        private static void dashstyle_changed(EdgeControlBase edgeControlBase, AvaloniaPropertyChangedEventArgs e)
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

        #endregion DashStyle

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

        private bool _isInDesignMode;

        private void EdgeControlBase_Loaded(object? sender, RoutedEventArgs e)
        {
            Loaded -= EdgeControlBase_Loaded;
            _isInDesignMode = CustomHelper.IsInDesignMode(this);
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

        private static void showarrows_changed(EdgeControlBase edgeControlBase,
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
                    RootArea.Children.Remove((Control)label);
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

        #endregion Properties & Fields

        #region Position methods

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

        #endregion Position methods

        #region Manual path controls

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

        #endregion Manual path controls

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

        /// <summary>
        /// Measure child objects such as template parts which are not updated automaticaly on first pass.
        /// </summary>
        /// <param name="child">Child Control</param>
        protected void MeasureChild(Control? child)
        {
            child?.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        #region public PrepareEdgePath()

        /// <summary>
        /// Complete edge update pass. Don't needed to be run manualy until some edge related modifications are done requiring full edge update.
        /// </summary>
        /// <param name="updateLabel">Update label data</param>
        public virtual void UpdateEdge(bool updateLabel = true)
        {
            if (!IsVisible && !IsHiddenEdgesUpdated) return;
            //first show label to get DesiredSize
            EdgeLabelControls.ForEach(l =>
            {
                if (l.ShowLabel) l.Show();
                else l.Hide();
            });
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

            PrepareEdgePath(true, null, updateLabel);
            if (LinePathObject == null) return;
            LinePathObject.Data = LineGeometry;
            LinePathObject.StrokeDashArray = StrokeDashArray;
        }

        internal int ParallelEdgeOffset;
        //internal int TargetOffset;

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
                //if (_edgePointerForSource != null && ShowArrows) _edgePointerForSource.Show();
                //if (_edgePointerForTarget != null && ShowArrows) _edgePointerForTarget.Show();

                if (HasSelfLoopedEdgeTemplate)
                    SelfLoopIndicator!.SetCurrentValue(IsVisibleProperty, false);
            }
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
                //var geometry = Linegeometry as EllipseGeometry;
                //TODO
                //geometry.Center = pt;
                //geometry.RadiusX = SelfLoopIndicatorRadius;
                //geometry.RadiusY = SelfLoopIndicatorRadius;
            }
            else _selfLoopedEdgeLastKnownRect = new Rect(pt, SelfLoopIndicator!.DesiredSize);
        }

        public virtual void PrepareEdgePathFromMousePointer(PointerEventArgs pointerEventArgs,
            bool useCurrentCoords = false)
        {
            //do not calculate invisible edges
            if (!IsVisible && !IsHiddenEdgesUpdated && ManualDrawing || !IsTemplateLoaded) return;

            //get the size of the source
            var sourceSize = new Size(Source!.Width, Source!.Height);

            //get the position center of the source
            var sourcePos = new Point(
                (useCurrentCoords ? GraphAreaBase.GetX(Source) : GraphAreaBase.GetFinalX(Source)) +
                sourceSize.Width * 0.5,
                (useCurrentCoords ? GraphAreaBase.GetY(Source) : GraphAreaBase.GetFinalY(Source)) +
                sourceSize.Height * 0.5);

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
                var sourceCp = Source.GetConnectionPointById(gEdge.SourceConnectionPointId.Value, true);
                if (sourceCp == null)
                {
                    throw new GX_ObjectNotFoundException(string.Format(
                        "Can't find source vertex VCP by edge source connection point Id({1}) : {0}", Source,
                        gEdge.SourceConnectionPointId));
                }

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

            //if (gEdge?.TargetConnectionPointId != null)
            //{
            //    //var targetCp = this.Target.GetConnectionPointById(gEdge.TargetConnectionPointId.Value, true);
            //    //if (targetCp == null)
            //    //    throw new GX_ObjectNotFoundException(string.Format("Can't find target vertex VCP by edge target connection point Id({1}) : {0}", this.Target, gEdge.TargetConnectionPointId));
            //    //if (targetCp.Shape == VertexShape.None) p2 = targetCp.RectangularSize.Center();
            //    //else
            //    //{
            //    //    var sourceCpPos = gEdge.SourceConnectionPointId.HasValue ? this.Source.GetConnectionPointById(gEdge.SourceConnectionPointId.Value, true).RectangularSize.Center() : hasRouteInfo ? routeInformation[routeInformation.Length - 2].ToWindows() : (sourcePos);
            //    //    p2 = GeometryHelper.GetEdgeEndpoint(targetCp.RectangularSize.Center(), targetCp.RectangularSize, sourceCpPos, targetCp.Shape);
            //    //}
            //}
            //else
            var p2 = GeometryHelper.GetEdgeEndpoint(
                targetPos, new Rect(targetPos, targetSize),
                hasRouteInfo ? routeInformation![routeInformation.Length - 2].ToAvalonia() : sourcePos,
                VertexShape.None);

            SourceConnectionPoint = p1;
            TargetConnectionPoint = p2;

            LineGeometry = new PathGeometry();
            PathFigure lineFigure;

            //if we have route and route consist of 2 or more points
            if (hasRouteInfo)
            {
                //replace start and end points with accurate ones
                var routePoints = routeInformation.ToAvalonia()!.ToList();
                routePoints.Clear();
                routePoints.Add(p1);
                routePoints.Add(p2);

                if (routedEdge.RoutingPoints != null)
                    routedEdge.RoutingPoints = routePoints.ToArray().ToGraphX();

                if (RootArea.EdgeCurvingEnabled)
                {
                    var oPolyLineSegment =
                        GeometryHelper.GetCurveThroughPoints([.. routePoints], 0.5, RootArea.EdgeCurvingTolerance);

                    if (hasEpTarget)
                    {
                        UpdateTargetEpData(oPolyLineSegment.Points[^1],
                            oPolyLineSegment.Points[^2]);
                        oPolyLineSegment.Points.RemoveAt(oPolyLineSegment.Points.Count - 1);
                    }

                    if (hasEpSource)
                    {
                        UpdateSourceEpData(oPolyLineSegment.Points.First(), oPolyLineSegment.Points[1]);
                        oPolyLineSegment.Points.RemoveAt(0);
                    }

                    lineFigure =
                        GeometryHelper.GetPathFigureFromPathSegments(routePoints[0], true, oPolyLineSegment);
                }
                else
                {
                    if (hasEpSource)
                        routePoints[0] =
                            routePoints[0].Subtract(UpdateSourceEpData(routePoints.First(), routePoints[1]));
                    if (hasEpTarget)
                        routePoints[^1] = routePoints[^1]
                            .Subtract(UpdateTargetEpData(p2, routePoints[^2]));

                    // Reverse the path if specified.
                    if (gEdge!.ReversePath)
                        routePoints.Reverse();

                    var pcol = new Points();
                    routePoints.ForEach(a => pcol.Add(a));

                    lineFigure = new PathFigure
                        { StartPoint = p1, Segments = [new PolyLineSegment { Points = pcol }], IsClosed = false };
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

                lineFigure = new PathFigure
                {
                    StartPoint = gEdge!.ReversePath ? p2 : p1,
                    Segments =
                        [new LineSegment { Point = gEdge.ReversePath ? p1 : p2 }],
                    IsClosed = false
                };
            }

            ((PathGeometry)LineGeometry).Figures!.Add(lineFigure);
            if (_updateLabelPosition)
                EdgeLabelControls.Where(l => l.ShowLabel).ForEach(l => l.UpdatePosition());

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
            //do not calculate invisible edges
            if (!IsVisible && !IsHiddenEdgesUpdated && Source == null || Target == null ||
                ManualDrawing || !IsTemplateLoaded) return;

            #region Get the inputs

            // Get the TopLeft position of the Source Vertex.
            var sourceTopLeft = new Point(
                useCurrentCoords ? GraphAreaBase.GetX(Source!) : GraphAreaBase.GetFinalX(Source!),
                useCurrentCoords ? GraphAreaBase.GetY(Source!) : GraphAreaBase.GetFinalY(Source!));

            // Get the TopLeft position of the Target Vertex.
            var targetTopLeft = new Point(
                useCurrentCoords ? GraphAreaBase.GetX(Target) : GraphAreaBase.GetFinalX(Target),
                useCurrentCoords ? GraphAreaBase.GetY(Target) : GraphAreaBase.GetFinalY(Target));

            //get the size of the source
            var sourceSize = _isInDesignMode ? new Size(80, 20) : new Size(Source!.Width, Source.Height);

            //get the size of the target
            var targetSize = _isInDesignMode ? new Size(80, 20) : new Size(Target.Width, Target.Height);

            //get the position center of the source
            var sourceCenter = new Point(
                sourceTopLeft.X + sourceSize.Width * .5,
                sourceTopLeft.Y + sourceSize.Height * .5);

            //get the position center of the target

            var targetCenter = new Point(
                targetTopLeft.X + targetSize.Width * .5,
                targetTopLeft.Y + targetSize.Height * .5);


            if (Edge is not IRoutingInfo routedEdge)
                throw new GX_InvalidDataException("Edge must implement IRoutingInfo interface");

            //get the route informations
            var routeInformation = externalRoutingPoints ?? routedEdge.RoutingPoints;

            var hasEpSource = EdgePointerForSource != null;
            var hasEpTarget = EdgePointerForTarget != null;

            #endregion Get the inputs

            //if self looped edge
            if (IsSelfLooped)
            {
                PrepareSelfLoopedEdge(sourceTopLeft);
                return;
            }

            //check if we have some edge route data
            var hasRouteInfo = routeInformation is { Length: > 1 };

            var gEdge = Edge as IGraphXCommonEdge;

            #region Helper lambda expressions

            IVertexConnectionPoint GetSourceCpOrThrow()
            {
                var cp = Source!.GetConnectionPointById(gEdge.SourceConnectionPointId!.Value, true);
                if (cp == null)
                    throw new GX_ObjectNotFoundException(string.Format(
                        "Can't find source vertex VCP by edge source connection point Id({1}) : {0}", Source,
                        gEdge.SourceConnectionPointId));
                return cp;
            }

            IVertexConnectionPoint GetTargetCpOrThrow()
            {
                var cp = Target.GetConnectionPointById(gEdge.TargetConnectionPointId!.Value, true);
                if (cp == null)
                    throw new GX_ObjectNotFoundException(string.Format(
                        "Can't find target vertex VCP by edge target connection point Id({1}) : {0}", Target,
                        gEdge.TargetConnectionPointId));
                return cp;
            }

            Point GetCpEndPoint(IVertexConnectionPoint cp, Point cpCenter, Point distantEnd)
            {
                // If the connection point (cp) doesn't have any shape, the edge comes from its center, otherwise find the location
                // on its perimeter that the edge should come from.
                var calculatedCp = cp.Shape == VertexShape.None
                    ? cpCenter
                    : GeometryHelper.GetEdgeEndpoint(cpCenter, cp.RectangularSize, distantEnd, cp.Shape);
                return calculatedCp;
            }

            bool NeedParallelCalc() => !hasRouteInfo && RootArea.EnableParallelEdges && IsParallel;

            #endregion

            //calculate edge source (p1) and target (p2) endpoints based on different settings
            if (gEdge is { SourceConnectionPointId: not null, TargetConnectionPointId: not null })
            {
                // Get the connection points and their centers
                var sourceCp = GetSourceCpOrThrow();
                var targetCp = GetTargetCpOrThrow();
                var sourceCpCenter = sourceCp.RectangularSize.Center();
                var targetCpCenter = targetCp.RectangularSize.Center();

                SourceConnectionPoint = GetCpEndPoint(sourceCp, sourceCpCenter, targetCpCenter);
                TargetConnectionPoint = GetCpEndPoint(targetCp, targetCpCenter, sourceCpCenter);
            }
            else if (gEdge?.SourceConnectionPointId != null)
            {
                var sourceCp = GetSourceCpOrThrow();
                var sourceCpCenter = sourceCp.RectangularSize.Center();

                // In the case of parallel edges, the target direction needs to be found and the correct offset calculated. Otherwise, fall back
                // to route information or simply the center of the target vertex.
                if (NeedParallelCalc())
                {
                    var m = new Point(targetCenter.X - sourceCenter.X, targetCenter.Y - sourceCenter.Y);
                    targetCenter = new Point(sourceCpCenter.X + m.X, sourceCpCenter.Y + m.Y);
                }
                else if (hasRouteInfo)
                {
                    targetCenter = routeInformation![1].ToAvalonia();
                }

                SourceConnectionPoint = GetCpEndPoint(sourceCp, sourceCpCenter, targetCenter);
                TargetConnectionPoint = GeometryHelper.GetEdgeEndpoint(targetCenter,
                    new Rect(targetTopLeft, targetSize),
                    hasRouteInfo ? routeInformation![routeInformation.Length - 2].ToAvalonia() : sourceCpCenter,
                    Target.VertexShape);
            }
            else if (gEdge?.TargetConnectionPointId != null)
            {
                var targetCp = GetTargetCpOrThrow();
                var targetCpCenter = targetCp.RectangularSize.Center();

                // In the case of parallel edges, the source direction needs to be found and the correct offset calculated. Otherwise, fall back
                // to route information or simply the center of the source vertex.
                if (NeedParallelCalc())
                {
                    var m = new Point(sourceCenter.X - targetCenter.X, sourceCenter.Y - targetCenter.Y);
                    sourceCenter = new Point(targetCpCenter.X + m.X, targetCpCenter.Y + m.Y);
                }
                else if (hasRouteInfo)
                {
                    sourceCenter = routeInformation![routeInformation.Length - 2].ToAvalonia();
                }

                SourceConnectionPoint = GeometryHelper.GetEdgeEndpoint(sourceCenter,
                    new Rect(sourceTopLeft, sourceSize),
                    hasRouteInfo ? routeInformation![1].ToAvalonia() : targetCpCenter, Source!.VertexShape);
                TargetConnectionPoint = GetCpEndPoint(targetCp, targetCpCenter, sourceCenter);
            }
            else
            {
                //calculate source and target edge attach points
                if (NeedParallelCalc())
                {
                    var origSC = sourceCenter;
                    var origTC = targetCenter;
                    sourceCenter = GetParallelOffset(origSC, origTC, ParallelEdgeOffset);
                    targetCenter = GetParallelOffset(origTC, origSC, -ParallelEdgeOffset);
                }

                SourceConnectionPoint = GeometryHelper.GetEdgeEndpoint(sourceCenter,
                    new Rect(sourceTopLeft, sourceSize),
                    hasRouteInfo ? routeInformation![1].ToAvalonia() : targetCenter, Source!.VertexShape);
                TargetConnectionPoint = GeometryHelper.GetEdgeEndpoint(targetCenter,
                    new Rect(targetTopLeft, targetSize),
                    hasRouteInfo ? routeInformation![routeInformation.Length - 2].ToAvalonia() : sourceCenter,
                    Target.VertexShape);
            }

            // If the logic above is working correctly, both the source and target connection points will exist.
            if (!SourceConnectionPoint.HasValue || !TargetConnectionPoint.HasValue)
                throw new GX_GeneralException("One or both connection points was not found due to an internal error.");

            var p1 = SourceConnectionPoint.Value;
            var p2 = TargetConnectionPoint.Value;

            LineGeometry = new PathGeometry();
            PathFigure lineFigure;

            //if we have route and route consist of 2 or more points
            if (RootArea != null && hasRouteInfo)
            {
                //replace start and end points with accurate ones
                var routePoints = routeInformation.ToAvalonia()!.ToList();
                routePoints.Remove(routePoints.First());
                routePoints.Remove(routePoints.Last());
                routePoints.Insert(0, p1);
                routePoints.Add(p2);

                if (externalRoutingPoints == null && routedEdge.RoutingPoints != null)
                    routedEdge.RoutingPoints = routePoints.ToArray().ToGraphX();

                if (RootArea.EdgeCurvingEnabled)
                {
                    var oPolyLineSegment =
                        GeometryHelper.GetCurveThroughPoints([.. routePoints], 0.5, RootArea.EdgeCurvingTolerance);

                    if (hasEpTarget)
                    {
                        UpdateTargetEpData(oPolyLineSegment.Points[^1],
                            oPolyLineSegment.Points[^2]);
                        oPolyLineSegment.Points.RemoveAt(oPolyLineSegment.Points.Count - 1);
                    }

                    if (hasEpSource)
                    {
                        UpdateSourceEpData(oPolyLineSegment.Points.First(), oPolyLineSegment.Points[1]);
                        oPolyLineSegment.Points.RemoveAt(0);
                    }

                    lineFigure =
                        GeometryHelper.GetPathFigureFromPathSegments(routePoints[0], true, oPolyLineSegment);
                }
                else
                {
                    if (hasEpSource)
                        routePoints[0] =
                            routePoints[0].Subtract(UpdateSourceEpData(routePoints.First(), routePoints[1]));
                    if (hasEpTarget)
                        routePoints[^1] = routePoints[^1]
                            .Subtract(UpdateTargetEpData(p2, routePoints[^2]));

                    // Reverse the path if specified.
                    if (gEdge!.ReversePath)
                        routePoints.Reverse();

                    var pcol = new Points();
                    routePoints.ForEach(a => pcol.Add(a));

                    lineFigure = new PathFigure
                    {
                        StartPoint = p1,
                        Segments = [new PolyLineSegment { Points = pcol }],
                        IsClosed = false
                    };
                }
            }
            else // no route defined
            {
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

                lineFigure = TransformUnroutedPath(new PathFigure
                {
                    StartPoint = gEdge!.ReversePath ? p2 : p1,
                    Segments = [new LineSegment { Point = gEdge.ReversePath ? p1 : p2 }],
                    IsClosed = false
                });
            }

            ((PathGeometry)LineGeometry).Figures!.Add(lineFigure);
            if (_updateLabelPosition && updateLabel)
                EdgeLabelControls.Where(l => l.ShowLabel).ForEach(l => l.UpdatePosition());
        }

        protected virtual PathFigure TransformUnroutedPath(PathFigure original)
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

        #endregion public PrepareEdgePath()

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
            return EdgeLabelControls.Select(l => l.GetSize()).ToList();
        }

        /*  public void SetCustomLabelSize(SysRect rect)
          {
              EdgeLabelControl.SetSize(rect);
          }*/

        /// <summary>
        /// Returns all edge controls attached to this entity
        /// </summary>
        public IList<IEdgeLabelControl> GetLabelControls()
        {
            return [.. EdgeLabelControls];
        }
    }
}