using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Models;
using Avalonia;
using Avalonia.Headless;
using Measure = Westermo.GraphX.Measure;
using Westermo.GraphX.Controls.Controls;

namespace GraphXBenchmarks;

[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Monitoring, warmupCount: 3, iterationCount: 10, invocationCount: 1)]
public class GraphRenderingBenchmarks
{
    private class BenchVertex : VertexBase
    {
        public string Name { get; set; } = string.Empty;

        public override string ToString() => Name;
    }

    private class BenchEdge : EdgeBase<BenchVertex>
    {
        public BenchEdge(BenchVertex source, BenchVertex target) : base(source, target)
        {
        }

        public override Measure.Point[] RoutingPoints { get; set; }
    }

    private static bool _avaloniaInitialized = false;
    private static readonly object _initLock = new object();

    private BidirectionalGraph<BenchVertex, BenchEdge> _smallGraph = null!;
    private BidirectionalGraph<BenchVertex, BenchEdge> _mediumGraph = null!;
    private BidirectionalGraph<BenchVertex, BenchEdge> _largeGraph = null!;
    private BidirectionalGraph<BenchVertex, BenchEdge> _selfLoopGraph = null!;
    private Dictionary<BenchVertex, Point> _smallPositions = null!;
    private Dictionary<BenchVertex, Point> _mediumPositions = null!;
    private Dictionary<BenchVertex, Point> _largePositions = null!;
    private Dictionary<BenchVertex, Point> _selfLoopPositions = null!;

    // Pre-loaded areas refreshed each iteration via [IterationSetup]
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _smallPreloaded = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _mediumPreloaded = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _largePreloaded = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _largeVerticesOnly = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _largeVerticesPositioned = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _largeParallelPreloaded = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _largeCurvingPreloaded = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _selfLoopPreloaded = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize Avalonia in headless mode (thread-safe, only once)
        lock (_initLock)
        {
            if (!_avaloniaInitialized)
            {
                AppBuilder.Configure<Application>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
                _avaloniaInitialized = true;
            }
        }

