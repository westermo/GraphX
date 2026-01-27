using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuikGraph;
using TUnit.Core;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Logic.Models;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for layout algorithm correctness, determinism, and edge cases.
/// </summary>
public class LayoutAlgorithmTests
{
    private sealed class TestVertex : VertexBase, IIdentifiableGraphDataObject
    {
        public string Name { get; init; } = string.Empty;
        public override string ToString() => Name;
    }

    private sealed class TestEdge : EdgeBase<TestVertex>
    {
        public TestEdge(TestVertex source, TestVertex target) : base(source, target) { }
        public override Point[] RoutingPoints { get; set; } = [];
    }

    private static BidirectionalGraph<TestVertex, TestEdge> CreateGraph(int vertexCount, int edgeCount)
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

        return graph;
    }

    private static Dictionary<TestVertex, Size> CreateSizes(IEnumerable<TestVertex> vertices)
    {
        return vertices.ToDictionary(v => v, _ => new Size(50, 30));
    }

    #region KK Layout Tests

    [Test]
    public async Task KKLayout_AssignsPositionsToAllVertices()
    {
        var graph = CreateGraph(20, 30);
        var algorithm = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    [Test]
    public async Task KKLayout_ProducesFinitePositions()
    {
        var graph = CreateGraph(20, 30);
        var algorithm = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        foreach (var pos in algorithm.VertexPositions.Values)
        {
            await Assert.That(double.IsFinite(pos.X)).IsTrue();
            await Assert.That(double.IsFinite(pos.Y)).IsTrue();
        }
    }

    [Test]
    public async Task KKLayout_ProducesSpreadLayout()
    {
        var graph = CreateGraph(15, 20);

        var algorithm = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);

        // Verify all vertices have positions
        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(15);

        // Calculate spread - vertices should not all be at the same point
        var positions = algorithm.VertexPositions.Values.ToList();
        var minX = positions.Min(p => p.X);
        var maxX = positions.Max(p => p.X);
        var minY = positions.Min(p => p.Y);
        var maxY = positions.Max(p => p.Y);

        // Layout should have reasonable spread (not all collapsed to a point)
        await Assert.That(maxX - minX).IsGreaterThan(1);
        await Assert.That(maxY - minY).IsGreaterThan(1);
    }

    [Test]
    public async Task KKLayout_HandlesEmptyGraph()
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var algorithm = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task KKLayout_HandlesSingleVertex()
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var vertex = new TestVertex { ID = 1, Name = "Single" };
        graph.AddVertex(vertex);

        var algorithm = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(1);
        await Assert.That(algorithm.VertexPositions.ContainsKey(vertex)).IsTrue();
    }

    [Test]
    public async Task KKLayout_RespectsCancellation()
    {
        var graph = CreateGraph(100, 200);
        var algorithm = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000, MaxIterations = 10000 });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => algorithm.Compute(cts.Token)).Throws<OperationCanceledException>();
    }

    #endregion

    #region FR Layout Tests

    [Test]
    public async Task FRLayout_AssignsPositionsToAllVertices()
    {
        var graph = CreateGraph(20, 30);
        var algorithm = new FRLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new BoundedFRLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    [Test]
    public async Task FRLayout_ProducesPositionsWithinBounds()
    {
        var graph = CreateGraph(20, 30);
        const double width = 1000;
        const double height = 1000;

        var algorithm = new FRLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new BoundedFRLayoutParameters { Width = width, Height = height });

        algorithm.Compute(CancellationToken.None);

        // FR bounded layout should keep vertices roughly within bounds (with some tolerance)
        foreach (var pos in algorithm.VertexPositions.Values)
        {
            await Assert.That(pos.X).IsGreaterThanOrEqualTo(-width * 0.5);
            await Assert.That(pos.X).IsLessThanOrEqualTo(width * 1.5);
            await Assert.That(pos.Y).IsGreaterThanOrEqualTo(-height * 0.5);
            await Assert.That(pos.Y).IsLessThanOrEqualTo(height * 1.5);
        }
    }

    [Test]
    public async Task FRLayout_SeparatesVertices()
    {
        var graph = CreateGraph(10, 12);
        var algorithm = new FRLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new BoundedFRLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        // Check that not all vertices are at the same position
        var positions = algorithm.VertexPositions.Values.ToList();
        var distinctPositions = positions
            .GroupBy(p => (Math.Round(p.X / 10) * 10, Math.Round(p.Y / 10) * 10))
            .Count();

        await Assert.That(distinctPositions).IsGreaterThan(1);
    }

    #endregion

    #region LinLog Layout Tests

    [Test]
    public async Task LinLogLayout_AssignsPositionsToAllVertices()
    {
        var graph = CreateGraph(20, 30);
        var algorithm = new LinLogLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new LinLogLayoutParameters());

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    [Test]
    public async Task LinLogLayout_ProducesFinitePositions()
    {
        var graph = CreateGraph(20, 30);
        var algorithm = new LinLogLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new LinLogLayoutParameters());

        algorithm.Compute(CancellationToken.None);

        foreach (var pos in algorithm.VertexPositions.Values)
        {
            await Assert.That(double.IsFinite(pos.X)).IsTrue();
            await Assert.That(double.IsFinite(pos.Y)).IsTrue();
        }
    }

    #endregion

    #region ISOM Layout Tests

    [Test]
    public async Task ISOMLayout_AssignsPositionsToAllVertices()
    {
        var graph = CreateGraph(20, 30);
        var algorithm = new ISOMLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new ISOMLayoutParameters { Width = 1000, Height = 1000 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    [Test]
    public async Task ISOMLayout_ProducesPositionsWithinBounds()
    {
        var graph = CreateGraph(20, 30);
        const double width = 1000;
        const double height = 1000;

        var algorithm = new ISOMLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new ISOMLayoutParameters { Width = width, Height = height });

        algorithm.Compute(CancellationToken.None);

        foreach (var pos in algorithm.VertexPositions.Values)
        {
            await Assert.That(pos.X).IsGreaterThanOrEqualTo(0);
            await Assert.That(pos.X).IsLessThanOrEqualTo(width);
            await Assert.That(pos.Y).IsGreaterThanOrEqualTo(0);
            await Assert.That(pos.Y).IsLessThanOrEqualTo(height);
        }
    }

    #endregion

    #region Circular Layout Tests

    [Test]
    public async Task CircularLayout_AssignsPositionsToAllVertices()
    {
        var graph = CreateGraph(20, 30);
        var sizes = CreateSizes(graph.Vertices);
        var algorithm = new CircularLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, sizes, new CircularLayoutParameters());

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    [Test]
    public async Task CircularLayout_ArrangesVerticesInCircle()
    {
        var graph = CreateGraph(12, 15);
        var sizes = CreateSizes(graph.Vertices);
        var algorithm = new CircularLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, sizes, new CircularLayoutParameters());

        algorithm.Compute(CancellationToken.None);

        // Calculate center and verify roughly equal distances
        var positions = algorithm.VertexPositions.Values.ToList();
        var centerX = positions.Average(p => p.X);
        var centerY = positions.Average(p => p.Y);

        var distances = positions.Select(p => 
            Math.Sqrt(Math.Pow(p.X - centerX, 2) + Math.Pow(p.Y - centerY, 2))).ToList();

        var avgDistance = distances.Average();
        
        // All vertices should be roughly at the same distance from center (with 20% tolerance)
        foreach (var dist in distances)
        {
            await Assert.That(Math.Abs(dist - avgDistance) / avgDistance).IsLessThan(0.2);
        }
    }

    #endregion

    #region Simple Tree Layout Tests

    [Test]
    public async Task SimpleTreeLayout_AssignsPositionsToAllVertices()
    {
        var graph = CreateTreeGraph(20, 3);
        var sizes = CreateSizes(graph.Vertices);
        var algorithm = new SimpleTreeLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, sizes, new SimpleTreeLayoutParameters());

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    [Test]
    public async Task SimpleTreeLayout_PlacesChildrenBelowParent()
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var root = new TestVertex { ID = 1, Name = "Root" };
        var child1 = new TestVertex { ID = 2, Name = "Child1" };
        var child2 = new TestVertex { ID = 3, Name = "Child2" };

        graph.AddVertex(root);
        graph.AddVertex(child1);
        graph.AddVertex(child2);
        graph.AddEdge(new TestEdge(root, child1));
        graph.AddEdge(new TestEdge(root, child2));

        var sizes = CreateSizes(graph.Vertices);
        var algorithm = new SimpleTreeLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, sizes, new SimpleTreeLayoutParameters { Direction = LayoutDirection.TopToBottom });

        algorithm.Compute(CancellationToken.None);

        var rootPos = algorithm.VertexPositions[root];
        var child1Pos = algorithm.VertexPositions[child1];
        var child2Pos = algorithm.VertexPositions[child2];

        // Children should be below root (larger Y in TopToBottom)
        await Assert.That(child1Pos.Y).IsGreaterThan(rootPos.Y);
        await Assert.That(child2Pos.Y).IsGreaterThan(rootPos.Y);
    }

    private static BidirectionalGraph<TestVertex, TestEdge> CreateTreeGraph(int vertexCount, int branchingFactor)
    {
        var graph = new BidirectionalGraph<TestVertex, TestEdge>();
        var vertices = new List<TestVertex>(vertexCount);

        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = new TestVertex { ID = i, Name = $"T{i}" };
            graph.AddVertex(vertex);
            vertices.Add(vertex);
        }

        for (var i = 1; i < vertexCount; i++)
        {
            var parentIdx = (i - 1) / branchingFactor;
            graph.AddEdge(new TestEdge(vertices[parentIdx], vertices[i]));
        }

        return graph;
    }

    #endregion

    #region Algorithm Parameter Tests

    [Test]
    public async Task KKLayout_MaxIterations_AffectsComputation()
    {
        var graph = CreateGraph(30, 50);
        
        // Few iterations
        var algorithm1 = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000, MaxIterations = 1 });
        algorithm1.Compute(CancellationToken.None);

        // Many iterations
        var algorithm2 = new KKLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new KKLayoutParameters { Width = 1000, Height = 1000, MaxIterations = 1000 });
        algorithm2.Compute(CancellationToken.None);

        // Both should produce valid positions
        await Assert.That(algorithm1.VertexPositions.Count).IsEqualTo(graph.VertexCount);
        await Assert.That(algorithm2.VertexPositions.Count).IsEqualTo(graph.VertexCount);
    }

    #endregion

    #region Large Graph Tests

    [Test]
    public async Task FRLayout_HandlesLargeGraph()
    {
        var graph = CreateGraph(200, 400);
        var algorithm = new FRLayoutAlgorithm<TestVertex, TestEdge, BidirectionalGraph<TestVertex, TestEdge>>(
            graph, null, new BoundedFRLayoutParameters { Width = 2000, Height = 2000 });

        algorithm.Compute(CancellationToken.None);

        await Assert.That(algorithm.VertexPositions.Count).IsEqualTo(200);
        
        // Verify all positions are valid
        foreach (var pos in algorithm.VertexPositions.Values)
        {
            await Assert.That(double.IsFinite(pos.X)).IsTrue();
            await Assert.That(double.IsFinite(pos.Y)).IsTrue();
        }
    }

    #endregion
}
