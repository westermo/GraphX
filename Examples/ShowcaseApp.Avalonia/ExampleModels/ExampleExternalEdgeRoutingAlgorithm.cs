using System.Collections.Generic;
using System.Threading;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Measure;

namespace ShowcaseApp.Avalonia.ExampleModels
{
    public class ExampleExternalEdgeRoutingAlgorithm<TVertex, TEdge> : IExternalEdgeRouting<TVertex, TEdge>
        where TEdge : notnull where TVertex : notnull
    {
        public void Compute(CancellationToken cancellationToken)
        {
        }

        public IDictionary<TVertex, Rect> VertexSizes { get; set; } = new Dictionary<TVertex, Rect>();

        public IDictionary<TVertex, Point> VertexPositions { get; set; } = new Dictionary<TVertex, Point>();

        private readonly Dictionary<TEdge, Point[]> _edgeRoutes = [];
        public IDictionary<TEdge, Point[]> EdgeRoutes => _edgeRoutes;

        public Point[]? ComputeSingle(TEdge edge)
        {
            return null;
        }

        public void UpdateVertexData(TVertex vertex, Point position, Rect size)
        {
        }


        public Rect AreaRectangle { get; set; }
    }
}