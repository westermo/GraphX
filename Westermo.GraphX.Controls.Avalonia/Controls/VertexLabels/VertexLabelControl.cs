using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Westermo.GraphX.Common.Exceptions;
using DefaultEventArgs = System.EventArgs;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class VertexLabelControl : ContentControl, IVertexLabelControl
    {
        internal Rect LastKnownRectSize;
        static VertexLabelControl()
        {
            AngleProperty.Changed.AddClassHandler<Control>(AngleChanged);
        }

        private static void AngleChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d is not Control ctrl)
                return;
            if (ctrl.RenderTransform is not TransformGroup tg)
                ctrl.RenderTransform = new RotateTransform { Angle = e.NewValue is double val ? val : 0.0, CenterX = .5, CenterY = .5 };
            else
            {
                var rt = tg.Children.FirstOrDefault(a => a is RotateTransform);
                if (rt == null)
                    tg.Children.Add(new RotateTransform { Angle = e.NewValue is double val ? val : 0.0, CenterX = .5, CenterY = .5 });
                else (rt as RotateTransform)!.Angle = e.NewValue is double val ? val : 0.0;
            }
        }

        public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<VertexLabelControl, double>(nameof(Angle));
        /// <summary>
        /// Gets or sets label drawing angle in degrees
        /// </summary>
        public double Angle
        {
            get => (double)GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        public static readonly StyledProperty<Point> LabelPositionProperty = AvaloniaProperty.Register<VertexLabelControl, Point>(nameof(LabelPosition));
        /// <summary>
        /// Gets or sets label position if LabelPositionMode is set to Coordinates
        /// Position is always measured from top left VERTEX corner.
        /// </summary>
        public Point LabelPosition
        {
            get => GetValue(LabelPositionProperty);
            set => SetValue(LabelPositionProperty, value);
        }

        public static readonly StyledProperty<VertexLabelPositionMode> LabelPositionModeProperty = AvaloniaProperty.Register<VertexLabelControl, VertexLabelPositionMode>(nameof(LabelPositionMode), VertexLabelPositionMode.Sides);
        /// <summary>
        /// Gets or set label positioning mode
        /// </summary>
        public VertexLabelPositionMode LabelPositionMode
        {
            get => GetValue(LabelPositionModeProperty);
            set => SetValue(LabelPositionModeProperty, value);
        }


        public static readonly StyledProperty<VertexLabelPositionSide> LabelPositionSideProperty = AvaloniaProperty.Register<VertexLabelControl, VertexLabelPositionSide>(nameof(LabelPositionSide), VertexLabelPositionSide.BottomRight);
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
            if (Design.IsDesignMode) return;

            LayoutUpdated += VertexLabelControl_LayoutUpdated;
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
        }

        protected virtual VertexControl? GetVertexControl(Control? parent)
        {
            while (parent != null)
            {
                if (parent is VertexControl control) return control;
                parent = parent.GetVisualParent() as Control;
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
            SetCurrentValue(IsVisibleProperty, false);
        }

        public void Show()
        {
            SetCurrentValue(IsVisibleProperty, true);
        }

        private void VertexLabelControl_LayoutUpdated(object? sender, DefaultEventArgs e)
        {
            var vc = GetVertexControl(GetParent());
            if (vc == null || !vc.ShowLabel) return;
            UpdatePosition();
        }

        protected virtual Control? GetParent()
        {
            return this.GetVisualParent() as Control;
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
}
