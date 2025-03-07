﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Controls.Models;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using ShowcaseApp.WPF.Models;
using Rect = Westermo.GraphX.Measure.Rect;
using Westermo.GraphX;
using System.Windows.Media;

namespace ShowcaseApp.WPF.Pages
{
    /// <summary>
    /// Interaction logic for EdgeRoutingGraph.xaml
    /// </summary>
    public partial class EdgeRoutingGraph : INotifyPropertyChanged
    {
        private PathFinderEdgeRoutingParameters _pfPrms;

        public PathFinderEdgeRoutingParameters PfErParameters
        {
            get => _pfPrms;
            set
            {
                _pfPrms = value;
                OnPropertyChanged("PfErParameters");
            }
        }

        private SimpleERParameters _simplePrms;

        public SimpleERParameters SimpleErParameters
        {
            get => _simplePrms;
            set
            {
                _simplePrms = value;
                OnPropertyChanged("SimpleErParameters");
            }
        }

        private BundleEdgeRoutingParameters _bundlePrms;

        public BundleEdgeRoutingParameters BundleEdgeRoutingParameters
        {
            get => _bundlePrms;
            set
            {
                _bundlePrms = value;
                OnPropertyChanged("BundleEdgeRoutingParameters");
            }
        }

        private readonly LogicCoreExample _logicCore;

        public EdgeRoutingGraph()
        {
            InitializeComponent();
            DataContext = this;
            _logicCore = new LogicCoreExample();
            erg_Area.LogicCore = _logicCore;
            erg_Area.LogicCore.ParallelEdgeDistance = 20;
            erg_Area.EdgeLabelFactory = new DefaultEdgelabelFactory();
            erg_Area.ControlFactory = new ExampleFactory();

            erg_showEdgeArrows.IsChecked = true;
            BundleEdgeRoutingParameters =
                (BundleEdgeRoutingParameters)_logicCore.AlgorithmFactory.CreateEdgeRoutingParameters(
                    EdgeRoutingAlgorithmTypeEnum.Bundling);
            SimpleErParameters =
                (SimpleERParameters)_logicCore.AlgorithmFactory.CreateEdgeRoutingParameters(EdgeRoutingAlgorithmTypeEnum
                    .SimpleER);
            PfErParameters =
                (PathFinderEdgeRoutingParameters)_logicCore.AlgorithmFactory.CreateEdgeRoutingParameters(
                    EdgeRoutingAlgorithmTypeEnum.PathFinder);

            erg_pfprm_formula.ItemsSource = Enum.GetValues<PathFindAlgorithm>().Cast<PathFindAlgorithm>();
            erg_pfprm_formula.SelectedIndex = 0;

            erg_but_randomgraph.Click += erg_but_randomgraph_Click;
            erg_but_relayout.Click += erg_but_relayout_Click;
            erg_useExternalERAlgo.Checked += erg_useExternalERAlgo_Checked;
            erg_useExternalERAlgo.Unchecked += erg_useExternalERAlgo_Checked;
            erg_dashstyle.ItemsSource = Enum.GetValues<EdgeDashStyle>().Cast<EdgeDashStyle>();
            erg_dashstyle.SelectedIndex = 0;
            erg_dashstyle.SelectionChanged += erg_dashstyle_SelectionChanged;
            erg_eralgo.ItemsSource = Enum.GetValues<EdgeRoutingAlgorithmTypeEnum>()
                .Cast<EdgeRoutingAlgorithmTypeEnum>();
            erg_eralgo.SelectedIndex = 0;
            erg_eralgo.SelectionChanged += erg_eralgo_SelectionChanged;
            erg_prmsbox.Visibility = Visibility.Collapsed;
            erg_recalculate.Checked += erg_recalculate_Checked;
            erg_recalculate.Unchecked += erg_recalculate_Checked;
            erg_randomizeAll.Click += erg_randomizeAll_Click;
            erg_showEdgeArrows.Checked += erg_showEdgeArrows_Checked;
            erg_showEdgeArrows.Unchecked += erg_showEdgeArrows_Checked;
            erg_showEdgeLabels.Checked += erg_showEdgeLabels_Checked;
            erg_showEdgeLabels.Unchecked += erg_showEdgeLabels_Checked;
            erg_alignEdgeLabels.Checked += erg_alignEdgeLabels_Checked;
            erg_alignEdgeLabels.Unchecked += erg_alignEdgeLabels_Checked;
            erg_enableParallelEdges.Checked += erg_enableParallelEdges_Checked;
            erg_enableParallelEdges.Unchecked += erg_enableParallelEdges_Checked;


            erg_randomizeArrows.Click += erg_randomizeArrows_Click;
            erg_useCurves.Checked += erg_useCurves_Checked;
            erg_useCurves.Unchecked += erg_useCurves_Checked;
            ZoomControl.SetViewFinderVisibility(erg_zoomctrl, Visibility.Visible);
        }

