using System.Threading;
using System.Collections.Generic;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Interfaces;

namespace ShowcaseApp.WPF
{
    public class ExampleExternalEdgeRoutingAlgorithm<TVertex, TEdge> : IExternalEdgeRouting<TVertex, TEdge>
    {
        public void Compute(CancellationToken cancellationToken)
        {
        }

        public IDictionary<TVertex, Rect> VertexSizes { get; set; }

        public IDictionary<TVertex, Point> VertexPositions { get; set; }

        private readonly Dictionary<TEdge, Point[]> _edgeRoutes = [];
        public IDictionary<TEdge, Point[]> EdgeRoutes => _edgeRoutes;

        public Point[] ComputeSingle(TEdge edge) { return null; }

        public void UpdateVertexData(TVertex vertex, Point position, Rect size) { }


        public Rect AreaRectangle { get; set; }

    }
}
