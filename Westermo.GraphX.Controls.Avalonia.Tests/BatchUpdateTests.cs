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
/// Tests for batch update functionality to reduce redundant layout passes.
/// </summary>
public class BatchUpdateTests
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

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, List<TVertex> vertices) CreateGraph(int vertexCount)
    {
        var graph = new BidirectionalGraph<TVertex, TEdge>();
        var vertices = new List<TVertex>();
        
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new TVertex($"V{i}") { ID = i };
            vertices.Add(v);
            graph.AddVertex(v);
        }

        // Create edges between consecutive vertices
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
            Width = 2000,
            Height = 2000
        };

        var positions = new Dictionary<TVertex, Point>();
        for (int i = 0; i < vertexCount; i++)
        {
            positions[vertices[i]] = new Point(i * 100, 100);
        }

        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);
        
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

        return (area, vertices);
    }

    [Test]
    public async Task BeginBatchUpdate_ReturnsDisposableScope()
    {
        var (area, _) = CreateGraph(5);
        
        using var scope = area.BeginBatchUpdate();
        
        await Assert.That(scope).IsNotNull();
        await Assert.That(scope).IsTypeOf<BatchUpdateScope>();
    }

    [Test]
    public async Task BeginDeferredPositionUpdate_ReturnsDisposableScope()
    {
        var (area, _) = CreateGraph(5);
        
        using var scope = area.BeginDeferredPositionUpdate();
        
        await Assert.That(scope).IsNotNull();
        await Assert.That(scope).IsTypeOf<BatchUpdateScope>();
    }

    [Test]
    public async Task BatchUpdate_SuppressesLayoutDuringScope()
    {
        var (area, vertices) = CreateGraph(10);
        
        using (area.BeginBatchUpdate())
        {
            // During batch update, layout should be suppressed
            await Assert.That(area.SuppressLayoutUpdates).IsTrue();
            
            // We can still make changes
            foreach (var v in vertices)
            {
                if (area.VertexList.TryGetValue(v, out var vc))
                {
                    vc.SetPosition(new Point(v.ID * 50, 200));
                }
            }
        }
        
        // After scope ends, layout should no longer be suppressed
        await Assert.That(area.SuppressLayoutUpdates).IsFalse();
    }

    [Test]
    public async Task DeferredPositionUpdate_SuppressesLayoutDuringScope()
    {
        var (area, vertices) = CreateGraph(10);
        
        using (area.BeginDeferredPositionUpdate())
        {
            await Assert.That(area.SuppressLayoutUpdates).IsTrue();
        }
        
        await Assert.That(area.SuppressLayoutUpdates).IsFalse();
    }

    [Test]
    public async Task NestedBatchUpdates_MaintainSuppressionUntilAllDisposed()
    {
        var (area, _) = CreateGraph(5);
        
        using (area.BeginBatchUpdate())
        {
            await Assert.That(area.SuppressLayoutUpdates).IsTrue();
            
            using (area.BeginBatchUpdate())
            {
                await Assert.That(area.SuppressLayoutUpdates).IsTrue();
            }
            
            // Still suppressed because outer scope is still active
            await Assert.That(area.SuppressLayoutUpdates).IsTrue();
        }
        
        // Now both scopes are disposed
        await Assert.That(area.SuppressLayoutUpdates).IsFalse();
    }

    [Test]
    public async Task BatchUpdate_CanMoveMultipleVertices()
    {
        var (area, vertices) = CreateGraph(20);
        
        var newPositions = new Dictionary<TVertex, Point>();
        for (int i = 0; i < vertices.Count; i++)
        {
            newPositions[vertices[i]] = new Point(i * 75, 300);
        }

        using (area.BeginDeferredPositionUpdate())
        {
            foreach (var kvp in newPositions)
            {
                if (area.VertexList.TryGetValue(kvp.Key, out var vc))
                {
                    vc.SetPosition(kvp.Value);
                }
            }
        }
        
        // Verify positions were updated
        foreach (var kvp in newPositions)
        {
            if (area.VertexList.TryGetValue(kvp.Key, out var vc))
            {
                var pos = vc.GetPosition();
                await Assert.That(pos.X).IsEqualTo(kvp.Value.X);
                await Assert.That(pos.Y).IsEqualTo(kvp.Value.Y);
            }
        }
    }
}
