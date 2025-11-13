using System;
using System.Collections.Generic;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Common.Models;

public sealed class GraphState<TVertex, TEdge, TGraph>(
    string id,
    TGraph graph,
    IAlgorithmStorage<TVertex, TEdge> storage,
    Dictionary<TVertex, Point> vPos,
    List<TEdge> vEdges,
    string description = "")
    : IDisposable
{
    /// <summary>
    /// Graph state unique identificator
    /// </summary>
    public string ID { get; private set; } = id;

    /// <summary>
    /// State description
    /// </summary>
    public string Description { get; private set; } = description;

    /// <summary>
    /// Saved data graph
    /// </summary>
    public TGraph Graph { get; private set; } = graph;

    /// <summary>
    /// Saved vertex positions
    /// </summary>
    public Dictionary<TVertex, Point> VertexPositions { get; private set; } = vPos;

    /// <summary>
    /// Saved visible edges with route points
    /// </summary>
    public List<TEdge> VisibleEdges { get; private set; } = vEdges;

    /// <summary>
    /// Saved algorithm storage
    /// </summary>
    public IAlgorithmStorage<TVertex, TEdge> AlgorithmStorage { get; private set; } = storage;

    public void Dispose()
    {
        Graph = default(TGraph);
        VertexPositions.Clear();
        VisibleEdges.Clear();
        AlgorithmStorage = null;
    }
}