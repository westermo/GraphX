using System;
using System.Windows;
using System.Windows.Media;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class ViewFinderDisplay : Control
    {
        static ViewFinderDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (ViewFinderDisplay), new FrameworkPropertyMetadata(typeof (ViewFinderDisplay)));
        }

        public ViewFinderDisplay()
        {
            HookOnInitialize();
        }

        #region Hooks

        protected virtual void HookOnInitialize()
        {
        }

        #endregion

        #region Background Property

        public static readonly StyledProperty<> BackgroundProperty =
            AvaloniaProperty.Register(nameof(Background), typeof (Brush), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF)), FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush) GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        #endregion

        #region ContentBounds Property

        private static readonly StyledPropertyKey ContentBoundsPropertyKey =
            AvaloniaProperty.RegisterReadOnly(nameof(ContentBounds), typeof (Rect), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly StyledProperty ContentBoundsProperty = ContentBoundsPropertyKey.StyledProperty;

        internal Rect ContentBounds
        {
            get => (Rect) GetValue(ContentBoundsProperty);
            set => SetValue(ContentBoundsPropertyKey, value);
        }

        #endregion

        #region ShadowBrush Property

        public static readonly StyledProperty ShadowBrushProperty =
            AvaloniaProperty.Register(nameof(ShadowBrush), typeof (Brush), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)), FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush ShadowBrush
        {
            get => (Brush) GetValue(ShadowBrushProperty);
            set => SetValue(ShadowBrushProperty, value);
        }

        #endregion

        #region ViewportBrush Property

        public static readonly StyledProperty ViewportBrushProperty =
            AvaloniaProperty.Register(nameof(ViewportBrush), typeof (Brush), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush ViewportBrush
        {
            get => (Brush) GetValue(ViewportBrushProperty);
            set => SetValue(ViewportBrushProperty, value);
        }

        #endregion

        #region ViewportPen Property

        public static readonly StyledProperty ViewportPenProperty =
            AvaloniaProperty.Register(nameof(ViewportPen), typeof (Pen), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata(new Pen(new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00)), 1d), FrameworkPropertyMetadataOptions.AffectsRender));

        public Pen ViewportPen
        {
            get => (Pen) GetValue(ViewportPenProperty);
            set => SetValue(ViewportPenProperty, value);
        }

        #endregion

        #region ViewportRect Property

        public static readonly StyledProperty ViewportRectProperty =
            AvaloniaProperty.Register(nameof(ViewportRect), typeof (Rect), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public Rect ViewportRect
        {
            get => (Rect) GetValue(ViewportRectProperty);
            set => SetValue(ViewportRectProperty, value);
        }

        #endregion

        #region VisualBrush Property

        private static readonly StyledPropertyKey VisualBrushPropertyKey =
            AvaloniaProperty.RegisterReadOnly(nameof(VisualBrush), typeof (VisualBrush), typeof (ViewFinderDisplay),
                new FrameworkPropertyMetadata((VisualBrush?) null));

        public static readonly StyledProperty VisualBrushProperty = VisualBrushPropertyKey.StyledProperty;

        internal VisualBrush? VisualBrush
        {
            get => (VisualBrush) GetValue(VisualBrushProperty);
            set => SetValue(VisualBrushPropertyKey, value);
        }

        #endregion

        #region AvailableSize Internal Property

        internal Size AvailableSize { get; private set; } = Size.Empty;

        #endregion

        #region Scale Internal Property

        internal double Scale { get; set; } = 1d;

        #endregion

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Note that we do not call the Arrange method on any children
            // because a ViewFinderDisplay has no children

            // the control's RenderSize should always match its DesiredSize
            return DesiredSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Note that we do not call the Measure method on any children
            // because a ViewFinderDisplay has no children.  It is merely used 
            // as a surface for the view finder's VisualBrush.

            // store the available size for use by the Zoombox control
            AvailableSize = availableSize;

            // Simulate size-to-content for the display panel by ensuring a width and height
            // based on the content bounds. Otherwise, the display panel may have no size, since it doesn't 
            // contain content.
            var width = DoubleHelper.IsNaN(ContentBounds.Width) ? 0 : Math.Max(0, ContentBounds.Width);
            var height = DoubleHelper.IsNaN(ContentBounds.Height) ? 0 : Math.Max(0, ContentBounds.Height);
            var displayPanelSize = new Size(width, height);

            // Now ensure that the result fits within the available size while maintaining
            // the width/height ratio of the content bounds
            if (displayPanelSize.Width > availableSize.Width || displayPanelSize.Height > availableSize.Height)
            {
                var aspectX = availableSize.Width/displayPanelSize.Width;
                var aspectY = availableSize.Height/displayPanelSize.Height;
                var scale = aspectX < aspectY ? aspectX : aspectY;
                displayPanelSize = new Size(Math.Max(0, displayPanelSize.Width * scale), Math.Max(0, displayPanelSize.Height * scale));
            }

            return displayPanelSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(Background, null, ContentBounds);

            dc.DrawRectangle(VisualBrush, null, ContentBounds);

            if (ViewportRect.IntersectsWith(new Rect(RenderSize)))
            {
                // draw shadow rectangles over the non-viewport regions
                var r1 = new Rect(new Point(0, 0), new Size(RenderSize.Width, Math.Max(0, ViewportRect.Top)));
                var r2 = new Rect(new Point(0, ViewportRect.Top), new Size(Math.Max(0, ViewportRect.Left), ViewportRect.Height));
                var r3 = new Rect(new Point(ViewportRect.Right, ViewportRect.Top), new Size(Math.Max(0, RenderSize.Width - ViewportRect.Right), ViewportRect.Height));
                var r4 = new Rect(new Point(0, ViewportRect.Bottom), new Size(RenderSize.Width, Math.Max(0, RenderSize.Height - ViewportRect.Bottom)));
                dc.DrawRectangle(ShadowBrush, null, r1);
                dc.DrawRectangle(ShadowBrush, null, r2);
                dc.DrawRectangle(ShadowBrush, null, r3);
                dc.DrawRectangle(ShadowBrush, null, r4);

                // draw the rectangle around the viewport region
                dc.DrawRectangle(ViewportBrush, ViewportPen, ViewportRect);
            }
            else
            {
                // if no part of the Rect is visible, just draw a 
                // shadow over the entire content bounds
                dc.DrawRectangle(ShadowBrush, null, new Rect(RenderSize));
            }
        }
    }
}
