using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl.Helpers;

namespace Westermo.GraphX.Controls.Controls.EdgeLabels;

public abstract class EdgeLabelControl : ContentControl, IEdgeLabelControl
{
    private EdgeControl? _edgeControl;

    internal Rect LastKnownRectSize;

    static EdgeLabelControl()
    {
        ShowLabelProperty.Changed.AddClassHandler<EdgeLabelControl>(ShowLabelChanged);
        AngleProperty.Changed.AddClassHandler<EdgeLabelControl>(AngleChanged);
    }

    public EdgeLabelControl()
    {
        RenderTransformOrigin = new RelativePoint(.5, .5, RelativeUnit.Relative);
        LayoutUpdated += EdgeLabelControl_LayoutUpdated;
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
        SizeChanged += EdgeLabelControl_SizeChanged;
        Loaded += EdgeLabelControl_Loaded;
        UpdateLabelOnSizeChange = true;
        UpdateLabelOnVisibilityChange = true;
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
        elc.EdgeControl?.UpdateEdge();
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


    private void EdgeLabelControl_SizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        if (!UpdateLabelOnSizeChange) return;
        UpdatePosition();
    }

    private Control? GetParent()
    {
        return this.GetVisualParent() as Control;
    }

    public void Dispose()
    {
        _edgeControl = null;
    }

    private static double GetLabelDistance(double edgeLength)
    {
        return edgeLength * .5; // set the label halfway the length of the edge
    }

    /// <summary>
    /// Automaticaly update edge label position
    /// </summary>
    public virtual void UpdatePosition()
    {
        if (double.IsNaN(DesiredSize.Width) || DesiredSize.Width == 0) return;

        if (EdgeControl == null)
            return;
        if (EdgeControl.Source == null || EdgeControl.Target == null)
        {
            Debug.WriteLine("EdgeLabelControl_LayoutUpdated() -> Got empty edgecontrol!");
            return;
        }

        //if hidden
        if (!IsVisible) return;

        if (EdgeControl.IsSelfLooped)
        {
            var idesiredSize = DesiredSize;
            var pt = EdgeControl.Source.GetCenterPosition();
            SetSelfLoopedSize(pt.ToGraphX(), idesiredSize);
            Arrange(LastKnownRectSize);
            return;
        }

        var p1 = EdgeControl.SourceConnectionPoint.GetValueOrDefault().ToGraphX();
        var p2 = EdgeControl.TargetConnectionPoint.GetValueOrDefault().ToGraphX();

        double edgeLength = 0;
        if (EdgeControl.Edge is IRoutingInfo routingInfo)
        {
            var routePoints = routingInfo.RoutingPoints;

            if (routePoints == null || routePoints.Length == 0)
            {
                // the edge is a single segment (p1,p2)
                edgeLength = GetLabelDistance(MathHelper.GetDistanceBetweenPoints(p1, p2));
            }
            else
            {
                // the edge has one or more segments
                // compute the total length of all the segments
                edgeLength = 0;
                var rplen = routePoints.Length;
                for (var i = 0; i <= rplen; ++i)
                    if (i == 0)
                        edgeLength += MathHelper.GetDistanceBetweenPoints(p1, routePoints[0]);
                    else if (i == rplen)
                        edgeLength += MathHelper.GetDistanceBetweenPoints(routePoints[rplen - 1], p2);
                    else
                        edgeLength += MathHelper.GetDistanceBetweenPoints(routePoints[i - 1], routePoints[i]);
                // find the line segment where the half distance is located
                edgeLength = GetLabelDistance(edgeLength);
                var newp1 = p1;
                var newp2 = p2;
                for (var i = 0; i <= rplen; ++i)
                {
                    double lengthOfSegment;
                    if (i == 0)
                        lengthOfSegment = MathHelper.GetDistanceBetweenPoints(newp1 = p1, newp2 = routePoints[0]);
                    else if (i == rplen)
                        lengthOfSegment =
                            MathHelper.GetDistanceBetweenPoints(newp1 = routePoints[rplen - 1], newp2 = p2);
                    else
                        lengthOfSegment =
                            MathHelper.GetDistanceBetweenPoints(newp1 = routePoints[i - 1], newp2 = routePoints[i]);
                    if (lengthOfSegment >= edgeLength)
                        break;
                    edgeLength -= lengthOfSegment;
                }

                // redefine our edge points
                p1 = newp1;
                p2 = newp2;
            }
        }

        // The label control should be laid out on a rectangle, in the middle of the edge
        var angleBetweenPoints = MathHelper.GetAngleBetweenPoints(p1, p2);
        var desiredSize = DesiredSize;
        var flipAxis = p1.X > p2.X; // Flip axis if source is "after" target

        edgeLength = ApplyLabelHorizontalOffset(edgeLength, LabelHorizontalOffset);

        // Calculate the center point of the edge
        var centerPoint = new Measure.Point(p1.X + edgeLength * Math.Cos(angleBetweenPoints),
            p1.Y - edgeLength * Math.Sin(angleBetweenPoints));
        if (AlignToEdge)
        {
            // If we're aligning labels to the edges make sure add the label vertical offset
            var yEdgeOffset = LabelVerticalOffset;
            if (FlipOnRotation && flipAxis &&
                !EdgeControl.IsParallel) // If we've flipped axis, move the offset to the other side of the edge
                yEdgeOffset = -yEdgeOffset;

            // Adjust offset for rotation. Remember, the offset is perpendicular from the edge tangent.
            // Slap on 90 degrees to the angle between the points, to get the direction of the offset.
            centerPoint.Y -= yEdgeOffset * Math.Sin(angleBetweenPoints + Math.PI / 2);
            centerPoint.X += yEdgeOffset * Math.Cos(angleBetweenPoints + Math.PI / 2);
            // Angle is in degrees
            Angle = -angleBetweenPoints * 180 / Math.PI;
            if (flipAxis)
                Angle += 180; // Reorient the label so that it's always "pointing north"
        }

        UpdateFinalPosition(centerPoint, desiredSize);
        LastKnownRectSize = LastKnownRectSize.IsEmpty()
            ? new Rect(
                double.IsNaN(LastKnownRectSize.X) ? 0 : LastKnownRectSize.X,
                double.IsNaN(LastKnownRectSize.Y) ? 0 : LastKnownRectSize.Y,
                double.IsNaN(LastKnownRectSize.Width) || LastKnownRectSize.Width == 0
                    ? desiredSize.Width
                    : LastKnownRectSize.Width,
                double.IsNaN(LastKnownRectSize.Height) || LastKnownRectSize.Height == 0
                    ? desiredSize.Height
                    : LastKnownRectSize.Height
            )
            : LastKnownRectSize;
        Arrange(LastKnownRectSize);
    }

