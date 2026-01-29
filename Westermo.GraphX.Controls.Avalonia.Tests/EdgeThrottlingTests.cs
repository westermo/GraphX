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
/// Tests for edge update throttling functionality during drag operations.
/// </summary>
public class EdgeThrottlingTests
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

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, EdgeControl edge) CreateSimpleGraph()
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
            EnableParallelEdges = false
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

        foreach (var vc in area.VertexList.Values)
        {
            vc.Width = 40;
            vc.Height = 30;
            EnsureVertexTemplate(vc);
        }

        var edge = (EdgeControl)area.EdgesList[e];
        EnsureEdgeTemplate(edge);

        return (area, edge);
    }

    [Test]
    public async Task UpdateThrottleMs_CanBeChanged()
    {
        var (_, edge) = CreateSimpleGraph();
        
        edge.UpdateThrottleMs = 32;
        
        await Assert.That(edge.UpdateThrottleMs).IsEqualTo(32);
    }

    [Test]
    public async Task UpdateThrottleMs_CanBeDisabled()
    {
        var (_, edge) = CreateSimpleGraph();
        
        edge.UpdateThrottleMs = 0;
        
        await Assert.That(edge.UpdateThrottleMs).IsEqualTo(0);
    }

    [Test]
    public async Task UpdateThrottleMs_NegativeValuesTreatedAsZero()
    {
        var (_, edge) = CreateSimpleGraph();
        
        edge.UpdateThrottleMs = -10;
        
        // Negative values should be clamped or treated as disabled
        await Assert.That(edge.UpdateThrottleMs).IsLessThanOrEqualTo(0);
    }

    [Test]
    public async Task EdgeControl_UpdatesImmediatelyWhenThrottleDisabled()
    {
        var (area, edge) = CreateSimpleGraph();
        edge.UpdateThrottleMs = 0;
        
        var v1 = area.LogicCore!.Graph!.Vertices.First();
        var vc = area.VertexList[v1];
        
        // Get initial geometry bounds
        edge.UpdateEdge();
        var initialBounds = edge.GeometryBounds;
        
        // Move vertex
        vc.SetPosition(new Point(100, 200));
        edge.UpdateEdge();
        
        var newBounds = edge.GeometryBounds;
        
        // Bounds should have changed
        await Assert.That(newBounds).IsNotNull();
        await Assert.That(newBounds).IsNotEqualTo(initialBounds);
    }

    [Test]
    public async Task EdgeControl_CanUpdateWithHighThrottle()
    {
        var (area, edge) = CreateSimpleGraph();
        edge.UpdateThrottleMs = 1000; // Very high throttle
        
        // Should still be able to force update
        edge.UpdateEdge();
        
        await Assert.That(edge.GeometryBounds).IsNotNull();
    }
}
