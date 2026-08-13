using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Owns the bitmap used by <see cref="GraphAreaBase"/> while its graph visuals
/// are temporarily replaced by a raster image for viewport manipulation.
/// </summary>
internal sealed class GraphAreaRasterCacheLayer : Control, IDisposable
{
    // Backends commonly impose texture dimension limits below this value. The
    // byte cap supplied by GraphAreaBase is the public, tunable guard; this is
    // a second guard against a narrow-but-very-tall unsupported texture.
    private const int MaximumBitmapDimension = 8192;

    private readonly GraphAreaBase _graphArea;
    private RenderTargetBitmap? _bitmap;

    public GraphAreaRasterCacheLayer(GraphAreaBase graphArea)
    {
        _graphArea = graphArea;
        IsHitTestVisible = false;
        Focusable = false;
        ClipToBounds = false;
    }

    public bool HasBitmap => _bitmap != null;

    public Rect ContentBounds { get; private set; }

    public PixelSize PixelSize => _bitmap?.PixelSize ?? default;

    internal RenderTargetBitmap? Bitmap => _bitmap;

    public int RasterizationCount { get; private set; }

    /// <summary>
    /// Creates a graph-world-coordinate bitmap by rendering individual graph
    /// children. Rendering the GraphArea itself would recursively include this
    /// layer and would clip graph-space offsets to its arranged extent.
    /// </summary>
    public bool TryRasterize(
        IReadOnlyCollection<Control> sourceChildren,
        Rect contentBounds,
        double renderScaling,
        long maximumBytes)
    {
        DisposeBitmap();
        ContentBounds = default;

        if (!RasterCacheGeometry.TryGetPixelSize(contentBounds, renderScaling, maximumBytes, MaximumBitmapDimension,
                out var pixelSize))
            return false;

        RenderTargetBitmap? bitmap = null;
        try
        {
            bitmap = RasterCacheGeometry.CreateBitmap(pixelSize, renderScaling);
            using var context = bitmap.CreateDrawingContext();
            GraphAreaChildRenderer.Render(
                context,
                _graphArea,
                sourceChildren,
                rect => new Rect(rect.X - contentBounds.X, rect.Y - contentBounds.Y, rect.Width, rect.Height),
                renderCachedRasterLayerAsBitmap: false,
                renderBatchedEdgeLayerDirectly: true);

            _bitmap = bitmap;
            bitmap = null;
            ContentBounds = contentBounds;
            RasterizationCount++;
            return true;
        }
        finally
        {
            bitmap?.Dispose();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_bitmap == null || ContentBounds is not { Width: > 0, Height: > 0 }) return;
        context.DrawImage(_bitmap, new Rect(_bitmap.Size), ContentBounds);
    }

    public void Dispose()
    {
        DisposeBitmap();
        ContentBounds = default;
    }

    private void DisposeBitmap()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
