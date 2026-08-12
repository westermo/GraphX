using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BenchmarkDotNet.Attributes;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Logic.Models;
using Measure = Westermo.GraphX.Measure;

namespace GraphXBenchmarks;

[MemoryDiagnoser]
public class AvaloniaControlBenchmarks
{
    private const int VertexCount = 120;
    private const int EdgeCount = 260;
    private const double VertexWidth = 56;
    private const double VertexHeight = 36;

    private static readonly Size GraphSurfaceSize = new(1600, 1200);
    private static readonly Rect GraphSurfaceRect = new(default(Point), GraphSurfaceSize);
    private static readonly Size ZoomViewportSize = new(900, 650);
    private static readonly Size WayfinderSize = new(240, 180);

    private sealed class BenchVertex : VertexBase
    {
        public string Name { get; set; } = string.Empty;

        public override string ToString() => Name;
    }

    private sealed class BenchEdge(BenchVertex source, BenchVertex target) : EdgeBase<BenchVertex>(source, target)
    {
        public override Measure.Point[] RoutingPoints { get; set; } = [];
    }

    private BidirectionalGraph<BenchVertex, BenchEdge> _graph = null!;
    private Dictionary<BenchVertex, Point> _positions = null!;

    private Window _graphAreaWindow = null!;
    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> _graphArea = null!;
    private RenderTargetBitmap _graphAreaBitmap = null!;

    private Window _wayfinderWindow = null!;
    private ZoomControl _wayfinderZoomControl = null!;
    private Wayfinder _wayfinder = null!;
    private RenderTargetBitmap _wayfinderBitmap = null!;
    private bool _wayfinderPanToggle;
    private bool _wayfinderResizeToggle;

    [GlobalSetup]
    public void Setup()
    {
        AvaloniaBenchmarkHost.EnsureInitialized();

        _graph = CreateGraph(VertexCount, EdgeCount);
        _positions = GeneratePositions([.. _graph.Vertices], GraphSurfaceSize.Width - VertexWidth,
            GraphSurfaceSize.Height - VertexHeight);
    }

    [IterationSetup]
    public void PrepareIteration()
    {
        (_graphAreaWindow, _graphArea) = CreateGraphAreaScene();
        _graphAreaBitmap = CreateRenderTargetBitmap(GetRenderableSize(_graphArea, GraphSurfaceSize));

        (_wayfinderWindow, _wayfinderZoomControl, _wayfinder) = CreateWayfinderScene();
        _wayfinderBitmap = CreateRenderTargetBitmap(GetRenderableSize(_wayfinder, WayfinderSize));
        // Build the source snapshot outside the measured methods. This makes
        // Wayfinder_Render and Wayfinder_PanOverlay_Render cache-hit paths.
        _wayfinderBitmap.Render(_wayfinder);
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        _graphAreaBitmap?.Dispose();
        _wayfinderBitmap?.Dispose();
        _graphAreaBitmap = null!;
        _wayfinderBitmap = null!;

        CloseWindow(_graphAreaWindow);
        CloseWindow(_wayfinderWindow);
        _graphAreaWindow = null!;
        _wayfinderWindow = null!;
    }

    [Benchmark(Description = "GraphArea Measure - preloaded graph")]
    public Size GraphArea_Measure()
    {
        _graphArea.InvalidateMeasure();
        _graphArea.Measure(GraphSurfaceSize);
        return _graphArea.DesiredSize;
    }

    [Benchmark(Description = "GraphArea Arrange - preloaded graph")]
    public Size GraphArea_Arrange()
    {
        _graphArea.InvalidateArrange();
        _graphArea.Arrange(GraphSurfaceRect);
        return _graphArea.Bounds.Size;
    }

    [Benchmark(Description = "GraphArea Render - preloaded graph")]
    public void GraphArea_Render()
    {
        _graphAreaBitmap.Render(_graphArea);
    }

    [Benchmark(Description = "Wayfinder Render - cached graph content")]
    public void Wayfinder_Render()
    {
        _wayfinderBitmap.Render(_wayfinder);
    }

    [Benchmark(Description = "Wayfinder Render - pan overlay cache hit")]
    public void Wayfinder_PanOverlay_Render()
    {
        _wayfinderPanToggle = !_wayfinderPanToggle;
        _wayfinderZoomControl.TranslateX = _wayfinderPanToggle ? -341 : -340;
        _wayfinderBitmap.Render(_wayfinder);
    }

    [Benchmark(Description = "Wayfinder Render - resize and cache rerasterize")]
    public void Wayfinder_RerasterizeAfterResize()
    {
        _wayfinderResizeToggle = !_wayfinderResizeToggle;
        var width = _wayfinderResizeToggle ? WayfinderSize.Width - 1 : WayfinderSize.Width;
        _wayfinder.Width = width;
        PumpLayout(_wayfinderWindow, 1);
        _wayfinderBitmap.Render(_wayfinder);
    }

