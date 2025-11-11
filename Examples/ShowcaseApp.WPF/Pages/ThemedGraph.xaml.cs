using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Animations;
using Westermo.GraphX.Controls.Models;
using ShowcaseApp.WPF.Models;

namespace ShowcaseApp.WPF.Pages;

/// <summary>
/// Interaction logic for ThemedGraph.xaml
/// </summary>
public partial class ThemedGraph
{
    // private ZoomControl tg_zoomctrl = new ZoomControl();

    public ThemedGraph()
    {
        InitializeComponent();

        var logic = new LogicCoreExample();
        tg_Area.LogicCore = logic;
        logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
        logic.DefaultOverlapRemovalAlgorithmParams.HorizontalGap = 50;
        logic.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 50;
        logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
        logic.EdgeCurvingEnabled = true;
        logic.AsyncAlgorithmCompute = true;
        tg_Area.SetVerticesDrag(true);
        tg_dragEnabled.IsChecked = true;

        tg_edgeMode.ItemsSource = new[] { "Draw all", "Draw for selected" };
        tg_edgeMode.SelectedIndex = 0;
        tg_edgeType.ItemsSource = Enum.GetValues<EdgesType>().Cast<EdgesType>();
        tg_edgeType.SelectedItem = EdgesType.All;
        tg_moveAnimation.ItemsSource = Enum.GetValues<MoveAnimation>().Cast<MoveAnimation>();
        tg_moveAnimation.SelectedItem = MoveAnimation.Move;
        tg_deleteAnimation.ItemsSource = Enum.GetValues<DeleteAnimation>().Cast<DeleteAnimation>();
        tg_deleteAnimation.SelectedItem = DeleteAnimation.Shrink;
        tg_mouseoverAnimation.ItemsSource = Enum.GetValues<MouseOverAnimation>().Cast<MouseOverAnimation>();
        tg_mouseoverAnimation.SelectedItem = MouseOverAnimation.Scale;
        tg_highlightType.ItemsSource = Enum.GetValues<GraphControlType>().Cast<GraphControlType>();
        tg_highlightType.SelectedItem = GraphControlType.VertexAndEdge;
        tg_highlightEdgeType.ItemsSource = Enum.GetValues<EdgesType>().Cast<EdgesType>();
        tg_highlightEdgeType.SelectedItem = EdgesType.All;
        HighlightEnabledSelectionChanged(null, null);
        DragMoveEdgesChecked(null, null);
        DragEnabledChanged(null, null);


        tg_Area.VertexSelected += AreaVertexSelected;
        tg_Area.GenerateGraphFinished += AreaGeneratedGraphFinished;
        tg_Area.RelayoutFinished += AreaRelayoutFinished;
        tg_dragMoveEdges.Checked += DragMoveEdgesChecked;
        tg_dragMoveEdges.Unchecked += DragMoveEdgesChecked;

        ZoomControl.SetViewFinderVisibility(tg_zoomctrl, Visibility.Visible);

        TgRegisterCommands();
    }

    #region Commands

    #region TGRelayoutCommand

    private static bool TgRelayoutCommandCanExecute(object sender)
    {
        return true;
    }

    private void TgRelayoutCommandExecute(object sender)
    {
        if (tg_Area.LogicCore!.AsyncAlgorithmCompute)
            tg_loader.Visibility = Visibility.Visible;

        tg_Area.RelayoutGraph(true);
    }

    #endregion

    private void TgRegisterCommands()
    {
        tg_but_relayout.Command = new SimpleCommand(TgRelayoutCommandCanExecute, TgRelayoutCommandExecute);
    }

    #endregion

    private void AreaVertexSelected(object sender, VertexSelectedEventArgs args)
    {
        if (args.MouseArgs!.LeftButton == MouseButtonState.Pressed && tg_edgeMode.SelectedIndex == 1)
        {
            tg_Area.GenerateEdgesForVertex(args.VertexControl, (EdgesType)tg_edgeType.SelectedItem);
        }

        if (args.MouseArgs.RightButton != MouseButtonState.Pressed) return;
        args.VertexControl.ContextMenu = new ContextMenu();
        var menuitem = new MenuItem { Header = "Delete item", Tag = args.VertexControl };
        menuitem.Click += DeleteItemClick;
        args.VertexControl.ContextMenu.Items.Add(menuitem);
        args.VertexControl.ContextMenu.IsOpen = true;
    }

