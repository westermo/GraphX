using System;
using MPoint = Westermo.GraphX.Measure.Point;
using MRect = Westermo.GraphX.Measure.Rect;
using MSize = Westermo.GraphX.Measure.Size;
using MVector = Westermo.GraphX.Measure.Vector;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for the platform-independent geometry primitives in <c>Westermo.GraphX.Measure</c>.
/// These structs back every layout, overlap-removal and edge-routing calculation in the logic core,
/// so subtle arithmetic mistakes here propagate into every rendered graph.
/// Tests marked with <c>[Skip]</c> describe the correct behaviour for known defects in the current implementation.
/// </summary>
public class MeasureTypesTests
{
    #region Point

    [Test]
    public async Task Point_Arithmetic_WithVectorsAndPoints()
    {
        var p = new MPoint(3, 4);
        var v = new MVector(1, 2);

        await Assert.That(p + v == new MPoint(4, 6)).IsTrue();
        await Assert.That(p - v == new MPoint(2, 2)).IsTrue();
        await Assert.That(p + new MPoint(1, 1) == new MPoint(4, 5)).IsTrue();

        // Point - Point yields the displacement vector between them.
        MVector diff = p - new MPoint(1, 1);
        await Assert.That(diff == new MVector(2, 3)).IsTrue();
    }

    [Test]
    public async Task Point_ScalarMultiplication_IsCommutative()
    {
        var p = new MPoint(2, -3);
        await Assert.That(p * 2 == new MPoint(4, -6)).IsTrue();
        await Assert.That(2 * p == new MPoint(4, -6)).IsTrue();
    }

    [Test]
    public async Task Point_Division_DividesComponents()
    {
        var p = new MPoint(8, 4);
        await Assert.That(p / 2 == new MPoint(4, 2)).IsTrue();
    }

    [Test]
    public async Task Point_EqualityOperator_UsesTolerance_WhileEqualsIsExact()
    {
        var a = new MPoint(1, 1);
        var b = new MPoint(1 + 1e-14, 1);

        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(a.Equals((object)new MPoint(1, 1))).IsTrue();
        await Assert.That(a.Equals((object)"not a point")).IsFalse();
    }

    [Test]
    public async Task Point_Conversions()
    {
        // Implicit conversions from Size and Vector.
        MPoint fromSize = new MSize(5, 6);
        MPoint fromVector = new MVector(-1, 2);
        await Assert.That(fromSize == new MPoint(5, 6)).IsTrue();
        await Assert.That(fromVector == new MPoint(-1, 2)).IsTrue();

        // Explicit conversion to Size takes absolute values so negative coordinates never throw.
        var size = (MSize)new MPoint(-3, -4);
        await Assert.That(size.Width).IsEqualTo(3);
        await Assert.That(size.Height).IsEqualTo(4);

        var vector = (MVector)new MPoint(7, 8);
        await Assert.That(vector == new MVector(7, 8)).IsTrue();
    }

    [Test]
    public async Task Point_Offset_MutatesInPlace_AndToStringFormatsXY()
    {
        var p = new MPoint(1, 2);
        p.Offset(10, 20);
        await Assert.That(p.X).IsEqualTo(11);
        await Assert.That(p.Y).IsEqualTo(22);
        await Assert.That(new MPoint(1, 2).ToString()).IsEqualTo($"{1d}:{2d}");
        await Assert.That(MPoint.Zero == new MPoint(0, 0)).IsTrue();
    }

    #endregion

    #region Vector

    [Test]
    public async Task Vector_Arithmetic()
    {
        var a = new MVector(1, 2);
        var b = new MVector(3, 4);

        await Assert.That(a + b == new MVector(4, 6)).IsTrue();
        await Assert.That(b - a == new MVector(2, 2)).IsTrue();
        await Assert.That(-a == new MVector(-1, -2)).IsTrue();
        await Assert.That(a * 2.0 == new MVector(2, 4)).IsTrue();
        await Assert.That(2.0 * a == new MVector(2, 4)).IsTrue();
        await Assert.That(3 * a == new MVector(3, 6)).IsTrue();
        await Assert.That(b / 2 == new MVector(1.5, 2)).IsTrue();

        // Vector * Vector is the dot product.
        await Assert.That(a * b).IsEqualTo(11);

        MPoint p = a + new MPoint(10, 10);
        await Assert.That(p == new MPoint(11, 12)).IsTrue();
    }

    [Test]
    public async Task Vector_LengthAndNormalize()
    {
        var v = new MVector(3, 4);
        await Assert.That(v.Length).IsEqualTo(5);
        await Assert.That(v.LengthSquared).IsEqualTo(25);

        v.Normalize();
        await Assert.That(Math.Abs(v.Length - 1) < 1e-12).IsTrue();
        await Assert.That(v == new MVector(0.6, 0.8)).IsTrue();
    }

