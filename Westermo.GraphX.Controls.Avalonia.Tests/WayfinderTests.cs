using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Controls.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Verifies the functional requirements of the Wayfinder add-on control.
///
/// Functional requirements covered here:
///   1. Attach/detach to a ZoomControl via the ZoomControl styled property.
///   2. Multiple wayfinders may share a single target.
///   3. Pure math: ComputeScale fits content uniformly inside the wayfinder.
///   4. Pure math: ContentBounds scales content size by Scale.
///   5. Pure math: ViewportRect maps the visible content region into wayfinder space.
///   6. Drag inside the viewport pans the target ZoomControl proportionally to 1/Scale.
///   7. Drag is a no-op when the full content is already visible (zoomed out).
///   8. Click outside the viewport but inside content bounds recenters the viewport.
///   9. Default visual properties have non-null sensible defaults.
///  10. Setting ZoomControl back to null detaches without crashing on subsequent zoom changes.
/// </summary>
public class WayfinderTests
{
    private const double Tolerance = 0.01;

    private static (ZoomControl zoom, Window window) CreateZoomControlWithContent(
        double viewportWidth, double viewportHeight,
        double contentWidth, double contentHeight)
    {
        var content = new Canvas { Width = contentWidth, Height = contentHeight };
        var zc = new ZoomControl { Content = content };
        var window = new Window { Width = viewportWidth, Height = viewportHeight, Content = zc };
        window.Show();
        window.Measure(new Size(viewportWidth, viewportHeight));
        window.Arrange(new Rect(0, 0, viewportWidth, viewportHeight));
        return (zc, window);
    }

    private static (ZoomControl zoom, Window window) CreateZoomControlWithOffsetContent(
        double viewportWidth, double viewportHeight, Rect contentRect)
    {
        // GraphArea reports an extent in graph coordinates, which may begin away
        // from the visual origin. Match that contract for pointer navigation tests.
        var content = new TrackableCanvas(contentRect)
        {
            Width = contentRect.Right,
            Height = contentRect.Bottom
        };
        var zc = new ZoomControl { Content = content };
        var window = new Window { Width = viewportWidth, Height = viewportHeight, Content = zc };
        window.Show();
        window.Measure(new Size(viewportWidth, viewportHeight));
        window.Arrange(new Rect(0, 0, viewportWidth, viewportHeight));
        return (zc, window);
    }

    #region Pure geometry helpers

    [Test]
    public async Task ComputeScale_FitsUniform_BasedOnLimitingDimension()
    {
        // Wayfinder 100x100, content 400x200. Limiting axis is width: 100/400 = 0.25.
        var scale = WayfinderGeometry.ComputeScale(new Size(400, 200), new Size(100, 100));
        await Assert.That(Math.Abs(scale - 0.25)).IsLessThan(Tolerance);
    }

    [Test]
    public async Task ComputeScale_TallContent_LimitedByHeight()
    {
        // Wayfinder 100x100, content 200x400. Limiting axis is height: 100/400 = 0.25.
        var scale = WayfinderGeometry.ComputeScale(new Size(200, 400), new Size(100, 100));
        await Assert.That(Math.Abs(scale - 0.25)).IsLessThan(Tolerance);
    }

    [Test]
    public async Task ComputeScale_ZeroOrInvalidSize_ReturnsOne()
    {
        await Assert.That(WayfinderGeometry.ComputeScale(default, new Size(100, 100))).IsEqualTo(1.0);
        await Assert.That(WayfinderGeometry.ComputeScale(new Size(100, 100), default)).IsEqualTo(1.0);
    }

    [Test]
    public async Task ComputeContentBounds_ScalesContentSizeByScale()
    {
        // 400x200 at scale 0.25 → 100x50, top-left at origin.
        var bounds = WayfinderGeometry.ComputeContentBounds(new Size(400, 200), 0.25);
        await Assert.That(Math.Abs(bounds.Width - 100)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(bounds.Height - 50)).IsLessThan(Tolerance);
        await Assert.That(bounds.X).IsEqualTo(0.0);
        await Assert.That(bounds.Y).IsEqualTo(0.0);
    }