    private void DeleteItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        if (menuItem.Tag is not VertexControl vc) return;
        if (vc.Vertex is not DataVertex vertex) return;
        tg_Area.RemoveVertexAndEdges(vertex);
    }

    private void ButRandomGraphClick(object sender, RoutedEventArgs e)
    {
        var graph = ShowcaseHelper.GenerateDataGraph(ShowcaseHelper.Rand.Next(10, 20));

        foreach (var item in graph.Vertices)
            ThemedDataStorage.FillDataVertex(item);
        foreach (var item in graph.Edges)
            item.ToolTipText = $"{item.Source.Name} -> {item.Target.Name}";

        //TIP: trick to disable zoomcontrol behaviour when it is performing fill animation from top left zoomed corner
        //instead we will fill-animate from maximum zoom distance            
        //tg_zoomctrl.Zoom = 0.01; //disable zoom control auto fill animation by setting this value
        tg_Area.GenerateGraph(graph, tg_edgeMode.SelectedIndex == 0);

        if (tg_Area.LogicCore!.AsyncAlgorithmCompute)
            tg_loader.Visibility = Visibility.Visible;
    }

    private void AreaRelayoutFinished(object sender, EventArgs e)
    {
        if (tg_Area.LogicCore!.AsyncAlgorithmCompute)
            tg_loader.Visibility = Visibility.Collapsed;
        if (tg_Area.MoveAnimation == null)
            tg_zoomctrl.ZoomToFill();
    }

    private void AreaGeneratedGraphFinished(object sender, EventArgs e)
    {
        if (tg_Area.LogicCore!.AsyncAlgorithmCompute)
            tg_loader.Visibility = Visibility.Collapsed;

        HighlightTypeSelectionChanged(null, null);
        HighlightEnabledSelectionChanged(null, null);
        HighlightEdgeTypeSelectionChanged(null, null);
        DragMoveEdgesChecked(null, null);
        DragEnabledChanged(null, null);

        tg_Area.SetEdgesDashStyle(EdgeDashStyle.Dash);
        tg_zoomctrl.ZoomToFill(); // ZoomToFill(); //manually update zoom control to fill the area
    }

    private void EdgeModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void EdgeTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void DragMoveEdgesChecked(object sender, RoutedEventArgs e)
    {
        foreach (var item in tg_Area.VertexList)
            DragBehaviour.SetUpdateEdgesOnMove(item.Value,
                tg_dragMoveEdges.IsChecked != null && tg_dragMoveEdges.IsChecked.Value);
    }


    private void DragEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (tg_dragEnabled.IsChecked == null) return;
        tg_dragMoveEdges.IsEnabled = (bool)tg_dragEnabled.IsChecked;

        foreach (var item in tg_Area.VertexList)
        {
            DragBehaviour.SetIsDragEnabled(item.Value,
                tg_dragEnabled.IsChecked.Value);
            DragBehaviour.SetUpdateEdgesOnMove(item.Value, true);
        }
    }

    private void MoveAnimationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch ((MoveAnimation)tg_moveAnimation.SelectedItem)
        {
            case MoveAnimation.None:
                tg_Area.MoveAnimation = null;
                break;
            default:
                tg_Area.MoveAnimation =
                    AnimationFactory.CreateMoveAnimation((MoveAnimation)tg_moveAnimation.SelectedItem,
                        TimeSpan.FromSeconds(1));
                tg_Area.MoveAnimation!.Completed += MoveAnimation_Completed;
                break;
        }
    }

    private void MoveAnimation_Completed(object sender, EventArgs e)
    {
        tg_zoomctrl.ZoomToFill();
    }

    private void DeleteAnimationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        tg_Area.DeleteAnimation = (DeleteAnimation)tg_deleteAnimation.SelectedItem switch
        {
            DeleteAnimation.None => null,
            DeleteAnimation.Shrink => AnimationFactory.CreateDeleteAnimation(
                (DeleteAnimation)tg_deleteAnimation.SelectedItem),
            _ => AnimationFactory.CreateDeleteAnimation((DeleteAnimation)tg_deleteAnimation.SelectedItem)
        };
    }

    private void MouseOverAnimationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        tg_Area.MouseOverAnimation = (MouseOverAnimation)tg_mouseoverAnimation.SelectedItem switch
        {
            MouseOverAnimation.None => null,
            _ => AnimationFactory.CreateMouseOverAnimation((MouseOverAnimation)tg_mouseoverAnimation.SelectedItem),
        };
    }

    private void HighlightTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in tg_Area.VertexList)
            HighlightBehaviour.SetHighlightControl(item.Value, (GraphControlType)tg_highlightType.SelectedItem);
        foreach (var item in tg_Area.EdgesList)
            HighlightBehaviour.SetHighlightControl(item.Value, (GraphControlType)tg_highlightType.SelectedItem);
    }

    private void HighlightEnabledSelectionChanged(object sender, RoutedEventArgs e)
    {
        foreach (var item in tg_Area.VertexList)
            HighlightBehaviour.SetIsHighlightEnabled(item.Value,
                tg_highlightEnabled.IsChecked != null && tg_highlightEnabled.IsChecked.Value);
        foreach (var item in tg_Area.EdgesList)
            HighlightBehaviour.SetIsHighlightEnabled(item.Value,
                tg_highlightEnabled.IsChecked != null && tg_highlightEnabled.IsChecked.Value);
    }

    private void HighlightEdgeTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in tg_Area.VertexList)
            HighlightBehaviour.SetHighlightEdges(item.Value, (EdgesType)tg_highlightEdgeType.SelectedItem);
        foreach (var item in tg_Area.EdgesList)
            HighlightBehaviour.SetHighlightEdges(item.Value, (EdgesType)tg_highlightEdgeType.SelectedItem);
    }

}