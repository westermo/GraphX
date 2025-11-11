using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Interfaces;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public partial class EfficientSugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph>(
    TGraph visitedGraph,
    EfficientSugiyamaLayoutParameters parameters,
    IDictionary<TVertex, Point> vertexPositions,
    IDictionary<TVertex, Size> vertexSizes)
    : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, EfficientSugiyamaLayoutParameters>(
            visitedGraph, vertexPositions, parameters),
        ILayoutEdgeRouting<TEdge>
    where TVertex : class
    where TEdge : IEdge<TVertex>
    where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>, IMutableVertexAndEdgeSet<TVertex, TEdge>
{
    /// <summary>
    /// The copy of the VisitedGraph which should be laid out.
    /// </summary>
    private IMutableBidirectionalGraph<SugiVertex, SugiEdge> _graph;

    /// <summary>
    /// Routing points for the edges of the original graph.
    /// </summary>
    private readonly IDictionary<TEdge, Point[]> _edgeRoutingPoints =
        new Dictionary<TEdge, Point[]>();

    private readonly IDictionary<TEdge, IList<SugiVertex>> _dummyVerticesOfEdges =
        new Dictionary<TEdge, IList<SugiVertex>>();

    private readonly IDictionary<TVertex, SugiVertex> _vertexMap = 
        new Dictionary<TVertex, SugiVertex>();

    /// <summary>
    /// Isolated vertices in the visited graph, which will be handled only in
    /// the last step of the layout.
    /// </summary>
    private List<SugiVertex> _isolatedVertices;

    /// <summary>
    /// Edges that has been involved in cycles in the original graph. (These has
    /// been reverted during this layout algorithm).
    /// </summary>
    private readonly IList<TEdge> _cycleEdges = [];

    /// <summary>
    /// It stores the vertices or segments which inside the layers.
    /// </summary>
    private readonly IList<IList<SugiVertex>> _layers =
        [];

    public EfficientSugiyamaLayoutAlgorithm(
        TGraph visitedGraph, 
        EfficientSugiyamaLayoutParameters parameters, 
        IDictionary<TVertex, Size> vertexSizes)
        : this(visitedGraph, parameters, null, vertexSizes)
    { }

    /// <summary>
    /// Initializes the private _graph field which stores the graph that 
    /// we operate on.
    /// </summary>
    private void InitTheGraph()
    {
        //make a copy of the original graph
        _graph = new BidirectionalGraph<SugiVertex, SugiEdge>();

        //copy the vertices
        foreach (var vertex in VisitedGraph.Vertices)
        {
            var size = new Size();
            if (vertexSizes != null)
                vertexSizes.TryGetValue(vertex, out size);

            var vertexWrapper = new SugiVertex(vertex, size);
            _graph.AddVertex(vertexWrapper);
            _vertexMap[vertex] = vertexWrapper;
        }

        //copy the edges
        foreach (var edge in VisitedGraph.Edges)
        {
            var edgeWrapper = new SugiEdge(edge, _vertexMap[edge.Source], _vertexMap[edge.Target]);
            _graph.AddEdge(edgeWrapper);
        }
    }

    public override void Compute(CancellationToken cancellationToken)
    {
        InitTheGraph();

        //first step
        DoPreparing();

        BuildSparseNormalizedGraph(cancellationToken);
        DoCrossingMinimizations(cancellationToken);
        CalculatePositions();
        var offsetY = 0d;
        if (Parameters.Direction == LayoutDirection.LeftToRight)
        {
            offsetY = VertexPositions.Values.Min(p => p.X);
            if (offsetY < 0) offsetY = -offsetY;
            foreach (var item in VertexPositions.ToDictionary(a=> a.Key, b=> b.Value))
            {
                VertexPositions[item.Key] = new Point(item.Value.Y * 1.5 + 0, item.Value.X + offsetY);
            }
        }

        if(Parameters.Direction == LayoutDirection.RightToLeft)
        {
            offsetY = VertexPositions.Values.Min(p => p.X);
            if (offsetY < 0) offsetY = -offsetY;
            foreach (var item in VertexPositions.ToDictionary(a => a.Key, b => b.Value))
            {
                VertexPositions[item.Key] = new Point(-item.Value.Y * 1.5, -item.Value.X + offsetY);
            }
        }

        if (Parameters.Direction == LayoutDirection.BottomToTop)
        {
            foreach (var item in VertexPositions.ToDictionary(a => a.Key, b => b.Value))
            {
                VertexPositions[item.Key] = new Point(item.Value.X, -item.Value.Y);
            }
        }

        DoEdgeRouting(offsetY);
    }

    #region ILayoutEdgeRouting<TEdge> Members

    public IDictionary<TEdge, Point[]> EdgeRoutes => _edgeRoutingPoints;

    #endregion
}