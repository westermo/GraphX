using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BenchmarkDotNet.Attributes;
using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Logic.Models;
using Measure = Westermo.GraphX.Measure;

namespace GraphXBenchmarks;

/// <summary>
/// Measures the Skia render path of individual GraphX controls after their templates and layout have
/// been prepared. This isolates the visual cost from graph creation and Avalonia layout setup.
/// </summary>
[MemoryDiagnoser]
public class GraphComponentRenderBenchmarks
{
    private const double VertexWidth = 64;
    private const double VertexHeight = 42;
    private const int MatrixVertexCount = 120;
    private const int MatrixEdgeCount = 260;
    private const double MatrixVertexWidth = 56;
    private const double MatrixVertexHeight = 36;

    private static readonly Size GraphSurfaceSize = new(640, 420);
    private static readonly Size VertexSurfaceSize = new(128, 96);
    private static readonly Size MatrixGraphSurfaceSize = new(1600, 1200);
    private static readonly Size MatrixViewportSize = new(640, 420);
    private static readonly Rect MatrixCullingViewport = new(0, 0, 250, 180);

    private RenderScene _vertex = null!;
    private RenderScene _straightEdge = null!;
    private RenderScene _pointerEdge = null!;
    private RenderScene _parallelEdge = null!;
    private RenderScene _selfLoopEdge = null!;
    private RenderScene _routedEdge = null!;
    private RenderScene _verticesOnlyGraphArea = null!;
    private RenderScene _edgesOnlyGraphArea = null!;
    private RenderScene _combinedGraphArea = null!;
    private RenderScene _matrixVerticesOnlyGraphArea = null!;
    private RenderScene _matrixEdgesOnlyGraphArea = null!;
    private RenderScene _matrixCombinedWithArrowsGraphArea = null!;
    private RenderScene _matrixCombinedWithoutArrowsGraphArea = null!;
    private RenderScene _matrixBatchedGraphArea = null!;
    private RenderScene _matrixCulledGraphArea = null!;
    private PanZoomScene _matrixPanZoomScene = null!;
    private PanZoomScene _matrixCachedPanZoomScene = null!;
    private bool _matrixPanToggle;
    private bool _matrixCachedPanToggle;

    [GlobalSetup]
    public void Setup()
    {
        AvaloniaBenchmarkHost.EnsureInitialized();

        _vertex = CreateVertexScene();
        _straightEdge = CreateEdgeScene(EdgeScenario.Straight);
        _pointerEdge = CreateEdgeScene(EdgeScenario.WithPointers);
        _parallelEdge = CreateEdgeScene(EdgeScenario.Parallel);
        _selfLoopEdge = CreateEdgeScene(EdgeScenario.SelfLoop);
        _routedEdge = CreateEdgeScene(EdgeScenario.Routed);
        _verticesOnlyGraphArea = CreateGraphAreaScene(GraphAreaComposition.VerticesOnly);
        _edgesOnlyGraphArea = CreateGraphAreaScene(GraphAreaComposition.EdgesOnly);
        _combinedGraphArea = CreateGraphAreaScene(GraphAreaComposition.Combined);

        var matrixGraph = CreateMatrixGraph();
        var matrixPositions = GenerateMatrixPositions([.. matrixGraph.Vertices]);
        _matrixVerticesOnlyGraphArea = CreateMatrixGraphAreaScene(
            matrixGraph, matrixPositions, GraphAreaComposition.VerticesOnly, showArrows: true);
        _matrixEdgesOnlyGraphArea = CreateMatrixGraphAreaScene(
            matrixGraph, matrixPositions, GraphAreaComposition.EdgesOnly, showArrows: true);
        _matrixCombinedWithArrowsGraphArea = CreateMatrixGraphAreaScene(
            matrixGraph, matrixPositions, GraphAreaComposition.Combined, showArrows: true);
        _matrixCombinedWithoutArrowsGraphArea = CreateMatrixGraphAreaScene(
            matrixGraph, matrixPositions, GraphAreaComposition.Combined, showArrows: false);
        _matrixBatchedGraphArea = CreateMatrixGraphAreaScene(
            matrixGraph, matrixPositions, GraphAreaComposition.Combined, showArrows: false,
            edgeRenderingMode: EdgeRenderingMode.Batched);
        _matrixCulledGraphArea = CreateMatrixGraphAreaScene(
            matrixGraph, matrixPositions, GraphAreaComposition.Combined, showArrows: true,
            cullingViewport: MatrixCullingViewport);
        _matrixPanZoomScene = CreateMatrixPanZoomScene(matrixGraph, matrixPositions);
        _matrixCachedPanZoomScene = CreateMatrixPanZoomScene(
            matrixGraph, matrixPositions, useGraphRenderCache: true);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _vertex?.Dispose();
        _straightEdge?.Dispose();
        _pointerEdge?.Dispose();
        _parallelEdge?.Dispose();
        _selfLoopEdge?.Dispose();
        _routedEdge?.Dispose();
        _verticesOnlyGraphArea?.Dispose();
        _edgesOnlyGraphArea?.Dispose();
        _combinedGraphArea?.Dispose();
        _matrixVerticesOnlyGraphArea?.Dispose();
        _matrixEdgesOnlyGraphArea?.Dispose();
        _matrixCombinedWithArrowsGraphArea?.Dispose();
        _matrixCombinedWithoutArrowsGraphArea?.Dispose();
        _matrixBatchedGraphArea?.Dispose();
        _matrixCulledGraphArea?.Dispose();
        _matrixPanZoomScene?.Dispose();
        _matrixCachedPanZoomScene?.Dispose();
    }

