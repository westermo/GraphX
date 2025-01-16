using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Controls.Avalonia
{
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
    public static class DragBehaviour
    {
        public delegate double SnapModifierFunc(GraphAreaBase area, Control obj, double val);

        #region Built-in snapping behavior

        private static readonly Predicate<Control>? _builtinIsSnappingPredicate = _ => false;

        private static readonly Predicate<Control> _builtinIsIndividualSnappingPredicate = _ => false;

        private static readonly SnapModifierFunc? _builtinSnapModifier = (_, _, val) => Math.Round(val * 0.1) * 10.0;

        #endregion Built-in snapping behavior

        #region Global snapping behavior management

        private static Predicate<Control>? _globalIsSnappingPredicate = _builtinIsSnappingPredicate;

        /// <summary>
        /// Gets or sets the predicate used to determine whether to snap an object. The global predicate is used whenever the
        /// primary dragged object does not have a different predicate set using the IsSnappingPredicate attached property.
        /// </summary>
        /// <remarks>
        /// Setting to null will restore the built-in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static Predicate<Control>? GlobalIsSnappingPredicate
        {
            get => _globalIsSnappingPredicate;
            set => _globalIsSnappingPredicate = value ?? _builtinIsSnappingPredicate;
        }

        private static Predicate<Control> _globalIsIndividualSnappingPredicate =
            _builtinIsIndividualSnappingPredicate;

        /// <summary>
        /// Gets or sets the predicate used to determine whether to perform individual snapping on a group of dragged objects.
        /// The global predicate is used whenever the primary dragged object does not have a different predicate set using the
        /// IsIndividualSnappingPredicate attached property.
        /// </summary>
        /// <remarks>
        /// Setting to null will restore the built-in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static Predicate<Control>? GlobalIsIndividualSnappingPredicate
        {
            get => _globalIsIndividualSnappingPredicate;
            set => _globalIsIndividualSnappingPredicate = value ?? _builtinIsIndividualSnappingPredicate;
        }

        private static SnapModifierFunc? _globalXSnapModifier = _builtinSnapModifier;

        /// <summary>
        /// Gets or sets the X value modifier to use when snapping an object. The global modifier is used whenever the
        /// primary dragged object does not have a different modifier set using the XSnapModifier attached property.
        /// </summary>
        /// <remarks>
        /// Setting to null will restore the built-in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static SnapModifierFunc? GlobalXSnapModifier
        {
            get => _globalXSnapModifier;
            set => _globalXSnapModifier = value ?? _builtinSnapModifier;
        }

        private static SnapModifierFunc? _globalYSnapModifier = _builtinSnapModifier;

        /// <summary>
        /// Gets or sets the Y value modifier to use when snapping an object. The global modifier is used whenever the
        /// primary dragged object does not have a different modifier set using the YSnapModifier attached property.
        /// </summary>
        /// <remarks>
        /// Setting to null will restore the built-in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static SnapModifierFunc? GlobalYSnapModifier
        {
            get => _globalYSnapModifier;
            set => _globalYSnapModifier = value ?? _builtinSnapModifier;
        }

        #endregion Global snapping behavior management

        #region Attached DPs

        static DragBehaviour()
        {
            IsDragEnabledProperty.Changed.AddClassHandler<Control>(OnIsDragEnabledPropertyChanged);
        }

        public static readonly AttachedProperty<bool> IsDragEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsDragEnabled", typeof(DragBehaviour));

        public static readonly AttachedProperty<bool> UpdateEdgesOnMoveProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("UpdateEdgesOnMove", typeof(DragBehaviour));

        public static readonly AttachedProperty<bool> IsTaggedProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsTagged", typeof(DragBehaviour));

        public static readonly AttachedProperty<bool> IsDraggingProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsDragging", typeof(DragBehaviour));

        public static readonly AttachedProperty<Predicate<Control>> IsSnappingPredicateProperty =
            AvaloniaProperty.RegisterAttached<Control, Predicate<Control>>(
                "IsSnappingPredicate", typeof(DragBehaviour), obj => _globalIsSnappingPredicate(obj));

        public static readonly AttachedProperty<Predicate<Control>> IsIndividualSnappingPredicateProperty =
            AvaloniaProperty.RegisterAttached<Control, Predicate<Control>>("IsIndividualSnappingPredicate",
                typeof(DragBehaviour),
                _globalIsIndividualSnappingPredicate);

        /// <summary>
        /// Snap feature modifier delegate for X axis
        /// </summary>
        public static readonly AttachedProperty<SnapModifierFunc> XSnapModifierProperty =
            AvaloniaProperty.RegisterAttached<Control, SnapModifierFunc>("XSnapModifier", typeof(DragBehaviour),
                _globalXSnapModifier);

        /// <summary>
        /// Snap feature modifier delegate for Y axis
        /// </summary>
        public static readonly AttachedProperty<SnapModifierFunc> YSnapModifierProperty =
            AvaloniaProperty.RegisterAttached<Control, SnapModifierFunc>("YSnapModifier", typeof(DragBehaviour),
                _globalYSnapModifier);

        private static readonly AttachedProperty<double> OriginalXProperty =
            AvaloniaProperty.RegisterAttached<Control, double>("OriginalX", typeof(DragBehaviour));

        private static readonly AttachedProperty<double> OriginalYProperty =
            AvaloniaProperty.RegisterAttached<Control, double>("OriginalY", typeof(DragBehaviour));

        private static readonly AttachedProperty<double> OriginalMouseXProperty =
            AvaloniaProperty.RegisterAttached<Control, double>("OriginalMouseX", typeof(DragBehaviour));

        private static readonly AttachedProperty<double> OriginalMouseYProperty =
            AvaloniaProperty.RegisterAttached<Control, double>("OriginalMouseY", typeof(DragBehaviour));

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

        public static bool GetIsDragging(Control obj)
        {
            return obj.GetValue(IsDraggingProperty);
        }

        public static void SetIsDragging(Control obj, bool value)
        {
            obj.SetValue(IsDraggingProperty, value);
        }

        public static Predicate<Control> GetIsSnappingPredicate(Control obj)
        {
            return obj.GetValue(IsSnappingPredicateProperty);
        }

        public static void SetIsSnappingPredicate(Control obj, Predicate<Control> value)
        {
            obj.SetValue(IsSnappingPredicateProperty, value);
        }

        public static Predicate<Control> GetIsIndividualSnappingPredicate(Control obj)
        {
            return obj.GetValue(IsIndividualSnappingPredicateProperty);
        }

        public static void SetIsIndividualSnappingPredicate(Control obj, Predicate<Control> value)
        {
            obj.SetValue(IsIndividualSnappingPredicateProperty, value);
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

        #region Get/Set methods for private Attached Properties

        private static double GetOriginalX(Control obj)
        {
            return obj.GetValue(OriginalXProperty);
        }

        private static void SetOriginalX(Control obj, double value)
        {
            obj.SetValue(OriginalXProperty, value);
        }

        private static double GetOriginalY(Control obj)
        {
            return obj.GetValue(OriginalYProperty);
        }

        private static void SetOriginalY(Control obj, double value)
        {
            obj.SetValue(OriginalYProperty, value);
        }

        private static double GetOriginalMouseX(Control obj)
        {
            return obj.GetValue(OriginalMouseXProperty);
        }

        private static void SetOriginalMouseX(Control obj, double value)
        {
            obj.SetValue(OriginalMouseXProperty, value);
        }

        private static double GetOriginalMouseY(Control obj)
        {
            return obj.GetValue(OriginalMouseYProperty);
        }

        private static void SetOriginalMouseY(Control obj, double value)
        {
            obj.SetValue(OriginalMouseYProperty, value);
        }

        #endregion Get/Set methods for private Attached Properties

        #region PropertyChanged callbacks

        private static void OnIsDragEnabledPropertyChanged(Control obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj is not IInputElement element)
                return;

            if (e.NewValue is bool == false)
                return;

            if ((bool)e.NewValue)
            {
                //register the event handlers
                if (element is VertexControl)
                {
                    element.PointerReleased += OnVertexDragFinished;
                    element.PointerPressed += OnVertexDragStarted;
                }
                else if (element is EdgeControl)
                {
                    element.PointerPressed += OnEdgeDragStarted;
                    element.PointerReleased += OnEdgeDragFinished;
                }
            }
            else
            {
                switch (element)
                {
                    //unregister the event handlers
                    case VertexControl:
                        element.PointerPressed -= OnVertexDragStarted;
                        element.PointerReleased -= OnVertexDragFinished;
                        break;
                    case EdgeControl:
                        element.PointerPressed -= OnEdgeDragStarted;
                        element.PointerReleased -= OnEdgeDragFinished;
                        break;
                }
            }
        }

        #endregion PropertyChanged callbacks

        private static void OnEdgeDragStarted(object? sender, PointerPressedEventArgs e)
        {
            var obj = (Control?)sender;
            if (obj is null) return;
            SetIsDragging(obj, true);

            if (obj is IInputElement element)
            {
                element.PointerMoved -= OnEdgeDragging;
                element.PointerMoved += OnEdgeDragging;
            }
            else
                throw new GX_InvalidDataException(
                    "The control must be a descendent of the Control or FrameworkContentElement!");

            e.Handled = false;
        }

        private static void OnEdgeDragging(object? sender, PointerEventArgs e)
        {
            var obj = (Control?)sender;
            if (obj is null) return;
            if (!GetIsDragging(obj))
                return;

            var edgeControl = (EdgeControl?)sender;
            if (edgeControl is null) return;
            edgeControl.PrepareEdgePathFromMousePointer(e);

            e.Handled = true;
        }

        private static void OnEdgeDragFinished(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is not EdgeControl edgeControl) return;

            var graphAreaBase = edgeControl.RootArea;

            var vertexControl = graphAreaBase.GetVertexControlAt(e.GetPosition(graphAreaBase));

            if (vertexControl != null)
            {
                edgeControl.Target = vertexControl;

                if (vertexControl.VertexConnectionPointsList.Count > 0)
                {
                    var vertexConnectionPoint = vertexControl.GetConnectionPointAt(e.GetPosition(graphAreaBase));

                    var edge = (IGraphXCommonEdge)edgeControl.Edge!;

                    if (vertexConnectionPoint != null)
                    {
                        edge.TargetConnectionPointId = vertexConnectionPoint.Id;
                    }
                    else
                    {
                        edge.TargetConnectionPointId = null;
                    }
                }

                edgeControl.UpdateEdge();

                var obj = (Control)sender;
                SetIsDragging(obj, false);

                if (sender is IInputElement element)
                {
                    element.PointerMoved -= OnVertexDragging;
                }
            }
        }

        private static void OnVertexDragStarted(object? sender, PointerPressedEventArgs e)
        {
            var obj = (Control?)sender;
            if (obj is null) return;
            //we are starting the drag

            SetIsDragging(obj, true);

            // Save the position of the mouse to the start position
            var area = GetAreaFromObject(obj);
            var pos = GetPositionInArea(area, e);
            SetOriginalMouseX(obj, pos.X);
            SetOriginalMouseY(obj, pos.Y);

            // Save the position of the dragged object to its starting position
            SetOriginalX(obj, GraphAreaBase.GetFinalX(obj));
            SetOriginalY(obj, GraphAreaBase.GetFinalY(obj));

            // Save starting position of all other tagged elements
            foreach (var item in area!.GetAllVertexControls())
                if (!ReferenceEquals(item, obj) && GetIsTagged(item))
                {
                    SetOriginalX(item, GraphAreaBase.GetFinalX(item));
                    SetOriginalY(item, GraphAreaBase.GetFinalY(item));
                }

            //capture the mouse
            if (obj is IInputElement element)
            {
                element.PointerMoved -= OnVertexDragging;
                element.PointerMoved += OnVertexDragging;
            }

            //else throw new GX_InvalidDataException("The control must be a descendent of the Control or FrameworkContentElement!");
            e.Handled = false;
        }

        private static void OnVertexDragFinished(object? sender, PointerReleasedEventArgs pointerReleasedEventArgs)
        {
            if (sender is not VertexControl vertexControl) return;
            UpdateVertexEdges(vertexControl);
            SetIsDragging(vertexControl, false);
            vertexControl.ClearValue(OriginalMouseXProperty);
            vertexControl.ClearValue(OriginalMouseYProperty);
            vertexControl.ClearValue(OriginalXProperty);
            vertexControl.ClearValue(OriginalYProperty);
            if (GetIsTagged(vertexControl))
            {
                var area = GetAreaFromObject(vertexControl);
                foreach (var item in area!.GetAllVertexControls())
                    if (GetIsTagged(item))
                    {
                        item.ClearValue(OriginalXProperty);
                        item.ClearValue(OriginalYProperty);
                    }
            }

            //we finished the drag, release the mouse
            if (sender is IInputElement element)
            {
                element.PointerMoved -= OnVertexDragging;
            }
        }

        private static void OnVertexDragging(object? sender, PointerEventArgs e)
        {
            var obj = (Control?)sender;
            if (obj is null) return;
            if (!GetIsDragging(obj))
                return;

            var area = GetAreaFromObject(obj);
            var pos = GetPositionInArea(area, e);

            var horizontalChange = pos.X - GetOriginalMouseX(obj);
            var verticalChange = pos.Y - GetOriginalMouseY(obj);

            // Determine whether to use snapping
            var snap = GetIsSnappingPredicate(obj)(obj);
            var individualSnap = false;
            // Snap modifier functions to apply to the primary dragged object
            SnapModifierFunc? snapXMod = null;
            SnapModifierFunc? snapYMod = null;
            // Snap modifier functions to apply to other dragged objects if they snap individually instead of moving
            // the same amounts as the primary object.
            SnapModifierFunc? individualSnapXMod = null;
            SnapModifierFunc? individualSnapYMod = null;
            if (snap)
            {
                snapXMod = GetXSnapModifier(obj);
                snapYMod = GetYSnapModifier(obj);
                // If objects snap to grid individually instead of moving the same amount as the primary dragged object,
                // use the same snap modifier on each individual object.
                individualSnap = GetIsIndividualSnappingPredicate(obj)(obj);
                if (individualSnap)
                {
                    individualSnapXMod = snapXMod;
                    individualSnapYMod = snapYMod;
                }
            }

            if (GetIsTagged(obj))
            {
                // When the dragged item is a tagged item, we could be dragging a group of objects. If the dragged object is a vertex, it's
                // automatically the primary object of the drag. If the dragged object is an edge, prefer the source vertex, but accept the
                // target vertex as the primary object of the drag and start with that.
                var primaryDragVertex = obj as VertexControl;
                if (primaryDragVertex == null)
                {
                    if (obj is EdgeControl ec)
                        primaryDragVertex = ec.Source ?? ec.Target;

                    if (primaryDragVertex == null)
                    {
                        Debug.WriteLine("OnDragging() -> Tagged and dragged the wrong object?");
                        return;
                    }
                }

                UpdateCoordinates(area!, primaryDragVertex, horizontalChange, verticalChange, snapXMod, snapYMod);

                if (!individualSnap)
                {
                    // When dragging groups of objects that all move the same amount (not snapping individually, but tracking with
                    // the movement of the primary dragged object), deterrmine how much offset the primary dragged object experienced
                    // and use that offset for the rest.
                    horizontalChange = GraphAreaBase.GetFinalX(primaryDragVertex) - GetOriginalX(primaryDragVertex);
                    verticalChange = GraphAreaBase.GetFinalY(primaryDragVertex) - GetOriginalY(primaryDragVertex);
                }

                foreach (var item in area!.GetAllVertexControls())
                    if (!ReferenceEquals(item, primaryDragVertex) && GetIsTagged(item))
                        UpdateCoordinates(area, item, horizontalChange, verticalChange, individualSnapXMod,
                            individualSnapYMod);
            }
            else UpdateCoordinates(area!, obj, horizontalChange, verticalChange, snapXMod, snapYMod);

            e.Handled = true;
        }

        private static void UpdateVertexEdges(VertexControl vc)
        {
            if (vc.Vertex == null) return;
            var ra = vc.RootArea ??
                     throw new GX_InvalidDataException(
                         "OnDragFinished() - IGraphControl object must always have RootArea property set!");
            if (!ra.IsEdgeRoutingEnabled) return;
            ra.ComputeEdgeRoutesByVertex(vc);
            vc.InvalidateVisual();
        }

        private static void UpdateCoordinates(GraphAreaBase area, Control obj, double horizontalChange,
            double verticalChange, SnapModifierFunc? xSnapModifier, SnapModifierFunc? ySnapModifier)
        {
            if (double.IsNaN(GraphAreaBase.GetX(obj)))
                GraphAreaBase.SetX(obj, 0);
            if (double.IsNaN(GraphAreaBase.GetY(obj)))
                GraphAreaBase.SetY(obj, 0, true);

            //move the object
            var x = GetOriginalX(obj) + horizontalChange;
            if (xSnapModifier != null)
                x = xSnapModifier(area, obj, x);
            GraphAreaBase.SetX(obj, x);

            var y = GetOriginalY(obj) + verticalChange;
            if (ySnapModifier != null)
                y = ySnapModifier(area, obj, y);
            GraphAreaBase.SetY(obj, y, true);

            if (GetUpdateEdgesOnMove(obj))
                UpdateVertexEdges((VertexControl)obj);

            //Debug.WriteLine("({0:##0.00000}, {1:##0.00000})", x, y);
        }

        private static Point GetPositionInArea(GraphAreaBase? area, PointerEventArgs e)
        {
            if (area != null)
            {
                var pos = e.GetPosition(area);
                return pos;
            }

            throw new GX_InvalidDataException(
                "DragBehavior.GetPositionInArea() - The input element must be a child of a GraphAreaBase.");
        }

        private static GraphAreaBase? GetAreaFromObject(object obj)
        {
            return obj switch
            {
                VertexControl control1 => control1.RootArea,
                EdgeControl control => control.RootArea,
                Control @object => @object.FindAncestorOfType<GraphAreaBase>(true),
                _ => null
            };
        }
    }
}