        _smallGraph = CreateGraph(10, 15);
        _mediumGraph = CreateGraph(100, 200);
        _largeGraph = CreateGraph(500, 1000);
        _smallPositions = GeneratePositions([.. _smallGraph.Vertices], 800, 600);
        _mediumPositions = GeneratePositions([.. _mediumGraph.Vertices], 2000, 1500);
        _largePositions = GeneratePositions([.. _largeGraph.Vertices], 5000, 4000);
        _selfLoopGraph = CreateGraphWithSelfLoops(100, 150, 20);
        _selfLoopPositions = GeneratePositions([.. _selfLoopGraph.Vertices], 2000, 1500);
    }

    [IterationSetup]
    public void PrepareIteration()
    {
        _smallPreloaded = CreateArea(_smallGraph, _smallPositions);
        _smallPreloaded.PreloadGraph(_smallPositions, showObjectsIfPosSpecified: true);

        _mediumPreloaded = CreateArea(_mediumGraph, _mediumPositions);
        _mediumPreloaded.PreloadGraph(_mediumPositions, showObjectsIfPosSpecified: true);

        _largePreloaded = CreateArea(_largeGraph, _largePositions);
        _largePreloaded.PreloadGraph(_largePositions, showObjectsIfPosSpecified: true);

        _largeVerticesOnly = CreateArea(_largeGraph, _largePositions);
        _largeVerticesOnly.PreloadVertexes();

        _largeVerticesPositioned = CreateArea(_largeGraph, _largePositions);
        _largeVerticesPositioned.PreloadVertexes();
        foreach (var item in _largePositions)
        {
            if (_largeVerticesPositioned.VertexList.TryGetValue(item.Key, out var value))
                value.SetPosition(item.Value);
        }

        var lcParallel = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            Graph = _largeGraph,
            EnableParallelEdges = true
        };
        _largeParallelPreloaded = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = lcParallel,
            Width = 5000,
            Height = 4000
        };
        _largeParallelPreloaded.PreloadGraph(_largePositions, showObjectsIfPosSpecified: true);

        var lcCurving = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            Graph = _largeGraph,
            EnableParallelEdges = false,
            EdgeCurvingEnabled = true
        };
        _largeCurvingPreloaded = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = lcCurving,
            Width = 5000,
            Height = 4000
        };
        _largeCurvingPreloaded.PreloadGraph(_largePositions, showObjectsIfPosSpecified: true);

        _selfLoopPreloaded = CreateArea(_selfLoopGraph, _selfLoopPositions);
        _selfLoopPreloaded.PreloadGraph(_selfLoopPositions, showObjectsIfPosSpecified: true);
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateGraph(int vertexCount, int edgeCount)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>();
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new BenchVertex
            {
                ID = i,
                Name = $"V{i}"
            };
            vertices.Add(v);
            graph.AddVertex(v);
        }

        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < edgeCount; i++)
        {
            var source = vertices[random.Next(vertexCount)];
            var target = vertices[random.Next(vertexCount)];
            if (source != target)
            {
                graph.AddEdge(new BenchEdge(source, target));
            }
        }

        return graph;
    }

    private static Dictionary<BenchVertex, Point> GeneratePositions(List<BenchVertex> vertices, double width,
        double height)
    {
        var positions = new Dictionary<BenchVertex, Point>();
        var random = new Random(42);
        foreach (var v in vertices)
        {
            positions[v] = new Point(random.NextDouble() * width, random.NextDouble() * height);
        }

        return positions;
    }

    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> CreateArea(
        BidirectionalGraph<BenchVertex, BenchEdge> graph, Dictionary<BenchVertex, Point> positions)
    {
        var lc = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            Graph = graph,
            EnableParallelEdges = false,
            EdgeCurvingEnabled = false
        };
        var area = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = lc,
            Width = 5000,
            Height = 4000
        };
        return area;
    }

    [Benchmark]
    public int SmallGraph_PreloadVertexes()
    {
        var area = CreateArea(_smallGraph, _smallPositions);
        area.PreloadVertexes();
        return area.VertexList.Count;
    }

    [Benchmark]
    public int MediumGraph_PreloadVertexes()
    {
        var area = CreateArea(_mediumGraph, _mediumPositions);
        area.PreloadVertexes();
        return area.VertexList.Count;
    }

    [Benchmark]
    public int LargeGraph_PreloadVertexes()
    {
        var area = CreateArea(_largeGraph, _largePositions);
        area.PreloadVertexes();
        return area.VertexList.Count;
    }

    [Benchmark]
    public int SmallGraph_PreloadAndGenerateEdges()
    {
        var area = CreateArea(_smallGraph, _smallPositions);
        area.PreloadGraph(_smallPositions, showObjectsIfPosSpecified: true);
        return area.VertexList.Count;
    }

    [Benchmark]
    public int MediumGraph_PreloadAndGenerateEdges()
    {
        var area = CreateArea(_mediumGraph, _mediumPositions);
        area.PreloadGraph(_mediumPositions, showObjectsIfPosSpecified: true);
        return area.VertexList.Count;
    }

    [Benchmark]
    public int LargeGraph_PreloadAndGenerateEdges()
    {
        var area = CreateArea(_largeGraph, _largePositions);
        area.PreloadGraph(_largePositions, showObjectsIfPosSpecified: true);
        return area.VertexList.Count;
    }

    [Benchmark]
    public void SmallGraph_UpdateAllEdges()
    {
        _smallPreloaded.UpdateAllEdges(true);
    }

    [Benchmark]
    public void MediumGraph_UpdateAllEdges()
    {
        _mediumPreloaded.UpdateAllEdges(true);
    }

    [Benchmark]
    public void LargeGraph_UpdateAllEdges()
    {
        _largePreloaded.UpdateAllEdges(true);
    }

    // ==================== ADDITIONAL GRANULAR BENCHMARKS ====================

    /// <summary>
    /// Benchmark edge generation separately after vertices are already loaded.
    /// This helps isolate edge path calculation costs.
    /// </summary>
    [Benchmark]
    public void LargeGraph_EdgeGenerationOnly()
    {
        _largeVerticesPositioned.GenerateAllEdges();
    }

    /// <summary>
    /// Benchmark UpdateAllEdges with performFullUpdate=false (rendering only)
    /// vs true (full update including children checks).
    /// </summary>
    [Benchmark]
    public void LargeGraph_UpdateEdgesRenderingOnly()
    {
        _largePreloaded.UpdateAllEdges(performFullUpdate: false);
    }

    /// <summary>
    /// Benchmark with parallel edges enabled - tests UpdateParallelEdgesData cost.
    /// </summary>
    [Benchmark]
    public void LargeGraph_UpdateEdges_WithParallelEdges()
    {
        _largeParallelPreloaded.UpdateAllEdges(true);
    }

    /// <summary>
    /// Benchmark with edge curving enabled - tests GetCurveThroughPoints calculation cost.
    /// This specifically profiles the curve generation algorithm.
    /// </summary>
    [Benchmark]
    public void LargeGraph_UpdateEdges_WithCurving()
    {
        _largeCurvingPreloaded.UpdateAllEdges(true);
    }

    /// <summary>
    /// Benchmark position updates overhead.
    /// </summary>
    [Benchmark]
    public void LargeGraph_PositionUpdatesCost()
    {
        foreach (var item in _largePositions)
        {
            if (_largeVerticesOnly.VertexList.TryGetValue(item.Key, out var value))
            {
                value.SetPosition(item.Value);
            }
        }
    }

    /// <summary>
    /// Benchmark with self-looped edges to test self-loop handling overhead.
    /// </summary>
    [Benchmark]
    public void MediumGraph_WithSelfLoops_UpdateAllEdges()
    {
        _selfLoopPreloaded.UpdateAllEdges(true);
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateGraphWithSelfLoops(int vertexCount, int edgeCount,
        int selfLoopCount)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>();
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new BenchVertex
            {
                ID = i,
                Name = $"V{i}"
            };
            vertices.Add(v);
            graph.AddVertex(v);
        }

        var random = new Random(42);

        // Add regular edges
        for (int i = 0; i < edgeCount; i++)
        {
            var source = vertices[random.Next(vertexCount)];
            var target = vertices[random.Next(vertexCount)];
            if (source != target)
            {
                graph.AddEdge(new BenchEdge(source, target));
            }
        }

        // Add self-loops
        for (int i = 0; i < selfLoopCount && i < vertexCount; i++)
        {
            var v = vertices[i];
            graph.AddEdge(new BenchEdge(v, v));
        }

        return graph;
    }
}