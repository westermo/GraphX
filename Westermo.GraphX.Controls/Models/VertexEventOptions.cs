using Westermo.GraphX.Common.Enums;

namespace Westermo.GraphX.Controls.Models;

public sealed class VertexEventOptions(VertexControl vc)
{
    /// <summary>
    /// Gets or sets if MouseMove event should be enabled
    /// </summary>
    public bool MouseMoveEnabled { get => _mousemove;
        set {
            if (_mousemove == value) return;
            _mousemove = value; _vc.UpdateEventhandling(EventType.MouseMove);
        } }
    private bool _mousemove = true;
    /// <summary>
    /// Gets or sets if MouseEnter event should be enabled
    /// </summary>
    public bool MouseEnterEnabled { get => _mouseenter;
        set {
            if (_mouseenter == value) return;
            _mouseenter = value; _vc.UpdateEventhandling(EventType.MouseEnter);
        } }
    private bool _mouseenter = true;
    /// <summary>
    /// Gets or sets if MouseLeave event should be enabled
    /// </summary>
    public bool MouseLeaveEnabled { get => _mouseleave;
        set {
            if (_mouseleave == value) return;
            _mouseleave = value; _vc.UpdateEventhandling(EventType.MouseLeave);
        } }
    private bool _mouseleave = true;
    /// <summary>
    /// Gets or sets if MouseDown event should be enabled
    /// </summary>
    public bool MouseClickEnabled { get => _mouseclick;
        set {
            if (_mouseclick == value) return;
            _mouseclick = value; _vc.UpdateEventhandling(EventType.MouseClick);
        } }
    private bool _mouseclick = true;
    /// <summary>
    /// Gets or sets if MouseDoubleClick event should be enabled
    /// </summary>
    public bool MouseDoubleClickEnabled { get => _mousedblclick;
        set {
            if (_mousedblclick == value) return;
            _mousedblclick = value; _vc.UpdateEventhandling(EventType.MouseDoubleClick);
        } }
    private bool _mousedblclick = true;

    /// <summary>
    /// Gets or sets if position trace enabled. If enabled then PositionChanged event will be rised on each X or Y property change.
    /// True by default. 
    /// </summary>
    public bool PositionChangeNotification { 
        get => _poschange;
        set 
        {
            if (_poschange == value) return;
            _poschange = value; 
            if(_vc == null) return;
            _vc.UpdatePositionTraceState();
        } 
    }
    private bool _poschange = true;

    private VertexControl _vc = vc;

    public void Clean()
    {
        _vc = null!;
    }

}