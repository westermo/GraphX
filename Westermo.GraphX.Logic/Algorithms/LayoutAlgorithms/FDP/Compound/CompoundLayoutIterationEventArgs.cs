using System.Collections.Generic;
using Westermo.GraphX.Measure;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
    public class CompoundLayoutIterationEventArgs<TVertex, TEdge>(
        int iteration,
        double statusInPercent,
        string message,
        IDictionary<TVertex, Point> vertexPositions,
        IDictionary<TVertex, Size> innerCanvasSizes)
        : LayoutIterationEventArgs<TVertex, TEdge>(iteration, statusInPercent, message, vertexPositions),
            ICompoundLayoutIterationEventArgs<TVertex>
        where TVertex : class
        where TEdge : IEdge<TVertex>
    {
        #region ICompoundLayoutIterationEventArgs<TVertex> Members

        public IDictionary<TVertex, Size> InnerCanvasSizes
        {
            get; private set;
        } = innerCanvasSizes;

        #endregion
    }
}
