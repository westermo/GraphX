using System.Windows.Input;

namespace Westermo.GraphX.Controls.Models;

public sealed class VertexSelectedEventArgs(VertexControl vc, MouseEventArgs e, ModifierKeys keys)
    : System.EventArgs
{
    public VertexControl VertexControl { get; private set; } = vc;
    public MouseButtonEventArgs? MouseArgs { get; private set; } = e as MouseButtonEventArgs;
    public ModifierKeys Modifiers { get; private set; } = keys;
}