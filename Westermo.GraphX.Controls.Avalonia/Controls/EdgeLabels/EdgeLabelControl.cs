using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Controls.EdgeLabels;

public abstract class EdgeLabelControl : ContentControl, IEdgeLabelControl
{
    private EdgeControl? _edgeControl;

    static EdgeLabelControl()
    {
        ShowLabelProperty.Changed.AddClassHandler<EdgeLabelControl>(ShowLabelChanged);
        AngleProperty.Changed.AddClassHandler<EdgeLabelControl>(AngleChanged);
        HorizontalAlignmentProperty.OverrideDefaultValue<EdgeLabelControl>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<EdgeLabelControl>(VerticalAlignment.Top);
        RenderTransformOriginProperty.OverrideDefaultValue<EdgeLabelControl>(new RelativePoint(0.5, 0.5,
            RelativeUnit.Relative));
    }

    protected EdgeControl? EdgeControl => _edgeControl ??= GetEdgeControl(GetParent());


    private static void AngleChanged(EdgeLabelControl d, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not double angle) return;
        if (d.RenderTransform is TransformGroup tg)
        {
            if (tg.Children.OfType<RotateTransform>().FirstOrDefault() is { } rt)
            {
                rt.Angle = angle;
                return;
            }

            tg.Children.Add(new RotateTransform { Angle = angle, CenterX = .5, CenterY = .5 });
            return;
        }

        d.RenderTransform = new RotateTransform { Angle = angle, CenterX = .5, CenterY = .5 };
    }

    private static void ShowLabelChanged(EdgeLabelControl elc, AvaloniaPropertyChangedEventArgs e)
    {
        // Ensure we only process valid boolean values for the ShowLabel property.
        if (e.NewValue is not bool showLabel)
            return;

        // Delegate visibility changes through Show()/Hide() so any custom logic
        // (e.g. self-loop edge visibility rules) is consistently applied.
        if (showLabel)
            elc.Show();
        else
            elc.Hide();

        // Invalidate the associated edge layout so the label is repositioned
        // immediately when its visibility changes.
        var edgeControl = elc.EdgeControl;
        edgeControl?.InvalidateMeasure();
        edgeControl?.InvalidateArrange();
    }

    public static readonly StyledProperty<bool> AlignToEdgeProperty =
        AvaloniaProperty.Register<EdgeLabelControl, bool>(nameof(AlignToEdge));

    /// <summary>
    /// Gets or sets if lables should be aligned to edges and be displayed under the same angle
    /// </summary>
    public bool AlignToEdge
    {
        get => GetValue(AlignToEdgeProperty);
        set => SetValue(AlignToEdgeProperty, value);
    }

    public static readonly StyledProperty<double> LabelVerticalOffsetProperty =
        AvaloniaProperty.Register<EdgeLabelControl, double>(nameof(LabelVerticalOffset));

    /// <summary>
    /// Offset for label Y axis to display it above/below the edge
    /// </summary>
    public double LabelVerticalOffset
    {
        get => GetValue(LabelVerticalOffsetProperty);
        set => SetValue(LabelVerticalOffsetProperty, value);
    }

    public static readonly StyledProperty<double> LabelHorizontalOffsetProperty =
        AvaloniaProperty.Register<EdgeLabelControl, double>(nameof(LabelHorizontalOffset));


    /// <summary>
    /// Offset for label X axis to display it along the edge
    /// </summary>
    public double LabelHorizontalOffset
    {
        get => GetValue(LabelHorizontalOffsetProperty);
        set => SetValue(LabelHorizontalOffsetProperty, value);
    }

    public static readonly StyledProperty<bool> ShowLabelProperty =
        AvaloniaProperty.Register<EdgeLabelControl, bool>(nameof(ShowLabel));

    /// <summary>
    /// Show edge label. Default value is False.
    /// </summary>
    public bool ShowLabel
    {
        get => GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    public static readonly StyledProperty<bool> DisplayForSelfLoopedEdgesProperty =
        AvaloniaProperty.Register<EdgeLabelControl, bool>(nameof(DisplayForSelfLoopedEdges));


    /// <summary>
    /// Gets or sets if label should be visible for self looped edge. Default value is false.
    /// </summary>
    public bool DisplayForSelfLoopedEdges
    {
        get => GetValue(DisplayForSelfLoopedEdgesProperty);
        set => SetValue(DisplayForSelfLoopedEdgesProperty, value);
    }

    public static readonly StyledProperty<bool> FlipOnRotationProperty =
        AvaloniaProperty.Register<EdgeLabelControl, bool>(nameof(FlipOnRotation), true);

    /// <summary>
    /// Gets or sets if label should flip on rotation when axis changes. Default value is true.
    /// </summary>
    public bool FlipOnRotation
    {
        get => GetValue(FlipOnRotationProperty);
        set => SetValue(FlipOnRotationProperty, value);
    }

    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<EdgeLabelControl, double>(nameof(Angle));


    /// <summary>
    /// Gets or sets label drawing angle in degrees
    /// </summary>
    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }


    protected virtual EdgeControl? GetEdgeControl(Control? parent)
    {
        while (parent != null)
        {
            if (parent is EdgeControl control) return control;
            parent = parent.Parent as Control;
        }

        return null;
    }

    public void Show()
    {
        if (EdgeControl is { IsSelfLooped: true } && !DisplayForSelfLoopedEdges) return;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }


    private Control? GetParent()
    {
        return this.GetVisualParent() as Control;
    }

    public void Dispose()
    {
        _edgeControl = null;
    }

    /// <summary>
    /// Gets or sets if label should update its position and size data on visual size change. Helps to update label correctly on template manipulations. Can be turned off for better performance.
    /// </summary>
    public bool UpdateLabelOnSizeChange { get; set; } = true;

    /// <summary>
    /// Gets or sets if label should additionaly update its position and size data on label visibility change. Can be turned off for better performance.
    /// </summary>
    public bool UpdateLabelOnVisibilityChange { get; set; } = true;
}