    protected virtual double ApplyLabelHorizontalOffset(double edgeLength, double offset)
    {
        if (offset == 0) return edgeLength;
        edgeLength += edgeLength / 100 * offset;
        return edgeLength;
    }

    /// <summary>
    /// Gets or sets if label should update its position and size data on visual size change. Helps to update label correctly on template manipulations. Can be turned off for better performance.
    /// </summary>
    public bool UpdateLabelOnSizeChange { get; set; }

    /// <summary>
    /// Gets or sets if label should additionaly update its position and size data on label visibility change. Can be turned off for better performance.
    /// </summary>
    public bool UpdateLabelOnVisibilityChange { get; set; }

    private void SetSelfLoopedSize(Measure.Point pt, Size idesiredSize)
    {
        pt.Offset(-idesiredSize.Width / 2,
            EdgeControl!.Source!.DesiredSize.Height * .5 + 2 + idesiredSize.Height * .5);
        LastKnownRectSize = new Rect(pt.X, pt.Y, idesiredSize.Width, idesiredSize.Height);
    }

    private void UpdateFinalPosition(Measure.Point centerPoint, Size desiredSize)
    {
        LastKnownRectSize = new Rect(centerPoint.X - desiredSize.Width / 2,
            centerPoint.Y - desiredSize.Height / 2, desiredSize.Width, desiredSize.Height);
    }

    /// <summary>
    /// Get label rectangular size
    /// </summary>
    public Rect GetSize()
    {
        return LastKnownRectSize;
    }

    /// <summary>
    /// Set label rectangular size
    /// </summary>
    public void SetSize(Rect size)
    {
        LastKnownRectSize = size;
    }

    private void EdgeLabelControl_Loaded(object? sender, RoutedEventArgs e)
    {
        if (EdgeControl is { IsSelfLooped: true } && !DisplayForSelfLoopedEdges) Hide();
        else Show();
    }

    private void EdgeLabelControl_LayoutUpdated(object? sender, EventArgs e)
    {
        if (EdgeControl == null || !ShowLabel) return;
        if (LastKnownRectSize.IsEmpty() || double.IsNaN(LastKnownRectSize.Width) ||
            LastKnownRectSize.Width == 0)
        {
            UpdatePosition();
        }
        else Arrange(LastKnownRectSize);
    }
}