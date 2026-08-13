using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.EdgeLabels;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public sealed class BatchedEdgeRenderingTests
{
    private sealed class TestVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
    }

    private sealed class TestEdge(TestVertex source, TestVertex target) : EdgeBase<TestVertex>(source, target)
    {
        public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; }
    }

    [Test]
    public async Task EdgeRenderingMode_DefaultsToStandard()
    {
        var graphArea = CreateGraphArea(out _);

        await Assert.That(graphArea.EdgeRenderingMode).IsEqualTo(EdgeRenderingMode.Standard);
        await Assert.That(graphArea.Children.OfType<BatchedEdgeLayer>()).IsEmpty();
    }

    [Test]
    public async Task BatchedMode_HidesOnlyEligibleDefaultEdgesAndPlacesLayerBelowGraphVisuals()
    {
        var graphArea = CreateGraphArea(out var edge);
        edge.ShowArrows = false;
        UpdateEdgeGeometry(edge);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        await Assert.That(edge.IsBatchedPathSuppressed).IsTrue();
        await Assert.That(graphArea.Children[0]).IsTypeOf<BatchedEdgeLayer>();

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Standard;

        await Assert.That(edge.IsBatchedPathSuppressed).IsFalse();
    }

    [Test]
    public async Task BatchedMode_FallsBackToIndividualVisualForManualOrCustomTemplateEdges()
    {
        var graphArea = CreateGraphArea(out var edge);
        edge.ShowArrows = false;
        edge.ManualDrawing = true;
        UpdateEdgeGeometry(edge);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        await Assert.That(edge.IsBatchedPathSuppressed).IsFalse();

        edge.ManualDrawing = false;
        edge.Template = CreateCustomEdgeTemplate();
        edge.ApplyTemplate();

        await Assert.That(edge.IsBatchedPathSuppressed).IsFalse();
    }

    [Test]
    public async Task BatchedMode_FallsBackForSelectedAndSelfLoopEdges()
    {
        var graphArea = CreateGraphArea(out var edge);
        edge.ShowArrows = false;
        edge.IsSelected = true;
        UpdateEdgeGeometry(edge);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        await Assert.That(edge.IsBatchedPathSuppressed).IsFalse();

        graphArea = CreateGraphArea(out edge, selfLoop: true);
        edge.ShowArrows = false;
        UpdateEdgeGeometry(edge);
        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        await Assert.That(edge.IsBatchedPathSuppressed).IsFalse();
    }

    [Test]
    public async Task BatchedMode_FallsBackForArrowAndLabelEdges()
    {
        var graphArea = CreateGraphArea(out var edge);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        await Assert.That(edge.Opacity).IsEqualTo(1d);

        graphArea = CreateGraphArea(out edge);
        edge.ShowArrows = false;
        new AttachableEdgeLabelControl().Attach(edge);
        UpdateEdgeGeometry(edge);
        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        await Assert.That(edge.Opacity).IsEqualTo(1d);
    }

    [Test]
    public async Task BatchedMode_InvalidatesLayerWhenAutomaticEdgeGeometryChanges()
    {
        var graphArea = CreateGraphArea(out var edge);
        edge.ShowArrows = false;
        UpdateEdgeGeometry(edge);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;
        var layer = (BatchedEdgeLayer)graphArea.Children[0];
        var refreshCount = layer.RefreshCount;
        var initialGeometry = edge.GetLineGeometry();
        ((TestEdge)edge.Edge!).RoutingPoints = [new(140, 120)];

        edge.InvalidateMeasure();
        UpdateEdgeGeometry(edge);

        await Assert.That(edge.GetLineGeometry()).IsNotSameReferenceAs(initialGeometry);
        await Assert.That(layer.RefreshCount).IsGreaterThan(refreshCount);
    }

    [Test]
    public async Task BatchedMode_PreservesApplicationOpacity()
    {
        var graphArea = CreateGraphArea(out var edge);
        edge.ShowArrows = false;
        edge.Opacity = 0.4;
        UpdateEdgeGeometry(edge);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;
        await Assert.That(edge.Opacity).IsEqualTo(0.4);

        edge.Opacity = 0.2;
        await Assert.That(edge.Opacity).IsEqualTo(0.2);

        graphArea.EdgeRenderingMode = EdgeRenderingMode.Standard;
        await Assert.That(edge.Opacity).IsEqualTo(0.2);
    }

    [Test]
    public async Task BatchedMode_RecreatesLayerAfterClearLayout()
    {
        var graphArea = CreateGraphArea(out var edge);
        edge.ShowArrows = false;
        UpdateEdgeGeometry(edge);
        graphArea.EdgeRenderingMode = EdgeRenderingMode.Batched;

        graphArea.ClearLayout();

        await Assert.That(graphArea.Children.OfType<BatchedEdgeLayer>().Count()).IsEqualTo(1);
    }

    private static GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>> CreateGraphArea(
        out EdgeControl edgeControl, bool selfLoop = false)
    {
        var source = new TestVertex("source") { ID = 1 };
        var target = selfLoop ? source : new TestVertex("target") { ID = 2 };
        var edge = new TestEdge(source, target);
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        graph.AddVertex(source);
        if (!selfLoop) graph.AddVertex(target);
        graph.AddEdge(edge);

        var graphArea = new GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>
        {
            Width = 500,
            Height = 400,
            EdgeLabelFactory = null,
            LogicCore = new GXLogicCore<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>
            {
                Graph = graph
            }
        };

        var positions = new Dictionary<TestVertex, Point>
        {
            [source] = new(40, 40)
        };
        if (!selfLoop)
            positions[target] = new(240, 40);
        graphArea.PreloadGraph(positions);

        foreach (var vertex in graphArea.VertexList.Values)
        {
            vertex.Width = 40;
            vertex.Height = 30;
        }

        graphArea.UpdateLayout();
        edgeControl = graphArea.EdgesList[edge];
        edgeControl.Template = CreateDefaultCompatibleEdgeTemplate();
        edgeControl.ApplyTemplate();
        UpdateEdgeGeometry(edgeControl);
        return graphArea;
    }

    private static void UpdateEdgeGeometry(EdgeControl edge)
    {
        edge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        edge.Arrange(new Rect(0, 0, Math.Max(1, edge.DesiredSize.Width), Math.Max(1, edge.DesiredSize.Height)));
    }

    private static ControlTemplate CreateDefaultCompatibleEdgeTemplate()
    {
        var content = new Grid();
        var path = new global::Avalonia.Controls.Shapes.Path { Name = "PART_edgePath" };
        path.Classes.Add("graphx-default-edge-path");
        content.Children.Add(path);
        var nameScope = new NameScope();
        nameScope.Register(path.Name!, path);

        return new ControlTemplate
        {
            TargetType = typeof(EdgeControl),
            Content = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, nameScope))
        };
    }

    private static ControlTemplate CreateCustomEdgeTemplate()
    {
        var content = new Grid();
        var path = new global::Avalonia.Controls.Shapes.Path { Name = "PART_edgePath" };
        content.Children.Add(path);
        var nameScope = new NameScope();
        nameScope.Register(path.Name!, path);

        return new ControlTemplate
        {
            TargetType = typeof(EdgeControl),
            Content = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, nameScope))
        };
    }
}
