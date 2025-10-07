using Avalonia;

namespace Westermo.GraphX.Controls.Avalonia
{
    /// <summary>
    /// Common GraphArea interface
    /// </summary>
    public interface IGraphAreaBase
    {
        void SetPrintMode(bool value, bool offsetControls = true, int margin = 0);

        Rect ContentSize { get; }
    }
}
