using Avalonia;

namespace Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;

public class AreaSelectedEventArgs(Rect rec) : System.EventArgs
{
    /// <summary>
    /// Rectangle data in coordinates of content object
    /// </summary>
    public Rect Rectangle { get; set; } = rec;
}