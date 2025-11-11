using Avalonia.Input;
using Avalonia.Interactivity;
using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Models;

public sealed class VertexSelectedEventArgs(VertexControl vc, RoutedEventArgs e, KeyModifiers keys)
    : System.EventArgs
{
    public VertexControl VertexControl { get; private set; } = vc;
    public RoutedEventArgs? MouseArgs { get; private set; } = e;
    public KeyModifiers Modifiers { get; private set; } = keys;
}