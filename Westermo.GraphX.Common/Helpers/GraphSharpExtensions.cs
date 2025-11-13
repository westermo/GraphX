using System;
using System.Collections.Generic;
using System.Linq;
using Westermo.GraphX.Common.Interfaces;
using QuikGraph;
using QuikGraph.Algorithms.ShortestPath;

namespace Westermo.GraphX.Common;

public static class GraphXExtensions
{
    /// <param name="graph">Graph</param>
    /// <typeparam name="TVertex">Vertex data type</typeparam>
    /// <typeparam name="TEdge">Edge data type</typeparam>
    extension<TVertex, TEdge>(IBidirectionalGraph<TVertex, TEdge> graph) where TVertex : class, IGraphXVertex
        where TEdge : class, IGraphXEdge<TVertex>
    {
        /// <summary>
        /// Get all edges associated with the vertex
        /// </summary>
        /// <param name="vertex">Vertex</param>
        public IEnumerable<TEdge> GetAllEdges(TVertex vertex)
        {
            var result = new List<TEdge>();
            graph.TryGetOutEdges(vertex, out var edges);
            if (edges != null)
                result.AddRange(edges);
            graph.TryGetInEdges(vertex, out edges);
            if (edges != null)
                result.AddRange(edges);
            return result;
        }
    }

    /// <param name="graph">The graph.</param>
    extension<TVertex, TEdge>(IBidirectionalGraph<TVertex, TEdge> graph) where TEdge : IEdge<TVertex>
    {
        public IEnumerable<TEdge> GetInEdges(TVertex vertex)
        {
            var result = new List<TEdge>();
            graph.TryGetInEdges(vertex, out var edges);
            if (edges != null)
                result.AddRange(edges);
            return result;
        }

        public IEnumerable<TEdge> GetOutEdges(TVertex vertex)
        {
            var result = new List<TEdge>();
            graph.TryGetOutEdges(vertex, out var edges);
            if (edges != null)
                result.AddRange(edges);
            return result;
        }

        /// <summary>
        /// Returns with the adjacent vertices of the <code>vertex</code>.
        /// </summary>
        /// <param name="vertex">The vertex which neighbours' we want to get.</param>
        /// <returns>List of the adjacent vertices of the <code>vertex</code>.</returns>
        public IEnumerable<TVertex> GetNeighbours(TVertex vertex)
        {
            return (from e in graph.InEdges(vertex) select e.Source)
                .Concat(from e in graph.OutEdges(vertex) select e.Target)
                .Distinct();
        }
    }


    extension<TVertex, TEdge>(IVertexAndEdgeListGraph<TVertex, TEdge> g) where TEdge : IEdge<TVertex>
    {
        public IEnumerable<TVertex> GetOutNeighbours(TVertex vertex)
        {
            return (from e in g.OutEdges(vertex)
                select e.Target).Distinct();
        }
    }

    extension<TVertex, TEdge>(IBidirectionalGraph<TVertex, TEdge> g) where TEdge : IEdge<TVertex>
    {
        public IEnumerable<TVertex> GetInNeighbours(TVertex vertex)
        {
            return (from e in g.InEdges(vertex)
                select e.Source).Distinct();
        }
    }


    /// <param name="g">The graph.</param>
    /// <typeparam name="TVertex">Type of the vertex.</typeparam>
    /// <typeparam name="TEdge">Type of the edge.</typeparam>
    extension<TVertex, TEdge>(IVertexAndEdgeListGraph<TVertex, TEdge> g)
        where TVertex : class where TEdge : IEdge<TVertex>
    {
        /// <summary>
        /// If the graph g is directed, then returns every edges which source is one of the vertices in the <code>set1</code>
        /// and the target is one of the vertices in <code>set2</code>.
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <param name="undirected">Assume undirected graph - get both in and out edges</param>
        /// <returns>Return the list of the selected edges.</returns>
        public IEnumerable<TEdge> GetEdgesBetween(List<TVertex> set1, List<TVertex> set2, bool undirected = false)
        {
            var edgesBetween = new List<TEdge>();

            //vegig kell menni az osszes vertex minden elen, es megnezni, hogy a target hol van
            foreach (var v in set1)
            {
                edgesBetween.AddRange(g.OutEdges(v).Where(edge => set2.Contains(edge.Target)));
                if (undirected)
                    edgesBetween.AddRange(g.Edges.Where(a => a.Target == v).Where(edge => set2.Contains(edge.Source)));
            }

            return edgesBetween;
        }
    }


