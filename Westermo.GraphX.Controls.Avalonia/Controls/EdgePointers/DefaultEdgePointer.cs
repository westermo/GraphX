using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl.Helpers;

namespace Westermo.GraphX.Controls.Controls.EdgePointers;

/// <summary>
/// Edge pointer control for edge endpoints customization
/// Represents ContentControl that can host different content, e.g. Image or Path
/// </summary>
public class DefaultEdgePointer : ContentControl, IEdgePointer
{
    /// <summary>
    /// This static initializer is used to override PropertyMetadata of the Visibility property so that it
    /// can be coerced according to the IsSuppressed property value. Suppressing an edge pointer will make
    /// it invisible to the user without altering the underlying value of the Visibility property. Thus,
    /// visibility can be controlled independently of other factors that may require making the pointer
    /// invisible to the user. For example, the HideEdgePointerByEdgeLength feature of EdgeControlBase may
    /// need to ensure the pointer is removed from view, but when the constraint is removed, it shouldn't
    /// cause pointers to be shown that weren't shown before.
    /// </summary>
    static DefaultEdgePointer()
    {
        var oldPmd = IsVisibleProperty.GetMetadata(typeof(ContentControl));
        var newPmd = new StyledPropertyMetadata<bool>(oldPmd.DefaultValue, coerce: CoerceVisibility);
        IsVisibleProperty.OverrideMetadata<DefaultEdgePointer>(newPmd);
        IsSuppressedProperty.Changed.AddClassHandler<DefaultEdgePointer>(OnSuppressChanged);
    }


    #region Common part

    internal Rect LastKnownRectSize;


    public static readonly StyledProperty<Point> OffsetProperty =
        AvaloniaProperty.Register<EdgeControl, Point>(nameof(Offset));

    /// <summary>
    /// Gets or sets offset for the image position
    /// </summary>
    public Point Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public static readonly StyledProperty<bool> NeedRotationProperty =
        AvaloniaProperty.Register<EdgeControl, bool>(nameof(NeedRotation), true);

    /// <inheritdoc />
    public bool NeedRotation
    {
        get => GetValue(NeedRotationProperty);
        set => SetValue(NeedRotationProperty, value);
    }

    /// <inheritdoc />
    public Measure.Point GetPosition()
    {
        return LastKnownRectSize.IsEmpty() ? new Point().ToGraphX() : LastKnownRectSize.Center().ToGraphX();
    }

    public void Show()
    {
        SetCurrentValue(IsVisibleProperty, true);
    }

    public void Hide()
    {
        SetCurrentValue(IsVisibleProperty, false);
    }

    /// <summary>
    /// Suppresses the pointer from view, overriding the Visibility value until unsuppressed.
    /// </summary>
    public void Suppress()
    {
        IsSuppressed = true;
    }

    /// <summary>
    /// Removes the suppression constraint, returning to the base value of the Visibility property.
    /// </summary>
    public void UnSuppress()
    {
        IsSuppressed = false;
    }


    public static readonly StyledProperty<bool> IsSuppressedProperty =
        AvaloniaProperty.Register<DefaultEdgePointer, bool>(nameof(IsSuppressed));

    /// <summary>
    /// Gets a value indicating whether the pointer is suppressed. A suppressed pointer won't be displayed, but
    /// suppressing does not alter the underlying Visibility property value.
    /// </summary>
    public bool IsSuppressed
    {
        get => GetValue(IsSuppressedProperty);
        private set => SetValue(IsSuppressedProperty, value);
    }

    /// <summary>
    /// When the IsSuppressed value changes, this callback triggers coercion of the Visibility property.
    /// </summary>
    private static void OnSuppressChanged(DefaultEdgePointer pointer, AvaloniaPropertyChangedEventArgs args)
    {
        pointer?.CoerceValue(IsVisibleProperty);
    }

    /// <summary>
    /// This coercion callback is used to alter the effective value of Visibility when pointer suppression is in effect.
    /// When the suppression constraint is removed, the base value of Visibility becomes effective again.
    /// </summary>
    private static bool CoerceVisibility(AvaloniaObject @object, bool baseValue)
    {
        return @object is not DefaultEdgePointer ecb ? baseValue : ecb.IsSuppressed;
    }

    private static EdgeControl? GetEdgeControl(Control? parent)
    {
        while (parent != null)
        {
            if (parent is EdgeControl control) return control;
            parent = parent.GetVisualParent() as Control;
        }

        return null;
    }

    #endregion


    private EdgeControl? _edgeControl;
    protected EdgeControl? EdgeControl => _edgeControl ??= GetEdgeControl(GetParent());

    public DefaultEdgePointer()
    {
        RenderTransformOrigin = new RelativePoint(.5, .5, RelativeUnit.Relative);
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
        LayoutUpdated += EdgePointer_LayoutUpdated;
    }

    /// <summary>
    /// Update edge pointer position and angle
    /// </summary>
    public virtual Measure.Point Update(Measure.Point? position, Measure.Vector direction, double angle = 0d)
    {
        if (DesiredSize.Width == 0 || DesiredSize.Height == 0 || !position.HasValue) return new Measure.Point();
        position = new Measure.Point(position.Value.X - direction.X,
            position.Value.Y - direction.Y);
        if (!double.IsNaN(DesiredSize.Width) && DesiredSize.Width != 0 && !double.IsNaN(position.Value.X))
        {
            if (!double.IsNaN(DesiredSize.Width) && DesiredSize.Width != 0 && !double.IsNaN(position.Value.X))
            {
                LastKnownRectSize =
                    new Rect(
                        new Point(position.Value.X - DesiredSize.Width * .5,
                            position.Value.Y - DesiredSize.Height * .5), DesiredSize);
                Arrange(LastKnownRectSize);
            }
        }

        RenderTransform = new RotateTransform { Angle = double.IsNaN(angle) ? 0 : angle, CenterX = 0, CenterY = 0 };
        var width = double.IsNaN(Width) ? Bounds.Width : Width;
        var height = double.IsNaN(Height) ? Bounds.Height : Height;
        return new Measure.Point(direction.X * width, direction.Y * height);
    }

    public void SetManualPosition(Measure.Point position)
    {
        LastKnownRectSize =
            new Rect(new Point(position.X - DesiredSize.Width * .5, position.Y - DesiredSize.Height * .5),
                DesiredSize);
        Arrange(LastKnownRectSize);
    }

    public void Dispose()
    {
        _edgeControl = null;
    }

    private void EdgePointer_LayoutUpdated(object? sender, EventArgs e)
    {
        if (LastKnownRectSize.Width != 0 && EdgeControl != null)
            Arrange(LastKnownRectSize);
    }

    private Control? GetParent()
    {
        return this.GetVisualParent() as Control;
    }
}