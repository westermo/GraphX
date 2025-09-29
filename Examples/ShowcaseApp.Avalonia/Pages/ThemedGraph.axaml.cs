﻿using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.Models;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls.Avalonia;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace ShowcaseApp.Avalonia.Pages
{
    /// <summary>
    /// Interaction logic for ThemedGraph.xaml
    /// </summary>
    public partial class ThemedGraph : UserControl
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
            tg_edgeType.ItemsSource = Enum.GetValues<EdgesType>();
            tg_edgeType.SelectedItem = EdgesType.All;
            tg_highlightType.ItemsSource = Enum.GetValues<GraphControlType>();
            tg_highlightType.SelectedItem = GraphControlType.VertexAndEdge;
            tg_highlightEdgeType.ItemsSource = Enum.GetValues<EdgesType>();
            tg_highlightEdgeType.SelectedItem = EdgesType.All;
            HighlightEnabledSelectionChanged(null, null);
            DragMoveEdgesChecked(null, null);
            DragEnabledChanged(null, null);


            tg_Area.VertexSelected += AreaVertexSelected;
            tg_Area.GenerateGraphFinished += AreaGeneratedGraphFinished;
            tg_Area.RelayoutFinished += AreaRelayoutFinished;
            tg_dragMoveEdges.IsCheckedChanged += DragMoveEdgesChecked;


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
                tg_loader.IsVisible = true;

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
            if (args.MouseArgs is not PointerEventArgs pea) return;
            if (pea.Properties.IsLeftButtonPressed && tg_edgeMode.SelectedIndex == 1)
            {
                tg_Area.GenerateEdgesForVertex(args.VertexControl, (EdgesType)tg_edgeType.SelectedItem);
            }

            if (!pea.Properties.IsRightButtonPressed) return;
            args.VertexControl.ContextMenu = new ContextMenu();
            var menuitem = new MenuItem { Header = "Delete item", Tag = args.VertexControl };
            menuitem.Click += DeleteItemClick;
            args.VertexControl.ContextMenu.Items.Add(menuitem);
            args.VertexControl.ContextMenu.Open();
        }

        private void DeleteItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.Tag is not VertexControl vc) return;
            if (vc.Vertex is not DataVertex vertex) return;
            tg_Area.RemoveVertexAndEdges(vertex);
        }

        private void ButRandomGraphClick(object? sender, RoutedEventArgs routedEventArgs)
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
                tg_loader.IsVisible = true;
        }

        private void AreaRelayoutFinished(object? sender, EventArgs e)
        {
            if (tg_Area.LogicCore!.AsyncAlgorithmCompute)
                tg_loader.IsVisible = false;
            tg_zoomctrl.ZoomToFill();
        }

        private void AreaGeneratedGraphFinished(object? sender, EventArgs e)
        {
            if (tg_Area.LogicCore!.AsyncAlgorithmCompute)
                tg_loader.IsVisible = false;

            HighlightTypeSelectionChanged(null, null);
            HighlightEnabledSelectionChanged(null, null);
            HighlightEdgeTypeSelectionChanged(null, null);
            DragMoveEdgesChecked(null, null);
            DragEnabledChanged(null, null);

            tg_Area.SetEdgesDashStyle(EdgeDashStyle.Dash);
            tg_zoomctrl.ZoomToFill(); // ZoomToFill(); //manually update zoom control to fill the area
        }

        private void EdgeModeSelectionChanged(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
        }

        private void EdgeTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DragMoveEdgesChecked(object? sender, RoutedEventArgs? e)
        {
            foreach (var item in tg_Area.VertexList)
                DragBehaviour.SetUpdateEdgesOnMove(item.Value,
                    tg_dragMoveEdges.IsChecked != null && tg_dragMoveEdges.IsChecked.Value);
        }


        private void DragEnabledChanged(object? sender, RoutedEventArgs? e)
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


        private void MoveAnimation_Completed(object sender, EventArgs e)
        {
            tg_zoomctrl.ZoomToFill();
        }


        private void HighlightTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in tg_Area.VertexList)
                HighlightBehaviour.SetHighlightControl(item.Value, (GraphControlType)tg_highlightType.SelectedItem);
            foreach (var item in tg_Area.EdgesList)
                HighlightBehaviour.SetHighlightControl(item.Value, (GraphControlType)tg_highlightType.SelectedItem);
        }

        private void HighlightEnabledSelectionChanged(object? sender, RoutedEventArgs? e)
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
}