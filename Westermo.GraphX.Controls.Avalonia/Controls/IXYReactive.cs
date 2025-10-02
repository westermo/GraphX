using Avalonia;

namespace Westermo.GraphX.Controls.Avalonia;

public interface IXYReactive
{
    void XYChanged(AvaloniaPropertyChangedEventArgs args);
}