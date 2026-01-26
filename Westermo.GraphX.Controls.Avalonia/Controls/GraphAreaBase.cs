using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Controls.Controls.Misc;
using Westermo.GraphX.Controls.Models;

namespace Westermo.GraphX.Controls.Controls;

public abstract class GraphAreaBase : Canvas, ITrackableContent, IGraphAreaBase
{
    static GraphAreaBase()
    {
        XProperty.Changed.AddClassHandler<Control>(x_changed);
        YProperty.Changed.AddClassHandler<Control>(y_changed);
    }

    /// <summary>
    /// Gets or Sets if GraphArea is in print mode when its size is recalculated on each Measure pass
    /// </summary>
    protected bool IsInPrintMode;

    public abstract void SetPrintMode(bool value, bool offsetControls = true, int margin = 0);

    /// <summary>
    /// Automaticaly assign unique Id (if missing) for vertex and edge data classes provided as Graph in GenerateGraph() method or by Addvertex() or AddEdge() methods
    /// </summary>
    public bool AutoAssignMissingDataId { get; set; } = true;

    /// <summary>
    /// Action that will take place when LogicCore property is changed. Default: None.
    /// </summary>
    public LogicCoreChangedAction LogicCoreChangeAction
    {
        get => GetValue(LogicCoreChangeActionProperty);
        set => SetValue(LogicCoreChangeActionProperty, value);
    }

    public static readonly StyledProperty<LogicCoreChangedAction> LogicCoreChangeActionProperty =
        AvaloniaProperty.Register<GraphAreaBase, LogicCoreChangedAction>(nameof(LogicCoreChangeAction));

    protected GraphAreaBase()
    {
        LogicCoreChangeAction = LogicCoreChangedAction.None;
    }

    #region Attached Dependency Property registrations

    private static void x_changed(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.SetValue(LeftProperty, e.NewValue);
        if (control is IXYReactive reactive)
            reactive.XYChanged(e);
    }

    private static void y_changed(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.SetValue(TopProperty, e.NewValue);
        if (control is IXYReactive reactive)
            reactive.XYChanged(e);
    }

    public static readonly AttachedProperty<double> XProperty =
        AvaloniaProperty.RegisterAttached<GraphAreaBase, Control, double>("X",
            defaultBindingMode: BindingMode.TwoWay, defaultValue: double.NaN);


    public static readonly AttachedProperty<double> FinalXProperty =
        AvaloniaProperty.RegisterAttached<GraphAreaBase, Control, double>("FinalX",
            defaultBindingMode: BindingMode.TwoWay, defaultValue: double.NaN);

    public static readonly AttachedProperty<double> FinalYProperty =
        AvaloniaProperty.RegisterAttached<GraphAreaBase, Control, double>("FinalY",
            defaultBindingMode: BindingMode.TwoWay, defaultValue: double.NaN);


    public static readonly AttachedProperty<double> YProperty =
        AvaloniaProperty.RegisterAttached<GraphAreaBase, Control, double>("Y",
            defaultBindingMode: BindingMode.TwoWay, defaultValue: double.NaN);


    public static double GetX(Control obj)
    {
        return obj.GetValue(XProperty);
    }

    public static void SetX(Control obj, double value, bool alsoSetFinal = true)
    {
        obj.SetValue(XProperty, value);
        if (alsoSetFinal)
            obj.SetValue(FinalXProperty, value);
    }

    public static double GetY(Control obj)
    {
        return obj.GetValue(YProperty);
    }

    public static void SetY(Control obj, double value, bool alsoSetFinal = false)
    {
        obj.SetValue(YProperty, value);
        if (alsoSetFinal)
            obj.SetValue(FinalYProperty, value);
    }


    public static double GetFinalX(Control obj)
    {
        return obj.GetValue(FinalXProperty);
    }

    public static void SetFinalX(Control obj, double value)
    {
        obj.SetValue(FinalXProperty, value);
    }

