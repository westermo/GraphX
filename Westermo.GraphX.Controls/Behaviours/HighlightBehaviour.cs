using System.Linq;
using System.Windows;
using System.Windows.Input;
using Westermo.GraphX.Common.Enums;

namespace Westermo.GraphX.Controls;

public static class HighlightBehaviour
{
    #region Attached props

    //trigger
    public static readonly DependencyProperty HighlightedProperty =
        DependencyProperty.RegisterAttached("Highlighted", typeof(bool), typeof(HighlightBehaviour),
            new PropertyMetadata(false));

    //settings
    public static readonly DependencyProperty IsHighlightEnabledProperty =
        DependencyProperty.RegisterAttached("IsHighlightEnabled", typeof(bool), typeof(HighlightBehaviour),
            new PropertyMetadata(false, OnIsHighlightEnabledPropertyChanged));

    public static readonly DependencyProperty HighlightControlProperty =
        DependencyProperty.RegisterAttached("HighlightControl", typeof(GraphControlType),
            typeof(HighlightBehaviour), new PropertyMetadata(GraphControlType.VertexAndEdge));

    public static readonly DependencyProperty HighlightEdgesProperty =
        DependencyProperty.RegisterAttached("HighlightEdges", typeof(EdgesType), typeof(HighlightBehaviour),
            new PropertyMetadata(EdgesType.Out));

    public static readonly DependencyProperty HighlightedEdgeTypeProperty =
        DependencyProperty.RegisterAttached("HighlightedEdgeType", typeof(HighlightedEdgeType),
            typeof(HighlightBehaviour), new PropertyMetadata(HighlightedEdgeType.None));

    public static HighlightedEdgeType GetHighlightedEdgeType(DependencyObject obj)
    {
        return (HighlightedEdgeType)obj.GetValue(HighlightedEdgeTypeProperty);
    }

    public static void SetHighlightedEdgeType(DependencyObject obj, HighlightedEdgeType value)
    {
        obj.SetValue(HighlightedEdgeTypeProperty, value);
    }

    public static bool GetIsHighlightEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsHighlightEnabledProperty);
    }

    public static void SetIsHighlightEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsHighlightEnabledProperty, value);
    }

    public static bool GetHighlighted(DependencyObject obj)
    {
        return (bool)obj.GetValue(HighlightedProperty);
    }

    public static void SetHighlighted(DependencyObject obj, bool value)
    {
        obj.SetValue(HighlightedProperty, value);
    }

    public static GraphControlType GetHighlightControl(DependencyObject obj)
    {
        return (GraphControlType)obj.GetValue(HighlightControlProperty);
    }

    public static void SetHighlightControl(DependencyObject obj, GraphControlType value)
    {
        obj.SetValue(HighlightControlProperty, value);
    }

    public static EdgesType GetHighlightEdges(DependencyObject obj)
    {
        return (EdgesType)obj.GetValue(HighlightEdgesProperty);
    }

    public static void SetHighlightEdges(DependencyObject obj, EdgesType value)
    {
        obj.SetValue(HighlightEdgesProperty, value);
    }

    #endregion

    #region PropertyChanged callbacks

    private static void OnIsHighlightEnabledPropertyChanged(DependencyObject obj,
        DependencyPropertyChangedEventArgs e)
    {
        if (obj is not IInputElement element)
            return;

        if (e.NewValue is bool == false)
            return;

        if ((bool)e.NewValue)
        {
            //register the event handlers
            element.MouseEnter += Element_MouseEnter;
            element.MouseLeave += Element_MouseLeave;
        }
        else
        {
            //unregister the event handlers
            element.MouseEnter -= Element_MouseEnter;
            element.MouseLeave -= Element_MouseLeave;
        }
    }

    private static void Element_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is DependencyObject == false) return;
        if (sender is not IGraphControl ctrl) return;

        var type = GetHighlightControl((DependencyObject)sender);
        var edgesType = GetHighlightEdges((DependencyObject)sender);
        SetHighlighted((DependencyObject)sender, false);

        if (type == GraphControlType.Vertex || type == GraphControlType.VertexAndEdge)
            foreach (var item in ctrl.RootArea.GetRelatedVertexControls(ctrl, edgesType).Cast<DependencyObject>())
                SetHighlighted(item, false);

        if (type == GraphControlType.Edge || type == GraphControlType.VertexAndEdge)
            foreach (var item in ctrl.RootArea.GetRelatedEdgeControls(ctrl, edgesType).Cast<DependencyObject>())
            {
                SetHighlighted(item, false);
                SetHighlightedEdgeType(item, HighlightedEdgeType.None);
            }
    }

    private static void Element_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is DependencyObject == false) return;
        if (sender is not IGraphControl ctrl) return;

        var type = GetHighlightControl((DependencyObject)sender);
        var edgesType = GetHighlightEdges((DependencyObject)sender);
        SetHighlighted((DependencyObject)sender, true);

        //highlight related vertices
        if (type == GraphControlType.Vertex || type == GraphControlType.VertexAndEdge)
            foreach (var item in ctrl.RootArea.GetRelatedVertexControls(ctrl, edgesType).Cast<DependencyObject>())
                SetHighlighted(item, true);
        //highlight related edges
        if (type == GraphControlType.Edge || type == GraphControlType.VertexAndEdge)
        {
            //separetely get in and out edges to set direction flag
            if (edgesType == EdgesType.In || edgesType == EdgesType.All)
                foreach (var item in ctrl.RootArea.GetRelatedEdgeControls(ctrl, EdgesType.In)
                             .Cast<DependencyObject>())
                {
                    SetHighlighted(item, true);
                    SetHighlightedEdgeType(item, HighlightedEdgeType.In);
                }

            if (edgesType == EdgesType.Out || edgesType == EdgesType.All)
                foreach (var item in ctrl.RootArea.GetRelatedEdgeControls(ctrl, EdgesType.Out)
                             .Cast<DependencyObject>())
                {
                    SetHighlighted(item, true);
                    SetHighlightedEdgeType(item, HighlightedEdgeType.Out);
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