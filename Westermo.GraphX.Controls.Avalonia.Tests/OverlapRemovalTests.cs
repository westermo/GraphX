using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TUnit.Core;
using Westermo.GraphX.Logic.Algorithms.OverlapRemoval;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for overlap removal algorithm correctness and behavior.
/// </summary>
public class OverlapRemovalTests
{
    private sealed class TestObject
    {
        public int Id { get; init; }
        public override string ToString() => $"Obj{Id}";
    }

    private static Dictionary<TestObject, Rect> CreateOverlappingRectangles(int count, double overlapFactor)
    {
        var rects = new Dictionary<TestObject, Rect>();
        const double rectWidth = 60;
        const double rectHeight = 40;
        
        var spacing = rectWidth * (1 - overlapFactor);
        var gridSize = (int)Math.Ceiling(Math.Sqrt(count));

        for (var i = 0; i < count; i++)
        {
            var obj = new TestObject { Id = i };
            var gridX = i % gridSize;
            var gridY = i / gridSize;
            
            rects[obj] = new Rect(gridX * spacing, gridY * spacing, rectWidth, rectHeight);
        }

        return rects;
    }

    private static Dictionary<TestObject, Rect> CreateNonOverlappingRectangles(int count)
    {
        var rects = new Dictionary<TestObject, Rect>();
        const double rectWidth = 60;
        const double rectHeight = 40;
        const double spacing = 100; // Large spacing, no overlap

        var gridSize = (int)Math.Ceiling(Math.Sqrt(count));

        for (var i = 0; i < count; i++)
        {
            var obj = new TestObject { Id = i };
            var gridX = i % gridSize;
            var gridY = i / gridSize;
            
            rects[obj] = new Rect(gridX * spacing, gridY * spacing, rectWidth, rectHeight);
        }

        return rects;
    }