    [Benchmark(Description = "VertexControl Render")]
    public void VertexControl_Render()
    {
        _vertex.Render();
    }

    [Benchmark(Description = "EdgeControl Render - straight without pointers")]
    public void EdgeControl_StraightWithoutPointers_Render()
    {
        _straightEdge.Render();
    }

    [Benchmark(Description = "EdgeControl Render - pointer enabled")]
    public void EdgeControl_WithPointers_Render()
    {
        _pointerEdge.Render();
    }

    [Benchmark(Description = "EdgeControl Render - parallel")]
    public void EdgeControl_Parallel_Render()
    {
        _parallelEdge.Render();
    }

    [Benchmark(Description = "EdgeControl Render - self loop")]
    public void EdgeControl_SelfLoop_Render()
    {
        _selfLoopEdge.Render();
    }

    [Benchmark(Description = "EdgeControl Render - routed")]
    public void EdgeControl_Routed_Render()
    {
        _routedEdge.Render();
    }

    [Benchmark(Description = "EdgeControl Layout - routed without geometry changes")]
    public void EdgeControl_Routed_UnchangedLayout()
    {
        _routedEdge.InvalidateAndLayout();
    }

    [Benchmark(Description = "GraphArea Render - vertices only")]
    public void GraphArea_VerticesOnly_Render()
    {
        _verticesOnlyGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea Render - edges only")]
    public void GraphArea_EdgesOnly_Render()
    {
        _edgesOnlyGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea Render - vertices and edges")]
    public void GraphArea_Combined_Render()
    {
        _combinedGraphArea.Render();
    }

    // The following matrix uses the same-sized deterministic scene as AvaloniaControlBenchmarks:
    // 120 vertices and 260 edges. Setup prepares all controls and geometry outside measurement.
    [Benchmark(Description = "GraphArea matrix (120V/260E) - edges only")]
    public void GraphAreaMatrix_EdgesOnly_Render()
    {
        // Isolates the standard templated edge visual cost.
        _matrixEdgesOnlyGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea matrix (120V/260E) - vertices only")]
    public void GraphAreaMatrix_VerticesOnly_Render()
    {
        // Isolates vertex visual cost.
        _matrixVerticesOnlyGraphArea.Render();
    }

    [Benchmark(Baseline = true, Description = "GraphArea matrix (120V/260E) - combined with arrows")]
    public void GraphAreaMatrix_CombinedWithArrows_Render()
    {
        // Current complete-graph rendering baseline: vertices, templated edges, and arrow pointers.
        _matrixCombinedWithArrowsGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea matrix (120V/260E) - combined without arrows")]
    public void GraphAreaMatrix_CombinedWithoutArrows_Render()
    {
        // Isolates the per-edge pointer visual overhead from the complete-graph baseline.
        _matrixCombinedWithoutArrowsGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea matrix (120V/260E) - batched straight edges without arrows")]
    public void GraphAreaMatrix_BatchedEdges_Render()
    {
        // All edges are non-looping, non-parallel, straight default-template edges with arrows disabled,
        // making every edge eligible for the shared BatchedEdgeLayer.
        _matrixBatchedGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea matrix (120V/260E) - viewport culled")]
    public void GraphAreaMatrix_ViewportCulled_Render()
    {
        // Most graph-coordinate content lies outside MatrixCullingViewport, so this measures the render
        // path after culling has hidden it rather than the O(N) visibility update itself.
        _matrixCulledGraphArea.Render();
    }

    [Benchmark(Description = "GraphArea matrix (120V/260E) - pan frame with viewport culling")]
    public void GraphAreaMatrix_PanFrame_Render()
    {
        // A translation schedules only the ZoomControl transform and GraphArea viewport/culling update.
        // It deliberately does not preload, relayout, or update edge geometry.
        _matrixPanToggle = !_matrixPanToggle;
        _matrixPanZoomScene.ZoomControl.TranslateX = _matrixPanToggle ? -460 : -440;
        Dispatcher.UIThread.RunJobs();
        _matrixPanZoomScene.Render();
    }

    [Benchmark(Description = "GraphArea matrix (120V/260E) - warm raster-cache pan frame")]
    public void GraphAreaMatrix_WarmRasterCachePanFrame_Render()
    {
        // Setup has already constructed the bitmap and hidden live graph
        // children. This measures only a translate and the cached image draw.
        _matrixCachedPanToggle = !_matrixCachedPanToggle;
        _matrixCachedPanZoomScene.ZoomControl.TranslateX = _matrixCachedPanToggle ? -460 : -440;
        Dispatcher.UIThread.RunJobs();
        _matrixCachedPanZoomScene.Render();
    }

    private static RenderScene CreateVertexScene()
    {
        var vertex = new VertexControl(new BenchVertex("Vertex"))
        {
            Width = VertexWidth,
            Height = VertexHeight
        };
        var canvas = new Canvas();
        canvas.Children.Add(vertex);
        Canvas.SetLeft(vertex, 24);
        Canvas.SetTop(vertex, 24);

        return CreateHostedScene(canvas, vertex, VertexSurfaceSize);
    }

    private static RenderScene CreateEdgeScene(EdgeScenario scenario)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var source = new BenchVertex("Source") { ID = 1 };
        var target = scenario == EdgeScenario.SelfLoop ? source : new BenchVertex("Target") { ID = 2 };
        graph.AddVertex(source);
        if (!ReferenceEquals(source, target))
            graph.AddVertex(target);

        var selectedEdge = new BenchEdge(source, target);
        graph.AddEdge(selectedEdge);
        if (scenario == EdgeScenario.Parallel)
        {
            graph.AddEdge(new BenchEdge(source, target));
            graph.AddEdge(new BenchEdge(source, target));
        }

        if (scenario == EdgeScenario.Routed)
        {
            // RoutingPoints and the routing flag are public GraphX APIs, so this exercises the
            // routed EdgeControl path without folding route computation into render measurements.
            selectedEdge.RoutingPoints =
            [
                new Measure.Point(100, 180),
                new Measure.Point(260, 80),
                new Measure.Point(410, 300),
                new Measure.Point(520, 220)
            ];
        }

        var logicCore = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            Graph = graph,
            EnableParallelEdges = scenario == EdgeScenario.Parallel,
            EdgeCurvingEnabled = false,
            DefaultEdgeRoutingAlgorithm = scenario == EdgeScenario.Routed
                ? EdgeRoutingAlgorithmTypeEnum.SimpleER
                : EdgeRoutingAlgorithmTypeEnum.None
        };
        var area = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = logicCore,
            Width = GraphSurfaceSize.Width,
            Height = GraphSurfaceSize.Height
        };
        var positions = new Dictionary<BenchVertex, Point>
        {
            [source] = scenario == EdgeScenario.SelfLoop ? new Point(280, 180) : new Point(80, 150)
        };
        if (!ReferenceEquals(source, target))
            positions[target] = new Point(500, 210);

        PrepareArea(area, positions, showArrows: scenario != EdgeScenario.Straight);
        var scene = CreateHostedScene(area, area, GraphSurfaceSize);
        area.UpdateAllEdges(true);
        PumpLayout(scene.Window, 2);
        scene.SetTarget(area.EdgesList[selectedEdge], GraphSurfaceSize);

        return scene;
    }

    private static RenderScene CreateGraphAreaScene(GraphAreaComposition composition)
    {
        const int vertexCount = 24;
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>(vertexCount);
        var positions = new Dictionary<BenchVertex, Point>(vertexCount);

        for (var index = 0; index < vertexCount; index++)
        {
            var vertex = new BenchVertex($"V{index}") { ID = index };
            graph.AddVertex(vertex);
            vertices.Add(vertex);
            positions[vertex] = new Point(40 + (index % 6) * 100, 40 + (index / 6) * 80);
        }

        if (composition != GraphAreaComposition.VerticesOnly)
        {
            for (var index = 1; index < vertices.Count; index++)
                graph.AddEdge(new BenchEdge(vertices[index - 1], vertices[index]));

            for (var index = 0; index < vertices.Count - 6; index += 3)
                graph.AddEdge(new BenchEdge(vertices[index], vertices[index + 6]));
        }

        var area = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
            {
                Graph = graph,
                EnableParallelEdges = false,
                EdgeCurvingEnabled = false
            },
            Width = GraphSurfaceSize.Width,
            Height = GraphSurfaceSize.Height
        };
        PrepareArea(area, positions, showArrows: true);
        var scene = CreateHostedScene(area, area, GraphSurfaceSize);
        area.UpdateAllEdges(true);
        PumpLayout(scene.Window, 2);

        if (composition == GraphAreaComposition.EdgesOnly)
        {
            foreach (var vertex in area.VertexList.Values)
                area.Children.Remove(vertex);

            PumpLayout(scene.Window, 1);
        }

        return scene;
    }

    private static RenderScene CreateMatrixGraphAreaScene(
        BidirectionalGraph<BenchVertex, BenchEdge> graph,
        Dictionary<BenchVertex, Point> positions,
        GraphAreaComposition composition,
        bool showArrows,
        EdgeRenderingMode edgeRenderingMode = EdgeRenderingMode.Standard,
        Rect? cullingViewport = null)
    {
        var area = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
            {
                Graph = graph,
                EnableParallelEdges = false,
                EdgeCurvingEnabled = false
            },
            Width = MatrixGraphSurfaceSize.Width,
            Height = MatrixGraphSurfaceSize.Height
        };
        PrepareMatrixArea(area, positions, showArrows);
        var scene = CreateHostedScene(area, area, MatrixGraphSurfaceSize);
        area.UpdateAllEdges(true);
        PumpLayout(scene.Window, 2);

        if (composition == GraphAreaComposition.VerticesOnly)
        {
            foreach (var edge in area.EdgesList.Values)
                area.Children.Remove(edge);
        }
        else if (composition == GraphAreaComposition.EdgesOnly)
        {
            foreach (var vertex in area.VertexList.Values)
                area.Children.Remove(vertex);
        }

        if (cullingViewport is { } viewport)
        {
            area.ViewportCulling.CullingMargin = 0;
            area.EnableViewportCulling = true;
            area.UpdateViewport(viewport);
        }

        // Set this only after the default theme has created PART_edgePath. This is intentionally
        // not a custom edge template: the default template's graphx-default-edge-path marker is
        // part of the batched-edge eligibility contract.
        area.EdgeRenderingMode = edgeRenderingMode;
        PumpLayout(scene.Window, 1);
        return scene;
    }

    private static PanZoomScene CreateMatrixPanZoomScene(
        BidirectionalGraph<BenchVertex, BenchEdge> graph,
        Dictionary<BenchVertex, Point> positions,
        bool useGraphRenderCache = false)
    {
        var area = new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
            {
                Graph = graph,
                EnableParallelEdges = false,
                EdgeCurvingEnabled = false
            },
            Width = MatrixGraphSurfaceSize.Width,
            Height = MatrixGraphSurfaceSize.Height
        };
        PrepareMatrixArea(area, positions, showArrows: true);
        area.ViewportCulling.CullingMargin = 0;
        area.EnableViewportCulling = true;

        var zoomControl = new ZoomControl
        {
            Width = MatrixViewportSize.Width,
            Height = MatrixViewportSize.Height,
            Mode = ZoomControlModes.Custom,
            Content = area
        };
        var scene = CreateHostedScene(zoomControl, zoomControl, MatrixViewportSize, area);
        area.UpdateAllEdges(true);
        PumpLayout(scene.Window, 2);
        zoomControl.TranslateX = -450;
        zoomControl.TranslateY = -350;
        Dispatcher.UIThread.RunJobs();
        var panZoomScene = new PanZoomScene(scene, zoomControl);
        if (useGraphRenderCache)
        {
            // Warm the cache before BenchmarkDotNet starts timing. The
            // benchmark deliberately contains no cache construction.
            area.EnableGraphRenderCache = true;
            panZoomScene.Render();
        }

        return panZoomScene;
    }

