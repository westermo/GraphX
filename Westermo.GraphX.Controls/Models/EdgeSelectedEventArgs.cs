using System.Windows.Input;

namespace Westermo.GraphX.Controls.Models;

public class EdgeSelectedEventArgs(EdgeControl ec, MouseButtonEventArgs? e, ModifierKeys keys) : System.EventArgs
{
    public EdgeControl EdgeControl { get; set; } = ec;
    public ModifierKeys Modifiers { get; set; } = keys;
    public MouseButtonEventArgs? MouseArgs { get; set; } = e;
}

public sealed class EdgeLabelSelectedEventArgs(
    IEdgeLabelControl label,
    EdgeControl ec,
    MouseButtonEventArgs e,
    ModifierKeys keys)
    : EdgeSelectedEventArgs(ec, e, keys)
{
    public IEdgeLabelControl EdgeLabelControl { get; set; } = label;
}