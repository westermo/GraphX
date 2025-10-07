using Avalonia.Input;
using Avalonia.Interactivity;

namespace Westermo.GraphX.Controls.Avalonia.Models
{
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
}