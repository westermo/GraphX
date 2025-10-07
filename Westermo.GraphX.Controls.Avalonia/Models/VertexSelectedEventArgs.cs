using Avalonia.Input;
using Avalonia.Interactivity;

namespace Westermo.GraphX.Controls.Avalonia.Models
{
    public sealed class VertexSelectedEventArgs(VertexControl vc, RoutedEventArgs e, KeyModifiers keys)
        : System.EventArgs
    {
        public VertexControl VertexControl { get; private set; } = vc;
        public RoutedEventArgs? MouseArgs { get; private set; } = e;
        public KeyModifiers Modifiers { get; private set; } = keys;
    }
}