using Avalonia;
using Avalonia.Input;

namespace Westermo.GraphX.Controls.Controls.Interfaces;

public interface IDraggable : IInputElement
{
    /// <summary>
    /// Begin dragging operation
    /// </summary>
    /// <param name="origin">Pointer event that started the drag</param>
    /// <returns>Indicator to begin tracking drag</returns>
    bool StartDrag(PointerPressedEventArgs origin);

    bool IsDragging { get; }

    /// <summary>
    /// Process an uninterrupted drag event
    /// </summary>
    /// <param name="current">The pointer events providing mouse location</param>
    void Drag(PointerEventArgs current);

    /// <summary>
    /// End dragging and set new position
    /// </summary>
    /// <param name="pointerReleasedEventArgs">The pointer release event which trigger the drag-end</param>
    /// <returns>An indicator whether the drag should be completed</returns>
    bool EndDrag(PointerReleasedEventArgs pointerReleasedEventArgs);

    /// <summary>
    /// Called to forcibly end dragging without setting a new position, typically in the case that the control unexpectedly lost mouse capture.
    /// </summary>
    void EndDrag();

    /// <summary>
    /// The container visual that the drag operations are relative to.
    /// </summary>
    Visual? Container { get; }
}