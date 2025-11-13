using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public interface IParameterizedLayoutAlgorithm
{
	ILayoutParameters GetParameters();
}

public interface IParameterizedLayoutAlgorithm<out TParam> : IParameterizedLayoutAlgorithm
	where TParam : ILayoutParameters
{
	TParam Parameters { get; }
}