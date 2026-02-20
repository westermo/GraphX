using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls.Behaviours;
using Westermo.GraphX.Controls.Controls.Interfaces;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Visual edge control representing a connection between two vertices in the graph.
/// </summary>
/// <remarks>
/// EdgeControl provides:
/// - Visual representation of graph edges with customizable line styles
/// - Support for self-looping edges (connecting a vertex to itself)
/// - Edge pointer (arrow) rendering at source and/or target endpoints
/// - Position tracking and automatic updates when connected vertices move
/// - Throttled updates for performance during rapid position changes
/// - Drag support via <see cref="IDraggable"/>
/// </remarks>
[Serializable]
public class EdgeControl : EdgeControlBase, IDraggable
{
    #region Dependency Properties

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<EdgeControl, double>(
            nameof(StrokeThickness), 5);


    /// <summary>
    /// Custom edge thickness
    /// </summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }


    private static readonly StyledProperty<bool> IsSelfLoopedProperty =
        AvaloniaProperty.Register<EdgeControl, bool>(nameof(IsSelfLooped));

    private bool IsSelfLoopedInternal => Source != null && Target != null && Source.Vertex == Target.Vertex;

    /// <summary>
    /// Gets if this edge is self looped (have same Source and Target)
    /// </summary>
    public sealed override bool IsSelfLooped
    {
        get => IsSelfLoopedInternal;
        protected set => SetValue(IsSelfLoopedProperty, value);
    }

    #endregion


    #region public Clean()

    public override void Clean()
    {
        Source = null;
        Target = null;
        Edge = null;
        RootArea = null!;
        DragBehaviour.SetIsDragEnabled(this, false);
        LineGeometry = null;
        LinePathObject = null;
        SelfLoopIndicator = null;
        EdgeLabelControls.ForEach(l => l.Dispose());
        EdgeLabelControls.Clear();

        if (EdgePointerForSource != null)
        {
            EdgePointerForSource.Dispose();
            EdgePointerForSource = null;
        }

        if (EdgePointerForTarget != null)
        {
            EdgePointerForTarget.Dispose();
            EdgePointerForTarget = null;
        }
    }

    #endregion

    #region Vertex position tracing

    protected override void OnSourceChanged(Control d, AvaloniaPropertyChangedEventArgs e)
    {
        EndpointChanged(e);
    }

    protected override void OnTargetChanged(Control d, AvaloniaPropertyChangedEventArgs e)
    {
        EndpointChanged(e);
    }

    private void EndpointChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is VertexControl oldTarget)
        {
            oldTarget.PositionChanged -= OnEndpointMoved;
            oldTarget.SizeChanged -= OnEndpointResized;
        }

        if (e.NewValue is VertexControl newTarget)
        {
            newTarget.PositionChanged += OnEndpointMoved;
            newTarget.SizeChanged += OnEndpointResized;
        }

        IsSelfLooped = IsSelfLoopedInternal;
        InvalidateMeasure();
    }

    private void OnEndpointMoved(object sender, EventArgs e)
    {
        InvalidateMeasure();
    }

    #endregion

    private void OnEndpointResized(object? sender, SizeChangedEventArgs e)
    {
        InvalidateMeasure();
    }

    static EdgeControl()
    {
        //override the StyleKey
    }

    private bool _clickTrack;
    private Point _clickTrackPoint;


    public EdgeControl()
        : this(null, null, null)
    {
    }

    public EdgeControl(VertexControl? source, VertexControl? target, object? edge, bool showArrows = true)
    {
        DataContext = edge;
        Source = source;
        Target = target;
        Edge = edge;
        DataContext = edge;
        SetCurrentValue(ShowArrowsProperty, showArrows);
        IsHiddenEdgesUpdated = true;

        DoubleTapped += EdgeControl_MouseDoubleClick;
        IsSelfLooped = IsSelfLoopedInternal;
    }

    ~EdgeControl()
    {
        DoubleTapped -= EdgeControl_MouseDoubleClick;
    }

    #region Event handlers

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);


        if (IsVisible && _clickTrack)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            RootArea?.OnEdgeClicked(this, e, e.KeyModifiers);
        }

        _clickTrack = false;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (IsVisible)
            RootArea?.OnEdgeMouseLeave(this, e, e.KeyModifiers);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (IsVisible)
            RootArea?.OnEdgeMouseEnter(this, e, e.KeyModifiers);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_clickTrack)
        {
            var curPoint = e.GetPosition(RootArea);

            if (curPoint != _clickTrackPoint)
                _clickTrack = false;
        }

        if (IsVisible)
            RootArea?.OnEdgeMouseMove(this, e, e.KeyModifiers);
    }

    private void EdgeControl_MouseDoubleClick(object? sender, TappedEventArgs e)
    {
        if (IsVisible)
            RootArea?.OnEdgeDoubleClick(this, e, e.KeyModifiers);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (IsVisible)
            RootArea?.OnEdgeSelected(this, e, e.KeyModifiers);
        _clickTrack = true;
        _clickTrackPoint = e.GetPosition(RootArea);
    }

    #endregion

    #region Click Event

    public static readonly RoutedEvent ClickEvent =
        RoutedEvent.Register<EdgeControl, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    #endregion

    public override void Dispose()
    {
        Clean();
    }

    /// <summary>
    /// Gets Edge data as specified class
    /// </summary>
    /// <typeparam name="T">Class</typeparam>
    public T? GetDataEdge<T>() where T : IGraphXCommonEdge
    {
        return (T?)Edge;
    }

    public bool StartDrag(PointerPressedEventArgs origin)
    {
        if (!DragBehaviour.GetIsDragEnabled(this)) return false;
        if (!IsVisible) return false;
        if (!origin.Properties.IsLeftButtonPressed) return false;
        IsDragging = true;
        return true;
    }

    public bool IsDragging { get; private set; }

    public void Drag(PointerEventArgs current)
    {
        if (IsDragging)
        {
            // During drag, store the override endpoint in the same coordinate space
            // as vertex positions (the graph area / RootArea). This avoids coordinate
            // mismatches when the edge path is calculated relative to the graph area.
            var graphAreaBase = RootArea;
            OverrideEndpoint = graphAreaBase != null
                ? current.GetPosition(graphAreaBase)
                : current.GetPosition(this);
        }
    }

    public bool EndDrag(PointerReleasedEventArgs e)
    {
        if (!IsDragging) return true;
        var graphAreaBase = RootArea;

        var vertexControl = graphAreaBase?.GetVertexControlAt(e.GetPosition(graphAreaBase));

        if (vertexControl == null) return true;
        OverrideEndpoint = null;
        Target = vertexControl;

        if (vertexControl.VertexConnectionPointsList.Count > 0)
        {
            var vertexConnectionPoint = vertexControl.GetConnectionPointAt(e.GetPosition(graphAreaBase));

            var edge = (IGraphXCommonEdge)Edge!;

            if (vertexConnectionPoint != null)
            {
                edge.TargetConnectionPointId = vertexConnectionPoint.Id;
            }
            else
            {
                edge.TargetConnectionPointId = null;
            }
        }

        IsDragging = false;
        return true;
    }

    public void EndDrag()
    {
    }

    public Visual? Container => RootArea;
}