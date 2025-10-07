using Avalonia.Controls;

namespace Westermo.GraphX.Controls.Avalonia
{
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
}
