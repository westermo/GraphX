using Avalonia;
using Avalonia.Media;

namespace Westermo.GraphX.Controls.Controls.Misc;

/// <summary>
/// The narrow set of information a shared rendering layer (<see cref="BatchedEdgeLayer"/>) needs to draw
/// one edge's geometry, obtained via <see cref="EdgeControlBase.TryGetBatchedRenderInfo"/>. Keeping this
/// as a single value type means the layer no longer needs to downcast to <see cref="EdgeControl"/> or call
/// multiple separate accessors to read an edge's foreground/stroke.
/// </summary>
internal readonly record struct BatchedEdgeRenderInfo(
    Geometry Geometry,
    Point Position,
    double Opacity,
    IBrush Foreground,
    double StrokeThickness);
