using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuikGraph;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.Models;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Models;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Rect = Westermo.GraphX.Measure.Rect;

namespace ShowcaseApp.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the GeneralGraph page demonstrating MVVM pattern with GraphX.
/// </summary>
public partial class GeneralGraphViewModel : ViewModelBase
{
    #region Fields

    private readonly LogicCoreExample _logicCore;
    private GraphAreaExample? _graphArea;

    #endregion

    #region Constructor

    public GeneralGraphViewModel()
    {
        _logicCore = new LogicCoreExample
        {
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER,
            EdgeCurvingEnabled = true
        };

        // Initialize collections for combo boxes
        LayoutAlgorithms = new ObservableCollection<LayoutAlgorithmTypeEnum>(
            Enum.GetValues<LayoutAlgorithmTypeEnum>());
        OverlapRemovalAlgorithms = new ObservableCollection<OverlapRemovalAlgorithmTypeEnum>(
            Enum.GetValues<OverlapRemovalAlgorithmTypeEnum>());
        EdgeRoutingAlgorithms = new ObservableCollection<EdgeRoutingAlgorithmTypeEnum>(
            Enum.GetValues<EdgeRoutingAlgorithmTypeEnum>());

        // Set default selections
        _selectedLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        _selectedOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
        _selectedEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
        _vertexCount = 30;

        // Initialize selected vertices set
        SelectedVertices = new HashSet<DataVertex>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the logic core for the graph area. Bind this to GraphArea.LogicCore.
    /// </summary>
    public IGXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> LogicCore => _logicCore;

    /// <summary>
    /// Gets the set of selected vertices. Bind this to GraphArea.SelectedVertices.
    /// </summary>
    public ISet<DataVertex> SelectedVertices { get; }

    /// <summary>
    /// Gets or sets the selection mode. Bind this to GraphArea.SelectionMode.
    /// </summary>
    [ObservableProperty] private SelectionMode _selectionMode = SelectionMode.Multiple;

    /// <summary>
    /// Gets the available layout algorithms for the combo box.
    /// </summary>
    public ObservableCollection<LayoutAlgorithmTypeEnum> LayoutAlgorithms { get; }

    /// <summary>
    /// Gets the available overlap removal algorithms for the combo box.
    /// </summary>
    public ObservableCollection<OverlapRemovalAlgorithmTypeEnum> OverlapRemovalAlgorithms { get; }

    /// <summary>
    /// Gets the available edge routing algorithms for the combo box.
    /// </summary>
    public ObservableCollection<EdgeRoutingAlgorithmTypeEnum> EdgeRoutingAlgorithms { get; }

    /// <summary>
    /// Gets or sets the selected layout algorithm.
    /// </summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(GenerateGraphCommand))]
    private LayoutAlgorithmTypeEnum _selectedLayoutAlgorithm;

    /// <summary>
    /// Gets or sets the selected overlap removal algorithm.
    /// </summary>
    [ObservableProperty] private OverlapRemovalAlgorithmTypeEnum _selectedOverlapRemovalAlgorithm;

    /// <summary>
    /// Gets or sets the selected edge routing algorithm.
    /// </summary>
    [ObservableProperty] private EdgeRoutingAlgorithmTypeEnum _selectedEdgeRoutingAlgorithm;

    /// <summary>
    /// Gets or sets the vertex count for graph generation.
    /// </summary>
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(GenerateGraphCommand))]
    private int _vertexCount;

    /// <summary>
    /// Gets or sets whether async computation is enabled.
    /// </summary>
    [ObservableProperty] private bool _isAsyncComputation;

    /// <summary>
    /// Gets or sets whether the loader overlay is visible.
    /// </summary>
    [ObservableProperty] private bool _isLoading;

    /// <summary>
    /// Gets or sets whether to use external layout algorithm.
    /// </summary>
    [ObservableProperty] private bool _useExternalLayoutAlgorithm;

    /// <summary>
    /// Gets or sets whether to use external overlap removal algorithm.
    /// </summary>
    [ObservableProperty] private bool _useExternalOverlapRemovalAlgorithm;

    /// <summary>
    /// Gets or sets whether to use external edge routing algorithm.
    /// </summary>
    [ObservableProperty] private bool _useExternalEdgeRoutingAlgorithm;

    /// <summary>
    /// Gets or sets whether zoom animation is enabled.
    /// </summary>
    [ObservableProperty] private bool _isZoomAnimationEnabled = true;

    /// <summary>
    /// Gets or sets the zoom step.
    /// </summary>
    [ObservableProperty] private double _zoomStep = 2.0;

    #endregion

    #region Methods

    /// <summary>
    /// Sets the reference to the GraphArea control. Called from the view's code-behind.
    /// </summary>
    /// <param name="graphArea">The GraphArea control instance.</param>
    public void SetGraphArea(GraphAreaExample graphArea)
    {
        _graphArea = graphArea;
        _graphArea.LogicCore = _logicCore;
        _graphArea.VertexLabelFactory = new DefaultVertexLabelFactory();
        _graphArea.SetEdgesDrag(true);
        _graphArea.ShowAllEdgesArrows();

        // Subscribe to events
        _graphArea.RelayoutFinished += OnRelayoutFinished;
        _graphArea.GenerateGraphFinished += OnGenerateGraphFinished;

        // Notify commands that their CanExecute state may have changed
        GenerateGraphCommand.NotifyCanExecuteChanged();
        RelayoutCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Applies the current algorithm settings to the logic core.
    /// </summary>
    private void ApplyAlgorithmSettings()
    {
        _logicCore.DefaultLayoutAlgorithm = SelectedLayoutAlgorithm;
        _logicCore.DefaultOverlapRemovalAlgorithm = SelectedOverlapRemovalAlgorithm;
        _logicCore.DefaultEdgeRoutingAlgorithm = SelectedEdgeRoutingAlgorithm;
        _logicCore.AsyncAlgorithmCompute = IsAsyncComputation;

        // Handle special algorithm configurations
        ConfigureLayoutAlgorithm();
        ConfigureOverlapRemovalAlgorithm();
        ConfigureEdgeRoutingAlgorithm();
    }

    private void ConfigureLayoutAlgorithm()
    {
        if (SelectedLayoutAlgorithm == LayoutAlgorithmTypeEnum.EfficientSugiyama)
        {
            if (_logicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.EfficientSugiyama)
                is EfficientSugiyamaLayoutParameters prms)
            {
                prms.EdgeRouting = SugiyamaEdgeRoutings.Orthogonal;
                prms.LayerDistance = prms.VertexDistance = 100;
                _logicCore.EdgeCurvingEnabled = false;
                _logicCore.DefaultLayoutAlgorithmParams = prms;
            }
        }
        else
        {
            _logicCore.EdgeCurvingEnabled = true;
        }

        _logicCore.DefaultLayoutAlgorithmParams = SelectedLayoutAlgorithm switch
        {
            LayoutAlgorithmTypeEnum.BoundedFR => _logicCore.AlgorithmFactory.CreateLayoutParameters(
                LayoutAlgorithmTypeEnum.BoundedFR),
            LayoutAlgorithmTypeEnum.FR => _logicCore.AlgorithmFactory.CreateLayoutParameters(
                LayoutAlgorithmTypeEnum.FR),
            // Configure KK algorithm with larger bounds to prevent tight node bundling
            LayoutAlgorithmTypeEnum.KK => CreateKKParameters(),
            _ => _logicCore.DefaultLayoutAlgorithmParams
        };
    }

    /// <summary>
    /// Creates KK layout parameters with appropriate bounds based on vertex count.
    /// </summary>
    private KKLayoutParameters CreateKKParameters()
    {
        // Scale the layout area based on vertex count to ensure adequate spacing
        // Use approximately 100 pixels per vertex for good spacing
        var dimension = Math.Max(1000, VertexCount * 40);
        return new KKLayoutParameters
        {
            Width = dimension,
            Height = dimension,
            MaxIterations = 500
        };
    }

    private void ConfigureOverlapRemovalAlgorithm()
    {
        switch (SelectedOverlapRemovalAlgorithm)
        {
            case OverlapRemovalAlgorithmTypeEnum.FSA:
            case OverlapRemovalAlgorithmTypeEnum.OneWayFSA:
                _logicCore.DefaultOverlapRemovalAlgorithmParams.HorizontalGap = 30;
                _logicCore.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 30;
                break;
        }
    }

    private void ConfigureEdgeRoutingAlgorithm()
    {
        if (SelectedEdgeRoutingAlgorithm == EdgeRoutingAlgorithmTypeEnum.Bundling)
        {
            var prm = new BundleEdgeRoutingParameters
            {
                Iterations = 200,
                SpringConstant = 5,
                Threshold = .1f
            };
            _logicCore.DefaultEdgeRoutingAlgorithmParams = prm;
            _logicCore.EdgeCurvingEnabled = true;
        }
        else
        {
            _logicCore.EdgeCurvingEnabled = false;
        }
    }

    private void AssignExternalLayoutAlgorithm(BidirectionalGraph<DataVertex, DataEdge> graph)
    {
        _logicCore.ExternalLayoutAlgorithm =
            _logicCore.AlgorithmFactory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.ISOM, graph);
    }

    #endregion

    #region Event Handlers

    private void OnRelayoutFinished(object? sender, EventArgs e)
    {
        IsLoading = false;
        RelayoutFinished?.Invoke(this, EventArgs.Empty);
    }

    private void OnGenerateGraphFinished(object? sender, EventArgs e)
    {
        if (_graphArea != null && !_graphArea.EdgesList.Any())
            _graphArea.GenerateAllEdges();

        IsLoading = false;
        GenerateGraphFinished?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when graph relayout is finished. Use this to trigger zoom to fill.
    /// </summary>
    public event EventHandler? RelayoutFinished;

    /// <summary>
    /// Raised when graph generation is finished. Use this to trigger zoom to fill.
    /// </summary>
    public event EventHandler? GenerateGraphFinished;

    /// <summary>
    /// Raised when save layout is requested. The view should show a save file dialog.
    /// </summary>
    public event EventHandler? SaveLayoutRequested;

    /// <summary>
    /// Raised when load layout is requested. The view should show an open file dialog.
    /// </summary>
    public event EventHandler? LoadLayoutRequested;

    /// <summary>
    /// Raised when export as image is requested. The view should show a save file dialog.
    /// </summary>
    public event EventHandler? ExportAsImageRequested;

    #endregion

    #region Commands

    /// <summary>
    /// Command to generate a random graph.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGenerateGraph))]
    private async Task GenerateGraph()
    {
        if (_graphArea == null) return;

        _graphArea.ClearLayout();
        ApplyAlgorithmSettings();

        var mult = SelectedLayoutAlgorithm switch
        {
            LayoutAlgorithmTypeEnum.LinLog => 45,
            _ => 25
        };

        var graph = ShowcaseHelper.GenerateDataGraph(VertexCount, true, mult);

        // Add self loop for demonstration
        if (graph.Vertices.Any())
            graph.AddEdge(new DataEdge(graph.Vertices.First(), graph.Vertices.First()));

        // Assign external algorithm if enabled
        if (UseExternalLayoutAlgorithm)
            AssignExternalLayoutAlgorithm(graph);

        if (IsAsyncComputation)
            IsLoading = true;

        await _graphArea.GenerateGraph(graph);

        if (IsAsyncComputation)
            IsLoading = false;
    }

    private bool CanGenerateGraph() => VertexCount > 0 && _graphArea != null;

    /// <summary>
    /// Command to relayout the current graph.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRelayout))]
    private async Task Relayout()
    {
        if (_graphArea == null) return;

        ApplyAlgorithmSettings();

        if (IsAsyncComputation)
            IsLoading = true;

        await _graphArea.RelayoutGraph(true);
        
        if (IsAsyncComputation)
            IsLoading = false;
    }

    private bool CanRelayout() => _graphArea?.LogicCore?.Graph != null;

    /// <summary>
    /// Command to save the graph layout. Raises an event for the view to handle file dialog.
    /// </summary>
    [RelayCommand]
    private void SaveLayout()
    {
        if (_graphArea == null) return;
        SaveLayoutRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Command to load a graph layout. Raises an event for the view to handle file dialog.
    /// </summary>
    [RelayCommand]
    private void LoadLayout()
    {
        if (_graphArea == null) return;
        LoadLayoutRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Command to save the graph state.
    /// </summary>
    [RelayCommand]
    private void SaveState()
    {
        _graphArea?.StateStorage.SaveState("gg_state_1");
    }

    /// <summary>
    /// Command to load the graph state.
    /// </summary>
    [RelayCommand]
    private void LoadState()
    {
        if (_graphArea?.StateStorage.ContainsState("gg_state_1") == true)
            _graphArea.StateStorage.LoadState("gg_state_1");
    }

    /// <summary>
    /// Command to export the graph as a PNG image. Raises an event for the view to handle file dialog.
    /// </summary>
    [RelayCommand]
    private void ExportAsImage()
    {
        if (_graphArea == null) return;
        ExportAsImageRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Property Change Handlers

    partial void OnSelectedLayoutAlgorithmChanged(LayoutAlgorithmTypeEnum value)
    {
        _logicCore.DefaultLayoutAlgorithm = value;
        ConfigureLayoutAlgorithm();

        // Reset edge routing for Sugiyama
        if (value == LayoutAlgorithmTypeEnum.EfficientSugiyama)
            SelectedEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
    }

    partial void OnSelectedOverlapRemovalAlgorithmChanged(OverlapRemovalAlgorithmTypeEnum value)
    {
        _logicCore.DefaultOverlapRemovalAlgorithm = value;
        ConfigureOverlapRemovalAlgorithm();
    }

    partial void OnSelectedEdgeRoutingAlgorithmChanged(EdgeRoutingAlgorithmTypeEnum value)
    {
        _logicCore.DefaultEdgeRoutingAlgorithm = value;
        ConfigureEdgeRoutingAlgorithm();
    }

    partial void OnIsAsyncComputationChanged(bool value)
    {
        _logicCore.AsyncAlgorithmCompute = value;
    }

    partial void OnUseExternalLayoutAlgorithmChanged(bool value)
    {
        if (value)
        {
            var graph = _logicCore.Graph ?? ShowcaseHelper.GenerateDataGraph(VertexCount);
            _logicCore.Graph = graph;
            AssignExternalLayoutAlgorithm(graph);
        }
        else
        {
            _logicCore.ExternalLayoutAlgorithm = null;
        }
    }

    partial void OnUseExternalOverlapRemovalAlgorithmChanged(bool value)
    {
        _logicCore.ExternalOverlapRemovalAlgorithm = value
            ? _logicCore.AlgorithmFactory.CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum.FSA, null)
            : null;
    }

    partial void OnUseExternalEdgeRoutingAlgorithmChanged(bool value)
    {
        if (value && _graphArea != null)
        {
            var graph = _logicCore.Graph ?? ShowcaseHelper.GenerateDataGraph(VertexCount);
            _logicCore.Graph = graph;
            _logicCore.ExternalEdgeRoutingAlgorithm =
                _logicCore.AlgorithmFactory.CreateEdgeRoutingAlgorithm(
                    EdgeRoutingAlgorithmTypeEnum.SimpleER,
                    new Rect(_graphArea.DesiredSize.ToGraphX()),
                    graph, null, null);
        }
        else
        {
            _logicCore.ExternalEdgeRoutingAlgorithm = null;
        }
    }

    #endregion
}