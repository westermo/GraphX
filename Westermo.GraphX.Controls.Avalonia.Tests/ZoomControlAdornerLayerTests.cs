using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Regression tests for adorners attached to <see cref="VertexControl"/>s while the hosting
/// <see cref="ZoomControl"/> is zoomed or panned.
///
/// Avalonia's <c>AdornerLayer</c> bakes <c>adornedElement.TransformToVisual(layer)</c> into each
/// adorner's <c>RenderTransform</c> during its own arrange pass, and only re-runs that pass when
/// it observes a <c>RenderTransform</c>/<c>Bounds</c> change on the adorned element or an ancestor.
/// Since the ZoomControl mutates its presenter's scale/translate transforms in place (no property
/// change is raised), it must explicitly invalidate the adorner layer - otherwise adorners stay
/// frozen at their pre-zoom screen position while the vertex moves underneath them.
/// </summary>
public class ZoomControlAdornerLayerTests
{
    private const double Tolerance = 0.5;

    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; }
    }

    /// <summary>
    /// Gives the vertex control a minimal template so it can be measured/arranged headlessly.
    /// </summary>
    private static void EnsureVertexTemplate(VertexControl vc)
    {
        if (vc.Template != null) return;
        var content = new Grid();
        var panel = new StackPanel { Name = "PART_vcproot" };
        content.Children.Add(panel);
        var ns = new NameScope();
        ns.Register("PART_vcproot", panel);
        var functor = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns));
        vc.Template = new ControlTemplate { TargetType = typeof(VertexControl), Content = functor };
        vc.ApplyTemplate();
    }

    /// <summary>
    /// Builds a window containing a ZoomControl hosting a GraphArea with a single vertex placed
    /// away from the origin, plus an adorner attached to that vertex via the adorner layer.
    /// </summary>
    private static (Window window, ZoomControl zoom, VertexControl vertex, Control adorner) CreateScene()
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
        vc.SetPosition(500, 300);
        GraphAreaBase.SetFinalX(vc, 500);
        GraphAreaBase.SetFinalY(vc, 300);

        var zc = new ZoomControl { Content = area, Mode = ZoomControlModes.Custom };
        // The headless test app has no window theme, so no VisualLayerManager (and therefore no
        // adorner layer) is created by the Window's template. Host one explicitly so the scene
        // matches a real themed application.
        var layerManager = new VisualLayerManager { Child = zc };
        var window = new Window { Width = 800, Height = 600, Content = layerManager };
        window.Show();
        window.Measure(new Size(800, 600));
        window.Arrange(new Rect(0, 0, 800, 600));
        window.UpdateLayout();

        // An adorner with no explicit size is arranged to exactly cover the adorned element, so
        // its local coordinate space coincides with the vertex's: a correctly positioned adorner
        // maps every local point to the same root coordinate as the vertex does.
        var adorner = new Border { Background = Brushes.Red };
        AdornerLayer.SetAdorner(vc, adorner);
        window.UpdateLayout();

        return (window, zc, vc, adorner);
    }

    /// <summary>
    /// Returns the offset (in root/window coordinates) between the adorner and the vertex for the
    /// given point expressed in their shared local coordinate space. A correctly tracking adorner
    /// yields (0,0) for every point, at any zoom level and pan offset.
    /// </summary>
    private static Point OffsetInRoot(Visual root, Visual vertex, Visual adorner, Point localPoint)
    {
        var vertexPoint = vertex.TranslatePoint(localPoint, root) ?? default;
        var adornerPoint = adorner.TranslatePoint(localPoint, root) ?? default;
        return adornerPoint - vertexPoint;
    }

    /// <summary>
    /// Asserts that the adorner is exactly aligned with the adorned vertex, checking both the
    /// origin (catches lost translation) and the far corner (catches lost scale).
    /// </summary>
    private static async Task AssertAdornerAlignedAsync(Window window, VertexControl vc, Control adorner,
        string phase)
    {
        var origin = OffsetInRoot(window, vc, adorner, new Point(0, 0));
        var corner = OffsetInRoot(window, vc, adorner, new Point(vc.Width, vc.Height));
        Console.WriteLine($"{phase}: originOffset={origin} cornerOffset={corner}");

        await Assert.That(origin.X).IsEqualTo(0).Within(Tolerance);
        await Assert.That(origin.Y).IsEqualTo(0).Within(Tolerance);
        await Assert.That(corner.X).IsEqualTo(0).Within(Tolerance);
        await Assert.That(corner.Y).IsEqualTo(0).Within(Tolerance);
    }

    [Test]
    public async Task PanningZoomControl_KeepsAdornerAlignedWithVertex()
    {
        var (window, zc, vc, adorner) = CreateScene();

        await AssertAdornerAlignedAsync(window, vc, adorner, "before pan");

        // Pure pan: no content-extent change, so nothing else invalidates the adorner layer.
        zc.TranslateX += 75;
        zc.TranslateY += 40;
        window.UpdateLayout();

        await AssertAdornerAlignedAsync(window, vc, adorner, "after pan");
    }

    [Test]
    public async Task ZoomingZoomControl_KeepsAdornerAlignedWithVertex()
    {
        var (window, zc, vc, adorner) = CreateScene();

        await AssertAdornerAlignedAsync(window, vc, adorner, "before zoom");

        zc.Zoom *= 2;
        window.UpdateLayout();

        await AssertAdornerAlignedAsync(window, vc, adorner, "after zoom");
    }

    [Test]
    public async Task ZoomToFill_KeepsAdornerAlignedWithVertex()
    {
        var (window, zc, vc, adorner) = CreateScene();

        await AssertAdornerAlignedAsync(window, vc, adorner, "before fill");

        zc.ZoomToFill();
        window.UpdateLayout();
        await AssertAdornerAlignedAsync(window, vc, adorner, "after first fill");

        // A second fill is a no-op for the content extent, so only the zoom/translate transform
        // mutation can keep the adorner in sync.
        zc.ZoomToFill();
        window.UpdateLayout();
        await AssertAdornerAlignedAsync(window, vc, adorner, "after second fill");
    }
}