    public static double GetFinalY(Control obj)
    {
        return obj.GetValue(FinalYProperty);
    }

    public static void SetFinalY(Control obj, double value)
    {
        obj.SetValue(FinalYProperty, value);
    }

    public static bool GetPositioningComplete(Control obj)
    {
        return obj.GetValue(PositioningCompleteProperty);
    }

    public static void SetPositioningComplete(Control obj, bool value)
    {
        obj.SetValue(PositioningCompleteProperty, value);
    }

    #region DP - ExternalSettings

    // todo: ExternalSettings or ExternalSettingsOnly?
    public static readonly StyledProperty<object> ExternalSettingsProperty =
        AvaloniaProperty.Register<GraphAreaBase, object>("ExternalSettingsOnly");

    /// <summary>
    ///User-defined settings storage for using in templates and converters
    /// </summary>
    public object ExternalSettings
    {
        get => GetValue(ExternalSettingsProperty);
        set => SetValue(ExternalSettingsProperty, value);
    }

    #endregion


    public static readonly StyledProperty<bool> PositioningCompleteProperty =
        AvaloniaProperty.RegisterAttached<GraphAreaBase, Control, bool>("PositioningComplete", true);

    #endregion

    #region Child EVENTS

    internal static readonly Size DesignSize = new(70, 25);

    /// <summary>
    /// Fires when ContentSize property is changed
    /// </summary>
    public event ContentSizeChangedEventHandler? ContentSizeChanged;

    protected void OnContentSizeChanged(Rect oldSize, Rect newSize)
    {
        ContentSizeChanged?.Invoke(this, new ContentSizeChangedEventArgs(oldSize, newSize));
    }

    /// <summary>
    /// Fires when vertex is double clicked
    /// </summary>
    public event VertexSelectedEventHandler? VertexDoubleClick;

    internal virtual void OnVertexDoubleClick(VertexControl vc, TappedEventArgs e)
    {
        VertexDoubleClick?.Invoke(this, new VertexSelectedEventArgs(vc, e, e.KeyModifiers));
    }

    /// <summary>
    /// Fires when vertex is selected
    /// </summary>
    public event VertexSelectedEventHandler? VertexSelected;

    internal virtual void OnVertexSelected(VertexControl vc, PointerEventArgs e, KeyModifiers keys)
    {
        VertexSelected?.Invoke(this, new VertexSelectedEventArgs(vc, e, keys));
    }

    /// <summary>
    /// Fires when vertex is clicked
    /// </summary>
    public event VertexClickedEventHandler? VertexClicked;

    internal virtual void OnVertexClicked(VertexControl vc, PointerEventArgs e, KeyModifiers keys)
    {
        VertexClicked?.Invoke(this, new VertexClickedEventArgs(vc, e, keys));
    }

    /// <summary>
    /// Fires when mouse up on vertex
    /// </summary>
    public event VertexSelectedEventHandler? VertexMouseUp;

    internal virtual void OnVertexMouseUp(VertexControl vc, PointerEventArgs e, KeyModifiers keys)
    {
        VertexMouseUp?.Invoke(this, new VertexSelectedEventArgs(vc, e, keys));
    }

    /// <summary>
    /// Fires when mouse is over the vertex control
    /// </summary>
    public event VertexSelectedEventHandler? VertexMouseEnter;

    internal virtual void OnVertexMouseEnter(VertexControl vc, PointerEventArgs e)
    {
        VertexMouseEnter?.Invoke(this, new VertexSelectedEventArgs(vc, e, e.KeyModifiers));
    }

    /// <summary>
    /// Fires when mouse is moved over the vertex control
    /// </summary>
    public event VertexMovedEventHandler? VertexMouseMove;

    internal virtual void OnVertexMouseMove(VertexControl vc, PointerEventArgs e)
    {
        VertexMouseMove?.Invoke(this, new VertexMovedEventArgs(vc, e));
    }

    /// <summary>
    /// Fires when mouse leaves vertex control
    /// </summary>
    public event VertexSelectedEventHandler? VertexMouseLeave;

