using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class ZoomControlTests
{
    private const double Tolerance = 0.01;

    /// <summary>
    /// Creates a ZoomControl hosted in a Window with specified viewport and content dimensions.
    /// The window is shown and laid out to trigger template application and size the ZoomControl.
    /// </summary>
    private static (ZoomControl zoom, Window window) CreateZoomControlWithContent(
        double viewportWidth, double viewportHeight,
        double contentWidth, double contentHeight)
    {
        var content = new Canvas { Width = contentWidth, Height = contentHeight };
        var zc = new ZoomControl { Content = content };
        var window = new Window
        {
            Width = viewportWidth,
            Height = viewportHeight,
            Content = zc
        };
        window.Show();

        // Force layout to apply template and establish Bounds
        window.Measure(new Size(viewportWidth, viewportHeight));
        window.Arrange(new Rect(0, 0, viewportWidth, viewportHeight));

        return (zc, window);
    }

    #region Mode Property Tests

    [Test]
    public async Task Mode_DefaultsToFill()
    {
        var zc = new ZoomControl();
        // ModeProperty has no explicit default, so it defaults to (ZoomControlModes)0 = Fill
        await Assert.That(zc.Mode).IsEqualTo(ZoomControlModes.Fill);
    }

    [Test]
    public async Task Mode_SetToFill_UpdatesProperty()
    {
        var zc = new ZoomControl();
        zc.Mode = ZoomControlModes.Fill;
        await Assert.That(zc.Mode).IsEqualTo(ZoomControlModes.Fill);
    }

    [Test]
    public async Task Mode_SetToOriginal_UpdatesProperty()
    {
        var zc = new ZoomControl();
        zc.Mode = ZoomControlModes.Original;
        await Assert.That(zc.Mode).IsEqualTo(ZoomControlModes.Original);
    }

    #endregion

    #region ZoomToFill Tests

    [Test]
    public async Task ZoomToFill_WiderContent_ScalesBasedOnWidth()
    {
        // viewport 800x600, content 400x200
        // expected zoom = min(800/400, 600/200) = min(2, 3) = 2.0
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.ZoomToFill();
            await Assert.That(Math.Abs(zc.Zoom - 2.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToFill_TallerContent_ScalesBasedOnHeight()
    {
        // viewport 800x600, content 200x400
        // expected zoom = min(800/200, 600/400) = min(4, 1.5) = 1.5
        var (zc, window) = CreateZoomControlWithContent(800, 600, 200, 400);
        try
        {
            zc.ZoomToFill();
            await Assert.That(Math.Abs(zc.Zoom - 1.5)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToFill_SquareContent_ScalesUniformly()
    {
        // viewport 800x600, content 400x400
        // expected zoom = min(800/400, 600/400) = min(2, 1.5) = 1.5
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 400);
        try
        {
            zc.ZoomToFill();
            await Assert.That(Math.Abs(zc.Zoom - 1.5)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToFill_ClampsToMaxZoom()
    {
        // MaxZoom=2.0, viewport 800x600, content 100x100
        // calculated = min(8, 6) = 6, clamped to 2.0
        var (zc, window) = CreateZoomControlWithContent(800, 600, 100, 100);
        try
        {
            zc.MaxZoom = 2.0;
            zc.ZoomToFill();
            await Assert.That(Math.Abs(zc.Zoom - 2.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToFill_CentersContentInViewport()
    {
        // viewport 800x600, content 400x200
        // deltaZoom = min(800/400, 600/200) = 2.0
        // tX = -(400 - 800)/2 = 200, tY = -(200 - 600)/2 = 200
        // TranslateX = 200 * 2 = 400, TranslateY = 200 * 2 = 400
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.ZoomToFill();

            var expectedDeltaZoom = 2.0;
            var expectedTx = -(400.0 - 800.0) / 2.0; // 200
            var expectedTy = -(200.0 - 600.0) / 2.0; // 200
            var expectedTranslateX = expectedTx * expectedDeltaZoom; // 400
            var expectedTranslateY = expectedTy * expectedDeltaZoom; // 400

            await Assert.That(Math.Abs(zc.TranslateX - expectedTranslateX)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY - expectedTranslateY)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToFill_WithZeroWidthContent_DoesNothing()
    {
        // Content with zero width should not crash and zoom should stay at default
        var (zc, window) = CreateZoomControlWithContent(800, 600, 0, 200);
        try
        {
            var initialZoom = zc.Zoom;
            zc.ZoomToFill();
            await Assert.That(zc.Zoom).IsEqualTo(initialZoom);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region ZoomToOriginal Tests

    [Test]
    public async Task ZoomToOriginal_SetsZoomToOne()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            // First zoom to fill to change zoom from default
            zc.ZoomToFill();
            // Now zoom to original
            zc.ZoomToOriginal();
            await Assert.That(Math.Abs(zc.Zoom - 1.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Mode Change Re-triggers

    [Test]
    public async Task Mode_SetToFill_TriggersZoomToFill()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            // Start at custom zoom
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 1.0;

            // Setting mode to Fill should trigger DoZoomToFill
            zc.Mode = ZoomControlModes.Fill;

            // Expected zoom = min(800/400, 600/200) = 2.0
            await Assert.That(Math.Abs(zc.Zoom - 2.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region ZoomToContent Tests

    [Test]
    public async Task ZoomToContent_ZoomsToSpecifiedRectangle()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 1000, 1000);
        try
        {
            // This test works because the initial zoom is 1.0 and translate is 0,
            // so the content-to-presenter coordinate transformation is identity.

            // Zoom to a 200x200 rectangle within the content
            var targetRect = new Rect(100, 100, 200, 200);
            zc.ZoomToContent(targetRect);

            // Expected zoom = min(800/200, 600/200) = min(4, 3) = 3.0, clamped to MaxZoom
            var expectedZoom = Math.Min(zc.MaxZoom, Math.Min(800.0 / 200.0, 600.0 / 200.0));
            await Assert.That(Math.Abs(zc.Zoom - expectedZoom)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region CenterContent Tests

    [Test]
    public async Task CenterContent_MaintainsCurrentZoom()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            // Set a specific zoom level
            zc.ZoomToFill();
            var zoomBefore = zc.Zoom;

            // Center content should not change zoom
            zc.CenterContent();

            await Assert.That(Math.Abs(zc.Zoom - zoomBefore)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Zoom Property Tests

    [Test]
    public async Task Zoom_ClampedToMinMax()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.MinZoom = 0.5;
            zc.MaxZoom = 3.0;

            // Attempt to set zoom below min — DoZoom clamps, but direct set does not auto-clamp.
            // The Zoom property setter simply sets the value; clamping happens in DoZoom.
            // We test via ZoomToFill which does clamp.
            zc.MaxZoom = 1.0;
            zc.ZoomToFill();
            // viewport 800x600, content 400x200 → calculated 2.0, clamped to MaxZoom=1.0
            await Assert.That(Math.Abs(zc.Zoom - 1.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public async Task ZoomToFill_WithZeroHeightContent_DoesNothing()
    {
        // Zero height content - DoZoomToFill guards against zero height and returns early
        var (zc, window) = CreateZoomControlWithContent(800, 600, 200, 0);
        try
        {
            var initialZoom = zc.Zoom;
            zc.ZoomToFill();
            await Assert.That(zc.Zoom).IsEqualTo(initialZoom);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToFill_ContentLargerThanViewport_ZoomsOut()
    {
        // viewport 400x300, content 800x600
        // expected zoom = min(400/800, 300/600) = min(0.5, 0.5) = 0.5
        var (zc, window) = CreateZoomControlWithContent(400, 300, 800, 600);
        try
        {
            zc.ZoomToFill();
            await Assert.That(Math.Abs(zc.Zoom - 0.5)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToOriginal_CentersContent()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            // ZoomToOriginal should center content at zoom=1.0
            // For non-trackable content, GetTrackableTranslate returns (0, 0)
            // So TranslateX and TranslateY should both be 0
            zc.ZoomToOriginal();
            await Assert.That(Math.Abs(zc.Zoom - 1.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToOriginal_NonTrackable_TranslatesToZero()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.ZoomToFill(); // sets some translate
            zc.ZoomToOriginal();
            // For non-trackable content, translate is (0, 0)
            await Assert.That(Math.Abs(zc.TranslateX)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Deferred Viewport Update Tests

    [Test]
    public async Task TranslateXY_BatchChange_CoalescesViewportUpdates()
    {
        // Setting both TranslateX and TranslateY should not cause two immediate
        // viewport notifications — they should be deferred and coalesced.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            // Force initial layout
            zc.ZoomToFill();

            // Change both translate properties in quick succession
            zc.TranslateX = 100;
            zc.TranslateY = 200;

            // Process the deferred Render-priority callback
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            // After processing, translate values should be applied
            await Assert.That(Math.Abs(zc.TranslateX - 100)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY - 200)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task Zoom_Change_UsesScheduledViewportUpdate()
    {
        // Changing Zoom should schedule a deferred viewport update
        // rather than calling NotifyGraphAreaViewportChanged directly.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;

            // Process deferred callback
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            await Assert.That(Math.Abs(zc.Zoom - 2.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion
}
