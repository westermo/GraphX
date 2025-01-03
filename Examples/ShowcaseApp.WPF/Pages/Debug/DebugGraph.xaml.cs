﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Westermo.GraphX;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Animations;
using Westermo.GraphX.Controls.Models;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms.Grouped;
using Westermo.GraphX.Logic.Algorithms.OverlapRemoval;
using QuikGraph;
using ShowcaseApp.WPF.Models;
using Rect = Westermo.GraphX.Measure.Rect;

namespace ShowcaseApp.WPF.Pages
{
    /// <summary>
    /// Interaction logic for DebugGraph.xaml
    /// </summary>
    public partial class DebugGraph : INotifyPropertyChanged
    {
        private DebugModeEnum _debugMode;

        public DebugModeEnum DebugMode
        {
            get => _debugMode;
            set
            {
                _debugMode = value;
                OnPropertyChanged("DebugMode");
            }
        }

        public DebugGraph()
        {
            InitializeComponent();
            DataContext = this;
            butEdgePointer.Click += butEdgePointer_Click;
            butGeneral.Click += butGeneral_Click;
            butRelayout.Click += butRelayout_Click;
            butVCP.Click += butVCP_Click;
            butEdgeLabels.Click += butEdgeLabels_Click;
            butGroupedGraph.Click += butGroupedGraph_Click;
            cbDebugMode.ItemsSource = Enum.GetValues<DebugModeEnum>().Cast<DebugModeEnum>();
            cbDebugMode.SelectionChanged += cbDebugMode_SelectionChanged;
            dg_zoomctrl.PropertyChanged += dg_zoomctrl_PropertyChanged;
            CreateNewArea();
            dg_zoomctrl.ZoomStep = 100;
        }

        private void butGroupedGraph_Click(object sender, RoutedEventArgs e)
        {
            CreateNewArea();
            if (dg_Area.LogicCore is null) return;
            dg_Area.LogicCore.Graph = ShowcaseHelper.GenerateDataGraph(10);
            dg_Area.LogicCore.Graph.Vertices.Take(5).ForEach(a => a.GroupId = 1);
            dg_Area.LogicCore.Graph.Vertices.Where(a => a.GroupId == 0).ForEach(a => a.GroupId = 2);
            dg_Area.LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            //generate group params
            var prms = new List<AlgorithmGroupParameters<DataVertex, DataEdge>>
            {
                new()
                {
                    GroupId = 1,
                    LayoutAlgorithm =
                        new RandomLayoutAlgorithm<DataVertex, DataEdge, GraphExample>(
                            new RandomLayoutAlgorithmParams { Bounds = new Rect(0, 0, 500, 500) }),

                    // ZoneRectangle = new Rect(0, 0, 500, 500)
                },
                new()
                {
                    GroupId = 2,
                    LayoutAlgorithm =
                        new RandomLayoutAlgorithm<DataVertex, DataEdge, GraphExample>(
                            new RandomLayoutAlgorithmParams { Bounds = new Rect(0, 0, 500, 500) }),

                    // ZoneRectangle = new Rect(1000, 0, 500, 500)
                }
            };

            var gParams = new GroupingLayoutAlgorithmParameters<DataVertex, DataEdge>(prms, true);
            //generate grouping algo
            dg_Area.LogicCore.ExternalLayoutAlgorithm =
                new GroupingLayoutAlgorithm<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>(
                    dg_Area.LogicCore.Graph, null, gParams);

            //generate graphs
            dg_Area.GenerateGraph();

            //generate group visuals
            foreach (var item in prms)
            {
                if (!item.ZoneRectangle.HasValue) continue;
                var rect = new Rectangle
                {
                    Width = item.ZoneRectangle.Value.Width,
                    Height = item.ZoneRectangle.Value.Height,
                    Fill = item.GroupId == 1 ? Brushes.Blue : Brushes.Black,
                    Opacity = .5
                };
                dg_Area.InsertCustomChildControl(0, rect);
                GraphAreaBase.SetX(rect, item.ZoneRectangle.Value.X);
                GraphAreaBase.SetY(rect, item.ZoneRectangle.Value.Y);
            }

            dg_zoomctrl.ZoomToFill();
        }

        private void dg_zoomctrl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Zoom")
            {
                Debug.WriteLine("Zoom: " + dg_zoomctrl.Zoom);
            }
        }

        private void CreateNewArea()
        {
            if (dg_Area != null)
            {
                dg_Area.GenerateGraphFinished -= dg_Area_GenerateGraphFinished;
                dg_Area.RelayoutFinished -= dg_Area_GenerateGraphFinished;
                dg_Area.ClearLayout();
                dg_Area.Dispose();
            }

            dg_Area = new GraphAreaExample
            {
                Name = "dg_Area",
                LogicCore = new LogicCoreExample(),
                Resources = new ResourceDictionary
                    { Source = new Uri("/Templates/Debug/TestTemplates.xaml", UriKind.RelativeOrAbsolute) }
            };
            dg_Area.SetVerticesDrag(true, true);
            dg_zoomctrl.Content = dg_Area;
            dg_Area.ShowAllEdgesLabels(false);
        }

        private void dg_Area_GenerateGraphFinished(object sender, EventArgs e)
        {
            dg_zoomctrl.ZoomToFill();
        }

