using Avalonia.Controls;

namespace Westermo.GraphX.Controls.Controls.Misc;

/// <summary>
/// Common imterface for all possible zoomcontrol objects
/// </summary>
public interface IZoomControl
{
    Control PresenterVisual { get; }
    double Zoom { get; set; }
    double Width { get; set; }
    double Height { get; set; }
}