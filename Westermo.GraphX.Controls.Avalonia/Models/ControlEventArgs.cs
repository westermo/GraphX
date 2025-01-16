namespace Westermo.GraphX.Controls.Avalonia.Models
{
    public sealed class ControlEventArgs(IGraphControl vc, bool removeDataObject) : System.EventArgs
    {
        public IGraphControl Control { get; private set; } = vc;

        public bool RemoveDataObject { get; private set; } = removeDataObject;
    }
}
