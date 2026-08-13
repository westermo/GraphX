using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Renders the graph-space children of a <see cref="GraphAreaBase"/> into a <see cref="DrawingContext"/>,
/// mapping each child's graph-space bounds (relative to the GraphArea) through a caller-supplied
/// transform. GraphArea intentionally reports its extent size while keeping graph-space child
/// coordinates, so a VisualBrush sourced from the GraphArea itself would clip children at positive
/// coordinates greater than that size, and drop children at negative coordinates. Sampling each child
/// individually from its own arranged bounds avoids that.
/// </summary>
/// <remarks>
/// Used by both the GraphArea raster cache (an identity/translate-only transform into an untransformed
/// graph-coordinate bitmap) and the Wayfinder minimap (a translate-and-scale transform into minimap
/// coordinates), so this graph-coordinate sampling logic is defined once. The two special cases below are
/// individually opt-in because they are only valid for one of those transforms; see their parameters.
/// </remarks>
internal static class GraphAreaChildRenderer
{
    /// <summary>
    /// Renders <paramref name="children"/> into <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The destination drawing context.</param>
    /// <param name="graphArea">The GraphArea the children belong to, used to resolve their graph-space origin.</param>
    /// <param name="children">The children to render, in the order they should be drawn.</param>
    /// <param name="mapToDestination">
    /// Maps a child's graph-space bounds (relative to <paramref name="graphArea"/>) to the destination
    /// rectangle it should be drawn into.
    /// </param>
    /// <param name="renderCachedRasterLayerAsBitmap">
    /// When true, a nested <see cref="GraphAreaRasterCacheLayer"/> child with a bitmap is blitted directly
    /// from that bitmap (mapping its content bounds) instead of being sampled through a VisualBrush. Only
    /// valid when the bitmap's own coordinate space matches graph-space, e.g. the Wayfinder minimap; the
    /// GraphArea raster cache's own rasterization never encounters this child type in its source list.
    /// </param>
    /// <param name="renderBatchedEdgeLayerDirectly">
    /// When true, a <see cref="BatchedEdgeLayer"/> child is rendered directly (translated into destination
    /// space) instead of through a VisualBrush. Only valid for a translate-only (unscaled) destination
    /// mapping, since the layer draws its own edge geometry without a Stretch.Fill; the Wayfinder's scaled
    /// minimap mapping must keep the generic VisualBrush fallback instead.
    /// </param>
    public static void Render(
        DrawingContext context,
        GraphAreaBase graphArea,
        IEnumerable<Control> children,
        Func<Rect, Rect> mapToDestination,
        bool renderCachedRasterLayerAsBitmap,
        bool renderBatchedEdgeLayerDirectly)
    {
        foreach (var child in children)
        {
            if (renderCachedRasterLayerAsBitmap &&
                child is GraphAreaRasterCacheLayer cacheLayer &&
                cacheLayer.Bitmap is { } cachedBitmap &&
                cacheLayer.ContentBounds is { Width: > 0, Height: > 0 } cacheBounds)
            {
                context.DrawImage(cachedBitmap, new Rect(cachedBitmap.Size), mapToDestination(cacheBounds));
                continue;
            }

            if (!child.IsVisible || child.Bounds is not { Width: > 0, Height: > 0 }) continue;

            if (renderBatchedEdgeLayerDirectly && child is BatchedEdgeLayer batchedEdgeLayer)
            {
                RenderBatchedEdgeLayer(context, batchedEdgeLayer, mapToDestination);
                continue;
            }

            var origin = child.TranslatePoint(default, graphArea);
            if (origin is not { } o) continue;

            var destination = mapToDestination(new Rect(o, child.Bounds.Size));
            var brush = new VisualBrush
            {
                Visual = child,
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                TileMode = TileMode.None,
                SourceRect = new RelativeRect(new Rect(default, child.Bounds.Size), RelativeUnit.Absolute)
            };
            context.DrawRectangle(brush, null, destination);
        }
    }

    private static void RenderBatchedEdgeLayer(
        DrawingContext context,
        BatchedEdgeLayer batchedEdgeLayer,
        Func<Rect, Rect> mapToDestination)
    {
        // The layer draws its edges relative to its own (graph-space) origin. Map that origin through the
        // same transform used for every other child so a translate-only destination mapping stays correct.
        var destinationOrigin = mapToDestination(new Rect(default(Point), default(Size))).Position;
        using (context.PushTransform(Matrix.CreateTranslation(destinationOrigin.X, destinationOrigin.Y)))
            batchedEdgeLayer.Render(context);
    }
}
