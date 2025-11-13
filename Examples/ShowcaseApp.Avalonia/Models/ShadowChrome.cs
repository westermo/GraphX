using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ShowcaseApp.Avalonia.Models;

public class ShadowChrome : Decorator
{
    private static SolidColorBrush? backgroundBrush;
    private static LinearGradientBrush? rightBrush;
    private static LinearGradientBrush? bottomBrush;
    private static RadialGradientBrush? bottomRightBrush;
    private static RadialGradientBrush? topRightBrush;
    private static RadialGradientBrush? bottomLeftBrush;

    static bool _initialized;

    // *** Constructors ***
    public ShadowChrome()
    {
        Margin = new Thickness(0, 0, 4, 4);
    }

    // *** Overriden base methods ***
    private static void EnsureInitialized()
    {
        if (_initialized) return;
        CreateBrushes();
        _initialized = true;
    }

    public override void Render(DrawingContext context)
    {
        EnsureInitialized();
        // Calculate the size of the shadow (use Margin.Right & Bottom as in original logic)
        var shadowSize = Math.Min(Margin.Right, Margin.Bottom);
        if (shadowSize <= 0 || Bounds.Width < shadowSize * 2 || Bounds.Height < shadowSize * 2) return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        var shadow = shadowSize;

        // background (area behind child) shifted by shadow offset
        var backgroundRect = new Rect(shadow, shadow, w - shadow, h - shadow);
        context.DrawRectangle(backgroundBrush, null, backgroundRect);

        // top-right (vertical edge start)
        context.DrawRectangle(topRightBrush, null, new Rect(w, shadow, shadow, shadow));
        // right side
        context.DrawRectangle(rightBrush, null, new Rect(w, shadow * 2, shadow, h - shadow * 2));
        // bottom-right corner
        context.DrawRectangle(bottomRightBrush, null, new Rect(w, h, shadow, shadow));
        // bottom edge
        context.DrawRectangle(bottomBrush, null, new Rect(shadow * 2, h, w - shadow * 2, shadow));
        // bottom-left corner
        context.DrawRectangle(bottomLeftBrush, null, new Rect(shadow, h, shadow, shadow));
    }


    // *** Private static methods ***
    private static void CreateBrushes()
    {
        var shadowColor = Color.FromArgb(128, 0, 0, 0);
        var transparentColor = Color.FromArgb(16, 0, 0, 0);

        var gradientStops = new GradientStops
        {
            new GradientStop(shadowColor, 0.5),
            new GradientStop(transparentColor, 1.0)
        };

        backgroundBrush = new SolidColorBrush(shadowColor);
        rightBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative), GradientStops = gradientStops
        };
        bottomBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative), GradientStops = gradientStops
        };

        bottomRightBrush = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
            Center = new RelativePoint(0, 0, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(1, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(1, RelativeUnit.Absolute),
            GradientStops = gradientStops
        };
        topRightBrush = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0, 1, RelativeUnit.Relative),
            Center = new RelativePoint(0, 1, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(1, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(1, RelativeUnit.Absolute),
            GradientStops = gradientStops
        };
        bottomLeftBrush = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(1, 0, RelativeUnit.Relative),
            Center = new RelativePoint(1, 0, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(1, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(1, RelativeUnit.Absolute),
            GradientStops = gradientStops
        };
    }
}