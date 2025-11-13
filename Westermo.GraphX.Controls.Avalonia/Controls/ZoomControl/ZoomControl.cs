using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Controls.ZoomControl.SupportClasses;
using Westermo.GraphX.Controls.Models;

namespace Westermo.GraphX.Controls.Controls.ZoomControl;

[TemplatePart(Name = PART_PRESENTER, Type = typeof(ZoomContentPresenter))]
public sealed class ZoomControl : ContentControl, IZoomControl, INotifyPropertyChanged
{
    private const string PART_PRESENTER = "PART_Presenter";

    private bool _clickTrack;
    private bool _startedAsAreaSelection;

    // Hook placeholders
    private void HookBeforeZoomChanging()
    {
    }

    private void HookAfterZoomChanging()
    {
    }

    // Simple helpers to emulate WPF ActualWidth/ActualHeight semantics
    private double ActualWidth => Bounds.Width;
    private double ActualHeight => Bounds.Height;

    // Public mode properties (moved earlier to ensure availability)
    public ZoomControlModes Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public ZoomViewModifierMode ModifierMode
    {
        get => GetValue(ModifierModeProperty);
        set => SetValue(ModifierModeProperty, value);
    }

    // Utility methods restored
    public void ZoomToOriginal() => DoZoomToOriginal();
    public void ZoomToFill() => DoZoomToFill();

    public void CenterContent()
    {
        if (_presenter == null) return;
        var initialTranslate = GetTrackableTranslate();
        DoZoomAnimation(Zoom, initialTranslate.X * Zoom, initialTranslate.Y * Zoom, false);
    }

    public Rect ToContentRectangle(Rect screenRectangle)
    {
        if (ContentVisual == null) return default;
        var tl = this.TranslatePoint(new Point(screenRectangle.X, screenRectangle.Y), ContentVisual);
        if (tl == null) return default;
        return screenRectangle == default
            ? default
            : new Rect(tl.Value.X, tl.Value.Y, screenRectangle.Width / Zoom, screenRectangle.Height / Zoom);
    }

    private Vector GetInitialTranslate(double contentWidth, double contentHeight, double offsetX = 0,
        double offsetY = 0)
    {
        if (_presenter == null) return new Vector(0, 0);
        var w = contentWidth - ActualWidth;
        var h = contentHeight - ActualHeight;
        var tX = -(w / 2.0 + offsetX);
        var tY = -(h / 2.0 + offsetY);
        return new Vector(tX, tY);
    }

    #region Center Command

    public static RoutedEvent Center =
        RoutedEvent.Register<ZoomControl, RoutedEventArgs>("Center", RoutingStrategies.Bubble);

    private void CenterContent(object sender, RoutedEventArgs e) => CenterContent();

    #endregion

    #region Fill Command

    public static RoutedEvent Fill =
        RoutedEvent.Register<ZoomControl, RoutedEventArgs>("FillToBounds", RoutingStrategies.Bubble);

    private void FillToBounds(object sender, RoutedEventArgs e) => ZoomToFill();

    #endregion

    #region ResetZoom Command

    public static RoutedEvent ResetZoom =
        RoutedEvent.Register<ZoomControl, RoutedEventArgs>("ResetZoom", RoutingStrategies.Bubble);

    private void ExecuteResetZoom(object sender, RoutedEventArgs e) => Zoom = 1d;

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


    #region Properties

    public bool IsAnimationEnabled { get; set; } = true;
    public bool UseCtrlForMouseWheel { get; set; } = true;
    public MouseWheelZoomingMode MouseWheelZoomingMode { get; set; }
    public event AreaSelectedEventHandler? AreaSelected;
    private void OnAreaSelected(Rect selection) => AreaSelected?.Invoke(this, new AreaSelectedEventArgs(selection));

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