    /// <param name="g">The graph.</param>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    extension<TVertex, TEdge>(IBidirectionalGraph<TVertex, TEdge> g) where TEdge : IEdge<TVertex>
    {
        /// <summary>
        /// Returns with the sources in the graph.
        /// </summary>
        /// <returns>Returns with the sources in the graph.</returns>
        public IEnumerable<TVertex> GetSources()
        {
            return from v in g.Vertices
                where g.InDegree(v) == 0
                select v;
        }
    }

    /// <param name="g">The graph.</param>
    extension<TVertex, TEdge, TGraph>(TGraph g) where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        /// <summary>
        /// Gets the diameter of a graph.
        /// The diameter is the greatest distance between two vertices.
        /// </summary>
        /// <returns>The diameter of the Graph <code>g</code>.</returns>
        public double GetDiameter()
        {
            return g.GetDiameter<TVertex, TEdge, TGraph>(out _);
        }

        /// <summary>
        /// Gets the diameter of a graph.
        /// The diameter is the greatest distance between two vertices.
        /// </summary>
        /// <param name="distances">This is an out parameter. It gives the distances between every vertex-pair.</param>
        /// <returns>The diameter of the Graph <code>g</code>.</returns>
        public double GetDiameter(out double[,] distances)
        {
            distances = GetDistances<TVertex, TEdge, TGraph>(g);

            var n = g.VertexCount;
            var distance = double.NegativeInfinity;
            for (var i = 0; i < n - 1; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    if (Math.Abs(double.MaxValue - distances[i, j]) < 1e-12)
                        continue;

                    distance = Math.Max(distance, distances[i, j]);
                }
            }

