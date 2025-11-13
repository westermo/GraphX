using Avalonia.Input;
using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Models;

public class ControlClickedEventArgs<CType>(CType c, PointerEventArgs? e, KeyModifiers keys) : System.EventArgs
{
    public CType Control { get; private set; } = c;
    public PointerEventArgs? MouseArgs { get; private set; } = e;
    public KeyModifiers Modifiers { get; private set; } = keys;
}

public sealed class VertexClickedEventArgs(VertexControl c, PointerEventArgs? e, KeyModifiers keys)
    : ControlClickedEventArgs<VertexControl>(c, e, keys);

public sealed class EdgeClickedEventArgs(EdgeControl c, PointerEventArgs? e, KeyModifiers keys)
    : ControlClickedEventArgs<EdgeControl>(c, e, keys);