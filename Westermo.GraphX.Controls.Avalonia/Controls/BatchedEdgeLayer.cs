using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Westermo.GraphX.Controls.Controls.Interfaces;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Renders the geometries of compatible graph edges in one visual pass.
/// </summary>
public sealed class BatchedEdgeLayer : Control
{
    private readonly GraphAreaBase _graphArea;

    // Every edge this layer currently knows about (added via SeedFromCurrentChildren/Register, removed via
    // Unregister/Forget or a full SynchronizeAllEdges). Refresh(edge) can update a single edge's
    // eligibility in O(1) only when it is already a member of this set; otherwise it falls back to a full
    // resync so edges added/removed outside of GraphArea's registration calls are still picked up.
    private readonly HashSet<EdgeControlBase> _registeredEdges = [];

    // The eligible subset of _registeredEdges. Keep the list in GraphArea child-registration order so batching
    // retains the deterministic z-order of the original individual edge controls.
    private readonly List<EdgeControlBase> _batchedEdges = [];
    private readonly HashSet<EdgeControlBase> _batchedEdgeSet = [];
    private readonly Dictionary<EdgeControlBase, CachedPen> _pens = [];
    private bool _invalidationPending;
    private bool _isSynchronizing;

    internal BatchedEdgeLayer(GraphAreaBase graphArea)
    {
        _graphArea = graphArea;
        IsHitTestVisible = false;
    }

    internal bool IsSynchronizing => _isSynchronizing;

    internal int RefreshCount { get; private set; }

    /// <summary>
    /// Registers every edge currently in the GraphArea. Called once when the layer is created so edges
    /// added before batched rendering was enabled are picked up without a per-update rescan.
    /// </summary>
    internal void SeedFromCurrentChildren()
    {
        _isSynchronizing = true;
        try
        {
            foreach (var edge in _graphArea.Children.OfType<EdgeControlBase>())
                RegisterCore(edge);
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    /// <summary>Registers a single edge added to the GraphArea while this layer is active.</summary>
    internal void Register(EdgeControlBase edge)
    {
        _isSynchronizing = true;
        try
        {
            RegisterCore(edge);
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    internal void Refresh(EdgeControlBase? changedEdge = null)
    {
        RefreshCount++;
        _isSynchronizing = true;
        try
        {
            if (changedEdge != null && _registeredEdges.Contains(changedEdge))
                UpdateEligibility(changedEdge);
            else
                SynchronizeAllEdges();
        }
        finally
        {
            _isSynchronizing = false;
        }

        if (_invalidationPending) return;
        _invalidationPending = true;
        InvalidateVisual();
    }

    internal void RestoreEdges()
    {
        _isSynchronizing = true;
        try
        {
            foreach (var edge in _batchedEdges)
                edge.SetBatchedPathSuppressed(false);

            _batchedEdges.Clear();
            _batchedEdgeSet.Clear();
            _registeredEdges.Clear();
            _pens.Clear();
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    internal void Forget(EdgeControlBase edge)
    {
        _isSynchronizing = true;
        try
        {
            _registeredEdges.Remove(edge);
            RestoreEdge(edge);
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    public override void Render(DrawingContext context)
    {
        _invalidationPending = false;
        base.Render(context);

        foreach (var edge in _batchedEdges)
        {
            if (!edge.TryGetBatchedRenderInfo(out var info)) continue;

            using (context.PushTransform(Matrix.CreateTranslation(info.Position.X, info.Position.Y)))
            {
                using (context.PushOpacity(info.Opacity))
                {
                    context.DrawGeometry(null, GetPen(edge, info), info.Geometry);
                }
            }
        }
    }

    private void RegisterCore(EdgeControlBase edge)
    {
        _registeredEdges.Add(edge);
        UpdateEligibility(edge);
    }

    private void UpdateEligibility(EdgeControlBase edge)
    {
        if (edge.CanRenderInBatchedLayer)
        {
            if (_batchedEdgeSet.Add(edge))
            {
                _batchedEdges.Add(edge);
                edge.SetBatchedPathSuppressed(true);
            }
        }
        else
        {
            RestoreEdge(edge);
        }
    }

    private void SynchronizeAllEdges()
    {
        var edges = _graphArea.Children.OfType<EdgeControlBase>().ToArray();
        _registeredEdges.Clear();
        foreach (var edge in edges)
        {
            _registeredEdges.Add(edge);
            UpdateEligibility(edge);
        }

        foreach (var edge in _batchedEdges.Except(edges).ToArray())
        {
            RestoreEdge(edge);
        }
    }

    private void RestoreEdge(EdgeControlBase edge)
    {
        if (!_batchedEdgeSet.Remove(edge)) return;
        _batchedEdges.Remove(edge);
        _pens.Remove(edge);
        edge.SetBatchedPathSuppressed(false);
    }

    private IPen GetPen(EdgeControlBase edge, BatchedEdgeRenderInfo info)
    {
        if (_pens.TryGetValue(edge, out var cached) &&
            ReferenceEquals(cached.Foreground, info.Foreground) &&
            cached.StrokeThickness == info.StrokeThickness)
            return cached.Pen;

        var pen = new Pen(info.Foreground, info.StrokeThickness);
        _pens[edge] = new CachedPen(info.Foreground, info.StrokeThickness, pen);
        return pen;
    }

    private readonly record struct CachedPen(IBrush Foreground, double StrokeThickness, IPen Pen);
}
