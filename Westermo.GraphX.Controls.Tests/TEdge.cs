using Westermo.GraphX.Common.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
{
    public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; } = null;
}