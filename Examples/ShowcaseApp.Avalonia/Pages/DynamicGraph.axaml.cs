using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.Models;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Behaviours;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Controls.Models;

namespace ShowcaseApp.Avalonia.Pages;

/// <summary>
/// Interaction logic for DynamicGraph.xaml
/// </summary>
public partial class DynamicGraph : UserControl
{
    private int _selIndex;

    public DynamicGraph()
    {
        InitializeComponent();
        var dgLogic = new LogicCoreExample();
        dg_Area.LogicCore = dgLogic;

        dg_addvertex.Click += AddVertex;
        dg_remvertex.Click += RemoveVertex;
        dg_addedge.Click += AddEdge;
        dg_remedge.Click += RemoveEdge;
        dgLogic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        dgLogic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
        dgLogic.DefaultOverlapRemovalAlgorithmParams.HorizontalGap = 50;
        dgLogic.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 50;

        dgLogic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
        dgLogic.EdgeCurvingEnabled = true;
        dgLogic.Graph = new GraphExample();
        dg_Area.VertexSelected += VertexSelected;
        dg_test.IsVisible = false;
        dg_zoomctrl.AreaSelected += ZoomAreaSelected;

        dg_dragsource.PointerPressed += DragSourcePressed;
        SetupDrop();

        dg_zoomctrl.IsAnimationEnabled = false;
        dg_Area.SetVerticesDrag(true, true);

        Loaded += DynamicGraph_Loaded;
        Unloaded += DynamicGraph_Unloaded;
    }

    private void SetupDrop()
    {
        AddHandler(DragDrop.DropEvent, DropVertex);
        AddHandler(DragDrop.DragEnterEvent, dg_Area_DragEnter);
    }


    private bool loaded;
    private Predicate<Control>? _originalGlobalIsSnapping;
    private Predicate<Control>? _originalGlobalIsSnappingIndividually;

    private void DynamicGraph_Loaded(object? sender, RoutedEventArgs e)
    {
        if (loaded)
            return;
        loaded = true;

        _originalGlobalIsSnapping = DragBehaviour.GlobalIsSnappingPredicate;
        _originalGlobalIsSnappingIndividually = DragBehaviour.GlobalIsIndividualSnappingPredicate;

        DragBehaviour.GlobalIsSnappingPredicate = IsSnapping;
        DragBehaviour.GlobalIsIndividualSnappingPredicate = IsSnappingIndividually;
    }

    private void DynamicGraph_Unloaded(object? sender, RoutedEventArgs e)
    {
        loaded = false;

        DragBehaviour.GlobalIsSnappingPredicate = _originalGlobalIsSnapping;
        DragBehaviour.GlobalIsIndividualSnappingPredicate = _originalGlobalIsSnappingIndividually;
    }

    private void FindRandom(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (!dg_Area.VertexList.Any()) return;
        _selIndex++;
        if (_selIndex >= dg_Area.VertexList.Count) _selIndex = 0;
        var vc = dg_Area.VertexList.ToList()[_selIndex].Value;

        const int offset = 100;
        var pos = vc.GetPosition();
        dg_zoomctrl.ZoomToContent(new Rect(pos.X - offset, pos.Y - offset, vc.Width + offset * 2,
            vc.Height + offset * 3));
    }

    private void ZoomAreaSelected(object sender, AreaSelectedEventArgs args)
    {
        var r = args.Rectangle;
        foreach (var item in from item in dg_Area.VertexList
                 let offset = item.Value.GetPosition()
                 let irect = new Rect(offset.X, offset.Y, item.Value.Width, item.Value.Width).ToGraphX()
                 where irect.IntersectsWith(r.ToGraphX())
                 select item)
        {
            SelectVertex(item.Value);
        }
    }

    #region Dragging example

    private void DragSourcePressed(object? sender,
        PointerPressedEventArgs pointerPressedEventArgs)
    {
        var data = new DataTransfer();
        DragDrop.DoDragDropAsync(pointerPressedEventArgs, data, DragDropEffects.Link);
    }

    private static void dg_Area_DragEnter(object? sender, DragEventArgs e)
    {
        //don't show drag effect if we are on drag source or don't have any item of needed type dragged
        if (sender == e.Source)
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void DropVertex(object? sender, DragEventArgs e)
    {
        //how to get dragged data by its type
        var pos = dg_zoomctrl.TranslatePoint(e.GetPosition(dg_zoomctrl), dg_Area);
        if (pos == null) return;
        var data = ThemedDataStorage.FillDataVertex(new DataVertex());
        var vc = new VertexControl(data);
        vc.SetPosition(pos.Value.X, pos.Value.Y);
        dg_Area.AddVertexAndData(data, vc);
    }

    #endregion

    private void VertexSelected(object sender, VertexSelectedEventArgs args)
    {
        if (args.MouseArgs is not PointerEventArgs pea) return;
        if (pea.Properties.IsLeftButtonPressed)
        {
            if (pea.KeyModifiers.HasFlag(KeyModifiers.Control))
                SelectVertex(args.VertexControl);
        }
        else if (pea.Properties.IsRightButtonPressed)
        {
            var countTagged = dg_Area.VertexList.Values.Count(DragBehaviour.GetIsTagged);
            var isTagged = DragBehaviour.GetIsTagged(args.VertexControl);
            args.VertexControl.ContextMenu = new ContextMenu();
            var mi = new MenuItem
            {
                Header = "Delete item" + (isTagged && countTagged > 1 ? "s" : ""), Tag = args.VertexControl,
                Margin = new Thickness(5)
            };
            mi.Click += mi_Click;
            args.VertexControl.ContextMenu.Items.Add(mi);
            args.VertexControl.ContextMenu.Open();
        }
    }

    private void mi_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        if (menuItem.Tag is not VertexControl vc) return;
        //if clicked vertex is tagged then remove all tagged vertices
        if (DragBehaviour.GetIsTagged(vc))
            RemoveVertex(null, null);
        else //else remove only selected vertex
            SafeRemoveVertex(vc);
    }

