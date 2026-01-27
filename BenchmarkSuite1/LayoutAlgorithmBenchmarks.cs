using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;
using QuikGraph;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Logic.Models;
using Westermo.GraphX.Measure;

namespace GraphXBenchmarks;

/// <summary>
/// Benchmarks for various layout algorithms to identify performance characteristics
/// and optimization opportunities with different graph sizes.
/// </summary>
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class LayoutAlgorithmBenchmarks
{
    public sealed class BenchVertex : VertexBase, IIdentifiableGraphDataObject
    {
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public sealed class BenchEdge : EdgeBase<BenchVertex>
    {
        public BenchEdge(BenchVertex source, BenchVertex target) : base(source, target) { }
        public override Point[] RoutingPoints { get; set; } = [];
    }

    // Graphs with different sizes for benchmarking scalability
    private BidirectionalGraph<BenchVertex, BenchEdge> _tinyGraph = null!;    // 10 nodes
    private BidirectionalGraph<BenchVertex, BenchEdge> _smallGraph = null!;   // 50 nodes
    private BidirectionalGraph<BenchVertex, BenchEdge> _mediumGraph = null!;  // 200 nodes
    private BidirectionalGraph<BenchVertex, BenchEdge> _largeGraph = null!;   // 500 nodes

    private Dictionary<BenchVertex, Size> _tinySizes = null!;
    private Dictionary<BenchVertex, Size> _smallSizes = null!;
    private Dictionary<BenchVertex, Size> _mediumSizes = null!;
    private Dictionary<BenchVertex, Size> _largeSizes = null!;

    // Tree-structured graphs for tree layout algorithms
    private BidirectionalGraph<BenchVertex, BenchEdge> _treeGraph = null!;
    private Dictionary<BenchVertex, Size> _treeSizes = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create graphs of various sizes
        _tinyGraph = CreateGraph(10, 15);
        _smallGraph = CreateGraph(50, 80);
        _mediumGraph = CreateGraph(200, 350);
        _largeGraph = CreateGraph(500, 900);

        // Create vertex sizes
        _tinySizes = CreateSizes(_tinyGraph.Vertices);
        _smallSizes = CreateSizes(_smallGraph.Vertices);
        _mediumSizes = CreateSizes(_mediumGraph.Vertices);
        _largeSizes = CreateSizes(_largeGraph.Vertices);

        // Create tree-structured graph for tree layouts
        _treeGraph = CreateTreeGraph(100, 3);
        _treeSizes = CreateSizes(_treeGraph.Vertices);
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

        // Ensure connectivity: create a spanning tree first
        for (var i = 1; i < vertexCount; i++)
        {
            var target = vertices[i];
            var source = vertices[random.Next(i)];
            graph.AddEdge(new BenchEdge(source, target));
        }

        // Add remaining random edges
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

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateTreeGraph(int vertexCount, int branchingFactor)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>(vertexCount);

        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = new BenchVertex { ID = i, Name = $"T{i}" };
            graph.AddVertex(vertex);
            vertices.Add(vertex);
        }

        // Create tree structure
        for (var i = 1; i < vertexCount; i++)
        {
            var parentIdx = (i - 1) / branchingFactor;
            graph.AddEdge(new BenchEdge(vertices[parentIdx], vertices[i]));
        }

        return graph;
    }

    private static Dictionary<BenchVertex, Size> CreateSizes(IEnumerable<BenchVertex> vertices)
    {
        var sizes = new Dictionary<BenchVertex, Size>();
        foreach (var vertex in vertices)
        {
            sizes[vertex] = new Size(50, 30);
        }
        return sizes;
    }

    #region Kamada-Kawai (KK) Layout Benchmarks

    [Benchmark(Description = "KK Layout - 10 nodes")]
    public IDictionary<BenchVertex, Point> KKLayout_Tiny()
    {
        var algorithm = new KKLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _tinyGraph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "KK Layout - 50 nodes")]
    public IDictionary<BenchVertex, Point> KKLayout_Small()
    {
        var algorithm = new KKLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "KK Layout - 200 nodes")]
    public IDictionary<BenchVertex, Point> KKLayout_Medium()
    {
        var algorithm = new KKLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, null, new KKLayoutParameters { Width = 2000, Height = 2000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion

    #region Fruchterman-Reingold (FR) Layout Benchmarks

    [Benchmark(Description = "FR Layout - 10 nodes")]
    public IDictionary<BenchVertex, Point> FRLayout_Tiny()
    {
        var algorithm = new FRLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _tinyGraph, null, new BoundedFRLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "FR Layout - 50 nodes")]
    public IDictionary<BenchVertex, Point> FRLayout_Small()
    {
        var algorithm = new FRLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new BoundedFRLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "FR Layout - 200 nodes")]
    public IDictionary<BenchVertex, Point> FRLayout_Medium()
    {
        var algorithm = new FRLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, null, new BoundedFRLayoutParameters { Width = 2000, Height = 2000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "FR Layout - 500 nodes")]
    public IDictionary<BenchVertex, Point> FRLayout_Large()
    {
        var algorithm = new FRLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _largeGraph, null, new BoundedFRLayoutParameters { Width = 3000, Height = 3000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion

    #region LinLog Layout Benchmarks

    [Benchmark(Description = "LinLog Layout - 10 nodes")]
    public IDictionary<BenchVertex, Point> LinLogLayout_Tiny()
    {
        var algorithm = new LinLogLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _tinyGraph, null, new LinLogLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "LinLog Layout - 50 nodes")]
    public IDictionary<BenchVertex, Point> LinLogLayout_Small()
    {
        var algorithm = new LinLogLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new LinLogLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "LinLog Layout - 200 nodes")]
    public IDictionary<BenchVertex, Point> LinLogLayout_Medium()
    {
        var algorithm = new LinLogLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, null, new LinLogLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion

    #region ISOM Layout Benchmarks

    [Benchmark(Description = "ISOM Layout - 10 nodes")]
    public IDictionary<BenchVertex, Point> ISOMLayout_Tiny()
    {
        var algorithm = new ISOMLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _tinyGraph, null, new ISOMLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "ISOM Layout - 50 nodes")]
    public IDictionary<BenchVertex, Point> ISOMLayout_Small()
    {
        var algorithm = new ISOMLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new ISOMLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "ISOM Layout - 200 nodes")]
    public IDictionary<BenchVertex, Point> ISOMLayout_Medium()
    {
        var algorithm = new ISOMLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, null, new ISOMLayoutParameters { Width = 2000, Height = 2000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion

    #region Circular Layout Benchmarks

    [Benchmark(Description = "Circular Layout - 50 nodes")]
    public IDictionary<BenchVertex, Point> CircularLayout_Small()
    {
        var algorithm = new CircularLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, _smallSizes, new CircularLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "Circular Layout - 200 nodes")]
    public IDictionary<BenchVertex, Point> CircularLayout_Medium()
    {
        var algorithm = new CircularLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _mediumGraph, null, _mediumSizes, new CircularLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "Circular Layout - 500 nodes")]
    public IDictionary<BenchVertex, Point> CircularLayout_Large()
    {
        var algorithm = new CircularLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _largeGraph, null, _largeSizes, new CircularLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion

    #region Simple Tree Layout Benchmarks

    [Benchmark(Description = "SimpleTree Layout - 100 nodes")]
    public IDictionary<BenchVertex, Point> SimpleTreeLayout()
    {
        var algorithm = new SimpleTreeLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _treeGraph, null, _treeSizes, new SimpleTreeLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion

    #region Comparative Benchmarks - Same Graph Size

    /// <summary>
    /// Compare all force-directed algorithms on the same medium-sized graph
    /// </summary>
    [Benchmark(Baseline = true, Description = "FR (Baseline) - 50 nodes")]
    public IDictionary<BenchVertex, Point> Baseline_FR_Small()
    {
        var algorithm = new FRLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new BoundedFRLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "KK vs FR - 50 nodes")]
    public IDictionary<BenchVertex, Point> Compare_KK_Small()
    {
        var algorithm = new KKLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new KKLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "LinLog vs FR - 50 nodes")]
    public IDictionary<BenchVertex, Point> Compare_LinLog_Small()
    {
        var algorithm = new LinLogLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new LinLogLayoutParameters());
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    [Benchmark(Description = "ISOM vs FR - 50 nodes")]
    public IDictionary<BenchVertex, Point> Compare_ISOM_Small()
    {
        var algorithm = new ISOMLayoutAlgorithm<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>(
            _smallGraph, null, new ISOMLayoutParameters { Width = 1000, Height = 1000 });
        algorithm.Compute(CancellationToken.None);
        return algorithm.VertexPositions;
    }

    #endregion
}
