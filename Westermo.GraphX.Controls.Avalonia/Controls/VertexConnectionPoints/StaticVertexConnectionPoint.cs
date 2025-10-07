using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Westermo.GraphX.Common.Enums;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class StaticVertexConnectionPoint : ContentControl, IVertexConnectionPoint
    {
        #region Common part

        public override string ToString()
        {
            return $"{RectangularSize}, {Shape}";
        }

        /// <summary>
        /// Connector identifier
        /// </summary>
        public int Id { get; set; }

        public static readonly StyledProperty<VertexShape> ShapeProperty =
            AvaloniaProperty.Register<StaticVertexConnectionPoint, VertexShape>(nameof(Shape), VertexShape.Circle);

        /// <summary>
        /// Gets or sets shape form for connection point (affects math calculations for edge end placement)
        /// </summary>
        public VertexShape Shape
        {
            get => GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }

        private Rect _rectangularSize;

        public Rect RectangularSize
        {
            get
            {
                if (!_rectangularSize.IsEmpty()) return _rectangularSize;
                UpdateLayout();
                CalculateRectangularSize();

                return _rectangularSize;
            }
            private set => _rectangularSize = value;
        }

        private void CalculateRectangularSize()
        {
            if (VertexControl is null) return;
            var vertexPosition = VertexControl.GetPosition();
            var myPosition = this.TranslatePoint(new Point(), VertexControl);
            if (myPosition is null) return;
            var position = new Point(myPosition.Value.X + vertexPosition.X, myPosition.Value.Y + vertexPosition.Y);
            _rectangularSize = new Rect(position,
                new Size(double.IsNaN(Width) ? Bounds.Width : Width, double.IsNaN(Height) ? Bounds.Height : Height));
        }

        public void Show()
        {
            SetCurrentValue(IsVisibleProperty, true);
        }

        public void Hide()
        {
            SetCurrentValue(IsVisibleProperty, false);
        }

        private static VertexControl? GetVertexControl(Control? parent)
        {
            return parent?.FindAncestorOfType<VertexControl>();
        }

        #endregion Common part

        private VertexControl? _vertexControl;
        protected VertexControl? VertexControl => _vertexControl ??= GetVertexControl(GetParent());

        public StaticVertexConnectionPoint()
        {
            RenderTransformOrigin = new RelativePoint(new Point(0.5, 0.5), RelativeUnit.Absolute);
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
            LayoutUpdated += OnLayoutUpdated;
        }

        public void Update()
        {
            UpdateLayout();
            CalculateRectangularSize();
        }

        public void Dispose()
        {
            _vertexControl = null;
        }

        public Control? GetParent()
        {
            return this.GetVisualParent() as Control;
        }

        protected virtual void OnLayoutUpdated(object? sender, EventArgs e)
        {
            CalculateRectangularSize();
        }
    }
}