using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Behaviours;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Logic.Models;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Verifies that vertex drags trigger an edge-routing recompute when
/// <see cref="DragBehaviour.UpdateEdgesOnMoveProperty"/> is enabled, mirroring the WPF behaviour.
/// </summary>
public class DragRerouteTests
{
    private sealed class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private sealed class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Point[]? RoutingPoints { get; set; }
    }

    /// <summary>
    /// GraphArea spy that records every <see cref="GraphArea.ComputeEdgeRoutesByVertex"/> invocation
    /// instead of running the real routing algorithm.
    /// </summary>
    private sealed class SpyGraphArea : GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
    {
        public List<VertexControl> RecomputedVertices { get; } = [];

        internal override void ComputeEdgeRoutesByVertex(VertexControl vc, bool vertexDataNeedUpdate = true)
        {
            RecomputedVertices.Add(vc);
        }
    }

    private static void EnsureVertexTemplate(VertexControl vc)
    {
        if (vc.Template != null) return;
        var content = new Grid();
        var panel = new StackPanel { Name = "PART_vcproot" };
        content.Children.Add(panel);
        var ns = new NameScope();
        ns.Register("PART_vcproot", panel);
        vc.Template = new ControlTemplate
        {
            TargetType = typeof(VertexControl),
            Content = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns))
        };
        vc.ApplyTemplate();
    }

    private static void EnsureEdgeTemplate(EdgeControl ec)
    {
        if (ec.Template != null) return;
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
        ec.Template = new ControlTemplate
        {
            TargetType = typeof(EdgeControl),
            Content = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns))
        };
        ec.ApplyTemplate();
    }

    private static SpyGraphArea CreateAreaWithRouting()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A") { ID = 1 };
        var v2 = new TVertex("B") { ID = 2 };
        g.AddVertex(v1);
        g.AddVertex(v2);
        g.AddEdge(new TEdge(v1, v2));

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER
        };

        var area = new SpyGraphArea { LogicCore = lc };
        area.PreloadVertexes();

        foreach (var kv in area.VertexList)
        {
            kv.Value.Width = 40;
            kv.Value.Height = 30;
            kv.Value.SetPosition(kv.Key == v1 ? 50 : 200, 80);
            GraphAreaBase.SetFinalX(kv.Value, kv.Value.GetPosition().X);
            GraphAreaBase.SetFinalY(kv.Value, kv.Value.GetPosition().Y);
            EnsureVertexTemplate(kv.Value);
        }

        foreach (var e in g.Edges)
        {
            var ec = area.ControlFactory.CreateEdgeControl(area.VertexList[e.Source], area.VertexList[e.Target], e);
            EnsureEdgeTemplate(ec);
            area.AddEdge(e, ec);
        }

        return area;
    }

    [Test]
    public async Task Drag_Recomputes_Routing_When_UpdateEdgesOnMove_Set()
    {
        var area = CreateAreaWithRouting();
        var vertexControl = area.VertexList.Values.First();

        DragBehaviour.SetIsDragEnabled(vertexControl, true);
        DragBehaviour.SetUpdateEdgesOnMove(vertexControl, true);

        vertexControl.SetPosition(500, 400);
        vertexControl.UpdateEdgesIfRequested();

        await Assert.That(area.RecomputedVertices).Contains(vertexControl);
    }

    [Test]
    public async Task Drag_Does_Not_Recompute_When_UpdateEdgesOnMove_Unset()
    {
        var area = CreateAreaWithRouting();
        var vertexControl = area.VertexList.Values.First();

        DragBehaviour.SetIsDragEnabled(vertexControl, true);
        // UpdateEdgesOnMove deliberately not set.

        vertexControl.SetPosition(500, 400);
        vertexControl.UpdateEdgesIfRequested();

        await Assert.That(area.RecomputedVertices).IsEmpty();
    }

    [Test]
    public async Task Drag_Does_Not_Recompute_When_EdgeRouting_Disabled()
    {
        var area = CreateAreaWithRouting();
        // Switch routing OFF: IsEdgeRoutingEnabled checks DefaultEdgeRoutingAlgorithm != None.
        area.LogicCore!.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;

        var vertexControl = area.VertexList.Values.First();
        DragBehaviour.SetIsDragEnabled(vertexControl, true);
        DragBehaviour.SetUpdateEdgesOnMove(vertexControl, true);

        vertexControl.SetPosition(500, 400);
        vertexControl.UpdateEdgesIfRequested();

        await Assert.That(area.RecomputedVertices).IsEmpty();
    }

    [Test]
    public async Task Drag_Recompute_Fires_Once_Per_Move()
    {
        var area = CreateAreaWithRouting();
        var vertexControl = area.VertexList.Values.First();

        DragBehaviour.SetIsDragEnabled(vertexControl, true);
        DragBehaviour.SetUpdateEdgesOnMove(vertexControl, true);

        vertexControl.SetPosition(100, 100);
        vertexControl.UpdateEdgesIfRequested();
        vertexControl.SetPosition(200, 200);
        vertexControl.UpdateEdgesIfRequested();
        vertexControl.SetPosition(300, 300);
        vertexControl.UpdateEdgesIfRequested();

        await Assert.That(area.RecomputedVertices.Count).IsEqualTo(3);
    }
}
