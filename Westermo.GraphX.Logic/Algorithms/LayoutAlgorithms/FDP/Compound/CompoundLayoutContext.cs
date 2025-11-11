using System.Collections.Generic;
using Westermo.GraphX.Measure;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class CompoundLayoutContext<TVertex, TEdge, TGraph>(
    TGraph graph,
    IDictionary<TVertex, Point> positions,
    IDictionary<TVertex, Size> sizes,
    LayoutMode mode,
    IDictionary<TVertex, Thickness> vertexBorders,
    IDictionary<TVertex, CompoundVertexInnerLayoutType> layoutTypes)
    : LayoutContext<TVertex, TEdge, TGraph>(graph, positions, sizes, mode),
        ICompoundLayoutContext<TVertex, TEdge, TGraph>
    where TEdge : IEdge<TVertex>
    where TGraph : class, IBidirectionalGraph<TVertex, TEdge>
{
    public IDictionary<TVertex, Thickness> VertexBorders { get; private set; } = vertexBorders;
    public IDictionary<TVertex, CompoundVertexInnerLayoutType> LayoutTypes { get; private set; } = layoutTypes;
}