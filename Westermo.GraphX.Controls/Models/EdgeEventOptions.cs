using Westermo.GraphX.Common.Enums;

namespace Westermo.GraphX.Controls.Models;

public sealed class EdgeEventOptions(EdgeControl ec)
{
    /// <summary>
    /// Gets or sets if MouseMove event should be enabled
    /// </summary>
    public bool MouseMoveEnabled
    {
        get => _mouseMove;
        set
        {
            if (_mouseMove == value) return;
            _mouseMove = value;
            _ec.UpdateEventhandling(EventType.MouseMove);
        }
    }

    private bool _mouseMove = true;

    /// <summary>
    /// Gets or sets if MouseEnter event should be enabled
    /// </summary>
    public bool MouseEnterEnabled
    {
        get => _mouseEnter;
        set
        {
            if (_mouseEnter == value) return;
            _mouseEnter = value;
            _ec.UpdateEventhandling(EventType.MouseEnter);
        }
    }

    private bool _mouseEnter = true;

    /// <summary>
    /// Gets or sets if MouseLeave event should be enabled
    /// </summary>
    public bool MouseLeaveEnabled
    {
        get => _mouseLeave;
        set
        {
            if (_mouseLeave == value) return;
            _mouseLeave = value;
            _ec.UpdateEventhandling(EventType.MouseLeave);
        }
    }

    private bool _mouseLeave = true;

    /// <summary>
    /// Gets or sets if MouseDown event should be enabled
    /// </summary>
    public bool MouseClickEnabled
    {
        get => _mouseclick;
        set
        {
            if (_mouseclick == value) return;
            _mouseclick = value;
            _ec.UpdateEventhandling(EventType.MouseClick);
        }
    }

    private bool _mouseclick = true;

    /// <summary>
    /// Gets or sets if MouseDoubleClick event should be enabled
    /// </summary>
    public bool MouseDoubleClickEnabled
    {
        get => _mouseDoubleClick;
        set
        {
            if (_mouseDoubleClick == value) return;
            _mouseDoubleClick = value;
            _ec.UpdateEventhandling(EventType.MouseDoubleClick);
        }
    }

    private bool _mouseDoubleClick = true;

    private EdgeControl _ec = ec;

    public void Clean()
    {
        _ec = null!;
    }
}