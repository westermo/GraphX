using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Westermo.GraphX.Controls.Controls.ZoomControl;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// End-to-end render tests for <see cref="Wayfinder"/>. These tests rasterise
/// the minimap with the Skia headless backend (configured in
/// <see cref="GlobalHooks"/>) and snapshot the rendered output via
/// Verify.Avalonia. Unlike the pure-math tests in
/// <see cref="WayfinderTests"/>, these tests would have caught the original
/// "wayfinder only renders the top-left 10×10 of the content" bug, because
/// the snapshot includes the actual rendered pixels.
///
/// Image diffs are tolerated up to the threshold registered in
/// <see cref="ModuleInit"/> via <see cref="VerifyImageMagick"/>, so small
/// AA / Skia-version differences do not cause spurious failures.
/// </summary>
public class WayfinderRenderTests
{
    private const double WayfinderWidth = 200;
    private const double WayfinderHeight = 120;

    private static (Window window, ZoomControl zc, Wayfinder wf) BuildScene(
        Action<Panel> populateContent,
        double zoomContentWidth = 800,
        double zoomContentHeight = 600,
        double zoom = 1.0,
        double translateX = 0.0,
        double translateY = 0.0)
    {
        // Use a plain Canvas (not GraphArea) so the test does not depend on
        // GraphX layout algorithms. The wayfinder treats any Panel-derived
        // ZoomControl content the same way: it walks Children and draws a
        // glyph per child at its arranged position.
        var content = new Canvas
        {
            Width = zoomContentWidth,
            Height = zoomContentHeight,
            Background = Brushes.Transparent
        };
        populateContent(content);

        var zc = new ZoomControl
        {
            Width = 400,
            Height = 240,
            Content = content
        };

        var wf = new Wayfinder
        {
            Width = WayfinderWidth,
            Height = WayfinderHeight,
            ZoomControl = zc,
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            ShadowBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
            ViewportBrush = Brushes.Transparent,
            ViewportPen = new Pen(Brushes.Black, 1)
        };

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Children = { zc, wf }
        };

        var window = new Window
        {
            Width = 420,
            Height = 400,
            Background = Brushes.White,
            Content = root
        };
        window.Show();

        // Drive layout passes manually — TUnit dispatches us with priority
        // Send, so the layout/render pump won't otherwise tick.
        for (var i = 0; i < 3; i++)
        {
            window.Measure(new Size(420, 400));
            window.Arrange(new Rect(0, 0, 420, 400));
            Dispatcher.UIThread.RunJobs();
        }

        // Assign Zoom/Translate AFTER the template has been applied (i.e.
        // after the first arrange pass) — ZoomControl coerces TranslateX/Y
        // to 0 while its inner ZoomContentPresenter is null, so setting
        // these values during construction silently snaps them to zero.
        zc.Zoom = zoom;
        zc.TranslateX = translateX;
        zc.TranslateY = translateY;

        for (var i = 0; i < 2; i++)
        {
            window.Measure(new Size(420, 400));
            window.Arrange(new Rect(0, 0, 420, 400));
            Dispatcher.UIThread.RunJobs();
        }

        return (window, zc, wf);
    }

    private static VerifySettings GetSettings([CallerMemberName] string? testName = null)
    {
        var settings = new VerifySettings();
        if (testName is not null) settings.UseMethodName(testName);
        settings.UseTypeName(nameof(WayfinderRenderTests));
        return settings;
    }

    /// <summary>
    /// Renders five vertices spaced across the canvas at known positions.
    /// Regressions where the contents do not render at all (the original
    /// VisualBrush + sentinel-DesiredSize bug) will surface as a wayfinder
    /// containing only the viewport rectangle.
    /// </summary>
    [Test]
    public async Task Renders_ContentGlyphs_AcrossWayfinder()
    {
        var (window, _, wf) = BuildScene(canvas =>
        {
            // Place vertices at the four corners + centre of the canvas so
            // that the wayfinder must show ink at the left, right, top and
            // bottom of the content area to be considered correct.
            AddBox(canvas, 20, 20, 40, 40);
            AddBox(canvas, 740, 20, 40, 40);
            AddBox(canvas, 20, 540, 40, 40);
            AddBox(canvas, 740, 540, 40, 40);
            AddBox(canvas, 380, 280, 40, 40);
        });

        // Verify.Avalonia takes ownership of closing the window.
        await Verify(window, GetSettings());
    }

    /// <summary>
    /// Zoom in and pan all the way to the right of the content. The
    /// viewport rectangle in the wayfinder should be drawn against the
    /// rightmost edge of the content area — proving the per-axis clamp
    /// allows the indicator to reach the right side.
    /// </summary>
    [Test]
    public async Task Viewport_ReachesRightmostEdge_WhenPannedFully()
    {
        // Content is 800 wide; ZoomControl viewport is 400 wide. At Zoom=2
        // the on-screen visible content width is 200 — i.e. only a quarter
        // of the canvas is visible at a time. The TranslateX that pins the
        // rightmost edge of the content to the right of the viewport is:
        //   screen = c*zoom + tx  →  tx = 400 - 800*2 = -1200.
        var (window, _, wf) = BuildScene(
            canvas =>
            {
                AddBox(canvas, 20, 280, 40, 40);
                AddBox(canvas, 380, 280, 40, 40);
                AddBox(canvas, 740, 280, 40, 40);
            },
            zoom: 2.0,
            translateX: -1200,
            translateY: -360);

        // Verify.Avalonia takes ownership of closing the window.
        await Verify(window, GetSettings());
    }

    /// <summary>
    /// Same scene, but panned all the way to the left. Confirms the
    /// indicator reaches the left edge — symmetric counterpart to the
    /// rightmost test.
    /// </summary>
    [Test]
    public async Task Viewport_ReachesLeftmostEdge_WhenPannedFully()
    {
        var (window, _, wf) = BuildScene(
            canvas =>
            {
                AddBox(canvas, 20, 280, 40, 40);
                AddBox(canvas, 380, 280, 40, 40);
                AddBox(canvas, 740, 280, 40, 40);
            },
            zoom: 2.0,
            translateX: 0,
            translateY: 0);

        // Verify.Avalonia takes ownership of closing the window.
        await Verify(window, GetSettings());
    }

    private static void AddBox(Panel canvas, double x, double y, double w, double h)
    {
        var rect = new Rectangle
        {
            Width = w,
            Height = h,
            Fill = Brushes.SteelBlue
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }
}
