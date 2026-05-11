using System;
using Avalonia;

namespace Westermo.GraphX.Controls.Controls.ZoomControl;

/// <summary>
/// Pure mathematical helpers for the <see cref="Wayfinder"/> control.
/// Extracted so that the geometry can be unit tested without an Avalonia
/// window or layout pass. All methods are allocation free and side effect free.
/// </summary>
internal static class WayfinderGeometry
{
    /// <summary>
    /// Computes the uniform scale factor that fits <paramref name="contentSize"/>
    /// inside <paramref name="wayfinderSize"/> while preserving aspect ratio.
    /// Returns 1.0 if either dimension is zero, NaN, or infinite.
    /// </summary>
    public static double ComputeScale(Size contentSize, Size wayfinderSize)
    {
        if (!IsFinitePositive(contentSize.Width) || !IsFinitePositive(contentSize.Height) ||
            !IsFinitePositive(wayfinderSize.Width) || !IsFinitePositive(wayfinderSize.Height))
            return 1.0;

        var sx = wayfinderSize.Width / contentSize.Width;
        var sy = wayfinderSize.Height / contentSize.Height;
        var scale = sx < sy ? sx : sy;
        return double.IsFinite(scale) && scale > 0 ? scale : 1.0;
    }

    /// <summary>
    /// Returns the rectangle (in wayfinder local coordinates) that the
    /// scaled-down content occupies. Always anchored at the origin.
    /// </summary>
    public static Rect ComputeContentBounds(Size contentSize, double scale)
    {
        if (!IsFinitePositive(contentSize.Width) || !IsFinitePositive(contentSize.Height) || scale <= 0)
            return default;

        return new Rect(0, 0,
            Math.Max(0, contentSize.Width * scale),
            Math.Max(0, contentSize.Height * scale));
    }

    /// <summary>
    /// Maps the currently visible region of the ZoomControl into wayfinder
    /// coordinates given the ZoomControl's <paramref name="zoom"/>,
    /// <paramref name="translateX"/>/<paramref name="translateY"/> and
    /// rendered <paramref name="zoomControlSize"/>, and the wayfinder's
    /// <paramref name="scale"/>.
    /// </summary>
    public static Rect ComputeViewportRect(
        double zoom, double translateX, double translateY,
        Size zoomControlSize, double scale)
    {
        if (zoom <= 0 || scale <= 0 ||
            !IsFinitePositive(zoomControlSize.Width) || !IsFinitePositive(zoomControlSize.Height))
            return default;

        // Visible content rect in content coordinates (mirrors ZoomControl.GetVisibleContentRect).
        var contentX = -translateX / zoom;
        var contentY = -translateY / zoom;
        var contentW = zoomControlSize.Width / zoom;
        var contentH = zoomControlSize.Height / zoom;

        return new Rect(contentX * scale, contentY * scale,
            Math.Max(0, contentW * scale), Math.Max(0, contentH * scale));
    }

    /// <summary>
    /// Clamps a wayfinder-space drag delta so that the viewport rectangle
    /// stays inside the content bounds on each axis independently. If the
    /// viewport already fully encloses the content along an axis (the entire
    /// content is visible on that axis), the corresponding component of the
    /// returned delta is zero — there is nothing to pan in that direction.
    /// Other axes are clamped to keep the viewport inside the content.
    /// </summary>
    public static Vector ClampDragDelta(Rect viewport, Rect content, Vector delta)
    {
        var dx = delta.X;
        var dy = delta.Y;

        // Horizontal: independently determine whether content is fully visible
        // along X. If so → no horizontal pan possible. Otherwise clamp so the
        // viewport stays inside content.
        if (viewport.X <= content.X && viewport.Right >= content.Right)
        {
            dx = 0;
        }
        else
        {
            if (viewport.X + dx < content.X) dx = content.X - viewport.X;
            if (viewport.Right + dx > content.Right) dx = content.Right - viewport.Right;
        }

        // Vertical: same logic, decoupled from horizontal so a graph that's
        // wider than tall (or vice versa) still pans on the constrained axis.
        if (viewport.Y <= content.Y && viewport.Bottom >= content.Bottom)
        {
            dy = 0;
        }
        else
        {
            if (viewport.Y + dy < content.Y) dy = content.Y - viewport.Y;
            if (viewport.Bottom + dy > content.Bottom) dy = content.Bottom - viewport.Bottom;
        }

        return new Vector(dx, dy);
    }

    private static bool IsFinitePositive(double v) => double.IsFinite(v) && v > 0;
}
