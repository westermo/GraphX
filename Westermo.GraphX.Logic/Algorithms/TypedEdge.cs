using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms
{
	public enum EdgeTypes
	{
		General,
		Hierarchical
	}

	public interface ITypedEdge
	{
		EdgeTypes Type { get; }
	}

	public class TypedEdge<TVertex>(TVertex source, TVertex target, EdgeTypes type)
		: Edge<TVertex>(source, target), ITypedEdge
	{
		public EdgeTypes Type => type;

		public override string ToString()
		{
			return $"{type}: {Source}-->{Target}";
		}
	}
}