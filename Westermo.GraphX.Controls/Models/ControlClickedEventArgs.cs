
using System.Windows.Input;

namespace Westermo.GraphX.Controls.Models
{
    public class ControlClickedEventArgs<CType>(CType c, MouseEventArgs? e, ModifierKeys keys) : System.EventArgs
    {
        public CType Control { get; private set; } = c;
        public MouseButtonEventArgs? MouseArgs { get; private set; } = e as MouseButtonEventArgs;
        public ModifierKeys Modifiers { get; private set; } = keys;
    }

    public sealed class VertexClickedEventArgs(VertexControl c, MouseEventArgs? e, ModifierKeys keys)
        : ControlClickedEventArgs<VertexControl>(c, e, keys);

    public sealed class EdgeClickedEventArgs(EdgeControl c, MouseEventArgs? e, ModifierKeys keys)
        : ControlClickedEventArgs<EdgeControl>(c, e, keys);
}