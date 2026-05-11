using System;
using System.Diagnostics;
using Avalonia;
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
            pt = MathHelper.RotateAround(pt.ToGraphX(), oVertexALocation.ToGraphX(), angle).ToAvalonia();
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
            : MathHelper.RotateAround(p.ToGraphX(), sourceBounds.Center().ToGraphX(), a).ToAvalonia();
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
}