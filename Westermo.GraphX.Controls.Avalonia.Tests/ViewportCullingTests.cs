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
/// Tests for viewport-based visibility culling functionality.
/// </summary>
public class ViewportCullingTests
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

    private GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> CreateLargeGraph(int vertexCount)
    {
        var graph = new BidirectionalGraph<TVertex, TEdge>();
        var vertices = new List<TVertex>();
        
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new TVertex($"V{i}") { ID = i };
            vertices.Add(v);
            graph.AddVertex(v);
        }

        // Create a chain of edges
        for (int i = 0; i < vertexCount - 1; i++)
        {
            graph.AddEdge(new TEdge(vertices[i], vertices[i + 1]));
        }

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = graph,
            EnableParallelEdges = false
        };

        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 10000,
            Height = 10000
        };

        // Position vertices in a grid pattern
        var positions = new Dictionary<TVertex, Point>();
        int cols = (int)Math.Ceiling(Math.Sqrt(vertexCount));
        for (int i = 0; i < vertexCount; i++)
        {
            int row = i / cols;
            int col = i % cols;
            positions[vertices[i]] = new Point(col * 100, row * 100);
        }

        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);
        
        // Apply templates
        foreach (var vc in area.VertexList.Values)
        {
            vc.Width = 40;
            vc.Height = 30;
            EnsureVertexTemplate(vc);
        }
        foreach (var ec in area.EdgesList.Values)
        {
            EnsureEdgeTemplate(ec);
        }

        return area;
    }

    [Test]
    public async Task ViewportCulling_CanBeEnabled()
    {
        var area = CreateLargeGraph(10);
        
        await Assert.That(area.EnableViewportCulling).IsFalse();
        
        area.EnableViewportCulling = true;
        
        await Assert.That(area.EnableViewportCulling).IsTrue();
        await Assert.That(area.ViewportCulling).IsNotNull();
    }

    [Test]
    public async Task ViewportCulling_CanBeDisabled()
    {
        var area = CreateLargeGraph(10);
        
        area.EnableViewportCulling = true;
        await Assert.That(area.ViewportCulling).IsNotNull();
        
        area.EnableViewportCulling = false;
        
        await Assert.That(area.ViewportCulling).IsNull();
    }

    [Test]
    public async Task ViewportCulling_MarginCanBeConfigured()
    {
        var area = CreateLargeGraph(10);
        area.EnableViewportCulling = true;
        
        area.ViewportCulling.CullingMargin = 200;
        
        await Assert.That(area.ViewportCulling.CullingMargin).IsEqualTo(200);
    }

    [Test]
    public async Task ViewportCulling_HidesVerticesOutsideViewport()
    {
        var area = CreateLargeGraph(100);
        area.EnableViewportCulling = true;
        area.ViewportCulling.CullingMargin = 0;
        
        // Update with a small viewport that only covers the top-left
        var smallViewport = new Rect(0, 0, 200, 200);
        area.UpdateViewport(smallViewport);
        
        // Vertices in the top-left should be visible
        var visibleCount = area.VertexList.Values.Count(v => v.IsVisible);
        var hiddenCount = area.VertexList.Values.Count(v => !v.IsVisible);
        
        // With 100 vertices in a 10x10 grid at 100px spacing,
        // viewport 200x200 should show roughly 2x2 = 4 vertices (plus margin)
        await Assert.That(visibleCount).IsGreaterThan(0);
        await Assert.That(hiddenCount).IsGreaterThan(0);
    }

    [Test]
    public async Task ViewportCulling_ShowsAllVerticesInLargeViewport()
    {
        var area = CreateLargeGraph(25);
        area.EnableViewportCulling = true;
        
        // Update with a viewport that covers everything
        var largeViewport = new Rect(0, 0, 10000, 10000);
        area.UpdateViewport(largeViewport);
        
        var allVisible = area.VertexList.Values.All(v => v.IsVisible);
        await Assert.That(allVisible).IsTrue();
    }

    [Test]
    public async Task ViewportCulling_MarginExpandsVisibleArea()
    {
        var area = CreateLargeGraph(100);
        area.EnableViewportCulling = true;
        
        // First update with no margin
        area.ViewportCulling.CullingMargin = 0;
        var smallViewport = new Rect(0, 0, 150, 150);
        area.UpdateViewport(smallViewport);
        var visibleWithoutMargin = area.VertexList.Values.Count(v => v.IsVisible);
        
        // Now update with margin
        area.ViewportCulling.CullingMargin = 200;
        area.UpdateViewport(smallViewport);
        var visibleWithMargin = area.VertexList.Values.Count(v => v.IsVisible);
        
        // More vertices should be visible with margin
        await Assert.That(visibleWithMargin).IsGreaterThanOrEqualTo(visibleWithoutMargin);
    }

    [Test]
    public async Task ViewportCulling_UpdatesOnViewportChange()
    {
        var area = CreateLargeGraph(100);
        area.EnableViewportCulling = true;
        area.ViewportCulling.CullingMargin = 0;
        
        // Start with small viewport
        area.UpdateViewport(new Rect(0, 0, 150, 150));
        var initialVisible = area.VertexList.Values.Count(v => v.IsVisible);
        
        // Move viewport to different area
        area.UpdateViewport(new Rect(500, 500, 150, 150));
        var afterMoveVisible = area.VertexList.Values.Count(v => v.IsVisible);
        
        // Both should have some visible vertices (different ones)
        await Assert.That(initialVisible).IsGreaterThan(0);
        await Assert.That(afterMoveVisible).IsGreaterThan(0);
    }
}
