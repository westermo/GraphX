using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Verifies that the Wayfinder samples GraphArea in graph coordinates rather
/// than using GraphArea's arranged size as the source coordinate system.
/// </summary>
public sealed class WayfinderGraphAreaRenderTests
{
    private const double VertexSize = 40;

    [Test]
    public async Task RendersPositiveOffsetGraphAreaAtTheCorrectMinimapCoordinates()
    {
        var (window, area, wayfinder) = CreateScene(new Point(100, 80));
        try
        {
            await Assert.That(area.ContentSize).IsEqualTo(new Rect(100, 80, 800, 600));
            await Assert.That(area.Bounds.Size).IsEqualTo(new Size(800, 600));
            await Assert.That(wayfinder.ContentBounds).IsEqualTo(new Rect(0, 0, 160, 120));

            using var image = Render(wayfinder);
            await AssertCornerPixels(image);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task RendersNegativeOffsetGraphAreaAtTheCorrectMinimapCoordinates()
    {
        var (window, area, wayfinder) = CreateScene(new Point(-100, -80));
        try
        {
            await Assert.That(area.ContentSize).IsEqualTo(new Rect(-100, -80, 800, 600));
            await Assert.That(area.Bounds.Size).IsEqualTo(new Size(800, 600));
            await Assert.That(wayfinder.ContentBounds).IsEqualTo(new Rect(0, 0, 160, 120));

            using var image = Render(wayfinder);
            await AssertCornerPixels(image);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    [Arguments(100, 80)]
    [Arguments(-100, -80)]
    public async Task RendersCachedGraphAreaAtTheCorrectMinimapCoordinates(double contentX, double contentY)
    {
        var (window, area, wayfinder) = CreateScene(new Point(contentX, contentY));
        try
        {
            area.EnableGraphRenderCache = true;

            await Assert.That(area.IsGraphRenderCacheActive).IsTrue();
            using var image = Render(wayfinder);
            await AssertCornerPixels(image);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static (Window window,
        GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>> area,
        Wayfinder wayfinder) CreateScene(Point contentTopLeft)
    {
        var area = new GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>();
        AddVertex(area, "top-left", contentTopLeft, Colors.Red);
        AddVertex(area, "top-right", contentTopLeft + new Vector(760, 0), Colors.Lime);
        AddVertex(area, "bottom-left", contentTopLeft + new Vector(0, 560), Colors.Blue);
        AddVertex(area, "bottom-right", contentTopLeft + new Vector(760, 560), Colors.Gold);

        var zoomControl = new ZoomControl
        {
            Width = 400,
            Height = 240,
            Content = area
        };
        var wayfinder = new Wayfinder
        {
            Width = 200,
            Height = 120,
            ZoomControl = zoomControl,
            Background = Brushes.White,
            ShadowBrush = null,
            ViewportBrush = Brushes.Transparent,
            ViewportPen = null
        };
        var window = new Window
        {
            Width = 420,
            Height = 400,
            Content = new StackPanel
            {
                Spacing = 8,
                Children = { zoomControl, wayfinder }
            }
        };
        window.Show();

        for (var i = 0; i < 3; i++)
        {
            window.Measure(new Size(420, 400));
            window.Arrange(new Rect(0, 0, 420, 400));
            Dispatcher.UIThread.RunJobs();
        }

        return (window, area, wayfinder);
    }

    private static async Task AssertCornerPixels(RenderTargetBitmap image)
    {
        // The 200 x 120 minimap fits the 800 x 600 graph at scale 0.2.
        // Therefore, the four 40 x 40 vertices occupy the four 8 x 8 corners
        // of the 160 x 120 minimap content area.
        await Assert.That(PixelMatches(image, 4, 4, Colors.Red)).IsTrue();
        await Assert.That(PixelMatches(image, 156, 4, Colors.Lime)).IsTrue();
        await Assert.That(PixelMatches(image, 4, 116, Colors.Blue)).IsTrue();
        await Assert.That(PixelMatches(image, 156, 116, Colors.Gold)).IsTrue();
    }

    private static void AddVertex(
        GraphArea<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>> area,
        string name,
        Point position,
        Color color)
    {
        var vertex = new TestVertex(name);
        var control = new VertexControl(vertex)
        {
            Width = VertexSize,
            Height = VertexSize,
            Template = CreateVertexTemplate(color)
        };
        control.SetPosition(position);
        GraphAreaBase.SetFinalX(control, position.X);
        GraphAreaBase.SetFinalY(control, position.Y);
        area.AddVertex(vertex, control);
    }

    private static ControlTemplate CreateVertexTemplate(Color color)
    {
        var brush = new SolidColorBrush(color);
        return new ControlTemplate
        {
            TargetType = typeof(VertexControl),
            Content = new Func<IServiceProvider?, object?>(_ =>
            {
                var root = new Grid
                {
                    Name = "PART_vcproot",
                    Background = brush
                };
                var nameScope = new NameScope();
                nameScope.Register("PART_vcproot", root);
                return new TemplateResult<Control>(root, nameScope);
            })
        };
    }

    private static RenderTargetBitmap Render(Wayfinder wayfinder)
    {
        var size = wayfinder.Bounds.Size;
        var image = new RenderTargetBitmap(
            new PixelSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)),
            new Vector(96, 96));
        image.Render(wayfinder);
        return image;
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
