using System;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Provides level-of-detail (LOD) rendering settings for optimizing large graph display.
/// At lower zoom levels, rendering is simplified to improve performance.
/// </summary>
public sealed class LevelOfDetailSettings
{
    /// <summary>
    /// Gets or sets whether level-of-detail rendering is enabled.
    /// When enabled, visual complexity is reduced at lower zoom levels.
    /// Default is true.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the zoom level below which edge arrows are hidden.
    /// Default is 0.3 (30% zoom).
    /// </summary>
    public double HideArrowsZoomThreshold { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets the zoom level below which edge labels are hidden.
    /// Default is 0.5 (50% zoom).
    /// </summary>
    public double HideEdgeLabelsZoomThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the zoom level below which vertex labels are hidden.
    /// Default is 0.4 (40% zoom).
    /// </summary>
    public double HideVertexLabelsZoomThreshold { get; set; } = 0.4;

    /// <summary>
    /// Gets or sets the zoom level below which edges use simplified (straight line) geometry.
    /// Default is 0.2 (20% zoom).
    /// </summary>
    public double SimplifyEdgesZoomThreshold { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets the zoom level below which curved edges become straight lines.
    /// Default is 0.25 (25% zoom).
    /// </summary>
    public double DisableCurvingZoomThreshold { get; set; } = 0.25;

    /// <summary>
    /// Determines whether arrows should be shown at the current zoom level.
    /// </summary>
    public bool ShouldShowArrows(double zoomLevel)
    {
        if (!IsEnabled) return true;
        return zoomLevel >= HideArrowsZoomThreshold;
    }

    /// <summary>
    /// Determines whether edge labels should be shown at the current zoom level.
    /// </summary>
    public bool ShouldShowEdgeLabels(double zoomLevel)
    {
        if (!IsEnabled) return true;
        return zoomLevel >= HideEdgeLabelsZoomThreshold;
    }

    /// <summary>
    /// Determines whether vertex labels should be shown at the current zoom level.
    /// </summary>
    public bool ShouldShowVertexLabels(double zoomLevel)
    {
        if (!IsEnabled) return true;
        return zoomLevel >= HideVertexLabelsZoomThreshold;
    }

    /// <summary>
    /// Determines whether edges should use simplified geometry at the current zoom level.
    /// </summary>
    public bool ShouldSimplifyEdges(double zoomLevel)
    {
        if (!IsEnabled) return false;
        return zoomLevel < SimplifyEdgesZoomThreshold;
    }

    /// <summary>
    /// Determines whether edge curving should be disabled at the current zoom level.
    /// </summary>
    public bool ShouldDisableCurving(double zoomLevel)
    {
        if (!IsEnabled) return false;
        return zoomLevel < DisableCurvingZoomThreshold;
    }
}
