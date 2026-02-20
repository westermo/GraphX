using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ShowcaseApp.Avalonia.ExampleModels;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls.Models;

namespace ShowcaseApp.Avalonia.Pages;

/// <summary>
/// Demonstrates performance optimizations for large graphs:
/// - Viewport culling
/// - Geometry caching
/// - Level of detail
/// - Edge update throttling
/// - Batch updates
/// </summary>
public partial class PerformanceGraph : UserControl
{
    private readonly Random _random = new(42);
    private readonly Stopwatch _stopwatch = new();
    private DispatcherTimer? _statsTimer;

    public PerformanceGraph()
    {
        InitializeComponent();

        var logic = new LogicCoreExample
        {
            DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog,
            DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None,
            EdgeCurvingEnabled = true
        };

        // Configure overlap removal for better spacing
        logic.DefaultOverlapRemovalAlgorithmParams.HorizontalGap = 80;
        logic.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 80;

        graphArea.LogicCore = logic;
        graphArea.SetVerticesDrag(true, true);
        graphArea.VertexLabelFactory = new DefaultVertexLabelFactory();

        // Initialize selection tracking with Multiple selection mode
        graphArea.SelectedVertices = new HashSet<DataVertex>();
        graphArea.SelectionMode = SelectionMode.Multiple;

        // Setup zoom control
        zoomCtrl.IsAnimationEnabled = false;
        zoomCtrl.MinZoom = 0.05;
        zoomCtrl.MaxZoom = 5;
        zoomCtrl.ZoomSensitivity = 250;

        // Wire up events
        btnGenerate.Click += OnGenerateClick;
        btnUpdateAllEdges.Click += OnUpdateAllEdgesClick;
        btnMoveRandom.Click += OnMoveRandomClick;
        btnBatchMove.Click += OnBatchMoveClick;
        btnZoomToFit.Click += OnZoomToFitClick;
        btnInvalidateCache.Click += OnInvalidateCacheClick;

        chkEnableCulling.IsCheckedChanged += OnCullingChanged;
        chkEnableCaching.IsCheckedChanged += OnCachingChanged;
        chkEnableLod.IsCheckedChanged += OnLodChanged;

        sliderArrowThreshold.PropertyChanged += OnSliderChanged;
        sliderLabelThreshold.PropertyChanged += OnSliderChanged;

        graphArea.GenerateGraphFinished += OnGraphGenerated;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Start stats timer
        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _statsTimer.Tick += UpdateStats;
        _statsTimer.Start();

        // Apply initial settings
        ApplySettings();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _statsTimer?.Stop();
        _statsTimer = null;
    }

    private void ApplySettings()
    {
        // Viewport Culling
        graphArea.EnableViewportCulling = chkEnableCulling.IsChecked == true;
        if (int.TryParse(txtCullingMargin.Text, out var margin))
            graphArea.ViewportCulling.CullingMargin = margin;

        // Level of Detail
        graphArea.LodSettings.IsEnabled = chkEnableLod.IsChecked == true;
        graphArea.LodSettings.HideArrowsZoomThreshold = sliderArrowThreshold.Value;
        graphArea.LodSettings.HideEdgeLabelsZoomThreshold = sliderLabelThreshold.Value;
        graphArea.LodSettings.HideVertexLabelsZoomThreshold = sliderLabelThreshold.Value;
    }

    private void UpdateStats(object? sender, EventArgs e)
    {
        var vertexCount = graphArea.VertexList.Count;
        var edgeCount = graphArea.EdgesList.Count;
        var visibleVertices = graphArea.VertexList.Values.Count(v => v.IsVisible);
        var visibleEdges = graphArea.EdgesList.Values.Count(ec => ec.IsVisible);
        var zoom = zoomCtrl.Zoom;

        txtCurrentZoom.Text = $"{zoom:F2}";
        txtArrowThreshold.Text = $"{sliderArrowThreshold.Value:F1}";
        txtLabelThreshold.Text = $"{sliderLabelThreshold.Value:F1}";

        txtCullingStats.Text = $"Visible: {visibleVertices}/{vertexCount} vertices, {visibleEdges}/{edgeCount} edges";

        txtStats.Text = $"""
                         Vertices: {vertexCount} ({visibleVertices} visible)
                         Edges: {edgeCount} ({visibleEdges} visible)
                         Zoom: {zoom:F2}
                         Culling: {(graphArea.EnableViewportCulling ? "ON" : "OFF")}
                         Caching: {(chkEnableCaching.IsChecked == true ? "ON" : "OFF")}
                         LOD: {(graphArea.LodSettings.IsEnabled ? "ON" : "OFF")}
                         """;
    }