    [Test]
    public async Task ComputeViewportRect_AtZoomOneNoTranslate_EqualsZoomControlBoundsScaled()
    {
        // Wayfinder scale 0.25, ZC viewport 200x150, content larger so viewport is a strict subset.
        // At zoom=1, translate=(0,0), the visible content rect is (0, 0, 200, 150).
        // Mapped to wayfinder: (0, 0, 50, 37.5).
        var rect = WayfinderGeometry.ComputeViewportRect(
            zoom: 1.0, translateX: 0, translateY: 0,
            zoomControlSize: new Size(200, 150),
            scale: 0.25);
        await Assert.That(Math.Abs(rect.X)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(rect.Y)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(rect.Width - 50)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(rect.Height - 37.5)).IsLessThan(Tolerance);
    }

    [Test]
    public async Task ComputeViewportRect_WithZoomAndTranslate_ShrinksAndShifts()
    {
        // Zoom=2 means visible content area is half size: 100x75 in content coords.
        // ZoomContentPresenter transforms around its center. The visible rect
        // therefore also includes half of the viewport's zoom delta:
        // (-translate/zoom + viewport/2 * (1 - 1/zoom)) = (150, 87.5).
        // Mapped at scale 0.25 → (37.5, 21.875, 25, 18.75).
        var rect = WayfinderGeometry.ComputeViewportRect(
            zoom: 2.0, translateX: -200, translateY: -100,
            zoomControlSize: new Size(200, 150),
            scale: 0.25);
        await Assert.That(Math.Abs(rect.X - 37.5)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(rect.Y - 21.875)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(rect.Width - 25)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(rect.Height - 18.75)).IsLessThan(Tolerance);
    }

