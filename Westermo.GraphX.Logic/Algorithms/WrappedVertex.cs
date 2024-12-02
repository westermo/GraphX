namespace Westermo.GraphX.Logic.Algorithms
{
	public class WrappedVertex<TVertex>(TVertex original)
	{
		public TVertex Original => original;
	}
}