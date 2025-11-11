using System.Collections.Generic;
using Westermo.GraphX.Measure;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class TestingCompoundLayoutIterationEventArgs<TVertex, TEdge, TVertexInfo, TEdgeInfo>(
    int iteration,
    double statusInPercent,
    string message,
    IDictionary<TVertex, Point> vertexPositions,
    IDictionary<TVertex, Size> innerCanvasSizes,
    IDictionary<TVertex, TVertexInfo> vertexInfos,
    Point gravitationCenter)
    : CompoundLayoutIterationEventArgs<TVertex, TEdge>(iteration, statusInPercent, message, vertexPositions,
        innerCanvasSizes), ILayoutInfoIterationEventArgs<TVertex, TEdge, TVertexInfo, TEdgeInfo>
    where TVertex : class
    where TEdge : IEdge<TVertex>
{
    public Point GravitationCenter { get; private set; } = gravitationCenter;

    public override object GetVertexInfo(TVertex vertex)
    {
        TVertexInfo info;
        if (vertexInfos.TryGetValue(vertex, out info))
            return info;

        return null;
    }

    public IDictionary<TVertex, TVertexInfo> VertexInfos => vertexInfos;

    public IDictionary<TEdge, TEdgeInfo> EdgeInfos => null;
}