        private void butEdgeLabels_Click(object sender, RoutedEventArgs e)
        {
            CreateNewArea();

            dg_Area.ShowAllEdgesLabels();
            dg_Area.AlignAllEdgesLabels();
            dg_Area.ShowAllEdgesArrows();
            if (dg_Area.LogicCore is null) return;
            dg_Area.LogicCore.Graph = ShowcaseHelper.GenerateDataGraph(2, false);

            var vertexList = dg_Area.LogicCore.Graph.Vertices.ToList();
            var edge = new DataEdge(vertexList[0], vertexList[1]) { Text = "Testing edge labels..." };
            dg_Area.LogicCore.Graph.AddEdge(edge);

            dg_Area.PreloadGraph(new Dictionary<DataVertex, Point>
                { { vertexList[0], new Point() }, { vertexList[1], new Point(0, 200) } });
            dg_Area.VertexList.Values.ToList().ForEach(a => a.SetConnectionPointsVisibility(false));
        }

        private void butVCP_Click(object sender, RoutedEventArgs e)
        {
            CreateNewArea();
            dg_Area.VertexList.Values.ToList().ForEach(a => a.SetConnectionPointsVisibility(true));
            dg_Area.LogicCore!.Graph = ShowcaseHelper.GenerateDataGraph(6, false);
            var vlist = dg_Area.LogicCore.Graph.Vertices.ToList();
            var edge = new DataEdge(vlist[0], vlist[1]) { SourceConnectionPointId = 1, TargetConnectionPointId = 1 };
            dg_Area.LogicCore.Graph.AddEdge(edge);
            edge = new DataEdge(vlist[0], vlist[0]) { SourceConnectionPointId = 1, TargetConnectionPointId = 1 };
            dg_Area.LogicCore.Graph.AddEdge(edge);


            dg_Area.LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.ISOM;
            dg_Area.LogicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            dg_Area.LogicCore.DefaultOverlapRemovalAlgorithmParams =
                dg_Area.LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)dg_Area.LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)dg_Area.LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
            dg_Area.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;

            dg_Area.GenerateGraph();
            var vertex = dg_Area.VertexList[edge.Target];

            var newVcp = new StaticVertexConnectionPoint { Id = 5, Margin = new Thickness(2, 0, 0, 0) };
            var cc = new Border
            {
                Margin = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(0),
                Child = newVcp
            };
            edge.TargetConnectionPointId = 5;
            vertex.VCPRoot?.Children.Add(cc);
            vertex.VertexConnectionPointsList.Add(newVcp);

            dg_Area.EdgesList[edge].UpdateEdge();
            dg_Area.UpdateAllEdges(true);
        }

        private void butRelayout_Click(object sender, RoutedEventArgs e)
        {
            dg_Area.RelayoutGraph(true);
        }

        private void butGeneral_Click(object sender, RoutedEventArgs e)
        {
            CreateNewArea();
            dg_Area.LogicCore!.Graph = ShowcaseHelper.GenerateDataGraph(2, false);
            dg_Area.LogicCore.Graph.Vertices.First().IsBlue = true;
            //dg_Area.LogicCore.Graph.AddEdge(new DataEdge(vlist[0], vlist[1]) { ArrowTarget = true});
            //dg_Area.LogicCore.Graph.AddEdge(new DataEdge(vlist[0], vlist[2]) { ArrowTarget = true });
            //dg_Area.LogicCore.Graph.AddEdge(new DataEdge(vlist[0], vlist[3]) { ArrowTarget = true });
            //dg_Area.LogicCore.Graph.AddEdge(new DataEdge(vlist[0], vlist[4]) { ArrowTarget = true });


            dg_Area.LogicCore.EdgeCurvingEnabled = true;
            dg_Area.LogicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.EfficientSugiyama;
            dg_Area.SetVerticesMathShape(VertexShape.Ellipse);
            dg_Area.GenerateGraph();

            dg_Area.VertexList.Values.ToList().ForEach(a => a.SetConnectionPointsVisibility(false));
        }

        private void butEdgePointer_Click(object sender, RoutedEventArgs e)
        {
            CreateNewArea();
            dg_Area.VertexList.Values.ToList().ForEach(a => a.SetConnectionPointsVisibility(false));
        }

        #region DebugMode switches

        private void cbDebugMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (DebugMode)
            {
                case DebugModeEnum.Clean:
                    CleanDMAnimations();
                    CleanDMERCurving();
                    CleanDMER();
                    break;
                case DebugModeEnum.Animations:
                    CleanDMERCurving();
                    CleanDMER();
                    dg_Area.MoveAnimation =
                        AnimationFactory.CreateMoveAnimation(MoveAnimation.Move, TimeSpan.FromSeconds(0.5));
                    dg_Area.MoveAnimation.Completed += dg_Area_GenerateGraphFinished;
                    dg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.Scale);
                    dg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade);
                    break;
                case DebugModeEnum.EdgeRoutingEnabled:
                    CleanDMAnimations();
                    CleanDMERCurving();
                    dg_Area.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
                    break;
                case DebugModeEnum.EdgeRoutingWithCurvingEnabled:
                    CleanDMAnimations();
                    CleanDMER();
                    dg_Area.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
                    dg_Area.LogicCore.EdgeCurvingEnabled = true;
                    break;
            }
        }

        private void CleanDMERCurving()
        {
            dg_Area.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
            dg_Area.LogicCore.EdgeCurvingEnabled = false;
        }

        private void CleanDMER()
        {
            dg_Area.LogicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
        }

        private void CleanDMAnimations()
        {
            if (dg_Area.MoveAnimation != null)
                dg_Area.MoveAnimation.Completed -= dg_Area_GenerateGraphFinished;
            dg_Area.MoveAnimation = null;
            dg_Area.MouseOverAnimation = null;
            dg_Area.DeleteAnimation = null;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public enum DebugModeEnum
    {
        Clean,
        Animations,
        EdgeRoutingEnabled,
        EdgeRoutingWithCurvingEnabled
    }
}