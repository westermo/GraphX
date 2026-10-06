using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using Westermo.GraphX.Logic.Algorithms;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for <see cref="SoftMutableBidirectionalGraph{TVertex,TEdge}"/>, the graph used by layout algorithms
/// (e.g. Sugiyama / compound layouts) to temporarily hide and later restore vertices and edges.
/// Hiding is a data-mutation path: a defect here silently drops or duplicates graph elements.
/// </summary>
public class SoftMutableGraphTests
{
    private sealed record Fixture(
        SoftMutableBidirectionalGraph<string, Edge<string>> Graph,
        Edge<string> AB,
        Edge<string> BC,
        Edge<string> CA,
        Edge<string> CD);

    /// <summary>
    /// Builds the graph A→B→C→A plus C→D.
    /// </summary>
    private static Fixture CreateGraph()
    {
        var g = new SoftMutableBidirectionalGraph<string, Edge<string>>();
        g.AddVertexRange(["A", "B", "C", "D"]);
        var ab = new Edge<string>("A", "B");
        var bc = new Edge<string>("B", "C");
        var ca = new Edge<string>("C", "A");
        var cd = new Edge<string>("C", "D");
        g.AddEdgeRange([ab, bc, ca, cd]);
        return new Fixture(g, ab, bc, ca, cd);
    }

    #region Hiding vertices

    [Test]
    public async Task HideVertex_RemovesVertexAndAllIncidentEdges()
    {
        var f = CreateGraph();

        var hidden = f.Graph.HideVertex("C");

        await Assert.That(hidden).IsTrue();
        await Assert.That(f.Graph.ContainsVertex("C")).IsFalse();
        await Assert.That(f.Graph.IsHiddenVertex("C")).IsTrue();
        await Assert.That(f.Graph.VertexCount).IsEqualTo(3);
        // Only A→B remains; B→C, C→A and C→D are hidden alongside C.
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(1);
        await Assert.That(f.Graph.ContainsEdge(f.AB)).IsTrue();
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(3);
        await Assert.That(f.Graph.IsHiddenEdge(f.BC)).IsTrue();
        await Assert.That(f.Graph.IsHiddenEdge(f.CA)).IsTrue();
        await Assert.That(f.Graph.IsHiddenEdge(f.CD)).IsTrue();
        await Assert.That(f.Graph.HiddenVertexCount).IsEqualTo(1);
        await Assert.That(f.Graph.HiddenVertices.Single()).IsEqualTo("C");
    }

    [Test]
    public async Task HideVertex_RaisesEdgeEventsBeforeVertexEvent()
    {
        var f = CreateGraph();
        var log = new List<string>();
        f.Graph.EdgeHidden += e => log.Add($"E:{e.Source}{e.Target}");
        f.Graph.VertexHidden += v => log.Add($"V:{v}");

        f.Graph.HideVertex("D");

        await Assert.That(log.Count).IsEqualTo(2);
        await Assert.That(log[0]).IsEqualTo("E:CD");
        await Assert.That(log[1]).IsEqualTo("V:D");
    }

    [Test]
    public async Task HideVertex_ReturnsFalse_WhenAlreadyHiddenOrUnknown()
    {
        var f = CreateGraph();
        var events = 0;
        f.Graph.HideVertex("A");
        f.Graph.VertexHidden += _ => events++;

        await Assert.That(f.Graph.HideVertex("A")).IsFalse();
        await Assert.That(f.Graph.HideVertex("Z")).IsFalse();
        await Assert.That(events).IsEqualTo(0);
        await Assert.That(f.Graph.HiddenVertexCount).IsEqualTo(1);
    }

    [Test]
    public async Task HideVertices_And_HideVerticesIf()
    {
        var f = CreateGraph();
        f.Graph.HideVertices(["A", "B"]);
        await Assert.That(f.Graph.HiddenVertexCount).IsEqualTo(2);
        await Assert.That(f.Graph.VertexCount).IsEqualTo(2);

        var g = CreateGraph();
        g.Graph.HideVerticesIf(v => v is "B" or "D", "tag");
        await Assert.That(g.Graph.IsHiddenVertex("B")).IsTrue();
        await Assert.That(g.Graph.IsHiddenVertex("D")).IsTrue();
        await Assert.That(g.Graph.IsHiddenVertex("A")).IsFalse();
    }

    [Test]
    public async Task IsHiddenVertex_FalseForVisibleOrUnknownVertex()
    {
        var f = CreateGraph();
        await Assert.That(f.Graph.IsHiddenVertex("A")).IsFalse();
        await Assert.That(f.Graph.IsHiddenVertex("Z")).IsFalse();
    }

    #endregion

    #region Unhiding vertices

    [Test]
    public async Task UnhideVertex_RestoresVertexButNotEdges()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("C");
        var unhiddenEvents = new List<string>();
        f.Graph.VertexUnhidden += v => unhiddenEvents.Add(v);

