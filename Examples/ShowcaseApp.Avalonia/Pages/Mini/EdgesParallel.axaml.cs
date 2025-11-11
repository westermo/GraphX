using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ShowcaseApp.Avalonia.ExampleModels;
using ShowcaseApp.Avalonia.Models;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls.Models;

namespace ShowcaseApp.Avalonia.Pages.Mini;

/// <summary>
/// Interaction logic for LayoutVCP.xaml
/// </summary>
public partial class EdgesParallel : UserControl, INotifyPropertyChanged
{
    private int _edgeDistance;

    public int EdgeDistance
    {
        get => _edgeDistance;
        set
        {
            _edgeDistance = value;
            if (graphArea.LogicCore != null) graphArea.LogicCore.ParallelEdgeDistance = value;
            graphArea.UpdateAllEdges(true);
        }
    }

    public EdgesParallel()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += ControlLoaded;

        cbEnablePE.IsChecked = true;
        _edgeDistance = 10;

        cbEnablePE.IsCheckedChanged += CbMathShapeOnChecked;
        graphArea.EdgeLabelFactory = new DefaultEdgeLabelFactory();
    }

    private void CbMathShapeOnChecked(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (graphArea.LogicCore != null) graphArea.LogicCore.EnableParallelEdges = (bool)cbEnablePE.IsChecked!;
        graphArea.UpdateAllEdges(true);
    }

    private void ControlLoaded(object? sender, RoutedEventArgs e)
    {
        GenerateGraph();
    }

    private void GenerateGraph()
    {
        var logicCore = new LogicCoreExample()
        {
            Graph = ShowcaseHelper.GenerateDataGraph(3, false)
        };
        var vList = logicCore.Graph.Vertices.ToList();

        //add edges
        ShowcaseHelper.AddEdge(logicCore.Graph, vList[0], vList[1]);
        ShowcaseHelper.AddEdge(logicCore.Graph, vList[1], vList[0]);

        ShowcaseHelper.AddEdge(logicCore.Graph, vList[1], vList[2]);
        ShowcaseHelper.AddEdge(logicCore.Graph, vList[1], vList[2]);
        ShowcaseHelper.AddEdge(logicCore.Graph, vList[2], vList[1]);

        graphArea.LogicCore = logicCore;
        //set positions 
        var posList = new Dictionary<DataVertex, Point>()
        {
            { vList[0], new Point(0, -150) },
            { vList[1], new Point(300, 0) },
            { vList[2], new Point(600, -150) },
        };

        //settings
        graphArea.LogicCore.EnableParallelEdges = true;
        graphArea.LogicCore.EdgeCurvingEnabled = true;
        graphArea.LogicCore.ParallelEdgeDistance = _edgeDistance;
        graphArea.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
        //preload graph
        graphArea.PreloadGraph(posList);
        //behaviors
        graphArea.SetVerticesDrag(true, true);
        graphArea.ShowAllEdgesLabels();
        graphArea.AlignAllEdgesLabels();
        zoomControl.MaxZoom = 50;
        //manual edge corrections
        var eList = graphArea.EdgesList.Values.ToList();
        eList[0].GetLabelControls().First().LabelVerticalOffset = 12;
        eList[1].GetLabelControls().First().LabelVerticalOffset = 12;

        eList[2].GetLabelControls().First().ShowLabel = false;
        //PS: to see how parallel edges logic works go to GraphArea::UpdateParallelEdgesData() method

        zoomControl.ZoomToFill();
    }
}