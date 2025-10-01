using Avalonia;
using Avalonia.Input;

namespace Westermo.GraphX.Controls.Avalonia.Controls.Interfaces;

public interface IDraggable : IInputElement
{
    bool StartDrag(PointerPressedEventArgs origin);
    bool IsDragging { get; }
    void Drag(PointerEventArgs current);
    bool EndDrag(PointerReleasedEventArgs pointerReleasedEventArgs);
    void EndDrag();
    Visual? Container { get; }
}