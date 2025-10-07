using Avalonia;
using Avalonia.Input;

namespace Westermo.GraphX.Controls.Avalonia.Models
{
    public sealed class VertexMovedEventArgs(VertexControl vc, PointerEventArgs e) : System.EventArgs
    {
        public VertexControl VertexControl { get; private set; } = vc;
        public Point Offset { get; private set; } = new();
        public PointerEventArgs Args { get; private set; } = e;
    }
}