using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Owns the lifecycle of a <see cref="GraphAreaBase"/>'s graph raster cache: capturing/hiding live source
/// children, tracking their bounds and visibility, subscribing to the changes that must invalidate the
/// cache, and rebuilding/tearing down the <see cref="GraphAreaRasterCacheLayer"/>. Extracted from
/// <see cref="GraphAreaBase"/> so that class only exposes thin forwarding members for its public API.
/// </summary>
internal sealed class GraphAreaRasterCacheController
{
    private const long DefaultMaximumGraphRenderCacheBytes = 64L * 1024 * 1024;

    private readonly GraphAreaBase _graphArea;
    private readonly Dictionary<Control, Rect> _sourceBounds = [];
    private readonly VisibilityPreservationTracker _visibility = new();
    private readonly VisualDescendantSubscriptionTracker _sourceVisualTracker;

    private GraphAreaRasterCacheLayer? _layer;
    private bool _requested;
    private bool _suppressInvalidation;
    private bool _viewportCullingWasEnabled;
    private TopLevel? _cacheTopLevel;
    private long _maximumBytes = DefaultMaximumGraphRenderCacheBytes;

    public GraphAreaRasterCacheController(GraphAreaBase graphArea)
    {
        _graphArea = graphArea;
        _sourceVisualTracker = new VisualDescendantSubscriptionTracker(SourcePropertyChanged);
        _graphArea.LayoutUpdated += RequestedLayoutUpdated;
    }

    public bool IsRequested => _requested;

    public bool IsActive => _layer?.HasBitmap == true;

    public GraphAreaRasterCacheLayer? Layer => _layer;

    public int RasterizationCount => _layer?.RasterizationCount ?? 0;

    public Rect ContentBounds => _layer?.ContentBounds ?? default;

    public RenderTargetBitmap? Bitmap => _layer?.Bitmap;

    public long MaximumBytes
    {
        get => _maximumBytes;
        set
        {
            if (_maximumBytes == value) return;
            _maximumBytes = value;
            Invalidate();
        }
    }

    public bool WasVisibleBeforeCaching(Control child) =>
        _visibility.TryGetOriginalVisibility(child, out var isVisible) && isVisible;

    /// <summary>Records the viewport-culling state a caller wants applied once live rendering resumes.</summary>
    public void SetViewportCullingAfterCachedRendering(bool isEnabled) => _viewportCullingWasEnabled = isEnabled;

    public bool Begin()
    {
        if (IsActive) return true;

        _requested = true;
        _viewportCullingWasEnabled = _graphArea.EnableViewportCulling;
        if (_viewportCullingWasEnabled)
            _graphArea.EnableViewportCulling = false;

        var sourceChildren = new List<Control>(_graphArea.Children.Count);
        foreach (var child in _graphArea.Children)
        {
            if (child is not GraphAreaRasterCacheLayer)
                sourceChildren.Add(child);
        }

        var cacheLayer = new GraphAreaRasterCacheLayer(_graphArea);
        try
        {
            if (!cacheLayer.TryRasterize(
                    sourceChildren,
                    _graphArea.ContentSize,
                    GetScaling(),
                    MaximumBytes))
            {
                cacheLayer.Dispose();
                RestoreViewportCulling();
                // A declarative cache request may arrive before the first measure pass. Keep it pending
                // until a valid content extent is available; size-cap failures still fall back to live.
                _requested = _graphArea.ContentSize is not { Width: > 0, Height: > 0 };
                return false;
            }

            _layer = cacheLayer;
            _suppressInvalidation = true;
            try
            {
                foreach (var child in sourceChildren)
                    _sourceBounds.Add(child, GetSourceBounds(child));

                _graphArea.Children.Insert(0, cacheLayer);
                SubscribeSources(sourceChildren);
                SubscribeScaling();

                foreach (var child in sourceChildren)
                    _visibility.Apply(child, isEligible: false);
            }
            finally
            {
                _suppressInvalidation = false;
            }

            _graphArea.InvalidateVisual();
            return true;
        }
        catch
        {
            if (_layer != null)
                Stop();
            else
            {
                cacheLayer.Dispose();
                RestoreViewportCulling();
            }

            _requested = false;
            throw;
        }
    }

