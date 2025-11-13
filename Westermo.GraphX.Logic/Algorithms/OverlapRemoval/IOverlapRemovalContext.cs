using System.Collections.Generic;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval;

public interface IOverlapRemovalContext<TVertex>
{
	IDictionary<TVertex, Rect> Rectangles { get; }
}