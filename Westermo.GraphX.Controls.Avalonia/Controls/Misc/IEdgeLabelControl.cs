using System;
using Avalonia;

namespace Westermo.GraphX.Controls.Controls.Misc;

public interface IEdgeLabelControl : IDisposable
{
    /// <summary>
    /// Gets or sets if label should be aligned with the edge
    /// </summary>
    bool AlignToEdge { get; set; }

    /// <summary>
    /// Gets or sets if label is visible
    /// </summary>
    bool ShowLabel { get; set; }

    /// <summary>
    /// Gets or sets label vertical offset
    /// </summary>
    double LabelVerticalOffset { get; set; }

    /// <summary>
    /// Gets or sets label horizontal offset
    /// </summary>
    double LabelHorizontalOffset { get; set; }

    /// <summary>
    /// Gets or sets label drawing angle in degrees
    /// </summary>
    double Angle { get; set; }

    /// <summary>
    /// Gets or sets if label should be visible for self looped edge. Default value is false.
    /// </summary>
    bool DisplayForSelfLoopedEdges { get; }

    /// <summary>
    /// Gets or sets if label should flip on rotation when axis changes. Default value is true.
    /// </summary>
    bool FlipOnRotation { get; }

    void Show();
    void Hide();
}