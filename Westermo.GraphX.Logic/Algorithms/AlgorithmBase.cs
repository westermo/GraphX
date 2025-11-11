using System.Threading;

namespace Westermo.GraphX.Logic.Algorithms;

public abstract class AlgorithmBase : IAlgorithm
{
	public abstract void Compute(CancellationToken cancellationToken);
}