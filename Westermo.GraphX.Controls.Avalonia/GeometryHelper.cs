using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Westermo.GraphX.Common.Enums;

/* Code here is partially used from NodeXL (https://nodexl.codeplex.com/)
 *
 *
 *
 * */

namespace Westermo.GraphX.Controls;

public static class GeometryHelper
{
    /// <summary>
    /// Get Intersection point on a rectangular surface
    /// </summary>
    /// <param name="a1">a1 is line1 start</param>
    /// <param name="a2">a2 is line1 end</param>
    /// <param name="b1">b1 is line2 start</param>
    /// <param name="b2">b2 is line2 end</param>
    /// <returns></returns>
    public static Vector? Intersects(Vector a1, Vector a2, Vector b1, Vector b2)
    {
        var a = a2 - a1;
        var b = b2 - b1;
        var aDotBPerpendicular = a.X * b.Y - a.Y * b.X;

        // if a dot b == 0, it means the lines are parallel so have infinite intersection points
        if (aDotBPerpendicular == 0)
            return null;

        var c = b1 - a1;

        // The intersection must fall within the line segment defined by the b1 and b2 endpoints.
        var u = (c.X * a.Y - c.Y * a.X) / aDotBPerpendicular;
        if (u is < 0 or > 1)
        {
            return null;
        }

        // The intersection point IS allowed to fall outside the line segment defined by the a1 and a2
        // endpoints, anywhere along the infinite line. When this is used to find the intersection of an
        // Edge as line a and Vertex side as line b, it allows the Edge to be elongated to the intersection.
        var t = (c.X * b.Y - c.Y * b.X) / aDotBPerpendicular;

        return a1 + t * a;
    }

    /// <summary>
    /// Generate PathGeometry object with curved Path using supplied route points
    /// </summary>
    /// <param name="points">Route points</param>
    /// <param name="tension"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static List<Point> GetCurveThroughPoints(Span<Point> points, double tension, double tolerance)
    {
        Debug.Assert(points.Length >= 2);
        Debug.Assert(tolerance > 0);

            // Pre-calculate estimated capacity to reduce list resizing
            var estimatedCapacity = EstimateCurvePointCount(points, tolerance);
            var oPolyLineSegment = new List<Point>(estimatedCapacity);

        if (points.Length == 2)
        {
            AddPointsToPolyLineSegment(oPolyLineSegment, points[0], points[0],
                points[1], points[1], tension, tolerance);
        }
        else
        {
            var iPoints = points.Length;

            for (var i = 0; i < iPoints; i++)
            {
                if (i == 0)
                {
                    AddPointsToPolyLineSegment(oPolyLineSegment, points[0],
                        points[0], points[1], points[2], tension, tolerance);
                }

                else if (i == iPoints - 2)
                {
                    AddPointsToPolyLineSegment(oPolyLineSegment, points[i - 1],
                        points[i], points[i + 1], points[i + 1], tension,
                        tolerance);
                }
                else if (i != iPoints - 1)
                {
                    AddPointsToPolyLineSegment(oPolyLineSegment, points[i - 1],
                        points[i], points[i + 1], points[i + 2], tension,
                        tolerance);
                }
            }

            oPolyLineSegment.Insert(0, points[0]);
        }

        return oPolyLineSegment;
    }

        /// <summary>
        /// Estimates the number of points needed for the curve to pre-allocate list capacity.
        /// </summary>
        /// <param name="points">The sequence of points that define the curve.</param>
        /// <param name="tolerance">The tolerance used when approximating the curve; smaller values typically require more points.</param>
        /// <returns>The estimated number of points required to represent the curve, used to pre-allocate the list capacity.</returns>
        private static int EstimateCurvePointCount(Span<Point> points, double tolerance)
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

        public static IList<Point> GetCurvePointsThroughPoints(Point[] points, double tension, double tolerance)
        {
            return GetCurveThroughPoints(points, tension, tolerance);
        }