    internal virtual void OnVertexMouseLeave(VertexControl vc, PointerEventArgs e)
    {
        VertexMouseLeave?.Invoke(this, new VertexSelectedEventArgs(vc, e, e.KeyModifiers));
    }

    /// <summary>
    /// Fires when layout algorithm calculation is finished
    /// </summary>
    public event EventHandler? LayoutCalculationFinished;

    protected virtual void OnLayoutCalculationFinished()
    {
        LayoutCalculationFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fires when overlap removal algorithm calculation is finished
    /// </summary>
    public event EventHandler? OverlapRemovalCalculationFinished;

    protected virtual void OnOverlapRemovalCalculationFinished()
    {
        OverlapRemovalCalculationFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fires when edge routing algorithm calculation is finished
    /// </summary>
    public event EventHandler? EdgeRoutingCalculationFinished;

    protected virtual void OnEdgeRoutingCalculationFinished()
    {
        EdgeRoutingCalculationFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fires when relayout operation is finished
    /// </summary>
    public event EventHandler? RelayoutFinished;

    protected virtual void OnRelayoutFinished()
    {
        RelayoutFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fires when graph generation operation is finished
    /// </summary>
    public event EventHandler? GenerateGraphFinished;

    protected virtual void OnGenerateGraphFinished()
    {
        GenerateGraphFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Fires when edge is selected
    /// </summary>
    public event EdgeSelectedEventHandler? EdgeSelected;

    internal virtual void OnEdgeSelected(EdgeControl ec, PointerPressedEventArgs? e, KeyModifiers keys)
    {
        EdgeSelected?.Invoke(this, new EdgeSelectedEventArgs(ec, e, keys));
    }

    /// <summary>
    /// Fires when edge is clicked
    /// </summary>
    public event EdgeClickedEventHandler? EdgeClicked;

    internal virtual void OnEdgeClicked(EdgeControl ec, PointerEventArgs? e, KeyModifiers keys)
    {
        EdgeClicked?.Invoke(this, new EdgeClickedEventArgs(ec, e, keys));
    }

    public event EdgeSelectedEventHandler? EdgeDoubleClick;

    internal void OnEdgeDoubleClick(EdgeControl edgeControl, TappedEventArgs? e, KeyModifiers keys)
    {
        EdgeDoubleClick?.Invoke(this, new EdgeSelectedEventArgs(edgeControl, e, keys));
    }

    public event EdgeSelectedEventHandler? EdgeMouseMove;

    internal void OnEdgeMouseMove(EdgeControl edgeControl, PointerEventArgs? e, KeyModifiers keys)
    {
        EdgeMouseMove?.Invoke(this, new EdgeSelectedEventArgs(edgeControl, e, keys));
    }

    public event EdgeSelectedEventHandler? EdgeMouseEnter;

    internal void OnEdgeMouseEnter(EdgeControl edgeControl, PointerEventArgs? e, KeyModifiers keys)
    {
        EdgeMouseEnter?.Invoke(this, new EdgeSelectedEventArgs(edgeControl, e, keys));
    }

    public event EdgeSelectedEventHandler? EdgeMouseLeave;

    internal void OnEdgeMouseLeave(EdgeControl edgeControl, PointerEventArgs? e, KeyModifiers keys)
    {
        EdgeMouseLeave?.Invoke(this, new EdgeSelectedEventArgs(edgeControl, e, keys));
    }

    #endregion

    #region ComputeEdgeRoutesByVertex()

    /// <summary>
    /// Compute new edge routes for all edges of the vertex
    /// </summary>
    /// <param name="vc">Vertex visual control</param>
    /// <param name="vertexDataNeedUpdate">If vertex data inside edge routing algorthm needs to be updated</param>
    internal virtual void ComputeEdgeRoutesByVertex(VertexControl vc, bool vertexDataNeedUpdate = true)
    {
    }

    #endregion

    #region Virtual members

    /// <summary>
    /// Returns all existing VertexControls addded into the layout
    /// </summary>
    /// <returns></returns>
    public abstract VertexControl[] GetAllVertexControls();

    public abstract VertexControl? GetVertexControlAt(Point position);

    public abstract ValueTask RelayoutGraph(bool generateAllEdges = false, CancellationToken token = default);

    // INTERNAL VARIABLES FOR CONTROLS INTEROPERABILITY
    internal abstract bool IsEdgeRoutingEnabled { get; }
    internal abstract bool EnableParallelEdges { get; }
    internal abstract bool EdgeCurvingEnabled { get; }
    internal abstract double EdgeCurvingTolerance { get; }


    /// <summary>
    /// Get controls related to specified control
    /// </summary>
    /// <param name="ctrl">Original control</param>
    /// <param name="resultType">Type of resulting related controls</param>
    /// <param name="edgesType">Optional edge controls type</param>
    public abstract List<IGraphControl> GetRelatedControls(IGraphControl ctrl,
        GraphControlType resultType = GraphControlType.VertexAndEdge, EdgesType edgesType = EdgesType.Out);

    /// <summary>
    /// Get vertex controls related to specified control
    /// </summary>
    /// <param name="ctrl">Original control</param>
    /// <param name="edgesType">Edge types to query for vertices</param>
    public abstract List<IGraphControl> GetRelatedVertexControls(IGraphControl ctrl,
        EdgesType edgesType = EdgesType.All);

    /// <summary>
    /// Get edge controls related to specified control
    /// </summary>
    /// <param name="ctrl">Original control</param>
    /// <param name="edgesType">Edge types to query</param>
    public abstract List<IGraphControl> GetRelatedEdgeControls(IGraphControl ctrl,
        EdgesType edgesType = EdgesType.All);


    /// <summary>
    /// Generates and displays edges for specified vertex
    /// </summary>
    /// <param name="vc">Vertex control</param>
    /// <param name="edgeType">Type of edges to display</param>
    /// <param name="edgesDefaultVisibility">Default edge visibility on layout</param>
    public abstract void GenerateEdgesForVertex(VertexControl vc, EdgesType edgeType,
        bool edgesDefaultVisibility = true);

    #endregion


    #region Measure & Arrange

    /// <summary>
    /// The position of the topLeft corner of the most top-left or top left object if UseNativeObjectArrange == false
    /// vertex.
    /// </summary>
    private Point _topLeft;

    /// <summary>
    /// The position of the bottom right corner of the most or bottom right object if UseNativeObjectArrange == false
    /// bottom-right vertex.
    /// </summary>
    private Point _bottomRight;

    /// <summary>
    /// Gets the size of the GraphArea taking into account positions of the children
    /// This is the main size pointer. Don't use DesiredSize or ActualWidth props as they are simulated.
    /// </summary>
    public Rect ContentSize => new(_topLeft, _bottomRight);

    /// <summary>
    /// Gets or sets additional area space for each side of GraphArea. Useful for zoom adjustments.
    /// 0 by default.
    /// </summary>
    public Size SideExpansionSize { get; set; }

    /// <summary>
    /// Gets or sets if edge route paths must be taken into consideration while determining area size
    /// </summary>
    private const bool COUNT_ROUTE_PATHS = true;

    /// <summary>
    /// Arranges the size of the control.
    /// </summary>
    /// <param name="arrangeSize">The arranged size of the control.</param>
    /// <returns>The size of the control.</returns>
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var minPoint = new Point(double.PositiveInfinity, double.PositiveInfinity);
        var maxPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);

        static bool IsFinite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));

        foreach (Control child in Children)
        {
            var x = GetX(child);
            var y = GetY(child);

            if (double.IsNaN(x) || double.IsNaN(y))
            {
                if (child is EdgeControl ec)
                {
                    x = double.IsNaN(x) ? 0d : x;
                    y = double.IsNaN(y) ? 0d : y;
                }
                else
                {
                    // For vertices with undefined coordinates skip arranging until coordinates are set to avoid invalid rectangles
                    continue;
                }

                if (COUNT_ROUTE_PATHS)
                {
                    var routingInfo = ec.Edge as IRoutingInfo;
                    var rps = routingInfo?.RoutingPoints;
                    if (rps != null)
                    {
                        foreach (var item in rps)
                        {
                            minPoint = new Point(Math.Min(minPoint.X, item.X), Math.Min(minPoint.Y, item.Y));
                            maxPoint = new Point(Math.Max(maxPoint.X, item.X), Math.Max(maxPoint.Y, item.Y));
                        }
                    }
                }
            }
            else
            {
                minPoint = new Point(Math.Min(minPoint.X, x), Math.Min(minPoint.Y, y));
                maxPoint = new Point(Math.Max(maxPoint.X, x), Math.Max(maxPoint.Y, y));
            }

            if (!IsFinite(x)) x = 0d;
            if (!IsFinite(y)) y = 0d;
            var width = child.DesiredSize.Width;
            var height = child.DesiredSize.Height;
            if (!IsFinite(width) || width < 0) width = 0d;
            if (!IsFinite(height) || height < 0) height = 0d;

            child.Arrange(new Rect(x, y, width, height));
        }

        return Design.IsDesignMode ? DesignSize :
            IsInPrintMode ? ContentSize.Size : new Size(10, 10);
    }

    /// <summary>
    /// Overridden measure. It calculates a size where all of
    /// of the vertices are visible.
    /// </summary>
    /// <param name="constraint">The size constraint.</param>
    /// <returns>The calculated size.</returns>
    protected override Size MeasureOverride(Size constraint)
    {
        var oldSize = ContentSize;
        var topLeft = new Measure.Point(double.PositiveInfinity, double.PositiveInfinity);
        var bottomRight = new Measure.Point(double.NegativeInfinity, double.NegativeInfinity);

        foreach (var child in Children)
        {
            //measure the child
            child.Measure(constraint);

            //get the position of the vertex
            var left = GetFinalX(child);
            var top = GetFinalY(child);

            if (!child.IsVisible) continue;

            if (double.IsNaN(left) || double.IsNaN(top))
            {
                if (!COUNT_ROUTE_PATHS || child is not EdgeControl ec) continue;
                if (ec.Edge is not IRoutingInfo routingInfo) continue;
                var rps = routingInfo.RoutingPoints;
                if (rps == null) continue;
                foreach (var item in rps)
                {
                    //get the top left corner point
                    topLeft.X = Math.Min(topLeft.X, item.X);
                    topLeft.Y = Math.Min(topLeft.Y, item.Y);

                    //calculate the bottom right corner point
                    bottomRight.X = Math.Max(bottomRight.X, item.X);
                    bottomRight.Y = Math.Max(bottomRight.Y, item.Y);
                }
            }
            else
            {
                //get the top left corner point
                topLeft.X = Math.Min(topLeft.X, left);
                topLeft.Y = Math.Min(topLeft.Y, top);

                //calculate the bottom right corner point
                bottomRight.X = Math.Max(bottomRight.X, left + child.DesiredSize.Width);
                bottomRight.Y = Math.Max(bottomRight.Y, top + child.DesiredSize.Height);
            }
        }

        topLeft.X -= SideExpansionSize.Width * .5;
        topLeft.Y -= SideExpansionSize.Height * .5;
        bottomRight.X += SideExpansionSize.Width * .5;
        bottomRight.Y += SideExpansionSize.Height * .5;
        _topLeft = topLeft.ToAvalonia();
        _bottomRight = bottomRight.ToAvalonia();
        var newSize = ContentSize;
        if (oldSize != newSize)
            OnContentSizeChanged(oldSize, newSize);
        return Design.IsDesignMode ? DesignSize :
            IsInPrintMode ? ContentSize.Size : new Size(10, 10);
    }

    #endregion
}