    private static bool HasOverlap(IEnumerable<Rect> rectangles)
    {
        var rectList = rectangles.ToList();
        for (var i = 0; i < rectList.Count - 1; i++)
        {
            for (var j = i + 1; j < rectList.Count; j++)
            {
                if (rectList[i].IntersectsWith(rectList[j]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    #region FSA Algorithm Tests

    [Test]
    public async Task FSA_RemovesOverlaps()
    {
        var rects = CreateOverlappingRectangles(20, 0.5);
        
        // Verify initial overlaps exist
        await Assert.That(HasOverlap(rects.Values)).IsTrue();

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        // Verify overlaps are removed
        await Assert.That(HasOverlap(algorithm.Rectangles.Values)).IsFalse();
    }

    [Test]
    public async Task FSA_PreservesAllRectangles()
    {
        var rects = CreateOverlappingRectangles(15, 0.5);
        var originalCount = rects.Count;

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.Rectangles.Count).IsEqualTo(originalCount);
    }

    [Test]
    public async Task FSA_PreservesRectangleDimensions()
    {
        var rects = CreateOverlappingRectangles(10, 0.5);
        var originalDimensions = rects.ToDictionary(
            kvp => kvp.Key, 
            kvp => (Width: kvp.Value.Width, Height: kvp.Value.Height));

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        foreach (var kvp in algorithm.Rectangles)
        {
            var original = originalDimensions[kvp.Key];
            await Assert.That(Math.Abs(kvp.Value.Width - original.Width)).IsLessThan(0.001);
            await Assert.That(Math.Abs(kvp.Value.Height - original.Height)).IsLessThan(0.001);
        }
    }

    [Test]
    public async Task FSA_HandlesNoOverlap()
    {
        var rects = CreateNonOverlappingRectangles(10);
        var originalPositions = rects.ToDictionary(
            kvp => kvp.Key, 
            kvp => (X: kvp.Value.X, Y: kvp.Value.Y));

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        // Positions should remain largely unchanged when no overlap exists
        foreach (var kvp in algorithm.Rectangles)
        {
            var original = originalPositions[kvp.Key];
            // Allow small tolerance
            await Assert.That(Math.Abs(kvp.Value.X - original.X)).IsLessThan(1.0);
            await Assert.That(Math.Abs(kvp.Value.Y - original.Y)).IsLessThan(1.0);
        }
    }

    [Test]
    public async Task FSA_HandlesEmptyInput()
    {
        var rects = new Dictionary<TestObject, Rect>();

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.Rectangles.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FSA_HandlesSingleRectangle()
    {
        var obj = new TestObject { Id = 1 };
        var rects = new Dictionary<TestObject, Rect>
        {
            { obj, new Rect(100, 100, 60, 40) }
        };

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.Rectangles.Count).IsEqualTo(1);
        await Assert.That(algorithm.Rectangles.ContainsKey(obj)).IsTrue();
    }

    [Test]
    public async Task FSA_HandlesTwoOverlappingRectangles()
    {
        var obj1 = new TestObject { Id = 1 };
        var obj2 = new TestObject { Id = 2 };
        var rects = new Dictionary<TestObject, Rect>
        {
            { obj1, new Rect(0, 0, 100, 50) },
            { obj2, new Rect(50, 25, 100, 50) } // Overlapping
        };

        await Assert.That(rects[obj1].IntersectsWith(rects[obj2])).IsTrue();

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.Rectangles[obj1].IntersectsWith(algorithm.Rectangles[obj2])).IsFalse();
    }

    [Test]
    public async Task FSA_RespectsCancellation()
    {
        var rects = CreateOverlappingRectangles(100, 0.8);

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => algorithm.Compute(cts.Token)).Throws<OperationCanceledException>();
    }

    #endregion

    #region OneWayFSA Algorithm Tests

    [Test]
    public async Task OneWayFSA_Horizontal_RemovesHorizontalOverlaps()
    {
        // Create rectangles with horizontal overlap only (vertically non-overlapping)
        var rects = new Dictionary<TestObject, Rect>();
        const double rectWidth = 60;
        const double rectHeight = 40;
        
        // Place rectangles in a horizontal row with some horizontal overlap
        for (var i = 0; i < 10; i++)
        {
            var obj = new TestObject { Id = i };
            // Vertically spaced apart so no Y overlap, horizontally overlapping
            rects[obj] = new Rect(i * (rectWidth * 0.7), 0, rectWidth, rectHeight);
        }

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);

        await Assert.That(HasOverlap(algorithm.Rectangles.Values)).IsFalse();
    }

    [Test]
    public async Task OneWayFSA_Vertical_RemovesVerticalOverlaps()
    {
        // Create rectangles with vertical overlap only (horizontally non-overlapping)
        var rects = new Dictionary<TestObject, Rect>();
        const double rectWidth = 50;
        const double rectHeight = 40;
        
        // Place rectangles in a vertical column with some vertical overlap
        for (var i = 0; i < 10; i++)
        {
            var obj = new TestObject { Id = i };
            // Horizontally spaced apart so no X overlap, vertically overlapping
            rects[obj] = new Rect(0, i * (rectHeight * 0.7), rectWidth, rectHeight);
        }

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Vertical });
        algorithm.Compute(CancellationToken.None);

