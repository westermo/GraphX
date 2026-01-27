using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for edge geometry caching functionality.
/// </summary>
public class GeometryCachingTests
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

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, 
             VertexControl sourceVc, VertexControl targetVc, EdgeControl edge) CreateSimpleGraph()
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

        var sourceVc = area.VertexList[v1];
        var targetVc = area.VertexList[v2];
        sourceVc.Width = 40;
        sourceVc.Height = 30;
        targetVc.Width = 40;
        targetVc.Height = 30;
        
        EnsureVertexTemplate(sourceVc);
        EnsureVertexTemplate(targetVc);

        var edge = (EdgeControl)area.EdgesList[e];
        EnsureEdgeTemplate(edge);

        return (area, sourceVc, targetVc, edge);
    }

    [Test]
    public async Task EnableGeometryCaching_DefaultsToFalse()
    {
        var (_, _, _, edge) = CreateSimpleGraph();
        
        await Assert.That(edge.EnableGeometryCaching).IsFalse();
    }

    [Test]
    public async Task EnableGeometryCaching_CanBeEnabled()
    {
        var (_, _, _, edge) = CreateSimpleGraph();
        
        edge.EnableGeometryCaching = true;
        
        await Assert.That(edge.EnableGeometryCaching).IsTrue();
    }

    [Test]
    public async Task InvalidateGeometryCache_ClearsCache()
    {
        var (_, sourceVc, targetVc, edge) = CreateSimpleGraph();
        edge.EnableGeometryCaching = true;
        
        // Update edge to populate cache
        edge.UpdateEdge();
        
        // Invalidate cache
        edge.InvalidateGeometryCache();
        
        // Cache should be invalid
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsFalse();
    }

    [Test]
    public async Task GeometryCache_IsValidAfterUpdate()
    {
        var (_, sourceVc, targetVc, edge) = CreateSimpleGraph();
        edge.EnableGeometryCaching = true;
        
        // Initial state - cache should be invalid
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsFalse();
        
        // Update edge
        edge.UpdateEdge();
        
        // Now cache should be valid
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsTrue();
    }

    [Test]
    public async Task GeometryCache_BecomesInvalidWhenSourceMoves()
    {
        var (_, sourceVc, targetVc, edge) = CreateSimpleGraph();
        edge.EnableGeometryCaching = true;
        
        // Update to populate cache
        edge.UpdateEdge();
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsTrue();
        
        // Move source vertex
        sourceVc.SetPosition(new Point(100, 150));
        
        // Cache should now be invalid
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsFalse();
    }

    [Test]
    public async Task GeometryCache_BecomesInvalidWhenTargetMoves()
    {
        var (_, sourceVc, targetVc, edge) = CreateSimpleGraph();
        edge.EnableGeometryCaching = true;
        
        // Update to populate cache
        edge.UpdateEdge();
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsTrue();
        
        // Move target vertex
        targetVc.SetPosition(new Point(300, 200));
        
        // Cache should now be invalid
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc, Size.Empty, Size.Empty)).IsFalse();
    }

    [Test]
    public async Task GeometryCache_RemainsValidIfPositionsUnchanged()
    {
        var (_, sourceVc, targetVc, edge) = CreateSimpleGraph();
        edge.EnableGeometryCaching = true;
        
        // Update to populate cache
        edge.UpdateEdge();
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsTrue();
        
        // Call UpdateEdge again without moving vertices
        edge.UpdateEdge();
        
        // Cache should still be valid
        await Assert.That(edge.IsGeometryCacheValid(sourceVc, targetVc)).IsTrue();
    }

    [Test]
    public async Task GeometryCache_DisabledByDefault_AlwaysUpdates()
    {
        var (_, sourceVc, targetVc, edge) = CreateSimpleGraph();
        
        // Caching is disabled by default
        await Assert.That(edge.EnableGeometryCaching).IsFalse();
        
        // Update edge multiple times - should work without caching
        edge.UpdateEdge();
        edge.UpdateEdge();
        edge.UpdateEdge();
        
        // Should not throw and geometry should exist
        await Assert.That(edge.GeometryBounds).IsNotNull();
    }

    [Test]
    public async Task GeometryBounds_ReturnsNullBeforeFirstUpdate()
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
            Graph = graph
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };
        
        // Create edge without updating
        var edge = new EdgeControl(null, null, e);
        
        // Before any update, geometry bounds should be null
        await Assert.That(edge.GeometryBounds).IsNull();
    }

    [Test]
    public async Task GeometryBounds_HasValueAfterUpdate()
    {
        var (_, _, _, edge) = CreateSimpleGraph();
        
        edge.UpdateEdge();
        
        await Assert.That(edge.GeometryBounds).IsNotNull();
        await Assert.That(edge.GeometryBounds!.Value.Width).IsGreaterThan(0);
    }
}
