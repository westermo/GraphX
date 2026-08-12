using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Records each control's <see cref="Visual.IsVisible"/> value the first time it is seen, so a temporary
/// eligibility-driven visibility change (viewport culling, graph raster caching) can be layered on top of
/// — and later removed without losing — an application/user visibility choice made before or during that
/// temporary state. Shared by <see cref="ViewportCulling"/> and the GraphArea raster cache.
/// </summary>
internal sealed class VisibilityPreservationTracker
{
    private readonly Dictionary<Control, bool> _originalVisibilities = [];

    /// <summary>Whether any controls are currently tracked.</summary>
    public bool IsEmpty => _originalVisibilities.Count == 0;

    /// <summary>Records <paramref name="control"/>'s current visibility if it is not already tracked.</summary>
    public void Track(Control control)
    {
        if (!_originalVisibilities.ContainsKey(control))
            _originalVisibilities.Add(control, control.IsVisible);
    }

    /// <summary>
    /// Tracks <paramref name="control"/> if needed, then applies <paramref name="isEligible"/>: the control
    /// is made visible only if it was originally visible AND is eligible, so an application choice to hide
    /// it is retained regardless of eligibility.
    /// </summary>
    public void Apply(Control control, bool isEligible)
    {
        Track(control);
        var shouldBeVisible = _originalVisibilities[control] && isEligible;
        if (control.IsVisible != shouldBeVisible)
            control.SetCurrentValue(Visual.IsVisibleProperty, shouldBeVisible);
    }

    /// <summary>
    /// Updates the tracked original visibility for an already-tracked control, without touching its actual
    /// current visibility. Used when an application/user visibility change happens while the control's
    /// displayed visibility is being driven by <see cref="Apply"/>, so that value wins once tracking ends.
    /// </summary>
    public void UpdateTrackedVisibility(Control control, bool isVisible)
    {
        if (_originalVisibilities.ContainsKey(control))
            _originalVisibilities[control] = isVisible;
    }

    /// <summary>Gets the original (pre-tracking) visibility recorded for <paramref name="control"/>, if any.</summary>
    public bool TryGetOriginalVisibility(Control control, out bool isVisible) =>
        _originalVisibilities.TryGetValue(control, out isVisible);

    /// <summary>Restores every tracked control to its recorded original visibility and stops tracking it.</summary>
    public void RestoreAll()
    {
        foreach (var (control, isVisible) in _originalVisibilities)
        {
            if (control.IsVisible != isVisible)
                control.SetCurrentValue(Visual.IsVisibleProperty, isVisible);
        }

        _originalVisibilities.Clear();
    }

    /// <summary>Stops tracking every control without changing its current visibility.</summary>
    public void Clear() => _originalVisibilities.Clear();
}