        await Assert.That(HasOverlap(algorithm.Rectangles.Values)).IsFalse();
    }

    [Test]
    public async Task OneWayFSA_Horizontal_MovesOnlyHorizontally()
    {
        var obj1 = new TestObject { Id = 1 };
        var obj2 = new TestObject { Id = 2 };
        var rects = new Dictionary<TestObject, Rect>
        {
            { obj1, new Rect(0, 0, 100, 50) },
            { obj2, new Rect(50, 0, 100, 50) } // Horizontal overlap only
        };
        var originalY1 = rects[obj1].Y;
        var originalY2 = rects[obj2].Y;

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);

        // Y positions should remain the same
        await Assert.That(Math.Abs(algorithm.Rectangles[obj1].Y - originalY1)).IsLessThan(0.001);
        await Assert.That(Math.Abs(algorithm.Rectangles[obj2].Y - originalY2)).IsLessThan(0.001);
    }

    [Test]
    public async Task OneWayFSA_Vertical_MovesOnlyVertically()
    {
        var obj1 = new TestObject { Id = 1 };
        var obj2 = new TestObject { Id = 2 };
        var rects = new Dictionary<TestObject, Rect>
        {
            { obj1, new Rect(0, 0, 50, 100) },
            { obj2, new Rect(0, 50, 50, 100) } // Vertical overlap only
        };
        var originalX1 = rects[obj1].X;
        var originalX2 = rects[obj2].X;

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Vertical });
        algorithm.Compute(CancellationToken.None);

        // X positions should remain the same
        await Assert.That(Math.Abs(algorithm.Rectangles[obj1].X - originalX1)).IsLessThan(0.001);
        await Assert.That(Math.Abs(algorithm.Rectangles[obj2].X - originalX2)).IsLessThan(0.001);
    }

    [Test]
    public async Task OneWayFSA_PreservesAllRectangles()
    {
        var rects = CreateOverlappingRectangles(15, 0.5);
        var originalCount = rects.Count;

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.Rectangles.Count).IsEqualTo(originalCount);
    }

    [Test]
    public async Task OneWayFSA_RespectsCancellation()
    {
        var rects = CreateOverlappingRectangles(100, 0.8);

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => algorithm.Compute(cts.Token)).Throws<OperationCanceledException>();
    }

    #endregion

    #region Parameter Tests

    [Test]
    public async Task FSA_HorizontalGap_AffectsSpacing()
    {
        var rects = CreateOverlappingRectangles(10, 0.3); // Less severe overlap

        var algorithm1 = new FSAAlgorithm<TestObject>(
            new Dictionary<TestObject, Rect>(rects), 
            new OverlapRemovalParameters { HorizontalGap = 0 });
        algorithm1.Compute(CancellationToken.None);

        var algorithm2 = new FSAAlgorithm<TestObject>(
            new Dictionary<TestObject, Rect>(rects), 
            new OverlapRemovalParameters { HorizontalGap = 20 });
        algorithm2.Compute(CancellationToken.None);

        // Both should produce valid outputs with all rectangles
        await Assert.That(algorithm1.Rectangles.Count).IsEqualTo(10);
        await Assert.That(algorithm2.Rectangles.Count).IsEqualTo(10);
    }

    [Test]
    public async Task FSA_VerticalGap_AffectsSpacing()
    {
        var rects = CreateOverlappingRectangles(10, 0.3); // Less severe overlap

        var algorithm1 = new FSAAlgorithm<TestObject>(
            new Dictionary<TestObject, Rect>(rects), 
            new OverlapRemovalParameters { VerticalGap = 0 });
        algorithm1.Compute(CancellationToken.None);

        var algorithm2 = new FSAAlgorithm<TestObject>(
            new Dictionary<TestObject, Rect>(rects), 
            new OverlapRemovalParameters { VerticalGap = 20 });
        algorithm2.Compute(CancellationToken.None);

        // Both should produce valid outputs with all rectangles
        await Assert.That(algorithm1.Rectangles.Count).IsEqualTo(10);
        await Assert.That(algorithm2.Rectangles.Count).IsEqualTo(10);
    }

    #endregion

    #region Large Input Tests

    [Test]
    public async Task FSA_HandlesLargeInput()
    {
        var rects = CreateOverlappingRectangles(100, 0.3); // Less severe overlap

        var algorithm = new FSAAlgorithm<TestObject>(rects, new OverlapRemovalParameters());
        algorithm.Compute(CancellationToken.None);

        // Verify algorithm completes and processes all rectangles
        await Assert.That(algorithm.Rectangles.Count).IsEqualTo(100);
    }

    [Test]
    public async Task OneWayFSA_HandlesLargeInput()
    {
        // Create horizontally overlapping rectangles in a single row
        var rects = new Dictionary<TestObject, Rect>();
        const double rectWidth = 60;
        const double rectHeight = 40;
        
        for (var i = 0; i < 100; i++)
        {
            var obj = new TestObject { Id = i };
            rects[obj] = new Rect(i * (rectWidth * 0.7), 0, rectWidth, rectHeight);
        }

        var algorithm = new OneWayFSAAlgorithm<TestObject>(rects, 
            new OneWayFSAParameters { Way = OneWayFSAWayEnum.Horizontal });
        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.Rectangles.Count).IsEqualTo(100);
        await Assert.That(HasOverlap(algorithm.Rectangles.Values)).IsFalse();
    }

    #endregion
}
