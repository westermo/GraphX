using System;

namespace Westermo.GraphX.Controls.Controls;

/// <summary>
/// Provides a scope for batching multiple edge updates into a single layout pass.
/// Use this when updating many edges at once to avoid redundant layout calculations.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// using (graphArea.BeginBatchUpdate())
/// {
///     // Update multiple edges here
///     foreach (var edge in edges)
///     {
///         edge.UpdateEdge();
///     }
/// }
/// // Single layout pass occurs when scope is disposed
/// </code>
/// </remarks>
public sealed class BatchUpdateScope : IDisposable
{
    private readonly GraphAreaBase _graphArea;
    private readonly EdgeControlBase[] _edges;
    private bool _isDisposed;

    internal BatchUpdateScope(GraphAreaBase graphArea, EdgeControlBase[] edges)
    {
        _graphArea = graphArea;
        _edges = edges;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Trigger a single layout pass
        _graphArea.InvalidateMeasure();
    }
}

/// <summary>
/// Provides a scope for deferring position change events during batch vertex moves.
/// Use this when moving many vertices at once to avoid redundant edge updates.
/// </summary>
public sealed class DeferredPositionUpdateScope : IDisposable
{
    private readonly VertexControlBase[] _vertices;
    private readonly bool[] _originalUpdateEdges;
    private bool _isDisposed;
    private readonly Action? _onDispose;

    internal DeferredPositionUpdateScope(VertexControlBase[] vertices, Action? onDispose = null)
    {
        _vertices = vertices;
        _originalUpdateEdges = new bool[vertices.Length];
        _onDispose = onDispose;

        // Store original UpdateEdgesOnMove state and disable during batch
        for (var i = 0; i < _vertices.Length; i++)
        {
            _originalUpdateEdges[i] = Behaviours.DragBehaviour.GetUpdateEdgesOnMove(_vertices[i]);
            Behaviours.DragBehaviour.SetUpdateEdgesOnMove(_vertices[i], false);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Restore original UpdateEdgesOnMove state
        for (var i = 0; i < _vertices.Length; i++)
        {
            Behaviours.DragBehaviour.SetUpdateEdgesOnMove(_vertices[i], _originalUpdateEdges[i]);
        }

        // Execute completion action (typically triggers edge update)
        _onDispose?.Invoke();
    }
}