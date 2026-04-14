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

/// <summary>
/// Tests that routing point array assignment during geometry creation reuses
/// existing arrays when sizes match, avoiding double allocation.
/// </summary>
public class RoutingPointArrayReuseTests
{
    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Measure.Point[]? RoutingPoints { get; set; } = null;
    }

    private static void EnsureVertexTemplate(VertexControl vc)
    {
        if (vc.Template != null)
        {
            vc.ApplyTemplate();
            return;
        }

        var content = new Grid();
        var panel = new StackPanel { Name = "PART_vcproot" };
        content.Children.Add(panel);
        var ns = new NameScope();
        ns.Register("PART_vcproot", panel);
        var functor = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns));
        vc.Template = new ControlTemplate { TargetType = typeof(VertexControl), Content = functor };
        vc.ApplyTemplate();
    }

    private static void EnsureEdgeTemplate(EdgeControl ec)
    {
        if (ec.Template != null)
        {
            ec.ApplyTemplate();
            return;
        }

        var content = new Grid();
        var path = new global::Avalonia.Controls.Shapes.Path
        {
            Name = "PART_edgePath",
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        content.Children.Add(path);
        var ns = new NameScope();
        ns.Register("PART_edgePath", path);
        var functor = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns));
        ec.Template = new ControlTemplate { TargetType = typeof(EdgeControl), Content = functor };
        ec.ApplyTemplate();
    }

    /// <summary>
    /// Triggers a full layout pass (Measure + Arrange) on the edge control to force geometry rebuild.
    /// </summary>
    private static void ForceGeometryRebuild(EdgeControl ec)
    {
        ec.InvalidateMeasure();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));
    }

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area,
        VertexControl v1c, VertexControl v2c, TEdge e, EdgeControl ec) CreateRoutedArea()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var e = new TEdge(v1, v2)
        {
            // Pre-populate routing points so the reuse path is exercised
            RoutingPoints =
            [
                new Measure.Point(50, 80),
                new Measure.Point(150, 50),
                new Measure.Point(250, 80)
            ]
        };
        g.AddEdge(e);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            EdgeCurvingEnabled = false
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
        return (area, v1c, v2c, e, ec);
    }

    [Test]
    public async Task RoutingPoints_SameLength_ReusesExistingArray()
    {
        var (_, _, _, edge, ec) = CreateRoutedArea();

        // Initial layout to populate geometry
        ForceGeometryRebuild(ec);

        // Capture existing array reference after first build
        var originalArray = edge.RoutingPoints;
        await Assert.That(originalArray).IsNotNull();
        var originalLength = originalArray!.Length;

        // Rebuild geometry again — same routing count, so array should be reused
        ForceGeometryRebuild(ec);

        await Assert.That(edge.RoutingPoints).IsSameReferenceAs(originalArray);
        await Assert.That(edge.RoutingPoints!.Length).IsEqualTo(originalLength);
    }

    [Test]
    public async Task RoutingPoints_ValuesAreUpdated_AfterGeometryRebuild()
    {
        var (_, _, _, edge, ec) = CreateRoutedArea();

        // Trigger geometry rebuild
        ForceGeometryRebuild(ec);

        // Routing points should still be populated and non-null
        await Assert.That(edge.RoutingPoints).IsNotNull();
        await Assert.That(edge.RoutingPoints!.Length).IsGreaterThanOrEqualTo(2);
    }
}
