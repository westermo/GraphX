using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;

namespace Westermo.GraphX.Controls.Controls;

public static class EdgeMathExtensions
{
    extension(ReadOnlySpan<Point> points)
    {
        public double PathLength()
        {
            var result = 0.0;
            for (var index = 0; index < points.Length - 1; index++)
            {
                var currentPoint = points[index];
                var nextPoint = points[index + 1];
                var lengthOfSegment =
                    MathHelper.DistanceTo(currentPoint, nextPoint);
                if (double.IsNaN(lengthOfSegment)) continue;
                result += lengthOfSegment;
            }

            return result;
        }

        public double FindHalfwayPoint(double edgeLength, out Point p1, out Point p2)
        {
            switch (points.Length)
            {
                // Degenerate polylines do not have a traversable segment, so return the available point data directly.
                case 0:
                    p1 = default;
                    p2 = default;
                    return 0;
                case 1:
                    p1 = points[0];
                    p2 = points[0];
                    return 0;
            }

            // We now want the midpoint along the entire polyline.
            edgeLength /= 2;
            p1 = points[0];
            p2 = points[1];
            var previousPoint = points[0];
            var remaining = edgeLength;
            var foundSegment = false;

            // Walk again to find the segment that contains the midpoint.
            for (var index = 1; index < points.Length; index++)
            {
                var currentPoint = points[index];
                var lengthOfSegment = MathHelper.DistanceTo(previousPoint, currentPoint);
                // Keep invalid segments consistent with PathLength(): skip them and preserve adjacency semantics
                // so the next iteration still considers the true neighbouring points.
                if (double.IsNaN(lengthOfSegment))
                {
                    previousPoint = currentPoint;
                    continue;
                }

                if (lengthOfSegment >= remaining)
                {
                    p1 = previousPoint;
                    p2 = currentPoint;
                    foundSegment = true;
                    break;
                }

                remaining -= lengthOfSegment;
                previousPoint = currentPoint;
            }

            // If the midpoint lies on the last segment to p2, handle it here.
            if (!foundSegment)
            {
                p1 = previousPoint;
                // 'remaining' is already the distance from newp1 along this last segment.
            }

            return remaining;
        }


        public Rect GetBounds(double padding)
        {
            if (points.IsEmpty)
                return default;
            // Collect bounds
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            foreach (var point in points)
            {
                if (point.X < minX) minX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.X > maxX) maxX = point.X;
                if (point.Y > maxY) maxY = point.Y;
            }

            // Expand bounds to include  padding
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public Point MidPoint(out double angle, out bool flipAxis, out Vector vector)
        {
            flipAxis = false;
            vector = Vector.Zero;
            angle = 0;
            if (points.IsEmpty)
                return default;

            if (points.Length == 1)
                return points[0];

            var edgeLength = points.PathLength();
            var remaining = points.FindHalfwayPoint(edgeLength, out var p1, out var p2);
            // After FindHalfwayPoint, p1 and p2 represent the segment containing the midpoint.
            // Compute flipAxis based on the updated segment endpoints, consistent with the non-routing branch.
            flipAxis = p1.X > p2.X;
            angle = MathHelper.GetAngleBetweenPoints(p1, p2);
            var vectorPoint = flipAxis ? p1 - p2 : p2 - p1;
            vector = vectorPoint.ToVector();
            return new Point(p1.X + remaining * Math.Cos(angle), p1.Y - remaining * Math.Sin(angle));
        }

        /// <summary>
        /// Generate PathGeometry object with curved Path using supplied route points
        /// </summary>
        /// <param name="target"></param>
        /// <param name="tension"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public List<Point> GetCurveThroughPoints(double tension, double tolerance)
        {
            var list = new List<Point>();
            points.GetCurveThroughPoints(list, tension, tolerance);
            return list;
        }

