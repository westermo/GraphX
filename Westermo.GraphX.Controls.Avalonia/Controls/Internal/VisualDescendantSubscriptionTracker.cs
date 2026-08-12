using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.VisualTree;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Tracks <see cref="AvaloniaPropertyChangedEventArgs"/> subscriptions on a visual and its descendants so
/// callers can cheaply refresh them (e.g. after a layout pass adds/removes descendants) without leaking
/// handlers on visuals that are no longer part of the tracked subtree. Used by the Wayfinder minimap cache
/// to notice rendering-affecting property changes anywhere under its tracked content visual, and by the
/// GraphArea raster cache to do the same for its captured source children.
/// </summary>
internal sealed class VisualDescendantSubscriptionTracker(EventHandler<AvaloniaPropertyChangedEventArgs> onPropertyChanged)
{
    private readonly HashSet<Visual> _subscribed = [];

    /// <summary>The number of subscribe/unsubscribe operations performed by this tracker.</summary>
    internal int SubscriptionChangeCount { get; private set; }

    /// <summary>Subscribes to <paramref name="root"/> and every current visual descendant of it.</summary>
    public void Track(Visual root)
    {
        Subscribe(root);
        foreach (var descendant in root.GetVisualDescendants())
            Subscribe(descendant);
    }

    /// <summary>
    /// Reconciles subscriptions with <paramref name="root"/>'s current subtree. Existing descendants retain
    /// their handlers; only visuals added to or removed from the subtree are subscribed or unsubscribed.
    /// </summary>
    public void Refresh(Visual root)
    {
        var currentVisuals = new HashSet<Visual> { root };
        foreach (var descendant in root.GetVisualDescendants())
            currentVisuals.Add(descendant);

        foreach (var visual in currentVisuals)
            Subscribe(visual);

        foreach (var visual in _subscribed.Where(visual => !currentVisuals.Contains(visual)).ToArray())
            Unsubscribe(visual);
    }

    /// <summary>Unsubscribes every visual currently tracked and clears the tracked set.</summary>
    public void UnsubscribeAll()
    {
        foreach (var visual in _subscribed.ToArray())
            Unsubscribe(visual);
        _subscribed.Clear();
    }

    private void Subscribe(Visual visual)
    {
        if (_subscribed.Add(visual))
        {
            visual.PropertyChanged += onPropertyChanged;
            SubscriptionChangeCount++;
        }
    }

    private void Unsubscribe(Visual visual)
    {
        if (!_subscribed.Remove(visual)) return;
        visual.PropertyChanged -= onPropertyChanged;
        SubscriptionChangeCount++;
    }
}
