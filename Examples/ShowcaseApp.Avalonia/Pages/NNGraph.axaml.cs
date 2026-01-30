using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.Models;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Models;

namespace ShowcaseApp.Avalonia.Pages;

/// <summary>
/// Interaction logic for DynamicGraph.xaml
/// </summary>
public partial class NNGraph : UserControl, IDisposable
{
    private EditorOperationMode _opMode = EditorOperationMode.Select;
    private VertexControl? _ecFrom;
    private readonly EditorObjectManager _editorManager;

    public NNGraph()
    {
        InitializeComponent();
        _editorManager = new EditorObjectManager(graphArea, zoomCtrl);
        var dgLogic = new LogicCoreExample();
        graphArea.LogicCore = dgLogic;

        // Initialize selection tracking with Multiple selection mode
        graphArea.SelectedVertices = new HashSet<DataVertex>();
        graphArea.SelectionMode = SelectionMode.Multiple;

        graphArea.VertexSelected += graphArea_VertexSelected;
        graphArea.EdgeSelected += graphArea_EdgeSelected;
        graphArea.SetVerticesMathShape(VertexShape.Circle);

        dgLogic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Custom;
        dgLogic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
        dgLogic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
        dgLogic.EdgeCurvingEnabled = true;

        zoomCtrl.IsAnimationEnabled = false;
        zoomCtrl.Zoom = 2;
        zoomCtrl.MinZoom = .5;
        zoomCtrl.MaxZoom = 50;
        zoomCtrl.ZoomSensitivity = 25;
        zoomCtrl.PointerPressed += zoomCtrl_MouseDown;

        butDelete.IsCheckedChanged += ToolbarButton_Checked;
        butSelect.IsCheckedChanged += ToolbarButton_Checked;
        butEdit.IsCheckedChanged += ToolbarButton_Checked;

        butSelect.IsChecked = true;
    }

    private void graphArea_EdgeSelected(object sender, EdgeSelectedEventArgs args)
    {
        if (args.MouseArgs is not PointerEventArgs pea) return;
        if (args.EdgeControl.Edge is not DataEdge dataEdge) return;

        if (!pea.Properties.IsLeftButtonPressed || _opMode != EditorOperationMode.Delete) return;
        graphArea.LogicCore?.Graph.RemoveEdge(dataEdge);
        graphArea.RemoveEdge(dataEdge);
    }

    private void graphArea_VertexSelected(object sender, VertexSelectedEventArgs args)
    {
        if (args.MouseArgs is not PointerEventArgs pea) return;

        if (!pea.Properties.IsLeftButtonPressed) return;
        switch (_opMode)
        {
            case EditorOperationMode.Edit:
                CreateEdgeControl(args.VertexControl);
                break;
            case EditorOperationMode.Delete:
                SafeRemoveVertex(args.VertexControl);
                break;
            case EditorOperationMode.Select:
            default:
                // Selection is now handled automatically by GraphArea via SelectedVertices
                // The IsSelected property on VertexControl is set by the GraphArea
                // and DragBehaviour now uses IsSelected for group dragging
                break;
        }
    }


    private void zoomCtrl_MouseDown(object? sender, PointerPressedEventArgs e)
    {
        //create vertices and edges only in Edit mode
        if (!e.Properties.IsLeftButtonPressed) return;
        switch (_opMode)
        {
            case EditorOperationMode.Edit:
            {
                var pos = zoomCtrl.TranslatePoint(e.GetPosition(zoomCtrl), graphArea)!.Value.ToGraphX();
                pos.Offset(-22.5, -22.5);
                var vc = CreateVertexControl(pos.ToAvalonia());
                CreateEdgeControl(vc);
                break;
            }
            case EditorOperationMode.Select:
                ClearSelectMode(true);
                break;
            case EditorOperationMode.Delete:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ToolbarButton_Checked(object? sender, RoutedEventArgs e)
    {
        if (butDelete.IsChecked == true && ReferenceEquals(sender, butDelete))
        {
            butEdit.IsChecked = false;
            butSelect.IsChecked = false;
            zoomCtrl.Cursor = new Cursor(StandardCursorType.Help);
            _opMode = EditorOperationMode.Delete;
            ClearEditMode();
            ClearSelectMode();
            return;
        }

        if (butEdit.IsChecked == true && ReferenceEquals(sender, butEdit))
        {
            butDelete.IsChecked = false;
            butSelect.IsChecked = false;
            zoomCtrl.Cursor = new Cursor(StandardCursorType.Ibeam);
            _opMode = EditorOperationMode.Edit;
            ClearSelectMode();
            return;
        }

        if (butSelect.IsChecked != true || !ReferenceEquals(sender, butSelect)) return;
        butEdit.IsChecked = false;
        butDelete.IsChecked = false;
        zoomCtrl.Cursor = new Cursor(StandardCursorType.Hand);
        _opMode = EditorOperationMode.Select;
        ClearEditMode();
        graphArea.SetVerticesDrag(true, true);
    }

    private void ClearSelectMode(bool soft = false)
    {
        // Clear the selection using the new SelectedVertices mechanism
        graphArea.SelectedVertices?.Clear();
        graphArea.SyncVertexSelectionState();

        if (!soft)
        {
            graphArea.SetVerticesDrag(false);
        }
    }

    private void ClearEditMode()
    {
        _editorManager.DestroyVirtualEdge();
        _ecFrom = null;
    }

    private VertexControl CreateVertexControl(Point position)
    {
        var data = new DataVertex("Vertex " + (graphArea.VertexList.Count + 1))
            { ImageId = ShowcaseHelper.Rand.Next(0, ThemedDataStorage.EditorImages.Count) };
        graphArea.LogicCore?.Graph.AddVertex(data);
        var vc = new VertexControl(data);
        graphArea.AddVertex(data, vc);
        GraphAreaBase.SetX(vc, position.X);
        GraphAreaBase.SetY(vc, position.Y, true);
        return vc;
    }

    private void CreateEdgeControl(VertexControl vc)
    {
        if (_ecFrom == null)
        {
            _editorManager.CreateVirtualEdge(vc, vc.GetPosition());
            _ecFrom = vc;
            return;
        }

        if (_ecFrom == vc) return;
        if (_ecFrom.Vertex is not DataVertex from) return;
        if (vc.Vertex is not DataVertex to) return;
        if (graphArea.LogicCore == null) return;

        var data = new DataEdge(from, to);
        graphArea.LogicCore!.Graph.AddEdge(data);
        var ec = new EdgeControl(_ecFrom, vc, data);
        graphArea.InsertEdge(data, ec);
        _ecFrom = null;
        _editorManager.DestroyVirtualEdge();
    }

    private void SafeRemoveVertex(VertexControl vc, bool removeFromSelected = false)
    {
        //remove all adjacent edges
        foreach (var ec in graphArea.GetRelatedControls(vc, GraphControlType.Edge, EdgesType.All)
                     .OfType<EdgeControl>())
        {
            if (ec.Edge is not DataEdge dataEdge) continue;
            graphArea.LogicCore!.Graph.RemoveEdge(dataEdge);
            graphArea.RemoveEdge(dataEdge);
        }

        if (vc.Vertex is not DataVertex dataVertex) return;
        graphArea.LogicCore!.Graph.RemoveVertex(dataVertex);
        graphArea.RemoveVertex(dataVertex);
    }

    public void Dispose()
    {
        _editorManager.Dispose();
        if (graphArea != null)
            graphArea.Dispose();
    }
}