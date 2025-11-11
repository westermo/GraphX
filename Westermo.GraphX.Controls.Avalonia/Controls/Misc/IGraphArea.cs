using System.Collections.Generic;

namespace Westermo.GraphX.Controls.Controls.Misc;

public interface IGraphArea<TVertex>
{
    IDictionary<TVertex, VertexControl> VertexList { get; set; }
}