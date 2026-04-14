using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.EdgeLabels;
using Westermo.GraphX.Controls.Controls.VertexLabels;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for fixes #9 (XYChanged debounce), #10 (edge pointer measure with infinity),
/// #11 (cached RotateTransform in label controls), and #12 (MathHelper.RotatePoint precision).
/// </summary>
public class MediumLowFixTests
{
    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; } = null;
    }

    #region Fix #12: MathHelper.RotatePoint precision

    [Test]
    public async Task RotatePoint_90Degrees_PreservesSubPixelPrecision()
    {
        // Rotating (1,0) around origin by 90 degrees should yield approximately (0,1)
        var result = MathHelper.RotatePoint(
            new Westermo.GraphX.Measure.Point(1, 0),
            new Westermo.GraphX.Measure.Point(0, 0),
            90);

        // With (int) casts this would be (0,0), without them it's approximately (0,1)
        await Assert.That(Math.Abs(result.X)).IsLessThanOrEqualTo(1e-10);
        await Assert.That(Math.Abs(result.Y - 1.0)).IsLessThanOrEqualTo(1e-10);
    }

    [Test]
    public async Task RotatePoint_45Degrees_ReturnsNonIntegerResult()
    {
        // Rotating (1,0) around origin by 45 degrees should yield ~(0.707, 0.707)
        var result = MathHelper.RotatePoint(
            new Westermo.GraphX.Measure.Point(1, 0),
            new Westermo.GraphX.Measure.Point(0, 0),
            45);

        var expected = Math.Sqrt(2) / 2.0;
        await Assert.That(Math.Abs(result.X - expected)).IsLessThanOrEqualTo(1e-10);
        await Assert.That(Math.Abs(result.Y - expected)).IsLessThanOrEqualTo(1e-10);
    }

    #endregion

    #region Fix #11: Cached RotateTransform in EdgeLabelControl

    [Test]
    public async Task EdgeLabelControl_AngleChange_UsesCachedRotateTransform()
    {
        var label = new TestEdgeLabelControl();

        // Set angle twice - should reuse the same RotateTransform instance
        label.Angle = 45;
        var firstTransform = label.RenderTransform as RotateTransform;
        await Assert.That(firstTransform).IsNotNull();
        await Assert.That(firstTransform!.Angle).IsEqualTo(45);

        label.Angle = 90;
        var secondTransform = label.RenderTransform as RotateTransform;
        await Assert.That(secondTransform).IsNotNull();

        // Should be the same instance (cached), not a new allocation
        await Assert.That(ReferenceEquals(firstTransform, secondTransform)).IsTrue();
        await Assert.That(secondTransform!.Angle).IsEqualTo(90);
    }

    #endregion

    #region Fix #11: Cached RotateTransform in VertexLabelControl

    [Test]
    public async Task VertexLabelControl_AngleChange_UsesCachedRotateTransform()
    {
        var label = new VertexLabelControl();

        label.Angle = 30;
        var firstTransform = label.RenderTransform as RotateTransform;
        await Assert.That(firstTransform).IsNotNull();
        await Assert.That(firstTransform!.Angle).IsEqualTo(30);

        label.Angle = 60;
        var secondTransform = label.RenderTransform as RotateTransform;
        await Assert.That(secondTransform).IsNotNull();

        // Should be the same instance (cached)
        await Assert.That(ReferenceEquals(firstTransform, secondTransform)).IsTrue();
        await Assert.That(secondTransform!.Angle).IsEqualTo(60);
    }

    #endregion

    #region Fix #9: XYChanged debounce

    [Test]
    public async Task VertexControl_SetPosition_FiresPositionChangedOnce()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.SetPosition(10, 10);

        int positionChangedCount = 0;
        vc.PositionChanged += (_, _) => positionChangedCount++;

        // SetPosition sets X then Y, which would fire XYChanged twice without debouncing
        vc.SetPosition(100, 200);

        // With coalescing, position changed should fire only once (synchronously on the second prop change)
        await Assert.That(positionChangedCount).IsEqualTo(1);
    }

    #endregion

    /// <summary>
    /// Concrete EdgeLabelControl for testing purposes.
    /// </summary>
    private class TestEdgeLabelControl : EdgeLabelControl
    {
    }
}
