using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Reproduction tests for the "edges not drawn until vertex moved" bug reported when many
/// vertices and edges are added to a GraphArea in bulk before positions are assigned.
/// </summary>
public class BulkAddEdgeVisibilityReproTests
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

    [Test]
    public async Task Edge_AddedBeforeVertexPositioned_BecomesVisibleAfterAutomaticRelayout()
    {
        // Simulate the real-world "bulk add" flow:
        //  1. Vertices are added to the GraphArea with no position yet (mirrors PreloadVertexes()).
        //  2. Edges referencing those un-positioned vertices are inserted at the front of the
        //     visual children collection (mirrors GenerateAllEdgesInternal()/InternalInsertEdge()
        //     with the default ControlDrawOrder.VerticesOnTop).
        //  3. The GraphArea receives its first layout pass while vertices are still un-positioned
        //     (this happens whenever the control is already on screen, or Avalonia otherwise runs
        //     an automatic layout pass before the layout algorithm has finished).
        //  4. The layout algorithm finally assigns real positions to the vertices (SetPosition),
        //     which is the *only* automatic trigger for edges to re-measure themselves.
        //  5. A subsequent, automatic layout pass runs (NOT a manual vertex drag).
        // Expectation: the edge should have valid, non-empty geometry after step 5 alone.

        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var edgeData = new TEdge(v1, v2);
        g.AddEdge(edgeData);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };

        // Step 1: add vertices without any position assigned (mirrors PreloadVertexes()).
        area.PreloadVertexes();
        foreach (var vc in area.VertexList.Values)
        {
            vc.Width = 40;
            vc.Height = 30;
            EnsureVertexTemplate(vc);
        }

        // Step 2: create the edge control and insert it at the front of Children (index 0),
        // matching InternalInsertEdge's default behavior under ControlDrawOrder.VerticesOnTop.
        var ec = area.ControlFactory.CreateEdgeControl(area.VertexList[v1], area.VertexList[v2], edgeData);
        EnsureEdgeTemplate(ec);
        area.InsertEdge(edgeData, ec);

        // Step 3: first layout pass while vertex positions are still NaN.
        area.Measure(new Size(500, 400));
        area.Arrange(new Rect(0, 0, 500, 400));

        // Step 4: the layout algorithm now assigns real positions (as RelayoutGraph.Assign() does).
        area.VertexList[v1].SetPosition(50, 100);
        area.VertexList[v2].SetPosition(250, 100);
        GraphAreaBase.SetFinalX(area.VertexList[v1], 50);
        GraphAreaBase.SetFinalY(area.VertexList[v1], 100);
        GraphAreaBase.SetFinalX(area.VertexList[v2], 250);
        GraphAreaBase.SetFinalY(area.VertexList[v2], 100);

        // Step 5: automatic follow-up layout pass (no manual drag / no explicit edge.InvalidateMeasure()).
        area.Measure(new Size(500, 400));
        area.Arrange(new Rect(0, 0, 500, 400));

        var geom = ec.GetLineGeometry();
        await Assert.That(geom).IsNotNull();
        await Assert.That(geom!.Bounds.Width + geom.Bounds.Height).IsGreaterThan(0);
    }

    [Test]
    public async Task Edge_AddedBeforeVertexPositioned_RealWindow_RealLayoutManager()
    {
        // Same scenario as above, but hosted in a real Window so InvalidateMeasure()/
        // InvalidateArrange() calls are processed by Avalonia's actual LayoutManager and
        // dispatcher-posted continuations (e.g. VertexControl.XYChanged's DispatcherPriority.Render
        // post) run exactly as they would in a real application - no manual Measure()/Arrange()
        // calls on the GraphArea itself.
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var edgeData = new TEdge(v1, v2);
        g.AddEdge(edgeData);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };

        var window = new Window { Width = 520, Height = 420, Content = area };
        window.Show();

        // Step 1: add vertices without any position assigned.
        area.PreloadVertexes();
        foreach (var vc in area.VertexList.Values)
        {
            vc.Width = 40;
            vc.Height = 30;
            EnsureVertexTemplate(vc);
        }

        // Step 2: insert the edge at the front of Children, matching InternalInsertEdge's default.
        var ec = area.ControlFactory.CreateEdgeControl(area.VertexList[v1], area.VertexList[v2], edgeData);
        EnsureEdgeTemplate(ec);
        area.InsertEdge(edgeData, ec);

        try
        {
            // Step 3: let the real LayoutManager run its first pass while positions are NaN.
            for (var i = 0; i < 3; i++)
            {
                window.Measure(new Size(520, 420));
                window.Arrange(new Rect(0, 0, 520, 420));
                Dispatcher.UIThread.RunJobs();
            }

            // Step 4: the layout algorithm assigns real positions (only automatic trigger available).
            area.VertexList[v1].SetPosition(50, 100);
            area.VertexList[v2].SetPosition(250, 100);
            GraphAreaBase.SetFinalX(area.VertexList[v1], 50);
            GraphAreaBase.SetFinalY(area.VertexList[v1], 100);
            GraphAreaBase.SetFinalX(area.VertexList[v2], 250);
            GraphAreaBase.SetFinalY(area.VertexList[v2], 100);

            // Step 5: let the real LayoutManager + dispatcher settle - NO manual edge.InvalidateMeasure(),
            // NO simulated vertex drag.
            for (var i = 0; i < 5; i++)
            {
                window.Measure(new Size(520, 420));
                window.Arrange(new Rect(0, 0, 520, 420));
                Dispatcher.UIThread.RunJobs();
            }

            var geom = ec.GetLineGeometry();
            await Assert.That(geom).IsNotNull();
            await Assert.That(geom!.Bounds.Width + geom.Bounds.Height).IsGreaterThan(0);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task ManyEdges_AddedBeforeVertexPositioned_AllBecomeVisibleAfterAutomaticRelayout()
    {
        // Scaled-up version: many vertices/edges, mirroring GenerateAllEdgesInternal() being called
        // while vertices are still unpositioned, then positions assigned in one synchronous burst
        // (as a layout algorithm would), then only automatic (non-drag) layout passes run.
        const int vertexCount = 200;
        var random = new Random(42);

        var g = new BidirectionalGraph<TVertex, TEdge>();
        var vertices = new TVertex[vertexCount];
        for (var i = 0; i < vertexCount; i++)
        {
            vertices[i] = new TVertex($"V{i}");
            g.AddVertex(vertices[i]);
        }

        for (var i = 1; i < vertexCount; i++)
            g.AddEdge(new TEdge(vertices[random.Next(i)], vertices[i]));
        for (var i = 0; i < vertexCount; i++)
        {
            var s = random.Next(vertexCount);
            var t = random.Next(vertexCount);
            if (s != t) g.AddEdge(new TEdge(vertices[s], vertices[t]));
        }

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 2000,
            Height = 2000
        };

        var window = new Window { Width = 2020, Height = 2020, Content = area };
        window.Show();

        // Step 1: vertices with no position yet.
        area.PreloadVertexes();
        foreach (var vc in area.VertexList.Values)
        {
            vc.Width = 40;
            vc.Height = 30;
            EnsureVertexTemplate(vc);
        }

        // Step 2: generate all edges - InternalInsertEdge() puts each at Children index 0
        // (ControlDrawOrder.VerticesOnTop is the default), all while vertex X/Y are still NaN.
        area.GenerateAllEdges(isVisibleByDefault: true, updateLayout: true);
        foreach (var ec in area.EdgesList.Values)
            EnsureEdgeTemplate(ec);

        try
        {
            for (var i = 0; i < 3; i++)
            {
                window.Measure(new Size(2020, 2020));
                window.Arrange(new Rect(0, 0, 2020, 2020));
                Dispatcher.UIThread.RunJobs();
            }

            // Step 3: assign positions to every vertex in one synchronous burst, like a layout
            // algorithm's result-assignment loop (RelayoutGraph.Assign()).
            var x = 0;
            foreach (var vc in area.VertexList.Values)
            {
                var px = 50 + (x % 40) * 45;
                var py = 50 + (x / 40) * 45;
                vc.SetPosition(px, py);
                GraphAreaBase.SetFinalX(vc, px);
                GraphAreaBase.SetFinalY(vc, py);
                x++;
            }

            // Step 4: only automatic layout passes - no manual per-edge InvalidateMeasure(), no drag.
            for (var i = 0; i < 5; i++)
            {
                window.Measure(new Size(2020, 2020));
                window.Arrange(new Rect(0, 0, 2020, 2020));
                Dispatcher.UIThread.RunJobs();
            }

            var missing = area.EdgesList.Values
                .Where(ec => ec.GetLineGeometry() is not { } geo || geo.Bounds.Width + geo.Bounds.Height <= 0)
                .ToList();

            await Assert.That(missing.Count).IsEqualTo(0);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
