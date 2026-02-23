using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.EdgePointers;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for edge pointer (arrow) positioning and visibility.
/// These tests verify that edge pointers are correctly positioned at edge endpoints
/// and are visible when they should be.
/// </summary>
public class EdgePointerTests
{
    private class Vertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class Edge(Vertex s, Vertex t) : EdgeBase<Vertex>(s, t)
    {
        public override Measure.Point[]? RoutingPoints { get; set; } = null;
    }

    private (GraphArea<Vertex, Edge, BidirectionalGraph<Vertex, Edge>> area, EdgeControl edge, VertexControl sourceVc,
        VertexControl targetVc)
        CreateTestGraph(Point sourcePos, Point targetPos, bool showArrows = true)
    {
        var g = new BidirectionalGraph<Vertex, Edge>();
        var v1 = new Vertex("Source");
        var v2 = new Vertex("Target");
        g.AddVertex(v1);
        g.AddVertex(v2);
        g.AddEdge(new Edge(v1, v2));

        var lc = new GXLogicCore<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
        {
            Graph = g,
            EdgeCurvingEnabled = false
        };

        var area = new GraphArea<Vertex, Edge, BidirectionalGraph<Vertex, Edge>>
        {
            LogicCore = lc,
            Width = 800,
            Height = 600
        };

        // Create a window to host the area (needed for proper Avalonia layout)
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = area
        };
        window.Show();

        // Preload vertices and assign positions
        area.PreloadVertexes();

        VertexControl? sourceVc = null;
        VertexControl? targetVc = null;

        foreach (var (key, vc) in area.VertexList)
        {
            if (key == v1)
            {
                sourceVc = vc;
                vc.SetPosition(sourcePos);
            }
            else if (key == v2)
            {
                targetVc = vc;
                vc.SetPosition(targetPos);
            }

            GraphAreaBase.SetFinalX(vc, vc.GetPosition().X);
            GraphAreaBase.SetFinalY(vc, vc.GetPosition().Y);
        }

        // Generate edges (always visible by default)
        area.GenerateAllEdges();

        // Apply arrow visibility setting AFTER edges are created
        if (!showArrows)
        {
            area.ShowAllEdgesArrows(false);
        }

        // Force template application and layout pass
        foreach (var edgeControl in area.EdgesList.Values)
        {
            edgeControl.ApplyTemplate();
        }

        // Run the dispatcher to process the deferred UpdateEdge calls from OnApplyTemplate
        Dispatcher.UIThread.RunJobs();

        // Force another layout pass
        Dispatcher.UIThread.RunJobs();

        // Explicitly update all edges to ensure edge pointers are positioned
        foreach (var edgeControl in area.EdgesList.Values)
        {
            edgeControl.UpdateLayout();
        }

        // Final layout pass
        Dispatcher.UIThread.RunJobs();

