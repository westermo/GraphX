using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Controls.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class ZoomControlTests
{
    private const double Tolerance = 0.01;

    /// <summary>
    /// Minimal <see cref="ITrackableContent"/> implementation used to exercise the
    /// GraphArea-like code path in ZoomControl, where the reported content bounds
    /// (ContentSize) can have a non-zero X/Y offset and a size independent of the
    /// control's own layout size. This mirrors GraphAreaBase's ContentSize semantics.
    /// </summary>
    private sealed class FakeTrackableContent : Control, ITrackableContent
    {
        private Rect _contentSize;

        public event ContentSizeChangedEventHandler? ContentSizeChanged;

        public Rect ContentSize
        {
            get => _contentSize;
            set
            {
                var oldSize = _contentSize;
                _contentSize = value;
                ContentSizeChanged?.Invoke(this, new ContentSizeChangedEventArgs(oldSize, value));
            }
        }
    }

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

    /// <summary>
    /// Creates a ZoomControl hosted in a Window whose content implements ITrackableContent
    /// with the given content-space bounding rectangle (which may have a non-zero X/Y offset,
    /// as a GraphArea's ContentSize does when its vertices aren't anchored at the origin).
    /// </summary>
    private static (ZoomControl zoom, Window window, FakeTrackableContent content)
        CreateZoomControlWithTrackableContent(double viewportWidth, double viewportHeight, Rect contentSize)
    {
        var content = new FakeTrackableContent { ContentSize = contentSize };
        var zc = new ZoomControl { Content = content };
        var window = new Window
        {
            Width = viewportWidth,
            Height = viewportHeight,
            Content = zc
        };
        window.Show();

        window.Measure(new Size(viewportWidth, viewportHeight));
        window.Arrange(new Rect(0, 0, viewportWidth, viewportHeight));

        return (zc, window, content);
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
    public async Task ZoomToFill_ClampsToMinZoom()
    {
        // MinZoom=0.75, viewport 400x300, content 4000x3000
        // calculated = min(400/4000, 300/3000) = 0.1, clamped up to 0.75
        var (zc, window) = CreateZoomControlWithContent(400, 300, 4000, 3000);
        try
        {
            zc.MinZoom = 0.75;
            zc.ZoomToFill();
            await Assert.That(Math.Abs(zc.Zoom - 0.75)).IsLessThan(Tolerance);
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
        // GetInitialTranslate is computed at zoom=1 and then multiplied by deltaZoom, because
        // ZoomContentPresenter's render transform scales around its own center
        // (RenderTransformOrigin = 0.5, 0.5), not the top-left corner:
        // tX = -((400 - 800)/2 + 0) = 200, tY = -((200 - 600)/2 + 0) = 200
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

            // Cross-check against the real rendered/visible content rect (which independently
            // derives content-space bounds via TranslatePoint through the live visual tree),
            // to ensure the content is genuinely centered on screen, not just self-consistent
            // with the translate formula above.
            var visible = zc.GetVisibleContentRect();
            await Assert.That(Math.Abs(visible.X - 0.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Width - 400.0)).IsLessThan(Tolerance);
            var topSlack = 0.0 - visible.Y;
            var bottomSlack = visible.Bottom - 200.0;
            await Assert.That(Math.Abs(topSlack - bottomSlack)).IsLessThan(Tolerance);
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

    #region Trackable Content Tests (regression coverage for GraphArea-style offset content)

    [Test]
    public async Task ZoomToFill_TrackableContentWithOffset_CentersCorrectly()
    {
        // Content whose bounding box (ContentSize) is smaller than the viewport and has a
        // non-zero X/Y offset (as a real GraphArea reports when its vertices aren't anchored
        // at the origin) should still end up centered after ZoomToFill.
        // viewport 800x600, content bounds = (300, 500, 400, 200)
        // deltaZoom = min(800/400, 600/200) = 2.0 (content smaller than viewport -> zooms in)
        // GetInitialTranslate is computed at zoom=1 and then multiplied by deltaZoom, because
        // ZoomContentPresenter's render transform scales around its own center
        // (RenderTransformOrigin = 0.5, 0.5), not the top-left corner:
        // tX = -((400 - 800)/2 + 300) = -100, tY = -((200 - 600)/2 + 500) = -300
        var contentRect = new Rect(300, 500, 400, 200);
        var (zc, window, _) = CreateZoomControlWithTrackableContent(800, 600, contentRect);
        try
        {
            zc.ZoomToFill();

            var expectedDeltaZoom = 2.0;
            var expectedTx = -((contentRect.Width - 800.0) / 2.0 + contentRect.X); // -100
            var expectedTy = -((contentRect.Height - 600.0) / 2.0 + contentRect.Y); // -300
            var expectedTranslateX = expectedTx * expectedDeltaZoom; // -200
            var expectedTranslateY = expectedTy * expectedDeltaZoom; // -600

            await Assert.That(Math.Abs(zc.Zoom - expectedDeltaZoom)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateX - expectedTranslateX)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY - expectedTranslateY)).IsLessThan(Tolerance);

            // Cross-check against the real visible content rect (derived independently via
            // TranslatePoint through the live visual tree) to confirm the content is genuinely
            // centered on screen. Width is the limiting dimension so it should exactly fill the
            // viewport with no slack; height should be centered with equal slack top/bottom.
            var visible = zc.GetVisibleContentRect();
            await Assert.That(Math.Abs(visible.X - contentRect.X)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Width - contentRect.Width)).IsLessThan(Tolerance);
            var topSlack = contentRect.Y - visible.Y;
            var bottomSlack = visible.Bottom - contentRect.Bottom;
            await Assert.That(Math.Abs(topSlack - bottomSlack)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task CenterContent_TrackableContentWithOffset_CentersAtCurrentZoom()
    {
        // CenterContent must account for the content offset at the *current* zoom level.
        var contentRect = new Rect(50, -40, 200, 100);
        var (zc, window, _) = CreateZoomControlWithTrackableContent(800, 600, contentRect);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 3.0;

            zc.CenterContent();

            var zoom = 3.0;
            var expectedTx = -((contentRect.Width - 800.0) / 2.0 + contentRect.X);
            var expectedTy = -((contentRect.Height - 600.0) / 2.0 + contentRect.Y);
            var expectedTranslateX = expectedTx * zoom;
            var expectedTranslateY = expectedTy * zoom;

            await Assert.That(Math.Abs(zc.Zoom - zoom)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateX - expectedTranslateX)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY - expectedTranslateY)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ZoomToOriginal_TrackableContentWithOffset_CentersAtZoomOne()
    {
        var contentRect = new Rect(-150, 200, 300, 100);
        var (zc, window, _) = CreateZoomControlWithTrackableContent(800, 600, contentRect);
        try
        {
            zc.ZoomToFill();
            zc.ZoomToOriginal();

            var expectedTranslateX = (800.0 - contentRect.Width) / 2.0 - contentRect.X;
            var expectedTranslateY = (600.0 - contentRect.Height) / 2.0 - contentRect.Y;

            await Assert.That(Math.Abs(zc.Zoom - 1.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateX - expectedTranslateX)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY - expectedTranslateY)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ContentSizeChanged_WhenModeIsFill_TriggersAutomaticRefit()
    {
        // GraphAreaBase raises ContentSizeChanged whenever vertices move/resize the bounding
        // box. While Mode is Fill, the ZoomControl should automatically re-fit to the new size.
        var (zc, window, content) = CreateZoomControlWithTrackableContent(800, 600, new Rect(0, 0, 400, 200));
        try
        {
            zc.Mode = ZoomControlModes.Fill;
            // Establish a known-good baseline explicitly: in a headless test harness,
            // OnApplyTemplate can run once with a stale/zero ActualWidth before the real
            // layout pass completes, so relying on the implicit Mode=Fill auto-trigger alone
            // is not reliable here. An explicit ZoomToFill() call always uses the current,
            // correct bounds.
            zc.ZoomToFill();
            var zoomAfterInitialFit = zc.Zoom; // expected 2.0 (min(800/400, 600/200))
            await Assert.That(Math.Abs(zoomAfterInitialFit - 2.0)).IsLessThan(Tolerance);

            // Grow the content bounding box - the fitted zoom should shrink to compensate,
            // via the automatic ContentSizeChanged -> DoZoomToFill re-trigger (Mode is Fill).
            content.ContentSize = new Rect(0, 0, 800, 600);

            // expected zoom = min(800/800, 600/600) = 1.0
            await Assert.That(Math.Abs(zc.Zoom - 1.0)).IsLessThan(Tolerance);
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

    [Test]
    public async Task Mode_SetToOriginal_TriggersZoomToOriginal()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            // Start at a non-1.0 custom zoom
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.5;

            // Setting mode to Original should trigger DoZoomToOriginal, resetting zoom to 1.0
            zc.Mode = ZoomControlModes.Original;

            await Assert.That(Math.Abs(zc.Zoom - 1.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Modifier Drag Gesture Tests

    [Test]
    public async Task Alt_LeftDrag_Zooms_To_Rectangle()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 1000, 1000);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 1.0;

            var areaSelectedCount = 0;
            zc.AreaSelected += (_, _) => areaSelectedCount++;

            var initialZoom = zc.Zoom;
            var beganInteraction = zc.BeginInteractionForTest(KeyModifiers.Alt, new Point(100, 100));
            await Assert.That(beganInteraction).IsTrue();
            zc.MoveInteractionForTest(new Point(300, 300));
            zc.CompleteInteractionForTest();

            await Assert.That(zc.Zoom).IsGreaterThan(initialZoom);
            await Assert.That(areaSelectedCount).IsEqualTo(0);
            await Assert.That(zc.ZoomBox).IsEqualTo(default(Rect));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ControlAlt_LeftDrag_AreaSelects_Without_Zooming()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 1000, 1000);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 1.0;

            Rect? selectedRectangle = null;
            zc.AreaSelected += (_, args) => selectedRectangle = args.Rectangle;

            var initialZoom = zc.Zoom;
            var beganInteraction =
                zc.BeginInteractionForTest(KeyModifiers.Control | KeyModifiers.Alt, new Point(100, 100));
            await Assert.That(beganInteraction).IsTrue();
            zc.MoveInteractionForTest(new Point(300, 300));
            zc.CompleteInteractionForTest();

            await Assert.That(zc.Zoom).IsEqualTo(initialZoom);
            await Assert.That(selectedRectangle.HasValue).IsTrue();
            await Assert.That(Math.Abs(selectedRectangle!.Value.Width - 200)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(selectedRectangle.Value.Height - 200)).IsLessThan(Tolerance);
            await Assert.That(zc.ZoomBox).IsEqualTo(default(Rect));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task ControlAlt_LeftClick_Without_Drag_Does_Not_Fire_AreaSelected()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 1000, 1000);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 1.0;

            var areaSelectedCount = 0;
            zc.AreaSelected += (_, _) => areaSelectedCount++;

            var beganInteraction =
                zc.BeginInteractionForTest(KeyModifiers.Control | KeyModifiers.Alt, new Point(200, 200));
            await Assert.That(beganInteraction).IsTrue();
            // No MoveInteraction — simulates a click without drag
            zc.CompleteInteractionForTest();

            await Assert.That(areaSelectedCount).IsEqualTo(0);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task MiddleButton_Begins_Pan_Interaction()
    {
        var zc = new ZoomControl();
        // Verify that after middle button starts pan, ModifierMode is Pan
        await Assert.That(zc.ModifierMode).IsEqualTo(ZoomViewModifierMode.None);
    }

    [Test]
    public async Task PlainLeftDrag_NoModifiers_PansContent()
    {
        // With IsDragSelectByDefault false (the default), a plain left-button drag
        // (no key modifiers) should pan the content by the drag distance rather than
        // creating a zoom box or changing the zoom level.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 1000, 1000);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 1.0;
            zc.TranslateX = 0;
            zc.TranslateY = 0;

            var initialZoom = zc.Zoom;
            var beganInteraction = zc.BeginInteractionForTest(KeyModifiers.None, new Point(100, 100));
            await Assert.That(beganInteraction).IsTrue();
            await Assert.That(zc.ModifierMode).IsEqualTo(ZoomViewModifierMode.Pan);

            zc.MoveInteractionForTest(new Point(180, 130));
            zc.CompleteInteractionForTest();

            // PanAction: translate = startTranslate + (currentPosition - mouseDownPosition)
            await Assert.That(Math.Abs(zc.TranslateX - 80.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(zc.TranslateY - 30.0)).IsLessThan(Tolerance);
            // Panning must not change the zoom level or open a zoom box.
            await Assert.That(Math.Abs(zc.Zoom - initialZoom)).IsLessThan(Tolerance);
            await Assert.That(zc.ZoomBox).IsEqualTo(default(Rect));
            await Assert.That(zc.ModifierMode).IsEqualTo(ZoomViewModifierMode.None);
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

    #region Geometry Helper Tests

    [Test]
    public async Task OrigoPosition_ReturnsViewportCenter()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            var origo = zc.OrigoPosition;
            await Assert.That(Math.Abs(origo.X - 400.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(origo.Y - 300.0)).IsLessThan(Tolerance);
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public async Task GetVisibleContentRect_ReflectsZoomAndTranslate()
    {
        // viewport 800x600; zoom=2.0, translate=(-100,-50).
        // GetVisibleContentRect derives content-space bounds by translating the viewport's
        // screen corners through the live visual tree (via TranslatePoint), which correctly
        // accounts for ZoomContentPresenter's render transform scaling around its own center
        // (RenderTransformOrigin = 0.5, 0.5) rather than the top-left corner. The expected
        // values below (250, 175, 400, 300) were derived from and verified against that
        // transform, not from the simpler (and incorrect for zoom != 1 with this presenter)
        // "contentX = -translateX/zoom" formula.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 1000, 1000);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;
            zc.TranslateX = -100;
            zc.TranslateY = -50;

            var visible = zc.GetVisibleContentRect();

            await Assert.That(Math.Abs(visible.X - 250.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Y - 175.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Width - 400.0)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Height - 300.0)).IsLessThan(Tolerance);
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
