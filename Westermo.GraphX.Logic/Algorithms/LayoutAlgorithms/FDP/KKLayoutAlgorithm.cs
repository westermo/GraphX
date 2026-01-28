using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class KKLayoutAlgorithm<TVertex, TEdge, TGraph>(
    TGraph visitedGraph,
    IDictionary<TVertex, Point> vertexPositions,
    KKLayoutParameters oldParameters)
    : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, KKLayoutParameters>(visitedGraph,
        vertexPositions, oldParameters)
    where TVertex : class
    where TEdge : IEdge<TVertex>
    where TGraph : IBidirectionalGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{

    #region Variables needed for the layout
    /// <summary>
    /// Minimal distances between the vertices.
    /// </summary>
    private double[] _distances;      // Flattened 2D array for better cache locality
    private double[] _edgeLengths;    // Flattened 2D array
    private double[] _springConstants; // Flattened 2D array
    private int _n;                   // Vertex count cached

    //cache for speed-up
    private TVertex[] _vertices;
    /// <summary>
    /// Positions of the vertices, stored by indices.
    /// </summary>
    private Point[] _positions;

    private double _diameter;
    private double _idealEdgeLength;
    #endregion

    #region Contructors
    public KKLayoutAlgorithm(TGraph visitedGraph, KKLayoutParameters oldParameters)
        : this(visitedGraph, null, oldParameters) { }

    #endregion

    /// <summary>
    /// Gets flattened array index for 2D access.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Idx(int i, int j) => i * _n + j;

    public override void Compute(CancellationToken cancellationToken)
    {
        if (VisitedGraph.VertexCount == 1)
        {
            VertexPositions.Add(VisitedGraph.Vertices.First(), new Point(0, 0));
            return;
        }

        _n = VisitedGraph.VertexCount;

        #region Initialization
        // Use flattened arrays for better cache locality
        var totalSize = _n * _n;
        _distances = new double[totalSize];
        _edgeLengths = new double[totalSize];
        _springConstants = new double[totalSize];
        _vertices = new TVertex[_n];
        _positions = new Point[_n];

        //initializing with random positions
        InitializeWithRandomPositions(Parameters.Width, Parameters.Height);

        //copy positions into array (speed-up)
        // Also build vertex index map for fast lookup
        var vertexToIndex = new Dictionary<TVertex, int>(_n);
        var index = 0;
        foreach (var v in VisitedGraph.Vertices)
        {
            _vertices[index] = v;
            _positions[index] = VertexPositions[v];
            vertexToIndex[v] = index;
            index++;
        }

        //calculating the diameter of the graph using optimized BFS-based shortest paths
        _diameter = ComputeShortestPathsAndDiameter(vertexToIndex, cancellationToken);

        //L0 is the length of a side of the display area
        var l0 = Math.Min(Parameters.Width, Parameters.Height);

        //ideal length = L0 / max d_i,j
        _idealEdgeLength = l0 / _diameter * Parameters.LengthFactor;

        //calculating the ideal distance between the nodes
        var disconnectedDist = _diameter * Parameters.DisconnectedMultiplier;
        var kParam = Parameters.K;
        for (var i = 0; i < _n - 1; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var j = i + 1; j < _n; j++)
            {
                var idxIJ = Idx(i, j);
                var idxJI = Idx(j, i);

                //distance between non-adjacent vertices
                var dist = disconnectedDist;

                //calculating the minimal distance between the vertices
                var distIJ = _distances[idxIJ];
                var distJI = _distances[idxJI];
                if (distIJ < double.MaxValue)
                    dist = Math.Min(distIJ, dist);
                if (distJI < double.MaxValue)
                    dist = Math.Min(distJI, dist);

                _distances[idxIJ] = _distances[idxJI] = dist;

                var edgeLen = _idealEdgeLength * dist;
                _edgeLengths[idxIJ] = _edgeLengths[idxJI] = edgeLen;

                // Avoid Math.Pow for squaring
                var distSquared = dist * dist;
                var springK = kParam / distSquared;
                _springConstants[idxIJ] = _springConstants[idxJI] = springK;
            }
        }
        #endregion

        if (_n == 0)
            return;

        //TODO check this condition
        for (var currentIteration = 0; currentIteration < Parameters.MaxIterations; currentIteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            #region An iteration
            var maxDeltaM = double.NegativeInfinity;
            var pm = -1;

            //get the 'p' with the max delta_m
            for (var i = 0; i < _n; i++)
            {
                var deltaM = CalculateEnergyGradient(i);
                if (maxDeltaM < deltaM)
                {
                    maxDeltaM = deltaM;
                    pm = i;
                }
            }
            //TODO is needed?
            if (pm == -1)
                return;

            //calculating the delta_x & delta_y with the Newton-Raphson method
            //there is an upper-bound for the while (deltaM > epsilon) {...} cycle (100)
            for (var i = 0; i < 100; i++)
            {
                _positions[pm] += CalcDeltaXY(pm);

                var deltaM = CalculateEnergyGradient(pm);
                //real stop condition
                if (deltaM < double.Epsilon)
                    break;
            }

            //what if some of the vertices would be exchanged?
            if (Parameters.ExchangeVertices && maxDeltaM < double.Epsilon)
            {
                var energy = CalcEnergy();
                for (var i = 0; i < _n - 1; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    for (var j = i + 1; j < _n; j++)
                    {
                        var xenergy = CalcEnergyIfExchanged(i, j);
                        if (energy > xenergy)
                        {
                            var p = _positions[i];
                            _positions[i] = _positions[j];
                            _positions[j] = p;
                            return;
                        }
                    }
                }
            }
            #endregion

            /* if ( ReportOnIterationEndNeeded )
                 Report( currentIteration );*/
        }
        Report(Parameters.MaxIterations);
    }

    /// <summary>
    /// Computes shortest paths using BFS (O(V*(V+E)) instead of Dijkstra (O(V*(V+E)*log V)).
    /// Returns the graph diameter.
    /// </summary>
    private double ComputeShortestPathsAndDiameter(Dictionary<TVertex, int> vertexToIndex, CancellationToken cancellationToken)
    {
        // Initialize distances to infinity
        for (var i = 0; i < _distances.Length; i++)
            _distances[i] = double.PositiveInfinity;

        // Set diagonal to 0
        for (var i = 0; i < _n; i++)
            _distances[Idx(i, i)] = 0;

        // Build adjacency list for faster BFS
        var adjacency = new List<int>[_n];
        for (var i = 0; i < _n; i++)
            adjacency[i] = new List<int>();

        foreach (var edge in VisitedGraph.Edges)
        {
            var srcIdx = vertexToIndex[edge.Source];
            var tgtIdx = vertexToIndex[edge.Target];
            adjacency[srcIdx].Add(tgtIdx);
            adjacency[tgtIdx].Add(srcIdx); // Treat as undirected for distance calculation
        }

        // BFS from each vertex
        var queue = new Queue<int>(_n);
        var visited = new bool[_n];

        for (var source = 0; source < _n; source++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Reset visited array
            Array.Clear(visited, 0, _n);
            queue.Clear();

            queue.Enqueue(source);
            visited[source] = true;
            _distances[Idx(source, source)] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDist = _distances[Idx(source, current)];

                foreach (var neighbor in adjacency[current])
                {
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        _distances[Idx(source, neighbor)] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Find diameter (max finite distance)
        var diameter = 0.0;
        for (var i = 0; i < _n - 1; i++)
        {
            for (var j = i + 1; j < _n; j++)
            {
                var dist = _distances[Idx(i, j)];
                if (dist < double.PositiveInfinity && dist > diameter)
                    diameter = dist;
            }
        }

        return diameter > 0 ? diameter : 1.0;
    }

    public override void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
    {
        VisitedGraph.Clear();
        VisitedGraph.AddVertexRange(vertices);
        VisitedGraph.AddEdgeRange(edges);
    }

    protected void Report(int currentIteration)
    {
        #region Copy the calculated positions
        for (var i = 0; i < _vertices.Length; i++)
            VertexPositions[_vertices[i]] = _positions[i];
        #endregion

        //OnIterationEnded( currentIteration, (double)currentIteration / (double)Parameters.MaxIterations, "Iteration " + currentIteration + " finished.", true );
    }

    /// <returns>
    /// Calculates the energy of the state where 
    /// the positions of the vertex 'p' & 'q' are exchanged.
    /// </returns>
    private double CalcEnergyIfExchanged(int p, int q)
    {
        double energy = 0;
        for (var i = 0; i < _n - 1; i++)
        {
            for (var j = i + 1; j < _n; j++)
            {
                var ii = i == p ? q : i;
                var jj = j == q ? p : j;

                var idx = Idx(i, j);
                var lIj = _edgeLengths[idx];
                var kIj = _springConstants[idx];
                var dx = _positions[ii].X - _positions[jj].X;
                var dy = _positions[ii].Y - _positions[jj].Y;
                var distSq = dx * dx + dy * dy;
                var dist = Math.Sqrt(distSq);

                energy += kIj * 0.5 * (distSq + lIj * lIj - 2 * lIj * dist);
            }
        }
        return energy;
    }

    /// <summary>
    /// Calculates the energy of the spring system.
    /// </summary>
    /// <returns>Returns with the energy of the spring system.</returns>
    private double CalcEnergy()
    {
        double energy = 0;
        for (var i = 0; i < _n - 1; i++)
        {
            for (var j = i + 1; j < _n; j++)
            {
                var idx = Idx(i, j);
                var lIj = _edgeLengths[idx];
                var kIj = _springConstants[idx];

                var dx = _positions[i].X - _positions[j].X;
                var dy = _positions[i].Y - _positions[j].Y;
                var distSq = dx * dx + dy * dy;
                var dist = Math.Sqrt(distSq);

                energy += kIj * 0.5 * (distSq + lIj * lIj - 2 * lIj * dist);
            }
        }
        return energy;
    }

    /// <summary>
    /// Determines a step to new position of the vertex m.
    /// </summary>
    /// <returns></returns>
    private Vector CalcDeltaXY(int m)
    {
        double dxm = 0, dym = 0, d2Xm = 0, dxmdym = 0, d2Ym = 0;
        var posM = _positions[m];

        for (var i = 0; i < _n; i++)
        {
            if (i != m)
            {
                var idx = Idx(m, i);
                var l = _edgeLengths[idx];
                var k = _springConstants[idx];
                var dx = posM.X - _positions[i].X;
                var dy = posM.Y - _positions[i].Y;

                //distance between the points
                var distSq = dx * dx + dy * dy;
                var d = Math.Sqrt(distSq);
                var ddd = d * distSq;  // d^3 = d * d^2, avoid Math.Pow

                var factor = 1 - l / d;
                dxm += k * factor * dx;
                dym += k * factor * dy;

                var dySq = dy * dy;
                var dxSq = dx * dx;
                d2Xm += k * (1 - l * dySq / ddd);
                dxmdym += k * l * dx * dy / ddd;
                d2Ym += k * (1 - l * dxSq / ddd);
            }
        }
        // d2E_dymdxm equals to d2E_dxmdym
        var dymdxm = dxmdym;

        var denomi = d2Xm * d2Ym - dxmdym * dymdxm;
        var deltaX = (dxmdym * dym - d2Ym * dxm) / denomi;
        var deltaY = (dymdxm * dxm - d2Xm * dym) / denomi;
        return new Vector(deltaX, deltaY);
    }

    /// <summary>
    /// Calculates the gradient energy of a vertex.
    /// </summary>
    /// <param name="m">The index of the vertex.</param>
    /// <returns>Calculates the gradient energy of the vertex <code>m</code>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double CalculateEnergyGradient(int m)
    {
        double dxm = 0, dym = 0;
        var posM = _positions[m];

        for (var i = 0; i < _n; i++)
        {
            if (i == m)
                continue;

            var dx = posM.X - _positions[i].X;
            var dy = posM.Y - _positions[i].Y;
            var distSq = dx * dx + dy * dy;
            var d = Math.Sqrt(distSq);

            var idx = Idx(m, i);
            var common = _springConstants[idx] * (1 - _edgeLengths[idx] / d);
            dxm += common * dx;
            dym += common * dy;
        }
        // delta_m = sqrt((dE/dx)^2 + (dE/dy)^2)
        return Math.Sqrt(dxm * dxm + dym * dym);
    }
}