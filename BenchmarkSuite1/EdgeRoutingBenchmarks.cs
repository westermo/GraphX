using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Logic.Models;
using Westermo.GraphX.Measure;

namespace GraphXBenchmarks;

/// <summary>
/// Benchmarks for edge routing algorithms to measure performance with different graph densities
/// and identify optimization opportunities.
/// </summary>
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class EdgeRoutingBenchmarks
{
    public sealed class BenchVertex : VertexBase
    {
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public sealed class BenchEdge : EdgeBase<BenchVertex>
    {
        public BenchEdge(BenchVertex source, BenchVertex target) : base(source, target) { }
        public override Point[] RoutingPoints { get; set; } = [];
    }

    // Graphs of different sizes and densities
    private BidirectionalGraph<BenchVertex, BenchEdge> _smallSparseGraph = null!;   // 20 nodes, sparse
    private BidirectionalGraph<BenchVertex, BenchEdge> _smallDenseGraph = null!;    // 20 nodes, dense
    private BidirectionalGraph<BenchVertex, BenchEdge> _mediumGraph = null!;        // 50 nodes
    private BidirectionalGraph<BenchVertex, BenchEdge> _largeGraph = null!;         // 100 nodes

    // Pre-computed positions from layout
    private Dictionary<BenchVertex, Point> _smallSparsePositions = null!;
    private Dictionary<BenchVertex, Point> _smallDensePositions = null!;
    private Dictionary<BenchVertex, Point> _mediumPositions = null!;
    private Dictionary<BenchVertex, Point> _largePositions = null!;

    // Vertex sizes for routing calculations
    private Dictionary<BenchVertex, Rect> _smallSparseSizes = null!;
    private Dictionary<BenchVertex, Rect> _smallDenseSizes = null!;
    private Dictionary<BenchVertex, Rect> _mediumSizes = null!;
    private Dictionary<BenchVertex, Rect> _largeSizes = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create graphs
        _smallSparseGraph = CreateGraph(20, 25);   // Sparse: ~1.25 edges per vertex
        _smallDenseGraph = CreateGraph(20, 60);    // Dense: 3 edges per vertex
        _mediumGraph = CreateGraph(50, 100);
        _largeGraph = CreateGraph(100, 200);

        // Compute layouts for each graph
        _smallSparsePositions = ComputeLayout(_smallSparseGraph);
        _smallDensePositions = ComputeLayout(_smallDenseGraph);
        _mediumPositions = ComputeLayout(_mediumGraph);
        _largePositions = ComputeLayout(_largeGraph);

        // Create vertex size rectangles
        _smallSparseSizes = CreateSizeRects(_smallSparsePositions);
        _smallDenseSizes = CreateSizeRects(_smallDensePositions);
        _mediumSizes = CreateSizeRects(_mediumPositions);
        _largeSizes = CreateSizeRects(_largePositions);
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateGraph(int vertexCount, int edgeCount)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>(vertexCount);
        var random = new Random(42); // Fixed seed for reproducibility

        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = new BenchVertex { ID = i, Name = $"V{i}" };
            graph.AddVertex(vertex);
            vertices.Add(vertex);
        }

        // Ensure connectivity first
        for (var i = 1; i < vertexCount; i++)
        {
            var target = vertices[i];
            var source = vertices[random.Next(i)];
            graph.AddEdge(new BenchEdge(source, target));
        }

        // Add remaining edges
        var addedEdges = vertexCount - 1;
        while (addedEdges < edgeCount)
        {
            var sourceIdx = random.Next(vertexCount);
            var targetIdx = random.Next(vertexCount);
            if (sourceIdx != targetIdx)
            {
                var source = vertices[sourceIdx];
                var target = vertices[targetIdx];
                if (!graph.ContainsEdge(source, target))
                {
                    graph.AddEdge(new BenchEdge(source, target));
                    addedEdges++;
                }
            }
        }

        return graph;
    }

    private static Dictionary<BenchVertex, Point> ComputeLayout(BidirectionalGraph<BenchVertex, BenchEdge> graph)
    {
        // Use FR layout to get positions
        var algorithm = new FRLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            graph, null, new BoundedFRLayoutParameters 
            { 
                Width = graph.VertexCount * 100, 
                Height = graph.VertexCount * 100 
            });
        algorithm.Compute(CancellationToken.None);

        return new Dictionary<BenchVertex, Point>(algorithm.VertexPositions);
    }

    private static Dictionary<BenchVertex, Rect> CreateSizeRects(Dictionary<BenchVertex, Point> positions)
    {
        var sizes = new Dictionary<BenchVertex, Rect>();
        const double width = 60;
        const double height = 40;

        foreach (var kvp in positions)
        {
            var pos = kvp.Value;
            sizes[kvp.Key] = new Rect(pos.X - width / 2, pos.Y - height / 2, width, height);
        }

        return sizes;
    }

    private static Rect GetGraphBounds(Dictionary<BenchVertex, Point> positions)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var pos in positions.Values)
        {
            minX = Math.Min(minX, pos.X);
            minY = Math.Min(minY, pos.Y);
            maxX = Math.Max(maxX, pos.X);
            maxY = Math.Max(maxY, pos.Y);
        }

        return new Rect(minX - 100, minY - 100, maxX - minX + 200, maxY - minY + 200);
    }

    #region Simple Edge Routing Benchmarks

    [Benchmark(Description = "SimpleER - 20 sparse edges")]
    public IDictionary<BenchEdge, Point[]> SimpleER_SmallSparse()
    {
        var algorithm = new SimpleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallSparseGraph, _smallSparsePositions, _smallSparseSizes, 
            new SimpleERParameters { SideStep = 10, BackStep = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "SimpleER - 20 dense edges")]
    public IDictionary<BenchEdge, Point[]> SimpleER_SmallDense()
    {
        var algorithm = new SimpleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallDenseGraph, _smallDensePositions, _smallDenseSizes,
            new SimpleERParameters { SideStep = 10, BackStep = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "SimpleER - 50 nodes")]
    public IDictionary<BenchEdge, Point[]> SimpleER_Medium()
    {
        var algorithm = new SimpleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, _mediumPositions, _mediumSizes,
            new SimpleERParameters { SideStep = 10, BackStep = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "SimpleER - 100 nodes")]
    public IDictionary<BenchEdge, Point[]> SimpleER_Large()
    {
        var algorithm = new SimpleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _largeGraph, _largePositions, _largeSizes,
            new SimpleERParameters { SideStep = 10, BackStep = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    #endregion

    #region PathFinder Edge Routing Benchmarks

    [Benchmark(Description = "PathFinderER - 20 sparse edges")]
    public IDictionary<BenchEdge, Point[]> PathFinderER_SmallSparse()
    {
        var algorithm = new PathFinderEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallSparseGraph, _smallSparsePositions, _smallSparseSizes,
            new PathFinderEdgeRoutingParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "PathFinderER - 20 dense edges")]
    public IDictionary<BenchEdge, Point[]> PathFinderER_SmallDense()
    {
        var algorithm = new PathFinderEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallDenseGraph, _smallDensePositions, _smallDenseSizes,
            new PathFinderEdgeRoutingParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "PathFinderER - 50 nodes")]
    public IDictionary<BenchEdge, Point[]> PathFinderER_Medium()
    {
        var algorithm = new PathFinderEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, _mediumPositions, _mediumSizes,
            new PathFinderEdgeRoutingParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    #endregion

    #region Bundle Edge Routing Benchmarks

    [Benchmark(Description = "BundleER - 20 sparse edges")]
    public IDictionary<BenchEdge, Point[]> BundleER_SmallSparse()
    {
        var bounds = GetGraphBounds(_smallSparsePositions);
        var algorithm = new BundleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            bounds, _smallSparseGraph, _smallSparsePositions, _smallSparseSizes,
            new BundleEdgeRoutingParameters { Iterations = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "BundleER - 20 dense edges")]
    public IDictionary<BenchEdge, Point[]> BundleER_SmallDense()
    {
        var bounds = GetGraphBounds(_smallDensePositions);
        var algorithm = new BundleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            bounds, _smallDenseGraph, _smallDensePositions, _smallDenseSizes,
            new BundleEdgeRoutingParameters { Iterations = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "BundleER - 50 nodes")]
    public IDictionary<BenchEdge, Point[]> BundleER_Medium()
    {
        var bounds = GetGraphBounds(_mediumPositions);
        var algorithm = new BundleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            bounds, _mediumGraph, _mediumPositions, _mediumSizes,
            new BundleEdgeRoutingParameters { Iterations = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    #endregion

    #region Comparative Benchmarks

    /// <summary>
    /// Compare routing algorithms on the same graph
    /// </summary>
    [Benchmark(Baseline = true, Description = "SimpleER (Baseline) - 50 nodes")]
    public IDictionary<BenchEdge, Point[]> Baseline_SimpleER()
    {
        var algorithm = new SimpleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, _mediumPositions, _mediumSizes,
            new SimpleERParameters { SideStep = 10, BackStep = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "PathFinder vs Simple - 50 nodes")]
    public IDictionary<BenchEdge, Point[]> Compare_PathFinder()
    {
        var algorithm = new PathFinderEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, _mediumPositions, _mediumSizes,
            new PathFinderEdgeRoutingParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    [Benchmark(Description = "Bundle vs Simple - 50 nodes")]
    public IDictionary<BenchEdge, Point[]> Compare_Bundle()
    {
        var bounds = GetGraphBounds(_mediumPositions);
        var algorithm = new BundleEdgeRouting<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            bounds, _mediumGraph, _mediumPositions, _mediumSizes,
            new BundleEdgeRoutingParameters { Iterations = 10 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.EdgeRoutes;
    }

    #endregion
}