        /// <summary>
        /// Generate PathGeometry object with curved Path using supplied route points
        /// </summary>
        /// <param name="target"></param>
        /// <param name="tension"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public void GetCurveThroughPoints(List<Point> target, double tension, double tolerance)
        {
            Debug.Assert(points.Length >= 2);
            Debug.Assert(tolerance > 0);

            // Pre-calculate estimated capacity to reduce list resizing
            var estimatedCapacity = points.EstimateCurvePointCount(tolerance);
            target.Clear();
            target.EnsureCapacity(estimatedCapacity);

            if (points.Length == 2)
            {
                AddPointsToPolyLineSegment(target, points[0], points[0],
                    points[1], points[1], tension, tolerance);
            }
            else
            {
                var iPoints = points.Length;

                for (var i = 0; i < iPoints; i++)
                {
                    if (i == 0)
                    {
                        AddPointsToPolyLineSegment(target, points[0],
                            points[0], points[1], points[2], tension, tolerance);
                    }

                    else if (i == iPoints - 2)
                    {
                        AddPointsToPolyLineSegment(target, points[i - 1],
                            points[i], points[i + 1], points[i + 1], tension,
                            tolerance);
                    }
                    else if (i != iPoints - 1)
                    {
                        AddPointsToPolyLineSegment(target, points[i - 1],
                            points[i], points[i + 1], points[i + 2], tension,
                            tolerance);
                    }
                }

                target.Insert(0, points[0]);
            }
        }

        /// <summary>
        /// Estimates the number of points needed for the curve to pre-allocate list capacity.
        /// </summary>
        /// <param name="tolerance">The tolerance used when approximating the curve; smaller values typically require more points.</param>
        /// <returns>The estimated number of points required to represent the curve, used to pre-allocate the list capacity.</returns>
        private int EstimateCurvePointCount(double tolerance)
        {
            if (points.Length < 2) return 2;

            var totalDistance = 0.0;
            for (var i = 0; i < points.Length - 1; i++)
            {
                totalDistance += Math.Abs(points[i].X - points[i + 1].X) +
                                 Math.Abs(points[i].Y - points[i + 1].Y);
            }

            // Estimate based on total distance and tolerance, with some buffer
            return Math.Max(points.Length, (int)(totalDistance / tolerance) + points.Length);
        }
    }

    private static void AddPointsToPolyLineSegment(List<Point> oPolyLineSegment, Point p0, Point p1,
        Point p2, Point p3, double tension, double tolerance)
    {
        Debug.Assert(oPolyLineSegment != null);
        Debug.Assert(tolerance > 0);

        var iPoints = PolylinePointCount(p1, p2, tolerance);


        if (iPoints <= 2)
        {
            oPolyLineSegment.Add(p2);
        }
        else
        {
            var dSx1 = tension * (p2.X - p0.X);
            var dSy1 = tension * (p2.Y - p0.Y);
            var dSx2 = tension * (p3.X - p1.X);
            var dSy2 = tension * (p3.Y - p1.Y);

            var dAx = dSx1 + dSx2 + 2 * p1.X - 2 * p2.X;
            var dAy = dSy1 + dSy2 + 2 * p1.Y - 2 * p2.Y;
            var dBx = -2 * dSx1 - dSx2 - 3 * p1.X + 3 * p2.X;
            var dBy = -2 * dSy1 - dSy2 - 3 * p1.Y + 3 * p2.Y;

            var dCx = dSx1;
            var dCy = dSy1;
            var dDx = p1.X;
            var dDy = p1.Y;

            // Pre-calculate divisor to avoid repeated division
            var divisor = 1.0 / (iPoints - 1);

            // Note that this starts at 1, not 0.
            for (var i = 1; i < iPoints; i++)
            {
                var t = i * divisor;
                var t2 = t * t; // Cache t squared
                var t3 = t2 * t; // Cache t cubed

                var oPoint = new Point(
                    dAx * t3 + dBx * t2 + dCx * t + dDx,
                    dAy * t3 + dBy * t2 + dCy * t + dDy
                );

                oPolyLineSegment.Add(oPoint);
            }
        }
    }

    private static int PolylinePointCount(Point p1, Point p2, double dTolerance)
    {
        return (int)((Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y)) / dTolerance);
    }

    /// <param name="points">A span of points, in control coordinates, that define the polyline of the edge. The span may be modified in place (e.g. reversed).</param>
    extension(Span<Point> points)
    {
        /// <summary>
        /// Builds a <see cref="StreamGeometry"/> from normalized points.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometry"/> representing the edge path defined by the provided points.
        /// </returns>
        public StreamGeometry ToStreamGeometry()
        {
            // Handle edge case gracefully: if fewer than 2 points, return an empty geometry
            // This can occur temporarily during rapid vertex dragging or when edge endpoints
            // are removed due to arrow pointer adjustments on very short edges.
            if (points.Length < 2)
                return new StreamGeometry();

            var geometry = new StreamGeometry();
            using var ctx = geometry.Open();
            ctx.BeginFigure(points[0], false);
            for (var i = 1; i < points.Length; i++)
                ctx.LineTo(points[i]);
            ctx.EndFigure(false);

            return geometry;
        }
    }
}