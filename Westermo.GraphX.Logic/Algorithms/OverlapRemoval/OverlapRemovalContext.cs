using System.Collections.Generic;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
    public class OverlapRemovalContext<TVertex> : IOverlapRemovalContext<TVertex>
    {
        public IDictionary<TVertex, Rect> Rectangles { get; private set; }

        public OverlapRemovalContext( IDictionary<TVertex, Rect> rectangles )
        {
            Rectangles = rectangles;
        }
    }
}