    private static void AddPointsToPolyLineSegment(List<Point> oPolyLineSegment, Point oPoint0, Point oPoint1,
        Point oPoint2, Point oPoint3, double dTension, double dTolerance)
    {
        Debug.Assert(oPolyLineSegment != null);
        Debug.Assert(dTolerance > 0);

        var iPoints = (int)((Math.Abs(oPoint1.X - oPoint2.X) +
                             Math.Abs(oPoint1.Y - oPoint2.Y)) / dTolerance);


        if (iPoints <= 2)
        {
            oPolyLineSegment.Add(oPoint2);
        }
        else
        {
            var dSx1 = dTension * (oPoint2.X - oPoint0.X);
            var dSy1 = dTension * (oPoint2.Y - oPoint0.Y);
            var dSx2 = dTension * (oPoint3.X - oPoint1.X);
            var dSy2 = dTension * (oPoint3.Y - oPoint1.Y);

            var dAx = dSx1 + dSx2 + 2 * oPoint1.X - 2 * oPoint2.X;
            var dAy = dSy1 + dSy2 + 2 * oPoint1.Y - 2 * oPoint2.Y;
            var dBx = -2 * dSx1 - dSx2 - 3 * oPoint1.X + 3 * oPoint2.X;
            var dBy = -2 * dSy1 - dSy2 - 3 * oPoint1.Y + 3 * oPoint2.Y;

            var dCx = dSx1;
            var dCy = dSy1;
            var dDx = oPoint1.X;
            var dDy = oPoint1.Y;

                // Pre-calculate divisor to avoid repeated division
                var divisor = 1.0 / (iPoints - 1);

                // Note that this starts at 1, not 0.
                for (var i = 1; i < iPoints; i++)
                {
                    var t = i * divisor;
                    var t2 = t * t;      // Cache t squared
                    var t3 = t2 * t;     // Cache t cubed

                    var oPoint = new Point(
                        dAx * t3 + dBx * t2 + dCx * t + dDx,
                        dAy * t3 + dBy * t2 + dCy * t + dDy
                    );

                oPolyLineSegment.Add(oPoint);
            }
        }
    }

    public static PathFigure GetPathFigureFromPathSegments(Point oStartPoint, bool bPathFigureIsFilled,
        params PathSegment[] aoPathSegments)
    {
        Debug.Assert(aoPathSegments != null);

        var oPathFigure = new PathFigure { StartPoint = oStartPoint, IsFilled = bPathFigureIsFilled };
        var oSegments = oPathFigure.Segments!;

        foreach (var oPathSegment in aoPathSegments)
        {
            oSegments.Add(oPathSegment);
        }

        return oPathFigure;
    }


    public static PathGeometry GetPathGeometryFromPathSegments(Point oStartPoint, bool bPathFigureIsFilled,
        params PathSegment[] aoPathSegments)
    {
        Debug.Assert(aoPathSegments != null);

        var oPathFigure = new PathFigure { StartPoint = oStartPoint, IsFilled = bPathFigureIsFilled };
        var oSegments = oPathFigure.Segments!;

        foreach (var oPathSegment in aoPathSegments)
        {
            oSegments.Add(oPathSegment);
        }

        var oPathGeometry = new PathGeometry();
        oPathGeometry.Figures!.Add(oPathFigure);
        return oPathGeometry;
    }

    /// <summary>
    /// Returns edge endpoint based on vertex math shape and rotation angle
    /// </summary>
    /// <param name="source">Vertex position</param>
    /// <param name="sourceSize">Vertex bounds</param>
    /// <param name="target">Opposing point of the edge</param>
    /// <param name="shape">Vertex math shape</param>
    /// <param name="angle">Vertex rotation angle</param>
    public static Point GetEdgeEndpoint(Point source, Rect sourceSize, Point target, VertexShape shape,
        double angle = 0)
    {
        return shape switch
        {
            VertexShape.Circle => GetEdgeEndpointOnCircle(source,
                Math.Max(sourceSize.Height, sourceSize.Width) * .5, target, angle),
            VertexShape.Ellipse => GetEdgeEndpointOnEllipse(source, sourceSize.Width * .5,
                sourceSize.Height * .5,
                target, angle),
            VertexShape.Diamond => GetEdgeEndpointOnDiamond(source, sourceSize.Width * .5, target),
            VertexShape.Triangle => GetEdgeEndpointOnTriangle(source, sourceSize.Width * .5, target),
            _ => GetEdgeEndpointOnRectangle(source, sourceSize, target, angle),
        };
    }

