using System.Collections.Generic;
using Westermo.GraphX.Measure;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class LayoutContext<TVertex, TEdge, TGraph>(
    TGraph graph,
    IDictionary<TVertex, Point> positions,
    IDictionary<TVertex, Size> sizes,
    LayoutMode mode)
    : ILayoutContext<TVertex, TEdge, TGraph>
    where TEdge : IEdge<TVertex>
    where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
{
    public IDictionary<TVertex, Point> Positions { get; private set; } = positions;

    public IDictionary<TVertex, Size> Sizes { get; private set; } = sizes;

    public TGraph Graph { get; private set; } = graph;

    public LayoutMode Mode { get; private set; } = mode;
}