    private static void PrepareArea(
        GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> area,
        Dictionary<BenchVertex, Point> positions,
        bool showArrows)
    {
        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);
        foreach (var vertex in area.VertexList.Values)
        {
            vertex.Width = VertexWidth;
            vertex.Height = VertexHeight;
        }

        area.ShowAllEdgesArrows(showArrows);
        area.UpdateAllEdges(true);
    }

    private static void PrepareMatrixArea(
        GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> area,
        Dictionary<BenchVertex, Point> positions,
        bool showArrows)
    {
        area.PreloadGraph(positions, showObjectsIfPosSpecified: true);
        foreach (var vertex in area.VertexList.Values)
        {
            vertex.Width = MatrixVertexWidth;
            vertex.Height = MatrixVertexHeight;
        }

        area.ShowAllEdgesArrows(showArrows);
        area.UpdateAllEdges(true);
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateMatrixGraph()
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>(MatrixVertexCount);
        var edgePairs = new HashSet<(int source, int target)>();

        for (var index = 0; index < MatrixVertexCount; index++)
        {
            var vertex = new BenchVertex($"M{index}") { ID = index };
            vertices.Add(vertex);
            graph.AddVertex(vertex);
        }

        var random = new Random(42);
        for (var target = 1; target < vertices.Count; target++)
        {
            var source = random.Next(target);
            edgePairs.Add((source, target));
            graph.AddEdge(new BenchEdge(vertices[source], vertices[target]));
        }

        while (edgePairs.Count < MatrixEdgeCount)
        {
            var source = random.Next(vertices.Count);
            var target = random.Next(vertices.Count);
            if (source == target || !edgePairs.Add((source, target))) continue;
            graph.AddEdge(new BenchEdge(vertices[source], vertices[target]));
        }

        return graph;
    }

    private static Dictionary<BenchVertex, Point> GenerateMatrixPositions(IReadOnlyList<BenchVertex> vertices)
    {
        var positions = new Dictionary<BenchVertex, Point>(vertices.Count);
        var random = new Random(1337);
        var columns = (int)Math.Ceiling(Math.Sqrt(vertices.Count));
        var rows = (int)Math.Ceiling(vertices.Count / (double)columns);
        var cellWidth = (MatrixGraphSurfaceSize.Width - MatrixVertexWidth) / columns;
        var cellHeight = (MatrixGraphSurfaceSize.Height - MatrixVertexHeight) / rows;

        for (var index = 0; index < vertices.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            positions[vertices[index]] = new Point(
                column * cellWidth + 12 + random.NextDouble() * Math.Max(8, cellWidth - 24),
                row * cellHeight + 12 + random.NextDouble() * Math.Max(8, cellHeight - 24));
        }

        return positions;
    }

    private static RenderScene CreateHostedScene(Control root, Control target, Size size, IDisposable cleanup = null)
    {
        var window = new Window
        {
            Width = size.Width,
            Height = size.Height,
            Content = root
        };
        window.Show();
        PumpLayout(window, 3);

        return new RenderScene(window, target, CreateRenderTargetBitmap(GetRenderableSize(target, size)),
            cleanup ?? root as IDisposable);
    }

    private static Size GetRenderableSize(Control control, Size fallback)
    {
        var size = control.Bounds.Size;
        return size.Width > 0 && size.Height > 0 ? size : fallback;
    }

    private static RenderTargetBitmap CreateRenderTargetBitmap(Size size)
    {
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(size.Width)),
            Math.Max(1, (int)Math.Ceiling(size.Height)));
        return new RenderTargetBitmap(pixelSize, new Vector(96, 96));
    }

    private static void PumpLayout(Window window, int passes)
    {
        for (var index = 0; index < passes; index++)
        {
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            Dispatcher.UIThread.RunJobs();
        }
    }

    private sealed class BenchVertex(string name) : VertexBase
    {
        public string Name { get; } = name;

        public override string ToString() => Name;
    }

    private sealed class BenchEdge(BenchVertex source, BenchVertex target) : EdgeBase<BenchVertex>(source, target)
    {
        public override Measure.Point[] RoutingPoints { get; set; } = [];
    }

    private sealed class RenderScene : IDisposable
    {
        private readonly Window _window;
        private readonly IDisposable _cleanup;
        private RenderTargetBitmap _bitmap;
        private Control _target;

        public RenderScene(Window window, Control target, RenderTargetBitmap bitmap, IDisposable cleanup)
        {
            _window = window;
            _target = target;
            _bitmap = bitmap;
            _cleanup = cleanup;
        }

        public Window Window => _window;

        public void Render()
        {
            _bitmap.Render(_target);
        }

        public void SetTarget(Control target, Size fallbackSize)
        {
            _bitmap.Dispose();
            _target = target;
            _bitmap = CreateRenderTargetBitmap(GetRenderableSize(target, fallbackSize));
        }

        public void InvalidateAndLayout()
        {
            _target.InvalidateMeasure();
            _target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _target.Arrange(new Rect(0, 0, _target.DesiredSize.Width, _target.DesiredSize.Height));
        }

        public void Dispose()
        {
            _bitmap.Dispose();
            _window.Close();
            Dispatcher.UIThread.RunJobs();
            _cleanup?.Dispose();
        }
    }

    private sealed class PanZoomScene(RenderScene renderScene, ZoomControl zoomControl) : IDisposable
    {
        public ZoomControl ZoomControl { get; } = zoomControl;

        public void Render() => renderScene.Render();

        public void Dispose() => renderScene.Dispose();
    }

    private enum EdgeScenario
    {
        Straight,
        WithPointers,
        Parallel,
        SelfLoop,
        Routed
    }

    private enum GraphAreaComposition
    {
        VerticesOnly,
        EdgesOnly,
        Combined
    }
}
