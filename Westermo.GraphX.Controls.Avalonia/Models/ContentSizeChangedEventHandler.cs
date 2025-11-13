using Avalonia;

namespace Westermo.GraphX.Controls.Models;

public sealed class ContentSizeChangedEventArgs(Rect oldSize, Rect newSize) : System.EventArgs
{
    public Rect OldSize { get; private set; } = oldSize;
    public Rect NewSize { get; private set; } = newSize;
}

public delegate void ContentSizeChangedEventHandler(object sender, ContentSizeChangedEventArgs e);