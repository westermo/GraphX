using System.Collections.Generic;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
    public class OverlapRemovalContext<TVertex>(IDictionary<TVertex, Rect> rectangles) : IOverlapRemovalContext<TVertex>
    {
        public IDictionary<TVertex, Rect> Rectangles { get; private set; } = rectangles;
    }
}