    private static void Mode_PropertyChanged(ZoomControl zc, AvaloniaPropertyChangedEventArgs e)
    {
        switch ((ZoomControlModes?)e.NewValue)
        {
            case ZoomControlModes.Fill: zc.DoZoomToFill(); break;
            case ZoomControlModes.Original: zc.DoZoomToOriginal(); break;
            case ZoomControlModes.Custom: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public static readonly StyledProperty<ZoomViewModifierMode> ModifierModeProperty =
        AvaloniaProperty.Register<ZoomControl, ZoomViewModifierMode>(nameof(ModifierMode));

    public static readonly StyledProperty<double> TranslateXProperty =
        AvaloniaProperty.Register<ZoomControl, double>(nameof(TranslateX), coerce: TranslateX_Coerce);

    public static readonly StyledProperty<double> TranslateYProperty =
        AvaloniaProperty.Register<ZoomControl, double>(nameof(TranslateY), coerce: TranslateY_Coerce);

    private static double TranslateX_Coerce(AvaloniaObject o, double d) =>
        ((ZoomControl)o).GetCoercedTranslateX(d, ((ZoomControl)o).Zoom);

    private double GetCoercedTranslateX(double baseValue, double zoom) => _presenter == null ? 0.0 : baseValue;

    private static double TranslateY_Coerce(AvaloniaObject o, double d) =>
        ((ZoomControl)o).GetCoercedTranslateY(d, ((ZoomControl)o).Zoom);

    private double GetCoercedTranslateY(double baseValue, double zoom) => _presenter == null ? 0.0 : baseValue;

    private static void TranslateX_PropertyChanged(ZoomControl zc, AvaloniaPropertyChangedEventArgs e)
    {
        if (zc._translateTransform == null) return;
        zc._translateTransform.X = (double)e.NewValue!;
        if (!zc._isZooming) zc.Mode = ZoomControlModes.Custom;
        zc.OnPropertyChanged(nameof(Presenter));
        zc.Presenter?.InvalidateVisual();
    }

    private static void TranslateY_PropertyChanged(ZoomControl zc, AvaloniaPropertyChangedEventArgs e)
    {
        if (zc._translateTransform == null) return;
        zc._translateTransform.Y = (double)e.NewValue!;
        if (!zc._isZooming) zc.Mode = ZoomControlModes.Custom;
        zc.OnPropertyChanged(nameof(Presenter));
        zc.Presenter?.InvalidateVisual();
    }

    public static readonly StyledProperty<IBrush> ZoomBoxBackgroundProperty =
        AvaloniaProperty.Register<ZoomControl, IBrush>(nameof(ZoomBoxBackground));

    public static readonly StyledProperty<IBrush> ZoomBoxBorderBrushProperty =
        AvaloniaProperty.Register<ZoomControl, IBrush>(nameof(ZoomBoxBorderBrush));

    public static readonly StyledProperty<Thickness> ZoomBoxBorderThicknessProperty =
        AvaloniaProperty.Register<ZoomControl, Thickness>(nameof(ZoomBoxBorderThickness), new Thickness(1.0));

    public static readonly StyledProperty<double> ZoomBoxOpacityProperty =
        AvaloniaProperty.Register<ZoomControl, double>(nameof(ZoomBoxOpacity), 0.5);

    public static readonly StyledProperty<Rect> ZoomBoxProperty =
        AvaloniaProperty.Register<ZoomControl, Rect>(nameof(ZoomBox));

    public static readonly StyledProperty<double> ZoomSensitivityProperty =
        AvaloniaProperty.Register<ZoomControl, double>(nameof(ZoomSensitivity), 1000.0);

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ZoomControl, double>(nameof(Zoom), 1.0);

    private static void Zoom_PropertyChanged(ZoomControl zc, AvaloniaPropertyChangedEventArgs e)
    {
        zc.HookBeforeZoomChanging();
        if (zc._scaleTransform == null) return;
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
        zc.Presenter?.InvalidateVisual();
        zc.OnPropertyChanged(nameof(Zoom));
        zc.HookAfterZoomChanging();
    }

    #endregion

    private Point _mouseDownPos;
    private ZoomContentPresenter? _presenter;
    private ScaleTransform? _scaleTransform;
    private Vector _startTranslate;
    private TransformGroup? _transformGroup;
    private TranslateTransform? _translateTransform;
    private bool _isZooming;

    public IBrush ZoomBoxBackground
    {
        get => GetValue(ZoomBoxBackgroundProperty);
        set => SetValue(ZoomBoxBackgroundProperty, value);
    }

    public IBrush ZoomBoxBorderBrush
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

    public Point OrigoPosition => new(ActualWidth / 2, ActualHeight / 2);

    public double TranslateX
    {
        get => GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public double TranslateY
    {
        get => GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    public TimeSpan AnimationLength
    {
        get => GetValue(AnimationLengthProperty);
        set => SetValue(AnimationLengthProperty, value);
    }

    public bool IsDragSelectByDefault
    {
        get => GetValue(IsDragSelectByDefaultProperty);
        set => SetValue(IsDragSelectByDefaultProperty, value);
    }

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public double ZoomStep
    {
        get => GetValue(ZoomStepProperty);
        set => SetValue(ZoomStepProperty, value);
    }

    public double ZoomSensitivity
    {
        get => GetValue(ZoomSensitivityProperty);
        set => SetValue(ZoomSensitivityProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set
        {
            if (Math.Abs(value - GetValue(ZoomProperty)) < 1e-10) return;
            SetValue(ZoomProperty, value);
        }
    }

    public Control? ContentVisual => Content as Control;
    public ITrackableContent? TrackableContent => Content as ITrackableContent;
    private bool _isga;
    public bool IsContentTrackable => _isga;

    public new ZoomContentPresenter? Presenter
    {
        get => _presenter;
        set
        {
            _presenter = value;
            if (_presenter == null) return;
            _transformGroup = new TransformGroup();
            _scaleTransform = new ScaleTransform();
            _translateTransform = new TranslateTransform();
            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_translateTransform);
            _presenter.RenderTransform = _transformGroup;
        }
    }

    public Control PresenterVisual => Presenter!;

    static ZoomControl()
    {
        ContentProperty.Changed.AddClassHandler<ZoomControl>((zc, e) =>
            zc.HandleContentChanged(e.OldValue, e.NewValue));
        ModeProperty.Changed.AddClassHandler<ZoomControl>(Mode_PropertyChanged);
        ZoomProperty.Changed.AddClassHandler<ZoomControl>(Zoom_PropertyChanged);
        TranslateXProperty.Changed.AddClassHandler<ZoomControl>(TranslateX_PropertyChanged);
        TranslateYProperty.Changed.AddClassHandler<ZoomControl>(TranslateY_PropertyChanged);
    }

    private void HandleContentChanged(object? oldContent, object? newContent)
    {
        if (Design.IsDesignMode) return;

        if (oldContent is ITrackableContent oldTrackable)
            oldTrackable.ContentSizeChanged -= Content_ContentSizeChanged;

        if (newContent is ITrackableContent newTrackable)
        {
            _isga = true;
            newTrackable.ContentSizeChanged += Content_ContentSizeChanged;
        }
        else _isga = false;

        if (Template != null) ApplyTemplate();
    }

    public ZoomControl()
    {
        if (Design.IsDesignMode) return;
        PointerWheelChanged += ZoomControl_MouseWheel;
        PointerPressed += ZoomControl_PointerPressed;
        PointerReleased += ZoomControl_PointerReleased;
    }

    private void ZoomControl_PointerPressed(object? sender, PointerPressedEventArgs e) => OnPointerDown(e);

    private void ZoomControl_PointerReleased(object? sender, PointerReleasedEventArgs e) =>
        ZoomControl_MouseUp(sender, e);

    private static void Content_ContentSizeChanged(object sender, ContentSizeChangedEventArgs e)
    {
    }

    private void ZoomControl_MouseWheel(object? sender, PointerWheelEventArgs e)
    {
        var handle = ((e.KeyModifiers & KeyModifiers.Control) > 0 && ModifierMode == ZoomViewModifierMode.None) ||
                     UseCtrlForMouseWheel;
        if (!handle) return;
        e.Handled = true;
        MouseWheelAction(e);
        _clickTrack = false;
    }

    private void MouseWheelAction(PointerWheelEventArgs e) => MouseWheelAction(e.Delta, e.GetPosition(this));

    private void MouseWheelAction(Vector delta, Point mousePosition)
    {
        var origoPosition = OrigoPosition;
        var distance = delta.Length;
        var step = Math.Max(1 / ZoomStep, Math.Min(ZoomStep, Math.Abs(distance) / 10000.0 * ZoomSensitivity + 1));
        var mod = delta.Y < 0 ? -1 : 1;
        var startPosition = MouseWheelZoomingMode == MouseWheelZoomingMode.Absolute ? origoPosition : mousePosition;
        var targetPosition =
            MouseWheelZoomingMode == MouseWheelZoomingMode.Absolute ? origoPosition : mousePosition;

        DoZoom(step, mod, origoPosition,
            startPosition, targetPosition);
    }

    private void ZoomControl_MouseUp(object? sender, PointerReleasedEventArgs e)
    {
        if (_clickTrack)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent));
            _clickTrack = false;
        }

        if (ModifierMode == ZoomViewModifierMode.ZoomBox)
        {
            if (_startedAsAreaSelection)
            {
                _startedAsAreaSelection = false;
                OnAreaSelected(ToContentRectangle(ZoomBox));
                ZoomBox = new Rect();
            }
            else ZoomToInternal(ZoomBox);
        }

        ModifierMode = ZoomViewModifierMode.None;
        if (Equals(e.Pointer.Captured, this))
            e.Pointer.Capture(null);
    }

    private void PanAction(Vector initialPoint, Vector diff)
    {
        var translate = initialPoint + diff;
        TranslateX = translate.X;
        TranslateY = translate.Y;
    }

    private void ZoomControl_PreviewMouseMove(object? sender, PointerEventArgs e)
    {
        if (_clickTrack)
        {
            var curPoint = e.GetPosition(this);
            if (curPoint != _mouseDownPos) _clickTrack = false;
        }

        switch (ModifierMode)
        {
            case ZoomViewModifierMode.None: return;
            case ZoomViewModifierMode.Pan: PanAction(_startTranslate, e.GetPosition(this) - _mouseDownPos); break;
            case ZoomViewModifierMode.ZoomBox:
                var pos = e.GetPosition(this);
                var x = Math.Min(_mouseDownPos.X, pos.X);
                var y = Math.Min(_mouseDownPos.Y, pos.Y);
                ZoomBox = new Rect(x, y, Math.Abs(_mouseDownPos.X - pos.X), Math.Abs(_mouseDownPos.Y - pos.Y));
                break;
            case ZoomViewModifierMode.ZoomIn:
            case ZoomViewModifierMode.ZoomOut:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnPointerDown(PointerPressedEventArgs e)
    {
        if (ModifierMode != ZoomViewModifierMode.None) return;
        _startedAsAreaSelection = false;
        switch (e.KeyModifiers)
        {
            case KeyModifiers.None:
                if (IsDragSelectByDefault)
                {
                    _startedAsAreaSelection = true;
                    ModifierMode = ZoomViewModifierMode.ZoomBox;
                }
                else ModifierMode = ZoomViewModifierMode.Pan;

                break;
            case KeyModifiers.Alt | KeyModifiers.Control:
            case KeyModifiers.Alt:
                _startedAsAreaSelection = true;
                ModifierMode = ZoomViewModifierMode.ZoomBox;
                break;
            case KeyModifiers.Shift: ModifierMode = ZoomViewModifierMode.Pan; break;
            case KeyModifiers.Control:
            case KeyModifiers.Meta:
            default: return;
        }

        _clickTrack = true;
        _mouseDownPos = e.GetPosition(this);
        if (ModifierMode == ZoomViewModifierMode.None) return;
        _startTranslate = new Vector(TranslateX, TranslateY);
        e.Pointer.Capture(this);
        PointerMoved += ZoomControl_PreviewMouseMove;
    }

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<ZoomControl, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    // Simplified animation: direct property set
    private void DoZoomAnimation(double targetZoom, double transformX, double transformY, bool isZooming = true)
    {
        _isZooming = isZooming;
        SetCurrentValue(TranslateXProperty, transformX);
        SetCurrentValue(TranslateYProperty, transformY);
        SetCurrentValue(ZoomProperty, targetZoom);
        _isZooming = false;
        OnZoomAnimationCompleted();
    }

    /// <summary>
    /// Zoom to rectangle area of the content
    /// </summary>
    /// <param name="rectangle">Rectangle area</param>
    /// <param name="usingContentCoordinates">Sets if content coordinates or screen coordinates was specified</param>
    public void ZoomToContent(Rect rectangle, bool usingContentCoordinates = true)
    {
        //if content isn't UIElement - return
        if (ContentVisual == null) return;
        // translate the region from the coordinate space of the content 
        // to the coordinate space of the content presenter
        var region = usingContentCoordinates && _presenter is not null
            ? new Rect(
                ContentVisual.TranslatePoint(rectangle.TopLeft, _presenter) ?? rectangle.TopLeft,
                ContentVisual.TranslatePoint(rectangle.BottomRight, _presenter) ?? rectangle.BottomRight)
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

    public event EventHandler? ZoomAnimationCompleted;
    private void OnZoomAnimationCompleted() => ZoomAnimationCompleted?.Invoke(this, EventArgs.Empty);

    private void DoZoomToOriginal()
    {
        if (_presenter == null) return;
        var initialTranslate = GetTrackableTranslate();
        DoZoomAnimation(1.0, initialTranslate.X, initialTranslate.Y);
    }

    private void DoZoomToFill()
    {
        if (_presenter == null) return;
        var c = IsContentTrackable ? TrackableContent!.ContentSize.Size : ContentVisual!.DesiredSize;
        if (c.Width == 0 || double.IsNaN(c.Width) || double.IsInfinity(c.Width)) return;
        var deltaZoom = Math.Min(MaxZoom, Math.Min(ActualWidth / c.Width, ActualHeight / c.Height));
        var initialTranslate =
            IsContentTrackable ? GetTrackableTranslate() : GetInitialTranslate(c.Width, c.Height);
        DoZoomAnimation(deltaZoom, initialTranslate.X * deltaZoom, initialTranslate.Y * deltaZoom);
    }

    private void ZoomToInternal(Rect rect, bool setDelta = false)
    {
        var deltaZoom = Math.Min(ActualWidth / rect.Width, ActualHeight / rect.Height);
        var startHandlePosition = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        DoZoom(deltaZoom, 1, OrigoPosition, startHandlePosition, OrigoPosition, setDelta);
        ZoomBox = new Rect();
    }

    private Vector GetTrackableTranslate()
    {
        if (!IsContentTrackable) return new Vector();
        return Design.IsDesignMode
            ? GetInitialTranslate(200, 100)
            : GetInitialTranslate(TrackableContent!.ContentSize.Width, TrackableContent.ContentSize.Height,
                TrackableContent.ContentSize.X, TrackableContent.ContentSize.Y);
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


        double transformX, transformY;
        if (setDelta)
        {
            transformX = GetCoercedTranslateX(endTranslate.X, currentZoom);
            transformY = GetCoercedTranslateY(endTranslate.Y, currentZoom);
        }
        else
        {
            transformX = GetCoercedTranslateX(TranslateX + endTranslate.X, currentZoom);
            transformY = GetCoercedTranslateY(TranslateY + endTranslate.Y, currentZoom);
        }

        DoZoomAnimation(currentZoom, transformX, transformY);
        Mode = ZoomControlModes.Custom;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (Design.IsDesignMode)
        {
            return;
        }

        Presenter = e.NameScope.Find<ZoomContentPresenter>(PART_PRESENTER);
        if (Presenter != null)
        {
            Presenter.SizeChanged -= Presenter_SizeChanged;
            Presenter.ContentSizeChanged -= Presenter_ContentSizeChanged;
            Presenter.SizeChanged += Presenter_SizeChanged;
            Presenter.ContentSizeChanged += Presenter_ContentSizeChanged;
        }

        if (Mode == ZoomControlModes.Fill) DoZoomToFill();
    }

    private void Presenter_ContentSizeChanged(object sender, Size newSize)
    {
        if (Mode == ZoomControlModes.Fill) DoZoomToFill();
    }

    private void Presenter_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (Mode == ZoomControlModes.Fill) DoZoomToFill();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

#region ResizeEdge Nested Type

public enum ResizeEdge
{
    None,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Left,
    Top,
    Right,
    Bottom
}

#endregion

#region CacheBits Nested Type

public enum CacheBits
{
    IsUpdatingView = 0x1,
    IsUpdatingViewport = 0x2,
    IsDraggingViewport = 0x4,
    IsResizingViewport = 0x8,
    IsMonitoringInput = 0x10,
    IsContentWrapped = 0x20,
    HasArrangedContentPresenter = 0x40,
    HasRenderedFirstView = 0x80,
    RefocusViewOnFirstRender = 0x100,
    HasUiPermission = 0x200
}

#endregion