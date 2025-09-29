using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Westermo.GraphX.Common.Enums;

namespace Westermo.GraphX.Controls.Avalonia
{
    public static class HighlightBehaviour
    {
        static HighlightBehaviour()
        {
            IsHighlightEnabledProperty.Changed.AddClassHandler<Control>(OnIsHighlightEnabledPropertyChanged);
        }

        #region Attached props

        //trigger
        public static readonly AttachedProperty<bool> HighlightedProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("Highlighted", typeof(HighlightBehaviour));

        //settings
        public static readonly AttachedProperty<bool> IsHighlightEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsHighlightEnabled", typeof(HighlightBehaviour));

        public static readonly AttachedProperty<GraphControlType> HighlightControlProperty =
            AvaloniaProperty.RegisterAttached<Control, GraphControlType>("HighlightControl", typeof(HighlightBehaviour),
                GraphControlType.VertexAndEdge);

        public static readonly AttachedProperty<EdgesType> HighlightEdgesProperty =
            AvaloniaProperty.RegisterAttached<Control, EdgesType>("HighlightEdges", typeof(HighlightBehaviour),
                EdgesType.Out);

        public static readonly AttachedProperty<HighlightedEdgeType> HighlightedEdgeTypeProperty =
            AvaloniaProperty.RegisterAttached<Control, HighlightedEdgeType>("HighlightedEdgeType",
                typeof(HighlightBehaviour),
                HighlightedEdgeType.None);

        public static HighlightedEdgeType GetHighlightedEdgeType(Control? obj)
        {
            return obj?.GetValue(HighlightedEdgeTypeProperty) ?? HighlightedEdgeType.None;
        }

        public static void SetHighlightedEdgeType(Control? obj, HighlightedEdgeType value)
        {
            obj?.SetValue(HighlightedEdgeTypeProperty, value);
        }

        public static bool GetIsHighlightEnabled(Control? obj)
        {
            return obj?.GetValue(IsHighlightEnabledProperty) ?? false;
        }

        public static void SetIsHighlightEnabled(Control? obj, bool value)
        {
            obj?.SetValue(IsHighlightEnabledProperty, value);
        }

        public static bool GetHighlighted(Control? obj)
        {
            return obj?.GetValue(HighlightedProperty) ?? false;
        }

        public static void SetHighlighted(Control? obj, bool value)
        {
            obj?.SetValue(HighlightedProperty, value);
        }

        public static GraphControlType GetHighlightControl(Control? obj)
        {
            return obj?.GetValue(HighlightControlProperty) ?? GraphControlType.VertexAndEdge;
        }

        public static void SetHighlightControl(Control? obj, GraphControlType value)
        {
            obj?.SetValue(HighlightControlProperty, value);
        }

        public static EdgesType GetHighlightEdges(Control? obj)
        {
            return obj?.GetValue(HighlightEdgesProperty) ?? EdgesType.Out;
        }

        public static void SetHighlightEdges(Control? obj, EdgesType value)
        {
            obj?.SetValue(HighlightEdgesProperty, value);
        }

        #endregion

        #region PropertyChanged callbacks

        private static void OnIsHighlightEnabledPropertyChanged(Control obj, AvaloniaPropertyChangedEventArgs e)
        {
            if (obj is not IInputElement element)
                return;

            if (e.NewValue is bool == false)
                return;

            if ((bool)e.NewValue)
            {
                //register the event handlers
                element.PointerEntered += Element_MouseEnter;
                element.PointerEntered += Element_MouseLeave;
            }
            else
            {
                //unregister the event handlers
                element.PointerEntered -= Element_MouseEnter;
                element.PointerEntered -= Element_MouseLeave;
            }
        }

        private static void Element_MouseLeave(object? sender, PointerEventArgs pointerEventArgs)
        {
            if (sender is Control == false) return;
            if (sender is not IGraphControl ctrl) return;
            if (ctrl.RootArea is null) return;

            var type = GetHighlightControl((Control)sender);
            var edgesType = GetHighlightEdges((Control)sender);
            SetHighlighted((Control)sender, false);

            if (type is GraphControlType.Vertex or GraphControlType.VertexAndEdge)
                foreach (var item in ctrl.RootArea.GetRelatedVertexControls(ctrl, edgesType).Cast<Control>())
                    SetHighlighted(item, false);

            switch (type)
            {
                case GraphControlType.Edge or GraphControlType.VertexAndEdge:
                {
                    foreach (var item in ctrl.RootArea.GetRelatedEdgeControls(ctrl, edgesType).Cast<Control>())
                    {
                        SetHighlighted(item, false);
                        SetHighlightedEdgeType(item, HighlightedEdgeType.None);
                    }

                    break;
                }
            }
        }

        private static void Element_MouseEnter(object? sender, PointerEventArgs pointerEventArgs)
        {
            if (sender is Control == false) return;
            if (sender is not IGraphControl ctrl) return;
            if (ctrl.RootArea is null) return;
            var type = GetHighlightControl((Control)sender);
            var edgesType = GetHighlightEdges((Control)sender);
            SetHighlighted((Control)sender, true);

            //highlight related vertices
            if (type is GraphControlType.Vertex or GraphControlType.VertexAndEdge)
                foreach (var item in ctrl.RootArea.GetRelatedVertexControls(ctrl, edgesType).Cast<Control>())
                    SetHighlighted(item, true);
            switch (type)
            {
                //highlight related edges
                case GraphControlType.Edge or GraphControlType.VertexAndEdge:
                {
                    //separetely get in and out edges to set direction flag
                    if (edgesType is EdgesType.In or EdgesType.All)
                        foreach (var item in ctrl.RootArea.GetRelatedEdgeControls(ctrl, EdgesType.In)
                                     .Cast<Control>())
                        {
                            SetHighlighted(item, true);
                            SetHighlightedEdgeType(item, HighlightedEdgeType.In);
                        }

                    if (edgesType is EdgesType.Out or EdgesType.All)
                        foreach (var item in ctrl.RootArea.GetRelatedEdgeControls(ctrl, EdgesType.Out)
                                     .Cast<Control>())
                        {
                            SetHighlighted(item, true);
                            SetHighlightedEdgeType(item, HighlightedEdgeType.Out);
                        }

                    break;
                }
            }
        }

        #endregion

        public enum HighlightType
        {
            Vertex,
            Edge,
            VertexAndEdge
        }

        public enum HighlightedEdgeType
        {
            In,
            Out,
            None
        }
    }
}