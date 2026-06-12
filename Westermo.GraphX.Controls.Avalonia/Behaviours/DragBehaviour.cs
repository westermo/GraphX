using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Westermo.GraphX.Controls.Controls.Interfaces;

namespace Westermo.GraphX.Controls.Behaviours;

/// <summary>
/// Dragging behavior of objects in a GraphX graph area is influenced using the attached properties of this class.
///
/// To enable dragging of an individual object, set the IsDragEnabled attached property to true on that object. When IsDragEnabled is true, the
/// object can be used to initiate dragging.
///
/// To drag a group of vertices, set the IsTagged attached property to true for all the vertices in the group. When dragging is started from
/// one of the tagged vertices, all the tagged ones will be moved.
///
/// "Primary drag object" defined: Whichever object gets the mouse/pointer events is treated as the primary drag object and its attached properties take
/// precedence for controlling drag behavior. When only one object is being dragged, it is the primary drag object. When a group of objects is tagged
/// and being dragged together, the one getting mouse events is the primary drag object.
///
/// There is limited support for dragging edges. It is achieved by setting IsDragEnabled to true for the edge AND tagging the edge and the vertices
/// it is attached to. When the user drags the edge, the drag is actually performed on the vertices.
///
/// For edges to be updated as a vertex is moved, set UpdateEdgesOnMove to true for the object being dragged.
///
/// Snapping is turned on or off by the GlobalIsSnappingPredicate or by the IsSnappingPredicate property on the primary drag object. The predicate is
/// called with each movement of the mouse/pointer and the primary drag object is passed in. If snapping should be performed, the predicate must return
/// true. To skip snapping logic, the predicate must return false. If no predicate is set using the IsSnappingPredicate, the  GlobalIsSnappingPredicate
/// is used. The default behavior is to snap while a shift key alone is pressed.
///
/// When dragging a group of objects and using snapping, there is an additional refinement that can be used for the snapping behavior of the individual
/// objects in the group. The individual objects can move the exact same amount as the primary object when it snaps, or they can snap individually, with
/// the snap calculation being performed for each one. The behavior is controlled for the entire group by the GlobalIsSnappingIndividuallyPredicate or
/// the IsIndividualSnappingPredicate property setting ON THE PRIMARY DRAG OBJECT. The default behavior is to move all dragged objects by the same offset
/// as the primary drag object.
///
/// Snapping calculations are performed by the functions set on the primary drag object using the GlobalXSnapModifier or XSnapModifier property and the
/// GlobalYSnapModifier or YSnapModifier propery. These functions are called for each movement and provided the GraphAreaBase, object being moved, and
/// the pre-snapped x or y value. The passed in parameters are intended to provide an opportunity to find elements within the graph area and do things
/// like snap to center aligned, snap to left aligned, etc. The default behavior is to simply round the value to the nearest 10.
/// </summary>
public delegate double SnapModifierFunc(Visual container, Control obj, double val);

public static class DragBehaviour
{
    #region Built-in snapping behavior

    /// <summary>
    /// Helper function for snapping to a certain amount of pixels
    /// </summary>
    /// <param name="gridSize">The number of pixels to snap to</param>
    /// <returns></returns>
    public static SnapModifierFunc GridSnap(double gridSize)
    {
        return (_, _, val) => Math.Round(val / gridSize) * gridSize;
    }

    /// <summary>
    /// Helper function for snapping to ctrl width
    /// </summary>
    public static readonly SnapModifierFunc WidthSnap =
        (_, ctrl, val) => Math.Round(val / ctrl.Bounds.Width) * ctrl.Bounds.Width;

    /// <summary>
    /// Helper function for snapping to ctrl height
    /// </summary>
    public static readonly SnapModifierFunc HeightSnap =
        (_, ctrl, val) => Math.Round(val / ctrl.Bounds.Height) * ctrl.Bounds.Height;

