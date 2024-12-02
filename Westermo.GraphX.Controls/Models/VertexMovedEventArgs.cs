using System.Windows;
using System.Windows.Input;

namespace Westermo.GraphX.Controls.Models
{
    public sealed class VertexMovedEventArgs(VertexControl vc, MouseEventArgs e) : System.EventArgs
    {
        public VertexControl VertexControl { get; private set; } = vc;
        public Point Offset { get; private set; } = new();
        public MouseEventArgs Args { get; private set; } = e;
    }
}
