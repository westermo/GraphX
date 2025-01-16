using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace Westermo.GraphX.Controls.Avalonia
{
    [TemplatePart(Name = PART_PRESENTER, Type = typeof(ZoomContentPresenter))]
    public class ZoomControl : ContentControl, IZoomControl, INotifyPropertyChanged
    {
        private const string PART_PRESENTER = "PART_Presenter";

        #region Viewfinder (minimap)

        #region Properties & Commands

        #region Center Command

        public static RoutedEvent Center =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("Center", RoutingStrategies.Bubble);

        private void CenterContent(object sender, RoutedEventArgs e)
        {
            CenterContent();
        }

        #endregion

        #region Fill Command

        public static RoutedEvent Fill =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("FillToBounds", RoutingStrategies.Bubble);

        private void FillToBounds(object sender, RoutedEventArgs e)
        {
            ZoomToFill();
        }

        #endregion

        #region ResetZoom Command

        public static RoutedEvent ResetZoom =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("ResetZoom", RoutingStrategies.Bubble);

        /// <summary>
        /// Executes when ResetZoom command is fired and resets the Zoom value to default one. Override to reset to custom zoom value.
        /// Default Zoom value is 1.
        /// </summary>
        protected virtual void ExecuteResetZoom(object sender, RoutedEventArgs e)
        {
            Zoom = 1d;
        }

        #endregion

        #region Refocus Command

        public static RoutedEvent Refocus =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("Refocus", RoutingStrategies.Bubble);

        private void CanRefocusView(object sender, RoutedEventArgs e)
        {
        }

        private void RefocusView(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region ResizeEdge Nested Type

        private enum ResizeEdge
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Left,
            Top,
            Right,
            Bottom,
        }

        #endregion

        #region CacheBits Nested Type

        private enum CacheBits
        {
            IsUpdatingView = 0x00000001,
            IsUpdatingViewport = 0x00000002,
            IsDraggingViewport = 0x00000004,
            IsResizingViewport = 0x00000008,
            IsMonitoringInput = 0x00000010,
            IsContentWrapped = 0x00000020,
            HasArrangedContentPresenter = 0x00000040,
            HasRenderedFirstView = 0x00000080,
            RefocusViewOnFirstRender = 0x00000100,
            HasUiPermission = 0x00000200,
        }

        #endregion

        #region Viewport Property

        public static readonly StyledProperty<Rect> ViewportProperty =
            AvaloniaProperty.Register<ZoomControl, Rect>(nameof(Viewport));

        public Rect Viewport => GetValue(ViewportProperty);

        private static void OnViewportChanged(Control o, AvaloniaPropertyChangedEventArgs e)
        {
            // keep the Position property in sync with the Viewport
            //var zoombox = (ZoomControl)o;
            //var pt = new Point(-zoombox.Viewport.Left * zoombox.Zoom / zoombox._viewboxFactor, -zoombox.Viewport.Top * zoombox.Zoom / zoombox._viewboxFactor);
            //zoombox.TranslateX = pt.X;
            //zoombox.TranslateY = pt.Y;
        }

        #endregion

        #region ViewFinder Property

        public static readonly StyledProperty<Control?> ViewFinderProperty =
            AvaloniaProperty.Register<ZoomControl, Control?>(nameof(ViewFinder));

        public Control? ViewFinder => GetValue(ViewFinderProperty);

        #endregion

        #region ViewFinderVisibility Attached Property

        public static readonly StyledProperty<bool> ViewFinderVisibleProperty =
            AvaloniaProperty.RegisterAttached<ZoomControl, Control, bool>("ViewFinderVisible", true);

        public static bool GetViewFinderVisibility(Control d)
        {
            return d.GetValue(ViewFinderVisibleProperty);
        }

        public static void SetViewFinderVisibility(Control d, bool value)
        {
            d.SetValue(ViewFinderVisibleProperty, value);
        }

        #endregion

        // the view finder display panel
        // this is used to show the current viewport
        private ViewFinderDisplay? _viewFinderDisplay;
        private double _viewboxFactor = 1.0;

        #endregion

        #region Attach / Detach

        private void AttachToVisualTree(TemplateAppliedEventArgs e)
        {
            // detach from the old tree
            DetachFromVisualTree();
            // set a reference to the ViewFinder element, if present
            SetValue(ViewFinderProperty, e.NameScope.Find<Control>("ViewFinder"));

            // locate the view finder display panel
            _viewFinderDisplay =
                VisualTreeHelperEx.FindDescendantByType(this, typeof(ViewFinderDisplay), false) as ViewFinderDisplay;

            // if a ViewFinder was specified but no display panel is present, throw an exception
            if (ViewFinder != null && _viewFinderDisplay == null)
                throw new Exception("ZoomControlHasViewFinderButNotDisplay");

            // set up the VisualBrush and adorner for the display panel
            if (_viewFinderDisplay != null && Content is not null)
            {
                // create VisualBrush for the view finder display panel
                CreateVisualBrushForViewFinder((Visual)Content);
                //rem due to fail in template bg binding //_viewFinderDisplay.Background = this.Background;

                // hook up event handlers for dragging and resizing the viewport
                _viewFinderDisplay.PointerMoved -= ViewFinderDisplayMouseMove;
                _viewFinderDisplay.PointerPressed -= ViewFinderDisplayBeginCapture;
                _viewFinderDisplay.PointerReleased -= ViewFinderDisplayEndCapture;

                _viewFinderDisplay.PointerMoved += ViewFinderDisplayMouseMove;
                _viewFinderDisplay.PointerPressed += ViewFinderDisplayBeginCapture;
                _viewFinderDisplay.PointerReleased += ViewFinderDisplayEndCapture;
            }
        }

        private static void _viewFinderDisplay_IsVisibleChanged(Control control, AvaloniaPropertyChangedEventArgs e)
        {
            if (control is ZoomControl zoomControl)
                zoomControl.ViewFinderVisibilityChanged(e);
        }

        private void ViewFinderVisibilityChanged(AvaloniaPropertyChangedEventArgs e)
        {
            //needed to overcome the case when viewbox was hidden by default so no size were calculated
            if (_viewFinderDisplay!.IsVisible && (bool?)e.NewValue == true)
            {
                _viewFinderDisplay.InvalidateMeasure();
                _viewFinderDisplay.UpdateLayout();
                UpdateViewFinderDisplayContentBounds();
            }
        }

        private void CreateVisualBrushForViewFinder(Visual visual)
        {
            _viewFinderDisplay!.VisualBrush = new VisualBrush(visual)
                { Stretch = Stretch.Uniform, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
        }

        private void DetachFromVisualTree()
        {
            // remove the view finder display panel's visual brush and adorner
            _viewFinderDisplay = null;
        }

        #endregion

        #region Mouse Events

        #region IsResizingViewport Private Property

        private bool IsResizingViewport
        {
            get => _cacheBits[(int)CacheBits.IsResizingViewport];
            set => _cacheBits[(int)CacheBits.IsResizingViewport] = value;
        }

        #endregion

        #region IsDraggingViewport Private Property

        private bool IsDraggingViewport
        {
            get => _cacheBits[(int)CacheBits.IsDraggingViewport];
            set => _cacheBits[(int)CacheBits.IsDraggingViewport] = value;
        }

        #endregion

        // state variables used during drag and select operations
        private Rect _resizeViewportBounds = default;
        private Point _resizeAnchorPoint = new(0, 0);
        private Point _resizeDraggingPoint = new(0, 0);
        private Point _originPoint = new(0, 0);
        private BitVector32 _cacheBits = new(0);

        private void ViewFinderDisplayBeginCapture(object? sender, PointerPressedEventArgs e)
        {
            const double arbitraryLargeValue = 10000000000;

            // if we need to acquire capture, the Tag property of the view finder display panel
            // will be a ResizeEdge value.
            if (_viewFinderDisplay!.Tag is not ResizeEdge) return;
            // if the Tag is ResizeEdge.None, then its a drag operation; otherwise, its a resize
            if ((ResizeEdge)_viewFinderDisplay.Tag == ResizeEdge.None)
            {
                IsDraggingViewport = true;
            }
            else
            {
                IsResizingViewport = true;
                var direction = new Vector();
                switch ((ResizeEdge)_viewFinderDisplay.Tag)
                {
                    case ResizeEdge.TopLeft:
                        _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.TopLeft;
                        _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.BottomRight;
                        direction = new Vector(-1, -1);
                        break;

                    case ResizeEdge.TopRight:
                        _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.TopRight;
                        _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.BottomLeft;
                        direction = new Vector(1, -1);
                        break;

                    case ResizeEdge.BottomLeft:
                        _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.BottomLeft;
                        _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.TopRight;
                        direction = new Vector(-1, 1);
                        break;

                    case ResizeEdge.BottomRight:
                        _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.BottomRight;
                        _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.TopLeft;
                        direction = new Vector(1, 1);
                        break;
                    case ResizeEdge.Left:
                        _resizeDraggingPoint = new Point(_viewFinderDisplay.ViewportRect.Left,
                            _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                        _resizeAnchorPoint = new Point(_viewFinderDisplay.ViewportRect.Right,
                            _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                        direction = new Vector(-1, 0);
                        break;
                    case ResizeEdge.Top:
                        _resizeDraggingPoint = new Point(
                            _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                            _viewFinderDisplay.ViewportRect.Top);
                        _resizeAnchorPoint = new Point(
                            _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                            _viewFinderDisplay.ViewportRect.Bottom);
                        direction = new Vector(0, -1);
                        break;
                    case ResizeEdge.Right:
                        _resizeDraggingPoint = new Point(_viewFinderDisplay.ViewportRect.Right,
                            _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                        _resizeAnchorPoint = new Point(_viewFinderDisplay.ViewportRect.Left,
                            _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                        direction = new Vector(1, 0);
                        break;
                    case ResizeEdge.Bottom:
                        _resizeDraggingPoint = new Point(
                            _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                            _viewFinderDisplay.ViewportRect.Bottom);
                        _resizeAnchorPoint = new Point(
                            _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                            _viewFinderDisplay.ViewportRect.Top);
                        direction = new Vector(0, 1);
                        break;
                }

                var contentRect = _viewFinderDisplay.ContentBounds;
                var minVector = new Vector(direction.X * arbitraryLargeValue, direction.Y * arbitraryLargeValue);
                var maxVector = new Vector(direction.X * contentRect.Width / MaxZoom,
                    direction.Y * contentRect.Height / MaxZoom);
                _resizeViewportBounds = new Rect(_resizeAnchorPoint + minVector, _resizeAnchorPoint + maxVector);
            }

            // store the origin of the operation and acquire capture
            _originPoint = e.GetPosition(_viewFinderDisplay);
            e.Pointer.Capture(_viewFinderDisplay);
            e.Handled = true;
        }

        private void ViewFinderDisplayEndCapture(object? sender, PointerReleasedEventArgs e)
        {
            // if a drag or resize is in progress, end it and release capture
            if (IsDraggingViewport || IsResizingViewport)
            {
                // call the DragDisplayViewport method to end the operation
                // and store the current position on the stack
                DragDisplayViewport(new Vector(0, 0));

                // reset the dragging state variables and release capture
                IsDraggingViewport = false;
                IsResizingViewport = false;
                _originPoint = new Point();
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        private void ViewFinderDisplayMouseMove(object? sender, PointerEventArgs e)
        {
            // if a drag operation is in progress, update the operation
            if (IsDraggingViewport || IsResizingViewport)
            {
                var pos = e.GetPosition(_viewFinderDisplay);
                var delta = pos - _originPoint;
                if (IsDraggingViewport)
                {
                    DragDisplayViewport(new Vector(delta.X, delta.Y));
                }
                else
                {
                    ResizeDisplayViewport(new Vector(delta.X, delta.Y),
                        (ResizeEdge)_viewFinderDisplay!.Tag!);
                }

                e.Handled = true;
            }
            else
            {
                // update the cursor based on the nearest corner
                var mousePos = e.GetPosition(_viewFinderDisplay);
                var viewportRect = _viewFinderDisplay!.ViewportRect;
                var cornerDelta = viewportRect.Width * viewportRect.Height > 100
                    ? 5.0
                    : Math.Sqrt(viewportRect.Width * viewportRect.Height) / 2;

                // if the mouse is within the Rect and the Rect does not encompass the entire content, set the appropriate cursor
                if (viewportRect.Contains(mousePos) && !DoubleHelper.AreVirtuallyEqual(
                        viewportRect.Intersect(_viewFinderDisplay.ContentBounds), _viewFinderDisplay.ContentBounds))
                {
                    if (PointHelper.DistanceBetween(mousePos, viewportRect.TopLeft) < cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.TopLeft;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeAll);
                    }
                    else if (PointHelper.DistanceBetween(mousePos, viewportRect.BottomRight) < cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.BottomRight;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeAll);
                    }
                    else if (PointHelper.DistanceBetween(mousePos, viewportRect.TopRight) < cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.TopRight;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeAll);
                    }
                    else if (PointHelper.DistanceBetween(mousePos, viewportRect.BottomLeft) < cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.BottomLeft;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeAll);
                    }
                    else if (mousePos.X <= viewportRect.Left + cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.Left;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    }
                    else if (mousePos.Y <= viewportRect.Top + cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.Top;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    }
                    else if (mousePos.X >= viewportRect.Right - cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.Right;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    }
                    else if (mousePos.Y >= viewportRect.Bottom - cornerDelta)
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.Bottom;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    }
                    else
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.None;
                        _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.SizeAll);
                    }
                }
                else
                {
                    _viewFinderDisplay.Tag = null;
                    _viewFinderDisplay.Cursor = new Cursor(StandardCursorType.Arrow);
                }
            }
        }

        #endregion

        #region Drag and resize

        private void DragDisplayViewport(Vector delta)
        {
            // UpdateViewFinderDisplayContentBounds();
            // get the scale of the view finder display panel, the selection rect, and the VisualBrush rect

            var scale = _viewFinderDisplay!.Scale;
            var viewportRect = _viewFinderDisplay.ViewportRect;
            var vbRect = _viewFinderDisplay.ContentBounds;

            // if the entire content is visible, do nothing
            if (viewportRect.Contains(vbRect))
                return;

            // ensure that we stay within the bounds of the VisualBrush
            var dx = delta.X;
            var dy = delta.Y;

            // check left boundary
            if (viewportRect.Left < vbRect.Left)
                dx = Math.Max(0, dx);
            else if (viewportRect.Left + dx < vbRect.Left)
                dx = vbRect.Left - viewportRect.Left;

            // check right boundary
            if (viewportRect.Right > vbRect.Right)
                dx = Math.Min(0, dx);
            else if (viewportRect.Right + dx > vbRect.Left + vbRect.Width)
                dx = vbRect.Left + vbRect.Width - viewportRect.Right;

            // check top boundary
            if (viewportRect.Top < vbRect.Top)
                dy = Math.Max(0, dy);
            else if (viewportRect.Top + dy < vbRect.Top)
                dy = vbRect.Top - viewportRect.Top;

            // check bottom boundary
            if (viewportRect.Bottom > vbRect.Bottom)
                dy = Math.Min(0, dy);
            else if (viewportRect.Bottom + dy > vbRect.Top + vbRect.Height)
                dy = vbRect.Top + vbRect.Height - viewportRect.Bottom;

            // call the main OnDrag handler that is used when dragging the content directly
            OnDrag(new Vector(-dx / scale / _viewboxFactor, -dy / scale / _viewboxFactor));

            // for a drag operation, update the origin with each delta
            _originPoint += new Vector(dx, dy);
        }

        private void ResizeDisplayViewport(Vector e, ResizeEdge relativeTo)
        {
            return; // NOT IMPLEMENTED
            // get the existing viewport rect and scale
/*
            var viewportRect = _viewFinderDisplay.ViewportRect;
            var scale = _viewFinderDisplay.Scale;

            // ensure that we stay within the bounds of the VisualBrush
            var x = Math.Max(_resizeViewportBounds.Left, Math.Min(_resizeDraggingPoint.X + e.HorizontalChange, _resizeViewportBounds.Right));
            var y = Math.Max(_resizeViewportBounds.Top, Math.Min(_resizeDraggingPoint.Y + e.VerticalChange, _resizeViewportBounds.Bottom));

            // get the selected region in the coordinate space of the content
            var anchorPoint = new Point(_resizeAnchorPoint.X / scale, _resizeAnchorPoint.Y / scale);
            var newRegionVector = new Vector((x - _resizeAnchorPoint.X) / scale / _viewboxFactor, (y - _resizeAnchorPoint.Y) / scale / _viewboxFactor);
            var region2 = new Rect(anchorPoint, newRegionVector);

            // now translate the region from the coordinate space of the content
            // to the coordinate space of the content presenter
            var region =
              new Rect(
                (Content as Control).TranslatePoint(region2.TopLeft, _presenter),
                (Content as Control).TranslatePoint(region2.BottomRight, _presenter));

            // calculate actual scale value
            var aspectX = RenderSize.Width / region.Width;
            var aspectY = RenderSize.Height / region.Height;
            scale = aspectX < aspectY ? aspectX : aspectY;

            // scale relative to the anchor point
            ZoomToInternal(region2);
*/
        }

        private void OnDrag(Vector e)
        {
            /* Point relativePosition = _relativePosition;
             double scale = this.Scale;
             Point newPosition = relativePosition + (this.ContentOffset * scale) + new Vector(e.HorizontalChange * scale, e.VerticalChange * scale);*/
            var dd = new Vector(e.X * Zoom * _viewboxFactor, e.Y * Zoom * _viewboxFactor);
            TranslateX += dd.X;
            TranslateY += dd.Y;
            UpdateViewport();
        }

        #endregion

        #region Updates & Refreshes

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // when the size is changing, the viewbox factor must be updated before updating the view
            UpdateViewboxFactor();
        }

        //private Rect _prevAreaSize;
        internal void UpdateViewport()
        {
            // if we haven't attached to the visual tree yet or we don't have content, just return
            if (ContentVisual == null || _viewFinderDisplay == null)
                return;

            var size = IsContentTrackable
                ? TrackableContent!.ContentSize
                : new Rect(0, 0, ContentVisual.DesiredSize.Width, ContentVisual.DesiredSize.Height);
            if (double.IsInfinity(size.X) || double.IsInfinity(size.Y)) return;

            // calculate the current viewport
            var viewport =
                new Rect(
                    this.TranslatePoint(new Point(0, 0), ContentVisual)!.Value - new Vector(size.X, 0),
                    this.TranslatePoint(new Point(Width, Height), ContentVisual)!.Value - new Vector(0, size.Y));


            // if the viewport has changed, set the Viewport dependency property
            if (!DoubleHelper.AreVirtuallyEqual(viewport, Viewport))
            {
                SetValue(ViewportProperty, viewport);
            }

            if (viewport == default)
            {
                _viewFinderDisplay.ViewportRect = viewport;
            }
            else
            {
                // adjust the viewport from the coordinate space of the Content element
                // to the coordinate space of the view finder display panel
                var scale = _viewFinderDisplay.Scale; // *_viewboxFactor;
                _viewFinderDisplay.ViewportRect = new Rect(viewport.Left * scale, viewport.Top * scale,
                    Math.Max(0.0, viewport.Width * scale), Math.Max(0.0, viewport.Height * scale));
            }
        }

        private void UpdateViewFinderDisplayContentBounds()
        {
            if (ContentVisual == null || _viewFinderDisplay is not { IsVisible: true })
                return;

            /*if (DesignerProperties.GetIsInDesignMode(this))
            {
                _viewFinderDisplay.ContentBounds = new Rect(new Size(100, 100));
                return;
            }*/

            UpdateViewboxFactor();

            // ensure the display panel has a size
            var contentSize = IsContentTrackable ? TrackableContent!.ContentSize.Size : ContentVisual.DesiredSize;
            if (contentSize == default || double.IsInfinity(contentSize.Width))
                contentSize = new Size(1, 1);
            var viewFinderSize = _viewFinderDisplay.AvailableSize;
            if (viewFinderSize == default || double.IsInfinity(viewFinderSize.Width))
                viewFinderSize = new Size(1, 1);
            if (viewFinderSize.Width > 0d && DoubleHelper.AreVirtuallyEqual(viewFinderSize.Height, 0d))
            {
                // update height to accomodate width, while keeping a ratio equal to the actual content
                viewFinderSize = new Size(viewFinderSize.Width,
                    Math.Max(0, contentSize.Height * viewFinderSize.Width / contentSize.Width));
            }
            else if (viewFinderSize.Height > 0d && DoubleHelper.AreVirtuallyEqual(viewFinderSize.Width, 0d))
            {
                // update width to accomodate height, while keeping a ratio equal to the actual content
                viewFinderSize = new Size(Math.Max(0, contentSize.Width * viewFinderSize.Height / contentSize.Height),
                    viewFinderSize.Width);
            }

            // determine the scale of the view finder display panel
            var aspectX = viewFinderSize.Width / contentSize.Width;
            var aspectY = viewFinderSize.Height / contentSize.Height;
            var scale = aspectX < aspectY ? aspectX : aspectY;
            if (double.IsInfinity(scale) || double.IsNaN(scale)) scale = 1d;
            if (CustomHelper.IsInDesignMode(this)) scale = 0.8;

            // determine the rect of the VisualBrush
            var vbWidth = contentSize.Width * scale;
            var vbHeight = contentSize.Height * scale;

            // set the ContentBounds and Scale properties on the view finder display panel
            _viewFinderDisplay.Scale = scale;
            _viewFinderDisplay.ContentBounds = new Rect(new Size(Math.Max(0, vbWidth), Math.Max(0, vbHeight)));
        }

        private void UpdateViewboxFactor()
        {
            if (ContentVisual == null) return;
            var contentWidth = Width;
            var trueContentWidth =
                IsContentTrackable ? TrackableContent!.ContentSize.Width : ContentVisual.DesiredSize.Width;
            if (contentWidth <= 1 || trueContentWidth <= 1) _viewboxFactor = 1d;
            else _viewboxFactor = contentWidth / trueContentWidth;
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets if zoom animation is enabled. Default value is True.
        /// </summary>
        public virtual bool IsAnimationEnabled { get; set; } = true;

        /// <summary>
        /// Use Ctrl key to zoom with mouse wheel or without it. Default value is True.
        /// </summary>
        public bool UseCtrlForMouseWheel { get; set; } = true;

        /// <summary>
        /// Gets or sets mousewheel zooming mode. Positional: depend on mouse position. Absolute: center area. Default value is Positional.
        /// </summary>
        public MouseWheelZoomingMode MouseWheelZoomingMode { get; set; }

        /// <summary>
        /// Fires when area has been selected using SelectionModifiers 
        /// </summary>
        public event AreaSelectedEventHandler? AreaSelected;

        private void OnAreaSelected(Rect selection)
        {
            AreaSelected?.Invoke(this, new AreaSelectedEventArgs(selection));
        }

        public static readonly StyledProperty<TimeSpan> AnimationLengthProperty =
            AvaloniaProperty.Register<ZoomControl, TimeSpan>(nameof(AnimationLength), TimeSpan.FromMilliseconds(500));

        public static readonly StyledProperty<bool> IsDragSelectByDefaultProperty =
            AvaloniaProperty.Register<ZoomControl, bool>(nameof(IsDragSelectByDefaultProperty));

        public static readonly StyledProperty<double> ZoomStepProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(ZoomStep), 5.0);

        public static readonly StyledProperty<double> MaxZoomProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(MaxZoom), 100.0);

        public static readonly StyledProperty<double> MinZoomProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(MinZoom), 0.01);


        public static readonly StyledProperty<ZoomControlModes> ModeProperty =
            AvaloniaProperty.Register<ZoomControl, ZoomControlModes>(nameof(Mode));

        private static void Mode_PropertyChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            var mode = (ZoomControlModes?)e.NewValue;
            switch (mode)
            {
                case ZoomControlModes.Fill:
                    zc.DoZoomToFill();
                    break;
                case ZoomControlModes.Original:
                    zc.DoZoomToOriginal();
                    break;
                case ZoomControlModes.Custom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static readonly StyledProperty<ZoomViewModifierMode> ModifierModeProperty =
            AvaloniaProperty.Register<ZoomControl, ZoomViewModifierMode>(nameof(ModifierMode));

        #region TranslateX TranslateY

        public static readonly StyledProperty<double> TranslateXProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(TranslateX), coerce: TranslateX_Coerce);

        public static readonly StyledProperty<double> TranslateYProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(TranslateY), coerce: TranslateY_Coerce);


        private static double TranslateX_Coerce(AvaloniaObject avaloniaObject, double d)
        {
            var zc = (ZoomControl)avaloniaObject;
            return zc.GetCoercedTranslateX(d, zc.Zoom);
        }

        private double GetCoercedTranslateX(double baseValue, double zoom)
        {
            return _presenter == null ? 0.0 : baseValue;
        }

        private static double TranslateY_Coerce(AvaloniaObject avaloniaObject, double d)
        {
            var zc = (ZoomControl)avaloniaObject;
            return zc.GetCoercedTranslateY(d, zc.Zoom);
        }

        private double GetCoercedTranslateY(double baseValue, double zoom)
        {
            return _presenter == null ? 0.0 : baseValue;
        }

        private static void TranslateX_PropertyChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            if (zc._translateTransform == null)
                return;
            zc._translateTransform.X = (double)e.NewValue!;
            if (!zc._isZooming)
                zc.Mode = ZoomControlModes.Custom;
            zc.OnPropertyChanged(nameof(Presenter));
            zc.Presenter?.OnPropertyChanged(nameof(RenderTransform));
        }

        private static void TranslateY_PropertyChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            if (zc._translateTransform == null)
                return;
            zc._translateTransform.Y = (double)e.NewValue!;
            if (!zc._isZooming)
                zc.Mode = ZoomControlModes.Custom;
            zc.OnPropertyChanged(nameof(Presenter));
            zc.Presenter?.OnPropertyChanged(nameof(RenderTransform));
        }

        #endregion

        public static readonly StyledProperty<Brush> ZoomBoxBackgroundProperty =
            AvaloniaProperty.Register<ZoomControl, Brush>(nameof(ZoomBoxBackground));


        public static readonly StyledProperty<Brush> ZoomBoxBorderBrushProperty =
            AvaloniaProperty.Register<ZoomControl, Brush>(nameof(ZoomBoxBorderBrush));


        public static readonly StyledProperty<Thickness> ZoomBoxBorderThicknessProperty =
            AvaloniaProperty.Register<ZoomControl, Thickness>(nameof(ZoomBoxBorderThickness), new Thickness(1.0));


        public static readonly StyledProperty<double> ZoomBoxOpacityProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(ZoomBoxOpacity), 0.5);


        public static readonly StyledProperty<Rect> ZoomBoxProperty =
            AvaloniaProperty.Register<ZoomControl, Rect>(nameof(ZoomBox));

        public static readonly StyledProperty<double> ZoomSensitivityProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(ZoomSensitivity), 100.0);

        #region Zoom

        public static readonly StyledProperty<double> ZoomProperty =
            AvaloniaProperty.Register<ZoomControl, double>(nameof(Zoom), 1.0);

        private static void Zoom_PropertyChanged(Control d, AvaloniaPropertyChangedEventArgs e)
        {
            var zc = (ZoomControl)d;
            zc.HookBeforeZoomChanging();

            if (zc._scaleTransform == null)
                return;

            var zoom = (double)e.NewValue!;
            zc._scaleTransform.ScaleX = zoom;
            zc._scaleTransform.ScaleY = zoom;
            if (!zc._isZooming)
            {
                var delta = (double)e.NewValue / (double)e.OldValue!;
                zc.TranslateX *= delta;
                zc.TranslateY *= delta;
                zc.Mode = ZoomControlModes.Custom;
            }

            zc.OnPropertyChanged(nameof(Presenter));
            zc.Presenter?.OnPropertyChanged(nameof(RenderTransform));
            zc.OnPropertyChanged(nameof(Zoom));
            zc.UpdateViewport();
            zc.HookAfterZoomChanging();
        }

        #endregion

        private Point _mouseDownPos;
        private ZoomContentPresenter? _presenter;

        /// <summary>
        /// Applied to the presenter.
        /// </summary>
        private ScaleTransform? _scaleTransform;

        private Vector _startTranslate;
        private TransformGroup? _transformGroup;

        /// <summary>
        /// Applied to the scrollviewer.
        /// </summary>
        private TranslateTransform? _translateTransform;

        private bool _isZooming;

        public Brush ZoomBoxBackground
        {
            get => GetValue(ZoomBoxBackgroundProperty);
            set => SetValue(ZoomBoxBackgroundProperty, value);
        }

        public Brush ZoomBoxBorderBrush
        {
            get => GetValue(ZoomBoxBorderBrushProperty);
            set => SetValue(ZoomBoxBorderBrushProperty, value);
        }

        public Thickness ZoomBoxBorderThickness
        {
            get => GetValue(ZoomBoxBorderThicknessProperty);
            set => SetValue(ZoomBoxBorderThicknessProperty, value);
        }

        public double ZoomBoxOpacity
        {
            get => GetValue(ZoomBoxOpacityProperty);
            set => SetValue(ZoomBoxOpacityProperty, value);
        }

        public Rect ZoomBox
        {
            get => GetValue(ZoomBoxProperty);
            set => SetValue(ZoomBoxProperty, value);
        }

        /// <summary>
        /// Gets origo (area center) position
        /// </summary>
        public Point OrigoPosition => new(Width / 2, Height / 2);

        /// <summary>
        /// Gets or sets translation value for X property
        /// </summary>
        public double TranslateX
        {
            get
            {
                var value = GetValue(TranslateXProperty);
                return double.IsNaN(value) ? 0 : value;
            }
            set => SetValue(TranslateXProperty, value);
        }

        /// <summary>
        /// Gets or sets translation value for Y property
        /// </summary>
        public double TranslateY
        {
            get
            {
                var value = GetValue(TranslateYProperty);
                return double.IsNaN(value) ? 0 : value;
            }
            set => SetValue(TranslateYProperty, value);
        }

        /// <summary>
        /// Gets or sets animation length
        /// </summary>
        public TimeSpan AnimationLength
        {
            get => GetValue(AnimationLengthProperty);
            set => SetValue(AnimationLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the value indicating whether to drag select without keyboard modifiers to the mouse button.
        /// </summary>
        public bool IsDragSelectByDefault
        {
            get => GetValue(IsDragSelectByDefaultProperty);
            set => SetValue(IsDragSelectByDefaultProperty, value);
        }

        /// <summary>
        /// Minimum zoom distance (Zoom out). Default value is 0.01
        /// </summary>
        public double MinZoom
        {
            get => GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        /// <summary>
        /// Maximum zoom distance (Zoom in). DEfault value is 100
        /// </summary>
        public double MaxZoom
        {
            get => GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        /// <summary>
        /// Gets or sets zoom step value (how fast the zoom can do). Default value is 5.
        /// </summary>
        public virtual double ZoomStep
        {
            get => GetValue(ZoomStepProperty);
            set => SetValue(ZoomStepProperty, value);
        }

        /// <summary>
        /// Gets or sets zoom sensitivity. Lower the value - smoother the zoom. Default value is 100.
        /// </summary>
        public virtual double ZoomSensitivity
        {
            get => GetValue(ZoomSensitivityProperty);
            set => SetValue(ZoomSensitivityProperty, value);
        }

        /// <summary>
        /// Gets or sets current zoom value
        /// </summary>
        public double Zoom
        {
            get => GetValue(ZoomProperty);
            set
            {
                if (Math.Abs(value - GetValue(ZoomProperty)) < 1e-10)
                    return;
                SetValue(ZoomProperty, value);
            }
        }

        /// <summary>
        /// Gets content object as Control
        /// </summary>
        public Control? ContentVisual => Content as Control;

        /// <summary>
        /// Gets content as ITrackableContent like GraphArea
        /// </summary>
        public ITrackableContent? TrackableContent => Content as ITrackableContent;

        private bool _isga;

        /// <summary>
        /// Is loaded content represents ITrackableContent object
        /// </summary>
        public bool IsContentTrackable => _isga;


        public ZoomContentPresenter? Presenter
        {
            get => _presenter;
            set
            {
                _presenter = value;
                if (_presenter == null)
                    return;

                //add the ScaleTransform to the presenter
                _transformGroup = new TransformGroup();
                _scaleTransform = new ScaleTransform();
                _translateTransform = new TranslateTransform();
                _transformGroup.Children.Add(_scaleTransform);
                _transformGroup.Children.Add(_translateTransform);
                _presenter.RenderTransform = _transformGroup;
                _presenter.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        public Control PresenterVisual => Presenter!;

        /// <summary>
        /// Gets or sets the active modifier mode.
        /// </summary>
        public ZoomViewModifierMode ModifierMode
        {
            get => GetValue(ModifierModeProperty);
            set => SetValue(ModifierModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the mode of the zoom control.
        /// </summary>
        public ZoomControlModes Mode
        {
            get => GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public RoutedEvent CommandZoomIn =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("ZoomIn", RoutingStrategies.Bubble);

        public RoutedEvent CommandZoomOut =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("ZoomOut", RoutingStrategies.Bubble);

        public RoutedEvent CommandPanLeft =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("PanLeft", RoutingStrategies.Bubble);

        public RoutedEvent CommandPanRight =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("PanRight", RoutingStrategies.Bubble);

        public RoutedEvent CommandPanTop =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("PanTop", RoutingStrategies.Bubble);

        public RoutedEvent CommandPanBottom =
            RoutedEvent.Register<ZoomControl, RoutedEventArgs>("PanBottom", RoutingStrategies.Bubble);

        #endregion

        #region Hooks

        protected virtual void HookBeforeZoomChanging()
        {
        }

        protected virtual void HookAfterZoomChanging()
        {
        }

        #endregion

        /// <summary>
        /// Gets or sets manual pan sensivity in points when using keys to pan zoomed content. Default value is 10.
        /// </summary>
        public double ManualPanSensivity { get; set; } = 10d;

        static ZoomControl()
        {
            ViewFinderVisibleProperty.Changed.AddClassHandler<Control>(_viewFinderDisplay_IsVisibleChanged);
        }

        public ZoomControl()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
            }
            else
            {
                PreviewMouseWheel += ZoomControl_MouseWheel;
                PreviewMouseDown += ZoomControl_PreviewMouseDown;
                MouseDown += ZoomControl_MouseDown;
                MouseUp += ZoomControl_MouseUp;

                AddHandler(SizeChangedEvent, new SizeChangedEventHandler(OnSizeChanged), true);

                BindCommand(Refocus, RefocusView, CanRefocusView);
                BindCommand(Center, CenterContent);
                BindCommand(Fill, FillToBounds);
                BindCommand(ResetZoom, ExecuteResetZoom);
                BindCommand(CommandZoomIn, (_, _) => MouseWheelAction(ZoomSensitivity, OrigoPosition));
                BindCommand(CommandZoomOut, (_, _) => MouseWheelAction(-ZoomSensitivity, OrigoPosition));
                BindCommand(CommandPanLeft,
                    (_, _) => PanAction(new Vector(TranslateX, TranslateY), new Vector(ManualPanSensivity, 0)));
                BindCommand(CommandPanRight,
                    (_, _) => PanAction(new Vector(TranslateX, TranslateY), new Vector(-ManualPanSensivity, 0)));
                BindCommand(CommandPanTop,
                    (_, _) => PanAction(new Vector(TranslateX, TranslateY), new Vector(0, ManualPanSensivity)));
                BindCommand(CommandPanBottom,
                    (_, _) => PanAction(new Vector(TranslateX, TranslateY), new Vector(0, -ManualPanSensivity)));
                BindKey(CommandPanLeft, Key.Left, KeyModifiers.None);
                BindKey(CommandPanRight, Key.Right, KeyModifiers.None);
                BindKey(CommandPanTop, Key.Up, KeyModifiers.None);
                BindKey(CommandPanBottom, Key.Down, KeyModifiers.None);
                BindKey(CommandZoomIn, Key.Up, KeyModifiers.Control);
                BindKey(CommandZoomOut, Key.Down, KeyModifiers.Control);
            }
        }

        protected void BindCommand(RoutedEvent command, EventHandler<RoutedEventArgs> execute,
            EventHandler<RoutedEventArgs>? canExecute = null)
        {
            var binding = new CommandBinding(command, execute, canExecute);
            CommandBindings.Add(binding);
        }

        /// <summary>
        /// Resets all key bindings for the control
        /// </summary>
        public void ResetKeyBindings()
        {
            InputBindings.Clear();
        }

        /// <summary>
        /// Binds specified key to command
        /// </summary>
        /// <param name="command">Command to execute on key press</param>
        /// <param name="key">Key</param>
        /// <param name="modifier">Key modifier</param>
        public void BindKey(RoutedUICommand command, Key key, KeyModifiers modifier)
        {
            InputBindings.Add(new KeyBinding(command, key, modifier));
        }

        #region ContentChanged

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            if (oldContent is ITrackableContent old) old.ContentSizeChanged -= Content_ContentSizeChanged;
            if (newContent != null)
            {
                if (newContent is ITrackableContent newc)
                {
                    _isga = true;
                    newc.ContentSizeChanged += Content_ContentSizeChanged;
                }
                else _isga = false;

                if (Template != null)
                    OnApplyTemplate();
                UpdateViewFinderDisplayContentBounds();
                UpdateViewport();
            }

            base.OnContentChanged(oldContent, newContent);
        }

        private void Content_ContentSizeChanged(object sender, ContentSizeChangedEventArgs e)
        {
            UpdateViewFinderDisplayContentBounds();
            UpdateViewport();
        }

        #endregion

        #region Mouse controls

        /// <summary>
        /// Converts screen rectangle area to rectangle in content coordinate space according to scale and translation
        /// </summary>
        /// <param name="screenRectangle">Screen rectangle data</param>
        public Rect ToContentRectangle(Rect screenRectangle)
        {
            var tl = this.TranslatePoint(new Point(screenRectangle.X, screenRectangle.Y), ContentVisual!)!.Value;

            //var br = TranslatePoint(new Point(screenRectangle.Right, screenRectangle.Bottom), ContentVisual);
            //return new Rect(tl.X, tl.Y, Math.Abs(Math.Abs(br.X) - Math.Abs(tl.X)), Math.Abs(Math.Abs(br.Y) - Math.Abs(tl.Y)));
            return screenRectangle == default
                ? default
                : new Rect(tl.X, tl.Y, screenRectangle.Width / Zoom, screenRectangle.Height / Zoom);
        }

        private void ZoomControl_MouseWheel(object sender, PointerWheelEventArgs e)
        {
            var handle =
                ((e.KeyModifiers & KeyModifiers.Control) > 0 && ModifierMode == ZoomViewModifierMode.None) ||
                UseCtrlForMouseWheel;
            if (!handle) return;

            e.Handled = true;
            MouseWheelAction(e);
            _clickTrack = false;
        }

        private void MouseWheelAction(PointerWheelEventArgs e)
        {
            MouseWheelAction(e.Delta, e.GetPosition(this));
        }

        /// <summary>
        /// Defines action on mousewheel
        /// </summary>
        /// <param name="delta">Delta from mousewheel args</param>
        /// <param name="mousePosition">Mouse position</param>
        protected virtual void MouseWheelAction(Vector delta, Point mousePosition)
        {
            var origoPosition = OrigoPosition;
            var distance = delta.Length;
            DoZoom(
                Math.Max(1 / ZoomStep, Math.Min(ZoomStep, Math.Abs(distance) / 10000.0 * ZoomSensitivity + 1)),
                distance < 0 ? -1 : 1,
                origoPosition,
                MouseWheelZoomingMode == MouseWheelZoomingMode.Absolute ? origoPosition : mousePosition,
                MouseWheelZoomingMode == MouseWheelZoomingMode.Absolute ? origoPosition : mousePosition);
        }

        private void ZoomControl_MouseUp(object sender, PointerWheelEventArgs e)
        {
            if (_clickTrack)
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent, this));
                _clickTrack = false;
                e.Handled = true;
            }

            switch (ModifierMode)
            {
                case ZoomViewModifierMode.None:
                    return;
                case ZoomViewModifierMode.Pan:
                    break;
                case ZoomViewModifierMode.ZoomIn:
                    break;
                case ZoomViewModifierMode.ZoomOut:
                    break;
                case ZoomViewModifierMode.ZoomBox:
                    if (_startedAsAreaSelection)
                    {
                        _startedAsAreaSelection = false;

                        OnAreaSelected(ToContentRectangle(ZoomBox));
                        ZoomBox = new Rect();
                    }
                    else ZoomToInternal(ZoomBox);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ModifierMode = ZoomViewModifierMode.None;
            PointerMoved -= ZoomControl_PreviewMouseMove;
        }

        protected virtual void PanAction(Vector initialPoint, Vector diff)
        {
            var translate = initialPoint + diff;
            TranslateX = translate.X;
            TranslateY = translate.Y;
            UpdateViewport();
        }

        private void ZoomControl_PreviewMouseMove(object? sender, PointerEventArgs pointerEventArgs)
        {
            if (_clickTrack)
            {
                var curPoint = Mouse.GetPosition(this);

                if (curPoint != _mouseDownPos)
                    _clickTrack = false;
            }

            switch (ModifierMode)
            {
                case ZoomViewModifierMode.None:
                    return;
                case ZoomViewModifierMode.Pan:
                    PanAction(_startTranslate, e.GetPosition(this) - _mouseDownPos);
                    break;
                case ZoomViewModifierMode.ZoomIn:
                    break;
                case ZoomViewModifierMode.ZoomOut:
                    break;
                case ZoomViewModifierMode.ZoomBox:
                    var pos = e.GetPosition(this);
                    var x = Math.Min(_mouseDownPos.X, pos.X);
                    var y = Math.Min(_mouseDownPos.Y, pos.Y);
                    var sizeX = Math.Abs(_mouseDownPos.X - pos.X);
                    var sizeY = Math.Abs(_mouseDownPos.Y - pos.Y);
                    ZoomBox = new Rect(x, y, sizeX, sizeY);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ZoomControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseDown(e, false);
        }

        private void ZoomControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseDown(e, true);
            e.Handled = false;
        }

        private bool _clickTrack;
        private bool _startedAsAreaSelection;

        private void OnMouseDown(MouseButtonEventArgs e, bool isPreview)
        {
            if (ModifierMode != ZoomViewModifierMode.None)
                return;
            _startedAsAreaSelection = false;
            switch (Keyboard.Modifiers)
            {
                case KeyModifiers.None:
                    if (!isPreview)
                    {
                        if (IsDragSelectByDefault)
                        {
                            _startedAsAreaSelection = true;
                            ModifierMode = ZoomViewModifierMode.ZoomBox;
                        }
                        else
                        {
                            ModifierMode = ZoomViewModifierMode.Pan;
                        }
                    }

                    break;
                case KeyModifiers.Alt | KeyModifiers.Control:
                    _startedAsAreaSelection = true;
                    ModifierMode = ZoomViewModifierMode.ZoomBox;
                    break;
                case KeyModifiers.Alt:
                    ModifierMode = ZoomViewModifierMode.ZoomBox;
                    break;
                case KeyModifiers.Control:
                    break;
                case KeyModifiers.Shift:
                    ModifierMode = ZoomViewModifierMode.Pan;
                    break;
                case KeyModifiers.Windows:
                    break;
                default:
                    return;
            }

            _clickTrack = true;
            _mouseDownPos = e.GetPosition(this);

            if (ModifierMode == ZoomViewModifierMode.None)
                return;

            _startTranslate = new Vector(TranslateX, TranslateY);
            Mouse.Capture(this);
            PreviewMouseMove += ZoomControl_PreviewMouseMove;
        }

        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(nameof(Click),
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ZoomControl));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        #endregion

        #region Animation

        public event EventHandler? ZoomAnimationCompleted;

        private void OnZoomAnimationCompleted()
        {
            ZoomAnimationCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void DoZoomAnimation(double targetZoom, double transformX, double transformY, bool isZooming = true)
        {
            if (targetZoom == 0d && double.IsNaN(transformX) && double.IsNaN(transformY)) return;
            _isZooming = isZooming;
            if (!IsAnimationEnabled)
            {
                SetCurrentValue(TranslateXProperty, transformX);
                SetCurrentValue(TranslateYProperty, transformY);
                SetCurrentValue(ZoomProperty, targetZoom);
                ZoomCompleted(this, EventArgs.Empty);
                return;
            }

            var duration = new Duration(AnimationLength);
            var value = GetValue(TranslateXProperty);
            if (double.IsNaN(value) || double.IsInfinity(value)) SetValue(TranslateXProperty, 0d);
            value = GetValue(TranslateYProperty);
            if (double.IsNaN(value) || double.IsInfinity(value)) SetValue(TranslateYProperty, 0d);
            StartAnimation(TranslateXProperty, transformX, duration);
            if (double.IsNaN(transformY) || double.IsInfinity(transformY)) transformY = 0;
            StartAnimation(TranslateYProperty, transformY, duration);
            if (double.IsNaN(targetZoom) || double.IsInfinity(targetZoom)) targetZoom = 1;
            StartAnimation(ZoomProperty, targetZoom, duration);
        }

        private void StartAnimation(StyledProperty dp, double toValue, Duration duration)
        {
            if (double.IsNaN(toValue) || double.IsInfinity(toValue))
            {
                if (dp == ZoomProperty)
                {
                    _isZooming = false;
                }

                return;
            }

            var animation = new DoubleAnimation(toValue, duration);
            Timeline.SetDesiredFrameRate(animation, 30);

            if (dp == ZoomProperty)
            {
                _zoomAnimCount++;
                animation.Completed += ZoomCompleted;
            }

            BeginAnimation(dp, animation, HandoffBehavior.Compose);
        }

        private int _zoomAnimCount;

        private void ZoomCompleted(object? sender, EventArgs e)
        {
            _zoomAnimCount--;
            if (_zoomAnimCount > 0)
                return;
            var zoom = Zoom;
            BeginAnimation(ZoomProperty, null);
            SetValue(ZoomProperty, zoom);
            _isZooming = false;
            UpdateViewport();
            OnZoomAnimationCompleted();
        }

        #endregion

        /// <summary>
        /// Zoom to rectangle area of the content
        /// </summary>
        /// <param name="rectangle">Rectangle area</param>
        /// <param name="usingContentCoordinates">Sets if content coordinates or screen coordinates was specified</param>
        public void ZoomToContent(Rect rectangle, bool usingContentCoordinates = true)
        {
            //if content isn't Control - return
            if (ContentVisual == null) return;
            // translate the region from the coordinate space of the content 
            // to the coordinate space of the content presenter
            var region = usingContentCoordinates
                ? new Rect(
                    ContentVisual.TranslatePoint(rectangle.TopLeft, _presenter),
                    ContentVisual.TranslatePoint(rectangle.BottomRight, _presenter))
                : rectangle;

            // calculate actual zoom, which must fit the entire selection 
            // while maintaining a 1:1 ratio
            var aspectX = ActualWidth / region.Width;
            var aspectY = ActualHeight / region.Height;
            var newRelativeScale = aspectX < aspectY ? aspectX : aspectY;
            // ensure that the scale value alls within the valid range
            if (newRelativeScale > MaxZoom)
                newRelativeScale = MaxZoom;
            else if (newRelativeScale < MinZoom)
                newRelativeScale = MinZoom;

            var center = new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
            var newRelativePosition =
                new Point((ActualWidth / 2 - center.X) * Zoom, (ActualHeight / 2 - center.Y) * Zoom);

            TranslateX = newRelativePosition.X;
            TranslateY = newRelativePosition.Y;
            Zoom = newRelativeScale;
        }

        /// <summary>
        /// Zoom to original size
        /// </summary>
        public void ZoomToOriginal()
        {
            if (Mode == ZoomControlModes.Original)
                DoZoomToOriginal();
            else Mode = ZoomControlModes.Original;
        }

        /// <summary>
        /// Centers content on the screen
        /// </summary>
        public void CenterContent()
        {
            if (_presenter == null)
                return;

            var initialTranslate = GetTrackableTranslate();
            DoZoomAnimation(Zoom, initialTranslate.X * Zoom, initialTranslate.Y * Zoom);
        }

        /// <summary>
        /// Zoom to fill screen area with the content
        /// </summary>
        public void ZoomToFill()
        {
            if (Mode == ZoomControlModes.Fill)
                DoZoomToFill();
            else Mode = ZoomControlModes.Fill;
        }

        private void ZoomToInternal(Rect rect, bool setDelta = false)
        {
            var deltaZoom = Math.Min(ActualWidth / rect.Width, ActualHeight / rect.Height);
            var startHandlePosition = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            DoZoom(deltaZoom, 1, OrigoPosition, startHandlePosition, OrigoPosition, setDelta);
            ZoomBox = new Rect();
        }

        /// <summary>
        /// Returns initial translate depending on container graph settings (to deal with optinal new coord system)
        /// </summary>
        private Vector GetTrackableTranslate()
        {
            if (!IsContentTrackable) return new Vector();
            return DesignerProperties.GetIsInDesignMode(this)
                ? GetInitialTranslate(200, 100)
                : GetInitialTranslate(TrackableContent!.ContentSize.Width, TrackableContent.ContentSize.Height,
                    TrackableContent.ContentSize.X, TrackableContent.ContentSize.Y);
        }

        private void DoZoomToOriginal()
        {
            if (_presenter == null)
                return;

            var initialTranslate = GetTrackableTranslate();
            DoZoomAnimation(1.0, initialTranslate.X, initialTranslate.Y);
        }

        private Vector GetInitialTranslate(double contentWidth, double contentHeight, double offsetX = 0,
            double offsetY = 0)
        {
            if (_presenter == null)
                return new Vector(0.0, 0.0);
            var w = contentWidth - ActualWidth;
            var h = contentHeight - ActualHeight;
            var tX = -(w / 2.0 + offsetX);
            var tY = -(h / 2.0 + offsetY);

            return new Vector(tX, tY);
        }

        private void DoZoomToFill()
        {
            if (_presenter == null)
                return;
            var c = IsContentTrackable ? TrackableContent!.ContentSize.Size : ContentVisual!.DesiredSize;
            if (c.Width == 0 || double.IsNaN(c.Width) || double.IsInfinity(c.Width)) return;

            var deltaZoom = Math.Min(MaxZoom, Math.Min(ActualWidth / c.Width, ActualHeight / c.Height));
            var initialTranslate =
                IsContentTrackable ? GetTrackableTranslate() : GetInitialTranslate(c.Width, c.Height);
            DoZoomAnimation(deltaZoom, initialTranslate.X * deltaZoom, initialTranslate.Y * deltaZoom);
        }

        private void DoZoom(double deltaZoom, int mod, Point origoPosition, Point startHandlePosition,
            Point targetHandlePosition, bool setDelta = false)
        {
            var startZoom = Zoom;
            var currentZoom = setDelta ? deltaZoom : mod == -1 ? startZoom / deltaZoom : startZoom * deltaZoom;
            currentZoom = Math.Max(MinZoom, Math.Min(MaxZoom, currentZoom));

            var startTranslate = new Vector(TranslateX, TranslateY);

            var v = startHandlePosition - origoPosition;
            var vTarget = targetHandlePosition - origoPosition;

            var targetPoint = (v - startTranslate) / startZoom;
            var zoomedTargetPointPos = targetPoint * currentZoom + startTranslate;
            var endTranslate = vTarget - zoomedTargetPointPos;


            if (setDelta)
            {
                var transformX = GetCoercedTranslateX(endTranslate.X, currentZoom);
                var transformY = GetCoercedTranslateY(endTranslate.Y, currentZoom);
                DoZoomAnimation(currentZoom, transformX, transformY);
            }
            else
            {
                var transformX = GetCoercedTranslateX(TranslateX + endTranslate.X, currentZoom);
                var transformY = GetCoercedTranslateY(TranslateY + endTranslate.Y, currentZoom);
                DoZoomAnimation(currentZoom, transformX, transformY);
            }

            Mode = ZoomControlModes.Custom;
        }

        /*private void FakeZoom()
        {
            var startZoom = Zoom;
            var currentZoom = startZoom;
            currentZoom = Math.Max(MinZoom, Math.Min(MaxZoom, currentZoom));

            var startTranslate = new Vector(TranslateX, TranslateY);

            var v = (OrigoPosition - OrigoPosition);
            var vTarget = (OrigoPosition - OrigoPosition);

            var targetPoint = (v - startTranslate) / startZoom;
            var zoomedTargetPointPos = targetPoint * currentZoom + startTranslate;
            var endTranslate = vTarget - zoomedTargetPointPos;

            var transformX = GetCoercedTranslateX(TranslateX + endTranslate.X, currentZoom);
            var transformY = GetCoercedTranslateY(TranslateY + endTranslate.Y, currentZoom);
            DoZoomAnimation(currentZoom, transformX, transformY);
        }*/

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            AttachToVisualTree(e);

            if (CustomHelper.IsInDesignMode(this))
            {
                ViewFinder?.SetCurrentValue(ViewFinderVisibleProperty, false);
                return;
            }

            //get the presenter, and initialize
            Presenter = GetTemplateChild(PART_PRESENTER) as ZoomContentPresenter;
            if (Presenter != null)
            {
                Presenter.SizeChanged -= Presenter_SizeChanged;
                Presenter.ContentSizeChanged -= Presenter_ContentSizeChanged;
                Presenter.SizeChanged += Presenter_SizeChanged;
                Presenter.ContentSizeChanged += Presenter_ContentSizeChanged;
            }

            if (Mode == ZoomControlModes.Fill)
                DoZoomToFill();
        }

        private void Presenter_ContentSizeChanged(object sender, Size newSize)
        {
            if (Mode == ZoomControlModes.Fill)
                DoZoomToFill();
        }

        private void Presenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateViewport();
            if (Mode == ZoomControlModes.Fill)
                DoZoomToFill();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public delegate void ValueChangedEventArgs(object sender, StyledPropertyChangedEventArgs args);
}