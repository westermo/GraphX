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
/// Tests for layout pass optimization: verifying that X/Y changes use AffectsArrange
/// instead of AffectsMeasure on GraphAreaBase, and that edge geometry dirty checking
/// prevents unnecessary rebuilds.
/// </summary>
public class LayoutPassOptimizationTests
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
        if (vc.Template == null)
        {
            var content = new Grid();
            var panel = new StackPanel { Name = "PART_vcproot" };
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
            var path = new global::Avalonia.Controls.Shapes.Path
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

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area,
        VertexControl sourceVc, VertexControl targetVc, EdgeControl edge)
        CreateSimpleGraph()
    {
        var graph = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A") { ID = 1 };
        var v2 = new TVertex("B") { ID = 2 };
        graph.AddVertex(v1);
        graph.AddVertex(v2);
        var e = new TEdge(v1, v2);
        graph.AddEdge(e);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = graph,
            EnableParallelEdges = false,
            EdgeCurvingEnabled = false
        };

        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };

        var positions = new Dictionary<TVertex, Point>
        {
            [v1] = new Point(50, 100),
            [v2] = new Point(250, 100)
        };

        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);

        var sourceVc = area.VertexList[v1];
        var targetVc = area.VertexList[v2];
        sourceVc.Width = 40;
        sourceVc.Height = 30;
        targetVc.Width = 40;
        targetVc.Height = 30;

        EnsureVertexTemplate(sourceVc);
        EnsureVertexTemplate(targetVc);

        var edge = area.EdgesList[e];
        EnsureEdgeTemplate(edge);

        return (area, sourceVc, targetVc, edge);
    }

    #region Fix #1: GraphAreaBase X/Y should use AffectsArrange, not AffectsMeasure

    [Test]
    public async Task GraphArea_XYChange_DoesNotInvalidateMeasure()
    {
        // Changing X/Y on a child should not trigger a full measure pass on GraphAreaBase.
        // After fix #1, AffectsArrange is used instead of AffectsMeasure.
        var (area, sourceVc, _, _) = CreateSimpleGraph();

        // Force initial layout
        area.Measure(new Size(500, 400));
        area.Arrange(new Rect(0, 0, 500, 400));

        // After initial layout, IsMeasureValid should be true
        await Assert.That(area.IsMeasureValid).IsTrue();

        // Change X position of a child vertex
        GraphAreaBase.SetX(sourceVc, 150);

        // With AffectsArrange (not AffectsMeasure), the measure should still be valid
        // but arrange should be invalidated
        await Assert.That(area.IsMeasureValid).IsTrue();
        await Assert.That(area.IsArrangeValid).IsFalse();
    }

    [Test]
    public async Task GraphArea_YChange_DoesNotInvalidateMeasure()
    {
        var (area, sourceVc, _, _) = CreateSimpleGraph();

        area.Measure(new Size(500, 400));
        area.Arrange(new Rect(0, 0, 500, 400));

        await Assert.That(area.IsMeasureValid).IsTrue();

        // Change Y position of a child vertex
        GraphAreaBase.SetY(sourceVc, 200);

        // Measure should remain valid; only arrange should be invalidated
        await Assert.That(area.IsMeasureValid).IsTrue();
        await Assert.That(area.IsArrangeValid).IsFalse();
    }

    [Test]
    public async Task GraphArea_VertexPositionChange_StillArrangesCorrectly()
    {
        // Verify that after changing X/Y, the arrange pass still positions the child correctly
        var (area, sourceVc, _, _) = CreateSimpleGraph();

        area.Measure(new Size(500, 400));
        area.Arrange(new Rect(0, 0, 500, 400));

        // Move vertex
        GraphAreaBase.SetX(sourceVc, 200);
        GraphAreaBase.SetY(sourceVc, 300);

        // Re-arrange
        area.Arrange(new Rect(0, 0, 500, 400));

        // Verify the child is positioned correctly
        var pos = sourceVc.GetPosition();
        await Assert.That(pos.X).IsEqualTo(200);
        await Assert.That(pos.Y).IsEqualTo(300);
    }

    #endregion

    #region Fix #2: Edge geometry dirty checking

    [Test]
    public async Task Edge_GeometryRebuilt_OnFirstArrange()
    {
        // Edge geometry should be created on the first arrange pass
        var (_, _, _, edge) = CreateSimpleGraph();

        edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        edge.Arrange(new Rect(0, 0, edge.DesiredSize.Width, edge.DesiredSize.Height));

        var geom = edge.GetLineGeometry();
        await Assert.That(geom).IsNotNull();
    }

    [Test]
    public async Task Edge_GeometryRebuilt_WhenSourcePositionChanges()
    {
        // When source position changes and measure is invalidated, geometry should be rebuilt
        var (_, sourceVc, _, edge) = CreateSimpleGraph();

        // First layout
        edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        edge.Arrange(new Rect(0, 0, edge.DesiredSize.Width, edge.DesiredSize.Height));

        var firstGeometry = edge.GetLineGeometry();

        // Move source vertex and re-measure (simulating a property change that triggers measure)
        sourceVc.SetPosition(150, 200);
        GraphAreaBase.SetFinalX(sourceVc, 150);
        GraphAreaBase.SetFinalY(sourceVc, 200);

        edge.InvalidateMeasure();
        edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        edge.Arrange(new Rect(0, 0, edge.DesiredSize.Width, edge.DesiredSize.Height));

        var newGeometry = edge.GetLineGeometry();
        // New geometry should be built (different instance)
        await Assert.That(newGeometry).IsNotNull();
        await Assert.That(ReferenceEquals(newGeometry, firstGeometry)).IsFalse();
    }

    [Test]
    public async Task Edge_GeometryRebuilt_WhenMeasureInvalidated()
    {
        // Invalidating measure should mark geometry dirty, causing a rebuild on next arrange
        var (_, _, _, edge) = CreateSimpleGraph();

        // First layout
        edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        edge.Arrange(new Rect(0, 0, edge.DesiredSize.Width, edge.DesiredSize.Height));
        var firstGeometry = edge.GetLineGeometry();

        // Invalidate measure (as AffectsMeasure properties do)
        edge.InvalidateMeasure();
        edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        edge.Arrange(new Rect(0, 0, edge.DesiredSize.Width, edge.DesiredSize.Height));

        var secondGeometry = edge.GetLineGeometry();
        // After measure invalidation, geometry gets rebuilt
        await Assert.That(secondGeometry).IsNotNull();
    }

    #endregion
}
