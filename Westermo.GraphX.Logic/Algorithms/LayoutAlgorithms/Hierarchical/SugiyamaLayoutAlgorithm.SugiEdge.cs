using System.Collections.Generic;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
	public partial class SugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph> 
        where TVertex : class 
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
	{
		private class SugiEdge(TEdge original, SugiVertex source, SugiVertex target, EdgeTypes type)
			: TypedEdge<SugiVertex>(source, target, type)
		{
			public bool IsLongEdge
			{
				get => DummyVertices != null;
				set
				{
					if ( IsLongEdge != value )
					{
						DummyVertices = value ? new List<SugiVertex>() : null;
					}
				}
			}

			public IList<SugiVertex> DummyVertices { get; private set; }
			public TEdge Original { get; private set; } = original;
			public bool IsReverted => !Original.Equals(default(TEdge)) && Original.Source == Target.Original && Original.Target == Source.Original;
		}
	}
}