    private (Window window, GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> area)
        CreateGraphAreaScene()
    {
        var area = PrepareArea(CreateArea(_graph));
        var window = new Window
        {
            Width = GraphSurfaceSize.Width,
            Height = GraphSurfaceSize.Height,
            Content = area
        };
        window.Show();
        PumpLayout(window, 3);
        return (window, area);
    }

    private (Window window, ZoomControl zoomControl, Wayfinder wayfinder) CreateWayfinderScene()
    {
        var area = PrepareArea(CreateArea(_graph));
        var zoomControl = new ZoomControl
        {
            Width = ZoomViewportSize.Width,
            Height = ZoomViewportSize.Height,
            Content = area
        };
        var wayfinder = new Wayfinder
        {
            Width = WayfinderSize.Width,
            Height = WayfinderSize.Height,
            ZoomControl = zoomControl,
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            ShadowBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
            ViewportBrush = Brushes.Transparent,
            ViewportPen = new Pen(Brushes.Black, 1)
        };

        var root = new StackPanel
        {
            Spacing = 12,
            Children = { zoomControl, wayfinder }
        };

        var window = new Window
        {
            Width = ZoomViewportSize.Width + 40,
            Height = ZoomViewportSize.Height + WayfinderSize.Height + 80,
            Content = root
        };

        window.Show();
        PumpLayout(window, 3);

        zoomControl.Mode = ZoomControlModes.Custom;
        zoomControl.Zoom = 1.35;
        zoomControl.TranslateX = -340;
        zoomControl.TranslateY = -220;
        PumpLayout(window, 2);

        return (window, zoomControl, wayfinder);
    }

    private GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> PrepareArea(
        GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> area)
    {
        area.PreloadGraph(_positions, showObjectsIfPosSpecified: true);
        foreach (var vertexControl in area.VertexList.Values)
        {
            vertexControl.Width = VertexWidth;
            vertexControl.Height = VertexHeight;
        }

        area.ShowAllEdgesArrows();
        area.UpdateAllEdges(true);
        return area;
    }

    private static GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>> CreateArea(
        BidirectionalGraph<BenchVertex, BenchEdge> graph)
    {
        var logicCore = new GXLogicCore<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            Graph = graph,
            EnableParallelEdges = true,
            EdgeCurvingEnabled = false
        };

        return new GraphArea<BenchVertex, BenchEdge, BidirectionalGraph<BenchVertex, BenchEdge>>
        {
            LogicCore = logicCore,
            Width = GraphSurfaceSize.Width,
            Height = GraphSurfaceSize.Height
        };
    }

    private static BidirectionalGraph<BenchVertex, BenchEdge> CreateGraph(int vertexCount, int edgeCount)
    {
        var graph = new BidirectionalGraph<BenchVertex, BenchEdge>();
        var vertices = new List<BenchVertex>(vertexCount);

        for (var i = 0; i < vertexCount; i++)
        {
            var vertex = new BenchVertex
            {
                ID = i,
                Name = $"N{i}"
            };
            vertices.Add(vertex);
            graph.AddVertex(vertex);
        }

        var random = new Random(42);

        for (var i = 1; i < vertexCount; i++)
            graph.AddEdge(new BenchEdge(vertices[random.Next(i)], vertices[i]));

        var addedEdges = vertexCount - 1;
        while (addedEdges < edgeCount)
        {
            var source = vertices[random.Next(vertexCount)];
            BenchVertex target;

            if (addedEdges % 19 == 0)
            {
                target = source;
            }
            else if (addedEdges % 5 == 0)
            {
                target = vertices[(int)((source.ID + 7) % vertexCount)];
            }
            else
            {
                target = vertices[random.Next(vertexCount)];
                if (ReferenceEquals(source, target))
                    target = vertices[(int)((target.ID + 1) % vertexCount)];
            }

            graph.AddEdge(new BenchEdge(source, target));
            addedEdges++;
        }

        return graph;
    }

    private static Dictionary<BenchVertex, Point> GeneratePositions(IReadOnlyList<BenchVertex> vertices, double width,
        double height)
    {
        var positions = new Dictionary<BenchVertex, Point>(vertices.Count);
        var random = new Random(1337);
        var columns = (int)Math.Ceiling(Math.Sqrt(vertices.Count));
        var rows = (int)Math.Ceiling(vertices.Count / (double)columns);
        var cellWidth = width / columns;
        var cellHeight = height / rows;

        for (var i = 0; i < vertices.Count; i++)
        {
            var column = i % columns;
            var row = i / columns;
            var x = column * cellWidth + 12 + random.NextDouble() * Math.Max(8, cellWidth - 24);
            var y = row * cellHeight + 12 + random.NextDouble() * Math.Max(8, cellHeight - 24);
            positions[vertices[i]] = new Point(x, y);
        }

        return positions;
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
        for (var i = 0; i < passes; i++)
        {
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void CloseWindow(Window window)
    {
        if (window == null) return;
        window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
