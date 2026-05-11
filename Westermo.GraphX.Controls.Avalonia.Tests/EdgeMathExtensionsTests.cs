using Avalonia;
using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public sealed class EdgeMathExtensionsTests
{
    private const double Tolerance = 1e-10;

    [Test]
    public async Task PathLength_ReturnsZero_WhenPolylineContainsSinglePoint()
    {
        // A single point does not form any line segments, so the total path length must be zero.
        Point[] points =
        [
            new(10, 20)
        ];

        var result = points.PathLength();

        await Assert.That(result).IsEqualTo(0d);
    }

    [Test]
    public async Task PathLength_ReturnsSumOfAllSegments_ForMultiSegmentPolyline()
    {
        // The chosen coordinates create two 3-4-5 triangles, making the expected total exact and stable.
        Point[] points =
        [
            new(0, 0),
            new(3, 4),
            new(6, 8)
        ];

        var result = points.PathLength();

        await Assert.That(result).IsEqualTo(10d);
    }

    [Test]
    public async Task PathLength_IgnoresSegmentsThatProduceNaN()
    {
        // The implementation explicitly skips segments whose computed distance is NaN.
        // This test documents that current behavior.
        Point[] points =
        [
            new(0, 0),
            new(3, 4),
            new(double.NaN, 0),
            new(10, 10)
        ];

        var result = points.PathLength();

        await Assert.That(result).IsEqualTo(5d);
    }

    [Test]
    public async Task FindHalfwayPoint_ReturnsSingleSegmentAndHalfDistance_ForStraightLine()
    {
        // For a single segment with total length 10, the halfway offset should be 5 on that same segment.
        Point[] points =
        [
            new(0, 0),
            new(6, 8)
        ];

        var edgeLength = points.PathLength();
        var remainingDistance = points.FindHalfwayPoint(edgeLength, out var p1, out var p2);

        await Assert.That(edgeLength).IsEqualTo(10d);
        await Assert.That(remainingDistance).IsEqualTo(5d);

        await Assert.That(p1.X).IsEqualTo(0d);
        await Assert.That(p1.Y).IsEqualTo(0d);
        await Assert.That(p2.X).IsEqualTo(6d);
        await Assert.That(p2.Y).IsEqualTo(8d);
    }

    [Test]
    public async Task FindHalfwayPoint_ReturnsSegmentEndingAtVertex_WhenHalfwayFallsExactlyOnBoundary()
    {
        // Total path length is 10, so halfway is exactly 5.
        // The first segment length is also 5, which means the midpoint lands exactly on the shared vertex.
        Point[] points =
        [
            new(0, 0),
            new(3, 4),
            new(6, 8)
        ];

        var edgeLength = points.PathLength();
        var remainingDistance = points.FindHalfwayPoint(edgeLength, out var p1, out var p2);

        await Assert.That(edgeLength).IsEqualTo(10);
        await Assert.That(remainingDistance).IsEqualTo(5);

        await Assert.That(p1.X).IsEqualTo(0);
        await Assert.That(p1.Y).IsEqualTo(0);
        await Assert.That(p2.X).IsEqualTo(3);
        await Assert.That(p2.Y).IsEqualTo(4);
    }

    [Test]
    public async Task FindHalfwayPoint_ReturnsLaterSegmentAndLocalOffset_WhenHalfwayFallsInsideSecondSegment()
    {
        // Segment lengths are 3 and 4, giving a total of 7 and a halfway distance of 3.5.
        // After consuming the first segment, 0.5 remains inside the second segment.
        Point[] points =
        [
            new(0, 0),
            new(0, 3),
            new(4, 3)
        ];

        var edgeLength = points.PathLength();
        var remainingDistance = points.FindHalfwayPoint(edgeLength, out var p1, out var p2);

        await Assert.That(edgeLength).IsEqualTo(7d);
        await Assert.That(remainingDistance).IsEqualTo(0.5d);

        await Assert.That(p1.X).IsEqualTo(0d);
        await Assert.That(p1.Y).IsEqualTo(3d);
        await Assert.That(p2.X).IsEqualTo(4d);
        await Assert.That(p2.Y).IsEqualTo(3d);
    }

    [Test]
    public async Task FindHalfwayPoint_SkipsNaNSegmentsWithoutBridgingAcrossInvalidWaypoint()
    {
        // The invalid waypoint splits the route into separate valid segments.
        // The halfway point must be resolved against the later valid segment instead of a synthetic bridge.
        Point[] points =
        [
            new(0, 0),
            new(double.NaN, 0),
            new(10, 0),
            new(20, 0)
        ];

        var edgeLength = points.PathLength();
        var remainingDistance = points.FindHalfwayPoint(edgeLength, out var p1, out var p2);

        await Assert.That(edgeLength).IsEqualTo(10d);
        await Assert.That(remainingDistance).IsEqualTo(5d);
        await AssertPointClose(p1, new Point(10, 0));
        await AssertPointClose(p2, new Point(20, 0));
    }

    [Test]
    public async Task GetBounds_ExpandsBoundsByPadding_ForMixedCoordinates()
    {
        // Padding should grow the bounds on all sides without changing the underlying extrema calculation.
        Point[] points =
        [
            new(-5, 2),
            new(4, -3),
            new(1, 7)
        ];

        var bounds = points.GetBounds(2d);

        await AssertRectClose(bounds, new Rect(-7, -5, 13, 14));
    }

    [Test]
    public async Task GetBounds_ReturnsZeroSizedRect_ForSinglePointWithoutPadding()
    {
        // A single point produces a degenerate rectangle anchored at that exact location.
        Point[] points =
        [
            new(4, 5)
        ];

        var bounds = points.GetBounds(0d);

        await AssertRectClose(bounds, new Rect(4, 5, 0, 0));
    }

    [Test]
    public async Task MidPoint_ReturnsSegmentMetadata_ForStraightHorizontalPolyline()
    {
        // The midpoint lies on the only segment, so the returned vector and angle should describe that segment directly.
        Point[] points =
        [
            new(0, 0),
            new(10, 0)
        ];

        var midpoint = points.MidPoint(out var angle, out var flipAxis, out var vector);

        await AssertPointClose(midpoint, new Point(5, 0));
        await AssertClose(angle, 0d);
        await Assert.That(flipAxis).IsFalse();
        await AssertVectorClose(vector, new Vector(10, 0));
    }

    [Test]
    public async Task MidPoint_UsesTheSegmentContainingTheHalfwayDistance()
    {
        // The total path length is 7, so the midpoint is 0.5 units into the horizontal tail segment.
        Point[] points =
        [
            new(0, 0),
            new(0, 3),
            new(4, 3)
        ];

        var midpoint = points.MidPoint(out var angle, out var flipAxis, out var vector);

        await AssertPointClose(midpoint, new Point(0.5, 3));
        await AssertClose(angle, 0d);
        await Assert.That(flipAxis).IsFalse();
        await AssertVectorClose(vector, new Vector(4, 0));
    }

    [Test]
    public async Task MidPoint_SetsFlipAxis_ForRightToLeftSegment()
    {
        // Reversed horizontal segments should still produce the same midpoint while flagging the axis flip.
        Point[] points =
        [
            new(10, 0),
            new(0, 0)
        ];

        var midpoint = points.MidPoint(out var angle, out var flipAxis, out var vector);

        await AssertPointClose(midpoint, new Point(5, 0));
        await AssertClose(angle, Math.PI);
        await Assert.That(flipAxis).IsTrue();
        await AssertVectorClose(vector, new Vector(10, 0));
    }

    [Test]
    public async Task MidPoint_SkipsNaNSegmentsConsistentlyWithPathLength()
    {
        // The midpoint should be computed from the surviving valid segment, matching the NaN-skipping path length logic.
        Point[] points =
        [
            new(0, 0),
            new(double.NaN, 0),
            new(10, 0),
            new(20, 0)
        ];

        var midpoint = points.MidPoint(out var angle, out var flipAxis, out var vector);

        await AssertPointClose(midpoint, new Point(15, 0));
        await AssertClose(angle, 0d);
        await Assert.That(flipAxis).IsFalse();
        await AssertVectorClose(vector, new Vector(10, 0));
    }

    [Test]
    public async Task GetCurveThroughPoints_TwoPoints_WithLargeTolerance_ReturnsOnlyEndpoint()
    {
        // With only two input points, the implementation takes the two-point branch which calls
        // AddPointsToPolyLineSegment directly without prepending points[0]. When the tolerance is
        // larger than the Manhattan distance, PolylinePointCount returns <= 2 and only the final
        // point is appended. This documents that exact behaviour.
        Point[] points =
        [
            new(0, 0),
            new(10, 0)
        ];

        var curve = points.GetCurveThroughPoints(tension: 0.5, tolerance: 100);

        await Assert.That(curve.Count).IsEqualTo(1);
        await AssertPointClose(curve[0], new Point(10, 0));
    }

    [Test]
    public async Task GetCurveThroughPoints_TwoPoints_SmallTolerance_StaysOnStraightLine()
    {
        // For a purely horizontal two-point input with zero tension, the Hermite spline degenerates
        // to the straight segment, so every generated point should have Y == 0 and the last point
        // must coincide with the final input point exactly (t = 1 in the parametric form).
        Point[] points =
        [
            new(0, 0),
            new(10, 0)
        ];

        var curve = points.GetCurveThroughPoints(tension: 0.0, tolerance: 1.0);

        await Assert.That(curve.Count).IsGreaterThan(2);
        foreach (var p in curve)
        {
            // All interpolated points must remain on the horizontal axis within numerical tolerance.
            await Assert.That(Math.Abs(p.Y)).IsLessThan(Tolerance);
        }
        await AssertPointClose(curve[^1], new Point(10, 0));
    }

    [Test]
    public async Task GetCurveThroughPoints_FourCollinearPoints_PreservesEndpointsAndMonotonicProgression()
    {
        // The three-or-more point branch inserts points[0] at index 0 and the last appended point
        // is points[^1]. For strictly increasing X values on a horizontal line the curve should
        // remain monotonic in X and retain both endpoints exactly.
        Point[] points =
        [
            new(0, 0),
            new(10, 0),
            new(20, 0),
            new(30, 0)
        ];

        var curve = points.GetCurveThroughPoints(tension: 0.5, tolerance: 1.0);

        await Assert.That(curve.Count).IsGreaterThanOrEqualTo(points.Length);
        await AssertPointClose(curve[0], points[0]);
        await AssertPointClose(curve[^1], points[^1]);

        // Monotonic X progression (non-decreasing) confirms no unexpected back-tracking on a straight route.
        for (var i = 1; i < curve.Count; i++)
        {
            await Assert.That(curve[i].X).IsGreaterThanOrEqualTo(curve[i - 1].X - Tolerance);
        }
    }

    [Test]
    public async Task GetCurveThroughPoints_ZigZag_StaysInsidePaddedBounds()
    {
        // A zig-zag route exercises the cardinal spline with non-collinear control points. The
        // resulting curve must start at points[0], end at points[^1], and remain inside a small
        // padding around the input bounding box.
        Point[] points =
        [
            new(0, 0),
            new(10, 20),
            new(20, 0),
            new(30, 20)
        ];

        var curve = points.GetCurveThroughPoints(tension: 0.5, tolerance: 0.5);

        await AssertPointClose(curve[0], points[0]);
        await AssertPointClose(curve[^1], points[^1]);

        // Compute a generously padded bounding box; a tension of 0.5 keeps the spline close to the polyline.
        ReadOnlySpan<Point> span = points;
        var bounds = span.GetBounds(padding: 15);
        foreach (var p in curve)
        {
            await Assert.That(bounds.Contains(p)).IsTrue();
        }
    }

    [Test]
    public async Task GetCurveThroughPoints_LargerTolerance_ProducesFewerOrEqualPoints()
    {
        // Tolerance directly controls segment subdivision via PolylinePointCount. Increasing the
        // tolerance must never increase the number of emitted points for the same input polyline.
        Point[] points =
        [
            new(0, 0),
            new(10, 5),
            new(20, -5),
            new(30, 0)
        ];

        var fineCurve = points.GetCurveThroughPoints(tension: 0.5, tolerance: 0.5);
        var coarseCurve = points.GetCurveThroughPoints(tension: 0.5, tolerance: 5.0);

        await Assert.That(coarseCurve.Count).IsLessThanOrEqualTo(fineCurve.Count);
    }

    private static async Task AssertClose(double actual, double expected)
    {
        await Assert.That(Math.Abs(actual - expected)).IsLessThan(Tolerance);
    }

    private static async Task AssertPointClose(Point actual, Point expected)
    {
        await AssertClose(actual.X, expected.X);
        await AssertClose(actual.Y, expected.Y);
    }

    private static async Task AssertRectClose(Rect actual, Rect expected)
    {
        await AssertClose(actual.X, expected.X);
        await AssertClose(actual.Y, expected.Y);
        await AssertClose(actual.Width, expected.Width);
        await AssertClose(actual.Height, expected.Height);
    }

    private static async Task AssertVectorClose(Vector actual, Vector expected)
    {
        await AssertClose(actual.X, expected.X);
        await AssertClose(actual.Y, expected.Y);
    }
}