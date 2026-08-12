using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Shared pixel-size validation, DPI-aware bitmap creation and effective render-scaling
/// lookup used by both the GraphArea raster cache (<see cref="GraphAreaRasterCacheLayer"/>)
/// and the Wayfinder minimap content cache. Centralizing these primitives keeps their
/// backend texture-size and memory-budget guards identical.
/// </summary>
internal static class RasterCacheGeometry
{
    /// <summary>
    /// Validates <paramref name="contentBounds"/>/<paramref name="renderScaling"/> and computes the
    /// physical pixel size a cache bitmap covering them would need, rejecting sizes that would exceed
    /// <paramref name="maximumDimension"/> per axis or <paramref name="maximumBytes"/> total (assuming 4
    /// bytes per pixel).
    /// </summary>
    public static bool TryGetPixelSize(
        Rect contentBounds,
        double renderScaling,
        long maximumBytes,
        int maximumDimension,
        out PixelSize pixelSize)
    {
        pixelSize = default;
        if (contentBounds is not { Width: > 0, Height: > 0 } ||
            !double.IsFinite(contentBounds.X) ||
            !double.IsFinite(contentBounds.Y) ||
            !double.IsFinite(contentBounds.Width) ||
            !double.IsFinite(contentBounds.Height) ||
            !double.IsFinite(renderScaling) ||
            renderScaling <= 0 ||
            maximumBytes <= 0)
            return false;

        var scaledWidth = Math.Ceiling(contentBounds.Width * renderScaling);
        var scaledHeight = Math.Ceiling(contentBounds.Height * renderScaling);
        if (scaledWidth is < 1 or > int.MaxValue || scaledWidth > maximumDimension ||
            scaledHeight is < 1 or > int.MaxValue || scaledHeight > maximumDimension)
            return false;

        var width = (int)scaledWidth;
        var height = (int)scaledHeight;
        var pixelCount = (long)width * height;
        if (pixelCount > maximumBytes / 4)
            return false;

        pixelSize = new PixelSize(width, height);
        return true;
    }

    /// <summary>
    /// Creates a <see cref="RenderTargetBitmap"/> whose DPI matches <paramref name="renderScaling"/> so
    /// that one device-independent unit in its drawing context maps to one device-independent unit of
    /// the cached content, while the backing pixel buffer is physically sized for that scaling.
    /// </summary>
    public static RenderTargetBitmap CreateBitmap(PixelSize pixelSize, double renderScaling) =>
        new(pixelSize, new Vector(96 * renderScaling, 96 * renderScaling));

    /// <summary>
    /// Returns the render scaling to rasterize at: the scaling of an explicitly subscribed top level when
    /// available, otherwise the current top level for <paramref name="visual"/>, clamped to a finite
    /// positive value (falling back to 1x) so callers never divide/multiply by zero or NaN.
    /// </summary>
    public static double GetEffectiveRenderScaling(TopLevel? subscribedTopLevel, Visual visual)
    {
        var scaling = subscribedTopLevel?.RenderScaling ?? TopLevel.GetTopLevel(visual)?.RenderScaling ?? 1;
        return double.IsFinite(scaling) && scaling > 0 ? scaling : 1;
    }
}
