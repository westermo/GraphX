using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Controls.ZoomControl;

/// <summary>
/// A standalone "wayfinder" / minimap control that renders a scaled-down
/// overview of the content of an associated <see cref="ZoomControl"/> and
/// indicates the current viewport. Drag inside the viewport rectangle to
/// pan the target ZoomControl, click outside it to recenter.
///
/// Usage (XAML):
/// <code>
///     &lt;zc:ZoomControl x:Name="zc" Content="..." /&gt;
///     &lt;zc:Wayfinder ZoomControl="{Binding ElementName=zc}"
///                   Width="200" Height="150" /&gt;
/// </code>
///
/// The wayfinder is intentionally NOT part of the default ZoomControl
/// template. Users that want a minimap simply place a Wayfinder somewhere in
/// their layout and bind its <see cref="ZoomControl"/> property to the
/// target ZoomControl.
/// </summary>
public sealed class Wayfinder : Control
{
    #region Styled properties

    public static readonly StyledProperty<ZoomControl?> ZoomControlProperty =
        AvaloniaProperty.Register<Wayfinder, ZoomControl?>(nameof(ZoomControl));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<Wayfinder, IBrush?>(nameof(Background),
            new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF)));

    public static readonly StyledProperty<IBrush?> ShadowBrushProperty =
        AvaloniaProperty.Register<Wayfinder, IBrush?>(nameof(ShadowBrush),
            new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)));

    public static readonly StyledProperty<IBrush?> ViewportBrushProperty =
        AvaloniaProperty.Register<Wayfinder, IBrush?>(nameof(ViewportBrush), Brushes.Transparent);

    public static readonly StyledProperty<IPen?> ViewportPenProperty =
        AvaloniaProperty.Register<Wayfinder, IPen?>(nameof(ViewportPen),
            new Pen(new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)), 1d));

    /// <summary>The ZoomControl this minimap is bound to. May be <c>null</c>.</summary>
    public ZoomControl? ZoomControl
    {
        get => GetValue(ZoomControlProperty);
        set => SetValue(ZoomControlProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? ShadowBrush
    {
        get => GetValue(ShadowBrushProperty);
        set => SetValue(ShadowBrushProperty, value);
    }

    public IBrush? ViewportBrush
    {
        get => GetValue(ViewportBrushProperty);
        set => SetValue(ViewportBrushProperty, value);
    }

    public IPen? ViewportPen
    {
        get => GetValue(ViewportPenProperty);
        set => SetValue(ViewportPenProperty, value);
    }

    #endregion

    #region Computed read-only state

    /// <summary>Uniform scale factor mapping content space to wayfinder space.</summary>
    public double Scale { get; private set; } = 1.0;

    /// <summary>The scaled content rectangle, in wayfinder-local coordinates.</summary>
    public Rect ContentBounds { get; private set; }

    /// <summary>The current viewport rectangle, in wayfinder-local coordinates.</summary>
    public Rect ViewportRect { get; private set; }

    /// <summary>
    /// The actual content extent in ZoomControl content-space coordinates
    /// (i.e. the trackable rectangle reported by <see cref="ITrackableContent"/>,
    /// or the visual's own bounds when the content is not trackable). Stored
    /// as a <see cref="Rect"/> because graph content frequently has a non-zero
    /// (or negative) top-left offset.
    /// </summary>
    private Rect _contentRect;

    #endregion

    private bool _isDragging;

    private Point _lastPointerPos;

    // Tracks whether the control is currently attached to the visual tree and
    // therefore has live event subscriptions on the target ZoomControl. Event
    // wiring is deferred to AttachedToVisualTree / DetachedFromVisualTree so
    // that detaching the wayfinder cleanly releases all references on the
    // target ZoomControl (otherwise the ZoomControl would keep the wayfinder
    // alive via its event handlers).
    private bool _subscribed;

    // The content visual we currently have a LayoutUpdated subscription on.
    // Tracked separately because ZoomControl.Content can change without the
    // wayfinder being detached, and we need to move the subscription with it.
    private Layoutable? _subscribedContentVisual;

    // VisualBrush rendering: a single brush wraps the ZoomControl's content
    // visual and is reused across renders. It is rebuilt when the source
    // visual changes (Content swap, ZoomControl swap), and its SourceRect is
    // updated each render to match the actual content extent. Avalonia's
    // VisualBrush (TileBrush) walks the live visual tree on each frame, so we
    // do not need to manually rasterise; calling InvalidateVisual on layout
    // changes is sufficient to keep the minimap in sync.
    private VisualBrush? _contentBrush;
    private Visual? _brushSourceVisual;

    static Wayfinder()
    {
        AffectsRender<Wayfinder>(BackgroundProperty, ShadowBrushProperty,
            ViewportBrushProperty, ViewportPenProperty);
        ZoomControlProperty.Changed.AddClassHandler<Wayfinder>((wf, e) =>
            wf.OnZoomControlChanged(e.OldValue as ZoomControl, e.NewValue as ZoomControl));
    }

    public Wayfinder()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    #region Attach / detach

    private void OnZoomControlChanged(ZoomControl? oldZc, ZoomControl? newZc)
    {
        if (oldZc != null && _subscribed)
            Unsubscribe(oldZc);

        if (newZc != null && _subscribed)
            Subscribe(newZc);

        RecomputeGeometry();
        InvalidateVisual();
    }

    private void Subscribe(ZoomControl zc)
    {
        ((AvaloniaObject)zc).PropertyChanged += TargetPropertyChanged;
        if (zc.Content is ITrackableContent tc)
            tc.ContentSizeChanged += TargetContentSizeChanged;
        SubscribeContentVisual(zc.ContentVisual);
    }

    private void Unsubscribe(ZoomControl zc)
    {
        _brushSourceVisual = null;
        _contentBrush = null;
        ((AvaloniaObject)zc).PropertyChanged -= TargetPropertyChanged;
        if (zc.Content is ITrackableContent tc)
            tc.ContentSizeChanged -= TargetContentSizeChanged;
        SubscribeContentVisual(null);
    }

    /// <summary>
    /// Re-targets the LayoutUpdated subscription onto a different content
    /// visual (or none). LayoutUpdated on the *content* visual fires whenever
    /// its layout pass runs — i.e. when graph descendants change (vertices
    /// added/moved, edges rerouted). It does NOT fire on pan/zoom because
    /// ZoomControl pans/zooms via RenderTransform, which doesn't trigger
    /// layout of its content. That makes it the right signal to refresh the
    /// minimap when the graph is rebuilt while staying cheap during
    /// pan/zoom animations.
    /// </summary>
    private void SubscribeContentVisual(Layoutable? newVisual)
    {
        if (ReferenceEquals(_subscribedContentVisual, newVisual)) return;
        if (_subscribedContentVisual != null)
            _subscribedContentVisual.LayoutUpdated -= ContentVisualLayoutUpdated;
        _subscribedContentVisual = newVisual;
        if (newVisual != null)
            newVisual.LayoutUpdated += ContentVisualLayoutUpdated;
    }

    private void ContentVisualLayoutUpdated(object? sender, EventArgs e)
    {
        // Descendants of the content visual (re)laid out — extent may have
        // grown/shrunk and the rendered geometry has changed. Refresh.
        RecomputeGeometry();
        InvalidateVisual();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_subscribed) return;
        _subscribed = true;
        if (ZoomControl is { } zc)
        {
            Subscribe(zc);
            RecomputeGeometry();
            InvalidateVisual();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_subscribed && ZoomControl is { } zc)
            Unsubscribe(zc);
        _subscribed = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void TargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Only react to the few properties that affect minimap geometry.
        if (e.Property == ZoomControl.ZoomProperty ||
            e.Property == ZoomControl.TranslateXProperty ||
            e.Property == ZoomControl.TranslateYProperty ||
            e.Property == ContentControl.ContentProperty ||
            e.Property == BoundsProperty)
        {
            if (e.Property == ContentControl.ContentProperty)
            {
                if (e.OldValue is ITrackableContent oldTc)
                    oldTc.ContentSizeChanged -= TargetContentSizeChanged;
                if (e.NewValue is ITrackableContent newTc)
                    newTc.ContentSizeChanged += TargetContentSizeChanged;
                // Re-target the LayoutUpdated subscription onto the new content
                // visual so that descendant relayouts (graph rebuilds, vertex
                // additions) continue to refresh the minimap.
                SubscribeContentVisual(ZoomControl?.ContentVisual);
            }

            RecomputeGeometry();
            InvalidateVisual();
        }
    }

    private void TargetContentSizeChanged(object sender,
        Models.ContentSizeChangedEventArgs e)
    {
        // The trackable content reported a new extent; recompute scale and
        // refresh so the minimap reflects the newly enlarged (or shrunk)
        // content region.
        RecomputeGeometry();
        InvalidateVisual();
    }

    #endregion

    #region Geometry / layout

    /// <summary>Recomputes <see cref="Scale"/>, <see cref="ContentBounds"/> and <see cref="ViewportRect"/>.</summary>
    private void RecomputeGeometry()
    {
        var available = Bounds.Size;
        if (available.Width <= 0 || available.Height <= 0)
            available = new Size(Width, Height);

        _contentRect = GetContentRect();
        var contentSize = _contentRect.Size;
        Scale = WayfinderGeometry.ComputeScale(contentSize, available);
        ContentBounds = WayfinderGeometry.ComputeContentBounds(contentSize, Scale);

        var zc = ZoomControl;
        if (zc != null)
        {
            // Compute viewport rect in content-space coordinates, then shift
            // by -_contentRect.TopLeft so the result is anchored to the same
            // origin as ContentBounds (top-left of the wayfinder content area).
            var vpContent = WayfinderGeometry.ComputeViewportRect(
                zc.Zoom,
                zc.TranslateX,
                zc.TranslateY,
                new Size(zc.Bounds.Width, zc.Bounds.Height),
                Scale);
            ViewportRect = new Rect(
                vpContent.X - _contentRect.X * Scale,
                vpContent.Y - _contentRect.Y * Scale,
                vpContent.Width,
                vpContent.Height);
        }
        else
        {
            ViewportRect = default;
        }
    }

    /// <summary>
    /// Returns the actual content extent, in ZoomControl content-space
    /// coordinates. For trackable content (e.g. <c>GraphArea</c>) this is the
    /// real bounding rectangle of all rendered children — possibly with a
    /// non-zero (or negative) top-left. For non-trackable content we fall
    /// back to the visual's own bounds at the origin.
    /// </summary>
    private Rect GetContentRect()
    {
        var zc = ZoomControl;
        if (zc == null) return default;
        if (zc.TrackableContent is { } trackable)
            return trackable.ContentSize;
        if (zc.ContentVisual is not { } visual) return default;
        var size = visual.DesiredSize;
        if (size.Width <= 0 || size.Height <= 0)
        {
            var b = visual.Bounds;
            if (b is { Width: > 0, Height: > 0 }) size = b.Size;
        }

        return new Rect(default, size);
    }

    protected override Size MeasureOverride(Size availableSize) => availableSize;

    protected override Size ArrangeOverride(Size finalSize)
    {
        var s = base.ArrangeOverride(finalSize);
        RecomputeGeometry();
        return s;
    }

    #endregion

    #region Pointer interaction

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (ZoomControl == null) return;
        var p = e.GetPosition(this);

        // Double-tap → jump immediately to the clicked location, regardless of
        // whether the press happens inside or outside the viewport rectangle.
        if (e.ClickCount >= 2)
        {
            if (!ContentBounds.Contains(p)) return;
            RecenterOnWayfinderPoint(p);
            e.Handled = true;

            return;
        }

        if (ViewportRect.Width > 0 && ViewportRect.Contains(p))
        {
            // Begin drag-pan.
            _isDragging = true;
            _lastPointerPos = p;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
        else if (ContentBounds.Contains(p))
        {
            // Single click outside the viewport but inside the content → recenter.
            RecenterOnWayfinderPoint(p);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Test/automation hook that mirrors the press-handling logic without
    /// requiring a synthesised <see cref="PointerPressedEventArgs"/>. Returns
    /// <c>true</c> when the press triggered a recenter (single click outside
    /// the viewport, or any double-tap inside the content bounds).
    /// </summary>
    internal bool HandlePressForTest(Point wayfinderPoint, int clickCount)
    {
        if (ZoomControl == null) return false;

        if (clickCount >= 2)
        {
            if (!ContentBounds.Contains(wayfinderPoint)) return false;
            RecenterOnWayfinderPoint(wayfinderPoint);
            return true;
        }

        if (ViewportRect.Width > 0 && ViewportRect.Contains(wayfinderPoint))
        {
            _isDragging = true;
            _lastPointerPos = wayfinderPoint;
            return false;
        }

        if (!ContentBounds.Contains(wayfinderPoint)) return false;
        RecenterOnWayfinderPoint(wayfinderPoint);
        return true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging || ZoomControl == null) return;
        var p = e.GetPosition(this);
        var delta = p - _lastPointerPos;
        PanByWayfinderDelta(delta);
        _lastPointerPos = p;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            if (Equals(e.Pointer.Captured, this))
                e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Pans the target ZoomControl as if the user had dragged the viewport
    /// rectangle by <paramref name="delta"/> in wayfinder space. A drag of
    /// (dx, 0) shifts the viewport right by dx wayfinder pixels, which means
    /// the ZoomControl's content shifts left by dx/Scale content pixels.
    /// </summary>
    public void PanByWayfinderDelta(Vector delta)
    {
        var zc = ZoomControl;
        if (zc == null || Scale <= 0) return;

        var clamped = WayfinderGeometry.ClampDragDelta(ViewportRect, ContentBounds, delta);
        if (clamped is { X: 0, Y: 0 }) return;

        // Move ZoomControl translate inversely: shifting the minimap viewport right
        // means the visible content slides left, i.e. translate decreases.
        var dxContent = clamped.X / Scale;
        var dyContent = clamped.Y / Scale;
        zc.TranslateX -= dxContent * zc.Zoom;
        zc.TranslateY -= dyContent * zc.Zoom;

        // When attached, TargetPropertyChanged already recomputes geometry as
        // TranslateX/Y change. Only do it manually when detached (e.g. unit
        // tests exercising the math without adding the control to a visual
        // tree).
        if (!_subscribed)
        {
            RecomputeGeometry();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Recenters the ZoomControl viewport so that the given wayfinder-local
    /// point becomes the centre of the visible region — clamped so the
    /// resulting viewport never extends past the content extent on an axis
    /// where the content is larger than the viewport. Without this clamp,
    /// clicking near the right edge of the wayfinder would centre the
    /// ZoomControl on the content's right edge, leaving half the viewport
    /// past the content and pushing all leftward nodes out of view.
    /// </summary>
    public void RecenterOnWayfinderPoint(Point wayfinderPoint)
    {
        var zc = ZoomControl;
        if (zc == null || Scale <= 0) return;

        // Convert from wayfinder coords to content coords, taking the content
        // rect's top-left offset into account (graph children may live at
        // non-zero or negative absolute coordinates).
        var contentX = wayfinderPoint.X / Scale + _contentRect.X;
        var contentY = wayfinderPoint.Y / Scale + _contentRect.Y;

        // ZoomControl maps content (cx, cy) to screen via: screen = cx * zoom + tx.
        // We want the centre of the ZoomControl's screen viewport to land on (cx, cy).
        // So tx = ScreenCenterX - cx * zoom.
        var zoom = zc.Zoom;
        var zcW = zc.Bounds.Width;
        var zcH = zc.Bounds.Height;
        var tx = zcW / 2 - contentX * zoom;
        var ty = zcH / 2 - contentY * zoom;

        // Clamp so the visible content rect [(-tx)/zoom, (-tx + zcW)/zoom]
        // stays inside [_contentRect.X, _contentRect.Right] when the content
        // is larger than the visible region on that axis. When the content
        // is smaller (zoomed out), no clamping — the viewport already
        // encloses the content and the centre is meaningful.
        var contentSpanX = zcW / zoom;
        var contentSpanY = zcH / zoom;
        if (contentSpanX < _contentRect.Width)
        {
            // tx_max (content left edge inside viewport): -tx/zoom = _contentRect.X  → tx = -_contentRect.X * zoom
            // tx_min (content right edge inside viewport): (-tx + zcW)/zoom = _contentRect.Right → tx = zcW - _contentRect.Right * zoom
            var txMax = -_contentRect.X * zoom;
            var txMin = zcW - _contentRect.Right * zoom;
            if (tx > txMax) tx = txMax;
            if (tx < txMin) tx = txMin;
        }

        if (contentSpanY < _contentRect.Height)
        {
            var tyMax = -_contentRect.Y * zoom;
            var tyMin = zcH - _contentRect.Bottom * zoom;
            if (ty > tyMax) ty = tyMax;
            if (ty < tyMin) ty = tyMin;
        }

        zc.TranslateX = tx;
        zc.TranslateY = ty;

        if (!_subscribed)
        {
            RecomputeGeometry();
            InvalidateVisual();
        }
    }

    #endregion

    #region Rendering

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        // 1. Background of the content area.
        if (Background != null && ContentBounds is { Width: > 0, Height: > 0 })
            dc.DrawRectangle(Background, null, ContentBounds);

        // 2. Live snapshot of the actual ZoomControl content via a VisualBrush.
        //    The brush samples an absolute rectangle (_contentRect) of the
        //    source visual rather than its DesiredSize — this is essential
        //    because GraphAreaBase.MeasureOverride reports a sentinel
        //    DesiredSize that does NOT cover its absolutely-positioned
        //    children. By telling the brush to read from the real content
        //    extent and stretch that into ContentBounds, we get a correct
        //    minimap regardless of how the source visual reports its size.
        DrawContentBrush(dc);

        // 3. Shadow over non-viewport area + viewport outline.
        var bounds = new Rect(Bounds.Size);
        var vp = ViewportRect;
        var intersects = vp is { Width: > 0, Height: > 0 } &&
                         !(vp.Right <= bounds.X || vp.X >= bounds.Right ||
                           vp.Bottom <= bounds.Y || vp.Y >= bounds.Bottom);

        if (intersects)
        {
            if (ShadowBrush != null)
            {
                var contentRect = ContentBounds;
                // Clip the viewport rect to the content area before computing
                // the shadow strips, otherwise a viewport that extends past
                // ContentBounds (typical when zoomed-out so the on-screen
                // viewport is larger than the graph) produces negative-sized
                // shadow strips that don't draw, leaving the unscoped area
                // unshaded.
                var clipped = new Rect(
                    Math.Max(vp.X, contentRect.X),
                    Math.Max(vp.Y, contentRect.Y),
                    Math.Max(0, Math.Min(vp.Right, contentRect.Right) - Math.Max(vp.X, contentRect.X)),
                    Math.Max(0, Math.Min(vp.Bottom, contentRect.Bottom) - Math.Max(vp.Y, contentRect.Y)));

                var top = new Rect(contentRect.X, contentRect.Y,
                    contentRect.Width, Math.Max(0, clipped.Y - contentRect.Y));
                var bottom = new Rect(contentRect.X, clipped.Bottom,
                    contentRect.Width, Math.Max(0, contentRect.Bottom - clipped.Bottom));
                var left = new Rect(contentRect.X, clipped.Y,
                    Math.Max(0, clipped.X - contentRect.X), clipped.Height);
                var right = new Rect(clipped.Right, clipped.Y,
                    Math.Max(0, contentRect.Right - clipped.Right), clipped.Height);
                if (top.Height > 0) dc.DrawRectangle(ShadowBrush, null, top);
                if (bottom.Height > 0) dc.DrawRectangle(ShadowBrush, null, bottom);
                if (left.Width > 0) dc.DrawRectangle(ShadowBrush, null, left);
                if (right.Width > 0) dc.DrawRectangle(ShadowBrush, null, right);
            }

            dc.DrawRectangle(ViewportBrush, ViewportPen, vp);
        }
        else if (ShadowBrush != null && ContentBounds is { Width: > 0, Height: > 0 })
        {
            // No part of the viewport is visible → entire content area is "off screen".
            dc.DrawRectangle(ShadowBrush, null, ContentBounds);
        }
    }

    /// <summary>
    /// Draws the live content of the bound ZoomControl into <see cref="ContentBounds"/>
    /// using a cached <see cref="VisualBrush"/>. The brush is rebuilt only when
    /// the source visual changes; its <c>SourceRect</c> is updated each frame
    /// so the brush always samples the current trackable content extent.
    /// </summary>
    private void DrawContentBrush(DrawingContext dc)
    {
        if (Scale <= 0 || ContentBounds.Width <= 0 || ContentBounds.Height <= 0) return;
        if (ZoomControl?.ContentVisual is not { } visual) return;
        if (_contentRect.Width <= 0 || _contentRect.Height <= 0) return;
        if (!ReferenceEquals(_brushSourceVisual, visual) || _contentBrush == null)
        {
            _brushSourceVisual = visual;
            _contentBrush = new VisualBrush
            {
                Visual = visual,
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None,
            };
        }

        // TrackableContent.ContentSize is expressed in graph coordinates, but
        // the content visual is arranged to the extent size. Positive graph
        // offsets must therefore not be used as a VisualBrush source offset:
        // doing so skips the top/left of the visual and clips the bottom/right
        // by the same amount.
        var sourceRect = new Rect(
            _contentRect.X > 0 ? 0 : _contentRect.X,
            _contentRect.Y > 0 ? 0 : _contentRect.Y,
            _contentRect.Width,
            _contentRect.Height);
        _contentBrush.SourceRect = new RelativeRect(sourceRect, RelativeUnit.Absolute);

        dc.DrawRectangle(_contentBrush, null, ContentBounds);
    }

    #endregion
}