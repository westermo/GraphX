using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Owns the lifecycle of a <see cref="GraphAreaBase"/>'s shared batched-edge rendering layer: creating and
/// removing the <see cref="BatchedEdgeLayer"/> as <see cref="GraphAreaBase.EdgeRenderingMode"/> changes,
/// forwarding edge registration, and coalescing invalidations while a <see cref="BatchUpdateScope"/> is
/// active. Extracted from <see cref="GraphAreaBase"/> so that class only exposes thin forwarding members
/// for its internal API.
/// </summary>
internal sealed class BatchedEdgeRenderController(GraphAreaBase graphArea)
{
    private int _invalidationDeferral;
    private bool _invalidationPending;
    private readonly HashSet<EdgeControlBase> _subscribedEdges = [];

    public BatchedEdgeLayer? Layer { get; private set; }

    public void OnModeChanged(EdgeRenderingMode newMode)
    {
        if (newMode == EdgeRenderingMode.Batched)
            Enable();
        else
            Disable();
    }

    public void Register(EdgeControlBase edge)
    {
        if (graphArea.EdgeRenderingMode != EdgeRenderingMode.Batched) return;
        Subscribe(edge);
        Layer?.Register(edge);
        NotifyChanged(edge);
    }

    public void Unregister(EdgeControlBase edge)
    {
        Unsubscribe(edge);
        Layer?.Forget(edge);
    }

    public void NotifyChanged(EdgeControlBase? edge = null)
    {
        if (graphArea.EdgeRenderingMode != EdgeRenderingMode.Batched) return;
        if (_invalidationDeferral > 0)
        {
            _invalidationPending = true;
            return;
        }

        Layer?.Refresh(edge);
    }

    public void BeginInvalidationDeferral() => _invalidationDeferral++;

    public void EndInvalidationDeferral()
    {
        if (_invalidationDeferral == 0) return;
        _invalidationDeferral--;
        if (_invalidationDeferral != 0 || !_invalidationPending) return;
        _invalidationPending = false;
        Layer?.Refresh();
    }

    /// <summary>Restores the shared edge-layer invariant after GraphArea clears its visual children.</summary>
    public void RecreateAfterChildrenClear()
    {
        foreach (var edge in _subscribedEdges.ToArray())
            Unsubscribe(edge);

        Layer = null;
        if (graphArea.EdgeRenderingMode == EdgeRenderingMode.Batched)
            Enable();
    }

    private void OnEdgePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not EdgeControlBase edge || Layer?.IsSynchronizing == true) return;
        NotifyChanged(edge);
    }

    private void Enable()
    {
        if (Layer is null)
        {
            var layer = new BatchedEdgeLayer(graphArea);
            Layer = layer;
            GraphAreaBase.SetX(layer, 0);
            GraphAreaBase.SetY(layer, 0);
            graphArea.Children.Insert(0, layer);
            layer.SeedFromCurrentChildren();
            foreach (var edge in graphArea.Children.OfType<EdgeControlBase>())
                Subscribe(edge);
        }

        NotifyChanged();
    }

    private void Disable()
    {
        if (Layer is null) return;
        foreach (var edge in _subscribedEdges.ToArray())
            Unsubscribe(edge);
        Layer.RestoreEdges();
        graphArea.Children.Remove(Layer);
        Layer = null;
    }

    private void Subscribe(EdgeControlBase edge)
    {
        if (_subscribedEdges.Add(edge))
            edge.PropertyChanged += OnEdgePropertyChanged;
    }

    private void Unsubscribe(EdgeControlBase edge)
    {
        if (_subscribedEdges.Remove(edge))
            edge.PropertyChanged -= OnEdgePropertyChanged;
    }
}
