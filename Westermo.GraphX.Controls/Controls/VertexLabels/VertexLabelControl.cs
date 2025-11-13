using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DefaultEventArgs = System.EventArgs;
using System.Linq;
using Westermo.GraphX.Common.Exceptions;

namespace Westermo.GraphX.Controls;

public class VertexLabelControl : ContentControl, IVertexLabelControl
{
    internal Rect LastKnownRectSize;


    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(nameof(Angle),
        typeof(double),
        typeof(VertexLabelControl),
        new PropertyMetadata(0.0, AngleChanged));

    private static void AngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement ctrl)
            return;
        if (ctrl.RenderTransform is not TransformGroup tg ) ctrl.RenderTransform = new RotateTransform {Angle = (double) e.NewValue, CenterX = .5, CenterY = .5};
        else
        {
            var rt = tg.Children.FirstOrDefault(a => a is RotateTransform);
            if (rt == null)
                tg.Children.Add(new RotateTransform {Angle = (double) e.NewValue, CenterX = .5, CenterY = .5});
            else (rt as RotateTransform)!.Angle = (double) e.NewValue;
        }
    }

    /// <summary>
    /// Gets or sets label drawing angle in degrees
    /// </summary>
    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public static readonly DependencyProperty LabelPositionProperty = DependencyProperty.Register(nameof(LabelPosition),
        typeof(Point),
        typeof(VertexLabelControl),
        new PropertyMetadata(new Point()));
    /// <summary>
    /// Gets or sets label position if LabelPositionMode is set to Coordinates
    /// Position is always measured from top left VERTEX corner.
    /// </summary>
    public Point LabelPosition
    {
        get => (Point)GetValue(LabelPositionProperty);
        set => SetValue(LabelPositionProperty, value);
    }

    public static readonly DependencyProperty LabelPositionModeProperty = DependencyProperty.Register(nameof(LabelPositionMode),
        typeof(VertexLabelPositionMode),
        typeof(VertexLabelControl),
        new PropertyMetadata(VertexLabelPositionMode.Sides));
    /// <summary>
    /// Gets or set label positioning mode
    /// </summary>
    public VertexLabelPositionMode LabelPositionMode
    {
        get => (VertexLabelPositionMode)GetValue(LabelPositionModeProperty);
        set => SetValue(LabelPositionModeProperty, value);
    }


    public static readonly DependencyProperty LabelPositionSideProperty = DependencyProperty.Register(nameof(LabelPositionSide),
        typeof(VertexLabelPositionSide),
        typeof(VertexLabelControl),
        new PropertyMetadata(VertexLabelPositionSide.BottomRight));
    /// <summary>
    /// Gets or sets label position side if LabelPositionMode is set to Sides
    /// </summary>
    public VertexLabelPositionSide LabelPositionSide 
    {
        get => (VertexLabelPositionSide)GetValue(LabelPositionSideProperty);
        set => SetValue(LabelPositionSideProperty, value);
    }

    public VertexLabelControl()
    {
        if (DesignerProperties.GetIsInDesignMode(this)) return;

        LayoutUpdated += VertexLabelControl_LayoutUpdated;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
    }

    protected virtual VertexControl? GetVertexControl(DependencyObject? parent)
    {
        while (parent != null)
        {
            if (parent is VertexControl control) return control;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }


    public virtual void UpdatePosition()
    {
        if (double.IsNaN(DesiredSize.Width) || DesiredSize.Width == 0) return;

        var vc = GetVertexControl(GetParent());
        if (vc == null) return;

        if (LabelPositionMode == VertexLabelPositionMode.Sides)
        {
            var pt = LabelPositionSide switch
            {
                VertexLabelPositionSide.TopRight => new Point(vc.DesiredSize.Width, -DesiredSize.Height),
                VertexLabelPositionSide.BottomRight => new Point(vc.DesiredSize.Width, vc.DesiredSize.Height),
                VertexLabelPositionSide.TopLeft => new Point(-DesiredSize.Width, -DesiredSize.Height),
                VertexLabelPositionSide.BottomLeft => new Point(-DesiredSize.Width, vc.DesiredSize.Height),
                VertexLabelPositionSide.Top => new Point(vc.DesiredSize.Width * .5 - DesiredSize.Width * .5, -DesiredSize.Height),
                VertexLabelPositionSide.Bottom => new Point(vc.DesiredSize.Width * .5 - DesiredSize.Width * .5, vc.DesiredSize.Height),
                VertexLabelPositionSide.Left => new Point(-DesiredSize.Width, vc.DesiredSize.Height * .5f - DesiredSize.Height * .5),
                VertexLabelPositionSide.Right => new Point(vc.DesiredSize.Width, vc.DesiredSize.Height * .5f - DesiredSize.Height * .5),
                _ => throw new GX_InvalidDataException("UpdatePosition() -> Unknown vertex label side!"),
            };
            LastKnownRectSize = new Rect(pt, DesiredSize);
        } 
        else LastKnownRectSize = new Rect(LabelPosition, DesiredSize);

        Arrange(LastKnownRectSize);
    }

    public void Hide()
    {
        SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
    }

    public void Show()
    {
        SetCurrentValue(VisibilityProperty, Visibility.Visible);
    }

    private void VertexLabelControl_LayoutUpdated(object? sender, DefaultEventArgs e)
    {
        var vc = GetVertexControl(GetParent());
        if (vc == null || !vc.ShowLabel) return;
        UpdatePosition();
    }

    protected virtual DependencyObject GetParent()
    {
        return VisualParent;
    }
}

/// <summary>
/// Contains different position modes for vertices
/// </summary>
public enum VertexLabelPositionMode
{
    /// <summary>
    /// Vertex label is positioned on one of the sides
    /// </summary>
    Sides,
    /// <summary>
    /// Vertex label is positioned using custom coordinates
    /// </summary>
    Coordinates
}

public enum VertexLabelPositionSide
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Top, Right, Bottom, Left
}