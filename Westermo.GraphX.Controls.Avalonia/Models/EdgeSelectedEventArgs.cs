using Avalonia.Input;
using Avalonia.Interactivity;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Models;

public class EdgeSelectedEventArgs(EdgeControl ec, RoutedEventArgs? e, KeyModifiers keys) : System.EventArgs
{
    public EdgeControl EdgeControl { get; set; } = ec;
    public KeyModifiers Modifiers { get; set; } = keys;
    public RoutedEventArgs? MouseArgs { get; set; } = e;
}

public sealed class EdgeLabelSelectedEventArgs(
    IEdgeLabelControl label,
    EdgeControl ec,
    RoutedEventArgs e,
    KeyModifiers keys)
    : EdgeSelectedEventArgs(ec, e, keys)
{
    public IEdgeLabelControl EdgeLabelControl { get; set; } = label;
}