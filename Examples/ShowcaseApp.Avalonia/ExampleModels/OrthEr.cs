using System;
using System.Collections.Generic;
using System.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Measure;

namespace ShowcaseApp.Avalonia.ExampleModels;

public class OrthEr<TVertex, TEdge, TGraph>(
    TGraph graph,
    IDictionary<TVertex, Point> vertexPositions,
    IDictionary<TVertex, Rect> vertexSizes,
    IEdgeRoutingParameters? parameters = null)
    : EdgeRoutingAlgorithmBase<TVertex, TEdge, TGraph>(graph, vertexPositions, vertexSizes, parameters)
    where TGraph : class, IMutableBidirectionalGraph<TVertex, TEdge>
    where TEdge : class, IGraphXEdge<TVertex>
    where TVertex : class, IGraphXVertex
{
    public override void Compute(CancellationToken cancellationToken)
    {
        foreach (var edge in Graph.Edges)
        {
            var sourcePosition = VertexPositions[edge.Source];
            var targetPosition = VertexPositions[edge.Target];
            var sourceSize = VertexSizes[edge.Source];
            var targetSize = VertexSizes[edge.Target];

            if (Math.Abs(sourcePosition.X - targetPosition.X) > 1e-12)
            {
                EdgeRoutes.Add(
                    edge,
                    [
                        new Point(0, 0),
                        new Point(targetPosition.X + targetSize.Width / 2,
                            sourcePosition.Y + sourceSize.Height / 2),
                        new Point(0, 0)
                    ]);
            }
        }
    }

    /// <summary>
    /// Compute edge routing for single edge
    /// </summary>
    /// <param name="edge">Supplied edge data</param>
    public override Point[]? ComputeSingle(TEdge edge)
    {
        return null;
    }
}