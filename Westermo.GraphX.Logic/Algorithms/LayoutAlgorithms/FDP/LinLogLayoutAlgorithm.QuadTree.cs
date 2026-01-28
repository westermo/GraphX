#nullable enable
using System;
using System.Runtime.CompilerServices;
using Westermo.GraphX.Measure;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public partial class LinLogLayoutAlgorithm<TVertex, TEdge, TGraph> 
	where TVertex : class
	where TEdge : IEdge<TVertex>
	where TGraph : IBidirectionalGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{
	/// <summary>
	/// Object pool for QuadTree nodes to avoid per-iteration allocations.
	/// </summary>
	private sealed class QuadTreePool
	{
		private QuadTreeNode[] _pool;
		private int _count;
		private int _nextFree;

		public QuadTreePool(int initialCapacity)
		{
			_pool = new QuadTreeNode[initialCapacity];
			for (var i = 0; i < initialCapacity; i++)
			{
				_pool[i] = new QuadTreeNode();
			}
			_count = initialCapacity;
			_nextFree = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public QuadTreeNode Rent()
		{
			if (_nextFree >= _count)
			{
				// Expand pool
				var newCapacity = _count * 2;
				var newPool = new QuadTreeNode[newCapacity];
				Array.Copy(_pool, newPool, _count);
				for (var i = _count; i < newCapacity; i++)
				{
					newPool[i] = new QuadTreeNode();
				}
				_pool = newPool;
				_count = newCapacity;
			}
			return _pool[_nextFree++];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			// Just reset counter - Initialize() clears children
			_nextFree = 0;
		}
	}

	/// <summary>
	/// Pooled QuadTree node - mutable and reusable.
	/// </summary>
	private sealed class QuadTreeNode
	{
		public QuadTreeNode? Child0;
		public QuadTreeNode? Child1;
		public QuadTreeNode? Child2;
		public QuadTreeNode? Child3;

		public int Index;
		public double PositionX;
		public double PositionY;
		public double Weight;
		public double MinX, MinY, MaxX, MaxY;
		public double Width;

		private const int MAX_DEPTH = 20;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Initialize(int index, double posX, double posY, double weight, double minX, double minY, double maxX, double maxY)
		{
			// Clear children from previous use
			Child0 = null;
			Child1 = null;
			Child2 = null;
			Child3 = null;
			
			Index = index;
			PositionX = posX;
			PositionY = posY;
			Weight = weight;
			MinX = minX;
			MinY = minY;
			MaxX = maxX;
			MaxY = maxY;
			Width = Math.Max(maxX - minX, maxY - minY);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			// No longer used - Initialize clears children
		}

		public void AddNode(int nodeIndex, double nodeX, double nodeY, double nodeWeight, int depth, QuadTreePool pool)
		{
			if (depth > MAX_DEPTH)
				return;

			if (Index >= 0)
			{
				AddNode2(Index, PositionX, PositionY, Weight, depth, pool);
				Index = -1;
			}

			var totalWeight = Weight + nodeWeight;
			PositionX = (PositionX * Weight + nodeX * nodeWeight) / totalWeight;
			PositionY = (PositionY * Weight + nodeY * nodeWeight) / totalWeight;
			Weight = totalWeight;

			AddNode2(nodeIndex, nodeX, nodeY, nodeWeight, depth, pool);
		}

		private void AddNode2(int nodeIndex, double nodeX, double nodeY, double nodeWeight, int depth, QuadTreePool pool)
		{
			var middleX = (MinX + MaxX) * 0.5;
			var middleY = (MinY + MaxY) * 0.5;
			var isRight = nodeX > middleX;
			var isBottom = nodeY > middleY;

			// Get child using direct field access
			QuadTreeNode? child;
			if (!isRight && !isBottom) child = Child0;
			else if (isRight && !isBottom) child = Child1;
			else if (!isRight && isBottom) child = Child2;
			else child = Child3;

			if (child == null)
			{
				double newMinX, newMaxX, newMinY, newMaxY;
				if (!isRight) { newMinX = MinX; newMaxX = middleX; }
				else { newMinX = middleX; newMaxX = MaxX; }
				if (!isBottom) { newMinY = MinY; newMaxY = middleY; }
				else { newMinY = middleY; newMaxY = MaxY; }
				
				child = pool.Rent();
				child.Initialize(nodeIndex, nodeX, nodeY, nodeWeight, newMinX, newMinY, newMaxX, newMaxY);
				
				// Set child using direct field access
				if (!isRight && !isBottom) Child0 = child;
				else if (isRight && !isBottom) Child1 = child;
				else if (!isRight && isBottom) Child2 = child;
				else Child3 = child;
			}
			else
			{
				child.AddNode(nodeIndex, nodeX, nodeY, nodeWeight, depth + 1, pool);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void MoveNode(double oldX, double oldY, double newX, double newY, double nodeWeight)
		{
			var factor = nodeWeight / Weight;
			PositionX += (newX - oldX) * factor;
			PositionY += (newY - oldY) * factor;

			var middleX = (MinX + MaxX) * 0.5;
			var middleY = (MinY + MaxY) * 0.5;
			var isRight = oldX > middleX;
			var isBottom = oldY > middleY;

			// Direct field access instead of GetChild
			QuadTreeNode? child;
			if (!isRight && !isBottom) child = Child0;
			else if (isRight && !isBottom) child = Child1;
			else if (!isRight && isBottom) child = Child2;
			else child = Child3;

			child?.MoveNode(oldX, oldY, newX, newY, nodeWeight);
		}
	}
}