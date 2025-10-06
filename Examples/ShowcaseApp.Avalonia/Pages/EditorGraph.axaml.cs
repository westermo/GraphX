using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.Models;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls.Avalonia;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace ShowcaseApp.Avalonia.Pages;

/// <summary>
/// Interaction logic for DynamicGraph.xaml
/// </summary>
public partial class EditorGraph : UserControl, IDisposable
{
    private EditorOperationMode _opMode = EditorOperationMode.Select;
    private VertexControl? _ecFrom;
    private readonly EditorObjectManager _editorManager;

    public EditorGraph()
    {
        InitializeComponent();
        _editorManager = new EditorObjectManager(graphArea, zoomCtrl);
        var dgLogic = new LogicCoreExample();
        graphArea.LogicCore = dgLogic;
        graphArea.VertexSelected += graphArea_VertexSelected;
        graphArea.EdgeSelected += graphArea_EdgeSelected;
        graphArea.SetVerticesMathShape(VertexShape.Circle);
        graphArea.VertexLabelFactory = new DefaultVertexLabelFactory();

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
        if (pea.Properties.IsLeftButtonPressed && _opMode == EditorOperationMode.Delete)
            graphArea.RemoveEdge(dataEdge, true);
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
                if (_opMode == EditorOperationMode.Select && args.Modifiers.HasFlag(KeyModifiers.Control))
                    SelectVertex(args.VertexControl);
                break;
        }
    }

    private static void SelectVertex(Control vc)
    {
        DragBehaviour.SetIsTagged(vc, !DragBehaviour.GetIsTagged(vc));
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
            zoomCtrl.Cursor = new Cursor(StandardCursorType.Hand);
            _opMode = EditorOperationMode.Edit;
            ClearSelectMode();
            return;
        }

        if (butSelect.IsChecked == true && ReferenceEquals(sender, butSelect))
        {
            butEdit.IsChecked = false;
            butDelete.IsChecked = false;
            zoomCtrl.Cursor = new Cursor(StandardCursorType.Hand);
            _opMode = EditorOperationMode.Select;
            ClearEditMode();
            graphArea.SetVerticesDrag(true, true);
            graphArea.SetEdgesDrag(true);
        }
    }

    private void ClearSelectMode(bool soft = false)
    {
        graphArea.VertexList.Values
            .Where(DragBehaviour.GetIsTagged)
            .ToList()
            .ForEach(a => { DragBehaviour.SetIsTagged(a, false); });

        if (!soft)
            graphArea.SetVerticesDrag(false);
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
        var vc = new VertexControl(data);
        vc.SetPosition(position);
        graphArea.AddVertexAndData(data, vc, true);
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
        if (_ecFrom.Vertex is not DataVertex from || vc.Vertex is not DataVertex to) return;
        var data = new DataEdge(from, to);
        var ec = new EdgeControl(_ecFrom, vc, data);
        graphArea.InsertEdgeAndData(data, ec, 0, true);

        _ecFrom = null;
        _editorManager.DestroyVirtualEdge();
    }

    private void SafeRemoveVertex(VertexControl vc)
    {
        if (vc.Vertex is not DataVertex dv) return;
        //remove vertex and all adjacent edges from layout and data graph
        graphArea.RemoveVertexAndEdges(dv);
    }

    public void Dispose()
    {
        _editorManager.Dispose();
        graphArea?.Dispose();
    }
}