    public static Point GetEdgeEndpointOnCircle(Point oVertexALocation, double dVertexARadius,
        Point oVertexBLocation, double angle = 0)
    {
        if (double.IsNaN(dVertexARadius)) return oVertexALocation;
        Debug.Assert(dVertexARadius >= 0);

        var dEdgeAngle = MathHelper.GetAngleBetweenPoints(oVertexALocation.ToGraphX(), oVertexBLocation.ToGraphX());
        var pt = new Point(
            oVertexALocation.X + dVertexARadius * Math.Cos(dEdgeAngle),
            oVertexALocation.Y - dVertexARadius * Math.Sin(dEdgeAngle)
        );
        return pt;
    }

    public static Point GetEdgeEndpointOnEllipse(Point oVertexALocation, double dVertexARadiusWidth,
        double dVertexARadiusHeight, Point oVertexBLocation, double angle = 0)
    {
        Debug.Assert(dVertexARadiusWidth >= 0);
        Debug.Assert(dVertexARadiusHeight >= 0);

        var sourcePoint = oVertexALocation;
        var targetPoint = oVertexBLocation;

        var dEdgeAngle = MathHelper.GetAngleBetweenPoints(sourcePoint.ToGraphX(), targetPoint.ToGraphX());
        if (angle != 0)
            dEdgeAngle = (dEdgeAngle.ToDegrees() + angle).ToRadians();

        var pt = new Point(
            sourcePoint.X + dVertexARadiusWidth * Math.Cos(dEdgeAngle),
            sourcePoint.Y - dVertexARadiusHeight * Math.Sin(dEdgeAngle)
        );
        if (angle != 0)
            pt = MathHelper.RotatePoint(pt.ToGraphX(), oVertexALocation.ToGraphX(), angle).ToAvalonia();
        return pt;
    }


    public static Point GetEdgeEndpointOnTriangle(Point oVertexLocation, double mDHalfWidth, Point otherEndpoint)
    {
        // Instead of doing geometry calculations similar to what is done in 
        // VertexDrawingHistory.GetEdgePointOnRectangle(), make use of that
        // method by making the triangle look like a rectangle.  First, figure
        // out how to rotate the triangle about the vertex location so that the
        // side containing the endpoint is vertical and to the right of the
        // vertex location.

        var dEdgeAngle = MathHelper.GetAngleBetweenPoints(
            oVertexLocation.ToGraphX(), otherEndpoint.ToGraphX());

        var dEdgeAngleDegrees = dEdgeAngle.ToDegrees();

        var dAngleToRotateDegrees = dEdgeAngleDegrees switch
        {
            >= -30.0 and < 90.0 => 30.0,
            >= -150.0 and < -30.0 => 270.0,
            _ => 150.0
        };

        // Now create a rotated rectangle that is centered on the vertex
        // location and that has the vertical, endpoint-containing triangle
        // side as the rectangle's right edge.

        var dWidth = 2.0 * mDHalfWidth;

        var oRotatedRectangle = new Rect(
            oVertexLocation.X,
            oVertexLocation.Y - mDHalfWidth,
            dWidth * MathHelper.Tangent30Degrees,
            dWidth
        );

        var oMatrix = GetRotatedMatrix(oVertexLocation,
            dAngleToRotateDegrees);

        // Rotate the other vertex location.
        var oRotatedOtherVertexLocation = oMatrix.Transform(otherEndpoint);

        // GetEdgeEndpointOnRectangle will compute an endpoint on the
        // rectangle's right edge.
        var oRotatedEdgeEndpoint = GetEdgeEndpointOnRectangle(oVertexLocation, oRotatedRectangle,
            oRotatedOtherVertexLocation);

        // Now rotate the edge endpoint in the other direction.
        oMatrix = GetRotatedMatrix(oVertexLocation,
            -dAngleToRotateDegrees);

        return oMatrix.Transform(oRotatedEdgeEndpoint);
    }