        private void erg_enableParallelEdges_Checked(object sender, RoutedEventArgs e)
        {
            erg_Area.LogicCore!.EnableParallelEdges =
                erg_enableParallelEdges.IsChecked != null && erg_enableParallelEdges.IsChecked.Value;
        }

        private void erg_useCurves_Checked(object sender, RoutedEventArgs e)
        {
            //update edge curving property
            erg_Area.LogicCore!.EdgeCurvingEnabled = erg_useCurves.IsChecked != null && erg_useCurves.IsChecked.Value;
            erg_Area.UpdateAllEdges();
        }

        private void erg_randomizeArrows_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in erg_Area.EdgesList.ToList())
                item.Value.SetCurrentValue(EdgeControlBase.ShowArrowsProperty,
                    Convert.ToBoolean(ShowcaseHelper.Rand.Next(0, 2)));
        }

        private void erg_showEdgeLabels_Checked(object sender, RoutedEventArgs e)
        {
            erg_Area.ShowAllEdgesLabels(erg_showEdgeLabels.IsChecked != null && erg_showEdgeLabels.IsChecked.Value);
            erg_Area.InvalidateVisual();
        }

        private void erg_alignEdgeLabels_Checked(object sender, RoutedEventArgs e)
        {
            erg_Area.AlignAllEdgesLabels(erg_alignEdgeLabels.IsChecked != null && erg_alignEdgeLabels.IsChecked.Value);
            erg_Area.InvalidateVisual();
        }

        private void erg_showEdgeArrows_Checked(object sender, RoutedEventArgs e)
        {
            erg_Area.ShowAllEdgesArrows(erg_showEdgeArrows.IsChecked != null && erg_showEdgeArrows.IsChecked.Value);
        }


        private void erg_randomizeAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in erg_Area.EdgesList.ToList())
            {
                var sarr = Enum.GetValues<EdgeDashStyle>();
                item.Value.DashStyle = (EdgeDashStyle)sarr.GetValue(ShowcaseHelper.Rand.Next(0, sarr.Length - 1))!;
            }
        }

        private void erg_recalculate_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in erg_Area.GetAllVertexControls())
                DragBehaviour.SetUpdateEdgesOnMove(item,
                    erg_recalculate.IsChecked != null && erg_recalculate.IsChecked.Value);
        }

        private void erg_dashstyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            erg_Area.SetEdgesDashStyle((EdgeDashStyle)erg_dashstyle.SelectedItem);
        }

        private void erg_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = CustomHelper.IsIntegerInput(e.Text);
        }

        private void erg_to1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = CustomHelper.IsDoubleInput(e.Text);
            if (e.Handled) return;
            var res = 0.0;
            if (sender is TextBox textBox && !double.TryParse(textBox.Text, out res)) return;
            if (res < 0.0 || res > 1.0) e.Handled = false;
        }

        private void erg_tominus1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = CustomHelper.IsDoubleInput(e.Text);
            if (e.Handled) return;
            var res = 0.0;
            if (sender is TextBox textBox && !double.TryParse(textBox.Text, out res)) return;
            if (res < -1.0 || res > 0.0) e.Handled = false;
        }


        private void erg_eralgo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            erg_recalculate.IsEnabled = true;
            if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.None)
                erg_prmsbox.Visibility = Visibility.Collapsed;
            else
            {
                //clean prms
                erg_prmsbox.Visibility = Visibility.Visible;
                if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.SimpleER)
                {
                    simpleer_prms_dp.Visibility = Visibility.Visible;
                    bundleer_prms_dp.Visibility = Visibility.Collapsed;
                    pfer_prms_dp.Visibility = Visibility.Collapsed;
                }

                if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.PathFinder)
                {
                    simpleer_prms_dp.Visibility = Visibility.Collapsed;
                    bundleer_prms_dp.Visibility = Visibility.Collapsed;
                    pfer_prms_dp.Visibility = Visibility.Visible;
                }

                if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.Bundling)
                {
                    simpleer_prms_dp.Visibility = Visibility.Collapsed;
                    bundleer_prms_dp.Visibility = Visibility.Visible;
                    pfer_prms_dp.Visibility = Visibility.Collapsed;
                    //bundling doesn't support single edge routing
                    erg_recalculate.IsChecked = false;
                    erg_recalculate.IsEnabled = false;
                }
            }
        }

        private void erg_useExternalERAlgo_Checked(object sender, RoutedEventArgs e)
        {
            if (erg_useExternalERAlgo.IsChecked == true)
            {
                if (erg_Area.LogicCore!.Graph == null) erg_but_randomgraph_Click(null, null);
                erg_Area.GetLogicCore<LogicCoreExample>().ExternalEdgeRoutingAlgorithm =
                    erg_Area.LogicCore.AlgorithmFactory.CreateEdgeRoutingAlgorithm(
                        EdgeRoutingAlgorithmTypeEnum.SimpleER,
                        new Rect(0, 0, erg_Area.DesiredSize.Width, erg_Area.DesiredSize.Height),
                        erg_Area.LogicCore.Graph, null, null);
            }
            else erg_Area.GetLogicCore<LogicCoreExample>().ExternalEdgeRoutingAlgorithm = null;
        }

        private void erg_but_relayout_Click(object sender, RoutedEventArgs e)
        {
            switch ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem)
            {
                case EdgeRoutingAlgorithmTypeEnum.PathFinder:
                    erg_Area.RelayoutGraph();
                    break;
                case EdgeRoutingAlgorithmTypeEnum.Bundling:
                    erg_Area.RelayoutGraph();
                    break;
                default:
                    erg_Area.GenerateGraph(erg_Area.LogicCore!.Graph);
                    break;
            }
        }

        private void GenerateRandomVertices(GraphExample graph, int index, int count, int minX,
            int maxX, int minY, int maxY)
        {
            var list = graph.Vertices.ToList();
            for (var i = index; i < index + count; i++)
            {
                var vertex = list[i];
                var vc = new VertexControl(vertex);
                erg_Area.AddVertex(vertex, vc);
                var point = new Point(ShowcaseHelper.Rand.Next(minX, maxX),
                    ShowcaseHelper.Rand.Next(minY, maxY));
                vc.SetPosition(point);
            }
        }

        private void erg_but_randomgraph_Click(object sender, RoutedEventArgs e)
        {
            if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem != EdgeRoutingAlgorithmTypeEnum.Bundling)
            {
                var gr = ShowcaseHelper.GenerateDataGraph(30);

                if (erg_Area.LogicCore!.EnableParallelEdges)
                {
                    if (erg_Area.VertexList.Count() < 2)
                    {
                        var v1 = new DataVertex();
                        gr.AddVertex(v1);
                        var v2 = new DataVertex();
                        gr.AddVertex(v2);
                        gr.AddEdge(new DataEdge(v1, v2) { Text = $"{v1.Text} -> {v2.Text}" });
                        gr.AddEdge(new DataEdge(v2, v1) { Text = $"{v2.Text} -> {v1.Text}" });
                    }
                    else
                    {
                        var v1 = gr.Vertices.ToList()[0];
                        var v2 = gr.Vertices.ToList()[1];
                        gr.AddEdge(new DataEdge(v1, v2) { Text = $"{v1.Text} -> {v2.Text}" });
                        gr.AddEdge(new DataEdge(v2, v1) { Text = $"{v2.Text} -> {v1.Text}" });
                    }
                }

                erg_Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithm =
                    (EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem;
                switch ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem)
                {
                    case EdgeRoutingAlgorithmTypeEnum.SimpleER:
                        erg_Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithmParams =
                            SimpleErParameters;
                        break;
                    case EdgeRoutingAlgorithmTypeEnum.PathFinder:
                        erg_Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithmParams = PfErParameters;
                        break;
                }

                erg_Area.GetLogicCore<LogicCoreExample>().DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.SimpleRandom;
                erg_Area.GetLogicCore<LogicCoreExample>().DefaultLayoutAlgorithmParams =
                    new RandomLayoutAlgorithmParams() { Bounds = new Rect(0, 0, 500, 500) };
                erg_Area.GetLogicCore<LogicCoreExample>().DefaultOverlapRemovalAlgorithm =
                    OverlapRemovalAlgorithmTypeEnum.FSA;
                erg_Area.GetLogicCore<LogicCoreExample>().DefaultOverlapRemovalAlgorithmParams =
                    erg_Area.LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum
                        .FSA);

                erg_Area.GenerateGraph(gr);
                erg_Area.SetVerticesDrag(true, true);
                erg_Area.SetEdgesDrag(true);
                erg_zoomctrl.ZoomToFill();

                return;
            }

            erg_Area.RemoveAllEdges();
            erg_Area.RemoveAllVertices();
            //generate graph
            var graph = new GraphExample();
            foreach (var item in ShowcaseHelper.DataSource.Take(120))
                graph.AddVertex(new DataVertex(item.Text) { ID = item.ID });

            var vlist = graph.Vertices.ToList();
            foreach (var item in vlist)
            {
                if (ShowcaseHelper.Rand.Next(0, 50) > 25) continue;
                var vertex2 = vlist[ShowcaseHelper.Rand.Next(0, graph.VertexCount - 1)];
                graph.AddEdge(new DataEdge(item, vertex2, ShowcaseHelper.Rand.Next(1, 50))
                { ToolTipText = $"{item} -> {vertex2}" });
            }

            //generate vertices

            GenerateRandomVertices(graph, 0, 40, 0, 2000, 0, 2000);
            GenerateRandomVertices(graph, 40, 40, 5000, 7000, 3000, 4000);
            GenerateRandomVertices(graph, 80, 40, 500, 2500, 6000, 9000);
            erg_Area.LogicCore!.Graph = graph;
            UpdateLayout();

            erg_Area.SetVerticesDrag(true);
            _logicCore.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Custom;
            _logicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.Bundling;
            _logicCore.DefaultEdgeRoutingAlgorithmParams = BundleEdgeRoutingParameters;
            erg_Area.GenerateGraph();

            erg_zoomctrl.ZoomToFill();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }

    internal class ExampleFactory : IGraphControlFactory
    {
        public GraphAreaBase FactoryRootArea => throw new NotImplementedException();

        public EdgeControl CreateEdgeControl(VertexControl source, VertexControl target, object edge, bool showArrows = true, Visibility visibility = Visibility.Visible)
        {
            return new FunnyEdgeControl
            {
                Source = source,
                Target = target,
                Edge = edge,
                ShowArrows = showArrows,
            };
        }

        public VertexControl CreateVertexControl(object vertexData)
        {
            return new VertexControl(vertexData);
        }
    }
    internal class FunnyEdgeControl : EdgeControl
    {
        internal int Frequency = 10;
        internal int Amplitude = 20;
        internal int PointCount = 90;
        protected override PathFigure TransformUnroutedPath(PathFigure original)
        {
            var startPoint = original.StartPoint;
            var endPoint = original.Segments.OfType<LineSegment>().First().Point;
            original.Segments.Clear();
            var poly = new PolyLineSegment();
            var vector = endPoint - original.StartPoint;
            var orthogonal = new Vector(-vector.Y, vector.X);
            orthogonal.Normalize();
            for (double i = 1; i <= PointCount; i++)
            {
                var p = startPoint
                + vector * (i / PointCount)
                + orthogonal * (Math.Sin(i * 2 * Math.PI * Frequency / PointCount) * Amplitude);
                poly.Points.Add(p);
            }
            poly.IsStroked = true;
            original.Segments.Add(poly);
            return original;
        }
    }
}