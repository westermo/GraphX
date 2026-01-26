using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class EdgeControlAdvancedTests
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
            var panel = new StackPanel() { Name = "PART_vcproot" };
            content.Children.Add(panel);
            var ns = new NameScope();
            ns.Register("PART_vcproot", panel);
            var functor = new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
            vc.Template = new ControlTemplate
            {
                TargetType = typeof(VertexControl),
                Content = functor
            };
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
            var functor = new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
            ec.Template = new ControlTemplate
            {
                TargetType = typeof(EdgeControl),
                Content = functor
            };
        }

        ec.ApplyTemplate();
    }

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, VertexControl v1c, VertexControl v2c,
        TEdge e, EdgeControl ec)
        CreateSimpleArea(bool curving = false)
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var e = new TEdge(v1, v2);
        g.AddEdge(e);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            EdgeCurvingEnabled = curving
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
            { LogicCore = lc, Width = 500, Height = 400 };
        area.PreloadVertexes();

        var v1c = area.VertexList[v1];
        var v2c = area.VertexList[v2];
        v1c.Width = 40;
        v1c.Height = 30;
        v1c.SetPosition(50, 80);
        v2c.Width = 40;
        v2c.Height = 30;
        v2c.SetPosition(250, 80);
        GraphAreaBase.SetFinalX(v1c, 50);
        GraphAreaBase.SetFinalY(v1c, 80);
        GraphAreaBase.SetFinalX(v2c, 250);
        GraphAreaBase.SetFinalY(v2c, 80);
        EnsureVertexTemplate(v1c);
        EnsureVertexTemplate(v2c);

        var ec = area.ControlFactory.CreateEdgeControl(v1c, v2c, e);
        EnsureEdgeTemplate(ec);
        area.AddEdge(e, ec);
        ec.UpdateEdge(true);
        return (area, v1c, v2c, e, ec);
    }

    // [Test]
    // public async Task Edge_UpdateAfterVertexMove_ChangesEndpoints()
    // {
    //     var data = CreateSimpleArea();
    //     var ec = data.ec;
    //     var originalSource = ec.SourceEndpoint;
    //     var originalTarget = ec.TargetEndpoint;
    //     await Assert.That(originalSource.HasValue && originalTarget.HasValue).IsTrue();
    //
    //     // move target vertex to the right and update
    //     data.v2c.SetPosition(350, 120);
    //     GraphAreaBase.SetFinalX(data.v2c, 350);
    //     GraphAreaBase.SetFinalY(data.v2c, 120);
    //     ec.UpdateEdge(true);
    //
    //     var movedSource = ec.SourceEndpoint;
    //     var movedTarget = ec.TargetEndpoint;
    //     await Assert.That(movedTarget!.Value.X).IsGreaterThan(originalTarget!.Value.X);
    //     await Assert.That(movedTarget.Value.Y).IsNotEqualTo(originalTarget.Value.Y);
    //     // Source endpoint direction should also change slightly due to angle change
    //     await Assert.That(movedSource!.Value.Y).IsNotEqualTo(originalSource!.Value.Y);
    // }
    //
    // [Test]
    // public async Task Edge_ManualRoutingPoints_IncreasePathSegments()
    // {
    //     var (_, _, _, e, ec) = CreateSimpleArea();
    //     // baseline geometry
    //     var baseGeom = ec.GetLineGeometry() as PathGeometry;
    //     await Assert.That(baseGeom).IsNotNull();
    //     var baseSegCount = baseGeom!.Figures[0].Segments.Count;
    //
    //     // assign routing points to create a polyline with bends
    //     e.RoutingPoints =
    //     [
    //         new Westermo.GraphX.Measure.Point(150, 20),
    //         new Westermo.GraphX.Measure.Point(150, 30),
    //         new Westermo.GraphX.Measure.Point(200, 160)
    //     ];
    //     ec.UpdateEdge(true);
    //     var routedGeom = ec.GetLineGeometry() as PathGeometry;
    //     await Assert.That(routedGeom).IsNotNull();
    //     var routedSegCount = routedGeom!.Figures[0].Segments.Count;
    //     await Assert.That(routedSegCount).IsGreaterThan(baseSegCount);
    // }

    [Test]
    public async Task Edge_CurvingEnabled_StillProducesGeometry()
    {
        var data =  CreateSimpleArea(curving: true);
        var geom = data.ec.GetLineGeometry();
        // We don't assert exact segment types; just ensure geometry exists under curving mode
        await Assert.That(geom).IsNotNull();
    }
}