    private void OnGenerateClick(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtNodeCount.Text, out var nodeCount)) nodeCount = 500;
        if (!int.TryParse(txtEdgeCount.Text, out var edgeCount)) edgeCount = 1000;

        nodeCount = Math.Clamp(nodeCount, 10, 5000);
        edgeCount = Math.Clamp(edgeCount, nodeCount - 1, nodeCount * 3);

        loader.IsVisible = true;
        graphArea.ClearLayout();

        // Generate graph asynchronously
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _stopwatch.Restart();
            var graph = GenerateLargeGraph(nodeCount, edgeCount);
            graphArea.LogicCore!.Graph = graph;
            graphArea.GenerateGraph(graph);
        }, DispatcherPriority.Background);
    }

    private void OnGraphGenerated(object? sender, EventArgs e)
    {
        _stopwatch.Stop();
        loader.IsVisible = false;

        // Apply settings to new edges
        ApplySettings();

        // Update all edges to generate geometry
        graphArea.UpdateAllEdges(true, skipHiddenEdges: false);

        zoomCtrl.ZoomToFill();

        txtStats.Text = $"Generated in {_stopwatch.ElapsedMilliseconds}ms\n" + txtStats.Text;
    }

    private GraphExample GenerateLargeGraph(int nodeCount, int edgeCount)
    {
        var graph = new GraphExample();
        var vertices = new DataVertex[nodeCount];

        // Create vertices
        for (int i = 0; i < nodeCount; i++)
        {
            vertices[i] = new DataVertex($"V{i}") { ID = i };
            graph.AddVertex(vertices[i]);
        }

        // Create a spanning tree first to ensure connectivity
        for (int i = 1; i < nodeCount; i++)
        {
            var parent = _random.Next(i);
            graph.AddEdge(new DataEdge(vertices[parent], vertices[i]));
        }

        // Add remaining random edges
        var remainingEdges = edgeCount - (nodeCount - 1);
        for (int i = 0; i < remainingEdges; i++)
        {
            var source = _random.Next(nodeCount);
            var target = _random.Next(nodeCount);
            if (source != target)
            {
                graph.AddEdge(new DataEdge(vertices[source], vertices[target]));
            }
        }

        return graph;
    }

    private void OnUpdateAllEdgesClick(object? sender, RoutedEventArgs e)
    {
        _stopwatch.Restart();
        graphArea.UpdateAllEdges(true);
        _stopwatch.Stop();

        txtStats.Text = $"UpdateAllEdges: {_stopwatch.ElapsedMilliseconds}ms\n" + txtStats.Text;
    }

    private void OnMoveRandomClick(object? sender, RoutedEventArgs e)
    {
        if (graphArea.VertexList.Count == 0) return;

        var vertices = graphArea.VertexList.Values.ToList();
        var count = Math.Min(50, vertices.Count);

        _stopwatch.Restart();
        for (int i = 0; i < count; i++)
        {
            var vc = vertices[_random.Next(vertices.Count)];
            var pos = vc.GetPosition();
            vc.SetPosition(new Point(pos.X + _random.Next(-50, 51), pos.Y + _random.Next(-50, 51)));
        }

        _stopwatch.Stop();

        txtStats.Text = $"Move 50 vertices: {_stopwatch.ElapsedMilliseconds}ms\n" + txtStats.Text;
    }

    private void OnBatchMoveClick(object? sender, RoutedEventArgs e)
    {
        if (graphArea.VertexList.Count == 0) return;

        var vertices = graphArea.VertexList.Values.ToList();
        var count = Math.Min(50, vertices.Count);

        _stopwatch.Restart();
        using (graphArea.BeginDeferredPositionUpdate())
        {
            for (int i = 0; i < count; i++)
            {
                var vc = vertices[_random.Next(vertices.Count)];
                var pos = vc.GetPosition();
                vc.SetPosition(new Point(pos.X + _random.Next(-50, 51), pos.Y + _random.Next(-50, 51)));
            }
        }

        _stopwatch.Stop();

        txtStats.Text = $"Batch move 50: {_stopwatch.ElapsedMilliseconds}ms\n" + txtStats.Text;
    }

    private void OnZoomToFitClick(object? sender, RoutedEventArgs e)
    {
        zoomCtrl.ZoomToFill();
    }

    private void OnInvalidateCacheClick(object? sender, RoutedEventArgs e)
    {
        // Note: Cache invalidation is not implemented here. We report this explicitly
        // to avoid misleading users into thinking any caches were actually cleared.
        txtStats.Text = "Cache invalidation is not available in this configuration.\n" + txtStats.Text;
    }

    private void OnCullingChanged(object? sender, RoutedEventArgs e)
    {
        graphArea.EnableViewportCulling = chkEnableCulling.IsChecked == true;
        if (int.TryParse(txtCullingMargin.Text, out var margin))
            graphArea.ViewportCulling.CullingMargin = margin;
    }

    private void OnCachingChanged(object? sender, RoutedEventArgs e)
    {
        var enabled = chkEnableCaching.IsChecked == true;
    }

    private void OnLodChanged(object? sender, RoutedEventArgs e)
    {
        graphArea.LodSettings.IsEnabled = chkEnableLod.IsChecked == true;
    }

    private void OnSliderChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(Slider.Value)) return;
        if (graphArea?.LodSettings == null) return;

        graphArea.LodSettings.HideArrowsZoomThreshold = sliderArrowThreshold.Value;
        graphArea.LodSettings.HideEdgeLabelsZoomThreshold = sliderLabelThreshold.Value;
        graphArea.LodSettings.HideVertexLabelsZoomThreshold = sliderLabelThreshold.Value;
    }
}