    /// <summary>
    /// Snapping is active when the Shift key alone is held.
    /// <see cref="CurrentPointerKeyModifiers"/> is updated on every PointerMoved event so that this predicate
    /// reflects the correct keyboard state regardless of whether Shift was pressed before or after the drag started.
    /// </summary>
    public static readonly Predicate<Control>? DefaultSnapPredicate =
        _ => (CurrentPointerKeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift;

    public static readonly SnapModifierFunc DefaultSnapModifier = GridSnap(DefaultSnapGridSize);

    #endregion Built-in snapping behavior

    #region Global snapping behavior management

    private const double DefaultSnapGridSize = 10.0;

    /// <summary>
    /// Gets the <see cref="KeyModifiers"/> captured from the most recent pointer-move event.
    /// Custom <see cref="GlobalIsSnappingPredicate"/> implementations can read this to react to
    /// keyboard modifiers without needing to change the predicate signature.
    /// </summary>
    public static KeyModifiers CurrentPointerKeyModifiers { get; private set; }

    #endregion Global snapping behavior management

    #region Attached Properties

    static DragBehaviour()
    {
        IsDragEnabledProperty.Changed.AddClassHandler<Control>(OnIsDragEnabledPropertyChanged);
        InputElement.PointerCaptureLostEvent.AddClassHandler<Control>(PointerCaptureLost,
            RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
    }

    public static readonly AttachedProperty<bool> IsDragEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsDragEnabled", typeof(DragBehaviour));

    public static readonly AttachedProperty<bool> UpdateEdgesOnMoveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("UpdateEdgesOnMove", typeof(DragBehaviour));

    public static readonly AttachedProperty<bool> IsTaggedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsTagged", typeof(DragBehaviour));

    public static readonly AttachedProperty<Predicate<Control>> IsSnappingPredicateProperty =
        AvaloniaProperty.RegisterAttached<Control, Predicate<Control>>(
            "IsSnappingPredicate", typeof(DragBehaviour), DefaultSnapPredicate);

    /// <summary>
    /// Snap feature modifier delegate for X axis
    /// </summary>
    public static readonly AttachedProperty<SnapModifierFunc> XSnapModifierProperty =
        AvaloniaProperty.RegisterAttached<Control, SnapModifierFunc>("XSnapModifier", typeof(DragBehaviour),
            DefaultSnapModifier);

    /// <summary>
    /// Snap feature modifier delegate for Y axis
    /// </summary>
    public static readonly AttachedProperty<SnapModifierFunc> YSnapModifierProperty =
        AvaloniaProperty.RegisterAttached<Control, SnapModifierFunc>("YSnapModifier", typeof(DragBehaviour),
            DefaultSnapModifier);

    #endregion Attached DPs

    #region Get/Set method for Attached Properties

    public static bool GetUpdateEdgesOnMove(Control obj)
    {
        return obj.GetValue(UpdateEdgesOnMoveProperty);
    }

    public static void SetUpdateEdgesOnMove(Control obj, bool value)
    {
        obj.SetValue(UpdateEdgesOnMoveProperty, value);
    }

    public static bool GetIsTagged(Control obj)
    {
        return obj.GetValue(IsTaggedProperty);
    }

    public static void SetIsTagged(Control obj, bool value)
    {
        obj.SetValue(IsTaggedProperty, value);
    }

    public static bool GetIsDragEnabled(Control obj)
    {
        return obj.GetValue(IsDragEnabledProperty);
    }

    public static void SetIsDragEnabled(Control obj, bool value)
    {
        obj.SetValue(IsDragEnabledProperty, value);
    }

    public static Predicate<Control> GetIsSnappingPredicate(Control obj)
    {
        return obj.GetValue(IsSnappingPredicateProperty);
    }

    public static void SetIsSnappingPredicate(Control obj, Predicate<Control> value)
    {
        obj.SetValue(IsSnappingPredicateProperty, value);
    }

    public static SnapModifierFunc GetXSnapModifier(Control obj)
    {
        return obj.GetValue(XSnapModifierProperty);
    }

    public static void SetXSnapModifier(Control obj, SnapModifierFunc value)
    {
        obj.SetValue(XSnapModifierProperty, value);
    }

    public static SnapModifierFunc GetYSnapModifier(Control obj)
    {
        return obj.GetValue(YSnapModifierProperty);
    }

    public static void SetYSnapModifier(Control obj, SnapModifierFunc value)
    {
        obj.SetValue(YSnapModifierProperty, value);
    }

    #endregion Get/Set method for Attached Properties

    private static void OnIsDragEnabledPropertyChanged(Control obj, AvaloniaPropertyChangedEventArgs e)
    {
        if (obj is not IDraggable draggable)
            return;

        if (e.NewValue is not bool value)
            return;

        if (value)
        {
            draggable.PointerReleased += PointerUp;
            InputElement.PointerReleasedEvent.AddClassHandler<Control>(PointerUp,
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel, true);
            draggable.PointerPressed += PointerDown;
        }
        else
        {
            draggable.PointerReleased -= PointerUp;
            draggable.PointerPressed -= PointerDown;
        }
    }

    private static void PointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not IDraggable draggable) return;
        if (!draggable.StartDrag(e)) return;
        draggable.Container?.Classes.Add("ChildDragging");
        draggable.PointerMoved += PointerMoved;
        e.Pointer.Capture(draggable);
        var affected = GetTagged(draggable);
        if (affected == null) return;
        foreach (var control in affected)
        {
            if (control == draggable) continue;
            control.StartDrag(e);
        }

        e.Handled = true;
    }

    private static IDraggable[]? GetTagged(IDraggable draggable)
    {
        var affected = draggable.Container?
            .GetVisualDescendants()
            .OfType<Control>().Where(GetIsTagged).OfType<IDraggable>().ToArray();
        return affected;
    }

    private static void PointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not IDraggable draggable) return;
        if (!draggable.EndDrag(e)) return;
        draggable.PointerMoved -= PointerMoved;
        draggable.Container?.Classes.Remove("ChildDragging");
        if (e.Pointer.Captured == draggable)
            e.Pointer.Capture(null);
        var affected = GetTagged(draggable);
        if (affected == null) return;
        foreach (var control in affected)
        {
            if (control == draggable) continue;
            control.EndDrag(e);
        }
    }

    private static void PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (sender is not IDraggable draggable) return;
        if (!draggable.IsDragging) return;
        draggable.Container?.Classes.Remove("ChildDragging");
        draggable.EndDrag();
        draggable.PointerMoved -= PointerMoved;
        if (e.Pointer.Captured == draggable)
            e.Pointer.Capture(null);
        var affected = GetTagged(draggable);
        if (affected == null) return;
        foreach (var control in affected)
        {
            if (control == draggable) continue;
            control.EndDrag();
        }
    }


    private static void PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not IDraggable draggable) return;
        CurrentPointerKeyModifiers = e.KeyModifiers;
        draggable.Drag(e);

        var affected = GetTagged(draggable);
        if (affected == null) return;
        foreach (var control in affected)
        {
            if (control == draggable) continue;
            control.Drag(e);
        }
    }

    public static Point Snap(IDraggable draggable, Point point)
    {
        if (draggable is not Control control) return point;
        var root = draggable.Container;
        if (root is null) return point;
        if (GetIsSnappingPredicate(control)(control))
        {
            var snapX = GetXSnapModifier(control);
            var snapY = GetYSnapModifier(control);
            point = new Point(snapX(root, control, point.X), snapY(root, control, point.Y));
        }

        return point;
    }
}