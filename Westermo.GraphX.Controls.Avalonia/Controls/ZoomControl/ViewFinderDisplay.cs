using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Westermo.GraphX.Controls.Controls.ZoomControl.Helpers;

namespace Westermo.GraphX.Controls.Controls.ZoomControl;

public class ViewFinderDisplay : Control
{
    public ViewFinderDisplay()
    {
        HookOnInitialize();
    }

    protected virtual void HookOnInitialize()
    {
    }

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, IBrush?>(nameof(Background),
            new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF)));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<Rect> ContentBoundsProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, Rect>(nameof(ContentBounds), default);

    internal Rect ContentBounds
    {
        get => GetValue(ContentBoundsProperty);
        set => SetValue(ContentBoundsProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ShadowBrushProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, IBrush?>(nameof(ShadowBrush),
            new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)));

    public IBrush? ShadowBrush
    {
        get => GetValue(ShadowBrushProperty);
        set => SetValue(ShadowBrushProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ViewportBrushProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, IBrush?>(nameof(ViewportBrush), Brushes.Transparent);

    public IBrush? ViewportBrush
    {
        get => GetValue(ViewportBrushProperty);
        set => SetValue(ViewportBrushProperty, value);
    }

    public static readonly StyledProperty<Pen?> ViewportPenProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, Pen?>(nameof(ViewportPen),
            new Pen(new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)), 1d));

    public Pen? ViewportPen
    {
        get => GetValue(ViewportPenProperty);
        set => SetValue(ViewportPenProperty, value);
    }

    public static readonly StyledProperty<Rect> ViewportRectProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, Rect>(nameof(ViewportRect), default);

    public Rect ViewportRect
    {
        get => GetValue(ViewportRectProperty);
        set => SetValue(ViewportRectProperty, value);
    }

    public static readonly StyledProperty<VisualBrush?> VisualBrushProperty =
        AvaloniaProperty.Register<ViewFinderDisplay, VisualBrush?>(nameof(VisualBrush));

    internal VisualBrush? VisualBrush
    {
        get => GetValue(VisualBrushProperty);
        set => SetValue(VisualBrushProperty, value);
    }

    internal Size AvailableSize { get; private set; } = default;
    internal double Scale { get; set; } = 1d;

    protected override Size ArrangeOverride(Size finalSize) => DesiredSize;

    protected override Size MeasureOverride(Size availableSize)
    {
        AvailableSize = availableSize;
        var width = DoubleHelper.IsNaN(ContentBounds.Width) ? 0 : Math.Max(0, ContentBounds.Width);
        var height = DoubleHelper.IsNaN(ContentBounds.Height) ? 0 : Math.Max(0, ContentBounds.Height);
        var displayPanelSize = new Size(width, height);
        if (displayPanelSize.Width > availableSize.Width || displayPanelSize.Height > availableSize.Height)
        {
            var aspectX = availableSize.Width / displayPanelSize.Width;
            var aspectY = availableSize.Height / displayPanelSize.Height;
            var scale = aspectX < aspectY ? aspectX : aspectY;
            displayPanelSize = new Size(Math.Max(0, displayPanelSize.Width * scale),
                Math.Max(0, displayPanelSize.Height * scale));
        }

        return displayPanelSize;
    }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);
        if (Background != null) dc.DrawRectangle(Background, null, ContentBounds);
        if (VisualBrush != null) dc.DrawRectangle(VisualBrush, null, ContentBounds);
        var boundsRect = new Rect(Bounds.Size);
        bool intersects = !(ViewportRect.Right <= boundsRect.X || ViewportRect.X >= boundsRect.Right ||
                            ViewportRect.Bottom <= boundsRect.Y || ViewportRect.Y >= boundsRect.Bottom);
        if (intersects)
        {
            var r1 = new Rect(new Point(0, 0), new Size(Bounds.Width, Math.Max(0, ViewportRect.Top)));
            var r2 = new Rect(new Point(0, ViewportRect.Top),
                new Size(Math.Max(0, ViewportRect.Left), ViewportRect.Height));
            var r3 = new Rect(new Point(ViewportRect.Right, ViewportRect.Top),
                new Size(Math.Max(0, Bounds.Width - ViewportRect.Right), ViewportRect.Height));
            var r4 = new Rect(new Point(0, ViewportRect.Bottom),
                new Size(Bounds.Width, Math.Max(0, Bounds.Height - ViewportRect.Bottom)));
            if (ShadowBrush != null)
            {
                dc.DrawRectangle(ShadowBrush, null, r1);
                dc.DrawRectangle(ShadowBrush, null, r2);
                dc.DrawRectangle(ShadowBrush, null, r3);
                dc.DrawRectangle(ShadowBrush, null, r4);
            }

            dc.DrawRectangle(ViewportBrush, ViewportPen, ViewportRect);
        }
        else if (ShadowBrush != null)
        {
            dc.DrawRectangle(ShadowBrush, null, new Rect(Bounds.Size));
        }
    }
}