    public static Point GetEdgeEndpointOnDiamond(Point oVertexLocation, double mDHalfWidth, Point otherEndpoint)
    {
        // A diamond is just a rotated square, so the
        // GetEdgePointOnRectangle() can be used if the
        // diamond and the other vertex location are first rotated 45 degrees
        // about the diamond's center.

        var dHalfSquareWidth = mDHalfWidth / Math.Sqrt(2.0);

        var oRotatedDiamond = new Rect(
            oVertexLocation.X - dHalfSquareWidth,
            oVertexLocation.Y - dHalfSquareWidth,
            2.0 * dHalfSquareWidth,
            2.0 * dHalfSquareWidth
        );

        var oMatrix = GetRotatedMatrix(oVertexLocation, 45);
        var oRotatedOtherVertexLocation = oMatrix.Transform(otherEndpoint);

        var oRotatedEdgeEndpoint =
            GetEdgeEndpointOnRectangle(oVertexLocation, oRotatedDiamond, oRotatedOtherVertexLocation);

        // Now rotate the computed edge endpoint in the other direction.

        oMatrix = GetRotatedMatrix(oVertexLocation, -45);

        return oMatrix.Transform(oRotatedEdgeEndpoint);
        //
    }

    public static Point GetEdgeEndpointOnRectangle(Point sourcePos, Rect sourceBounds, Point targetPos,
        double angle = 0)
    {
        var targetPoint = Rotate(targetPos, -angle);

        if (targetPoint.X <= sourcePos.X)
        {
            var leftSide = Intersects(sourcePos.ToVector(), targetPoint.ToVector(),
                sourceBounds.TopLeft().ToVector(), sourceBounds.BottomLeft().ToVector());
            if (leftSide.HasValue)
            {
                return Rotate(new Point(leftSide.Value.X, leftSide.Value.Y), angle);
            }
        }
        else
        {
            var rightSide = Intersects(sourcePos.ToVector(), targetPoint.ToVector(),
                sourceBounds.TopRight().ToVector(), sourceBounds.BottomRight().ToVector());
            if (rightSide.HasValue)
            {
                return Rotate(new Point(rightSide.Value.X, rightSide.Value.Y), angle);
            }
        }

        if (targetPoint.Y <= sourcePos.Y)
        {
            var topSide = Intersects(sourcePos.ToVector(), targetPoint.ToVector(),
                sourceBounds.TopLeft().ToVector(), sourceBounds.TopRight().ToVector());
            if (topSide.HasValue)
            {
                return Rotate(new Point(topSide.Value.X, topSide.Value.Y), angle);
            }
        }
        else
        {
            var bottomSide = Intersects(sourcePos.ToVector(), targetPoint.ToVector(),
                sourceBounds.BottomLeft().ToVector(), sourceBounds.BottomRight().ToVector());
            if (bottomSide.HasValue)
            {
                return Rotate(new Point(bottomSide.Value.X, bottomSide.Value.Y), angle);
            }
        }

        return Rotate(new Point(sourcePos.X, sourcePos.Y), angle);

        Point Rotate(Point p, double a) => angle == 0.0
            ? p
            : MathHelper.RotatePoint(p.ToGraphX(), sourceBounds.Center().ToGraphX(), a).ToAvalonia();
    }

    public static PathFigure GenerateOldArrow(Point ip1, Point ip2)
    {
        var p1 = new Vector(ip1.X, ip1.Y);
        var p2 = new Vector(ip2.X, ip2.Y);
        var v = p1 - p2;
        v = v / v.Length * 5;
        var n = new Vector(-v.Y, v.X) * 0.7;
        var ov1 = p2 + v - n;
        var ov2 = p2 + v + n;
        var fig = new PathFigure
        {
            StartPoint = ip2,
            Segments =
            [
                new LineSegment { Point = new Point(ov1.X, ov1.Y) },
                new LineSegment { Point = new Point(ov2.X, ov2.Y) }
            ],
            IsClosed = true
        };
        return fig;
    }

