using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public sealed class GraphAreaRasterCacheTests
{
    private const double GraphWidth = 800;
    private const double GraphHeight = 600;
    private const double ItemSize = 40;

    [Test]
    public async Task CachedGraphRendering_HidesLiveSources_AndRestoresTheirOriginalVisibility()
    {
        var (window, area, visible, hidden, _) = CreateScene(default);
        try
        {
            hidden.IsVisible = false;

            area.EnableGraphRenderCache = true;

            await Assert.That(area.IsGraphRenderCacheActive).IsTrue();
            await Assert.That(visible.IsVisible).IsFalse();
            await Assert.That(hidden.IsVisible).IsFalse();

            area.EnableGraphRenderCache = false;

            await Assert.That(area.IsGraphRenderCacheActive).IsFalse();
            await Assert.That(visible.IsVisible).IsTrue();
            await Assert.That(hidden.IsVisible).IsFalse();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task CachedGraphRendering_PreservesAnExplicitVisibleStateChange()
    {
        var (window, area, _, hidden, _) = CreateScene(default);
        try
        {
            hidden.IsVisible = false;
            area.EnableGraphRenderCache = true;

            // The cache suppresses its sources, but an explicit state change
            // must win when it restores live rendering.
            hidden.IsVisible = true;

            await Assert.That(area.IsGraphRenderCacheActive).IsFalse();
            await Assert.That(hidden.IsVisible).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task CachedGraphRendering_IsReused_WhenZoomControlOnlyPans()
    {
        var (window, area, visible, _, zoomControl) = CreateScene(default);
        try
        {
            area.EnableGraphRenderCache = true;
            var rasterizations = area.GraphRenderCacheRasterizationCount;

            zoomControl.Mode = ZoomControlModes.Custom;
            zoomControl.TranslateX = -120;
            zoomControl.TranslateY = -80;
            Dispatcher.UIThread.RunJobs();
            using var image = Render(zoomControl);

            await Assert.That(area.IsGraphRenderCacheActive).IsTrue();
            await Assert.That(area.GraphRenderCacheRasterizationCount).IsEqualTo(rasterizations);
            await Assert.That(visible.IsVisible).IsFalse();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task CachedGraphRendering_LeavesCachedMode_WhenASourceVisualChanges()
    {
        var (window, area, visible, _, _) = CreateScene(default);
        try
        {
            area.EnableGraphRenderCache = true;
            visible.Background = Brushes.Crimson;

            await Assert.That(area.IsGraphRenderCacheActive).IsFalse();
            await Assert.That(visible.IsVisible).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task CachedGraphRendering_LeavesCachedMode_WhenGraphChildrenChange()
    {
        var (window, area, visible, _, _) = CreateScene(default);
        try
        {
            area.EnableGraphRenderCache = true;
            var added = AddItem(area, new Point(400, 300), Colors.Crimson);

            await Assert.That(area.IsGraphRenderCacheActive).IsFalse();
            await Assert.That(visible.IsVisible).IsTrue();
            await Assert.That(added.IsVisible).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task CachedGraphRendering_FallsBackToLiveRendering_WhenMaximumCacheSizeIsExceeded()
    {
        var (window, area, visible, _, _) = CreateScene(default);
        try
        {
            area.MaximumGraphRenderCacheBytes = 1;
            area.EnableGraphRenderCache = true;

            await Assert.That(area.IsGraphRenderCacheActive).IsFalse();
            await Assert.That(visible.IsVisible).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task CachedGraphRendering_DisablesViewportCullingUntilLiveRenderingIsRestored()
    {
        var (window, area, _, _, _) = CreateScene(default);
        try
        {
            var culledVertex = new VertexControl(new TestVertex("culled"))
            {
                Width = ItemSize,
                Height = ItemSize
            };
            GraphAreaBase.SetX(culledVertex, GraphWidth + 200);
            GraphAreaBase.SetY(culledVertex, GraphHeight + 200);
            GraphAreaBase.SetFinalX(culledVertex, GraphWidth + 200);
            GraphAreaBase.SetFinalY(culledVertex, GraphHeight + 200);
            area.Children.Add(culledVertex);
            PumpLayout(window);

            area.ViewportCulling.CullingMargin = 0;
            area.EnableViewportCulling = true;
            area.UpdateViewport(new Rect(0, 0, 100, 100));
            await Assert.That(culledVertex.IsVisible).IsFalse();

            area.EnableGraphRenderCache = true;

            await Assert.That(area.IsGraphRenderCacheActive).IsTrue();
            await Assert.That(area.WasVisibleBeforeGraphRenderCaching(culledVertex)).IsTrue();

            area.EnableGraphRenderCache = false;

            await Assert.That(area.EnableViewportCulling).IsTrue();
            await Assert.That(culledVertex.IsVisible).IsFalse();

            area.EnableGraphRenderCache = true;
            area.ViewportCulling.IsEnabled = false;
            area.EnableGraphRenderCache = false;

            await Assert.That(area.EnableViewportCulling).IsFalse();
            await Assert.That(culledVertex.IsVisible).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    [Arguments(100, 80, -100, -80)]
    public async Task CachedGraphRendering_MapsNonZeroAndNegativeContentOffsets(
        double contentX,
        double contentY,
        double translateX,
        double translateY)
    {
        var (window, area, _, _, zoomControl) = CreateScene(new Point(contentX, contentY));
        try
        {
            area.EnableGraphRenderCache = true;
            zoomControl.Mode = ZoomControlModes.Custom;
            zoomControl.TranslateX = translateX;
            zoomControl.TranslateY = translateY;
            Dispatcher.UIThread.RunJobs();

            await Assert.That(area.CachedGraphContentBounds).IsEqualTo(
                new Rect(contentX, contentY, GraphWidth, GraphHeight));
            var cache = area.GraphRenderCacheBitmap!;
            await Assert.That(PixelMatches(cache, 4, 4, Colors.Red)).IsTrue();
            await Assert.That(PixelMatches(cache, 764, 4, Colors.Lime)).IsTrue();
            await Assert.That(PixelMatches(cache, 4, 564, Colors.Blue)).IsTrue();
            await Assert.That(PixelMatches(cache, 764, 564, Colors.Gold)).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static (
        Window window,
        GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>> area,
        Border visible,
        Border hidden,
        ZoomControl zoomControl) CreateScene(Point contentTopLeft)
    {
        var area = new GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>();
        var visible = AddItem(area, contentTopLeft, Colors.Red);
        AddItem(area, contentTopLeft + new Vector(GraphWidth - ItemSize, 0), Colors.Lime);
        AddItem(area, contentTopLeft + new Vector(0, GraphHeight - ItemSize), Colors.Blue);
        AddItem(area, contentTopLeft + new Vector(GraphWidth - ItemSize, GraphHeight - ItemSize), Colors.Gold);
        var hidden = AddItem(area, contentTopLeft + new Vector(200, 200), Colors.Black);

        var zoomControl = new ZoomControl
        {
            Width = GraphWidth,
            Height = GraphHeight,
            Content = area
        };
        var window = new Window
        {
            Width = GraphWidth,
            Height = GraphHeight,
            Content = zoomControl
        };
        window.Show();
        PumpLayout(window);

        return (window, area, visible, hidden, zoomControl);
    }

    private static Border AddItem(
        GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>> area,
        Point position,
        Color color)
    {
        var item = new Border
        {
            Width = ItemSize,
            Height = ItemSize,
            Background = new SolidColorBrush(color)
        };
        GraphAreaBase.SetX(item, position.X);
        GraphAreaBase.SetY(item, position.Y);
        GraphAreaBase.SetFinalX(item, position.X);
        GraphAreaBase.SetFinalY(item, position.Y);
        area.Children.Add(item);
        return item;
    }

    private static RenderTargetBitmap Render(Control control)
    {
        var size = control.Bounds.Size;
        var image = new RenderTargetBitmap(
            new PixelSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)),
            new Vector(96, 96));
        image.Render(control);
        return image;
    }

    private static void PumpLayout(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static bool PixelMatches(RenderTargetBitmap image, int x, int y, Color color)
    {
        var stride = image.PixelSize.Width * 4;
        var pixels = new byte[stride * image.PixelSize.Height];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            image.CopyPixels(new PixelRect(image.PixelSize), handle.AddrOfPinnedObject(), pixels.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        var offset = y * stride + x * 4;
        return (pixels[offset] == color.B && pixels[offset + 1] == color.G && pixels[offset + 2] == color.R) ||
               (pixels[offset] == color.R && pixels[offset + 1] == color.G && pixels[offset + 2] == color.B);
    }

    private sealed class TestVertex(string name) : VertexBase
    {
        public override string ToString() => name;
    }

    private sealed class TestEdge(TestVertex source, TestVertex target) : EdgeBase<TestVertex>(source, target)
    {
        public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; }
    }
}
