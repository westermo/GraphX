using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Controls.Models;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Integration tests exercising a real <see cref="GraphArea{TVertex,TEdge,TGraph}"/> (which
/// implements <see cref="Westermo.GraphX.Controls.Controls.Misc.ITrackableContent"/> via
/// <see cref="GraphAreaBase"/>) together with a real <see cref="ZoomControl"/>, mirroring how
/// WeConfig's TopologyGraphLayout inserts custom "geometry" overlay controls
/// (Subnet/Ring/Route) via InsertCustomChildControl and reports ContentSize.
/// </summary>
public class GraphAreaZoomIntegrationTests
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

    /// <summary>
    /// Mimics WeConfig's GraphAreaGeometry&lt;T&gt;.MeasureOverride fallback-offset logic exactly,
    /// including its (default-parameter) calls into GraphAreaBase.SetX/SetY.
    /// </summary>
    private sealed class FakeEmptyGeometry(IDictionary<TVertex, VertexControl> vertexList) : Control
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            // Items is always empty/null -> always takes the fallback path, like WeConfig's
            // SubnetGeometry/RingGeometry when there are no subnets/rings to draw.
            foreach (var vertex in vertexList.Values)
            {
                var pos = vertex.GetPosition();
                GraphAreaBase.SetX(this, pos.X);
                GraphAreaBase.SetY(this, pos.Y);
                return default;
            }

            GraphAreaBase.SetX(this, 0);
            GraphAreaBase.SetY(this, 0);
            return default;
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
        var functor =
            new Func<IServiceProvider?, object?>(_ =>
                new TemplateResult<Control>(content, ns));
        vc.Template = new ControlTemplate
            { TargetType = typeof(VertexControl), Content = functor };
        vc.ApplyTemplate();
    }

    [Test]
    public async Task SingleVertex_WithEmptyGeometryOverlay_ContentSizeMatchesVertexBounds()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("Root");
        g.AddVertex(v1);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.Width = 64;
        vc.Height = 64;
        EnsureVertexTemplate(vc);

        // Insert the fake "empty geometry overlay" BEFORE the vertex is positioned,
        // exactly like TopologyGraphLayout's constructor does (geometries constructed/inserted
        // at index 0-2, before any vertex position is known).
        var geometry = new FakeEmptyGeometry(area.VertexList);
        area.InsertCustomChildControl(0, geometry);

        // Now position the single vertex somewhere away from the origin - simulating a
        // real, already-laid-out node (e.g. loaded from a saved topology / custom layout).
        vc.SetPosition(500, 300);
        GraphAreaBase.SetFinalX(vc, 500);
        GraphAreaBase.SetFinalY(vc, 300);

        area.Measure(new Size(2000, 2000));
        area.Arrange(new Rect(0, 0, 2000, 2000));

        var contentSize = area.ContentSize;
        Console.WriteLine($"ContentSize = {contentSize}");
        Console.WriteLine($"geometry X={GraphAreaBase.GetX(geometry)} Y={GraphAreaBase.GetY(geometry)} " +
                           $"FinalX={GraphAreaBase.GetFinalX(geometry)} FinalY={GraphAreaBase.GetFinalY(geometry)}");

        // Expect ContentSize to tightly match the vertex's own bounds: (500,300)-(564,364).
        await Assert.That(contentSize.X).IsEqualTo(500).Within(0.5);
        await Assert.That(contentSize.Y).IsEqualTo(300).Within(0.5);
        await Assert.That(contentSize.Width).IsEqualTo(64).Within(0.5);
        await Assert.That(contentSize.Height).IsEqualTo(64).Within(0.5);
    }

    [Test]
    public async Task SingleVertex_WithEmptyGeometryOverlay_FirstMeasureBeforeVertexPositioned()
    {
        // This time, measure ONCE before the vertex has a real position (mimicking the very
        // first layout pass, before the layout algorithm has run), then again after.
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("Root");
        g.AddVertex(v1);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.Width = 64;
        vc.Height = 64;
        EnsureVertexTemplate(vc);

        var geometry = new FakeEmptyGeometry(area.VertexList);
        area.InsertCustomChildControl(0, geometry);

        // First measure pass: vertex position is still NaN (unset).
        area.Measure(new Size(2000, 2000));
        area.Arrange(new Rect(0, 0, 2000, 2000));
        Console.WriteLine($"Pass 1 ContentSize = {area.ContentSize}");

        // Now the layout algorithm assigns the real position.
        vc.SetPosition(500, 300);
        GraphAreaBase.SetFinalX(vc, 500);
        GraphAreaBase.SetFinalY(vc, 300);
        area.InvalidateMeasure();
        area.Measure(new Size(2000, 2000));
        area.Arrange(new Rect(0, 0, 2000, 2000));

        var contentSize = area.ContentSize;
        Console.WriteLine($"Pass 2 ContentSize = {contentSize}");
        Console.WriteLine($"geometry X={GraphAreaBase.GetX(geometry)} Y={GraphAreaBase.GetY(geometry)} " +
                           $"FinalX={GraphAreaBase.GetFinalX(geometry)} FinalY={GraphAreaBase.GetFinalY(geometry)}");

        await Assert.That(contentSize.X).IsEqualTo(500).Within(0.5);
        await Assert.That(contentSize.Y).IsEqualTo(300).Within(0.5);
        await Assert.That(contentSize.Width).IsEqualTo(64).Within(0.5);
        await Assert.That(contentSize.Height).IsEqualTo(64).Within(0.5);
    }

    [Test]
    public async Task SingleVertex_WithThreeEmptyGeometryOverlays_ZoomToFillCentersContent()
    {
        // End-to-end: real GraphArea (ITrackableContent) + 3 "empty geometry" overlays
        // (mirroring WeConfig's Subnet/Ring/Route geometries with zero items) + real ZoomControl,
        // in a real Window, to check whether ZoomToFill() actually centers a single small vertex.
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("Root");
        g.AddVertex(v1);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.Width = 64;
        vc.Height = 64;
        EnsureVertexTemplate(vc);

        // 3 overlays inserted before the vertex is positioned, exactly like
        // TopologyGraphLayout's constructor (InsertCustomChildControl(0/1/2, ...)).
        var geom0 = new FakeEmptyGeometry(area.VertexList);
        var geom1 = new FakeEmptyGeometry(area.VertexList);
        var geom2 = new FakeEmptyGeometry(area.VertexList);
        area.InsertCustomChildControl(0, geom0);
        area.InsertCustomChildControl(1, geom1);
        area.InsertCustomChildControl(2, geom2);

        // Position the single vertex away from the origin (like a real saved topology).
        vc.SetPosition(500, 300);
        GraphAreaBase.SetFinalX(vc, 500);
        GraphAreaBase.SetFinalY(vc, 300);

        var zc = new ZoomControl { Content = area, Mode = ZoomControlModes.Fill };
        var window = new Window { Width = 800, Height = 600, Content = zc };
        window.Show();
        window.Measure(new Size(800, 600));
        window.Arrange(new Rect(0, 0, 800, 600));

        // Give the dispatcher a chance to run any queued layout/measure passes.
        for (var i = 0; i < 5; i++)
        {
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
                global::Avalonia.Threading.DispatcherPriority.Background);
        }

        Console.WriteLine($"area.ContentSize = {area.ContentSize}");
        Console.WriteLine($"zc.Zoom={zc.Zoom} TranslateX={zc.TranslateX} TranslateY={zc.TranslateY}");
        Console.WriteLine($"zc.ActualWidth={zc.Bounds.Width} zc.ActualHeight={zc.Bounds.Height}");

        // Compute where the content's own center (in content space) ends up on screen,
        // using the same center-origin transform model verified for ZoomControl:
        // finalScreen(p) = C*(1-zoom) + zoom*p + translate.
        var cs = area.ContentSize;
        var contentCenter = new Point(cs.X + cs.Width / 2, cs.Y + cs.Height / 2);
        var viewportCenter = new Point(zc.Bounds.Width / 2, zc.Bounds.Height / 2);
        var zoom = zc.Zoom;
        var finalScreenX = viewportCenter.X * (1 - zoom) + zoom * contentCenter.X + zc.TranslateX;
        var finalScreenY = viewportCenter.Y * (1 - zoom) + zoom * contentCenter.Y + zc.TranslateY;
        Console.WriteLine($"content center projected to screen = ({finalScreenX}, {finalScreenY}), viewport center = {viewportCenter}");

        await Assert.That(finalScreenX).IsEqualTo(viewportCenter.X).Within(1.0);
        await Assert.That(finalScreenY).IsEqualTo(viewportCenter.Y).Within(1.0);
    }

    [Test]
    public async Task OneShotZoomToFill_CalledBeforeControlHasBounds_WithModeOriginal_IsRetriedOnceLaidOut()
    {
        // Reproduces WeConfig's actual usage pattern (View.axaml.cs / ZoomControlInfo):
        // - The ViewModel's ZoomControlInfo.Mode defaults to ZoomControlModes.Original (NOT Fill).
        // - WeConfig calls ZoomControl.ZoomToFill() exactly ONCE, directly, from a "Loaded"
        //   handler - it does NOT set Mode = Fill.
        // If that one-shot call races against the ZoomControl's own layout (ActualWidth/Height
        // still 0), DoZoomToFill()'s zero-bounds guard makes it a no-op. Since Mode never becomes
        // Fill, the request must still be retried via the pending-fill flag once real bounds
        // become available - otherwise content would be left un-fitted forever.
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("Root");
        g.AddVertex(v1);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.Width = 64;
        vc.Height = 64;
        EnsureVertexTemplate(vc);
        vc.SetPosition(500, 300);
        GraphAreaBase.SetFinalX(vc, 500);
        GraphAreaBase.SetFinalY(vc, 300);

        // Mode = Original, matching ZoomControlInfo's default - Fill auto-refit is NOT active.
        var zc = new ZoomControl { Content = area, Mode = ZoomControlModes.Original };

        // Call ZoomToFill() immediately, BEFORE the control is even attached to a Window/visual
        // tree - i.e. before it has ever had a chance to be measured/arranged, exactly the race
        // OnGraphLayoutLoaded can lose for a trivially-fast-loading single-node graph.
        zc.ZoomToFill();

        Console.WriteLine(
            $"Immediately after one-shot ZoomToFill (no bounds yet): Zoom={zc.Zoom} TranslateX={zc.TranslateX} TranslateY={zc.TranslateY}");

        // Now actually attach/show/layout the control - bounds become available only now.
        var window = new Window { Width = 800, Height = 600, Content = zc };
        window.Show();
        window.Measure(new Size(800, 600));
        window.Arrange(new Rect(0, 0, 800, 600));
        for (var i = 0; i < 5; i++)
        {
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
                global::Avalonia.Threading.DispatcherPriority.Background);
        }

        Console.WriteLine($"area.ContentSize = {area.ContentSize}");
        Console.WriteLine($"zc.Zoom={zc.Zoom} TranslateX={zc.TranslateX} TranslateY={zc.TranslateY}");
        Console.WriteLine($"zc.ActualWidth={zc.Bounds.Width} zc.ActualHeight={zc.Bounds.Height}");

        var cs = area.ContentSize;
        var contentCenter = new Point(cs.X + cs.Width / 2, cs.Y + cs.Height / 2);
        var viewportCenter = new Point(zc.Bounds.Width / 2, zc.Bounds.Height / 2);
        var zoom = zc.Zoom;
        var finalScreenX = viewportCenter.X * (1 - zoom) + zoom * contentCenter.X + zc.TranslateX;
        var finalScreenY = viewportCenter.Y * (1 - zoom) + zoom * contentCenter.Y + zc.TranslateY;
        Console.WriteLine(
            $"content center projected to screen = ({finalScreenX}, {finalScreenY}), viewport center = {viewportCenter}");

        // Without the pending-fill retry mechanism, the content would stay at its raw,
        // un-fitted position (Zoom=1, Translate=0 => screen = content coordinates
        // (500+32, 300+32) = (532, 332)) instead of the viewport center (400, 300) - i.e. shoved
        // toward a corner with blank space elsewhere, exactly the reported symptom. With the fix,
        // the deferred request is retried once the control is actually laid out, and the content
        // ends up properly centered even though Mode never became Fill.
        await Assert.That(finalScreenX).IsEqualTo(viewportCenter.X).Within(1.0);
        await Assert.That(finalScreenY).IsEqualTo(viewportCenter.Y).Within(1.0);
    }
}
