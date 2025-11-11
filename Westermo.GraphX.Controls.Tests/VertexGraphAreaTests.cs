using System.Windows;
using QuikGraph;
using TUnit.Core.Executors;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

[TestExecutor<STAThreadExecutor>]
public class VertexGraphAreaTests
{
    [Test]
    public async Task Vertex_SetPosition_UpdatesGetPosition()
    {
        var area = BaseHelpers.CreateArea();
        var vc = area.VertexList.Values.First();
        vc.SetPosition(123, 321);
        var pos = vc.GetPosition();
        await Assert.That(pos.X).IsEqualTo(123);
        await Assert.That(pos.Y).IsEqualTo(321);
    }

    [Test]
    public async Task Vertex_HideWithEdges_HidesConnectedEdge()
    {
        var area = BaseHelpers.CreateArea();
        var edge = area.EdgesList.Values.First();
        var v1 = area.VertexList.Values.First();
        await Assert.That(edge.Visibility).IsEqualTo(Visibility.Visible);
        v1.HideWithEdges();
        await Assert.That(v1.Visibility).IsEqualTo(Visibility.Collapsed);
        await Assert.That(edge.Visibility).IsEqualTo(Visibility.Collapsed);
        v1.ShowWithEdges();
        await Assert.That(v1.Visibility).IsEqualTo(Visibility.Visible);
    }

    [Test]
    public async Task GraphArea_GetRelatedControls_ForVertex_ReturnsEdge()
    {
        var area = BaseHelpers.CreateArea();
        var v1 = area.VertexList.Values.First();
        var related = area.GetRelatedControls(v1, GraphControlType.Edge, EdgesType.All);
        await Assert.That(related.Count).IsGreaterThan(0);
        await Assert.That(related.Any(r => r is EdgeControl)).IsTrue();
    }

    [Test]
    public async Task GraphArea_AddVertexAndEdge_IncreasesCollections()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        var vc1 = new VertexControl(v1);
        var vc2 = new VertexControl(v2);
        BaseHelpers.EnsureVertexTemplate(vc1);
        BaseHelpers.EnsureVertexTemplate(vc2);
        area.AddVertexAndData(v1, vc1);
        area.AddVertexAndData(v2, vc2);
        await Assert.That(area.VertexList.Count).IsEqualTo(2);
        var e = new TEdge(v1, v2);
        var ec = new EdgeControl(vc1, vc2, e);
        BaseHelpers.EnsureEdgeTemplate(ec);
        area.AddEdgeAndData(e, ec);
        await Assert.That(area.EdgesList.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Vertex_LabelVisibility_Toggles()
    {
        var area = BaseHelpers.CreateArea();
        var vc = area.VertexList.Values.First();
        // Ensure template to attach (no actual label control so just check property changes don't throw)
        vc.ShowLabel = true;
        await Assert.That(vc.ShowLabel).IsTrue();
        vc.ShowLabel = false;
        await Assert.That(vc.ShowLabel).IsFalse();
    }
}