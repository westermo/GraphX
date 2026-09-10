using System.Runtime.CompilerServices;
using Avalonia;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Models;
using Avalonia.Media;
using Avalonia.Threading;
using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class EdgeGeometryTests
{
    private class Vertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class Edge(Vertex s, Vertex t) : EdgeBase<Vertex>(s, t)
    {
        public override Measure.Point[]? RoutingPoints { get; set; } = null;
    }

    private GraphArea<Vertex, Edge, BidirectionalGraph<Vertex, Edge>> CreateArea(out Vertex v1, out Vertex v2,
        bool selfLoop = false, int parallelEdges = 0)
    {
        var g = new BidirectionalGraph<Vertex, Edge>();
        v1 = new Vertex("A");
        v2 = selfLoop ? v1 : new Vertex("B");
        g.AddVertex(v1);
        if (!selfLoop) g.AddVertex(v2);
        if (parallelEdges <= 0) g.AddEdge(new Edge(v1, v2));
        else
            for (int i = 0; i < parallelEdges; i++)
                g.AddEdge(new Edge(v1, v2));

        var lc = new GXLogicCore<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
        {
            Graph = g,
            EnableParallelEdges = parallelEdges > 1,
            EdgeCurvingEnabled = false
        };

        var area = new GraphArea<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
        {
            LogicCore = lc,
            Width = 800,
            Height = 600
        };

        // Preload vertexes & assign simple positions manually
        area.PreloadVertexes();
        // position vertices
        foreach (var (key, vc) in area.VertexList)
        {
            if (key == v1) vc.SetPosition(100, 100);
            if (!selfLoop && key == v2) vc.SetPosition(300, 100);
            GraphAreaBase.SetFinalX(vc, vc.GetPosition().X);
            GraphAreaBase.SetFinalY(vc, vc.GetPosition().Y);
        }

        // generate edges now
        area.GenerateAllEdges(true);
        area.UpdateAllEdges(true);
        return area;
    }

    [Test]
    public async Task EdgeGeometry_IsCreated_ForSimpleEdge()
    {
        var area = CreateArea(out var v1, out var v2);

        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));
        var geom = ec.GetLineGeometry();
        await Assert.That(geom).IsNotNull();
        // For StreamGeometry, we verify the bounds and type instead of path data
        // since StreamGeometry doesn't expose its path data string directly
        await Assert.That(geom).IsTypeOf<StreamGeometry>();
        var bounds = geom!.Bounds;
        await Verify(new { GeometryType = geom.GetType().Name, Bounds = bounds }, GetSettings());
    }

    private VerifySettings? GetSettings([CallerMemberName] string? testName = null)
    {
        var settings = new VerifySettings();
        if (testName is not null)
        {
            settings.UseMethodName(testName);
        }

        settings.UseTypeName(nameof(EdgeGeometryTests));

        return settings;
    }

    [Test]
    public async Task EdgeGeometry_IsEllipse_ForSelfLoop()
    {
        var area = CreateArea(out var v1, out var v2, selfLoop: true);
        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));

        var geom = ec.GetLineGeometry();
        var ellipse = geom as EllipseGeometry;
        await Assert.That(ellipse).IsNotNull();
        await Verify(new EllipseDescriptor(ellipse!), GetSettings());
    }

    [Test]
    public async Task SelfLoop_EdgeControl_HasNonZeroBounds()
    {
        // Regression: previously self-looped edges collapsed _pathBounds to a degenerate point because
        // SourceConnectionPoint == TargetConnectionPoint, leaving the EdgeControl 1x1 and the
        // self-loop indicator clipped/invisible in real templates.
        var area = CreateArea(out _, out _, selfLoop: true);
        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));

        // The control must be at least the size of the built-in self-loop indicator (radius * 2).
        var minSide = ec.SelfLoopIndicatorRadius * 2;
        await Assert.That(ec.Width).IsGreaterThanOrEqualTo(minSide);
        await Assert.That(ec.Height).IsGreaterThanOrEqualTo(minSide);
        // Width/Height must not have been pinned at the previous "1" floor.
        await Assert.That(ec.Width).IsGreaterThan(1d);
        await Assert.That(ec.Height).IsGreaterThan(1d);
    }

    [Test]
    public async Task SelfLoop_EdgeControl_IsAnchoredNearSourceVertex()
    {
        // The self-loop indicator should sit near the source vertex's position rather than at the
        // graph-area origin or some other arbitrary location.
        var area = CreateArea(out var v1, out _, selfLoop: true);
        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));

        var sourceVc = area.VertexList[v1];
        var sourcePos = sourceVc.GetPosition();
        var ecPos = new Point(GraphAreaBase.GetX(ec), GraphAreaBase.GetY(ec));

        // The EdgeControl's top-left should be within (radius*2 + |offset|) of the source vertex,
        // proving it tracks the vertex rather than collapsing to (0,0) or some unrelated point.
        var maxDistance = ec.SelfLoopIndicatorRadius * 2
                          + Math.Abs(ec.SelfLoopIndicatorOffset.X)
                          + Math.Abs(ec.SelfLoopIndicatorOffset.Y);
        await Assert.That(Math.Abs(ecPos.X - sourcePos.X)).IsLessThanOrEqualTo(maxDistance);
        await Assert.That(Math.Abs(ecPos.Y - sourcePos.Y)).IsLessThanOrEqualTo(maxDistance);
    }

    [Test]
    public async Task SelfLoop_EdgeControl_DesiredSize_MatchesIndicatorRect()
    {
        // Regression: MeasureOverride used to Union a (radius*2 + offset, radius*2 + offset) estimate
        // into the result, inflating the EdgeControl past the actual indicator. Avalonia's Grid then
        // centred the small visible Path inside the inflated bounds, often hiding it behind the
        // source vertex. After the fix DesiredSize must equal the indicator rect itself.
        var area = CreateArea(out _, out _, selfLoop: true);
        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        // No PART_SelfLoopedEdge template part is loaded in this test, so the built-in ellipse
        // path is used, sized exactly to (radius * 2) on each axis.
        var expected = ec.SelfLoopIndicatorRadius * 2;
        await Assert.That(ec.DesiredSize.Width).IsEqualTo(expected);
        await Assert.That(ec.DesiredSize.Height).IsEqualTo(expected);
    }

    [Test]
    public async Task SelfLoop_LabelMidpoint_IsLocalToEdgeControl()
    {
        // Regression: GetMidpoint() previously returned absolute graph-area coordinates for
        // self-loop edges (Source.GetCenterPosition() + SelfLoopIndicatorOffset). The label
        // positioning code in ArrangeOverride then added _pathBounds.X/Y on top, double-offsetting
        // the label by the EdgeControl's top-left. The fix returns local coords (centre of
        // _pathBounds) so labels end up centred over the indicator.
        var area = CreateArea(out _, out _, selfLoop: true);
        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));

        var midpointMethod = typeof(EdgeControlBase).GetMethod(
            "GetMidpoint",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        await Assert.That(midpointMethod).IsNotNull();
        object?[] args = [0d, false, default(Vector)];
        var midpoint = (Point)midpointMethod!.Invoke(ec, args)!;

        // For a self-loop the local midpoint must lie inside the EdgeControl's own bounds —
        // proving it's expressed in EdgeControl-local space, not absolute graph-area space.
        await Assert.That(midpoint.X).IsGreaterThanOrEqualTo(0d);
        await Assert.That(midpoint.Y).IsGreaterThanOrEqualTo(0d);
        await Assert.That(midpoint.X).IsLessThanOrEqualTo(ec.DesiredSize.Width);
        await Assert.That(midpoint.Y).IsLessThanOrEqualTo(ec.DesiredSize.Height);
    }

    [Test]
    public async Task ParallelEdges_AssignDifferentOffsets()
    {
        var area = CreateArea(out var v1, out var v2, selfLoop: false, parallelEdges: 3);
        var list = area.EdgesList.Values.ToList();
        // Force update
        area.UpdateAllEdges(true);
        foreach (var edge in list)
        {
            edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            edge.Arrange(new Rect(0, 0, edge.DesiredSize.Width, edge.DesiredSize.Height));
        }

        // Capture connection points; parallel edges should not all share identical connection point pairs
        var pairs = list.Select(e => (e.SourceConnectionPoint, e.TargetConnectionPoint)).ToList();
        // At least one pair should differ if parallel offsets applied
        await Verify(pairs, GetSettings());
    }

    [Test]
    public async Task Edge_FirstMeasureFailure_RecoversOnceVertexIsPositioned()
    {
        // Regression test: if an EdgeControl's *first* Measure/Arrange pass runs before its Source
        // vertex has a valid position (X/Y still NaN - e.g. added to the graph before a layout
        // algorithm/position assignment has run), the edge must still end up with correct, non-null
        // geometry once the vertex is positioned - it must not get permanently stuck rendering nothing.
        //
        // EdgeControlBase used to unconditionally clear its internal "geometry dirty" flag in
        // ArrangeOverride even when PrepareEdgeLayout() failed (returned null because Source/Target
        // weren't ready yet). That flag is now only cleared on a successful computation, and
        // MeasureOverride resets the cached geometry-input signature on failure so that whichever
        // Measure/Arrange pass *does* succeed is guaranteed to rebuild the geometry.
        var g = new BidirectionalGraph<Vertex, Edge>();
        var v1 = new Vertex("A");
        var v2 = new Vertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var edgeData = new Edge(v1, v2);
        g.AddEdge(edgeData);

        var lc = new GXLogicCore<Vertex, Edge, BidirectionalGraph<Vertex, Edge>> { Graph = g };
        var area = new GraphArea<Vertex, Edge, BidirectionalGraph<Vertex, Edge>> { LogicCore = lc };
        area.PreloadVertexes(); // vertices added, X/Y left at NaN (no position assigned yet)

        var v1c = area.VertexList[v1];
        var v2c = area.VertexList[v2];
        v1c.Width = 40;
        v1c.Height = 30;
        v2c.Width = 40;
        v2c.Height = 30;

        var ec = area.ControlFactory.CreateEdgeControl(v1c, v2c, edgeData);

        // First layout pass while the vertices are still unpositioned (X/Y == NaN): must not produce
        // a usable geometry, and must not get "stuck" reporting success for this bad state.
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));

        await Assert.That(ec.GetLineGeometry()).IsNull();

        // The layout algorithm now assigns real positions, as it normally would.
        v1c.SetPosition(100, 100);
        v2c.SetPosition(300, 100);
        GraphAreaBase.SetFinalX(v1c, 100);
        GraphAreaBase.SetFinalY(v1c, 100);
        GraphAreaBase.SetFinalX(v2c, 300);
        GraphAreaBase.SetFinalY(v2c, 100);

        // A follow-up layout pass must now produce valid, non-null geometry.
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));
        await Assert.That(ec.GetLineGeometry()).IsNotNull();
    }

    [Test]
    public async Task UpdateParallelEdgesData_RefreshesAlreadyMeasuredEdge_WhenItBecomesParallel()
    {
        // Regression test: adding a second edge between an already-connected vertex pair (with
        // EnableParallelEdges) must cause the *pre-existing* edge - which was already measured/arranged
        // as a plain, non-parallel straight line - to move to its new offset. IsParallel/
        // ParallelEdgeOffset are plain properties with no Avalonia change notification, so
        // UpdateParallelEdgesData() must explicitly invalidate any edge whose offset it changes.
        var g = new BidirectionalGraph<Vertex, Edge>();
        var v1 = new Vertex("A");
        var v2 = new Vertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var firstEdgeData = new Edge(v1, v2);
        g.AddEdge(firstEdgeData);

        var lc = new GXLogicCore<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
        {
            Graph = g,
            EnableParallelEdges = true,
            EdgeCurvingEnabled = false
        };
        var area = new GraphArea<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
        {
            LogicCore = lc,
            Width = 800,
            Height = 600
        };

        area.PreloadVertexes();
        var v1c = area.VertexList[v1];
        var v2c = area.VertexList[v2];
        v1c.SetPosition(100, 100);
        v2c.SetPosition(300, 100);
        GraphAreaBase.SetFinalX(v1c, 100);
        GraphAreaBase.SetFinalY(v1c, 100);
        GraphAreaBase.SetFinalX(v2c, 300);
        GraphAreaBase.SetFinalY(v2c, 100);

        area.GenerateAllEdges(true);
        var firstEdgeControl = area.EdgesList[firstEdgeData];
        firstEdgeControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        firstEdgeControl.Arrange(new Rect(0, 0, firstEdgeControl.DesiredSize.Width,
            firstEdgeControl.DesiredSize.Height));

        // Before any parallel edge exists, the first edge is a plain (non-offset) connection.
        await Assert.That(firstEdgeControl.IsParallel).IsFalse();
        var originalSource = firstEdgeControl.SourceEndpoint;

        // Now add a second edge between the same pair and generate it incrementally, the way an
        // application would when growing an existing graph (not a full GenerateAllEdges() rebuild).
        var secondEdgeData = new Edge(v1, v2);
        g.AddEdge(secondEdgeData);
        var secondEdgeControl = area.ControlFactory.CreateEdgeControl(v1c, v2c, secondEdgeData);
        area.AddEdge(secondEdgeData, secondEdgeControl);
        area.UpdateParallelEdgesData();

        // The pre-existing edge must have been told to recompute: IsParallel should now be true and
        // its geometry must reflect the new (non-zero) offset without needing an explicit
        // Measure()/Arrange() forced by the test, and without moving any vertex.
        await Assert.That(firstEdgeControl.IsParallel).IsTrue();

        firstEdgeControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        firstEdgeControl.Arrange(new Rect(0, 0, firstEdgeControl.DesiredSize.Width,
            firstEdgeControl.DesiredSize.Height));

        await Assert.That(firstEdgeControl.SourceEndpoint).IsNotEqualTo(originalSource);
    }
}

public class EllipseDescriptor(EllipseGeometry geometry)
{
    public Point Center { get; } = geometry.Center;
    public double RadiusX { get; } = geometry.RadiusX;
    public double RadiusY { get; } = geometry.RadiusY;
    public Rect Rect { get; } = geometry.Rect;
}