            return distance;
        }
    }

    /// <param name="g">The graph.</param>
    /// <returns>Returns with the distance between the vertices (distance: number of the edges).</returns>
    private static double[,] GetDistances<TVertex, TEdge, TGraph>(TGraph g)
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        var distances = new double[g.VertexCount, g.VertexCount];
        for (var k = 0; k < g.VertexCount; k++)
        {
            for (var j = 0; j < g.VertexCount; j++)
            {
                distances[k, j] = double.PositiveInfinity;
            }
        }

        var undirected = new UndirectedBidirectionalGraph<TVertex, TEdge>(g);

        //compute the distances from every vertex: O(n(n^2 + e)) complexity
        var i = 0;
        try
        {
            foreach (var source in g.Vertices)
            {
                //compute the distances from the 'source'
                var spaDijkstra =
                    new UndirectedDijkstraShortestPathAlgorithm<TVertex, TEdge>(undirected, _ => 1,
                        QuikGraph.Algorithms.DistanceRelaxers.ShortestDistance);
                spaDijkstra.Compute(source);

                var j = 0;
                foreach (var v in undirected.Vertices)
                {
                    var d = spaDijkstra.GetDistance(v);
                    distances[i, j] = Math.Min(distances[i, j], d);
                    distances[i, j] = Math.Min(distances[i, j], distances[j, i]);
                    distances[j, i] = Math.Min(distances[i, j], distances[j, i]);
                    j++;
                }

                i++;
            }
        }
        catch
        {
            // ignored
        }

        return distances;
    }

    extension<TVertex>(IEdge<TVertex> edge)
    {
        public TVertex OtherVertex(TVertex thisVertex)
        {
            return edge.Source.Equals(thisVertex) ? edge.Target : edge.Source;
        }
    }


    extension<TVertex, TEdge>(IMutableEdgeListGraph<TVertex, TEdge> graph) where TEdge : IEdge<TVertex>
    {
        public void AddEdgeRange(IEnumerable<TEdge> edges)
        {
            foreach (var edge in edges)
                graph.AddEdge(edge);
        }
    }


    extension<TOldVertex, TOldEdge>(IVertexAndEdgeListGraph<TOldVertex, TOldEdge> oldGraph)
        where TOldEdge : IEdge<TOldVertex>
    {
        public BidirectionalGraph<TNewVertex, TNewEdge> Convert<TNewVertex, TNewEdge>(
            Func<TOldVertex, TNewVertex> vertexMapperFunc,
            Func<TOldEdge, TNewEdge> edgeMapperFunc) where TNewEdge : IEdge<TNewVertex>
        {
            return oldGraph.Convert(
                new BidirectionalGraph<TNewVertex, TNewEdge>(oldGraph.AllowParallelEdges, oldGraph.VertexCount),
                vertexMapperFunc,
                edgeMapperFunc);
        }

        public BidirectionalGraph<TOldVertex, TNewEdge> Convert<TNewEdge>(Func<TOldEdge, TNewEdge> edgeMapperFunc)
            where TNewEdge : IEdge<TOldVertex>
        {
            return oldGraph.Convert<TOldVertex, TOldEdge, TOldVertex, TNewEdge>(null, edgeMapperFunc);
        }

        public TNewGraph Convert<TNewVertex, TNewEdge, TNewGraph>(TNewGraph newGraph,
            Func<TOldVertex, TNewVertex> vertexMapperFunc,
            Func<TOldEdge, TNewEdge> edgeMapperFunc) where TNewEdge : IEdge<TNewVertex>
            where TNewGraph : IMutableVertexAndEdgeListGraph<TNewVertex, TNewEdge>
        {
            //VERTICES
            newGraph.AddVertexRange(vertexMapperFunc != null
                ? oldGraph.Vertices.Select(vertexMapperFunc)
                : oldGraph.Vertices.Cast<TNewVertex>());

            //EDGES
            newGraph.AddEdgeRange(edgeMapperFunc != null
                ? oldGraph.Edges.Select(edgeMapperFunc)
                : oldGraph.Edges.Cast<TNewEdge>());

            return newGraph;
        }

        public TNewGraph Convert<TNewEdge, TNewGraph>(TNewGraph newGraph,
            Func<TOldEdge, TNewEdge> edgeMapperFunc) where TNewEdge : IEdge<TOldVertex>
            where TNewGraph : IMutableVertexAndEdgeListGraph<TOldVertex, TNewEdge>
        {
            return oldGraph.Convert<TOldVertex, TOldEdge, TOldVertex, TNewEdge, TNewGraph>(newGraph, null,
                edgeMapperFunc);
        }

        public TNewGraph Convert<TNewGraph>(TNewGraph newGraph)
            where TNewGraph : IMutableVertexAndEdgeListGraph<TOldVertex, TOldEdge>
        {
            return oldGraph.Convert<TOldVertex, TOldEdge, TOldVertex, TOldEdge, TNewGraph>(newGraph, null, null);
        }

        public BidirectionalGraph<TOldVertex, TOldEdge> CopyToBidirectionalGraph(bool includeEmpty = true)
        {
            var newGraph = new BidirectionalGraph<TOldVertex, TOldEdge>();

            //copy the vertices
            if (!includeEmpty)
                newGraph.AddVerticesAndEdgeRange(oldGraph.Edges);
            else
            {
                newGraph.AddVertexRange(oldGraph.Vertices);
                newGraph.AddEdgeRange(oldGraph.Edges);
            }

            return newGraph;
        }
    }


    extension<TGraph, TVertex, TEdge>(TGraph graph) where TGraph : IMutableBidirectionalGraph<TVertex, TEdge>, new()
        where TVertex : class, IGraphXVertex
        where TEdge : class, IGraphXEdge<TVertex>
    {
        public TGraph CopyToGraph(bool includeEmpty = true)
        {
            var newGraph = new TGraph();

            //copy the vertices
            if (!includeEmpty)
                newGraph.AddVerticesAndEdgeRange(graph.Edges);
            else
            {
                newGraph.AddVertexRange(graph.Vertices);
                newGraph.AddEdgeRange(graph.Edges);
            }

            return newGraph;
        }
    }
}