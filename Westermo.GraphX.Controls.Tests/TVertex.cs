using Westermo.GraphX.Common.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class TVertex(string name) : VertexBase
{
    public string Name { get; } = name;
    public override string ToString() => Name;
}