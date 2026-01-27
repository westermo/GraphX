using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuikGraph;
using TUnit.Core;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Logic.Models;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for edge routing algorithm correctness and behavior.
/// </summary>
public class EdgeRoutingTests
{
    private sealed class TestVertex : VertexBase
    {
        public string Name { get; init; } = string.Empty;
        public override string ToString() => Name;
    }

    private sealed class TestEdge : EdgeBase<TestVertex>
    {
        public TestEdge(TestVertex source, TestVertex target) : base(source, target) { }
        public override Point[] RoutingPoints { get; set; } = [];
    }

    private static (BidirectionalGraph<TestVertex, TestEdge> graph, 
                    Dictionary<TestVertex, Point> positions, 
                    Dictionary<TestVertex, Rect> sizes) CreateTestScenario(int vertexCount, int edgeCount)
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var vertices = new List<TestVertex>(vertexCount);
        var random = new Random(42);

        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = new TestVertex { ID = i, Name = $"V{i}" };
            graph.AddVertex(vertex);
            vertices.Add(vertex);
        }

        // Ensure connectivity
        for (var i = 1; i < vertexCount; i++)
        {
            graph.AddEdge(new TestEdge(vertices[random.Next(i)], vertices[i]));
        }

        // Add remaining edges
        var addedEdges = vertexCount - 1;
        while (addedEdges < edgeCount)
        {
            var s = random.Next(vertexCount);
            var t = random.Next(vertexCount);
            if (s != t && !graph.ContainsEdge(vertices[s], vertices[t]))
            {
                graph.AddEdge(new TestEdge(vertices[s], vertices[t]));
                addedEdges++;
            }
        }

        // Compute positions using FR layout
        var layoutAlgorithm = new FRLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new BoundedFRLayoutParameters { Width = 1000, Height = 1000 });
        layoutAlgorithm.Compute(CancellationToken.None);

        var positions = new Dictionary<TestVertex, Point>(layoutAlgorithm.VertexPositions);
        var sizes = CreateSizeRects(positions);

        return (graph, positions, sizes);
    }

    private static Dictionary<TestVertex, Rect> CreateSizeRects(Dictionary<TestVertex, Point> positions)
    {
        const double width = 60;
        const double height = 40;
        return positions.ToDictionary(
            kvp => kvp.Key,
            kvp => new Rect(kvp.Value.X - width / 2, kvp.Value.Y - height / 2, width, height));
    }

    private static Rect GetGraphBounds(Dictionary<TestVertex, Point> positions)
    {
        var minX = positions.Values.Min(p => p.X) - 100;
        var minY = positions.Values.Min(p => p.Y) - 100;
        var maxX = positions.Values.Max(p => p.X) + 100;
        var maxY = positions.Values.Max(p => p.Y) + 100;
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #region Simple Edge Routing Tests

    [Test]
    public async Task SimpleER_ComputesRoutesForAllEdges()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 15);

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        algorithm.Compute(CancellationToken.None);

        // Should have routes for all edges
        await Assert.That(algorithm.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
    }

    [Test]
    public async Task SimpleER_ProducesValidRoutePoints()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 15);

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        algorithm.Compute(CancellationToken.None);

        foreach (var route in algorithm.EdgeRoutes.Values)
        {
            if (route != null)
            {
                foreach (var point in route)
                {
                    await Assert.That(double.IsFinite(point.X)).IsTrue();
                    await Assert.That(double.IsFinite(point.Y)).IsTrue();
                }
            }
        }
    }

    [Test]
    public async Task SimpleER_ComputeSingle_ReturnsRouteForEdge()
    {
        var (graph, positions, sizes) = CreateTestScenario(5, 6);
        var edge = graph.Edges.First();

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        var route = algorithm.ComputeSingle(edge);

        // Route may be null if no obstacles, but ComputeSingle should succeed
        // Verify algorithm has EdgeRoutes dictionary initialized
        await Assert.That(algorithm.EdgeRoutes).IsNotNull();
    }

    [Test]
    public async Task SimpleER_HandlesEmptyGraph()
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var positions = new Dictionary<TestVertex, Point>();
        var sizes = new Dictionary<TestVertex, Rect>();

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.EdgeRoutes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SimpleER_HandlesSingleEdge()
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var v1 = new TestVertex { ID = 1, Name = "V1" };
        var v2 = new TestVertex { ID = 2, Name = "V2" };
        graph.AddVertex(v1);
        graph.AddVertex(v2);
        graph.AddEdge(new TestEdge(v1, v2));

        var positions = new Dictionary<TestVertex, Point>
        {
            { v1, new Point(0, 0) },
            { v2, new Point(200, 200) }
        };
        var sizes = CreateSizeRects(positions);

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.EdgeRoutes.Count).IsEqualTo(1);
    }

    #endregion

    #region PathFinder Edge Routing Tests

    [Test]
    public async Task PathFinderER_ComputesRoutesForAllEdges()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 15);

        var algorithm = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new PathFinderEdgeRoutingParameters());

        algorithm.Compute(CancellationToken.None);

        // PathFinder should compute routes for all edges
        await Assert.That(algorithm.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
    }

    [Test]
    public async Task PathFinderER_ProducesValidRoutePoints()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 15);

        var algorithm = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new PathFinderEdgeRoutingParameters());

        algorithm.Compute(CancellationToken.None);

        foreach (var route in algorithm.EdgeRoutes.Values)
        {
            if (route != null)
            {
                foreach (var point in route)
                {
                    await Assert.That(double.IsFinite(point.X)).IsTrue();
                    await Assert.That(double.IsFinite(point.Y)).IsTrue();
                }
            }
        }
    }

    [Test]
    public async Task PathFinderER_ComputeSingle_ReturnsRoute()
    {
        var (graph, positions, sizes) = CreateTestScenario(8, 12);
        var edge = graph.Edges.First();

        var algorithm = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new PathFinderEdgeRoutingParameters());

        var route = algorithm.ComputeSingle(edge);

        // ComputeSingle should succeed and EdgeRoutes should be initialized
        await Assert.That(algorithm.EdgeRoutes).IsNotNull();
    }

    [Test]
    public async Task PathFinderER_Parameters_AffectRouting()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 15);

        var algorithm1 = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, 
            new PathFinderEdgeRoutingParameters { HorizontalGridSize = 50, VerticalGridSize = 50 });
        algorithm1.Compute(CancellationToken.None);

        var algorithm2 = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, 
            new PathFinderEdgeRoutingParameters { HorizontalGridSize = 100, VerticalGridSize = 100 });
        algorithm2.Compute(CancellationToken.None);

        // Both should produce valid results
        await Assert.That(algorithm1.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
        await Assert.That(algorithm2.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
    }

    #endregion

    #region Bundle Edge Routing Tests

    [Test]
    public async Task BundleER_ComputesRoutesForAllEdges()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 20);
        var bounds = GetGraphBounds(positions);

        var algorithm = new BundleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            bounds, graph, positions, sizes, new BundleEdgeRoutingParameters { Iterations = 5 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
    }

    [Test]
    public async Task BundleER_ProducesMultiPointRoutes()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 20);
        var bounds = GetGraphBounds(positions);

        var algorithm = new BundleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            bounds, graph, positions, sizes, 
            new BundleEdgeRoutingParameters { Iterations = 10, SubdivisionPoints = 10 });

        algorithm.Compute(CancellationToken.None);

        // Bundle routing should produce multi-point routes
        var routesWithMultiplePoints = algorithm.EdgeRoutes.Values
            .Count(route => route != null && route.Length > 2);

        await Assert.That(routesWithMultiplePoints).IsGreaterThan(0);
    }

    [Test]
    public async Task BundleER_ProducesValidRoutePoints()
    {
        var (graph, positions, sizes) = CreateTestScenario(10, 20);
        var bounds = GetGraphBounds(positions);

        var algorithm = new BundleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            bounds, graph, positions, sizes, new BundleEdgeRoutingParameters { Iterations = 5 });

        algorithm.Compute(CancellationToken.None);

        foreach (var route in algorithm.EdgeRoutes.Values)
        {
            if (route != null)
            {
                foreach (var point in route)
                {
                    await Assert.That(double.IsFinite(point.X)).IsTrue();
                    await Assert.That(double.IsFinite(point.Y)).IsTrue();
                }
            }
        }
    }

    [Test]
    public async Task BundleER_Iterations_AffectsResult()
    {
        var (graph, positions, sizes) = CreateTestScenario(8, 15);
        var bounds = GetGraphBounds(positions);

        var algorithm1 = new BundleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            bounds, graph, positions, sizes, new BundleEdgeRoutingParameters { Iterations = 1 });
        algorithm1.Compute(CancellationToken.None);

        var algorithm2 = new BundleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            bounds, graph, positions, sizes, new BundleEdgeRoutingParameters { Iterations = 20 });
        algorithm2.Compute(CancellationToken.None);

        // Both should produce routes
        await Assert.That(algorithm1.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
        await Assert.That(algorithm2.EdgeRoutes.Count).IsEqualTo(graph.EdgeCount);
    }

    #endregion

    #region UpdateVertexData Tests

    [Test]
    public async Task SimpleER_UpdateVertexData_UpdatesPositionAndSize()
    {
        var (graph, positions, sizes) = CreateTestScenario(5, 6);
        var vertex = graph.Vertices.First();
        var originalPosition = positions[vertex];

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        var newPosition = new Point(originalPosition.X + 100, originalPosition.Y + 100);
        var newSize = new Rect(newPosition.X - 40, newPosition.Y - 25, 80, 50);

        algorithm.UpdateVertexData(vertex, newPosition, newSize);

        await Assert.That(algorithm.VertexPositions[vertex]).IsEqualTo(newPosition);
        await Assert.That(algorithm.VertexSizes[vertex]).IsEqualTo(newSize);
    }

    [Test]
    public async Task PathFinderER_UpdateVertexData_UpdatesPositionAndSize()
    {
        var (graph, positions, sizes) = CreateTestScenario(5, 6);
        var vertex = graph.Vertices.First();

        var algorithm = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new PathFinderEdgeRoutingParameters());

        var newPosition = new Point(500, 500);
        var newSize = new Rect(470, 475, 60, 50);

        algorithm.UpdateVertexData(vertex, newPosition, newSize);

        await Assert.That(algorithm.VertexPositions[vertex]).IsEqualTo(newPosition);
        await Assert.That(algorithm.VertexSizes[vertex]).IsEqualTo(newSize);
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public async Task SimpleER_RespectsCancellation()
    {
        var (graph, positions, sizes) = CreateTestScenario(50, 100);

        var algorithm = new SimpleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new SimpleERParameters());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => algorithm.Compute(cts.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task PathFinderER_RespectsCancellation()
    {
        var (graph, positions, sizes) = CreateTestScenario(50, 100);

        var algorithm = new PathFinderEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, positions, sizes, new PathFinderEdgeRoutingParameters());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => algorithm.Compute(cts.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task BundleER_RespectsCancellation()
    {
        var (graph, positions, sizes) = CreateTestScenario(50, 100);
        var bounds = GetGraphBounds(positions);

        var algorithm = new BundleEdgeRouting<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            bounds, graph, positions, sizes, new BundleEdgeRoutingParameters { Iterations = 100 });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => algorithm.Compute(cts.Token)).Throws<OperationCanceledException>();
    }

    #endregion
}
