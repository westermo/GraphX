using Avalonia;

namespace Westermo.GraphX.Controls.Controls.Misc;

/// <summary>
/// Common GraphArea interface
/// </summary>
public interface IGraphAreaBase
{
    void SetPrintMode(bool value, bool offsetControls = true, int margin = 0);

    Rect ContentSize { get; }
}