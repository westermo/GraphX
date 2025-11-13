using Avalonia;

namespace Westermo.GraphX.Controls.Controls;

public interface IXYReactive
{
    void XYChanged(AvaloniaPropertyChangedEventArgs args);
}