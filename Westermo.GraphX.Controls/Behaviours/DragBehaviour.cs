﻿using System;
using System.Diagnostics;
using System.Windows;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Controls
{
    /// <summary>
    /// Dragging behavior of objects in a GraphX graph area is influenced using the attached properties of this class.
    ///
    /// To enable dragging of an individual object, set the IsDragEnabled attached property to true on that object. When IsDragEnabled is true, the
    /// object can be used to initiate dragging.
    ///
    /// To drag a group of vertices, set the IsTagged attached property to true for all of the vertices in the group. When dragging is started from
    /// one of the tagged vertices, all of the tagged ones will be move.
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
        public delegate double SnapModifierFunc(GraphAreaBase area, DependencyObject obj, double val);

        #region Built-in snapping behavior

        private static readonly Predicate<DependencyObject>? _builtinIsSnappingPredicate = _ =>
            System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift;

        private static readonly Predicate<DependencyObject> _builtinIsIndividualSnappingPredicate = _ => false;

        private static readonly SnapModifierFunc? _builtinSnapModifier = (_, _, val) => Math.Round(val * 0.1) * 10.0;

        #endregion Built-in snapping behavior

        #region Global snapping behavior management

        private static Predicate<DependencyObject>? _globalIsSnappingPredicate = _builtinIsSnappingPredicate;

        /// <summary>
        /// Gets or sets the predicate used to determine whether to snap an object. The global predicate is used whenever the
        /// primary dragged object does not have a different predicate set using the IsSnappingPredicate attached property.
        /// </summary>
        /// <remarks>
        /// Setting to null will restore the built in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static Predicate<DependencyObject>? GlobalIsSnappingPredicate
        {
            get => _globalIsSnappingPredicate;
            set => _globalIsSnappingPredicate = value ?? _builtinIsSnappingPredicate;
        }

        private static Predicate<DependencyObject> _globalIsIndividualSnappingPredicate =
            _builtinIsIndividualSnappingPredicate;

        /// <summary>
        /// Gets or sets the predicate used to determine whether to perform individual snapping on a group of dragged objects.
        /// The global predicate is used whenever the primary dragged object does not have a different predicate set using the
        /// IsIndividualSnappingPredicate attached property.
        /// </summary>
        /// <remarks>
        /// Setting to null will restore the built in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static Predicate<DependencyObject>? GlobalIsIndividualSnappingPredicate
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
        /// Setting to null will restore the built in behavior, but it is recommended to track the preceding value and restore that.
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
        /// Setting to null will restore the built in behavior, but it is recommended to track the preceding value and restore that.
        /// </remarks>
        public static SnapModifierFunc? GlobalYSnapModifier
        {
            get => _globalYSnapModifier;
            set => _globalYSnapModifier = value ?? _builtinSnapModifier;
        }

        #endregion Global snapping behavior management

        #region Attached DPs

        public static readonly DependencyProperty IsDragEnabledProperty =
            DependencyProperty.RegisterAttached("IsDragEnabled", typeof(bool), typeof(DragBehaviour),
                new PropertyMetadata(false, OnIsDragEnabledPropertyChanged));

        public static readonly DependencyProperty UpdateEdgesOnMoveProperty =
            DependencyProperty.RegisterAttached("UpdateEdgesOnMove", typeof(bool), typeof(DragBehaviour),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsTaggedProperty =
            DependencyProperty.RegisterAttached("IsTagged", typeof(bool), typeof(DragBehaviour),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.RegisterAttached("IsDragging", typeof(bool), typeof(DragBehaviour),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsSnappingPredicateProperty = DependencyProperty.RegisterAttached(
            "IsSnappingPredicate", typeof(Predicate<DependencyObject>), typeof(DragBehaviour),
            new PropertyMetadata(new Predicate<DependencyObject>(obj => { return _globalIsSnappingPredicate(obj); })));

        public static readonly DependencyProperty IsIndividualSnappingPredicateProperty =
            DependencyProperty.RegisterAttached("IsIndividualSnappingPredicate", typeof(Predicate<DependencyObject>),
                typeof(DragBehaviour),
                new PropertyMetadata(
                    new Predicate<DependencyObject>(obj => _globalIsIndividualSnappingPredicate(obj))));

        /// <summary>
        /// Snap feature modifier delegate for X axis
        /// </summary>
        public static readonly DependencyProperty XSnapModifierProperty =
            DependencyProperty.RegisterAttached("XSnapModifier", typeof(SnapModifierFunc), typeof(DragBehaviour),
                new PropertyMetadata(new SnapModifierFunc((area, obj, val) => _globalXSnapModifier(area, obj, val))));

        /// <summary>
        /// Snap feature modifier delegate for Y axis
        /// </summary>
        public static readonly DependencyProperty YSnapModifierProperty =
            DependencyProperty.RegisterAttached("YSnapModifier", typeof(SnapModifierFunc), typeof(DragBehaviour),
                new PropertyMetadata(new SnapModifierFunc((area, obj, val) => _globalYSnapModifier(area, obj, val))));

        private static readonly DependencyProperty OriginalXProperty =
            DependencyProperty.RegisterAttached("OriginalX", typeof(double), typeof(DragBehaviour),
                new PropertyMetadata(0.0));

        private static readonly DependencyProperty OriginalYProperty =
            DependencyProperty.RegisterAttached("OriginalY", typeof(double), typeof(DragBehaviour),
                new PropertyMetadata(0.0));

        private static readonly DependencyProperty OriginalMouseXProperty =
            DependencyProperty.RegisterAttached("OriginalMouseX", typeof(double), typeof(DragBehaviour),
                new PropertyMetadata(0.0));

        private static readonly DependencyProperty OriginalMouseYProperty =
            DependencyProperty.RegisterAttached("OriginalMouseY", typeof(double), typeof(DragBehaviour),
                new PropertyMetadata(0.0));

        #endregion Attached DPs

        #region Get/Set method for Attached Properties

        public static bool GetUpdateEdgesOnMove(DependencyObject obj)
        {
            return (bool)obj.GetValue(UpdateEdgesOnMoveProperty);
        }

        public static void SetUpdateEdgesOnMove(DependencyObject obj, bool value)
        {
            obj.SetValue(UpdateEdgesOnMoveProperty, value);
        }

        public static bool GetIsTagged(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsTaggedProperty);
        }

        public static void SetIsTagged(DependencyObject obj, bool value)
        {
            obj.SetValue(IsTaggedProperty, value);
        }

        public static bool GetIsDragEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragEnabledProperty);
        }

        public static void SetIsDragEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragEnabledProperty, value);
        }

        public static bool GetIsDragging(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDraggingProperty);
        }

        public static void SetIsDragging(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDraggingProperty, value);
        }

        public static Predicate<DependencyObject> GetIsSnappingPredicate(DependencyObject obj)
        {
            return (Predicate<DependencyObject>)obj.GetValue(IsSnappingPredicateProperty);
        }

        public static void SetIsSnappingPredicate(DependencyObject obj, Predicate<DependencyObject> value)
        {
            obj.SetValue(IsSnappingPredicateProperty, value);
        }

        public static Predicate<DependencyObject> GetIsIndividualSnappingPredicate(DependencyObject obj)
        {
            return (Predicate<DependencyObject>)obj.GetValue(IsIndividualSnappingPredicateProperty);
        }

        public static void SetIsIndividualSnappingPredicate(DependencyObject obj, Predicate<DependencyObject> value)
        {
            obj.SetValue(IsIndividualSnappingPredicateProperty, value);
        }

        public static SnapModifierFunc GetXSnapModifier(DependencyObject obj)
        {
            return (SnapModifierFunc)obj.GetValue(XSnapModifierProperty);
        }

        public static void SetXSnapModifier(DependencyObject obj, SnapModifierFunc value)
        {
            obj.SetValue(XSnapModifierProperty, value);
        }

        public static SnapModifierFunc GetYSnapModifier(DependencyObject obj)
        {
            return (SnapModifierFunc)obj.GetValue(YSnapModifierProperty);
        }

        public static void SetYSnapModifier(DependencyObject obj, SnapModifierFunc value)
        {
            obj.SetValue(YSnapModifierProperty, value);
        }

        #endregion Get/Set method for Attached Properties

        #region Get/Set methods for private Attached Properties

        private static double GetOriginalX(DependencyObject obj)
        {
            return (double)obj.GetValue(OriginalXProperty);
        }

        private static void SetOriginalX(DependencyObject obj, double value)
        {
            obj.SetValue(OriginalXProperty, value);
        }

        private static double GetOriginalY(DependencyObject obj)
        {
            return (double)obj.GetValue(OriginalYProperty);
        }

        private static void SetOriginalY(DependencyObject obj, double value)
        {
            obj.SetValue(OriginalYProperty, value);
        }

        private static double GetOriginalMouseX(DependencyObject obj)
        {
            return (double)obj.GetValue(OriginalMouseXProperty);
        }

        private static void SetOriginalMouseX(DependencyObject obj, double value)
        {
            obj.SetValue(OriginalMouseXProperty, value);
        }

        private static double GetOriginalMouseY(DependencyObject obj)
        {
            return (double)obj.GetValue(OriginalMouseYProperty);
        }

        private static void SetOriginalMouseY(DependencyObject obj, double value)
        {
            obj.SetValue(OriginalMouseYProperty, value);
        }

        #endregion Get/Set methods for private Attached Properties

        #region PropertyChanged callbacks

        private static void OnIsDragEnabledPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
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
                    element.MouseLeftButtonDown += OnVertexDragStarted;
                    element.PreviewMouseLeftButtonUp += OnVertexDragFinished;
                }
                else if (element is EdgeControl)
                {
                    element.MouseLeftButtonDown += OnEdgeDrageStarted;
                    element.PreviewMouseLeftButtonUp += OnEdgeDragFinished;
                }
            }
            else
            {
                switch (element)
                {
                    //unregister the event handlers
                    case VertexControl:
                        element.MouseLeftButtonDown -= OnVertexDragStarted;
                        element.PreviewMouseLeftButtonUp -= OnVertexDragFinished;
                        break;
                    case EdgeControl:
                        element.MouseLeftButtonDown -= OnEdgeDrageStarted;
                        element.PreviewMouseLeftButtonUp -= OnEdgeDragFinished;
                        break;
                }
            }
        }

        #endregion PropertyChanged callbacks

        private static void OnEdgeDrageStarted(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var obj = (DependencyObject)sender;

            SetIsDragging(obj, true);

            if (obj is IInputElement element)
            {
                element.CaptureMouse();
                element.MouseMove -= OnEdgeDragging;
                element.MouseMove += OnEdgeDragging;
            }
            else
                throw new GX_InvalidDataException(
                    "The control must be a descendent of the FrameworkElement or FrameworkContentElement!");

            e.Handled = false;
        }

        private static void OnEdgeDragging(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var obj = (DependencyObject)sender;
            if (!GetIsDragging(obj))
                return;

            var edgeControl = (EdgeControl)sender;

            edgeControl.PrepareEdgePathFromMousePointer();

            e.Handled = true;
        }

        private static void OnEdgeDragFinished(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

                var obj = (DependencyObject)sender;
                SetIsDragging(obj, false);
                //obj.ClearValue(OriginalMouseXProperty);
                //obj.ClearValue(OriginalMouseYProperty);
                //obj.ClearValue(OriginalXProperty);
                //obj.ClearValue(OriginalYProperty);

                if (sender is IInputElement element)
                {
                    element.MouseMove -= OnVertexDragging;
                    element.ReleaseMouseCapture();
                }
            }
        }

        private static void OnVertexDragStarted(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var obj = (DependencyObject)sender;
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
                element.CaptureMouse();
                element.MouseMove -= OnVertexDragging;
                element.MouseMove += OnVertexDragging;
            }

            //else throw new GX_InvalidDataException("The control must be a descendent of the FrameworkElement or FrameworkContentElement!");
            e.Handled = false;
        }

        private static void OnVertexDragFinished(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UpdateVertexEdges((VertexControl)sender);

            var obj = (DependencyObject)sender;
            SetIsDragging(obj, false);
            obj.ClearValue(OriginalMouseXProperty);
            obj.ClearValue(OriginalMouseYProperty);
            obj.ClearValue(OriginalXProperty);
            obj.ClearValue(OriginalYProperty);
            if (GetIsTagged(obj))
            {
                var area = GetAreaFromObject(obj);
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
                element.MouseMove -= OnVertexDragging;
                element.ReleaseMouseCapture();
            }
        }

        private static void OnVertexDragging(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var obj = (DependencyObject)sender;
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
            var ra = vc.RootArea ?? throw new GX_InvalidDataException("OnDragFinished() - IGraphControl object must always have RootArea property set!");
            if (!ra.IsEdgeRoutingEnabled) return;
            ra.ComputeEdgeRoutesByVertex(vc);
            vc.InvalidateVisual();
        }

        private static void UpdateCoordinates(GraphAreaBase area, DependencyObject obj, double horizontalChange,
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

        private static Point GetPositionInArea(GraphAreaBase? area, System.Windows.Input.MouseEventArgs e)
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
            GraphAreaBase? area = null;

            if (obj is VertexControl control1)
                area = control1.RootArea;
            else if (obj is EdgeControl control)
                area = control.RootArea;
            else if (obj is DependencyObject @object)
                area =
                    VisualTreeHelperEx.FindAncestorByType(@object, typeof(GraphAreaBase), false) as
                        GraphAreaBase;

            return area;
        }
    }
}