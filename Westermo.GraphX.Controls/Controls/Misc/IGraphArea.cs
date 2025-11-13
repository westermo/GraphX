using System.Collections.Generic;

namespace Westermo.GraphX.Controls;

public interface IGraphArea<TVertex>
{
    IDictionary<TVertex, VertexControl> VertexList { get; set; }
}