    private void RemoveEdge(object? sender, RoutedEventArgs e)
    {
        if (!dg_Area.EdgesList.Any()) return;
        //remove visual and data edge
        dg_Area.RemoveEdge(dg_Area.EdgesList.Last().Key, true);
    }

    private void AddEdge(object? sender, RoutedEventArgs e)
    {
        //add new edge between random vertices
        var dataEdge = GenerateRandomEdge();
        if (dataEdge == null) return;
        var ec = new EdgeControl(
            dg_Area.VertexList.FirstOrDefault(a => a.Key == dataEdge.Source)
                .Value,
            dg_Area.VertexList.FirstOrDefault(a => a.Key == dataEdge.Target)
                .Value, dataEdge);
        dg_Area.InsertEdgeAndData(dataEdge, ec);
    }

    private void RemoveVertex(object? sender, RoutedEventArgs? e)
    {
        //remove all tagged vertices from the graph entirely
        dg_Area.VertexList.Values
            .Where(DragBehaviour.GetIsTagged)
            .ToList()
            .ForEach(SafeRemoveVertex);
    }

    private void AddVertex(object? sender, RoutedEventArgs e)
    {
        var data = ThemedDataStorage.FillDataVertex(new DataVertex());
        dg_Area.AddVertexAndData(data, new VertexControl(data));

        //we have to check if there is only one vertex and set coordinates manulay 
        //because layout algorithms skip all logic if there are less than two vertices
        if (dg_Area.VertexList.Count == 1)
        {
            dg_Area.VertexList.First().Value.SetPosition(0, 0);
            dg_Area.UpdateLayout(); //update layout to update vertex size
        }
        else dg_Area.RelayoutGraph(true);

        dg_zoomctrl.ZoomToFill();
    }

    /// <summary>
    /// Remove vertex and do all cleanup necessary for current demo
    /// </summary>
    /// <param name="vc">vertexControl object</param>
    private void SafeRemoveVertex(VertexControl vc)
    {
        if (vc.Vertex is not DataVertex vertex) return;
        dg_Area.RemoveVertexAndEdges(vertex);
        dg_zoomctrl.ZoomToFill();
    }

    /// <summary>
    /// generates random edge based on the current vertices data in the graph
    /// </summary>
    private DataEdge? GenerateRandomEdge()
    {
        if (dg_Area.VertexList.Count < 2) return null;
        var vlist = dg_Area.LogicCore!.Graph.Vertices.ToList();
        var rnd1 = vlist[ShowcaseHelper.Rand.Next(0, vlist.Count - 1)];
        vlist.Remove(rnd1);
        var rnd2 = vlist[ShowcaseHelper.Rand.Next(0, vlist.Count - 1)];
        return new DataEdge(rnd1, rnd2);
    }

    /// <summary>
    /// Select vertex by setting its tag and highlight value
    /// </summary>
    /// <param name="vc">VertexControl object</param>
    private void SelectVertex(Control vc)
    {
        if (DragBehaviour.GetIsTagged(vc))
        {
            DragBehaviour.SetIsTagged(vc, false);
            vc.ClearValue(DragBehaviour.XSnapModifierProperty);
            vc.ClearValue(DragBehaviour.YSnapModifierProperty);
        }
        else
        {
            DragBehaviour.SetIsTagged(vc, true);
            DragBehaviour.SetXSnapModifier(vc, ExaggeratedSnappingXModifier);
            DragBehaviour.SetYSnapModifier(vc, ExaggeratedSnappingYModifier);
        }
    }

    private bool IsSnapping(Control obj)
    {
        return dg_snap.IsChecked ?? false;
    }

    private bool IsSnappingIndividually(Control obj)
    {
        return dg_snapIndividually.IsChecked ?? false;
    }

    private double ExaggeratedSnappingXModifier(Visual area, Control obj, double val)
    {
        if (dg_snapExaggerate.IsChecked ?? false)
        {
            return Math.Round(val * 0.01) * 100.0;
        }

        System.Diagnostics.Debug.Assert(DragBehaviour.GlobalXSnapModifier != null);
        return DragBehaviour.GlobalXSnapModifier(area, obj, val);
    }

    private double ExaggeratedSnappingYModifier(Visual area, Control obj, double val)
    {
        if (dg_snapExaggerate.IsChecked ?? false)
        {
            return Math.Round(val * 0.01) * 100.0;
        }

        System.Diagnostics.Debug.Assert(DragBehaviour.GlobalYSnapModifier != null);
        return DragBehaviour.GlobalYSnapModifier(area, obj, val);
    }
}