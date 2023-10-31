﻿using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
    public interface IMutableCompoundGraph<TVertex, TEdge> 
        : ICompoundGraph<TVertex, TEdge>,
          IMutableBidirectionalGraph<TVertex, TEdge> 
        where TEdge : IEdge<TVertex>
    {
    }
}