    public void End()
    {
        _requested = false;
        Stop();
    }

    public void Invalidate()
    {
        _requested = false;
        Stop();
    }

    public void OnAttachedToVisualTree()
    {
        if (IsActive)
        {
            // A cache built while detached used the 1x fallback scale. Rebuild against the actual
            // top-level scale before continuing to use it.
            Invalidate();
            Begin();
        }
        else if (_requested)
        {
            Begin();
        }
    }

    public void OnDetachedFromVisualTree()
    {
        End();
        UnsubscribeScaling();
    }

    private void Stop()
    {
        if (_layer == null)
        {
            RestoreViewportCulling();
            return;
        }

        _suppressInvalidation = true;
        try
        {
            UnsubscribeSources();
            UnsubscribeScaling();
            _graphArea.Children.Remove(_layer);
            _layer.Dispose();
            _layer = null;

            _visibility.RestoreAll();
            _sourceBounds.Clear();
            RestoreViewportCulling();
        }
        finally
        {
            _suppressInvalidation = false;
        }

        _graphArea.InvalidateMeasure();
        _graphArea.InvalidateVisual();
    }

    private void RestoreViewportCulling()
    {
        if (!_viewportCullingWasEnabled) return;
        _viewportCullingWasEnabled = false;
        _graphArea.EnableViewportCulling = true;
    }

    private void SubscribeSources(IReadOnlyCollection<Control> sourceChildren)
    {
        _graphArea.Children.CollectionChanged += ChildrenChanged;
        _graphArea.LayoutUpdated += LayoutUpdatedWhileActive;
        foreach (var child in sourceChildren)
            _sourceVisualTracker.Track(child);
    }

    private void UnsubscribeSources()
    {
        _graphArea.Children.CollectionChanged -= ChildrenChanged;
        _graphArea.LayoutUpdated -= LayoutUpdatedWhileActive;
        _sourceVisualTracker.UnsubscribeAll();
    }

    private void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressInvalidation)
            Invalidate();
    }

    private void SourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_suppressInvalidation) return;
        if (sender is Control child &&
            e.Property == Visual.IsVisibleProperty &&
            e.NewValue is bool isVisible)
        {
            // Preserve an explicit visibility request made while the cache owns the rendered scene
            // before restoring the live graph.
            _visibility.UpdateTrackedVisibility(child, isVisible);
        }

        Invalidate();
    }

    private void LayoutUpdatedWhileActive(object? sender, EventArgs e)
    {
        if (_suppressInvalidation || !IsActive) return;
        if (ContentBounds != _graphArea.ContentSize)
        {
            Invalidate();
            return;
        }

        foreach (var (child, cachedBounds) in _sourceBounds)
        {
            if (!_graphArea.Children.Contains(child) || GetSourceBounds(child) != cachedBounds)
            {
                Invalidate();
                return;
            }
        }
    }

    private void RequestedLayoutUpdated(object? sender, EventArgs e)
    {
        if (_requested && !IsActive)
            Begin();
    }

    private Rect GetSourceBounds(Control child)
    {
        var origin = child.TranslatePoint(default, _graphArea) ?? default;
        return new Rect(origin, child.Bounds.Size);
    }

    private double GetScaling() => RasterCacheGeometry.GetEffectiveRenderScaling(_cacheTopLevel, _graphArea);

    private void SubscribeScaling()
    {
        var topLevel = TopLevel.GetTopLevel(_graphArea);
        if (ReferenceEquals(_cacheTopLevel, topLevel)) return;
        UnsubscribeScaling();
        _cacheTopLevel = topLevel;
        if (_cacheTopLevel != null)
            _cacheTopLevel.ScalingChanged += ScalingChanged;
    }

    private void UnsubscribeScaling()
    {
        if (_cacheTopLevel != null)
            _cacheTopLevel.ScalingChanged -= ScalingChanged;
        _cacheTopLevel = null;
    }

    private void ScalingChanged(object? sender, EventArgs e) => Invalidate();
}
