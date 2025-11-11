using Avalonia;
using Avalonia.Input;
using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Models;

public sealed class VertexMovedEventArgs(VertexControl vc, PointerEventArgs e) : System.EventArgs
{
    public VertexControl VertexControl { get; private set; } = vc;
    public Point Offset { get; private set; } = new();
    public PointerEventArgs Args { get; private set; } = e;
}