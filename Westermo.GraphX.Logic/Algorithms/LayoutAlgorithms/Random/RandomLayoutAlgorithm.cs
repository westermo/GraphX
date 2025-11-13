using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class RandomLayoutAlgorithm<TVertex, TEdge, TGraph> : LayoutAlgorithmBase<TVertex, TEdge, TGraph>
    where TVertex : class, IGraphXVertex
    where TEdge : IGraphXEdge<TVertex>
    where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{
    private readonly RandomLayoutAlgorithmParams _parameters;

    public RandomLayoutAlgorithm(TGraph graph, IDictionary<TVertex, Point> positions,
        RandomLayoutAlgorithmParams prms)
        : base(graph, positions)
    {
        _parameters = prms;
    }

    public RandomLayoutAlgorithm(RandomLayoutAlgorithmParams prms)
        : base(default(TGraph))
    {
        _parameters = prms;
    }

    public override void Compute(CancellationToken cancellationToken)
    {
        if (VisitedGraph is null) return;
        VertexPositions.Clear();
        var bounds = _parameters?.Bounds ?? new RandomLayoutAlgorithmParams().Bounds;
        var boundsWidth = (int)bounds.Width;
        var boundsHeight = (int)bounds.Height;
        var seed = _parameters?.Seed ?? Guid.NewGuid().GetHashCode();
        var rnd = new Random(seed);
        foreach (var item in VisitedGraph.Vertices)
        {
            if (item.SkipProcessing != ProcessingOptionEnum.Freeze || VertexPositions.Count == 0)
            {
                var x = (int)bounds.X;
                var y = (int)bounds.Y;
                var size = VertexSizes.FirstOrDefault(a => a.Key == item).Value;
                VertexPositions.Add(item,
                    new Point(rnd.Next(x, x + boundsWidth - (int)size.Width),
                        rnd.Next(y, y + boundsHeight - (int)size.Height)));
            }
        }
    }

    public override bool NeedVertexSizes => true;

    public override bool SupportsObjectFreeze => true;

    public override void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
    {
        if (VisitedGraph is null) return;
        VisitedGraph.Clear();
        VisitedGraph.AddVertexRange(vertices);
        VisitedGraph.AddEdgeRange(edges);
    }
}