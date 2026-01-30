#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Models;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public partial class LinLogLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, LinLogLayoutParameters>
	where TVertex : class
	where TEdge : IEdge<TVertex>
	where TGraph : IBidirectionalGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{
	#region Constructors

	public LinLogLayoutAlgorithm(TGraph visitedGraph)
		: base(visitedGraph)
	{
		EnsureUniqueRandomInitialPositions = true;
	}

	public LinLogLayoutAlgorithm( TGraph visitedGraph, IDictionary<TVertex, Point> positions,
		LinLogLayoutParameters parameters )
		: base( visitedGraph, positions, parameters ) 
	{ 
		EnsureUniqueRandomInitialPositions = true;
	}
	#endregion

	#region Member variables - privates

	// Use struct to avoid heap allocations for vertices
	private struct LinLogVertexData
	{
		public TVertex OriginalVertex;
		public int AttractionStart;  // Index into _allAttractions array
		public int AttractionCount;
		public double RepulsionWeight;
		public double PositionX;
		public double PositionY;
	}

	// Use struct for edges too
	private struct LinLogEdgeData
	{
		public int TargetIndex;
		public double AttractionWeight;
	}

	private LinLogVertexData[] _vertices = null!;
	private LinLogEdgeData[] _allAttractions = null!;  // Flat array of all edges
	private double _baryCenterX;
	private double _baryCenterY;
	private double _repulsionMultiplier;
	
	// Cached parameter values to avoid property access in hot loops
	private double _attractionExponent;
	private double _repulsiveExponent;
	private double _gravitationMultiplier;
	
	// Pooled QuadTree to avoid per-iteration allocations
	private QuadTreePool _quadTreePool = null!;

	#endregion


	public override void Compute(CancellationToken cancellationToken)
	{
		switch (VisitedGraph.VertexCount)
		{
			case 0:
				return;
			case 1:
				VertexPositions.Add(VisitedGraph.Vertices.First(), new Point(0, 0));
				return;
		}

		InitializeWithRandomPositions( 1, 1, -0.5, -0.5 );

		InitAlgorithm();

		var finalRepuExponent = Parameters.repulsiveExponent;
		var finalAttrExponent = Parameters.attractionExponent;
		var iterationCount = Parameters.iterationCount;
		var threshold60 = 0.6 * iterationCount;
		var threshold90 = 0.9 * iterationCount;

		for ( var step = 1; step <= iterationCount; step++ )
		{
			cancellationToken.ThrowIfCancellationRequested();

			ComputeBaryCenter();
			_quadTreePool.Reset();
			var quadTree = BuildQuadTree();
			var quadTreeWidth = quadTree?.Width ?? 1.0;

			#region Cooling function
			if ( iterationCount >= 50 && finalRepuExponent < 1.0 )
			{
				_attractionExponent = finalAttrExponent;
				_repulsiveExponent = finalRepuExponent;
				if ( step <= threshold60 )
				{
					// use energy model with few local minima 
					_attractionExponent += 1.1 * ( 1.0 - finalRepuExponent );
					_repulsiveExponent += 0.9 * ( 1.0 - finalRepuExponent );
				}
				else if ( step <= threshold90 )
				{
					// gradually move to final energy model
					var factor = ( 0.9 - step / (double)iterationCount ) / 0.3;
					_attractionExponent += 1.1 * ( 1.0 - finalRepuExponent ) * factor;
					_repulsiveExponent += 0.9 * ( 1.0 - finalRepuExponent ) * factor;
				}
			}
			#endregion

			#region Move each node
			var n = _vertices.Length;
			for ( var i = 0; i < n; i++ )
			{
				ref var v = ref _vertices[i];
				var oldEnergy = GetEnergy( i, quadTree );

				// compute direction of the move of the node
				GetDirection( i, quadTree, quadTreeWidth, out var bestDirX, out var bestDirY );

				// line search: compute length of the move
				var oldPosX = v.PositionX;
				var oldPosY = v.PositionY;

				var bestEnergy = oldEnergy;
				var bestMultiple = 0;
				bestDirX /= 32;
				bestDirY /= 32;
				
				// Search smaller movements
				for ( var multiple = 32;
				     multiple >= 1 && ( bestMultiple == 0 || bestMultiple / 2 == multiple );
				     multiple /= 2 )
				{
					v.PositionX = oldPosX + bestDirX * multiple;
					v.PositionY = oldPosY + bestDirY * multiple;
					var curEnergy = GetEnergy( i, quadTree );
					if ( curEnergy < bestEnergy )
					{
						bestEnergy = curEnergy;
						bestMultiple = multiple;
					}
				}

				// Search larger movements
				for ( var multiple = 64;
				     multiple <= 128 && bestMultiple == multiple / 2;
				     multiple *= 2 )
				{
					v.PositionX = oldPosX + bestDirX * multiple;
					v.PositionY = oldPosY + bestDirY * multiple;
					var curEnergy = GetEnergy( i, quadTree );
					if ( curEnergy < bestEnergy )
					{
						bestEnergy = curEnergy;
						bestMultiple = multiple;
					}
				}

				// Apply best movement
				var newPosX = oldPosX + bestDirX * bestMultiple;
				var newPosY = oldPosY + bestDirY * bestMultiple;
				v.PositionX = newPosX;
				v.PositionY = newPosY;
				if ( bestMultiple > 0 && quadTree != null )
				{
					quadTree.MoveNode( oldPosX, oldPosY, newPosX, newPosY, v.RepulsionWeight );
				}
			}
			#endregion
		}
		CopyPositions();
		NormalizePositions();
	}

	public override void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
	{
		VisitedGraph.Clear();
		VisitedGraph.AddVertexRange(vertices);
		VisitedGraph.AddEdgeRange(edges);
	}

	protected void CopyPositions()
	{
		// Copy positions
		for (var i = 0; i < _vertices.Length; i++)
		{
			ref var v = ref _vertices[i];
			VertexPositions[v.OriginalVertex] = new Point(v.PositionX, v.PositionY);
		}
	}

	protected void Report( int step )
	{
		CopyPositions();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void GetDirection( int index, QuadTreeNode? quadTree, double quadTreeWidth, out double dirX, out double dirY )
	{
		dirX = 0;
		dirY = 0;

		var dir2 = AddRepulsionDirection( index, quadTree, ref dirX, ref dirY );
		dir2 += AddAttractionDirection( index, ref dirX, ref dirY );
		dir2 += AddGravitationDirection( index, ref dirX, ref dirY );

		if ( dir2 != 0.0 )
		{
			dirX /= dir2;
			dirY /= dir2;

			var lengthSq = dirX * dirX + dirY * dirY;
			var maxLen = quadTreeWidth / 8;
			var maxLenSq = maxLen * maxLen;
			if ( lengthSq > maxLenSq )
			{
				var scale = maxLen / Math.Sqrt(lengthSq);
				dirX *= scale;
				dirY *= scale;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double AddGravitationDirection( int index, ref double dirX, ref double dirY )
	{
		ref var v = ref _vertices[index];
		var gx = _baryCenterX - v.PositionX;
		var gy = _baryCenterY - v.PositionY;
		var distSq = gx * gx + gy * gy;
		var dist = Math.Sqrt(distSq);
		
		var exponent = _attractionExponent - 2;
		var tmp = _gravitationMultiplier * _repulsionMultiplier * Math.Max( v.RepulsionWeight, 1 ) * FastPow( dist, exponent );
		dirX += gx * tmp;
		dirY += gy * tmp;

		return tmp * Math.Abs( _attractionExponent - 1 );
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double AddAttractionDirection( int index, ref double dirX, ref double dirY )
	{
		var dir2 = 0.0;
		ref var v = ref _vertices[index];
		var vPosX = v.PositionX;
		var vPosY = v.PositionY;
		var attrExp = _attractionExponent;
		var attrExpMinus2 = attrExp - 2;
		var absAttrExpMinus1 = Math.Abs(attrExp - 1);
		
		var attrStart = v.AttractionStart;
		var attrEnd = attrStart + v.AttractionCount;
		
		for (var i = attrStart; i < attrEnd; i++)
		{
			ref var e = ref _allAttractions[i];
			var targetIdx = e.TargetIndex;
			
			// Skip self-loops
			if ( targetIdx == index )
				continue;

			ref var target = ref _vertices[targetIdx];
			var ax = target.PositionX - vPosX;
			var ay = target.PositionY - vPosY;
			var distSq = ax * ax + ay * ay;
			if ( distSq <= 0 )
				continue;

			var dist = Math.Sqrt(distSq);
			var tmp = e.AttractionWeight * FastPow( dist, attrExpMinus2 );
			dir2 += tmp * absAttrExpMinus1;

			dirX += ax * tmp;
			dirY += ay * tmp;
		}
		return dir2;
	}

	/// <summary>
	/// Computes repulsion direction using quadtree for Barnes-Hut approximation.
	/// </summary>
	private double AddRepulsionDirection( int index, QuadTreeNode? quadTree, ref double dirX, ref double dirY )
	{
		ref var v = ref _vertices[index];

		if ( quadTree == null || quadTree.Index == index || v.RepulsionWeight <= 0 )
			return 0.0;

		var vPosX = v.PositionX;
		var vPosY = v.PositionY;
		var rx = quadTree.PositionX - vPosX;
		var ry = quadTree.PositionY - vPosY;
		var distSq = rx * rx + ry * ry;
		var dist = Math.Sqrt(distSq);
		
		if ( quadTree.Index < 0 && dist < 2.0 * quadTree.Width )
		{
			var dir2 = 0.0;
			if (quadTree.Child0 != null)
				dir2 += AddRepulsionDirection( index, quadTree.Child0, ref dirX, ref dirY );
			if (quadTree.Child1 != null)
				dir2 += AddRepulsionDirection( index, quadTree.Child1, ref dirX, ref dirY );
			if (quadTree.Child2 != null)
				dir2 += AddRepulsionDirection( index, quadTree.Child2, ref dirX, ref dirY );
			if (quadTree.Child3 != null)
				dir2 += AddRepulsionDirection( index, quadTree.Child3, ref dirX, ref dirY );
			return dir2;
		}

		if ( dist > 0.0 )
		{
			var tmp = _repulsionMultiplier * v.RepulsionWeight * quadTree.Weight
			          * FastPow( dist, _repulsiveExponent - 2 );
			dirX -= rx * tmp;
			dirY -= ry * tmp;
			return tmp * Math.Abs( _repulsiveExponent - 1 );
		}

		return 0.0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double GetEnergy( int index, QuadTreeNode? q )
	{
		return GetRepulsionEnergy( index, q )
		       + GetAttractionEnergy( index ) + GetGravitationEnergy( index );
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double GetGravitationEnergy( int index )
	{
		ref var v = ref _vertices[index];

		var dx = v.PositionX - _baryCenterX;
		var dy = v.PositionY - _baryCenterY;
		var dist = Math.Sqrt(dx * dx + dy * dy);
		return _gravitationMultiplier * _repulsionMultiplier * Math.Max( v.RepulsionWeight, 1 )
			* FastPow( dist, _attractionExponent ) / _attractionExponent;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private double GetAttractionEnergy( int index )
	{
		var energy = 0.0;
		ref var v = ref _vertices[index];
		var vPosX = v.PositionX;
		var vPosY = v.PositionY;
		var attrExp = _attractionExponent;
		
		var attrStart = v.AttractionStart;
		var attrEnd = attrStart + v.AttractionCount;
		
		for (var i = attrStart; i < attrEnd; i++)
		{
			ref var e = ref _allAttractions[i];
			if ( e.TargetIndex == index )
				continue;

			ref var target = ref _vertices[e.TargetIndex];
			var dx = target.PositionX - vPosX;
			var dy = target.PositionY - vPosY;
			var dist = Math.Sqrt(dx * dx + dy * dy);
			energy += e.AttractionWeight * FastPow( dist, attrExp ) / attrExp;
		}
		return energy;
	}

	private double GetRepulsionEnergy( int index, QuadTreeNode? tree )
	{
		if ( tree == null || tree.Index == index || index >= _vertices.Length )
			return 0.0;

		ref var v = ref _vertices[index];
		var dx = v.PositionX - tree.PositionX;
		var dy = v.PositionY - tree.PositionY;
		var dist = Math.Sqrt(dx * dx + dy * dy);
		
		if ( tree.Index < 0 && dist < 2 * tree.Width )
		{
			var energy = 0.0;
			if (tree.Child0 != null)
				energy += GetRepulsionEnergy( index, tree.Child0 );
			if (tree.Child1 != null)
				energy += GetRepulsionEnergy( index, tree.Child1 );
			if (tree.Child2 != null)
				energy += GetRepulsionEnergy( index, tree.Child2 );
			if (tree.Child3 != null)
				energy += GetRepulsionEnergy( index, tree.Child3 );
			return energy;
		}

		if ( _repulsiveExponent == 0.0 )
			return -_repulsionMultiplier * v.RepulsionWeight * tree.Weight * Math.Log( dist );

		return -_repulsionMultiplier * v.RepulsionWeight * tree.Weight
			* FastPow( dist, _repulsiveExponent ) / _repulsiveExponent;
	}

	/// <summary>
	/// Fast power function that optimizes common cases.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double FastPow(double baseVal, double exponent)
	{
		// Common exponent optimizations
		if (exponent == 0.0) return 1.0;
		if (exponent == 1.0) return baseVal;
		if (exponent == -1.0) return 1.0 / baseVal;
		if (exponent == 2.0) return baseVal * baseVal;
		if (exponent == -2.0) { var inv = 1.0 / baseVal; return inv * inv; }
		if (exponent == 0.5) return Math.Sqrt(baseVal);
		if (exponent == -0.5) return 1.0 / Math.Sqrt(baseVal);
		return Math.Pow(baseVal, exponent);
	}

	private void InitAlgorithm()
	{
		var n = VisitedGraph.VertexCount;
		_vertices = new LinLogVertexData[n];
		
		// Initialize QuadTree pool - estimate 4 nodes per vertex for tree structure
		_quadTreePool = new QuadTreePool(n * 4);

		// Build vertex index map
		var vertexToIndex = new Dictionary<TVertex, int>(n);
		var idx = 0;
		foreach (var v in VisitedGraph.Vertices)
		{
			vertexToIndex[v] = idx;
			idx++;
		}

		// Count total edges for flat array allocation
		var totalEdges = 0;
		foreach (var v in VisitedGraph.Vertices)
		{
			totalEdges += VisitedGraph.Degree(v);
		}
		_allAttractions = new LinLogEdgeData[totalEdges];

		// Initialize vertices and edges
		var attrIdx = 0;
		idx = 0;
		foreach (var v in VisitedGraph.Vertices)
		{
			var pos = VertexPositions[v];
			var degree = VisitedGraph.Degree(v);
			
			_vertices[idx] = new LinLogVertexData
			{
				OriginalVertex = v,
				AttractionStart = attrIdx,
				AttractionCount = degree,
				RepulsionWeight = 0,
				PositionX = pos.X,
				PositionY = pos.Y
			};

			// Add in-edges
			foreach (var e in VisitedGraph.InEdges(v))
			{
				var weight = e is WeightedEdge<TVertex> we ? we.Weight : 1.0;
				_allAttractions[attrIdx] = new LinLogEdgeData
				{
					TargetIndex = vertexToIndex[e.Source],
					AttractionWeight = weight
				};
				_vertices[idx].RepulsionWeight += 1;
				attrIdx++;
			}

			// Add out-edges
			foreach (var e in VisitedGraph.OutEdges(v))
			{
				var weight = e is WeightedEdge<TVertex> we ? we.Weight : 1.0;
				_allAttractions[attrIdx] = new LinLogEdgeData
				{
					TargetIndex = vertexToIndex[e.Target],
					AttractionWeight = weight
				};
				_vertices[idx].RepulsionWeight += 1;
				attrIdx++;
			}

			_vertices[idx].RepulsionWeight = Math.Max(_vertices[idx].RepulsionWeight, Parameters.gravitationMultiplier);
			idx++;
		}

		// Cache parameter values
		_attractionExponent = Parameters.attractionExponent;
		_repulsiveExponent = Parameters.repulsiveExponent;
		_gravitationMultiplier = Parameters.gravitationMultiplier;
		
		_repulsionMultiplier = ComputeRepulsionMultiplier();
	}

	private void ComputeBaryCenter()
	{
		_baryCenterX = 0;
		_baryCenterY = 0;
		var repWeightSum = 0.0;
		
		for (var i = 0; i < _vertices.Length; i++)
		{
			ref var v = ref _vertices[i];
			repWeightSum += v.RepulsionWeight;
			_baryCenterX += v.PositionX * v.RepulsionWeight;
			_baryCenterY += v.PositionY * v.RepulsionWeight;
		}
		
		if ( repWeightSum > 0.0 )
		{
			_baryCenterX /= repWeightSum;
			_baryCenterY /= repWeightSum;
		}
	}

	private double ComputeRepulsionMultiplier()
	{
		// Avoid LINQ allocations - use simple loops
		var attractionSum = 0.0;
		var repulsionSum = 0.0;
		
		for (var i = 0; i < _allAttractions.Length; i++)
		{
			attractionSum += _allAttractions[i].AttractionWeight;
		}
		
		for (var i = 0; i < _vertices.Length; i++)
		{
			repulsionSum += _vertices[i].RepulsionWeight;
		}

		if ( repulsionSum > 0 && attractionSum > 0 )
		{
			var repSumSq = repulsionSum * repulsionSum;
			return attractionSum / repSumSq * FastPow( repulsionSum, 0.5 * ( _attractionExponent - _repulsiveExponent ) );
		}

		return 1;
	}

	/// <summary>
	/// Builds a QuadTree for Barnes-Hut approximation using pooled nodes.
	/// </summary>
	private QuadTreeNode? BuildQuadTree()
	{
		// Find min/max positions
		var minX = double.MaxValue;
		var minY = double.MaxValue;
		var maxX = double.MinValue;
		var maxY = double.MinValue;

		for (var i = 0; i < _vertices.Length; i++)
		{
			ref var v = ref _vertices[i];
			if ( v.RepulsionWeight <= 0 )
				continue;

			if (v.PositionX < minX) minX = v.PositionX;
			if (v.PositionY < minY) minY = v.PositionY;
			if (v.PositionX > maxX) maxX = v.PositionX;
			if (v.PositionY > maxY) maxY = v.PositionY;
		}

		// Add nodes to QuadTree using pool
		QuadTreeNode? result = null;
		for (var i = 0; i < _vertices.Length; i++)
		{
			ref var v = ref _vertices[i];
			if ( v.RepulsionWeight <= 0 )
				continue;

			if ( result == null )
			{
				result = _quadTreePool.Rent();
				result.Initialize( i, v.PositionX, v.PositionY, v.RepulsionWeight, minX, minY, maxX, maxY );
			}
			else
			{
				result.AddNode( i, v.PositionX, v.PositionY, v.RepulsionWeight, 0, _quadTreePool );
			}
		}
		return result;
	}
}