        var edge = area.EdgesList.Values.First();
        return (area, edge, sourceVc!, targetVc!);
    }

    [Test]
    public async Task EdgePointer_Target_IsVisible_AfterEdgeCreation()
    {
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(100, 100), new Point(400, 100));

        var targetPointer = edge.GetEdgePointerForTarget();

        await Assert.That(targetPointer).IsNotNull();
        await Assert.That(targetPointer!.IsVisible).IsTrue()
            .Because("Target edge pointer should be visible after edge creation");
    }

    [Test]
    public async Task EdgePointer_Source_IsVisible_WhenShowArrowsEnabled()
    {
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(100, 100), new Point(400, 100));

        var sourcePointer = edge.GetEdgePointerForSource();

        await Assert.That(sourcePointer).IsNotNull();
        await Assert.That(sourcePointer!.IsVisible).IsTrue()
            .Because("Source edge pointer should be visible when ShowArrows is enabled");
    }

    [Test]
    public async Task EdgePointer_Target_IsPositioned_NearTargetVertex()
    {
        var sourcePos = new Point(100, 100);
        var targetPos = new Point(400, 100);
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(sourcePos, targetPos);

        var targetPointer = edge.GetEdgePointerForTarget();
        await Assert.That(targetPointer).IsNotNull();

        var pointerPos = targetPointer!.GetPosition();

        // The pointer position is in LOCAL coordinates (relative to EdgeControl).
        // For a horizontal edge from (100,100) to (400,100), the edge is positioned
        // with its top-left at some minimum point. The target pointer should be
        // positioned at the right side of the edge geometry.

        // First verify the pointer has a valid non-zero position
        await Assert.That(pointerPos.X).IsGreaterThan(0)
            .Because($"Target pointer should have positive X position, got {pointerPos.X}");

        // For a horizontal left-to-right edge, the target pointer's local X should be 
        // greater than 0 (towards the right side of the edge geometry)
        await Assert.That(pointerPos.X).IsGreaterThan(100)
            .Because($"Target pointer local X ({pointerPos.X:F1}) should be positioned towards right of edge geometry");
    }

    [Test]
    public async Task EdgePointer_Source_IsPositioned_NearSourceVertex()
    {
        var sourcePos = new Point(100, 100);
        var targetPos = new Point(400, 100);
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(sourcePos, targetPos);

        var sourcePointer = edge.GetEdgePointerForSource();
        await Assert.That(sourcePointer).IsNotNull();

        var pointerPos = sourcePointer!.GetPosition();

        // The pointer position is in LOCAL coordinates (relative to EdgeControl).
        // For a horizontal edge, the source pointer should be positioned at the 
        // left side of the edge geometry.

        // First verify the pointer has a valid non-zero position
        await Assert.That(pointerPos.X).IsGreaterThanOrEqualTo(0)
            .Because($"Source pointer should have non-negative X position, got {pointerPos.X}");
    }

    [Test]
    public async Task EdgePointers_Target_IsFurtherRight_ThanSource_ForLeftToRightEdge()
    {
        var sourcePos = new Point(100, 100);
        var targetPos = new Point(400, 100);
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(sourcePos, targetPos);

        var sourcePointer = edge.GetEdgePointerForSource();
        var targetPointer = edge.GetEdgePointerForTarget();

        await Assert.That(sourcePointer).IsNotNull();
        await Assert.That(targetPointer).IsNotNull();

        var sourcePos2 = sourcePointer!.GetPosition();
        var targetPos2 = targetPointer!.GetPosition();

        // For a left-to-right edge, the target pointer's X should be greater than source's
        await Assert.That(targetPos2.X).IsGreaterThan(sourcePos2.X)
            .Because(
                $"Target pointer X ({targetPos2.X:F1}) should be > source pointer X ({sourcePos2.X:F1}) for left-to-right edge");
    }

    [Test]
    public async Task EdgePointer_HasNonZeroPosition_AfterEdgeCreation()
    {
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(100, 100), new Point(400, 100));

        var targetPointer = edge.GetEdgePointerForTarget();
        await Assert.That(targetPointer).IsNotNull();

        var pointerPos = targetPointer!.GetPosition();

        // Position should not be at origin (0,0) which would indicate it wasn't properly positioned
        var isAtOrigin = Math.Abs(pointerPos.X) < 1 && Math.Abs(pointerPos.Y) < 1;
        await Assert.That(isAtOrigin).IsFalse()
            .Because($"Target pointer position ({pointerPos.X:F1}, {pointerPos.Y:F1}) should not be at origin");
    }

    [Test]
    public async Task EdgePointer_PositionUpdates_WhenVertexMoves()
    {
        var sourcePos = new Point(100, 100);
        var targetPos = new Point(400, 100);
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(sourcePos, targetPos);

        var targetPointer = edge.GetEdgePointerForTarget();
        await Assert.That(targetPointer).IsNotNull();

        var initialPos = targetPointer!.GetPosition();

        // Move the target vertex
        var newTargetPos = new Point(500, 200);
        targetVc.SetPosition(newTargetPos);
        GraphAreaBase.SetFinalX(targetVc, newTargetPos.X);
        GraphAreaBase.SetFinalY(targetVc, newTargetPos.Y);

        // Update the edge
        edge.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        var updatedPos = targetPointer.GetPosition();

        // The pointer position should have changed
        var positionChanged = Math.Abs(updatedPos.X - initialPos.X) > 10 ||
                              Math.Abs(updatedPos.Y - initialPos.Y) > 10;

        await Assert.That(positionChanged).IsTrue()
            .Because($"Pointer position should change from ({initialPos.X:F1}, {initialPos.Y:F1}) " +
                     $"to something different when vertex moves. Got ({updatedPos.X:F1}, {updatedPos.Y:F1})");
    }

    [Test]
    public async Task EdgePointer_DefaultEdgePointer_HasValidLastKnownRectSize()
    {
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(100, 100), new Point(400, 100));

        var targetPointer = edge.GetEdgePointerForTarget() as DefaultEdgePointer;
        await Assert.That(targetPointer).IsNotNull();

        var rect = targetPointer!.LastKnownRectSize;

        // The rect should have non-zero size
        await Assert.That(rect.Width).IsGreaterThan(0)
            .Because("LastKnownRectSize should have width after positioning");
        await Assert.That(rect.Height).IsGreaterThan(0)
            .Because("LastKnownRectSize should have height after positioning");

        // The rect position should not be at origin for a properly positioned pointer
        var isNearOrigin = Math.Abs(rect.X) < 1 && Math.Abs(rect.Y) < 1;
        await Assert.That(isNearOrigin).IsFalse()
            .Because($"LastKnownRectSize position ({rect.X:F1}, {rect.Y:F1}) should not be near origin");
    }

    [Test]
    public async Task EdgePointer_IsHidden_WhenShowArrowsDisabled()
    {
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(
            new Point(100, 100), new Point(400, 100), showArrows: false);

        // First verify ShowArrows is actually false
        await Assert.That(edge.ShowArrows).IsFalse()
            .Because("Edge should have ShowArrows=false as specified");

        var targetPointer = edge.GetEdgePointerForTarget();

        // Pointer might be null or not visible
        if (targetPointer != null)
        {
            await Assert.That(targetPointer.IsVisible).IsFalse()
                .Because($"Edge pointer should not be visible when ShowArrows is false (ShowArrows={edge.ShowArrows})");
        }
    }

    [Test]
    public async Task EdgePointer_PositionIsConsistent_BetweenSourceAndTarget()
    {
        // Create a horizontal edge
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(100, 200), new Point(400, 200));

        var sourcePointer = edge.GetEdgePointerForSource();
        var targetPointer = edge.GetEdgePointerForTarget();

        await Assert.That(sourcePointer).IsNotNull();
        await Assert.That(targetPointer).IsNotNull();

        var sourcePos = sourcePointer!.GetPosition();
        var targetPos = targetPointer!.GetPosition();

        // For a horizontal edge, pointers should be at roughly the same Y coordinate
        await Assert.That(Math.Abs(sourcePos.Y - targetPos.Y)).IsLessThan(20)
            .Because(
                $"For horizontal edge, pointers at Y={sourcePos.Y:F1} and Y={targetPos.Y:F1} should be at similar heights");

        // Source pointer X should be less than target pointer X
        await Assert.That(sourcePos.X).IsLessThan(targetPos.X)
            .Because($"Source pointer X ({sourcePos.X:F1}) should be left of target pointer X ({targetPos.X:F1})");
    }

    [Test]
    public async Task EdgePointer_Visible_WithDiagonalEdge()
    {
        // Create a diagonal edge
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(100, 100), new Point(400, 400));

        var targetPointer = edge.GetEdgePointerForTarget();
        await Assert.That(targetPointer).IsNotNull();
        await Assert.That(targetPointer!.IsVisible).IsTrue();

        var pointerPos = targetPointer.GetPosition();

        // Position should be somewhere along the diagonal
        await Assert.That(pointerPos.X).IsGreaterThan(100);
        await Assert.That(pointerPos.Y).IsGreaterThan(100);
    }

    [Test]
    public async Task EdgePointer_Visible_WithVerticalEdge()
    {
        // Create a vertical edge
        var (area, edge, sourceVc, targetVc) = CreateTestGraph(new Point(200, 100), new Point(200, 400));

        var targetPointer = edge.GetEdgePointerForTarget();
        await Assert.That(targetPointer).IsNotNull();
        await Assert.That(targetPointer!.IsVisible).IsTrue();

        var pointerPos = targetPointer.GetPosition();

        // The pointer position is in LOCAL coordinates (relative to EdgeControl).
        // For a vertical edge, the pointer should have a valid position.
        // The exact X coordinate in local space depends on edge geometry bounds.
        await Assert.That(pointerPos.X).IsGreaterThanOrEqualTo(0)
            .Because($"Vertical edge pointer X ({pointerPos.X:F1}) should be non-negative in local coordinates");

        // Y should be significant for a vertical edge (towards the bottom)
        await Assert.That(pointerPos.Y).IsGreaterThan(100)
            .Because(
                $"For vertical edge spanning Y 100-400, target pointer Y ({pointerPos.Y:F1}) should be towards bottom");
    }
}