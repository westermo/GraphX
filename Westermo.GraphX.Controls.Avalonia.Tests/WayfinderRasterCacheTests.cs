using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Runtime.InteropServices;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Exercises the Wayfinder's cached content layer independently from its
/// viewport overlay. These tests use the Skia headless backend configured by
/// <see cref="GlobalHooks"/> and intentionally render the control repeatedly.
/// </summary>
public sealed class WayfinderRasterCacheTests
{
    private const double WayfinderWidth = 200;
    private const double WayfinderHeight = 180;

    [Test]
    public async Task ContentCache_UsesPhysicalPixels_AndIsReusedForPanAndZoom()
    {
        var (window, zoomControl, _, wayfinder) = CreateScene();
        try
        {
            using var initialImage = Render(wayfinder);

            var initialRasterizations = wayfinder.CacheRasterizationCount;
            var scaling = TopLevel.GetTopLevel(wayfinder)!.RenderScaling;
            var expectedPixelSize = new PixelSize(
                (int)Math.Ceiling(wayfinder.ContentBounds.Width * scaling),
                (int)Math.Ceiling(wayfinder.ContentBounds.Height * scaling));

            await Assert.That(initialRasterizations > 0).IsTrue();
            await Assert.That(wayfinder.CachedContentPixelSize).IsEqualTo(expectedPixelSize);

            zoomControl.Mode = ZoomControlModes.Custom;
            zoomControl.Zoom = 1.75;
            zoomControl.TranslateX = -160;
            zoomControl.TranslateY = -90;

            using var pannedAndZoomedImage = Render(wayfinder);

            await Assert.That(wayfinder.CacheRasterizationCount).IsEqualTo(initialRasterizations);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task ContentCache_IsDisposedAndRerasterized_WhenContentLayoutChanges()
    {
        var (window, _, content, wayfinder) = CreateScene();
        try
        {
            using var initialImage = Render(wayfinder);
            var initialRasterizations = wayfinder.CacheRasterizationCount;

            content.Children.Add(new Rectangle
            {
                Width = 60,
                Height = 40,
                Fill = Brushes.Crimson
            });
            Canvas.SetLeft(content.Children[^1], 700);
            Canvas.SetTop(content.Children[^1], 520);
            PumpLayout(window);

            using var updatedImage = Render(wayfinder);

            await Assert.That(wayfinder.CacheRasterizationCount).IsEqualTo(initialRasterizations + 1);
            await Assert.That(wayfinder.HasContentCache).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task ContentCache_IsRerasterized_WhenSourceVisualOnlyChangesRendering()
    {
        var (window, _, content, wayfinder) = CreateScene();
        try
        {
            using var initialImage = Render(wayfinder);
            var initialRasterizations = wayfinder.CacheRasterizationCount;

            ((Rectangle)content.Children[0]).Fill = Brushes.Crimson;

            using var updatedImage = Render(wayfinder);

            await Assert.That(wayfinder.CacheRasterizationCount).IsEqualTo(initialRasterizations + 1);
            await Assert.That(ContainsColor(updatedImage, Colors.Crimson)).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task ContentCache_KeepsExistingDescendantSubscriptions_WhenLayoutIsUpdated()
    {
        var (window, _, _, wayfinder) = CreateScene();
        try
        {
            using var image = Render(wayfinder);
            var initialSubscriptionChanges = wayfinder.SourceVisualSubscriptionChangeCount;

            PumpLayout(window);

            await Assert.That(wayfinder.SourceVisualSubscriptionChangeCount)
                .IsEqualTo(initialSubscriptionChanges);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task ContentCache_IsDisposed_WhenZoomControlIsDetached()
    {
        var (window, _, _, wayfinder) = CreateScene();
        try
        {
            using var image = Render(wayfinder);
            await Assert.That(wayfinder.HasContentCache).IsTrue();

            wayfinder.ZoomControl = null;

            await Assert.That(wayfinder.HasContentCache).IsFalse();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Test]
    public async Task ContentCache_IsDiscarded_ForWayfinderSizeAndSourceChanges()
    {
        var (window, zoomControl, _, wayfinder) = CreateScene();
        try
        {
            using var initialImage = Render(wayfinder);
            var initialRasterizations = wayfinder.CacheRasterizationCount;
            var initialPixelSize = wayfinder.CachedContentPixelSize;

            wayfinder.Width = 160;
            PumpLayout(window);

            using var resizedImage = Render(wayfinder);
            var resizedRasterizations = wayfinder.CacheRasterizationCount;

            await Assert.That(resizedRasterizations).IsEqualTo(initialRasterizations + 1);
            await Assert.That(wayfinder.CachedContentPixelSize).IsNotEqualTo(initialPixelSize);

            zoomControl.Content = new Canvas
            {
                Width = 800,
                Height = 600,
                Children =
                {
                    new Rectangle
                    {
                        Width = 50,
                        Height = 50,
                        Fill = Brushes.DarkOrange
                    }
                }
            };

            await Assert.That(wayfinder.HasContentCache).IsFalse();

            PumpLayout(window);
            using var replacementImage = Render(wayfinder);

            await Assert.That(wayfinder.CacheRasterizationCount).IsEqualTo(resizedRasterizations + 1);
            await Assert.That(ContainsColor(replacementImage, Colors.DarkOrange)).IsTrue();
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static (Window window, ZoomControl zoomControl, Canvas content, Wayfinder wayfinder) CreateScene()
    {
        var content = new Canvas
        {
            Width = 800,
            Height = 600
        };
        content.Children.Add(new Rectangle
        {
            Width = 40,
            Height = 40,
            Fill = Brushes.SteelBlue
        });
        Canvas.SetLeft(content.Children[0], 20);
        Canvas.SetTop(content.Children[0], 20);

        var zoomControl = new ZoomControl
        {
            Width = 400,
            Height = 240,
            Content = content
        };
        var wayfinder = new Wayfinder
        {
            Width = WayfinderWidth,
            Height = WayfinderHeight,
            ZoomControl = zoomControl,
            Background = Brushes.White,
            ShadowBrush = null,
            ViewportBrush = Brushes.Transparent,
            ViewportPen = null
        };
        var window = new Window
        {
            Width = 420,
            Height = 440,
            Content = new StackPanel
            {
                Spacing = 12,
                Children = { zoomControl, wayfinder }
            }
        };
        window.Show();
        PumpLayout(window);
        return (window, zoomControl, content, wayfinder);
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

    private static void PumpLayout(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static bool ContainsColor(RenderTargetBitmap image, Color color)
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

        for (var i = 0; i < pixels.Length; i += 4)
        {
            // Avalonia.Skia uses BGRA, but accept RGBA so this remains a
            // render-contract test rather than a backend-format test.
            if ((pixels[i] == color.B && pixels[i + 1] == color.G && pixels[i + 2] == color.R) ||
                (pixels[i] == color.R && pixels[i + 1] == color.G && pixels[i + 2] == color.B))
                return true;
        }

        return false;
    }
}