    [Test]
    public async Task Vector_Negate_CrossProduct_AngleBetween()
    {
        var v = new MVector(1, -2);
        v.Negate();
        await Assert.That(v == new MVector(-1, 2)).IsTrue();

        var x = new MVector(1, 0);
        var y = new MVector(0, 1);
        await Assert.That(MVector.CrossProduct(x, y)).IsEqualTo(1);
        await Assert.That(MVector.CrossProduct(y, x)).IsEqualTo(-1);

        // AngleBetween is signed and expressed in degrees.
        await Assert.That(Math.Abs(MVector.AngleBetween(x, y) - 90) < 1e-9).IsTrue();
        await Assert.That(Math.Abs(MVector.AngleBetween(y, x) + 90) < 1e-9).IsTrue();
        await Assert.That(Math.Abs(MVector.AngleBetween(x, -x) - 180) < 1e-9).IsTrue();
    }

    [Test]
    public async Task Vector_ToPoint_AndZero()
    {
        await Assert.That(new MVector(2, 3).ToPoint == new MPoint(2, 3)).IsTrue();
        await Assert.That(MVector.Zero.Length).IsEqualTo(0);
    }

    #endregion

    #region Size

    [Test]
    public async Task Size_Constructor_RejectsNegativeDimensions()
    {
        Assert.Throws<ArgumentException>(() => new MSize(-1, 1));
        Assert.Throws<ArgumentException>(() => new MSize(1, -1));
        var ok = new MSize(0, 0);
        await Assert.That(ok.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Size_Empty_IsEmpty_AndCannotBeModified()
    {
        var empty = MSize.Empty;
        await Assert.That(empty.IsEmpty).IsTrue();
        Assert.Throws<InvalidOperationException>(() => empty.Width = 1);
        Assert.Throws<InvalidOperationException>(() => empty.Height = 1);
        await Assert.That(MSize.Equals(MSize.Empty, MSize.Empty)).IsTrue();
        await Assert.That(MSize.Empty.GetHashCode()).IsEqualTo(0);
    }

    [Test]
    public async Task Size_Setters_RejectNegativeValues()
    {
        var s = new MSize(1, 1);
        Assert.Throws<ArgumentException>(() => s.Width = -1);
        Assert.Throws<ArgumentException>(() => s.Height = -1);
        s.Width = 10;
        await Assert.That(s.Width).IsEqualTo(10);
        await Assert.That(s.Height).IsEqualTo(1);
    }

    [Test]
    public async Task Size_HeightSetter_DoesNotChangeWidth()
    {
        var s = new MSize(10, 1);
        s.Height = 20;
        await Assert.That(s.Width).IsEqualTo(10);
        await Assert.That(s.Height).IsEqualTo(20);
    }

    [Test]
    public async Task Size_Conversions_AndEquality()
    {
        var s = new MSize(2, 3);
        await Assert.That((MVector)s == new MVector(2, 3)).IsTrue();
        // NOTE (anti-pattern): Size declares `explicit operator Point(Size)` while Point declares
        // `implicit operator Point(Size)`, so `(Point)size` is ambiguous (CS0457) and cannot be written.
        // Only the Vector conversion is usable from C#.
        await Assert.That(s == new MSize(2, 3)).IsTrue();
        await Assert.That(s != new MSize(3, 2)).IsTrue();
        await Assert.That(s.Equals((object)new MSize(2, 3))).IsTrue();
    }

    #endregion

    #region Rect - construction

    [Test]
    public async Task Rect_Constructors()
    {
        var r1 = new MRect(new MPoint(1, 2), new MSize(3, 4));
        await Assert.That(r1 == new MRect(1, 2, 3, 4)).IsTrue();

        // Two-point constructor normalises the corners regardless of their order.
        var r2 = new MRect(new MPoint(4, 6), new MPoint(1, 2));
        await Assert.That(r2 == new MRect(1, 2, 3, 4)).IsTrue();

        var r3 = new MRect(new MPoint(1, 2), new MVector(3, 4));
        await Assert.That(r3 == new MRect(1, 2, 3, 4)).IsTrue();

        var r4 = new MRect(new MSize(5, 6));
        await Assert.That(r4 == new MRect(0, 0, 5, 6)).IsTrue();

        Assert.Throws<ArgumentException>(() => new MRect(0, 0, -1, 1));
        Assert.Throws<ArgumentException>(() => new MRect(0, 0, 1, -1));
    }

    [Test]
    public async Task Rect_ConstructedFromEmptySize_IsZeroRect()
    {
        var r = new MRect(new MPoint(5, 5), MSize.Empty);
        await Assert.That(r.IsEmpty).IsFalse();
        await Assert.That(r == new MRect(0, 0, 0, 0)).IsTrue();
        await Assert.That(new MRect(MSize.Empty) == new MRect(0, 0, 0, 0)).IsTrue();
    }

    [Test]
    public async Task Rect_EdgesAndCorners()
    {
        var r = new MRect(1, 2, 3, 4);
        await Assert.That(r.Left).IsEqualTo(1);
        await Assert.That(r.Top).IsEqualTo(2);
        await Assert.That(r.Right).IsEqualTo(4);
        await Assert.That(r.Bottom).IsEqualTo(6);
        await Assert.That(r.TopLeft == new MPoint(1, 2)).IsTrue();
        await Assert.That(r.TopRight == new MPoint(4, 2)).IsTrue();
        await Assert.That(r.BottomLeft == new MPoint(1, 6)).IsTrue();
        await Assert.That(r.BottomRight == new MPoint(4, 6)).IsTrue();
    }

    [Test]
    public async Task Rect_Empty_Semantics()
    {
        var e = MRect.Empty;
        await Assert.That(e.IsEmpty).IsTrue();
        await Assert.That(double.IsNegativeInfinity(e.Right)).IsTrue();
        await Assert.That(double.IsNegativeInfinity(e.Bottom)).IsTrue();
        await Assert.That(e.Size.IsEmpty).IsTrue();
        await Assert.That(MRect.Equals(MRect.Empty, MRect.Empty)).IsTrue();
        await Assert.That(MRect.Empty.GetHashCode()).IsEqualTo(0);

        Assert.Throws<InvalidOperationException>(() => e.Width = 1);
        Assert.Throws<InvalidOperationException>(() => e.Height = 1);
        Assert.Throws<InvalidOperationException>(() => e.Offset(1, 1));
        Assert.Throws<InvalidOperationException>(() => e.Offset(new MVector(1, 1)));
        Assert.Throws<InvalidOperationException>(() => e.Inflate(1, 1));
    }

    [Test]
    public async Task Rect_LocationAndSizeSetters()
    {
        var r = new MRect(1, 2, 3, 4);
        r.Location = new MPoint(10, 20);
        r.Size = new MSize(30, 40);
        await Assert.That(r == new MRect(10, 20, 30, 40)).IsTrue();

        // Assigning an empty size collapses the rect to zero dimensions.
        r.Size = MSize.Empty;
        await Assert.That(r.Width).IsEqualTo(0);
        await Assert.That(r.Height).IsEqualTo(0);

        Assert.Throws<ArgumentException>(() => r.Width = -1);
        Assert.Throws<ArgumentException>(() => r.Height = -1);
    }

    #endregion

    #region Rect - Offset / Inflate

    [Test]
    public async Task Rect_Offset_InstanceAndStatic()
    {
        var r = new MRect(0, 0, 2, 2);
        r.Offset(1, 2);
        await Assert.That(r == new MRect(1, 2, 2, 2)).IsTrue();
        r.Offset(new MVector(-1, -2));
        await Assert.That(r == new MRect(0, 0, 2, 2)).IsTrue();

        // Static overloads return a moved copy and leave the source untouched (struct semantics).
        var moved = MRect.Offset(r, 5, 5);
        await Assert.That(moved == new MRect(5, 5, 2, 2)).IsTrue();
        await Assert.That(r == new MRect(0, 0, 2, 2)).IsTrue();
        await Assert.That(MRect.Offset(r, new MVector(3, 4)) == new MRect(3, 4, 2, 2)).IsTrue();
    }

    [Test]
    public async Task Rect_Inflate_GrowsOnAllSides()
    {
        var r = new MRect(10, 10, 4, 4);
        r.Inflate(1, 2);
        await Assert.That(r == new MRect(9, 8, 6, 8)).IsTrue();

        await Assert.That(MRect.Inflate(new MRect(10, 10, 4, 4), new MSize(1, 1)) == new MRect(9, 9, 6, 6)).IsTrue();
        await Assert.That(MRect.Inflate(new MRect(10, 10, 4, 4), 1, 1) == new MRect(9, 9, 6, 6)).IsTrue();

        var source = new MRect(10, 10, 4, 4);
        var copy = MRect.InflateNew(source, 1, 1);
        await Assert.That(copy == new MRect(9, 9, 6, 6)).IsTrue();
        await Assert.That(source == new MRect(10, 10, 4, 4)).IsTrue();
    }

    [Test]
    public async Task Rect_Deflate_PastZero_CollapsesToZeroRect()
    {
        var r = new MRect(10, 10, 4, 4);
        r.Inflate(-3, -3);
        await Assert.That(r == new MRect(0, 0, 0, 0)).IsTrue();
    }

    #endregion

    #region Rect - Intersections

    [Test]
    public async Task Rect_IntersectsWith_IncludesTouchingEdges()
    {
        var a = new MRect(0, 0, 10, 10);
        await Assert.That(a.IntersectsWith(new MRect(5, 5, 10, 10))).IsTrue();
        await Assert.That(a.IntersectsWith(new MRect(10, 0, 5, 5))).IsTrue();
        await Assert.That(a.IntersectsWith(new MRect(10.001, 0, 5, 5))).IsFalse();
        await Assert.That(a.IntersectsWith(MRect.Empty)).IsFalse();
        await Assert.That(MRect.Empty.IntersectsWith(a)).IsFalse();
    }

    [Test]
    public async Task Rect_Intersect_ReturnsOverlapArea()
    {
        var a = new MRect(0, 0, 10, 10);
        var overlap = MRect.Intersect(a, new MRect(5, 6, 10, 10));
        await Assert.That(overlap == new MRect(5, 6, 5, 4)).IsTrue();

        // Instance version mutates the receiver.
        a.Intersect(new MRect(2, 2, 3, 3));
        await Assert.That(a == new MRect(2, 2, 3, 3)).IsTrue();
    }

    [Test]
    public async Task Rect_Intersect_Disjoint_YieldsZeroRect()
    {
        var result = MRect.Intersect(new MRect(0, 0, 1, 1), new MRect(5, 5, 1, 1));
        await Assert.That(result.IsEmpty).IsFalse();
        await Assert.That(result == new MRect(0, 0, 0, 0)).IsTrue();
    }

    #endregion

    #region Rect - Union

    [Test]
    public async Task Rect_Union_ProducesBoundingBox()
    {
        var u = MRect.Union(new MRect(0, 0, 2, 2), new MRect(5, 6, 1, 1));
        await Assert.That(u == new MRect(0, 0, 6, 7)).IsTrue();

        var withPoint = MRect.Union(new MRect(0, 0, 2, 2), new MPoint(-3, 4));
        await Assert.That(withPoint == new MRect(-3, 0, 5, 4)).IsTrue();

        var instance = new MRect(1, 1, 1, 1);
        instance.Union(new MPoint(0, 0));
        await Assert.That(instance == new MRect(0, 0, 2, 2)).IsTrue();
    }

    [Test]
    public async Task Rect_Union_WithEmptyArgument_LeavesRectUnchanged()
    {
        var r = new MRect(1, 2, 3, 4);
        r.Union(MRect.Empty);
        await Assert.That(r == new MRect(1, 2, 3, 4)).IsTrue();
    }

    [Test]
    public async Task Rect_Union_PropagatesInfiniteSize()
    {
        var u = MRect.Union(new MRect(0, 0, 1, 1), new MRect(0, 0, double.PositiveInfinity, double.PositiveInfinity));
        await Assert.That(double.IsPositiveInfinity(u.Width)).IsTrue();
        await Assert.That(double.IsPositiveInfinity(u.Height)).IsTrue();
    }

    [Test]
    [Skip("Known bug: Rect.Union on an Empty receiver yields (0,0,0,0) instead of the other rect, so accumulating bounds from Rect.Empty always loses the first rect.")]
    public async Task Rect_Union_EmptyReceiver_BecomesOtherRect()
    {
        var u = MRect.Union(MRect.Empty, new MRect(1, 2, 3, 4));
        await Assert.That(u == new MRect(1, 2, 3, 4)).IsTrue();
    }

    #endregion

    #region Rect - Contains / equality

    [Test]
    public async Task Rect_Contains_IsInclusive()
    {
        var r = new MRect(0, 0, 10, 10);
        await Assert.That(r.Contains(new MPoint(0, 0))).IsTrue();
        await Assert.That(r.Contains(10, 10)).IsTrue();
        await Assert.That(r.Contains(5, 5)).IsTrue();
        await Assert.That(r.Contains(10.5, 5)).IsFalse();
        await Assert.That(r.Contains(-0.5, 5)).IsFalse();

        await Assert.That(r.Contains(new MRect(0, 0, 10, 10))).IsTrue();
        await Assert.That(r.Contains(new MRect(2, 2, 3, 3))).IsTrue();
        await Assert.That(r.Contains(new MRect(8, 8, 3, 3))).IsFalse();
        await Assert.That(r.Contains(MRect.Empty)).IsFalse();
        await Assert.That(MRect.Empty.Contains(0, 0)).IsFalse();
    }

    [Test]
    public async Task Rect_Equality_OperatorUsesTolerance_EqualsIsExact()
    {
        var a = new MRect(0, 0, 1, 1);
        var b = new MRect(1e-14, 0, 1, 1);
        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(a.Equals((object)new MRect(0, 0, 1, 1))).IsTrue();
        await Assert.That(a.Equals((object)42)).IsFalse();
    }

    #endregion
}
