using Avalonia;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Models;
using Avalonia.Media;

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
        var geom = ec.GetLineGeometry();
        await Assert.That(geom).IsNotNull();
        var pg = geom as PathGeometry;
        await Verify(pg);
    }

    [Test]
    public async Task EdgeGeometry_IsEllipse_ForSelfLoop()
    {
        var area = CreateArea(out var v1, out var v2, selfLoop: true);
        var ec = area.EdgesList.Values.First();
        var geom = ec.GetLineGeometry();
        var ellipse = geom as EllipseGeometry;
        await Assert.That(ellipse).IsNotNull();
        await Verify(new EllipseDescriptor(ellipse));
    }

    [Test]
    public async Task ParallelEdges_AssignDifferentOffsets()
    {
        var area = CreateArea(out var v1, out var v2, selfLoop: false, parallelEdges: 3);
        var list = area.EdgesList.Values.ToList();
        // Force update
        area.UpdateAllEdges(true);
        // Capture connection points; parallel edges should not all share identical connection point pairs
        var pairs = list.Select(e => (e.SourceConnectionPoint, e.TargetConnectionPoint)).ToList();
        // At least one pair should differ if parallel offsets applied
        await Verify(pairs);
    }
}

public class EllipseDescriptor(EllipseGeometry geometry)
{
    public Point Center { get; } = geometry.Center;
    public double RadiusX { get; } = geometry.RadiusX;
    public double RadiusY { get; } = geometry.RadiusY;
    public Rect Rect { get; } = geometry.Rect;
}