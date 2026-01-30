using System;
using Avalonia;
using Avalonia.Controls;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Provides viewport-based culling support for GraphArea controls.
/// When enabled, controls outside the visible viewport are hidden to improve rendering performance
/// for large graphs (1000+ nodes).
/// </summary>
public sealed class ViewportCulling : IDisposable
{
    private readonly GraphAreaBase _graphArea;
    private Rect _currentViewport;
    private bool _isEnabled;
    private bool _isDisposed;
    private double _cullingMargin = 100; // Extra margin around viewport to avoid popping

    /// <summary>
    /// Gets or sets whether viewport culling is enabled.
    /// When enabled, controls outside the visible viewport will have IsVisible set to false.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            if (_isEnabled)
                UpdateCulling();
            else
                ShowAllControls();
        }
    }

    /// <summary>
    /// Gets or sets the margin around the viewport where controls remain visible.
    /// This prevents controls from popping in/out during scrolling. Default is 100 pixels.
    /// </summary>
    public double CullingMargin
    {
        get => _cullingMargin;
        set
        {
            if (Math.Abs(_cullingMargin - value) < 0.001) return;
            _cullingMargin = Math.Max(0, value);
            if (_isEnabled)
                UpdateCulling();
        }
    }

    /// <summary>
    /// Gets the current viewport rectangle in graph coordinates.
    /// </summary>
    public Rect CurrentViewport => _currentViewport;

    public ViewportCulling(GraphAreaBase graphArea)
    {
        _graphArea = graphArea ?? throw new ArgumentNullException(nameof(graphArea));
    }

    /// <summary>
    /// Updates the viewport and recalculates visibility for all controls.
    /// Call this method when the viewport changes (zoom, pan, resize).
    /// </summary>
    /// <param name="viewport">The new viewport rectangle in graph coordinates.</param>
    public void UpdateViewport(Rect viewport)
    {
        _currentViewport = viewport;
        if (_isEnabled)
            UpdateCulling();
    }

    /// <summary>
    /// Performs visibility culling for all controls based on the current viewport.
    /// </summary>
    public void UpdateCulling()
    {
        if (_isDisposed || !_isEnabled) return;
        if (_currentViewport.Width <= 0 || _currentViewport.Height <= 0) return;

        // Expand viewport by margin; ensure margin is non-negative even if field is modified directly
        var margin = _cullingMargin < 0 ? 0 : _cullingMargin;
        var expandedViewport = _currentViewport.Inflate(margin);

        foreach (var child in _graphArea.Children)
        {
            if (child is VertexControlBase vertex)
            {
                UpdateVertexVisibility(vertex, expandedViewport);
            }
            else if (child is EdgeControlBase edge)
            {
                UpdateEdgeVisibility(edge, expandedViewport);
            }
            // Labels and other controls are handled by their parent controls
        }
    }

    private void UpdateVertexVisibility(VertexControlBase vertex, Rect viewport)
    {
        var x = GraphAreaBase.GetX(vertex);
        var y = GraphAreaBase.GetY(vertex);
        
        if (double.IsNaN(x) || double.IsNaN(y))
        {
            // Position not set yet, keep visible
            return;
        }

        var vertexBounds = new Rect(x, y, 
            Math.Max(vertex.Bounds.Width, 1), 
            Math.Max(vertex.Bounds.Height, 1));

        var shouldBeVisible = viewport.Intersects(vertexBounds);
        
        if (vertex.IsVisible != shouldBeVisible)
        {
            vertex.SetCurrentValue(Visual.IsVisibleProperty, shouldBeVisible);
            // Also update associated label if any
            if (vertex.VertexLabelControl is Control label)
            {
                label.SetCurrentValue(Visual.IsVisibleProperty, shouldBeVisible && vertex.ShowLabel);
            }
        }
    }

    private void UpdateEdgeVisibility(EdgeControlBase edge, Rect viewport)
    {
        // For edges, check if either endpoint or the edge bounds are in viewport
        var source = edge.Source;
        var target = edge.Target;

        if (source == null || target == null)
        {
            return;
        }

        // Quick check: if both vertices are visible, edge should be visible
        if (source.IsVisible || target.IsVisible)
        {
            if (!edge.IsVisible)
                edge.SetCurrentValue(Visual.IsVisibleProperty, true);
            return;
        }

        // If both vertices are hidden but edge might cross viewport, check edge bounds
        var edgeBounds = edge.GeometryBounds;
        if (edgeBounds.HasValue)
        {
            var bounds = edgeBounds.Value;
            var edgePosition = edge.GetPosition();
            var worldBounds = new Rect(
                edgePosition.X + bounds.X, 
                edgePosition.Y + bounds.Y,
                bounds.Width, 
                bounds.Height);

            var shouldBeVisible = viewport.Intersects(worldBounds);
            if (edge.IsVisible != shouldBeVisible)
            {
                edge.SetCurrentValue(Visual.IsVisibleProperty, shouldBeVisible);
            }
        }
        else
        {
            // No geometry yet, hide if vertices are hidden
            if (edge.IsVisible)
                edge.SetCurrentValue(Visual.IsVisibleProperty, false);
        }
    }

    /// <summary>
    /// Shows all controls regardless of viewport position.
    /// Called when culling is disabled.
    /// </summary>
    private void ShowAllControls()
    {
        foreach (var child in _graphArea.Children)
        {
            if (child is VertexControlBase vertex)
            {
                if (!vertex.IsVisible)
                    vertex.SetCurrentValue(Visual.IsVisibleProperty, true);
            }
            else if (child is EdgeControlBase edge)
            {
                if (!edge.IsVisible)
                    edge.SetCurrentValue(Visual.IsVisibleProperty, true);
            }
        }
    }

    /// <summary>
    /// Checks if a point is within the current viewport (with margin).
    /// </summary>
    public bool IsInViewport(Point point)
    {
        if (!_isEnabled) return true;
        return _currentViewport.Inflate(_cullingMargin).Contains(point);
    }

    /// <summary>
    /// Checks if a rectangle intersects the current viewport (with margin).
    /// </summary>
    public bool IntersectsViewport(Rect bounds)
    {
        if (!_isEnabled) return true;
        return _currentViewport.Inflate(_cullingMargin).Intersects(bounds);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        ShowAllControls();
    }
}