    [Test]
    public async Task GetVisibleContentRect_MatchesTransformedViewportCorners()
    {
        var (zc, window) = CreateZoomControlWithContent(400, 300, 800, 600);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;
            zc.TranslateX = -80;
            zc.TranslateY = -40;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            var visual = zc.ContentVisual!;
            var expectedTopLeft = zc.TranslatePoint(default, visual)!.Value;
            var expectedBottomRight = zc.TranslatePoint(new Point(zc.Bounds.Width, zc.Bounds.Height), visual)!.Value;
            var visible = zc.GetVisibleContentRect();

            await Assert.That(Math.Abs(visible.X - expectedTopLeft.X)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Y - expectedTopLeft.Y)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Right - expectedBottomRight.X)).IsLessThan(Tolerance);
            await Assert.That(Math.Abs(visible.Bottom - expectedBottomRight.Y)).IsLessThan(Tolerance);
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task ClampDragDelta_WhenContentFullyVisible_ReturnsZero()
    {
        // Viewport fully encloses content → no drag possible.
        var content = new Rect(0, 0, 100, 100);
        var viewport = new Rect(-10, -10, 200, 200);
        var clamped = WayfinderGeometry.ClampDragDelta(viewport, content, new Vector(50, 50));
        await Assert.That(clamped.X).IsEqualTo(0.0);
        await Assert.That(clamped.Y).IsEqualTo(0.0);
    }

    [Test]
    public async Task ClampDragDelta_WhenViewportSmaller_AllowsMoveWithinBounds()
    {
        // Content (0,0,100,100), viewport (10,10,30,30). Move +50,+50 → clamped to keep viewport inside.
        var content = new Rect(0, 0, 100, 100);
        var viewport = new Rect(10, 10, 30, 30);
        var clamped = WayfinderGeometry.ClampDragDelta(viewport, content, new Vector(50, 50));
        // Max allowed dx so viewport.Right <= content.Right: 100 - (10+30) = 60. 50 <= 60 → 50.
        await Assert.That(Math.Abs(clamped.X - 50)).IsLessThan(Tolerance);
        await Assert.That(Math.Abs(clamped.Y - 50)).IsLessThan(Tolerance);
    }

    [Test]
    public async Task ClampDragDelta_OvershootsRight_IsClampedToBoundary()
    {
        var content = new Rect(0, 0, 100, 100);
        var viewport = new Rect(50, 50, 30, 30); // right = 80
        var clamped = WayfinderGeometry.ClampDragDelta(viewport, content, new Vector(50, 0));
        // Max dx = 100 - 80 = 20.
        await Assert.That(Math.Abs(clamped.X - 20)).IsLessThan(Tolerance);
    }

    [Test]
    public async Task ClampDragDelta_OvershootsLeft_IsClampedToBoundary()
    {
        var content = new Rect(0, 0, 100, 100);
        var viewport = new Rect(10, 10, 30, 30);
        var clamped = WayfinderGeometry.ClampDragDelta(viewport, content, new Vector(-50, 0));
        // Min dx = 0 - 10 = -10.
        await Assert.That(Math.Abs(clamped.X - (-10))).IsLessThan(Tolerance);
    }

    #endregion

    #region Attach / detach

    [Test]
    public async Task Wayfinder_DefaultProperties_AreNonNull()
    {
        var wf = new Wayfinder();
        await Assert.That(wf.Background).IsNotNull();
        await Assert.That(wf.ShadowBrush).IsNotNull();
        await Assert.That(wf.ViewportBrush).IsNotNull();
        await Assert.That(wf.ViewportPen).IsNotNull();
        await Assert.That(wf.ZoomControl).IsNull();
    }

    [Test]
    public async Task Wayfinder_AttachToZoomControl_PicksUpScale()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            // Force layout to give the wayfinder an AvailableSize.
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                // content 400x200, available 100x100 → scale 0.25.
                await Assert.That(Math.Abs(wf.Scale - 0.25)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ContentBounds.Width - 100)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ContentBounds.Height - 50)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_DetachByNullingZoomControl_StopsTracking()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                wf.ZoomControl = null;
                // Changing zoom on the (now-detached) target must not throw.
                zc.Mode = ZoomControlModes.Custom;
                zc.Zoom = 1.5;
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Assert.That(wf.ZoomControl).IsNull();
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_TwoWayfindersCanShareOneZoomControl()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            var wf1 = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var wf2 = new Wayfinder { Width = 50, Height = 50, ZoomControl = zc };
            var host = new Window
            {
                Width = 400,
                Height = 200,
                Content = new StackPanel { Children = { wf1, wf2 } }
            };
            host.Show();
            host.Measure(new Size(400, 200));
            host.Arrange(new Rect(0, 0, 400, 200));
            try
            {
                await Assert.That(wf1.ZoomControl).IsEqualTo(zc);
                await Assert.That(wf2.ZoomControl).IsEqualTo(zc);
                // Different sizes → different scales.
                await Assert.That(wf1.Scale).IsNotEqualTo(wf2.Scale);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    #endregion

    #region Interaction (drag / recenter)

    [Test]
    public async Task Wayfinder_PanByDrag_TranslatesZoomControlByInverseScale()
    {
        // Use the public PanByDelta helper rather than synthesising real pointer events,
        // which is awkward in headless mode.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;
            var initialTx = zc.TranslateX;
            var initialTy = zc.TranslateY;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                // 10px drag in wayfinder-space at scale 0.25 = 40px in content-space at zoom 2 = 80px translate.
                wf.PanByWayfinderDelta(new Vector(10, 0));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                // Translate should have decreased (drag-right in minimap pans content left).
                var expectedDelta = -10.0 / wf.Scale * zc.Zoom;
                await Assert.That(Math.Abs((zc.TranslateX - initialTx) - expectedDelta)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateY - initialTy)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_PanByDrag_FullyVisibleContent_IsNoOp()
    {
        // When zoomed out so content fits entirely, viewport ⊇ content → drag has no effect.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 100, 100);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 0.5; // visible region is much larger than content.
            var initialTx = zc.TranslateX;
            var initialTy = zc.TranslateY;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                wf.PanByWayfinderDelta(new Vector(20, 20));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Assert.That(Math.Abs(zc.TranslateX - initialTx)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateY - initialTy)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_RecenterOnPoint_MovesViewportCenterToClickedPoint()
    {
        // After RecenterOnWayfinderPoint(p), the viewport rect's centre should be at p
        // (clamped to content bounds).
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 4.0; // small visible region, so it can be recentered freely.

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                // Click near the wayfinder's content centre (~50, ~25).
                var target = new Point(50, 25);
                wf.RecenterOnWayfinderPoint(target);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                var vp = wf.ViewportRect;
                var center = new Point(vp.X + vp.Width / 2, vp.Y + vp.Height / 2);
                await Assert.That(Math.Abs(center.X - target.X)).IsLessThan(1.0);
                await Assert.That(Math.Abs(center.Y - target.Y)).IsLessThan(1.0);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_RecenterOnRightEdge_KeepsViewportInsideContent()
    {
        // Regression: clicking near the right edge of the wayfinder used to
        // place the click point at the *centre* of the ZoomControl viewport,
        // pushing half the viewport past the content's right edge and
        // leaving leftward nodes off-screen. The recenter must clamp the
        // resulting translate so the viewport stays inside the content.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 4.0; // visible region 200x150 is smaller than 400x200 content.

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                // Click at the very right edge of the content area in
                // wayfinder space. Content is 400x200 → ContentBounds is
                // 100x50 at the origin, so x=100 is the right edge.
                wf.RecenterOnWayfinderPoint(new Point(100, 25));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                var vp = wf.ViewportRect;
                // The viewport's right edge must coincide with the content
                // right edge — i.e. fully showing the rightmost content,
                // not extending past it.
                await Assert.That(Math.Abs(vp.Right - wf.ContentBounds.Right)).IsLessThan(1.0);
                await Assert.That(vp.X).IsGreaterThanOrEqualTo(wf.ContentBounds.X - 0.01);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_RecenterOnPoint_AccountsForTrackableContentOffset()
    {
        // The minimap is normalized to the trackable extent's top-left. A
        // pointer at (50, 25) must therefore target graph point (300, 180),
        // not the unoffset point (200, 100).
        var contentRect = new Rect(100, 80, 800, 600);
        var (zc, window) = CreateZoomControlWithOffsetContent(400, 300, contentRect);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                var target = new Point(25, 12.5);
                wf.RecenterOnWayfinderPoint(target);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                var viewportCenter = new Point(
                    wf.ViewportRect.X + wf.ViewportRect.Width / 2,
                    wf.ViewportRect.Y + wf.ViewportRect.Height / 2);
                await Assert.That(Math.Abs(viewportCenter.X - target.X)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(viewportCenter.Y - target.Y)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateX + 200)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateY + 60)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_PanByDrag_AccountsForTrackableContentOffset()
    {
        var contentRect = new Rect(100, 80, 800, 600);
        var (zc, window) = CreateZoomControlWithOffsetContent(400, 300, contentRect);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                wf.RecenterOnWayfinderPoint(new Point(50, 37.5));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
                var initialViewport = wf.ViewportRect;
                var initialTranslateX = zc.TranslateX;
                var initialTranslateY = zc.TranslateY;

                wf.PanByWayfinderDelta(new Vector(10, 5));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                await Assert.That(Math.Abs(wf.ViewportRect.X - initialViewport.X - 10)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Y - initialViewport.Y - 5)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateX - initialTranslateX + 160)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateY - initialTranslateY + 80)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_ViewportRect_NormalizesCenteredVisibleContentRect()
    {
        var contentRect = new Rect(100, 80, 800, 600);
        var (zc, window) = CreateZoomControlWithOffsetContent(400, 300, contentRect);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 2.0;
            zc.TranslateX = -80;
            zc.TranslateY = -40;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
                var visible = zc.GetVisibleContentRect();

                await Assert.That(Math.Abs(wf.ViewportRect.X - (visible.X - contentRect.X) * wf.Scale)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Y - (visible.Y - contentRect.Y) * wf.Scale)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Width - visible.Width * wf.Scale)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Height - visible.Height * wf.Scale)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_ZoomToFill_AlignsViewportWithOffsetTrackableContent()
    {
        var contentRect = new Rect(100, 80, 800, 600);
        var (zc, window) = CreateZoomControlWithOffsetContent(400, 300, contentRect);
        try
        {
            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                zc.ZoomToFill();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                var visible = zc.GetVisibleContentRect();
                await Assert.That(Math.Abs(visible.X - contentRect.X)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(visible.Y - contentRect.Y)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(visible.Width - contentRect.Width)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(visible.Height - contentRect.Height)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.X - wf.ContentBounds.X)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Y - wf.ContentBounds.Y)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Width - wf.ContentBounds.Width)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(wf.ViewportRect.Height - wf.ContentBounds.Height)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_DoubleTapInsideViewport_ImmediatelyRecenters()
    {
        // A double-tap *inside* the viewport rectangle should recenter on the
        // tapped point — unlike a single click, which would only start a drag.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 4.0;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                // First single-click somewhere outside the viewport to put the
                // viewport at a known location, so we can pick a point inside it.
                wf.RecenterOnWayfinderPoint(new Point(70, 30));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                var vp = wf.ViewportRect;
                // Pick a target that is (a) inside the current viewport so the
                // double-tap is "inside" semantically, and (b) inside the
                // unclamped recenter zone so the resulting viewport centre
                // lands exactly on the target. The clamp engages near the
                // wayfinder edges to keep nodes from being pushed off; this
                // test deliberately stays in the "clamp-free" interior.
                var target = new Point(vp.X + vp.Width / 2 - 5, vp.Y + vp.Height / 2 - 5);
                await Assert.That(vp.Contains(target)).IsTrue();

                var handled = wf.HandlePressForTest(target, clickCount: 2);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                await Assert.That(handled).IsTrue();
                var newVp = wf.ViewportRect;
                var center = new Point(newVp.X + newVp.Width / 2, newVp.Y + newVp.Height / 2);
                await Assert.That(Math.Abs(center.X - target.X)).IsLessThan(1.0);
                await Assert.That(Math.Abs(center.Y - target.Y)).IsLessThan(1.0);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    [Test]
    public async Task Wayfinder_DoubleTapOutsideContentBounds_IsIgnored()
    {
        // A double-tap entirely outside the content rectangle (i.e. in the
        // wayfinder's empty padding area) should be ignored — no pan, no throw.
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 200);
        try
        {
            zc.Mode = ZoomControlModes.Custom;
            zc.Zoom = 4.0;
            var initialTx = zc.TranslateX;
            var initialTy = zc.TranslateY;

            var wf = new Wayfinder { Width = 100, Height = 100, ZoomControl = zc };
            var host = new Window { Width = 200, Height = 200, Content = wf };
            host.Show();
            host.Measure(new Size(200, 200));
            host.Arrange(new Rect(0, 0, 200, 200));
            try
            {
                // Content (400x200) at scale 0.25 = 100x50, anchored top-left.
                // A point at (50, 80) is below the content rectangle.
                var target = new Point(50, 80);
                await Assert.That(wf.ContentBounds.Contains(target)).IsFalse();

                var handled = wf.HandlePressForTest(target, clickCount: 2);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

                await Assert.That(handled).IsFalse();
                await Assert.That(Math.Abs(zc.TranslateX - initialTx)).IsLessThan(Tolerance);
                await Assert.That(Math.Abs(zc.TranslateY - initialTy)).IsLessThan(Tolerance);
            }
            finally { host.Close(); }
        }
        finally { window.Close(); }
    }

    #endregion

    private sealed class TrackableCanvas(Rect contentSize) : Canvas, ITrackableContent
    {
        public event ContentSizeChangedEventHandler? ContentSizeChanged
        {
            add { }
            remove { }
        }

        public Rect ContentSize { get; } = contentSize;
    }
}
