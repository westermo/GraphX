using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class VertexGraphAreaTests
{
    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; } = null;
    }

    private static void EnsureVertexTemplate(VertexControl vc)
    {
        if (vc.Template == null)
        {
            var content = new Grid();
            var panel = new StackPanel()
            {
                Name = "PART_vcproot",
            };
            content.Children.Add(panel);
            var ns = new NameScope();
            ns.Register("PART_vcproot", panel);
            var functor =
                new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
            var template = new ControlTemplate
            {
                TargetType = typeof(VertexControl),
                Content = functor
            };
            vc.Template = template;
        }

        vc.ApplyTemplate();
    }

    private static void EnsureEdgeTemplate(EdgeControl ec)
    {
        if (ec.Template == null)
        {
            var content = new Grid();
            var path = new global::Avalonia.Controls.Shapes.Path()
            {
                Name = "PART_edgePath",
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            content.Children.Add(path);
            var ns = new NameScope();
            ns.Register("PART_edgePath", path);
            var functor =
                new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
            ec.Template = new ControlTemplate()
            {
                TargetType = typeof(EdgeControl),
                Content = functor
            };
        }

        ec.ApplyTemplate();
    }

    private GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> CreateArea(int edges = 1)
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        for (int i = 0; i < edges; i++) g.AddEdge(new TEdge(v1, v2));

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();
        // Assign positions & template
        foreach (var kv in area.VertexList)
        {
            kv.Value.Width = 40;
            kv.Value.Height = 30;
            kv.Value.SetPosition(kv.Key == v1 ? 50 : 200, 80);
            GraphAreaBase.SetFinalX(kv.Value, kv.Value.GetPosition().X);
            GraphAreaBase.SetFinalY(kv.Value, kv.Value.GetPosition().Y);
            EnsureVertexTemplate(kv.Value);
        }

        // create edges manually
        foreach (var e in g.Edges)
        {
            var ec = area.ControlFactory.CreateEdgeControl(area.VertexList[e.Source], area.VertexList[e.Target], e);
            EnsureEdgeTemplate(ec);
            area.AddEdge(e, ec);
            ec.UpdateEdge(true);
        }

        return area;
    }

    [Test]
    public async Task Vertex_SetPosition_UpdatesGetPosition()
    {
        var area = CreateArea();
        var vc = area.VertexList.Values.First();
        vc.SetPosition(123, 321);
        var pos = vc.GetPosition();
        await Assert.That(pos.X).IsEqualTo(123);
        await Assert.That(pos.Y).IsEqualTo(321);
    }

    [Test]
    public async Task Vertex_HideWithEdges_HidesConnectedEdge()
    {
        var area = CreateArea();
        var edge = area.EdgesList.Values.First();
        var v1 = area.VertexList.Values.First();
        await Assert.That(edge.IsVisible).IsTrue();
        v1.HideWithEdges();
        await Assert.That(v1.IsVisible).IsFalse();
        await Assert.That(edge.IsVisible).IsFalse();
        v1.ShowWithEdges();
        await Assert.That(v1.IsVisible).IsTrue();
    }

    [Test]
    public async Task GraphArea_GetRelatedControls_ForVertex_ReturnsEdge()
    {
        var area = CreateArea();
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
        EnsureVertexTemplate(vc1);
        EnsureVertexTemplate(vc2);
        area.AddVertexAndData(v1, vc1);
        area.AddVertexAndData(v2, vc2);
        await Assert.That(area.VertexList.Count).IsEqualTo(2);
        var e = new TEdge(v1, v2);
        var ec = new EdgeControl(vc1, vc2, e);
        EnsureEdgeTemplate(ec);
        area.AddEdgeAndData(e, ec);
        await Assert.That(area.EdgesList.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Vertex_LabelVisibility_Toggles()
    {
        var area = CreateArea();
        var vc = area.VertexList.Values.First();
        // Ensure template to attach (no actual label control so just check property changes don't throw)
        vc.ShowLabel = true;
        await Assert.That(vc.ShowLabel).IsTrue();
        vc.ShowLabel = false;
        await Assert.That(vc.ShowLabel).IsFalse();
    }
}