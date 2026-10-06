using System;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms;

internal class GraphHideHelper<TVertex, TEdge>(IMutableBidirectionalGraph<TVertex, TEdge> managedGraph)
	: ISoftMutableGraph<TVertex, TEdge>
	where TEdge : IEdge<TVertex>
{
	#region Helper Types
	protected class HiddenCollection
	{
		public List<TVertex> hiddenVertices = [];
		public List<TEdge> hiddenEdges = [];
	}
	#endregion

	#region Properties, fields, events
	// Sets give O(1) Contains/Remove; lists made hiding n elements O(n^2), which hurt
	// layouts such as Sugiyama that hide every parallel/long edge during computation.
	private readonly HashSet<TVertex> _hiddenVertices = [];
	private readonly HashSet<TEdge> _hiddenEdges = [];
	private readonly Dictionary<string, HiddenCollection> _hiddenCollections = new();
	private readonly Dictionary<TVertex, HashSet<TEdge>> _hiddenEdgesOf = new();

	public event EdgeAction<TVertex, TEdge> EdgeHidden;
	public event EdgeAction<TVertex, TEdge> EdgeUnhidden;

	public event VertexAction<TVertex> VertexHidden;
	public event VertexAction<TVertex> VertexUnhidden;
	#endregion

	#region Event handlers, helper methods

	/// <summary>
	/// Returns every edge connected with the vertex <code>v</code>.
	/// </summary>
	/// <param name="v">The vertex.</param>
	/// <returns>Edges, adjacent to the vertex <code>v</code>.</returns>
	protected IEnumerable<TEdge> EdgesFor( TVertex v )
	{
		return managedGraph.InEdges( v ).Concat( managedGraph.OutEdges( v ) );
	}

	protected HiddenCollection GetHiddenCollection( string tag )
	{
		HiddenCollection h;
		if ( !_hiddenCollections.TryGetValue( tag, out h ) )
		{
			h = new HiddenCollection();
			_hiddenCollections[tag] = h;
		}
		return h;
	}

	protected void OnEdgeHidden( TEdge e )
	{
		EdgeHidden?.Invoke(e);
	}

	protected void OnEdgeUnhidden( TEdge e )
	{
		EdgeUnhidden?.Invoke(e);
	}

	protected void OnVertexHidden( TVertex v )
	{
		VertexHidden?.Invoke(v);
	}

	protected void OnVertexUnhidden( TVertex v )
	{
		VertexUnhidden?.Invoke(v);
	}
	#endregion

	#region ISoftMutableGraph<TVertex,TEdge> Members

	public IEnumerable<TVertex> HiddenVertices => _hiddenVertices;

	public IEnumerable<TEdge> HiddenEdges => _hiddenEdges;

	/// <summary>
	/// Hides the vertex <code>v</code>.
	/// </summary>
	/// <param name="v">The vertex to hide.</param>
	public bool HideVertex( TVertex v )
	{
		if ( managedGraph.ContainsVertex( v ) && !_hiddenVertices.Contains( v ) )
		{
			HideEdges( EdgesFor( v ) );

			//hide the vertex
			managedGraph.RemoveVertex( v );
			_hiddenVertices.Add( v );
			OnVertexHidden( v );
			return true;
		}

		return false;
	}

	/// <summary>
	/// Hides a lot of vertices.
	/// </summary>
	/// <param name="vertices">The vertices to hide.</param>
	public void HideVertices( IEnumerable<TVertex> vertices )
	{
		var verticesToHide = new List<TVertex>( vertices );
		foreach ( var v in verticesToHide )
		{
			HideVertex( v );
		}
	}

	public bool HideVertex( TVertex v, string tag )
	{
		var h = GetHiddenCollection( tag );
		var eeh = new EdgeAction<TVertex, TEdge>( e => h.hiddenEdges.Add( e ) );
		var veh = new VertexAction<TVertex>( vertex => h.hiddenVertices.Add( vertex ) );
		EdgeHidden += eeh;
		VertexHidden += veh;
		var ret = HideVertex( v );
		EdgeHidden -= eeh;
		VertexHidden -= veh;
		return ret;
	}

	public void HideVertices( IEnumerable<TVertex> vertices, string tag )
	{
		foreach ( var v in vertices )
		{
			HideVertex( v, tag );
		}
	}

	public void HideVerticesIf( Func<TVertex, bool> predicate, string tag )
	{
		var verticesToHide = managedGraph.Vertices.Where(predicate).ToList();
		HideVertices( verticesToHide, tag );
	}

	public bool IsHiddenVertex( TVertex v )
	{
		return !managedGraph.ContainsVertex( v ) && _hiddenVertices.Contains( v );
	}

	public bool UnhideVertex( TVertex v )
	{
		//if v not hidden, it's an error
		if ( !IsHiddenVertex( v ) )
			return false;

		//unhide the vertex
		managedGraph.AddVertex( v );
		_hiddenVertices.Remove( v );
		OnVertexUnhidden( v );
		return true;
	}

	public void UnhideVertexAndEdges( TVertex v )
	{
		UnhideVertex( v );
		// UnhideEdges snapshots its input, so passing the live set is safe.
		if ( _hiddenEdgesOf.TryGetValue( v, out var hiddenEdgesOfV ) )
			UnhideEdges( hiddenEdgesOfV );
	}

	public bool HideEdge( TEdge e )
	{
		if ( managedGraph.ContainsEdge( e ) && _hiddenEdges.Add( e ) )
		{
			managedGraph.RemoveEdge( e );

			// A self-loop maps to the same set twice; the set keeps it once.
			GetHiddenEdgeSetOf( e.Source ).Add( e );
			GetHiddenEdgeSetOf( e.Target ).Add( e );

			OnEdgeHidden( e );
			return true;
		}

		return false;
	}

	private HashSet<TEdge> GetHiddenEdgeSetOf( TVertex v )
	{
		if ( !_hiddenEdgesOf.TryGetValue( v, out var hiddenEdgeSet ) )
		{
			hiddenEdgeSet = [];
			_hiddenEdgesOf[v] = hiddenEdgeSet;
		}
		return hiddenEdgeSet;
	}

	/// <summary>
	/// Removes <paramref name="e"/> from the per-vertex index of <paramref name="v"/>,
	/// dropping the entry once empty so the dictionary doesn't grow with stale keys.
	/// </summary>
	private void RemoveFromHiddenEdgesOf( TVertex v, TEdge e )
	{
		if ( _hiddenEdgesOf.TryGetValue( v, out var set ) && set.Remove( e ) && set.Count == 0 )
			_hiddenEdgesOf.Remove( v );
	}

	// Read-only queries must not insert empty lists for unknown vertices.
	public IEnumerable<TEdge> HiddenEdgesOf( TVertex v )
	{
		return _hiddenEdgesOf.TryGetValue( v, out var set ) ? set : Enumerable.Empty<TEdge>();
	}

	public int HiddenEdgeCountOf( TVertex v )
	{
		return _hiddenEdgesOf.TryGetValue( v, out var set ) ? set.Count : 0;
	}

	public bool HideEdge( TEdge e, string tag )
	{
		var h = GetHiddenCollection( tag );
		var eeh = new EdgeAction<TVertex, TEdge>( edge => h.hiddenEdges.Add( edge ) );
		EdgeHidden += eeh;
		var ret = HideEdge( e );
		EdgeHidden -= eeh;
		return ret;
	}

	public void HideEdges( IEnumerable<TEdge> edges )
	{
		var edgesToHide = new List<TEdge>( edges );
		foreach ( var e in edgesToHide )
		{
			HideEdge( e );
		}
	}

	public void HideEdges( IEnumerable<TEdge> edges, string tag )
	{
		var edgesToHide = new List<TEdge>( edges );
		foreach ( var e in edgesToHide )
		{
			HideEdge( e, tag );
		}
	}

	public void HideEdgesIf( Func<TEdge, bool> predicate, string tag )
	{
		var edgesToHide = managedGraph.Edges.Where(predicate).ToList();
		HideEdges( edgesToHide, tag );
	}

	public bool IsHiddenEdge( TEdge e )
	{
		return !managedGraph.ContainsEdge( e ) && _hiddenEdges.Contains( e );
	}

	public bool UnhideEdge( TEdge e )
	{
		if ( IsHiddenVertex( e.Source ) || IsHiddenVertex( e.Target ) || !IsHiddenEdge( e ) )
			return false;

		//unhide the edge
		managedGraph.AddEdge( e );
		_hiddenEdges.Remove( e );

		RemoveFromHiddenEdgesOf( e.Source, e );
		RemoveFromHiddenEdgesOf( e.Target, e );

		OnEdgeUnhidden( e );
		return true;
	}

	public void UnhideEdgesIf( Func<TEdge, bool> predicate )
	{
		var edgesToUnhide = _hiddenEdges.Where(predicate).ToList();
		UnhideEdges( edgesToUnhide );
	}

	public void UnhideEdges( IEnumerable<TEdge> edges )
	{
		var edgesToUnhide = new List<TEdge>( edges );
		foreach ( var e in edgesToUnhide )
		{
			UnhideEdge( e );
		}
	}

	public bool Unhide( string tag )
	{
		// Look up without creating, so an unknown tag reports false instead of succeeding.
		if ( !_hiddenCollections.TryGetValue( tag, out var h ) )
			return false;
		foreach ( var v in h.hiddenVertices )
		{
			UnhideVertex( v );
		}
		foreach ( var e in h.hiddenEdges )
		{
			UnhideEdge( e );
		}
		return _hiddenCollections.Remove( tag );
	}

	public bool UnhideAll()
	{
		// Iterate over snapshots: an element that fails to unhide stays in the hidden list,
		// so looping until the list is empty could spin forever.
		foreach ( var v in _hiddenVertices.ToArray() )
		{
			UnhideVertex( v );
		}
		foreach ( var e in _hiddenEdges.ToArray() )
		{
			UnhideEdge( e );
		}
		return _hiddenVertices.Count == 0 && _hiddenEdges.Count == 0;
	}

	public int HiddenVertexCount => _hiddenVertices.Count;

	public int HiddenEdgeCount => _hiddenEdges.Count;

	#endregion

	#region IBidirectionalGraph<TVertex,TEdge> Members

	public int Degree( TVertex v )
	{
		throw new NotImplementedException();
	}

	public int InDegree( TVertex v )
	{
		throw new NotImplementedException();
	}

	public TEdge InEdge( TVertex v, int index )
	{
		throw new NotImplementedException();
	}

	public IEnumerable<TEdge> InEdges( TVertex v )
	{
		throw new NotImplementedException();
	}

	public bool IsInEdgesEmpty( TVertex v )
	{
		throw new NotImplementedException();
	}

	#endregion

	#region IIncidenceGraph<TVertex,TEdge> Members

	public bool ContainsEdge( TVertex source, TVertex target )
	{
		throw new NotImplementedException();
	}

	public bool TryGetEdge( TVertex source, TVertex target, out TEdge edge )
	{
		throw new NotImplementedException();
	}

	public bool TryGetEdges( TVertex source, TVertex target, out IEnumerable<TEdge> edges )
	{
		throw new NotImplementedException();
	}

	#endregion

	#region IImplicitGraph<TVertex,TEdge> Members

	public bool IsOutEdgesEmpty( TVertex v )
	{
		throw new NotImplementedException();
	}

	public int OutDegree( TVertex v )
	{
		throw new NotImplementedException();
	}

	public TEdge OutEdge( TVertex v, int index )
	{
		throw new NotImplementedException();
	}

	public IEnumerable<TEdge> OutEdges( TVertex v )
	{
		throw new NotImplementedException();
	}

	#endregion

	#region IGraph<TVertex,TEdge> Members

	public bool AllowParallelEdges => throw new NotImplementedException();

	public bool IsDirected => throw new NotImplementedException();

	#endregion

	#region IVertexSet<TVertex,TEdge> Members

	public bool ContainsVertex( TVertex vertex )
	{
		throw new NotImplementedException();
	}

	public bool IsVerticesEmpty => throw new NotImplementedException();

	public int VertexCount => throw new NotImplementedException();

	public IEnumerable<TVertex> Vertices => throw new NotImplementedException();

	#endregion

	#region IEdgeListGraph<TVertex,TEdge> Members

	public bool ContainsEdge( TEdge edge )
	{
		throw new NotImplementedException();
	}

	public int EdgeCount => throw new NotImplementedException();

	public IEnumerable<TEdge> Edges => throw new NotImplementedException();

	public bool IsEdgesEmpty => throw new NotImplementedException();

	#endregion

	public bool TryGetInEdges( TVertex v, out IEnumerable<TEdge> edges )
	{
		throw new NotImplementedException();
	}
		
	public bool TryGetOutEdges( TVertex v, out IEnumerable<TEdge> edges )
	{
		throw new NotImplementedException();
	}
}