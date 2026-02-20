using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Behaviours;
using Westermo.GraphX.Controls.Controls.EdgeLabels;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Models;
using Westermo.GraphX.Controls.Models.Interfaces;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Generic graph visualization control that displays vertices and edges using a customizable layout algorithm.
/// </summary>
/// <typeparam name="TVertex">The type of vertex data objects. Must implement <see cref="IGraphXVertex"/>.</typeparam>
/// <typeparam name="TEdge">The type of edge data objects. Must implement <see cref="IGraphXEdge{TVertex}"/>.</typeparam>
/// <typeparam name="TGraph">The type of the graph data structure. Must be a mutable bidirectional graph.</typeparam>
/// <remarks>
/// GraphArea is the main visualization control for graphs. It provides:
/// - Automatic vertex and edge layout using configurable algorithms
/// - Support for vertex and edge selection
/// - Event handling for user interactions
/// - Integration with ZoomControl for pan/zoom functionality
/// - State storage for saving and restoring graph layouts
/// - Level-of-detail rendering for large graphs
/// - Custom control factories for vertices and edges
/// </remarks>
public class GraphArea<TVertex, TEdge, TGraph> : GraphAreaBase, IDisposable
    where TVertex : class, IGraphXVertex
    where TEdge : class, IGraphXEdge<TVertex>
    where TGraph : class, IMutableBidirectionalGraph<TVertex, TEdge>
{
    #region My properties

    static GraphArea()
    {
        LogicCoreProperty.Changed.AddClassHandler<GraphArea<TVertex, TEdge, TGraph>>(LogicCoreChanged);
    }

    /// <summary>
    /// Gets or sets vertex label control factory. Vertex labels will be generated at the end of the graph generation process.
    /// </summary>
    public ILabelFactory<Control>? VertexLabelFactory { get; set; }

    /// <summary>
    /// Gets or sets edge label control factory. Edge labels will be generated at the end of the graph generation process.
    /// </summary>
    public ILabelFactory<Control>? EdgeLabelFactory { get; set; }

    /// <summary>
    /// Gets or sets in which order GraphX controls are drawn
    /// </summary>
    public ControlDrawOrder ControlsDrawOrder { get; set; }


    public static readonly StyledProperty<ISet<TVertex>?> SelectedVerticesProperty =
        AvaloniaProperty.Register<GraphAreaBase, ISet<TVertex>?>(
            nameof(SelectedVertices));

    /// <summary>
    /// Gets or sets the collection of currently selected vertex data objects.
    /// Bind to this property to track and control vertex selection state.
    /// </summary>
    public ISet<TVertex>? SelectedVertices
    {
        get => GetValue(SelectedVerticesProperty);
        set => SetValue(SelectedVerticesProperty, value);
    }

    public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<GraphAreaBase, SelectionMode>(
            nameof(SelectionMode), SelectionMode.Multiple);

    /// <summary>
    /// Gets or sets the vertex selection mode.
    /// <see cref="Avalonia.Controls.SelectionMode.Single"/> allows only one vertex to be selected at a time.
    /// <see cref="Avalonia.Controls.SelectionMode.Multiple"/> allows multiple vertices to be selected. Default is Multiple.
    /// </summary>
    public SelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly StyledProperty<IGXLogicCore<TVertex, TEdge, TGraph>?> LogicCoreProperty =
        AvaloniaProperty.Register<GraphAreaBase, IGXLogicCore<TVertex, TEdge, TGraph>?>(
            nameof(LogicCore));

    private static void LogicCoreChanged(GraphArea<TVertex, TEdge, TGraph> graph, AvaloniaPropertyChangedEventArgs args)
    {
        if (graph.Parent == null) return;
        Task.Run(graph.LogicCoreAction);
    }

    private CancellationTokenSource? _logicCoreReactionCts;

    private async Task LogicCoreAction()
    {
        try
        {
            if (_logicCoreReactionCts != null)
            {
                await _logicCoreReactionCts.CancelAsync();
                _logicCoreReactionCts.Dispose();
            }

            _logicCoreReactionCts = new CancellationTokenSource();
            await OnLogicCore(_logicCoreReactionCts.Token);
        }
        catch (OperationCanceledException)
        {
            //ignore
        }
        catch (Exception ex)
        {
            Debug.WriteLine("LogicCore reaction error: " + ex);
        }
    }

    private async Task OnLogicCore(CancellationToken cancellationToken)
    {
        switch (LogicCoreChangeAction)
        {
            case LogicCoreChangedAction.GenerateGraph:
            case LogicCoreChangedAction.GenerateGraphWithEdges:
                await GenerateGraph(cancellation: cancellationToken);
                break;
            case LogicCoreChangedAction.RelayoutGraph:
                await RelayoutGraph(cancellationToken);
                break;
            case LogicCoreChangedAction.RelayoutGraphWithEdges:
                await RelayoutGraph(true, cancellationToken);
                break;
            case LogicCoreChangedAction.None:
            default:
                break;
        }
    }

    /// <summary>
    /// Gets or sets GraphX logic core object that will drive this visual
    /// </summary>
    public IGXLogicCore<TVertex, TEdge, TGraph>? LogicCore
    {
        get => GetValue(LogicCoreProperty);
        set => SetValue(LogicCoreProperty, value);
    }

    /// <summary>
    /// Gets or sets control factory class that allows you to define what vertex and edge controls will be generated by GraphX
    /// </summary>
    public IGraphControlFactory ControlFactory { get; set; }

    /// <summary>
    /// Gets logic core unsafely converted to specified type
    /// </summary>
    /// <typeparam name="T">Logic core type</typeparam>
    public T GetLogicCore<T>()
    {
        return (T)LogicCore!;
    }

    /// <summary>
    /// Sets the logic core that controls layout algorithms and graph operations.
    /// </summary>
    /// <param name="core">The logic core instance to use.</param>
    public void SetLogicCore(IGXLogicCore<TVertex, TEdge, TGraph> core)
    {
        LogicCore = core;
    }

    /// <summary>
    /// Gets or sets if visual properties such as edge dash style or vertex shape should be automatically reapplied to visuals when graph is regenerated.
    /// True by default.
    /// </summary>
    public bool EnableVisualPropsRecovery { get; set; }

    /// <summary>
    /// Gets or sets if visual properties such as edge dash style or vertex shape should be automatically applied to newly added visuals which are added using AddVertex() or AddEdge() or similar methods.
    /// True by default.
    /// </summary>
    public bool EnableVisualPropsApply { get; set; }

    /// <summary>
    /// Link to LogicCore. Gets if edge routing is used.
    /// </summary>
    internal override bool IsEdgeRoutingEnabled => LogicCore is { IsEdgeRoutingEnabled: true };

    /// <summary>
    /// Link to LogicCore. Gets if parallel edges are enabled.
    /// </summary>
    internal override bool EnableParallelEdges => LogicCore is { EnableParallelEdges: true };

    /// <summary>
    /// Link to LogicCore. Gets if edge curving is used.
    /// </summary>
    internal override bool EdgeCurvingEnabled => LogicCore is { EdgeCurvingEnabled: true };

    /// <summary>
    /// Link to LogicCore. Gets edge curving tolerance.
    /// </summary>
    internal override double EdgeCurvingTolerance => LogicCore?.EdgeCurvingTolerance ?? 0;

    /// <summary>
    /// Add custom control for
    /// </summary>
    /// <param name="control"></param>
    public virtual void AddCustomChildControl(Control control)
    {
        if (!Children.Contains(control))
            Children.Add(control);
        SetX(control, 0);
        SetY(control, 0);
    }

    /// <summary>
    /// Inserts custom control into GraphArea
    /// </summary>
    /// <param name="index">Insertion index</param>
    /// <param name="control">Custom control</param>
    public virtual void InsertCustomChildControl(int index, Control control)
    {
        Children.Insert(index, control);
        SetX(control, 0);
        SetY(control, 0);
    }

    /// <summary>
    /// Remove custom control from GraphArea children.
    /// </summary>
    /// <param name="control">Custom control</param>
    public virtual void RemoveCustomChildControl(Control control)
    {
        Children.Remove(control);
    }

    /// <summary>
    /// Returns all child controls of specified type using optional condition predicate
    /// </summary>
    /// <typeparam name="T">Type of the child</typeparam>
    public IEnumerable<T> GetChildControls<T>(Func<T, bool>? condition = null)
    {
        return condition == null ? Children.OfType<T>() : Children.OfType<T>().Where(condition);
    }

    #region StateStorage

    private StateStorage<TVertex, TEdge, TGraph>? _stateStorage;

    /// <summary>
    /// Provides methods for saving and loading graph layout states
    /// </summary>
    public StateStorage<TVertex, TEdge, TGraph> StateStorage
    {
        get
        {
            if (_stateStorage == null && !IsDisposed)
                CreateNewStateStorage();
            return _stateStorage!;
        }
        private set => _stateStorage = value;
    }

    #endregion

    private readonly Dictionary<TEdge, EdgeControl> _edgesList = [];
    private readonly Dictionary<TVertex, VertexControl> _vertexList = [];

    /// <summary>
    /// Gets edge controls read only collection. To modify collection use AddEdge() RemoveEdge() methods.
    /// </summary>
    public IDictionary<TEdge, EdgeControl> EdgesList => _edgesList;

    /// <summary>
    /// Gets vertex controls read only collection. To modify collection use AddVertex() RemoveVertex() methods.
    /// </summary>
    public IDictionary<TVertex, VertexControl> VertexList => _vertexList;

    #endregion

    #region Level of Detail Implementation

    /// <inheritdoc/>
    protected override void ApplyEdgeLodSettings(bool showArrows, bool showLabels)
    {
        foreach (var edge in _edgesList.Values)
        {
            // Apply arrow visibility based on LOD
            edge.SetCurrentValue(EdgeControlBase.ShowArrowsProperty, showArrows);

            // Apply label visibility based on LOD
            foreach (var label in edge.EdgeLabelControls)
            {
                if (label is Control control)
                {
                    control.IsVisible = showLabels && label.ShowLabel;
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void ApplyVertexLodSettings(bool showLabels)
    {
        foreach (var vertex in _vertexList.Values)
        {
            if (vertex.VertexLabelControl is Control label)
            {
                label.IsVisible = showLabels && vertex.ShowLabel;
            }
        }
    }

    #endregion

    public GraphArea()
    {
        EnableVisualPropsRecovery = true;
        EnableVisualPropsApply = true;
        EdgeLabelFactory = new DefaultEdgeLabelFactory();

        VertexLabelFactory = new DefaultVertexLabelFactory();
        ControlFactory = new GraphControlFactory(this);
    }

    #region Edge & vertex controls operations

    /// <summary>
    /// Returns first vertex that is found under specified coordinates
    /// </summary>
    /// <param name="position">GraphArea coordinate space position</param>
    public override VertexControl? GetVertexControlAt(Point position)
    {
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        return VertexList.Values.FirstOrDefault(a =>
        {
            var pos = a.GetPosition();
            var rect = new Rect(pos.X, pos.Y, a.Bounds.Width, a.Bounds.Height);
            return rect.Contains(position);
        });
    }


    /// <summary>
    /// Returns all existing VertexControls added into the layout as new Array
    /// </summary>
    public override VertexControl[] GetAllVertexControls()
    {
        return [.. _vertexList.Values];
    }

    #region Remove controls

    /// <summary>
    /// Remove all vertices from layout. Optionally can remove vertices from data graph also.
    /// </summary>
    /// <param name="removeVerticesFromDataGraph">Also remove vertices from data graph if possible. Default value is False.</param>
    public void RemoveAllVertices(bool removeVerticesFromDataGraph = false)
    {
        var hasStorage = LogicCore?.AlgorithmStorage != null;
        foreach (var item in _vertexList)
        {
            RemoveVertexInternal(item.Key, false, removeVerticesFromDataGraph);
            if (hasStorage && (item.Key.SkipProcessing != ProcessingOptionEnum.Exclude ||
                               removeVerticesFromDataGraph))
                LogicCore!.AlgorithmStorage.RemoveSingleVertex(item.Key);
        }

        _vertexList.Clear();
    }

    /// <summary>
    /// Remove all edges from layout. Optionaly can remove edges from data graph also.
    /// </summary>
    /// <param name="removeEdgesFromDataGraph">Also remove edges from data graph if possible. Default value is False.</param>
    public void RemoveAllEdges(bool removeEdgesFromDataGraph = false)
    {
        var hasStorage = LogicCore?.AlgorithmStorage != null;
        foreach (var item in _edgesList)
        {
            if (hasStorage && (item.Key.SkipProcessing != ProcessingOptionEnum.Exclude || removeEdgesFromDataGraph))
                LogicCore!.AlgorithmStorage.RemoveSingleEdge(item.Key);
            RemoveEdgeInternal(item.Key, false, removeEdgesFromDataGraph);
        }

        _edgesList.Clear();
    }

    /// <summary>
    /// Remove vertex from layout
    /// </summary>
    /// <param name="vertexData">Vertex data object</param>
    /// <param name="removeVertexFromDataGraph">Also remove vertex from data graph if possible. Default value is False.</param>
    public void RemoveVertex(TVertex vertexData, bool removeVertexFromDataGraph = false)
    {
        RemoveVertexInternal(vertexData, true, removeVertexFromDataGraph);
        var hasStorage = LogicCore?.AlgorithmStorage != null &&
                         (vertexData.SkipProcessing != ProcessingOptionEnum.Exclude || removeVertexFromDataGraph);
        if (hasStorage) LogicCore!.AlgorithmStorage.RemoveSingleVertex(vertexData);
    }

    /// <summary>
    /// Remove vertex and all associated edges from the layout.
    /// </summary>
    /// <param name="vertexData">Vertex data object</param>
    /// <param name="eType">Edge types to remove</param>
    /// <param name="removeEdgesFromDataGraph">Also remove edges from data graph if possible. Default value is True.</param>
    /// <param name="removeVertexFromDataGraph">Also remove vertex from data graph if possible. Default value is True.</param>
    public void RemoveVertexAndEdges(TVertex vertexData, EdgesType eType = EdgesType.All,
        bool removeEdgesFromDataGraph = true, bool removeVertexFromDataGraph = true)
    {
        if (VertexList.TryGetValue(vertexData, out VertexControl? value))
        {
            // OPTIMIZATION: Avoid ToList() allocation by using foreach directly
            var relatedControls = GetRelatedControls(value, GraphControlType.Edge, eType);
            foreach (var a in relatedControls)
            {
                RemoveEdge((TEdge)((EdgeControl)a).Edge!, removeEdgesFromDataGraph);
            }
        }

        RemoveVertex(vertexData, removeVertexFromDataGraph);
    }

    private void RemoveVertexInternal(TVertex? vertexData, bool removeFromList,
        bool removeVertexFromDataGraph = false)
    {
        if (vertexData == null || !_vertexList.TryGetValue(vertexData, out VertexControl? ctrl)) return;
        if (removeFromList)
            _vertexList.Remove(vertexData);
        // Always perform control cleanup (selection state, labels, children removal)
        RemoveVertexInternal(ctrl, removeVertexFromDataGraph);
    }

    private void RemoveVertexInternal(VertexControl ctrl, bool removeVertexFromDataGraph = false)
    {
        // Remove from selection if selected
        if (ctrl.Vertex is TVertex vertex)
        {
            SelectedVertices?.Remove(vertex);
            ctrl.IsSelected = false;
        }

        if (ctrl.VertexLabelControl != null)
        {
            Children.Remove((Control)ctrl.VertexLabelControl);
            ctrl.DetachLabel();
        }

        Children.Remove(ctrl);
        if (removeVertexFromDataGraph && LogicCore?.Graph != null &&
            LogicCore.Graph.ContainsVertex((TVertex)ctrl.Vertex!))
            LogicCore.Graph.RemoveVertex((TVertex)ctrl.Vertex!);
        ctrl.Clean();
    }

    /// <summary>
    /// Remove edge from layout
    /// </summary>
    /// <param name="edgeData">Edge data object</param>
    /// <param name="removeEdgeFromDataGraph">Remove edge from data graph if possible. Default value is False.</param>
    public void RemoveEdge(TEdge edgeData, bool removeEdgeFromDataGraph = false)
    {
        RemoveEdgeInternal(edgeData, true, removeEdgeFromDataGraph);
        var hasStorage = LogicCore?.AlgorithmStorage != null &&
                         (edgeData.SkipProcessing != ProcessingOptionEnum.Exclude || removeEdgeFromDataGraph);
        if (hasStorage) LogicCore!.AlgorithmStorage.RemoveSingleEdge(edgeData);
    }

    private void RemoveEdgeInternal(TEdge? edgeData, bool removeFromList, bool removeEdgeFromDataGraph = false)
    {
        if (edgeData == null || !_edgesList.TryGetValue(edgeData, out EdgeControl? ctrl)) return;
        if (removeFromList)
            _edgesList.Remove(edgeData);
        else RemoveEdgeInternal(ctrl, removeEdgeFromDataGraph);
    }

    private void RemoveEdgeInternal(EdgeControlBase ctrl, bool removeEdgeFromDataGraph = false)
    {
        ctrl.DetachLabels();

        Children.Remove(ctrl);
        if (removeEdgeFromDataGraph && LogicCore?.Graph != null && ctrl.Edge is TEdge edge &&
            LogicCore.Graph.ContainsEdge(edge))
            LogicCore.Graph.RemoveEdge(edge);
        ctrl.Clean();
    }

    #endregion

    #region Add controls

    /// <summary>
    /// Add vertex to layout
    /// </summary>
    /// <param name="vertexData">Vertex data object</param>
    /// <param name="vertexControl">Vertex visual control object</param>
    /// <param name="generateLabel">Generate vertex label for this control using VertexLabelFactory</param>
    public void AddVertex(TVertex vertexData, VertexControl? vertexControl, bool generateLabel = false)
    {
        if (AutoAssignMissingDataId && vertexData.ID == -1)
            vertexData.ID = GetNextUniqueId(true);
        InternalAddVertex(vertexData, vertexControl);
        if (EnableVisualPropsApply && vertexControl != null)
            ReapplySingleVertexVisualProperties(vertexControl);
        if (generateLabel && VertexLabelFactory != null)
            GenerateVertexLabel(vertexControl!);
        var hasStorage = LogicCore?.AlgorithmStorage != null &&
                         vertexData.SkipProcessing != ProcessingOptionEnum.Exclude;
        if (!hasStorage) return;
        var pos = vertexControl!.GetPosition(true);
        LogicCore!.AlgorithmStorage.AddSingleVertex(vertexData, pos.ToGraphX(),
            new Rect(pos, new Size(vertexControl.Bounds.Width, vertexControl.Bounds.Width)).ToGraphX());
    }


    /// <summary>
    /// Usability extension method.
    /// Add data vertex to graph and vertex control to layout. LogicCore::Graph should be assigned or exception will be thrown.
    /// </summary>
    /// <param name="vertexData">Vertex data object</param>
    /// <param name="vertexControl">Vertex visual control object</param>
    /// <param name="generateLabel">Generate vertex label for this control using VertexLabelFactory</param>
    public void AddVertexAndData(TVertex vertexData, VertexControl vertexControl, bool generateLabel = false)
    {
        if (LogicCore?.Graph == null)
            throw new GX_InvalidDataException(
                "LogicCore or its graph hasn't been assigned. Can't add data vertex!");
        LogicCore.Graph.AddVertex(vertexData);
        AddVertex(vertexData, vertexControl, generateLabel);
    }

    protected void InternalAddVertex(TVertex? vertexData, VertexControl? vertexControl)
    {
        if (vertexControl == null || vertexData == null) return;
        vertexControl.RootArea = this;
        if (!_vertexList.TryAdd(vertexData, vertexControl))
            throw new GX_InvalidDataException(
                "AddVertex() -> Vertex with the same data has already been added to layout!");
        Children.Add(vertexControl);
    }

    /// <summary>
    /// Add an edge to layout. Edge is added into the end of the visual tree causing it to be rendered above all vertices.
    /// </summary>
    /// <param name="edgeData">Edge data object</param>
    /// <param name="edgeControl">Edge visual control</param>
    /// <param name="generateLabel">Generate edge label for this control using EdgeLabelFactory</param>
    public void AddEdge(TEdge edgeData, EdgeControl? edgeControl, bool generateLabel = false)
    {
        if (AutoAssignMissingDataId && edgeData.ID == -1)
            edgeData.ID = GetNextUniqueId(false);
        InternalAddEdge(edgeData, edgeControl);
        if (EnableVisualPropsApply && edgeControl != null)
            ReapplySingleEdgeVisualProperties(edgeControl);
        if (generateLabel && EdgeLabelFactory != null)
            GenerateEdgeLabel(edgeControl!);
    }

    /// <summary>
    /// Usability extension method.
    /// Add data edge to graph and edge to layout. LogicCore::Graph should be assigned or exception will be thrown.
    /// </summary>
    /// <param name="edgeData">Edge data object</param>
    /// <param name="edgeControl">Edge visual control</param>
    /// <param name="generateLabel">Generate edge label for this control using EdgeLabelFactory</param>
    public void AddEdgeAndData(TEdge edgeData, EdgeControl edgeControl, bool generateLabel = false)
    {
        if (LogicCore?.Graph == null)
            throw new GX_InvalidDataException("LogicCore or its graph hasn't been assigned. Can't add data edge!");
        LogicCore.Graph.AddEdge(edgeData);
        AddEdge(edgeData, edgeControl, generateLabel);
    }

    protected void InternalAddEdge(TEdge? edgeData, EdgeControl? edgeControl)
    {
        if (edgeControl == null || edgeData == null) return;
        if (_edgesList.ContainsKey(edgeData))
            throw new GX_InvalidDataException(
                "AddEdge() -> An edge with the same data has already been added to layout!");
        edgeControl.RootArea = this;
        _edgesList.Add(edgeData, edgeControl);
        Children.Add(edgeControl);
    }

    /// <summary>
    /// Insert an edge to layout at specified position. By default, edge is inserted into the begining of the visual tree causing it to be rendered below all of the vertices.
    /// </summary>
    /// <param name="edgeData">Edge data object</param>
    /// <param name="edgeControl">Edge visual control</param>
    /// <param name="num">Insert position</param>
    /// <param name="generateLabel">Generate edge label for this control using EdgeLabelFactory</param>
    public void InsertEdge(TEdge edgeData, EdgeControl? edgeControl, int num = 0, bool generateLabel = false)
    {
        if (AutoAssignMissingDataId && edgeData.ID == -1)
            edgeData.ID = GetNextUniqueId(false);
        InternalInsertEdge(edgeData, edgeControl, num);
        if (EnableVisualPropsApply && edgeControl != null)
            ReapplySingleEdgeVisualProperties(edgeControl);
        if (generateLabel && EdgeLabelFactory != null)
            GenerateEdgeLabel(edgeControl!);
    }

    /// <summary>
    /// Usability extension method.
    /// Insert an edge to layout at specified position and add data edge. By default, edge is inserted into the begining of the visual tree causing it to be rendered below all of the vertices.
    /// LogicCore::Graph should be assigned or exception will be thrown.
    /// </summary>
    /// <param name="edgeData">Edge data object</param>
    /// <param name="edgeControl">Edge visual control</param>
    /// <param name="num">Insert position</param>
    /// <param name="generateLabel">Generate edge label for this control using EdgeLabelFactory</param>
    public void InsertEdgeAndData(TEdge edgeData, EdgeControl edgeControl, int num = 0, bool generateLabel = false)
    {
        if (LogicCore?.Graph == null)
            throw new GX_InvalidDataException("LogicCore or its graph hasn't been assigned. Can't add data edge!");
        LogicCore.Graph.AddEdge(edgeData);
        InsertEdge(edgeData, edgeControl, num, generateLabel);
    }

    protected void InternalInsertEdge(TEdge? edgeData, EdgeControl? edgeControl, int num = 0)
    {
        if (edgeControl == null || edgeData == null) return;
        if (_edgesList.ContainsKey(edgeData))
            throw new GX_InvalidDataException("AddEdge() -> An edge with the same data has already been added!");
        edgeControl.RootArea = this;
        _edgesList.Add(edgeData, edgeControl);
        try
        {
            if (ControlsDrawOrder == ControlDrawOrder.VerticesOnTop || num != 0)
                Children.Insert(num, edgeControl);
            else Children.Add(edgeControl);
        }
        catch (Exception ex)
        {
            throw new GX_GeneralException(ex.Message + ". Probably you have an error in edge template.", ex);
        }
    }

    #endregion

    #endregion

    #region Automatic data ID storage and resolving

    private int _dataIdCounter = 1;
    private int _edgeDataIdCounter = 1;

    protected virtual int GetNextUniqueId(bool isVertex)
    {
        if (isVertex)
        {
            while (DataIdsCollection.Contains(_dataIdCounter))
            {
                _dataIdCounter++;
            }

            DataIdsCollection.Add(_dataIdCounter);
            return _dataIdCounter;
        }

        while (EdgeDataIdsCollection.Contains(_edgeDataIdCounter))
        {
            _edgeDataIdCounter++;
        }

        EdgeDataIdsCollection.Add(_edgeDataIdCounter);
        return _edgeDataIdCounter;
    }

    protected readonly HashSet<long> DataIdsCollection = [];
    protected readonly HashSet<long> EdgeDataIdsCollection = [];

    #endregion

    #region GenerateGraph

    #region Label factories

    protected virtual void GenerateVertexLabels()
    {
        if (VertexLabelFactory == null) return;
        // OPTIMIZATION: Collect items to remove first to avoid modifying collection during iteration
        var toRemove = ListPool<Control>.Rent();
        try
        {
            foreach (var child in Children)
            {
                if (child is IVertexLabelControl)
                    toRemove.Add(child);
            }

            foreach (var item in toRemove)
            {
                Children.Remove(item);
            }
        }
        finally
        {
            ListPool<Control>.Return(toRemove);
        }

        foreach (var kvp in VertexList)
        {
            GenerateVertexLabel(kvp.Value);
        }
    }

    protected virtual void GenerateVertexLabel(VertexControl vertexControl)
    {
        var labels = VertexLabelFactory!.CreateLabel(vertexControl);
        var uiElements = labels as Control[] ?? [.. labels];
        // OPTIMIZATION: Check interface implementation without LINQ
        foreach (var l in uiElements)
        {
            if (l is not IVertexLabelControl)
                throw new GX_InvalidDataException(
                    "Generated vertex label should implement IVertexLabelControl interface");
        }

        foreach (var l in uiElements)
        {
            if (_svVertexLabelShow == false || !IsVisible)
                l.IsVisible = false;
            AddCustomChildControl(l);
            l.Measure(new Size(double.MaxValue, double.MaxValue));
            ((IVertexLabelControl)l).UpdatePosition();
        }
    }

    protected virtual void GenerateEdgeLabels()
    {
        if (EdgeLabelFactory == null) return;
        // OPTIMIZATION: Collect items to remove first to avoid modifying collection during iteration
        var toRemove = ListPool<Control>.Rent();
        try
        {
            foreach (var child in Children)
            {
                if (child is IEdgeLabelControl)
                    toRemove.Add(child);
            }

            foreach (var item in toRemove)
            {
                Children.Remove(item);
            }
        }
        finally
        {
            ListPool<Control>.Return(toRemove);
        }

        foreach (var kvp in EdgesList)
        {
            GenerateEdgeLabel(kvp.Value);
        }
    }

    protected virtual void GenerateEdgeLabel(EdgeControl edgeControl)
    {
        var labels = EdgeLabelFactory!.CreateLabel(edgeControl);
        var uiElements = labels as Control[] ?? [.. labels];
        // OPTIMIZATION: Check interface implementation without LINQ
        foreach (var a in uiElements)
        {
            if (a is not IEdgeLabelControl)
                throw new GX_InvalidDataException("Generated edge label should implement IEdgeLabelControl interface");
        }

        uiElements.ForEach(l =>
        {
            AddCustomChildControl(l);
            l.Measure(new Size(double.MaxValue, double.MaxValue));
            ((IEdgeLabelControl)l).UpdatePosition();
        });
    }

    #endregion

    #region Sizes operations

    /// <summary>
    /// Get vertex control sizes
    /// </summary>
    public Dictionary<TVertex, Size> GetVertexSizes()
    {
        //measure if needed and get all vertex sizes
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var vertexSizes = new Dictionary<TVertex, Size>(_vertexList.Count(a =>
            ((IGraphXVertex)a.Value.Vertex!).SkipProcessing != ProcessingOptionEnum.Exclude));
        //go through the vertex presenters and get the actual layoutpositions
        foreach (var vc in VertexList.Where(vc =>
                     ((IGraphXVertex)vc.Value.Vertex!).SkipProcessing != ProcessingOptionEnum.Exclude))
        {
            vertexSizes[vc.Key] = new Size(vc.Value.DesiredSize.Width, vc.Value.DesiredSize.Height);
        }

        return vertexSizes;
    }


    public Dictionary<TVertex, Size> GetVertexSizesAndPositions(
        out IDictionary<TVertex, Point> vertexPositions)
    {
        //measure if needed and get all vertex sizes
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var count = _vertexList.Count(a =>
            ((IGraphXVertex)a.Value.Vertex!).SkipProcessing != ProcessingOptionEnum.Exclude);
        var vertexSizes = new Dictionary<TVertex, Size>(count);
        vertexPositions = new Dictionary<TVertex, Point>(count);
        //go through the vertex presenters and get the actual layoutpositions
        foreach (var vc in VertexList.Where(vc =>
                     ((IGraphXVertex)vc.Value.Vertex!).SkipProcessing != ProcessingOptionEnum.Exclude))
        {
            vertexSizes[vc.Key] = new Size(vc.Value.DesiredSize.Width, vc.Value.DesiredSize.Height);
            vertexPositions[vc.Key] = vc.Value.GetPosition();
        }

        return vertexSizes;
    }

    /// <summary>
    /// Returns all vertices positions list
    /// </summary>
    public Dictionary<TVertex, Point> GetVertexPositions()
    {
        return VertexList
            .Where(a => ((IGraphXVertex)a.Value.Vertex!).SkipProcessing != ProcessingOptionEnum.Exclude)
            .ToDictionary(vertex => vertex.Key, vertex => vertex.Value.GetPosition());
    }

    #endregion

    #region PreloadVertexes()

    /// <summary>
    /// For manual graph generation only!
    /// Generates visual objects for all vertices and edges w/o any algorithms. Objects are hidden by default. Optionally, sets vertex coordinates.
    /// If there is any edge routing algorithm needed then it should be set before the call to this method.
    /// </summary>
    /// <param name="positions">Optional vertex positions</param>
    /// <param name="showObjectsIfPosSpecified">If True, all objects will be made visible when positions are specified</param>
    /// <param name="autoresolveIds">Automatically assign unique Ids to data objects. Can be vital for different GraphX logic parts such as parallel edges.</param>
    public virtual void PreloadGraph(Dictionary<TVertex, Point>? positions = null,
        bool showObjectsIfPosSpecified = true, bool autoresolveIds = true)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        if (LogicCore.Graph == null)
            throw new GX_InvalidDataException("LogicCore.Graph -> Not initialized!");

        if (autoresolveIds)
            AutoresolveIds(false);

        PreloadVertexes();

        if (positions != null)
        {
            foreach (var item in positions)
            {
                if (VertexList.TryGetValue(item.Key, out var value))
                    value.SetPosition(item.Value);
                VertexList[item.Key].SetCurrentValue(PositioningCompleteProperty, true);
            }
        }

        UpdateLayout();
        RestoreAlgorithmStorage();
        GenerateAllEdges(positions != null, false);
    }

    /// <summary>
    /// Clears all visual objects and generates VertexControl objects from specified graph. All vertices are created hidden by default.
    /// This method can be used for custom external algorithm implementations or manual visual graph population.
    /// </summary>
    /// <param name="graph">Data graph, by default is null and uses LogicCore.Graph as the source</param>
    /// <param name="dataContextToDataItem">Sets DataContext property to vertex data item of the control</param>
    /// <param name="forceVisPropRecovery"></param>
    public virtual void PreloadVertexes(TGraph? graph = null, bool dataContextToDataItem = true,
        bool forceVisPropRecovery = false)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        if (graph == null && LogicCore.Graph == null)
            throw new GX_InvalidDataException("graph param empty and LogicCore.Graph -> Not initialized!");
        graph ??= LogicCore.Graph;

        //clear edge and vertex controls
        RemoveAllVertices();
        RemoveAllEdges();

        //preload vertex controls
        foreach (var it in graph.Vertices.Where(a => a.SkipProcessing != ProcessingOptionEnum.Exclude))
        {
            var vc = ControlFactory.CreateVertexControl(it);
            vc.DataContext = dataContextToDataItem ? it : null;
            vc.SetCurrentValue(PositioningCompleteProperty,
                false); // Style can make them invisible until positioning is complete (after layout positions are calculated)
            InternalAddVertex(it, vc);
        }

        GenerateVertexLabels();

        if (forceVisPropRecovery)
            ReapplyVertexVisualProperties();
        //assign graph
        LogicCore.Graph = graph;
    }

    #endregion

    #region RelayoutGraph()

    private CancellationTokenSource? _layoutCancellationSource;

    /// <summary>
    /// Gets or sets if visual graph should be updated if graph is filtered.
    /// Remove all visuals with no keys in data graph and add all visuals that has keys in data graph.
    /// Default value is True.
    /// </summary>
    public bool EnableVisualsRenewOnFiltering { get; set; } = true;

    protected virtual async ValueTask RelayoutGraph(CancellationToken cancellationToken)
    {
        Dictionary<TVertex, Size>? vertexSizes = null;
        IDictionary<TVertex, Point>? vertexPositions = null;
        IGXLogicCore<TVertex, TEdge, TGraph>? localLogicCore = null;

        await RunOnDispatcherThread(PreUpdate, cancellationToken);

        if (localLogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");

        if (vertexSizes is null || vertexPositions is null || !localLogicCore.GenerateAlgorithmStorage(
                vertexSizes.ToDictionary(s => s.Key, s => s.Value.ToGraphX()),
                vertexPositions.ToDictionary(s => s.Key, s => s.Value.ToGraphX())))
            return;

        //clear routing info
        localLogicCore.Graph.Edges.ForEach(a => a.RoutingPoints = null);

        var resultCoords = localLogicCore.Compute(cancellationToken);
        var t = Stopwatch.GetTimestamp();
        await RunOnDispatcherThread(Assign, cancellationToken);
        Debug.WriteLine("VIS: " + Stopwatch.GetElapsedTime(t));
        return;

        void Assign()
        {
            //setup vertex positions from result data
            foreach (var item in resultCoords)
            {
                if (!_vertexList.TryGetValue(item.Key, out var vc)) continue;

                SetFinalX(vc, item.Value.X);
                SetFinalY(vc, item.Value.Y);

                if (double.IsNaN(GetX(vc))) vc.SetPosition(item.Value.X, item.Value.Y, false);
                vc.SetCurrentValue(PositioningCompleteProperty,
                    true); // Style can show vertexes with layout positions assigned
            }

            SetCurrentValue(LogicCoreProperty, localLogicCore);
            UpdateLayout(); //update all changes
        }

        void PreUpdate()
        {
            if (LogicCore == null) return;

            //add missing visuals and remove old ones if graph is filtered to reflect filtering
            if (EnableVisualsRenewOnFiltering && (LogicCore.IsFiltered || LogicCore.IsFilterRemoved))
            {
                //remove edge if it has been removed from data graph
                _edgesList.Keys.ToList()
                    .ForEach(a =>
                    {
                        if (!LogicCore.Graph.Edges.Contains(a)) RemoveEdge(a);
                    });
                //remove vertex if it has been removed from data graph
                _vertexList.Keys.ToList()
                    .ForEach(a =>
                    {
                        if (!LogicCore.Graph.Vertices.Contains(a)) RemoveVertex(a);
                    });

                LogicCore.Graph.Vertices.ForEach(v =>
                {
                    if (!_vertexList.ContainsKey(v)) AddVertex(v, ControlFactory.CreateVertexControl(v));
                });

                LogicCore.Graph.Edges.ForEach(e =>
                {
                    if (_edgesList.ContainsKey(e)) return;
                    var source = _vertexList[e.Source];
                    var target = _vertexList[e.Target];
                    AddEdge(e, ControlFactory.CreateEdgeControl(source, target, e));
                });
            }

            UpdateLayout(); //update layout so we can get actual control sizes

            if (LogicCore.AreVertexSizesNeeded())
                vertexSizes = GetVertexSizesAndPositions(out vertexPositions);
            else
                vertexPositions = GetVertexPositions();

            localLogicCore = LogicCore;
        }
    }

    /// <summary>
    /// Relayout graph using the same vertexes
    /// </summary>
    /// <param name="generateAllEdges">Generate all available edges for graph</param>
    /// <param name="cancellationToken"></param>
    public override async ValueTask RelayoutGraph(bool generateAllEdges = false,
        CancellationToken cancellationToken = default)
    {
        LogicCore?.PushFilters();
        await RelayoutGraphMain(generateAllEdges, cancellation: cancellationToken);
    }

    protected virtual async ValueTask RelayoutGraphMain(bool generateAllEdges = false, bool standalone = true,
        CancellationToken cancellation = default)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        if (LogicCore.AsyncAlgorithmCompute)
        {
            // Launch _relayoutGraph on a background thread using the task thread pool
            await Task.Factory.StartNew(() => RelayoutGraph(cancellation), cancellation);
        }
        else
        {
            await RelayoutGraph(cancellation);
        }

        await FinishUpRelayoutGraph(generateAllEdges, standalone, cancellation);
    }

    protected virtual async ValueTask FinishUpRelayoutGraph(bool generateAllEdges, bool standalone,
        CancellationToken cancellationToken)
    {
        await RunOnDispatcherThread(() =>
        {
            if (generateAllEdges)
            {
                if (_edgesList.Count == 0)
                {
                    GenerateAllEdgesInternal();
                    if (EnableVisualPropsRecovery) ReapplyEdgeVisualProperties();
                }
                else UpdateAllEdges();
            }

            if (!standalone)
            {
                if (EnableVisualPropsRecovery) ReapplyVertexVisualProperties();
                OnGenerateGraphFinished();
            }
            else
            {
                OnRelayoutFinished();
            }
        }, cancellationToken);
    }

    #region WPF/METRO threading stuff

    private static async ValueTask RunOnDispatcherThread(Action action, CancellationToken cancellation)
    {
        var dispatcher = Dispatcher.UIThread;
        if (dispatcher.CheckAccess())
            action(); // On UI thread already, so make a direct call
        else await dispatcher.InvokeAsync(action, DispatcherPriority.Normal, cancellation);
    }


    private static void UnwrapAndRethrow(AggregateException ex)
    {
        if (ex.InnerExceptions.Count == 1)
        {
            var innerException = ex.InnerExceptions[0];
            innerException.PreserveStackTrace();
            throw innerException;
        }

        // It's truly an aggregate exception, so throw as is
        ex.PreserveStackTrace();
        throw ex;
    }

    #endregion

    #endregion

    /// <summary>
    /// Cancel all undergoing async calculations
    /// </summary>
    public void CancelRelayout()
    {
        _layoutCancellationSource?.Cancel();
        _layoutCancellationSource?.Dispose();
        _layoutCancellationSource = null;
    }

    /// <summary>
    /// Generate visual graph
    /// </summary>
    /// <param name="graph">Data graph</param>
    /// <param name="generateAllEdges">Generate all available edges for graph</param>
    /// <param name="dataContextToDataItem">Sets visual edge and vertex controls DataContext property to vertex data item of the control (Allows prop binding in xaml templates)</param>
    /// <param name="cancellation"></param>
    public virtual async ValueTask GenerateGraph(TGraph graph, bool generateAllEdges = true,
        bool dataContextToDataItem = true, CancellationToken cancellation = default)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized! (Is NULL)");
        if (LogicCore.Graph == null)
            throw new InvalidDataException(
                "GraphArea.GenerateGraph() -> LogicCore.Graph property is null while trying to generate graph!");
        LogicCore.PushFilters();
        if (AutoAssignMissingDataId)
            AutoresolveIds(false, graph);
        if (!LogicCore.IsCustomLayout)
            PreloadVertexes(graph, dataContextToDataItem);
        await RelayoutGraphMain(generateAllEdges, false, cancellation);
    }

    /// <summary>
    /// Generate visual graph using Graph property (it must be set before this method is called)
    /// </summary>
    /// <param name="generateAllEdges">Generate all available edges for graph</param>
    /// <param name="dataContextToDataItem">Sets visual edge and vertex controls DataContext property to vertex data item of the control (Allows prop binding in xaml templates)</param>
    /// <param name="cancellation"></param>
    public virtual async ValueTask GenerateGraph(bool generateAllEdges = true, bool dataContextToDataItem = true,
        CancellationToken cancellation = default)
    {
        await GenerateGraph(LogicCore!.Graph, generateAllEdges, dataContextToDataItem, cancellation);
    }

    public void AutoresolveEntitiesId()
    {
        AutoresolveIds(true);
    }

    protected virtual void AutoresolveIds(bool includeEdgeIds, TGraph? graph = null)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        graph ??= LogicCore.Graph;
        if (graph == null) return;

        DataIdsCollection.Clear();
        _dataIdCounter = 1;

        // First, rebuild data ID collection for all vertices and edges that already have assigned IDs.
        foreach (var item in graph.Vertices.Where(a => a.ID != -1))
        {
            var added = DataIdsCollection.Add(item.ID);
            Debug.Assert(added,
                $"Duplicate ID '{item.ID}' found while adding a vertex ID during rebuild of data ID collection.");
        }

        // Generate unique IDs for all vertices and edges that don't already have a unique ID.
        foreach (var item in graph.Vertices.Where(a => a.ID == -1))
            item.ID = GetNextUniqueId(true);
        if (includeEdgeIds)
            AutoresolveEdgeIds(graph);
    }

    protected virtual void AutoresolveEdgeIds(TGraph? graph = null)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        graph ??= LogicCore.Graph;
        if (graph == null) return;

        EdgeDataIdsCollection.Clear();
        _edgeDataIdCounter = 1;
        foreach (var item in graph.Edges.Where(a => a.ID != -1))
        {
            var added = EdgeDataIdsCollection.Add(item.ID);
            Debug.Assert(added,
                $"Duplicate ID '{item.ID}' found while adding an edge ID during rebuild of data ID collection.");
        }

        foreach (var item in graph.Edges.Where(a => a.ID == -1))
            item.ID = GetNextUniqueId(false);
    }

    #endregion


    #region Methods for EDGE and VERTEX properties set

    protected void ReapplyVertexVisualProperties()
    {
        foreach (var item in VertexList.Values)
            ReapplySingleVertexVisualProperties(item);
    }

    protected void ReapplySingleVertexVisualProperties(VertexControl item)
    {
        if (_svVerticesDragEnabled != null) DragBehaviour.SetIsDragEnabled(item, _svVerticesDragEnabled.Value);
        if (_svVerticesDragUpdateEdges != null)
            DragBehaviour.SetUpdateEdgesOnMove(item, _svVerticesDragUpdateEdges.Value);
        if (_svVertexShape != null) item.VertexShape = _svVertexShape.Value;
        if (_svVertexLabelShow != null) item.ShowLabel = _svVertexLabelShow.Value;
        CustomReapplySingleVertexVisualProperties(item);
    }

    protected virtual void CustomReapplySingleVertexVisualProperties(VertexControl item)
    {
    }

    protected void ReapplyEdgeVisualProperties()
    {
        foreach (var item in EdgesList.Values)
            ReapplySingleEdgeVisualProperties(item);
    }

    protected virtual void CustomReapplySingleEdgeVisualProperties(EdgeControl item)
    {
    }

    protected void ReapplySingleEdgeVisualProperties(EdgeControl item)
    {
        if (_edgesDragEnabled != null) DragBehaviour.SetIsDragEnabled(item, _edgesDragEnabled.Value);
        if (_svEdgeDashStyle != null) item.DashStyle = _svEdgeDashStyle.Value;
        if (_svShowEdgeArrows != null)
            item.SetCurrentValue(EdgeControlBase.ShowArrowsProperty, _svShowEdgeArrows.Value);
        CustomReapplySingleEdgeVisualProperties(item);
    }


    private EdgeDashStyle? _svEdgeDashStyle; // EdgeDashStyle.Solid;

    /// <summary>
    /// Sets all edges dash style
    /// </summary>
    /// <param name="style">Selected style</param>
    public void SetEdgesDashStyle(EdgeDashStyle style)
    {
        _svEdgeDashStyle = style;
        foreach (var item in EdgesList)
            item.Value.DashStyle = style;
    }

    private bool? _svShowEdgeArrows;

    /// <summary>
    /// Show or hide all edges arrows. Default value is True.
    /// </summary>
    /// <param name="isEnabled">Boolean value</param>
    public void ShowAllEdgesArrows(bool isEnabled = true)
    {
        _svShowEdgeArrows = isEnabled;
        foreach (var item in _edgesList.Values)
            item.ShowArrows = isEnabled;
        InvalidateMeasure();
    }

    //private bool? _svShowEdgeLabels;
    /// <summary>
    /// Show or hide all edges labels
    /// </summary>
    /// <param name="isEnabled">Boolean value</param>
    public void ShowAllEdgesLabels(bool isEnabled = true)
    {
        // Use SetCurrentValue to avoid breaking any existing bindings on the ShowLabel styled property.
        foreach (var label in _edgesList.Values.SelectMany(item => item.EdgeLabelControls))
            label.SetCurrentValue(EdgeLabelControl.ShowLabelProperty, isEnabled);

        InvalidateVisual();
    }

    private bool? _svVertexLabelShow;

    /// <summary>
    /// Show or hide all vertex labels
    /// </summary>
    /// <param name="isEnabled">Boolean value</param>
    public void ShowAllVerticesLabels(bool isEnabled = true)
    {
        _svVertexLabelShow = isEnabled;
        foreach (var item in _vertexList.Values)
            item.SetCurrentValue(VertexControlBase.ShowLabelProperty, isEnabled);
        InvalidateVisual();
    }

    //private bool? _svAlignEdgeLabels;
    /// <summary>
    /// Aligns all labels with edges or displays them horizontaly
    /// </summary>
    /// <param name="isEnabled">Boolean value</param>
    public void AlignAllEdgesLabels(bool isEnabled = true)
    {
        foreach (var item in _edgesList.Values)
            item.EdgeLabelControls.ForEach(l => l.AlignToEdge = isEnabled);
        InvalidateVisual();
    }

    private bool? _svVerticesDragEnabled;
    private bool? _svVerticesDragUpdateEdges;

    /// <summary>
    /// Sets drag mode for all vertices
    /// </summary>
    /// <param name="isEnabled">Is drag mode enabled</param>
    /// <param name="updateEdgesOnMove">Is edges update enabled while dragging (use this if you have edge routing algorithms enabled)</param>
    public void SetVerticesDrag(bool isEnabled, bool updateEdgesOnMove = false)
    {
        _svVerticesDragEnabled = isEnabled;
        _svVerticesDragUpdateEdges = updateEdgesOnMove;

        foreach (var item in VertexList)
        {
            DragBehaviour.SetIsDragEnabled(item.Value, isEnabled);
            DragBehaviour.SetUpdateEdgesOnMove(item.Value, updateEdgesOnMove);
        }
    }

    private bool? _edgesDragEnabled;

    /// <summary>
    /// Sets drag mode for all edges
    /// </summary>
    /// <param name="isEnabled">Is drag mode enabled</param>
    public void SetEdgesDrag(bool isEnabled)
    {
        _edgesDragEnabled = isEnabled;

        foreach (var item in EdgesList)
        {
            DragBehaviour.SetIsDragEnabled(item.Value, isEnabled);
        }
    }

    private VertexShape? _svVertexShape; // = VertexShape.Rectangle;

    /// <summary>
    /// Sets math shape for all vertices
    /// </summary>
    /// <param name="shape">Selected math shape</param>
    public void SetVerticesMathShape(VertexShape shape)
    {
        _svVertexShape = shape;
        foreach (var item in VertexList)
            item.Value.SetCurrentValue(VertexControlBase.VertexShapeProperty, shape);
    }

    #endregion

    #region Generate Edges (ForVertex, All ... and stuff)

    #region ComputeEdgeRoutesByVertex()

    /// <summary>
    /// Compute new edge routes for all edges of the vertex
    /// </summary>
    /// <param name="vc">Vertex visual control</param>
    /// <param name="vertexDataNeedUpdate">If vertex data inside edge routing algorthm needs to be updated</param>
    internal override void ComputeEdgeRoutesByVertex(VertexControl vc, bool vertexDataNeedUpdate = true)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore is not initialized!");
        LogicCore.ComputeEdgeRoutesByVertex((TVertex)vc.Vertex!,
            vertexDataNeedUpdate ? vc.GetPosition().ToGraphX() : null,
            vertexDataNeedUpdate ? new Size(vc.Bounds.Width, vc.Bounds.Height).ToGraphX() : null);
    }

    #endregion

    #region GenerateAllEdges()

    protected virtual void GenerateAllEdgesInternal(bool isVisibleByDefault = true)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        RemoveAllEdges();

        AutoresolveEdgeIds();

        foreach (var item in LogicCore.Graph.Edges)
        {
            if (item.Source == null || item.Target == null) continue;
            if (!_vertexList.ContainsKey(item.Source) ||
                !_vertexList.TryGetValue(item.Target, out var value)) continue;
            var edgectrl = ControlFactory.CreateEdgeControl(_vertexList[item.Source], value,
                item, _svShowEdgeArrows ?? true, isVisibleByDefault);
            InternalInsertEdge(item, edgectrl);
            //setup path
        }

        if (LogicCore.EnableParallelEdges)
            UpdateParallelEdgesData();

        // Single layout update for all edges
        InvalidateMeasure();

        GenerateEdgeLabels();
    }

    /// <summary>
    /// Generates all possible valid edges for Graph vertexes
    /// </summary>
    /// <param name="isVisibleByDefault">true</param>
    /// <param name="updateLayout">Ensures that layout is properly updated before edges calculation. If you are sure that it is already updated you can set this param to False to increase performance. </param>
    public virtual void GenerateAllEdges(bool isVisibleByDefault = true,
        bool updateLayout = true)
    {
        if (updateLayout) UpdateLayout();
        GenerateAllEdgesInternal(isVisibleByDefault);
    }

    /// <summary>
    /// Update parallel edges information. Only needed to be run when edges has been added manualy and has to track parallel ones.
    /// Essentialy refreshes EdgeControl::IsParallel property
    /// </summary>
    /// <param name="edgeList">Optonal parameter. Specifies initial list of edges. If null then all edges are parsed. Default value is Null.</param>
    public virtual void UpdateParallelEdgesData(Dictionary<TEdge, EdgeControl>? edgeList = null)
    {
        edgeList ??= _edgesList;

        // Clear IsParallel flag - optimized: avoid LINQ allocation
        foreach (var edge in edgeList.Values)
        {
            edge.IsParallel = false;
        }

        // OPTIMIZATION: Use pooled dictionary to reduce allocations
        var edgeGroups = DictionaryPool<(long, long), List<KeyValuePair<TEdge, EdgeControl>>>.Rent();
        var rentedLists = ListPool<List<KeyValuePair<TEdge, EdgeControl>>>.Rent();

        try
        {
            foreach (var edge in edgeList)
            {
                // Skip edges that can't be parallel
                if (!edge.Value.CanBeParallel || edge.Key.IsSelfLoop) continue;
                if (edge.Key is { SourceConnectionPointId: not null, TargetConnectionPointId: not null }) continue;

                // Create a normalized key (smaller ID first)
                var sourceId = edge.Key.Source.ID;
                var targetId = edge.Key.Target.ID;
                var key = sourceId < targetId ? (sourceId, targetId) : (targetId, sourceId);

                if (!edgeGroups.TryGetValue(key, out var group))
                {
                    group = ListPool<KeyValuePair<TEdge, EdgeControl>>.Rent();
                    rentedLists.Add(group);
                    edgeGroups[key] = group;
                }

                group.Add(edge);
            }

            foreach (var groupKvp in edgeGroups)
            {
                var list = groupKvp.Value;
                if (list.Count <= 1) continue; // Single edges don't need parallel processing

                // Sort in place: edges with connection points go to the end
                list.Sort((a, b) =>
                {
                    var aHasCp = a.Key.SourceConnectionPointId.HasValue || a.Key.TargetConnectionPointId.HasValue;
                    var bHasCp = b.Key.SourceConnectionPointId.HasValue || b.Key.TargetConnectionPointId.HasValue;
                    return aHasCp.CompareTo(bHasCp);
                });

                var first = list[0];

                // Alternate sides with each step
                var viceversa = 1;

                // Count edges without connection points (optimized: avoid TakeWhile/Count)
                var countWithoutCp = 0;
                foreach (var e in list)
                {
                    if (e.Key.SourceConnectionPointId.HasValue || e.Key.TargetConnectionPointId.HasValue)
                        break;
                    countWithoutCp++;
                }

                var even = countWithoutCp % 2 == 0;

                // For even numbers of edges, initial offset is a half step from the center
                var initialOffset = even ? LogicCore!.ParallelEdgeDistance / 2 : 0;

                for (var i = 0; i < list.Count; i++)
                {
                    var kvp = list[i];
                    kvp.Value.IsParallel = true;

                    var offset = viceversa *
                                 (initialOffset + LogicCore!.ParallelEdgeDistance * ((i + (even ? 0 : 1)) / 2));
                    //if source to target edge
                    if (kvp.Key.Source == first.Key.Source)
                    {
                        kvp.Value.ParallelEdgeOffset = offset;
                    }
                    else //if target to source edge - just switch offsets
                    {
                        kvp.Value.ParallelEdgeOffset = -offset;
                    }

                    //change trigger to opposite
                    viceversa = -viceversa;
                }
            }
        }
        finally
        {
            // Return all pooled lists
            foreach (var list in rentedLists)
            {
                ListPool<KeyValuePair<TEdge, EdgeControl>>.Return(list);
            }

            ListPool<List<KeyValuePair<TEdge, EdgeControl>>>.Return(rentedLists);
            DictionaryPool<(long, long), List<KeyValuePair<TEdge, EdgeControl>>>.Return(edgeGroups);
        }
    }

    #endregion

    #region GenerateEdgesForVertex()

    /// <summary>
    /// Generates and displays edges for specified vertex
    /// </summary>
    /// <param name="vc">Vertex control</param>
    /// <param name="edgeType">Type of edges to display</param>
    /// <param name="isVisibleByDefault">Default edge visibility on layout</param>
    public override void GenerateEdgesForVertex(VertexControl vc, EdgesType edgeType,
        bool isVisibleByDefault = true)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        RemoveAllEdges();

        TEdge[]? inlist = null;
        TEdge[]? outlist = null;
        switch (edgeType)
        {
            case EdgesType.Out:
                outlist = [.. LogicCore.Graph.OutEdges((TVertex)vc.Vertex!)];
                break;
            case EdgesType.In:
                inlist = [.. LogicCore.Graph.InEdges((TVertex)vc.Vertex!)];
                break;
            default:
                outlist = [.. LogicCore.Graph.OutEdges((TVertex)vc.Vertex!)];
                inlist = [.. LogicCore.Graph.InEdges((TVertex)vc.Vertex!)];
                break;
        }

        var gotSelfLoop = false;
        if (inlist != null)
            foreach (var item in inlist)
            {
                if (gotSelfLoop) continue;
                var ctrl = ControlFactory.CreateEdgeControl(_vertexList[item.Source], vc, item,
                    _svShowEdgeArrows ?? true,
                    isVisibleByDefault);
                InsertEdge(item, ctrl);
                ctrl.PrepareEdgePath();
                if (item.Source == item.Target) gotSelfLoop = true;
            }

        if (outlist != null)
            foreach (var item in outlist)
            {
                if (gotSelfLoop) continue;
                var ctrl = ControlFactory.CreateEdgeControl(vc, _vertexList[item.Target], item,
                    _svShowEdgeArrows ?? true,
                    isVisibleByDefault);
                InsertEdge(item, ctrl);
                ctrl.PrepareEdgePath();
                if (item.Source == item.Target) gotSelfLoop = true;
            }
    }

    #endregion

    /// <summary>
    /// Update visual appearance for all possible visual edges
    /// </summary>
    /// <param name="performFullUpdate">If True - perform full edge update including all children checks such as pointers & labels. If False - update only edge routing and edge visual</param>
    /// <param name="skipHiddenEdges">If True - skip updating edges that are not visible (useful with viewport culling). Default is true.</param>
    public virtual void UpdateAllEdges(bool performFullUpdate = false, bool skipHiddenEdges = true)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");

        if (LogicCore.EnableParallelEdges)
            UpdateParallelEdgesData();


        try
        {
            foreach (var ec in _edgesList.Values)
            {
                // OPTIMIZATION: Skip hidden edges when culling is enabled
                if (skipHiddenEdges && !ec.IsVisible)
                    continue;
                ec.InvalidateMeasure();
            }
        }
        finally
        {
            // Single layout update for all edges at once
            InvalidateMeasure();
        }
    }

    #region Batch Update Support

    /// <summary>
    /// Begins a batch update scope where multiple edge updates are batched into a single layout pass.
    /// Use this when updating many edges at once to improve performance.
    /// </summary>
    /// <returns>A disposable scope that triggers layout when disposed.</returns>
    /// <example>
    /// <code>
    /// using (graphArea.BeginBatchUpdate())
    /// {
    ///     foreach (var edge in edges)
    ///         edge.UpdateEdge();
    /// }
    /// </code>
    /// </example>
    public BatchUpdateScope BeginBatchUpdate()
    {
        return new BatchUpdateScope(this, [.. _edgesList.Values]);
    }

    /// <summary>
    /// Begins a deferred position update scope where vertex position changes don't trigger immediate edge updates.
    /// Use this when moving many vertices at once to avoid redundant edge recalculations.
    /// </summary>
    /// <returns>A disposable scope that triggers a single edge update when disposed.</returns>
    public DeferredPositionUpdateScope BeginDeferredPositionUpdate()
    {
        return new DeferredPositionUpdateScope(
            [.. _vertexList.Values],
            () => UpdateAllEdges());
    }

    #endregion

    #endregion

    #region GetRelatedControls

    public override List<IGraphControl> GetRelatedVertexControls(IGraphControl ctrl,
        EdgesType edgesType = EdgesType.All)
    {
        if (ctrl == null)
            throw new GX_InvalidDataException("Supplied ctrl value is null!");
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        if (LogicCore.Graph == null)
        {
            throw new InvalidOperationException(
                "LogicCore.Graph property not set while using GetRelatedVertexControls method!");
        }

        var list = new List<IGraphControl>();
        if (ctrl is VertexControl vc)
        {
            var vData = (TVertex)vc.Vertex!;
            var vList = new List<TVertex>();
            switch (edgesType)
            {
                case EdgesType.All:
                    vList = [.. LogicCore.Graph.GetNeighbours(vData)];
                    break;
                case EdgesType.In:
                    vList = [.. LogicCore.Graph.GetInNeighbours(vData)];
                    break;
                case EdgesType.Out:
                    vList = [.. LogicCore.Graph.GetOutNeighbours(vData)];
                    break;
            }

            list.AddRange(VertexList.Where(a => vList.Contains(a.Key)).Select(a => a.Value));
        }

        if (ctrl is not EdgeControl ec) return list;

        var edge = (TEdge)ec.Edge!;
        if (edge.Target != null && _vertexList.TryGetValue(edge.Target, out var value))
        {
            list.Add(value);
        }

        if (edge.Source != null && _vertexList.TryGetValue(edge.Source, out var val))
        {
            list.Add(val);
        }

        return list;
    }

    public override List<IGraphControl> GetRelatedEdgeControls(IGraphControl ctrl,
        EdgesType edgesType = EdgesType.All)
    {
        if (ctrl == null)
            throw new GX_InvalidDataException("Supplied ctrl value is null!");
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        if (LogicCore.Graph == null)
        {
            throw new InvalidOperationException(
                "LogicCore.Graph property not set while using GetRelatedEdgeControls method!");
        }

        var list = new List<IGraphControl>();
        switch (ctrl)
        {
            case VertexControl vc:
            {
                var vData = (TVertex)vc.Vertex!;
                var eList = new List<TEdge>();
                switch (edgesType)
                {
                    case EdgesType.All:
                        eList = [.. LogicCore.Graph.GetAllEdges(vData)];
                        break;
                    case EdgesType.In:
                        eList = [.. LogicCore.Graph.GetInEdges(vData)];
                        break;
                    case EdgesType.Out:
                        eList = [.. LogicCore.Graph.GetOutEdges(vData)];
                        break;
                }

                list.AddRange(EdgesList.Where(a => eList.Contains(a.Key)).Select(a => a.Value));
                break;
            }
        }

        return list;
    }

    /// <summary>
    /// Get controls related to specified control
    /// </summary>
    /// <param name="ctrl">Original control</param>
    /// <param name="resultType">Type of resulting related controls</param>
    /// <param name="edgesType">Optional edge controls type</param>
    public override List<IGraphControl> GetRelatedControls(IGraphControl ctrl,
        GraphControlType resultType = GraphControlType.VertexAndEdge, EdgesType edgesType = EdgesType.Out)
    {
        if (ctrl == null)
            throw new GX_InvalidDataException("Supplied ctrl value is null!");
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");
        if (LogicCore.Graph == null)
        {
            throw new InvalidOperationException(
                "LogicCore.Graph property not set while using GetRelatedControls method!");
        }

        var list = new List<IGraphControl>();
        if (ctrl is VertexControl vc)
        {
            List<TEdge>? edgesInList = null;
            List<TEdge>? edgesOutList = null;
            if (edgesType is EdgesType.In or EdgesType.All)
            {
                LogicCore.Graph.TryGetInEdges((TVertex)vc.Vertex!, out var inEdges);
                edgesInList = inEdges?.ToList();
            }

            if (edgesType is EdgesType.Out or EdgesType.All)
            {
                LogicCore.Graph.TryGetOutEdges((TVertex)vc.Vertex!, out var outEdges);
                edgesOutList = outEdges?.ToList();
            }

            if (resultType is GraphControlType.Edge or GraphControlType.VertexAndEdge)
            {
                if (edgesInList != null)
                    list.AddRange(from item in edgesInList
                        where _edgesList.ContainsKey(item)
                        select _edgesList[item]);
                if (edgesOutList != null)
                    list.AddRange(from item in edgesOutList
                        where _edgesList.ContainsKey(item)
                        select _edgesList[item]);
            }

            if (resultType != GraphControlType.Vertex && resultType != GraphControlType.VertexAndEdge) return list;

            if (edgesInList != null)
                list.AddRange(from item in edgesInList
                    where _vertexList.ContainsKey(item.Source)
                    select _vertexList[item.Source]);
            if (edgesOutList != null)
                list.AddRange(from item in edgesOutList
                    where _vertexList.ContainsKey(item.Target)
                    select _vertexList[item.Target]);
            return list;
        }

        if (ctrl is not EdgeControl ec) return list;
        var edge = (TEdge)ec.Edge!;
        if (resultType == GraphControlType.Edge) return list;
        if (_vertexList.TryGetValue(edge.Target, out var value)) list.Add(value);
        if (_vertexList.TryGetValue(edge.Source, out var value1)) list.Add(value1);
        return list;
    }

    #endregion

    #region Serialization support

    /// <summary>
    /// Obtain graph layout data, which can then be used with a serializer.
    /// </summary>
    /// <exception cref="GX_InvalidDataException">Occurs when LogicCore or object Id isn't set</exception>
    public virtual List<GraphSerializationData> ExtractSerializationData()
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");

        if (AutoAssignMissingDataId)
            AutoresolveIds(true);

        var dlist = new List<GraphSerializationData>();
        foreach (var item in VertexList) //ALWAYS serialize vertices first
        {
            dlist.Add(new GraphSerializationData
            {
                Position = item.Value.GetPosition().ToGraphX(),
                Data = item.Key,
                IsVisible = item.Value.IsVisible,
                HasLabel = item.Value.VertexLabelControl != null
            });
            if (item.Key.ID == -1)
                throw new GX_InvalidDataException(
                    "ExtractSerializationData() -> All vertex datas must have positive unique ID!");
        }

        foreach (var item in EdgesList)
        {
            dlist.Add(new GraphSerializationData
            {
                Position = new Measure.Point(),
                Data = item.Key,
                IsVisible = item.Value.IsVisible,
                HasLabel = item.Value.EdgeLabelControls.Count > 0
            });
            if (item.Key.ID == -1)
                throw new GX_InvalidDataException(
                    "ExtractSerializationData() -> All edge datas must have positive unique ID!");
        }

        return dlist;
    }

    /// <summary>
    /// Rebuilds the graph layout from serialization data.
    /// </summary>
    /// <param name="data">The serialization data</param>
    /// <exception cref="GX_InvalidDataException">Occurs when LogicCore isn't set</exception>
    /// <exception cref="GX_SerializationException">Occurs when edge source or target isn't set</exception>
    public virtual void RebuildFromSerializationData(IEnumerable<GraphSerializationData> data)
    {
        if (LogicCore == null)
            throw new GX_InvalidDataException("LogicCore -> Not initialized!");

        RemoveAllEdges();
        RemoveAllVertices();

        if (LogicCore.Graph == null) LogicCore.Graph = Activator.CreateInstance<TGraph>();
        else LogicCore.Graph.Clear();

        var graphSerializationDatas = data as GraphSerializationData[] ?? [.. data];
        var vlist = graphSerializationDatas.Where(a => a.Data is TVertex);
        foreach (var item in vlist)
        {
            var vertexdata = (TVertex)item.Data;
            var ctrl = ControlFactory.CreateVertexControl(vertexdata);
            ctrl.IsVisible = item.IsVisible;
            ctrl.SetPosition(item.Position.X, item.Position.Y);
            AddVertex(vertexdata, ctrl);
            LogicCore.Graph.AddVertex(vertexdata);
            ctrl.ApplyTemplate();
            if (item.HasLabel)
                GenerateVertexLabel(ctrl);
        }

        var edgeList = graphSerializationDatas.Where(a => a.Data is TEdge);

        foreach (var item in edgeList)
        {
            var edgeData = (TEdge)item.Data;
            if (edgeData == null) continue;
            var sourceId = edgeData.Source.ID;
            var targetId = edgeData.Target.ID;
            var dataSource = _vertexList.Keys.FirstOrDefault(a => a.ID == sourceId);
            var dataTarget = _vertexList.Keys.FirstOrDefault(a => a.ID == targetId);

            edgeData.Source = dataSource!;
            edgeData.Target = dataTarget!;

            if (dataSource == null || dataTarget == null)
                throw new GX_SerializationException(
                    "DeserializeFromFile() -> Serialization logic is broken! Vertex not found. All vertices must be processed before edges!");
            var ecc = ControlFactory.CreateEdgeControl(_vertexList[dataSource], _vertexList[dataTarget], edgeData,
                true, item.IsVisible);
            InsertEdge(edgeData, ecc);
            LogicCore.Graph.AddEdge(edgeData);
            if (item.HasLabel)
                GenerateEdgeLabel(ecc);
        }

        if (AutoAssignMissingDataId)
            AutoresolveIds(true);

        //update edge layout and shapes manually
        //to correctly draw arrows in any case except they are manually disabled
        UpdateLayout();

        RestoreAlgorithmStorage();
    }

    private void RestoreAlgorithmStorage()
    {
        var vSizes = GetVertexSizesAndPositions(out var vPositions);
        LogicCore!.GenerateAlgorithmStorage(vSizes.ToDictionary(s => s.Key, s => s.Value.ToGraphX()),
            vPositions.ToDictionary(s => s.Key, s => s.Value.ToGraphX()));
    }

    #endregion

    #region Export and printing

    /// <summary>
    /// Export current graph layout into the PNG image file. layout will be saved in full size.
    /// </summary>
    public virtual async Task ExportAsPng()
    {
        await ExportAsImageDialog(ImageType.PNG);
    }

    /// <summary>
    /// Export current graph layout into the JPEG image file. layout will be saved in full size.
    /// </summary>
    /// <param name="quality">Optional image quality parameter</param>
    public virtual async Task ExportAsJpeg(int quality = 100)
    {
        await ExportAsImageDialog(ImageType.JPEG, true, PrintHelper.DEFAULT_DPI, quality);
    }

    /// <summary>
    /// Export current graph layout into the chosen image file and format. layout will be saved in full size.
    /// </summary>
    /// <param name="itype">Image format</param>
    /// <param name="useZoomControlSurface">Use zoom control parent surface to render bitmap (only visible zoom content will be exported)</param>
    /// <param name="dpi">Optional image DPI parameter</param>
    /// <param name="quality">Optional image quality parameter (for JPEG)</param>
    public virtual async Task ExportAsImageDialog(ImageType itype, bool useZoomControlSurface = false,
        double dpi = PrintHelper.DEFAULT_DPI, int quality = 100)
    {
        var fileType = itype.ToString();
        var fileExt = itype switch
        {
            ImageType.PNG => "*.png",
            ImageType.JPEG => "*.jpg",
            ImageType.BMP => "*.bmp",
            ImageType.GIF => "*.gif",
            ImageType.TIFF => "*.tiff",
            _ => throw new GX_InvalidDataException("ExportAsImage() -> Unknown output image format specified!"),
        };
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var dlg = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Exporting graph as {fileType} image...",
            DefaultExtension = fileExt,
            FileTypeChoices = [new FilePickerFileType(fileType) { Patterns = [fileExt] }]
        });
        if (dlg is null) return;

        ExportAsImage(dlg.Name, itype, useZoomControlSurface, dpi, quality);
    }

    public virtual void ExportAsImage(string filename, ImageType itype, bool useZoomControlSurface = false,
        double dpi = PrintHelper.DEFAULT_DPI, int quality = 100)
    {
        PrintHelper.ExportToImage(this, new Uri(filename, UriKind.Absolute), itype, useZoomControlSurface, dpi,
            quality);
    }

    private Size _oldSizeExpansion;

    /// <summary>
    /// Sets GraphArea into printing mode when its size will be recalculated on each measure and child controls will be arranged accordingly.
    /// Use with caution. Can spoil normal work while active but is essential to set before printing or grabbing an image.
    /// </summary>
    /// <param name="value">True or False</param>
    /// <param name="offsetControls">Offset child controls to fit into GraphArea control size</param>
    /// <param name="margin">Optional print area margin around the graph</param>
    public override void SetPrintMode(bool value, bool offsetControls = true, int margin = 0)
    {
        if (IsInPrintMode == value) return;
        IsInPrintMode = value;

        if (IsInPrintMode)
        {
            //set parent background
            if (Parent is ZoomControl.ZoomControl parent)
                Background = parent.Background;
            //set margin
            _oldSizeExpansion = SideExpansionSize;
            SideExpansionSize = margin == 0 ? new Size(0, 0) : new Size(margin, margin);
        }
        else
        {
            //reset margin
            SideExpansionSize = _oldSizeExpansion;
            //clear background
            if (Parent is ZoomControl.ZoomControl)
                Background = Brushes.Transparent;
        }

        if (offsetControls)
        {
            var offset = new Point(ContentSize.TopLeft.X - (margin == 0 ? 0 : margin * .5),
                ContentSize.TopLeft.Y - (margin == 0 ? 0 : margin * .5));

            foreach (var child in Children)
            {
                //skip edge controls
                if (child is EdgeControl) continue;
                //get current position
                var pos = new Point(GetX(child), GetY(child));
                //skip children with unset coordinates
                if (double.IsNaN(pos.X) || double.IsInfinity(pos.X)) continue;
                //adjust coordinates
                SetX(child, pos.X - (IsInPrintMode ? offset.X : -offset.X));
                SetY(child, pos.Y - (IsInPrintMode ? offset.Y : -offset.Y), true);
            }
        }

        InvalidateMeasure();
        UpdateLayout();
    }

    /// <summary>
    /// Print current visual graph layout as visible in ZoomControl (if wrapped in)
    /// </summary>
    /// <param name="description">Optional header description</param>
    public virtual void PrintVisibleAreaDialog(string description = "")
    {
        var vis = Parent switch
        {
            IZoomControl zoomControl => zoomControl.PresenterVisual,
            Control { Parent: IZoomControl control } => control.PresenterVisual,
            _ => this
        };

        PrintHelper.PrintVisualDialog(vis, description);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Gets if object has been disposed and can't be used anymore
    /// </summary>
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        CancelRelayout();
        _logicCoreReactionCts?.Dispose();
        _stateStorage?.Dispose();
        _layoutCancellationSource?.Dispose();
        IsDisposed = true;
        OnDispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Runs when base dispose is done
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// Clear graph visual layout (all edges, vertices and their states storage if any) and (optionally) LogicCore
    /// </summary>
    /// <param name="removeCustomObjects">Also remove any possible custom objects</param>
    /// <param name="clearStates">Also clear states storage (if you select clearLogicCore it will be cleaned anyway)</param>
    /// <param name="clearLogicCore">Also clear LogiCore data</param>
    public virtual void ClearLayout(bool removeCustomObjects = true, bool clearStates = false,
        bool clearLogicCore = false)
    {
        RemoveAllEdges();
        RemoveAllVertices();
        if (removeCustomObjects)
            Children.Clear();
        CreateNewStateStorage();

        if (clearLogicCore)
            LogicCore?.Clear();

        if (clearLogicCore || clearStates)
        {
            _stateStorage?.Dispose();
        }
    }

    protected virtual void CreateNewStateStorage()
    {
        _stateStorage = new StateStorage<TVertex, TEdge, TGraph>(this);
    }

    #endregion

    #region MoveTo routines

    /// <summary>
    /// Move specified visual entity on top of the visual tree
    /// </summary>
    /// <param name="control">Visual entity</param>
    /// <param name="moveLabels">Also move attached labels (if any)</param>
    public virtual void MoveToFront<T>(T control, bool moveLabels = true)
        where T : class
    {
        MoveTo(true, control, moveLabels);
    }

    /// <summary>
    /// Move specified visual entity to the bottom of the visual tree
    /// </summary>
    /// <param name="control">Visual entity</param>
    /// <param name="moveLabels">Also move attached labels (if any). Disable to decrease performance hit if you don't use labels.</param>
    public virtual void MoveToBack<T>(T control, bool moveLabels = true)
        where T : class
    {
        MoveTo(false, control, moveLabels);
    }

    /// <summary>
    /// Internal method to process MoveTo public methods
    /// </summary>
    /// <typeparam name="T">Control type</typeparam>
    /// <param name="toFront">If True - move to front overwise move to back</param>
    /// <param name="control">Control</param>
    /// <param name="moveLabels">Also move labels</param>
    protected virtual void MoveTo<T>(bool toFront, T control, bool moveLabels = true)
        where T : class
    {
        var result = (Control?)(object?)Children.OfType<T>().FirstOrDefault(a => a == control);
        if (result == null) return;
        if (!Children.Contains(result)) return;
        Children.Remove(result);
        if (toFront) Children.Add(result);
        else Children.Insert(0, result);
        if (!moveLabels) return;
        var vResult = result as VertexControl;
        var eResult = result as EdgeControl;
        Control? element = null;
        if (vResult?.VertexLabelControl != null)
            element = (Control)vResult.VertexLabelControl;
        if (eResult?.EdgeLabelControls.Count > 0)
        {
            eResult.EdgeLabelControls.ForEach(l =>
            {
                if (Children.Contains((Control)l))
                {
                    Children.Remove((Control)l);
                    if (toFront) Children.Add((Control)l);
                    else Children.Insert(0, (Control)l);
                }
            });
        }
        else if (element != null && Children.Contains(element))
        {
            Children.Remove(element);
            if (toFront) Children.Add(element);
            else Children.Insert(0, element);
        }
    }

    #endregion

    /// <summary>
    /// Synchronizes the IsSelected property on all vertex controls based on SelectedVertices collection.
    /// Call this method after programmatically modifying SelectedVertices to update visual states.
    /// </summary>
    public void SyncVertexSelectionState()
    {
        if (SelectedVertices is null)
        {
            // No selection tracking - clear all selection states
            foreach (var kvp in _vertexList)
            {
                kvp.Value.IsSelected = false;
            }

            return;
        }

        // Update IsSelected for each vertex control based on whether it's in the selection set
        foreach (var kvp in _vertexList)
        {
            kvp.Value.IsSelected = SelectedVertices.Contains(kvp.Key);
        }
    }

    internal override void OnVertexSelected(VertexControl vc, PointerEventArgs e, KeyModifiers keys)
    {
        base.OnVertexSelected(vc, e, keys);
        if (SelectedVertices is null) return;
        switch (SelectionMode)
        {
            case SelectionMode.Single:
            {
                // Single selection mode: always replace selection with the clicked vertex
                if (vc.Vertex is TVertex v)
                {
                    ClearSelectionState();
                    SelectedVertices.Clear();
                    SelectedVertices.Add(v);
                    vc.IsSelected = true;
                }

                break;
            }
            case SelectionMode.AlwaysSelected:
            {
                // AlwaysSelected mode: at least one item must remain selected
                // Clicking a vertex selects it; if already selected and it's the only one, keep it selected
                if (vc.Vertex is TVertex v)
                {
                    if (SelectedVertices.Contains(v) && SelectedVertices.Count == 1)
                    {
                        // Cannot deselect the only selected vertex - keep it selected
                        break;
                    }

                    ClearSelectionState();
                    SelectedVertices.Clear();
                    SelectedVertices.Add(v);
                    vc.IsSelected = true;
                }

                break;
            }
            case SelectionMode.Toggle:
            {
                // Toggle mode: clicking always toggles selection state without requiring modifier keys
                if (vc.Vertex is TVertex v)
                {
                    if (!SelectedVertices.Add(v))
                    {
                        SelectedVertices.Remove(v);
                        vc.IsSelected = false;
                    }
                    else
                    {
                        vc.IsSelected = true;
                    }
                }

                break;
            }
            default:
            case SelectionMode.Multiple:
            {
                // Multiple selection mode: requires modifier keys to extend/toggle selection
                if (vc.Vertex is not TVertex v) return;
                if (keys.HasFlag(KeyModifiers.Control))
                {
                    // Ctrl+Click: toggle selection of the clicked vertex
                    if (!SelectedVertices.Add(v))
                    {
                        SelectedVertices.Remove(v);
                        vc.IsSelected = false;
                    }
                    else
                    {
                        vc.IsSelected = true;
                    }
                }
                else if (keys.HasFlag(KeyModifiers.Shift))
                {
                    // Shift+Click: add to selection
                    SelectedVertices.Add(v);
                    vc.IsSelected = true;
                }
                else if (SelectedVertices.Contains(v))
                {
                    // Click on already selected vertex without modifiers: keep selection intact
                    // This allows dragging a group of selected vertices
                }
                else
                {
                    // Click on unselected vertex without modifiers: replace selection
                    ClearSelectionState();
                    SelectedVertices.Clear();
                    SelectedVertices.Add(v);
                    vc.IsSelected = true;
                }

                break;
            }
        }
    }

    /// <summary>
    /// Clears IsSelected on all vertex controls. Used internally when replacing selection.
    /// </summary>
    private void ClearSelectionState()
    {
        foreach (var kvp in _vertexList)
        {
            kvp.Value.IsSelected = false;
        }
    }
}