        await Assert.That(f.Graph.UnhideVertex("C")).IsTrue();

        await Assert.That(f.Graph.ContainsVertex("C")).IsTrue();
        await Assert.That(f.Graph.IsHiddenVertex("C")).IsFalse();
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(1);
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(3);
        await Assert.That(unhiddenEvents.Single()).IsEqualTo("C");
    }

    [Test]
    public async Task UnhideVertex_ReturnsFalse_WhenNotHidden()
    {
        var f = CreateGraph();
        await Assert.That(f.Graph.UnhideVertex("A")).IsFalse();
        await Assert.That(f.Graph.UnhideVertex("Z")).IsFalse();
    }

    [Test]
    public async Task UnhideVertexAndEdges_RestoresEdgesWhoseOtherEndpointIsVisible()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("C");

        f.Graph.UnhideVertexAndEdges("C");

        await Assert.That(f.Graph.VertexCount).IsEqualTo(4);
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(4);
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(0);
        await Assert.That(f.Graph.HiddenEdgeCountOf("C")).IsEqualTo(0);
    }

    [Test]
    public async Task UnhideVertexAndEdges_KeepsEdgesToStillHiddenVerticesHidden()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("D");
        f.Graph.HideVertex("C");

        f.Graph.UnhideVertexAndEdges("C");

        // C→D cannot be restored while D is still hidden.
        await Assert.That(f.Graph.IsHiddenEdge(f.CD)).IsTrue();
        await Assert.That(f.Graph.ContainsEdge(f.BC)).IsTrue();
        await Assert.That(f.Graph.ContainsEdge(f.CA)).IsTrue();
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(1);
    }

    #endregion

    #region Hiding / unhiding edges

    [Test]
    public async Task HideEdge_TracksEdgePerEndpoint()
    {
        var f = CreateGraph();
        var hiddenEvents = 0;
        f.Graph.EdgeHidden += _ => hiddenEvents++;

        await Assert.That(f.Graph.HideEdge(f.AB)).IsTrue();
        await Assert.That(f.Graph.HideEdge(f.AB)).IsFalse();

        await Assert.That(hiddenEvents).IsEqualTo(1);
        await Assert.That(f.Graph.ContainsEdge(f.AB)).IsFalse();
        await Assert.That(f.Graph.ContainsVertex("A")).IsTrue();
        await Assert.That(f.Graph.HiddenEdgeCountOf("A")).IsEqualTo(1);
        await Assert.That(f.Graph.HiddenEdgeCountOf("B")).IsEqualTo(1);
        await Assert.That(f.Graph.HiddenEdgesOf("A").Single()).IsSameReferenceAs(f.AB);
        await Assert.That(f.Graph.HiddenEdgeCountOf("D")).IsEqualTo(0);
    }

    [Test]
    public async Task HideEdge_ReturnsFalse_ForEdgeNotInGraph()
    {
        var f = CreateGraph();
        await Assert.That(f.Graph.HideEdge(new Edge<string>("A", "D"))).IsFalse();
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(0);
    }

    [Test]
    public async Task HiddenEdgesOf_UnknownVertex_IsEmpty_AndDoesNotAffectLaterHides()
    {
        // Querying an unknown vertex must be a pure read.
        var f = CreateGraph();
        await Assert.That(f.Graph.HiddenEdgesOf("Z")).IsEmpty();
        await Assert.That(f.Graph.HiddenEdgeCountOf("Z")).IsEqualTo(0);
        await Assert.That(f.Graph.HiddenEdgesOf("A")).IsEmpty();

        f.Graph.HideEdge(f.AB);
        await Assert.That(f.Graph.HiddenEdgeCountOf("A")).IsEqualTo(1);
        await Assert.That(f.Graph.HiddenEdgeCountOf("Z")).IsEqualTo(0);
    }

    [Test]
    public async Task UnhideEdge_RestoresEdgeAndRaisesEvent()
    {
        var f = CreateGraph();
        f.Graph.HideEdge(f.CD);
        Edge<string>? unhidden = null;
        f.Graph.EdgeUnhidden += e => unhidden = e;

        await Assert.That(f.Graph.UnhideEdge(f.CD)).IsTrue();

        await Assert.That(f.Graph.ContainsEdge(f.CD)).IsTrue();
        await Assert.That(f.Graph.IsHiddenEdge(f.CD)).IsFalse();
        await Assert.That(f.Graph.HiddenEdgeCountOf("C")).IsEqualTo(0);
        await Assert.That(f.Graph.HiddenEdgeCountOf("D")).IsEqualTo(0);
        await Assert.That(unhidden).IsSameReferenceAs(f.CD);
    }

    [Test]
    public async Task UnhideEdge_Fails_WhileAnEndpointIsHidden()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("D");

        await Assert.That(f.Graph.UnhideEdge(f.CD)).IsFalse();
        await Assert.That(f.Graph.IsHiddenEdge(f.CD)).IsTrue();
    }

    [Test]
    public async Task UnhideEdge_ReturnsFalse_ForVisibleEdge()
    {
        var f = CreateGraph();
        await Assert.That(f.Graph.UnhideEdge(f.AB)).IsFalse();
    }

    [Test]
    public async Task HideEdgesIf_And_UnhideEdgesIf_FilterByPredicate()
    {
        var f = CreateGraph();
        f.Graph.HideEdgesIf(e => e.Source == "C", "fromC");
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(2);
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(2);

        f.Graph.UnhideEdgesIf(e => e.Target == "D");
        await Assert.That(f.Graph.ContainsEdge(f.CD)).IsTrue();
        await Assert.That(f.Graph.IsHiddenEdge(f.CA)).IsTrue();
    }

    [Test]
    public async Task HideEdges_And_UnhideEdges_Bulk()
    {
        var f = CreateGraph();
        f.Graph.HideEdges([f.AB, f.BC]);
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(2);

        f.Graph.UnhideEdges(f.Graph.HiddenEdges);
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(0);
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(4);
    }

    #endregion

    #region Tags

    [Test]
    public async Task Unhide_ByTag_RestoresOnlyElementsHiddenUnderThatTag()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("D", "groupD");
        f.Graph.HideEdge(f.AB, "edgeTag");

        var removed = f.Graph.Unhide("groupD");

        await Assert.That(removed).IsTrue();
        await Assert.That(f.Graph.ContainsVertex("D")).IsTrue();
        // The incident edge C→D was recorded under the same tag and is restored too.
        await Assert.That(f.Graph.ContainsEdge(f.CD)).IsTrue();
        // Elements hidden under a different tag are untouched.
        await Assert.That(f.Graph.IsHiddenEdge(f.AB)).IsTrue();
    }

    [Test]
    public async Task Unhide_ByTag_RestoresVerticesBeforeEdges()
    {
        var f = CreateGraph();
        // Hiding C under a tag records C plus its three incident edges.
        f.Graph.HideVertex("C", "c");

        f.Graph.Unhide("c");

        await Assert.That(f.Graph.VertexCount).IsEqualTo(4);
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(4);
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(0);
    }

    [Test]
    public async Task Unhide_UnknownTag_ReturnsFalse_AndChangesNothing()
    {
        var f = CreateGraph();
        f.Graph.HideEdge(f.AB);
        await Assert.That(f.Graph.Unhide("does-not-exist")).IsFalse();
        await Assert.That(f.Graph.IsHiddenEdge(f.AB)).IsTrue();
    }

    #endregion

    #region UnhideAll

    [Test]
    public async Task UnhideAll_RestoresEverything()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("C");
        f.Graph.HideEdge(f.AB);
        f.Graph.HideVertex("D");

        await Assert.That(f.Graph.UnhideAll()).IsTrue();

        await Assert.That(f.Graph.VertexCount).IsEqualTo(4);
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(4);
        await Assert.That(f.Graph.HiddenVertexCount).IsEqualTo(0);
        await Assert.That(f.Graph.HiddenEdgeCount).IsEqualTo(0);
        foreach (var v in new[] { "A", "B", "C", "D" })
            await Assert.That(f.Graph.HiddenEdgeCountOf(v)).IsEqualTo(0);
    }

    [Test]
    public async Task UnhideAll_OnCleanGraph_IsNoOp()
    {
        var f = CreateGraph();
        await Assert.That(f.Graph.UnhideAll()).IsTrue();
        await Assert.That(f.Graph.VertexCount).IsEqualTo(4);
        await Assert.That(f.Graph.EdgeCount).IsEqualTo(4);
    }

    [Test]
    public async Task UnhideAll_ReturnsFalse_WhenAnEdgeCannotBeUnhidden()
    {
        var f = CreateGraph();
        f.Graph.HideEdge(f.AB);
        f.Graph.HideEdge(f.CD);
        // Re-adding the hidden edge directly makes UnhideEdge fail for it (it is no longer "hidden").
        f.Graph.AddEdge(f.AB);

        // Must terminate rather than retrying the failing edge forever.
        await Assert.That(f.Graph.UnhideAll()).IsFalse();

        // The other hidden edge is still restored.
        await Assert.That(f.Graph.ContainsEdge(f.CD)).IsTrue();
        await Assert.That(f.Graph.IsHiddenEdge(f.CD)).IsFalse();
    }

    [Test]
    public async Task UnhideAll_ReturnsFalse_WhenAVertexCannotBeUnhidden()
    {
        var f = CreateGraph();
        f.Graph.HideVertex("D");
        // Re-adding the hidden vertex directly makes UnhideVertex fail for it.
        f.Graph.AddVertex("D");

        await Assert.That(f.Graph.UnhideAll()).IsFalse();
    }

    #endregion
}
