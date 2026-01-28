using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Westermo.GraphX.Measure;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class FRLayoutAlgorithm<TVertex, TEdge, TGraph> : ParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, FRLayoutParametersBase>
    where TVertex : class
    where TEdge : IEdge<TVertex>
    where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{
    /// <summary>
    /// Actual temperature of the 'mass'.
    /// </summary>
    private double _temperature;

    private double _maxWidth = double.PositiveInfinity;
    private double _maxHeight = double.PositiveInfinity;

    // Cached arrays to avoid per-iteration allocations
    private TVertex[] _vertexArray = null!;
    private Point[] _positions = null!;
    private Vector[] _forces = null!;
    private Dictionary<TVertex, int> _vertexIndices = null!;

    protected override FRLayoutParametersBase DefaultParameters => new FreeFRLayoutParameters();

    #region Constructors
    public FRLayoutAlgorithm(TGraph visitedGraph)
        : base(visitedGraph) { }

    public FRLayoutAlgorithm(TGraph visitedGraph, IDictionary<TVertex, Point> vertexPositions, FRLayoutParametersBase parameters)
        : base(visitedGraph, vertexPositions, parameters) { }
    #endregion

    /// <summary>
    /// It computes the layout of the vertices.
    /// </summary>
    public override void Compute(CancellationToken cancellationToken)
    {
        var n = VisitedGraph.VertexCount;
        if (n == 0)
            return;
            
        if (n == 1)
        {
            VertexPositions.Add(VisitedGraph.Vertices.First(), new Point(0, 0));
            return;
        }

        //initializing the positions
        if (Parameters is BoundedFRLayoutParameters)
        {
            var param = Parameters as BoundedFRLayoutParameters;
            InitializeWithRandomPositions(param.Width, param.Height);
            _maxWidth = param.Width;
            _maxHeight = param.Height;
        }
        else
        {
            InitializeWithRandomPositions(10.0, 10.0);
        }
        Parameters.VertexCount = n;

        // Initialize cached arrays (allocated once, reused every iteration)
        _vertexArray = new TVertex[n];
        _positions = new Point[n];
        _forces = new Vector[n];
        _vertexIndices = new Dictionary<TVertex, int>(n);

        var idx = 0;
        foreach (var v in VisitedGraph.Vertices)
        {
            _vertexArray[idx] = v;
            _vertexIndices[v] = idx;
            _positions[idx] = VertexPositions[v];
            idx++;
        }

        // Actual temperature of the 'mass'. Used for cooling.
        var minimalTemperature = Parameters.InitialTemperature * 0.01;
        _temperature = Parameters.InitialTemperature;
        for (var i = 0;
             i < Parameters._iterationLimit
             && _temperature > minimalTemperature;
             i++)
        {
            IterateOne(cancellationToken);

            //make some cooling
            switch (Parameters._coolingFunction)
            {
                case FRCoolingFunction.Linear:
                    _temperature *= 1.0 - i / (double)Parameters._iterationLimit;
                    break;
                case FRCoolingFunction.Exponential:
                    _temperature *= Parameters._lambda;
                    break;
            }
        }

        // Copy final positions back to VertexPositions
        for (var i = 0; i < n; i++)
        {
            VertexPositions[_vertexArray[i]] = _positions[i];
        }
    }

    public override void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
    {
        VisitedGraph.Clear();
        VisitedGraph.AddVertexRange(vertices);
        VisitedGraph.AddEdgeRange(edges);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector SafeDelta(double dx, double dy, double length)
    {
        if (double.IsNaN(dx) || double.IsNaN(dy) || length < double.Epsilon)
            return new Vector(0, 0);
        return new Vector(dx, dy);
    }

    protected void IterateOne(CancellationToken cancellationToken)
    {
        var n = _vertexArray.Length;
        var repulsionConstant = Parameters.ConstantOfRepulsion;
        var attractionConstant = Parameters.ConstantOfAttraction;

        // Reset forces (reuse array, avoid allocation)
        for (var i = 0; i < n; i++)
        {
            _forces[i] = new Vector(0, 0);
        }

        #region Repulsive forces - O(n²) but with array access instead of dictionary
        for (var i = 0; i < n; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var posI = _positions[i];
            var forceX = 0.0;
            var forceY = 0.0;

            for (var j = 0; j < n; j++)
            {
                if (i == j)
                    continue;

                var posJ = _positions[j];
                var dx = posI.X - posJ.X;
                var dy = posI.Y - posJ.Y;
                var distSquared = dx * dx + dy * dy;
                var dist = Math.Max(Math.Sqrt(distSquared), double.Epsilon);
                
                // Repulsion force: k²/d
                var force = repulsionConstant / dist;
                forceX += dx / dist * force;
                forceY += dy / dist * force;
            }

            if (!double.IsNaN(forceX) && !double.IsNaN(forceY))
            {
                _forces[i] = new Vector(forceX, forceY);
            }
        }
        #endregion

        #region Attractive forces
        foreach (var e in VisitedGraph.Edges)
        {
            var sourceIdx = _vertexIndices[e.Source];
            var targetIdx = _vertexIndices[e.Target];

            var posS = _positions[sourceIdx];
            var posT = _positions[targetIdx];
            var dx = posS.X - posT.X;
            var dy = posS.Y - posT.Y;
            var distSquared = dx * dx + dy * dy;
            var dist = Math.Max(Math.Sqrt(distSquared), double.Epsilon);

            // Attraction force: d²/k
            var force = distSquared / attractionConstant;
            var fx = dx / dist * force;
            var fy = dy / dist * force;

            if (!double.IsNaN(fx) && !double.IsNaN(fy))
            {
                _forces[sourceIdx].X -= fx;
                _forces[sourceIdx].Y -= fy;
                _forces[targetIdx].X += fx;
                _forces[targetIdx].Y += fy;
            }
        }
        #endregion

        #region Limit displacement and apply forces
        for (var i = 0; i < n; i++)
        {
            var delta = _forces[i];
            var length = Math.Max(delta.Length, double.Epsilon);
            
            // Limit by temperature
            var limitedLength = Math.Min(length, _temperature);
            delta = new Vector(delta.X / length * limitedLength, delta.Y / length * limitedLength);

            if (double.IsNaN(delta.X) || double.IsNaN(delta.Y))
                delta = new Vector(0, 0);

            // Apply force
            var pos = _positions[i];
            pos.X += delta.X;
            pos.Y += delta.Y;

            // Clamp to bounds
            pos.X = Math.Min(_maxWidth, Math.Max(0, pos.X));
            pos.Y = Math.Min(_maxHeight, Math.Max(0, pos.Y));
            
            _positions[i] = pos;
        }
        #endregion
    }
}