    public static PathFigure GenerateArrow(Point oArrowTipLocation, Point start, Point end,
        double customAngle = 0.1, double arrowWidth = 3.0)
    {
        // Compute the arrow's dimensions.  The width factor is arbitrary and
        // was determined experimentally.

        var dArrowAngle = Math.Abs(customAngle - 0.1) < 1e-9
            ? MathHelper.GetAngleBetweenPoints(start.ToGraphX(), end.ToGraphX())
            : customAngle;
        var dArrowTipX = oArrowTipLocation.X;
        var dArrowTipY = oArrowTipLocation.Y;
        var dArrowHalfHeight = arrowWidth / 2.0;
        var dX = dArrowTipX - arrowWidth;

        // Compute the arrow's three points as if the arrow were at an angle of
        // zero degrees, then use a rotated transform to adjust for the actual
        // specified angle.

        var oMatrix = GetRotatedMatrix(oArrowTipLocation,
            -dArrowAngle.ToDegrees());
        Point[] aoPoints =
        [
            // Index 0: Arrow tip.

            oMatrix.Transform(oArrowTipLocation),

            // Index 1: Arrow bottom.

            oMatrix.Transform(new Point(dX, dArrowTipY - dArrowHalfHeight)),

            // Index 2: Arrow top.

            oMatrix.Transform(new Point(dX, dArrowTipY + dArrowHalfHeight)),

            // Index 3: Center of the flat end of the arrow.
            //
            // Note: The 0.2 is to avoid a gap between the edge end-cap and the
            // flat end of the arrow, but it sometimes causes the two to
            // overlap slightly, and that can show if the edge isn't opaque.
            // What is the correct way to get the end-cap to merge invisibly
            // with the arrow?

            oMatrix.Transform(new Point(dX + 0.2, dArrowTipY))
        ];


        return GetPathFigureFromPoints(aoPoints[0], aoPoints[1], aoPoints[2]);
    }

    /// <summary>
    /// Returns matrix rotated around specified point by angle in degrees
    /// </summary>
    /// <param name="centerOfRotation">Rotation center</param>
    /// <param name="angleToRotateDegrees">Angle in degrees</param>
    public static Matrix GetRotatedMatrix(Point centerOfRotation, double angleToRotateDegrees)
    {
        return RotateAt(Matrix.Identity, angleToRotateDegrees, centerOfRotation.X, centerOfRotation.Y);
    }

    private static Matrix RotateAt(Matrix input, double angle, double centerX, double centerY)
    {
        angle %= 360.0; // Doing the modulo before converting to radians reduces total error
        return input * CreateRotationRadians(angle * (Math.PI / 180.0), centerX, centerY);
    }

    private static Matrix CreateRotationRadians(double angle, double centerX, double centerY)
    {
        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);
        var dx = centerX * (1.0 - cos) + centerY * sin;
        var dy = centerY * (1.0 - cos) - centerX * sin;

        return new Matrix(cos, sin, -sin, cos, dx, dy);
    }

    public static PathFigure GetPathFigureFromPoints(Point startPoint, params Point[] otherPoints)
    {
        var oPathFigure = new PathFigure { StartPoint = startPoint };
        var oPathSegmentCollection = new PathSegments();

        foreach (var item in otherPoints)
            oPathSegmentCollection.Add(new LineSegment
            {
                Point = item,
                IsStroked = true
            });


        oPathFigure.Segments = oPathSegmentCollection;
        oPathFigure.IsClosed = true;
        return oPathFigure;
    }

    public static PathGeometry GetPathGeometryFromPoints(Point startPoint, params Point[] otherPoints)
    {
        Debug.Assert(otherPoints != null);

        var iOtherPoints = otherPoints.Length;

        Debug.Assert(iOtherPoints > 0);

        var oPathFigure = new PathFigure
        {
            StartPoint = startPoint
        };

        var oPathSegmentCollection = new PathSegments();
        foreach (var item in otherPoints)
            oPathSegmentCollection.Add(new LineSegment()
            {
                Point = item,
                IsStroked = true
            });

        oPathFigure.Segments = oPathSegmentCollection;
        oPathFigure.IsClosed = true;


        var oPathGeometry = new PathGeometry();

        oPathGeometry.Figures!.Add(oPathFigure);

        return oPathGeometry;
    }
}