using System.Collections.Generic;
using System.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Measure;

/*
 External layout algorithm implementation example
 
 Also shows how to use internal algorithms inside the external one.
 
 */
namespace ShowcaseApp.Avalonia.ExampleModels
{
    public class ExampleExternalLayoutAlgorithm(IMutableBidirectionalGraph<DataVertex, DataEdge> graph)
        : IExternalLayout<DataVertex, DataEdge>
    {
        public bool SupportsObjectFreeze => true;

        public void ResetGraph(IEnumerable<DataVertex> vertices, IEnumerable<DataEdge> edges)
        {
            _graph = new BidirectionalGraph<DataVertex, DataEdge>();
            _graph.AddVertexRange(vertices);
            _graph.AddEdgeRange(edges);
        }

        private IMutableBidirectionalGraph<DataVertex, DataEdge> _graph = graph;

        public void Compute(CancellationToken cancellationToken)
        {
            var pars = new EfficientSugiyamaLayoutParameters { LayerDistance = 200 };
            var algo = new EfficientSugiyamaLayoutAlgorithm<DataVertex, DataEdge, IMutableBidirectionalGraph<DataVertex, DataEdge>>(_graph, pars, VertexPositions, VertexSizes);
            algo.Compute(cancellationToken);

            // now you can use = algo.VertexPositions for custom manipulations

            //set this algo calculation results 
            VertexPositions = algo.VertexPositions;
        }

        public IDictionary<DataVertex, Point> VertexPositions { get; private set; } = new Dictionary<DataVertex, Point>();

        public IDictionary<DataVertex, Size> VertexSizes { get; set; } = new Dictionary<DataVertex, Size>();

        public bool NeedVertexSizes => true;
    }
}
