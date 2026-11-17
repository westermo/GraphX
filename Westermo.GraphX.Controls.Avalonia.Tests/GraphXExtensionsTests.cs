using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Models;
using MPoint = Westermo.GraphX.Measure.Point;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for <see cref="GraphXExtensions"/>, the graph query and copy helpers used throughout the
/// logic core (layout, overlap removal, edge routing) and by consumers to clone and convert graphs.
/// </summary>
public class GraphXExtensionsTests
{
    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override MPoint[]? RoutingPoints { get; set; } = null;
    }

    private class TGraph : BidirectionalGraph<TVertex, TEdge>;

    /// <summary>
    /// Creates a graph with the given vertex names and "XY" edge strings (X→Y).
    /// </summary>
    private static (TGraph Graph, Dictionary<string, TVertex> V) Build(string vertices, params string[] edges)
    {
        var g = new TGraph();
        var map = vertices.ToDictionary(c => c.ToString(), c => new TVertex(c.ToString()));
        g.AddVertexRange(map.Values);
        foreach (var e in edges)
            g.AddEdge(new TEdge(map[e[0].ToString()], map[e[1].ToString()]));
        return (g, map);
    }

    private static string Names(IEnumerable<TVertex> vertices) =>
        string.Join(",", vertices.Select(v => v.Name).OrderBy(n => n));

    private static string Names(IEnumerable<TEdge> edges) =>
        string.Join(",", edges.Select(e => e.Source.Name + e.Target.Name).OrderBy(n => n));

    #region Edge and neighbour queries

    [Test]
    public async Task GetAllEdges_ReturnsInAndOutEdges()
    {
        var (g, v) = Build("ABCD", "AB", "BC", "DB");
        await Assert.That(Names(g.GetAllEdges(v["B"]))).IsEqualTo("AB,BC,DB");
    }

    [Test]
    public async Task GetAllEdges_ReturnsEmpty_ForVertexNotInGraph()
    {
        var (g, _) = Build("AB", "AB");
        await Assert.That(g.GetAllEdges(new TVertex("X")).Count()).IsEqualTo(0);
    }

    [Test]
    public async Task GetAllEdges_SelfLoop_IsReportedTwice()
    {
        // A self loop is both an in- and out-edge; callers must be aware it appears twice.
        var (g, v) = Build("A", "AA");
        await Assert.That(g.GetAllEdges(v["A"]).Count()).IsEqualTo(2);
    }

    [Test]
    public async Task GetInEdges_And_GetOutEdges()
    {
        var (g, v) = Build("ABC", "AB", "CB", "BA");
        await Assert.That(Names(g.GetInEdges(v["B"]))).IsEqualTo("AB,CB");
        await Assert.That(Names(g.GetOutEdges(v["B"]))).IsEqualTo("BA");
        // Missing vertices are tolerated and yield empty results instead of throwing.
        await Assert.That(g.GetInEdges(new TVertex("X")).Count()).IsEqualTo(0);
        await Assert.That(g.GetOutEdges(new TVertex("X")).Count()).IsEqualTo(0);
    }

    [Test]
    public async Task GetNeighbours_IsDistinctUnionOfInAndOutNeighbours()
    {
        // A↔B in both directions must only list each neighbour once.
        var (g, v) = Build("ABCD", "AB", "BA", "CA", "AD");
        await Assert.That(Names(g.GetNeighbours(v["A"]))).IsEqualTo("B,C,D");
        await Assert.That(Names(g.GetOutNeighbours(v["A"]))).IsEqualTo("B,D");
        await Assert.That(Names(g.GetInNeighbours(v["A"]))).IsEqualTo("B,C");
    }

    [Test]
    public async Task GetOutNeighbours_RemovesDuplicatesFromParallelEdges()
    {
        var (g, v) = Build("AB", "AB", "AB");
        await Assert.That(g.EdgeCount).IsEqualTo(2);
        await Assert.That(g.GetOutNeighbours(v["A"]).Count()).IsEqualTo(1);
        await Assert.That(g.GetInNeighbours(v["B"]).Count()).IsEqualTo(1);
    }

    [Test]
    public async Task GetSources_ReturnsVerticesWithoutInEdges()
    {
        var (g, _) = Build("ABCDE", "AB", "BC", "DC");
        // E is isolated and therefore also a source.
        await Assert.That(Names(g.GetSources())).IsEqualTo("A,D,E");
    }

    [Test]
    public async Task GetSources_EmptyForCycle()
    {
        var (g, _) = Build("ABC", "AB", "BC", "CA");
        await Assert.That(g.GetSources().Any()).IsFalse();
    }

    #endregion

    #region GetEdgesBetween

    [Test]
    public async Task GetEdgesBetween_Directed_OnlySet1ToSet2()
    {
        var (g, v) = Build("ABCD", "AC", "CA", "BD", "AB");
        var result = g.GetEdgesBetween([v["A"], v["B"]], [v["C"], v["D"]]);
        await Assert.That(Names(result)).IsEqualTo("AC,BD");
    }

    [Test]
    public async Task GetEdgesBetween_Undirected_IncludesReverseEdges()
    {
        var (g, v) = Build("ABCD", "AC", "CA", "DB", "AB");
        var result = g.GetEdgesBetween([v["A"], v["B"]], [v["C"], v["D"]], undirected: true);
        await Assert.That(Names(result)).IsEqualTo("AC,CA,DB");
    }

    [Test]
    public async Task GetEdgesBetween_EmptySets_ReturnEmpty()
    {
        var (g, v) = Build("AB", "AB");
        await Assert.That(g.GetEdgesBetween([], [v["B"]]).Any()).IsFalse();
        await Assert.That(g.GetEdgesBetween([v["A"]], []).Any()).IsFalse();
    }

    [Test]
    public async Task GetEdgesBetween_Undirected_OverlappingSets_ReturnsEachEdgeOnce()
    {
        // An edge whose endpoints are in both sets matches in both directions but must be reported once.
        var (g, v) = Build("ABC", "AB", "BA", "CC", "AC");
        List<TVertex> both = [v["A"], v["B"], v["C"]];
        var result = g.GetEdgesBetween(both, both, undirected: true).ToList();
        await Assert.That(result.Count).IsEqualTo(4);
        await Assert.That(Names(result)).IsEqualTo("AB,AC,BA,CC");
    }

    [Test]
    public async Task GetEdgesBetween_Undirected_PartialOverlap_ReturnsEachEdgeOnce()
    {
        // B is in both sets: A→B (set1→set2) and B→A (set2→set1) are distinct edges; B→C appears once.
        var (g, v) = Build("ABC", "AB", "BA", "BC", "CB");
        var result = g.GetEdgesBetween([v["A"], v["B"]], [v["B"], v["C"]], undirected: true).ToList();
        await Assert.That(Names(result)).IsEqualTo("AB,BA,BC,CB");
        await Assert.That(result.Distinct().Count()).IsEqualTo(result.Count);
    }

    [Test]
    public async Task GetEdgesBetween_DuplicateInputVertices_DoNotDuplicateEdges()
    {
        var (g, v) = Build("AB", "AB");
        var result = g.GetEdgesBetween([v["A"], v["A"]], [v["B"], v["B"]]).ToList();
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetEdgesBetween_ParallelEdges_AreAllReturned()
    {
        // Distinct edge instances between the same vertices are separate results.
        var (g, v) = Build("AB", "AB", "AB");
        var result = g.GetEdgesBetween([v["A"]], [v["B"]], undirected: true).ToList();
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetEdgesBetween_VerticesNotInGraph_AreIgnored()
    {
        var (g, v) = Build("AB", "AB");
        var stranger = new TVertex("X");
        var result = g.GetEdgesBetween([v["A"], stranger], [v["B"], stranger], undirected: true).ToList();
        await Assert.That(Names(result)).IsEqualTo("AB");
    }

    #endregion

    #region Diameter

    [Test]
    public async Task GetDiameter_PathGraph_IsNumberOfHops()
    {
        var (g, _) = Build("ABCD", "AB", "BC", "CD");
        await Assert.That(g.GetDiameter<TVertex, TEdge, TGraph>()).IsEqualTo(3d);
    }

    [Test]
    public async Task GetDiameter_IgnoresEdgeDirection()
    {
        // Directed A→C needs two hops (A→B→C) but the diameter is computed on the undirected view.
        var (g, _) = Build("ABC", "AB", "BC", "CA");
        await Assert.That(g.GetDiameter<TVertex, TEdge, TGraph>()).IsEqualTo(1d);
    }

    [Test]
    public async Task GetDiameter_OutDistances_AreSymmetricWithZeroDiagonal()
    {
        var (g, _) = Build("ABC", "AB", "BC");
        var diameter = g.GetDiameter<TVertex, TEdge, TGraph>(out var d);

        await Assert.That(diameter).IsEqualTo(2d);
        await Assert.That(d.GetLength(0)).IsEqualTo(3);
        for (var i = 0; i < 3; i++)
        {
            await Assert.That(d[i, i]).IsEqualTo(0d);
            for (var j = 0; j < 3; j++)
                await Assert.That(d[i, j]).IsEqualTo(d[j, i]);
        }
    }

    [Test]
    public async Task GetDiameter_SingleOrNoVertex_IsNegativeInfinity()
    {
        var (single, _) = Build("A");
        var (empty, _) = Build("");
        await Assert.That(single.GetDiameter<TVertex, TEdge, TGraph>()).IsEqualTo(double.NegativeInfinity);
        await Assert.That(empty.GetDiameter<TVertex, TEdge, TGraph>()).IsEqualTo(double.NegativeInfinity);
    }

    [Test]
    public async Task GetDiameter_DisconnectedGraph_IgnoresUnreachablePairs()
    {
        // Two components: A-B-C and D-E. Unreachable pairs are skipped, so the diameter is
        // the largest finite distance (A..C = 2).
        var (g, _) = Build("ABCDE", "AB", "BC", "DE");
        await Assert.That(g.GetDiameter<TVertex, TEdge, TGraph>()).IsEqualTo(2d);
    }

    #endregion

    #region Misc helpers

    [Test]
    public async Task OtherVertex_ReturnsOppositeEndpoint()
    {
        var a = new TVertex("A");
        var b = new TVertex("B");
        var e = new TEdge(a, b);
        await Assert.That(e.OtherVertex(a)).IsSameReferenceAs(b);
        await Assert.That(e.OtherVertex(b)).IsSameReferenceAs(a);
        // Any vertex that is not the source maps to the source.
        await Assert.That(e.OtherVertex(new TVertex("X"))).IsSameReferenceAs(a);
    }

    [Test]
    public async Task AddEdgeRange_AddsAllEdges()
    {
        var (g, v) = Build("ABC");
        g.AddEdgeRange(new[] { new TEdge(v["A"], v["B"]), new TEdge(v["B"], v["C"]) }.AsEnumerable());
        await Assert.That(g.EdgeCount).IsEqualTo(2);
    }

    #endregion

    #region Copy / Convert

    [Test]
    public async Task CopyToBidirectionalGraph_IncludeEmpty_KeepsIsolatedVertices()
    {
        var (g, _) = Build("ABC", "AB");
        var copy = g.CopyToBidirectionalGraph();

        await Assert.That(copy).IsNotSameReferenceAs(g);
        await Assert.That(copy.VertexCount).IsEqualTo(3);
        await Assert.That(copy.EdgeCount).IsEqualTo(1);
    }

    [Test]
    public async Task CopyToBidirectionalGraph_ExcludeEmpty_DropsIsolatedVertices()
    {
        var (g, _) = Build("ABC", "AB");
        var copy = g.CopyToBidirectionalGraph(includeEmpty: false);

        await Assert.That(Names(copy.Vertices)).IsEqualTo("A,B");
        await Assert.That(copy.EdgeCount).IsEqualTo(1);
    }

    [Test]
    public async Task CopyToBidirectionalGraph_IsShallow_ButStructurallyIndependent()
    {
        var (g, v) = Build("AB", "AB");
        var copy = g.CopyToBidirectionalGraph();

        // Elements are shared...
        await Assert.That(copy.Vertices.Contains(v["A"])).IsTrue();
        // ...but mutating the copy's structure must not affect the original.
        copy.RemoveVertex(v["A"]);
        await Assert.That(g.VertexCount).IsEqualTo(2);
        await Assert.That(g.EdgeCount).IsEqualTo(1);
    }

    [Test]
    public async Task CopyToGraph_PreservesGraphType()
    {
        var (g, _) = Build("ABC", "AB");

        var full = g.CopyToGraph<TGraph, TVertex, TEdge>();
        var trimmed = g.CopyToGraph<TGraph, TVertex, TEdge>(includeEmpty: false);

        await Assert.That(full).IsTypeOf<TGraph>();
        await Assert.That(full.VertexCount).IsEqualTo(3);
        await Assert.That(full.EdgeCount).IsEqualTo(1);
        await Assert.That(trimmed.VertexCount).IsEqualTo(2);
    }

    [Test]
    public async Task Convert_MapsVerticesAndEdges()
    {
        var (g, _) = Build("ABC", "AB", "BC");

        var converted = g.Convert<TVertex, TEdge, string, SEdge<string>>(
            vx => vx.Name,
            e => new SEdge<string>(e.Source.Name, e.Target.Name));

        await Assert.That(string.Join(",", converted.Vertices.OrderBy(x => x))).IsEqualTo("A,B,C");
        await Assert.That(converted.ContainsEdge("A", "B")).IsTrue();
        await Assert.That(converted.ContainsEdge("B", "C")).IsTrue();
        await Assert.That(converted.EdgeCount).IsEqualTo(2);
    }

    [Test]
    public async Task Convert_IntoExistingGraph_WithoutMappers_CopiesElements()
    {
        var (g, _) = Build("ABC", "AB");
        var target = new TGraph();

        var result = g.Convert<TVertex, TEdge, TGraph>(target);

        await Assert.That(result).IsSameReferenceAs(target);
        await Assert.That(target.VertexCount).IsEqualTo(3);
        await Assert.That(target.EdgeCount).IsEqualTo(1);
    }

    #endregion
}
