using System;
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
                if (_rectangularSize.IsEmpty())
                    UpdateLayout();
                return _rectangularSize;
            }
            private set => _rectangularSize = value;
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

            while (parent != null)
            {
                if (parent is VertexControl control) return control;
                parent = parent.GetVisualParent() as Control;
            }

            return null;
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
            var vPos = VertexControl!.GetPosition();
            var position = new Point(vPos.X, vPos.Y);
            RectangularSize = new Rect(position, new Size(Width, Height));
        }
    }
}