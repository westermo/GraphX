using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Models;

public sealed class ControlEventArgs(IGraphControl vc, bool removeDataObject) : System.EventArgs
{
    public IGraphControl Control { get; private set; } = vc;

    public bool RemoveDataObject { get; private set; } = removeDataObject;
}