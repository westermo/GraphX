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

/// <summary>
/// Benchmarks for performance optimizations including viewport culling,
/// batch updates, geometry caching, object pooling, and LOD.
/// </summary>
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class OptimizationBenchmarks
{
    private class BenchVertex : VertexBase
    {
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    private class BenchEdge : EdgeBase<BenchVertex>
    {
        public BenchEdge(BenchVertex source, BenchVertex target) : base(source, target) { }
        public override Measure.Point[] RoutingPoints { get; set; } = null!;
    }

    private static bool _avaloniaInitialized = false;
    private static readonly object _initLock = new object();

    private BidirectionalGraph<BenchVertex, BenchEdge> _largeGraph = null!;
    private Dictionary<BenchVertex, Point> _largePositions = null!;
    
    // Pre-loaded areas for isolated benchmarks
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _cachingTestArea = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _cullingTestArea = null!;

    [GlobalSetup]
    public void Setup()
    {
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

        _largeGraph = CreateGraph(1000, 2000);
        _largePositions = GenerateGridPositions([.. _largeGraph.Vertices], 100);
        
        // Pre-create areas for caching tests - first update populates cache
        _cachingTestArea = CreateAndPreloadArea(_largeGraph, _largePositions);
        foreach (var edge in _cachingTestArea.EdgesList.Values)
        {
            edge.EnableGeometryCaching = true;
        }
        _cachingTestArea.UpdateAllEdges(true, skipHiddenEdges: false);
        
        // Pre-create area for culling tests with culling enabled and viewport set
        _cullingTestArea = CreateAndPreloadArea(_largeGraph, _largePositions);
        _cullingTestArea.EnableViewportCulling = true;
        _cullingTestArea.UpdateViewport(new Rect(0, 0, 2500, 2500)); // 25% viewport
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateGraph(int vertexCount, int edgeCount)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>();
        
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new BenchVertex { ID = i, Name = $"V{i}" };
            vertices.Add(v);
            graph.AddVertex(v);
        }

        var random = new Random(42);
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

    private static Dictionary<BenchVertex, Point> GenerateGridPositions(List<BenchVertex> vertices, double spacing)
    {
        var positions = new Dictionary<BenchVertex, Point>();
        int cols = (int)Math.Ceiling(Math.Sqrt(vertices.Count));
        
        for (int i = 0; i < vertices.Count; i++)
        {
            int row = i / cols;
            int col = i % cols;
            positions[vertices[i]] = new Point(col * spacing, row * spacing);
        }

        return positions;
    }

    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> CreateAndPreloadArea(
        BidirectionalGraph<BenchVertex, BenchEdge> graph,
        Dictionary<BenchVertex, Point> positions)
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
            Width = 10000,
            Height = 10000
        };
        
        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);
        return area;
    }

    // ==================== VIEWPORT CULLING BENCHMARKS ====================

    /// <summary>
    /// Benchmark viewport culling update on a large graph.
    /// </summary>
    [Benchmark]
    public void ViewportCulling_UpdateVisibility_1000Nodes()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        area.EnableViewportCulling = true;
        
        // Simulate viewport that covers only 25% of the graph
        var viewport = new Rect(0, 0, 2500, 2500);
        area.UpdateViewport(viewport);
    }

    /// <summary>
    /// Benchmark repeated viewport updates (panning simulation).
    /// </summary>
    [Benchmark]
    public void ViewportCulling_PanSimulation_10Updates()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        area.EnableViewportCulling = true;
        
        // Simulate 10 pan operations
        for (int i = 0; i < 10; i++)
        {
            var x = i * 500;
            var viewport = new Rect(x, 0, 2000, 2000);
            area.UpdateViewport(viewport);
        }
    }

    /// <summary>
    /// Baseline: update all edges without culling - updates all 2000 edges.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void NoCulling_UpdateAllEdges_1000Nodes()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        // skipHiddenEdges = false to ensure all edges are updated
        area.UpdateAllEdges(true, skipHiddenEdges: false);
    }

    /// <summary>
    /// Update edges with culling enabled - skips hidden edges.
    /// Pre-loaded area has viewport set to 25% so ~75% of edges should be skipped.
    /// </summary>
    [Benchmark]
    public void WithCulling_UpdateVisibleEdgesOnly()
    {
        // Uses pre-configured area with culling and 25% viewport from GlobalSetup
        // skipHiddenEdges = true (default) skips invisible edges
        _cullingTestArea.UpdateAllEdges(true, skipHiddenEdges: true);
    }

    /// <summary>
    /// Update edges with culling enabled but forcing all edges to update.
    /// Shows the overhead of culling visibility checks without the skip benefit.
    /// </summary>
    [Benchmark]
    public void WithCulling_UpdateAllEdges_NoSkip()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        area.EnableViewportCulling = true;
        area.UpdateViewport(new Rect(0, 0, 2500, 2500));
        // skipHiddenEdges = false to update all edges even if hidden
        area.UpdateAllEdges(true, skipHiddenEdges: false);
    }

    // ==================== BATCH UPDATE BENCHMARKS ====================

    /// <summary>
    /// Move 100 vertices without batching - each triggers edge updates.
    /// </summary>
    [Benchmark]
    public void NoBatching_Move100Vertices()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        var vertices = new List<BenchVertex>(_largeGraph.Vertices);
        
        for (int i = 0; i < 100 && i < vertices.Count; i++)
        {
            if (area.VertexList.TryGetValue(vertices[i], out var vc))
            {
                var pos = vc.GetPosition();
                vc.SetPosition(new Point(pos.X + 10, pos.Y + 10));
            }
        }
    }

    /// <summary>
    /// Move 100 vertices with batch update - single edge update at end.
    /// </summary>
    [Benchmark]
    public void WithBatching_Move100Vertices()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        var vertices = new List<BenchVertex>(_largeGraph.Vertices);
        
        using (area.BeginDeferredPositionUpdate())
        {
            for (int i = 0; i < 100 && i < vertices.Count; i++)
            {
                if (area.VertexList.TryGetValue(vertices[i], out var vc))
                {
                    var pos = vc.GetPosition();
                    vc.SetPosition(new Point(pos.X + 10, pos.Y + 10));
                }
            }
        }
    }

    // ==================== GEOMETRY CACHING BENCHMARKS ====================

    /// <summary>
    /// Update all edges without geometry caching - must recalculate every time.
    /// </summary>
    [Benchmark]
    public void NoCaching_UpdateAllEdges()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        
        // Ensure caching is disabled
        foreach (var edge in area.EdgesList.Values)
        {
            edge.EnableGeometryCaching = false;
        }
        
        area.UpdateAllEdges(true, skipHiddenEdges: false);
    }

    /// <summary>
    /// Update all edges with geometry caching enabled - first update populates cache.
    /// </summary>
    [Benchmark]
    public void WithCaching_UpdateAllEdges_FirstPass()
    {
        var area = CreateAndPreloadArea(_largeGraph, _largePositions);
        
        foreach (var edge in area.EdgesList.Values)
        {
            edge.EnableGeometryCaching = true;
        }
        
        // First pass - populates cache
        area.UpdateAllEdges(true, skipHiddenEdges: false);
    }

    /// <summary>
    /// Update edges when cache is already populated - should skip geometry recalculation.
    /// Uses pre-loaded area where cache was populated in GlobalSetup.
    /// </summary>
    [Benchmark]
    public void WithCaching_UpdateAllEdges_CacheHit()
    {
        // _cachingTestArea already has cache populated from GlobalSetup
        // This measures pure cache-hit performance
        _cachingTestArea.UpdateAllEdges(true, skipHiddenEdges: false);
    }

    // ==================== OBJECT POOLING BENCHMARKS ====================

    /// <summary>
    /// Benchmark list allocation without pooling.
    /// </summary>
    [Benchmark]
    public void NoPooling_AllocateLists_1000Times()
    {
        for (int i = 0; i < 1000; i++)
        {
            var list = new List<int>();
            for (int j = 0; j < 100; j++)
                list.Add(j);
            // List is garbage collected
        }
    }

    /// <summary>
    /// Benchmark list usage with pooling.
    /// </summary>
    [Benchmark]
    public void WithPooling_RentReturnLists_1000Times()
    {
        for (int i = 0; i < 1000; i++)
        {
            var list = ListPool<int>.Rent();
            for (int j = 0; j < 100; j++)
                list.Add(j);
            ListPool<int>.Return(list);
        }
    }

    /// <summary>
    /// Benchmark dictionary allocation without pooling.
    /// </summary>
    [Benchmark]
    public void NoPooling_AllocateDictionaries_1000Times()
    {
        for (int i = 0; i < 1000; i++)
        {
            var dict = new Dictionary<int, int>();
            for (int j = 0; j < 50; j++)
                dict[j] = j * 2;
            // Dictionary is garbage collected
        }
    }

    /// <summary>
    /// Benchmark dictionary usage with pooling.
    /// </summary>
    [Benchmark]
    public void WithPooling_RentReturnDictionaries_1000Times()
    {
        for (int i = 0; i < 1000; i++)
        {
            var dict = DictionaryPool<int, int>.Rent();
            for (int j = 0; j < 50; j++)
                dict[j] = j * 2;
            DictionaryPool<int, int>.Return(dict);
        }
    }

    // ==================== LEVEL OF DETAIL BENCHMARKS ====================

    /// <summary>
    /// Benchmark LOD threshold checks.
    /// </summary>
    [Benchmark]
    public void LodSettings_ThresholdChecks_1Million()
    {
        var settings = new LevelOfDetailSettings
        {
            IsEnabled = true,
            HideArrowsZoomThreshold = 0.3,
            HideEdgeLabelsZoomThreshold = 0.5,
            HideVertexLabelsZoomThreshold = 0.4
        };

        var random = new Random(42);
        int visible = 0;
        
        for (int i = 0; i < 1_000_000; i++)
        {
            var zoom = random.NextDouble() * 2.0; // 0.0 to 2.0
            if (settings.ShouldShowArrows(zoom)) visible++;
            if (settings.ShouldShowEdgeLabels(zoom)) visible++;
            if (settings.ShouldShowVertexLabels(zoom)) visible++;
        }
    }

    // ==================== PARALLEL EDGES WITH POOLING ====================

    /// <summary>
    /// Benchmark UpdateParallelEdgesData with pooled collections.
    /// </summary>
    [Benchmark]
    public void ParallelEdges_WithPooling_1000Edges()
    {
        var graph = CreateGraphWithParallelEdges(200, 1000);
        var positions = GenerateGridPositions([.. graph.Vertices], 100);
        
        var lc = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            Graph = graph,
            EnableParallelEdges = true
        };
        
        var area = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = lc,
            Width = 5000,
            Height = 5000
        };
        
        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);
        
        // This internally uses pooled collections
        area.UpdateAllEdges(true);
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateGraphWithParallelEdges(int vertexCount, int edgeCount)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>(allowParallelEdges: true);
        var vertices = new List<BenchVertex>();
        
        for (int i = 0; i < vertexCount; i++)
        {
            var v = new BenchVertex { ID = i, Name = $"V{i}" };
            vertices.Add(v);
            graph.AddVertex(v);
        }

        var random = new Random(42);
        
        // Create edges with some parallel edges (same source/target)
        for (int i = 0; i < edgeCount; i++)
        {
            var sourceIdx = random.Next(vertexCount);
            var targetIdx = random.Next(vertexCount);
            if (sourceIdx != targetIdx)
            {
                graph.AddEdge(new BenchEdge(vertices[sourceIdx], vertices[targetIdx]));
            }
        }

        return graph;
    }
}
