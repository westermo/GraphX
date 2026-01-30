using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class ISOMLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, ISOMLayoutParameters>
	where TVertex : class
	where TEdge : IEdge<TVertex>
	where TGraph : IBidirectionalGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{
	#region Private fields
	private Queue<int> _queue = null!;
	private Point _tempPos;
	private double _adaptation;
	private int _radius;
	
	// Array-based storage for better performance
	private TVertex[] _vertices = null!;
	private Point[] _positions = null!;
	private bool[] _visited = null!;
	private int[] _distance = null!;
	private Dictionary<TVertex, int> _vertexToIndex = null!;
	private List<int>[] _adjacency = null!;
	#endregion

	#region Constructors

	public ISOMLayoutAlgorithm( TGraph visitedGraph, ISOMLayoutParameters oldParameters )
		: base( visitedGraph )
	{
		Init( oldParameters );
	}

	public ISOMLayoutAlgorithm( TGraph visitedGraph, IDictionary<TVertex, Point> vertexPositions,
		ISOMLayoutParameters oldParameters )
		: base( visitedGraph, vertexPositions, oldParameters )
	{
		Init( oldParameters );
	}

	protected void Init( ISOMLayoutParameters oldParameters )
	{
		//init _parameters
		InitParameters( oldParameters );
		_adaptation = Parameters.InitialAdaption;
	}
	#endregion

	public override void Compute(CancellationToken cancellationToken)
	{
		var n = VisitedGraph.VertexCount;
		if (n == 0)
			return;
			
		if (n == 1)
		{
			if(!VertexPositions.ContainsKey(VisitedGraph.Vertices.First()))
				VertexPositions.Add(VisitedGraph.Vertices.First(), new Point(0, 0));
			return;
		}

		//initialize vertex positions
		InitializeWithRandomPositions( Parameters.Width, Parameters.Height );

		// Initialize arrays once
		_vertices = new TVertex[n];
		_positions = new Point[n];
		_visited = new bool[n];
		_distance = new int[n];
		_vertexToIndex = new Dictionary<TVertex, int>(n);
		_adjacency = new List<int>[n];
		_queue = new Queue<int>(n);

		var idx = 0;
		foreach (var vertex in VisitedGraph.Vertices)
		{
			_vertices[idx] = vertex;
			_positions[idx] = VertexPositions[vertex];
			_vertexToIndex[vertex] = idx;
			_adjacency[idx] = new List<int>();
			idx++;
		}

		// Build adjacency list
		foreach (var edge in VisitedGraph.Edges)
		{
			var srcIdx = _vertexToIndex[edge.Source];
			var tgtIdx = _vertexToIndex[edge.Target];
			_adjacency[srcIdx].Add(tgtIdx);
			_adjacency[tgtIdx].Add(srcIdx);
		}

		_radius = Parameters.InitialRadius;
		var rnd = new Random(Parameters.Seed);
		var maxEpoch = Parameters.MaxEpoch;
		var coolingFactor = Parameters.CoolingFactor;
		var minAdaption = Parameters.MinAdaption;
		var initialAdaption = Parameters.InitialAdaption;
		var radiusConstantTime = Parameters.RadiusConstantTime;
		var minRadius = Parameters.MinRadius;
		
		for ( var epoch = 0; epoch < maxEpoch; epoch++ )
		{
			cancellationToken.ThrowIfCancellationRequested();

			Adjust(rnd);

			//Update Parameters
			var factor = Math.Exp( -coolingFactor * ( 1.0 * epoch / maxEpoch ) );
			_adaptation = Math.Max( minAdaption, factor * initialAdaption );
			if ( _radius > minRadius && epoch % radiusConstantTime == 0 )
			{
				_radius--;
			}
		}

		// Copy positions back
		for (var i = 0; i < n; i++)
		{
			VertexPositions[_vertices[i]] = _positions[i];
		}
	}

	public override void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
	{
		VisitedGraph.Clear();
		VisitedGraph.AddVertexRange(vertices);
		VisitedGraph.AddEdgeRange(edges);
	}

	/// <summary>
	/// Adjust all vertices once per epoch.
	/// </summary>
	protected void Adjust(Random rnd)
	{
		_tempPos = new Point {
			X = 0.1 * Parameters.Width + rnd.NextDouble() * 0.8 * Parameters.Width, 
			Y = 0.1 * Parameters.Height + rnd.NextDouble() * 0.8 * Parameters.Height
		};

		//find the closest vertex index to this random point
		var closestIdx = GetClosestIndex(_tempPos);

		//reset visited and distance arrays
		Array.Clear(_visited, 0, _visited.Length);
		Array.Clear(_distance, 0, _distance.Length);
		
		AdjustVertex(closestIdx);
	}

	private void AdjustVertex(int closestIdx)
	{
		_queue.Clear();
		_distance[closestIdx] = 0;
		_visited[closestIdx] = true;
		_queue.Enqueue(closestIdx);

		while (_queue.Count > 0)
		{
			var currentIdx = _queue.Dequeue();
			var currentDist = _distance[currentIdx];
			var pos = _positions[currentIdx];

			var forceX = _tempPos.X - pos.X;
			var forceY = _tempPos.Y - pos.Y;
			
			// Use bit shift for power of 2: 2^distance
			var divisor = 1 << currentDist;
			var factor = _adaptation / divisor;

			pos.X += factor * forceX;
			pos.Y += factor * forceY;
			_positions[currentIdx] = pos;

			// If still within radius, propagate to neighbors
			if (currentDist < _radius)
			{
				foreach (var neighborIdx in _adjacency[currentIdx])
				{
					if (!_visited[neighborIdx])
					{
						_visited[neighborIdx] = true;
						_distance[neighborIdx] = currentDist + 1;
						_queue.Enqueue(neighborIdx);
					}
				}
			}
		}
	}

	/// <summary>
	/// Finds the closest vertex index to the given position.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetClosestIndex(Point tempPos)
	{
		var closestIdx = 0;
		var minDistSq = double.MaxValue;

		for (var i = 0; i < _positions.Length; i++)
		{
			var dx = tempPos.X - _positions[i].X;
			var dy = tempPos.Y - _positions[i].Y;
			var distSq = dx * dx + dy * dy;  // Compare squared distances to avoid sqrt
			if (distSq < minDistSq)
			{
				closestIdx = i;
				minDistSq = distSq;
			}
		}
		return closestIdx;
	}
}