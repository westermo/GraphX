namespace Westermo.GraphX.Controls.Controls.Misc;

/// <summary>
/// Defines how automatically generated edge paths are rendered.
/// </summary>
public enum EdgeRenderingMode
{
    /// <summary>
    /// Each edge renders through its own templated <see cref="EdgeControl"/>.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Eligible default edge paths are rendered by a shared graph-area layer.
    /// Edges that need individual visuals continue to use their templates.
    /// </summary>
    Batched
}
