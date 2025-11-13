using System.Collections.Generic;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Logic.Algorithms;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
using Westermo.GraphX.Logic.Algorithms.OverlapRemoval;
using QuikGraph;

namespace Westermo.GraphX.Logic.Models;

public sealed class AlgorithmFactory<TVertex, TEdge, TGraph> : IAlgorithmFactory<TVertex, TEdge, TGraph>
    where TVertex : class, IGraphXVertex
    where TEdge : class, IGraphXEdge<TVertex>
    where TGraph : class, IMutableBidirectionalGraph<TVertex, TEdge>, new()
{
    #region Layout factory
    /// <summary>
    /// Generate and initialize layout algorithm
    /// </summary>
    /// <param name="newAlgorithmType">Layout algorithm type</param>
    /// <param name="iGraph">Graph</param>
    /// <param name="positions">Optional vertex positions</param>
    /// <param name="sizes">Optional vertex sizes</param>
    /// <param name="parameters">Optional algorithm parameters</param>
    public ILayoutAlgorithm<TVertex, TEdge, TGraph> CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum newAlgorithmType, TGraph iGraph, IDictionary<TVertex, Point> positions = null, IDictionary<TVertex, Size> sizes = null, ILayoutParameters parameters = null)
    {
        if (iGraph == null) return null;
        parameters ??= CreateLayoutParameters(newAlgorithmType);
        var graph = iGraph.CopyToGraph<TGraph, TVertex, TEdge>();

        graph.RemoveEdgeIf(a => a.SkipProcessing == ProcessingOptionEnum.Exclude);
        graph.RemoveVertexIf(a => a.SkipProcessing == ProcessingOptionEnum.Exclude);

        return newAlgorithmType switch
        {
            LayoutAlgorithmTypeEnum.Tree => new SimpleTreeLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, sizes, parameters as SimpleTreeLayoutParameters),
            LayoutAlgorithmTypeEnum.SimpleRandom => new RandomLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, parameters as RandomLayoutAlgorithmParams),
            LayoutAlgorithmTypeEnum.Circular => new CircularLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, sizes, parameters as CircularLayoutParameters),
            LayoutAlgorithmTypeEnum.FR => new FRLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, parameters as FRLayoutParametersBase),
            LayoutAlgorithmTypeEnum.BoundedFR => new FRLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, parameters as BoundedFRLayoutParameters),
            LayoutAlgorithmTypeEnum.KK => new KKLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, parameters as KKLayoutParameters),
            LayoutAlgorithmTypeEnum.ISOM => new ISOMLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, parameters as ISOMLayoutParameters),
            LayoutAlgorithmTypeEnum.LinLog => new LinLogLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, positions, parameters as LinLogLayoutParameters),
            LayoutAlgorithmTypeEnum.EfficientSugiyama => new EfficientSugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, parameters as EfficientSugiyamaLayoutParameters, positions, sizes),
            LayoutAlgorithmTypeEnum.Sugiyama => new SugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, sizes, positions, parameters as SugiyamaLayoutParameters,
                e => e is TypedEdge<TVertex>
                    ? (e as TypedEdge<TVertex>).Type
                    : EdgeTypes.Hierarchical),
            LayoutAlgorithmTypeEnum.CompoundFDP => new CompoundFDPLayoutAlgorithm<TVertex, TEdge, TGraph>(graph, sizes, new Dictionary<TVertex, Thickness>(), new Dictionary<TVertex, CompoundVertexInnerLayoutType>(),
                positions, parameters as CompoundFDPLayoutParameters),
            /*case LayoutAlgorithmTypeEnum.BalloonTree:
return new BalloonTreeLayoutAlgorithm<TVertex, TEdge, TGraph>(Graph, Positions, Sizes, parameters as BalloonTreeLayoutParameters, Graph.Vertices.FirstOrDefault());*/
            _ => null,
        };
    }

    /// <summary>
    /// Creates parameters data for layout algorithm
    /// </summary>
    /// <param name="algorithmType">Layout algorithm type</param>
    public ILayoutParameters CreateLayoutParameters(LayoutAlgorithmTypeEnum algorithmType)
    {
        return algorithmType switch
        {
            LayoutAlgorithmTypeEnum.Tree => new SimpleTreeLayoutParameters(),
            LayoutAlgorithmTypeEnum.Circular => new CircularLayoutParameters(),
            LayoutAlgorithmTypeEnum.FR => new FreeFRLayoutParameters(),
            LayoutAlgorithmTypeEnum.BoundedFR => new BoundedFRLayoutParameters(),
            LayoutAlgorithmTypeEnum.KK => new KKLayoutParameters(),
            LayoutAlgorithmTypeEnum.ISOM => new ISOMLayoutParameters(),
            LayoutAlgorithmTypeEnum.LinLog => new LinLogLayoutParameters(),
            LayoutAlgorithmTypeEnum.EfficientSugiyama => new EfficientSugiyamaLayoutParameters(),
            LayoutAlgorithmTypeEnum.Sugiyama => new SugiyamaLayoutParameters(),
            LayoutAlgorithmTypeEnum.CompoundFDP => new CompoundFDPLayoutParameters(),
            LayoutAlgorithmTypeEnum.SimpleRandom => new RandomLayoutAlgorithmParams(),
            // case LayoutAlgorithmTypeEnum.BalloonTree:
            //     return new BalloonTreeLayoutParameters();
            _ => null,
        };
    }

    /// <summary>
    /// Returns True if specified layout algorithm needs vertex size data for its calculations
    /// </summary>
    /// <param name="algorithmType">Layout algorithm type</param>
    public bool NeedSizes(LayoutAlgorithmTypeEnum algorithmType)
    {
        return algorithmType switch
        {
            LayoutAlgorithmTypeEnum.Tree or LayoutAlgorithmTypeEnum.Circular or LayoutAlgorithmTypeEnum.EfficientSugiyama or LayoutAlgorithmTypeEnum.Sugiyama or LayoutAlgorithmTypeEnum.CompoundFDP or LayoutAlgorithmTypeEnum.SimpleRandom => true,
            _ => false,
        };
    }

    /// <summary>
    /// Returns True if specified layout algorithm ever needs edge routing pass
    /// </summary>
    /// <param name="algorithmType">Layout algorithm type</param>
    public bool NeedEdgeRouting(LayoutAlgorithmTypeEnum algorithmType)
    {
        return algorithmType != LayoutAlgorithmTypeEnum.Sugiyama && algorithmType != LayoutAlgorithmTypeEnum.EfficientSugiyama;
    }

    /// <summary>
    /// Returns True if specified layout algorithm ever needs overlap removal pass
    /// </summary>
    /// <param name="algorithmType">Layout algorithm type</param>
    public bool NeedOverlapRemoval(LayoutAlgorithmTypeEnum algorithmType)
    {
        return algorithmType != LayoutAlgorithmTypeEnum.Sugiyama
               && algorithmType != LayoutAlgorithmTypeEnum.EfficientSugiyama
               && algorithmType != LayoutAlgorithmTypeEnum.Circular
               && algorithmType != LayoutAlgorithmTypeEnum.Tree;
        /*&& algorithmType != LayoutAlgorithmTypeEnum.BalloonTree*/
    }
    #endregion

    #region OverlapRemoval factory

    /// <summary>
    /// Creates uninitialized overlap removal algorithm
    /// </summary>
    /// <param name="newAlgorithmType">Algorithm type</param>
    public IOverlapRemovalAlgorithm<TVertex> CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum newAlgorithmType)
    {
        return CreateOverlapRemovalAlgorithm(newAlgorithmType, null);
    }

    /// <summary>
    /// Create and initialize overlap removal algorithm
    /// </summary>
    /// <param name="newAlgorithmType">Algorithm type</param>
    /// <param name="rectangles">Object sizes list</param>
    /// <param name="parameters">Optional algorithm parameters</param>
    public IOverlapRemovalAlgorithm<TVertex> CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum newAlgorithmType, IDictionary<TVertex, Rect> rectangles, IOverlapRemovalParameters parameters = null)
    {
        //if (Rectangles == null) return null;
        parameters ??= CreateOverlapRemovalParameters(newAlgorithmType);

        return newAlgorithmType switch
        {
            OverlapRemovalAlgorithmTypeEnum.FSA => new FSAAlgorithm<TVertex>(rectangles, parameters is OverlapRemovalParameters ? parameters : new OverlapRemovalParameters()),
            OverlapRemovalAlgorithmTypeEnum.OneWayFSA => new OneWayFSAAlgorithm<TVertex>(rectangles, parameters is OneWayFSAParameters ? parameters as OneWayFSAParameters : new OneWayFSAParameters()),
            _ => null,
        };
    }

    public IOverlapRemovalAlgorithm<T> CreateFSAA<T>(IDictionary<T, Rect> rectangles, float horGap, float vertGap) where T : class
    {
        return new FSAAlgorithm<T>(rectangles, new OverlapRemovalParameters { HorizontalGap = horGap, VerticalGap = vertGap});
    }

    /// <summary>
    /// Creates uninitialized FSAA overlap removal algorithm instance
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    /// <typeparam name="TParam">Algorithm parameters</typeparam>
    /// <param name="horGap">Horizontal gap setting</param>
    /// <param name="vertGap">Vertical gap setting</param>
    public IOverlapRemovalAlgorithm<T, IOverlapRemovalParameters> CreateFSAA<T>(float horGap, float vertGap) where T : class 
    {
        return new FSAAlgorithm<T, IOverlapRemovalParameters>(null, new OverlapRemovalParameters { HorizontalGap = horGap, VerticalGap = vertGap });
    }

    /// <summary>
    /// Create overlap removal algorithm parameters
    /// </summary>
    /// <param name="algorithmType">Overlap removal algorithm type</param>
    public IOverlapRemovalParameters CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum algorithmType)
    {
        return algorithmType switch
        {
            OverlapRemovalAlgorithmTypeEnum.FSA => new OverlapRemovalParameters(),
            OverlapRemovalAlgorithmTypeEnum.OneWayFSA => new OneWayFSAParameters(),
            _ => null,
        };
    }


    #endregion

    #region Edge Routing factory
    public IExternalEdgeRouting<TVertex, TEdge> CreateEdgeRoutingAlgorithm(EdgeRoutingAlgorithmTypeEnum newAlgorithmType, Rect graphArea, TGraph iGraph, IDictionary<TVertex, Point> positions, IDictionary<TVertex, Rect> rectangles, IEdgeRoutingParameters parameters = null)
    {
        //if (Rectangles == null) return null;
        parameters ??= CreateEdgeRoutingParameters(newAlgorithmType);
        var graph = iGraph.CopyToGraph<TGraph, TVertex, TEdge>();
        graph.RemoveEdgeIf(a => a.SkipProcessing == ProcessingOptionEnum.Exclude);
        graph.RemoveVertexIf(a => a.SkipProcessing == ProcessingOptionEnum.Exclude);

        return newAlgorithmType switch
        {
            EdgeRoutingAlgorithmTypeEnum.SimpleER => new SimpleEdgeRouting<TVertex, TEdge, TGraph>(graph, positions, rectangles, parameters),
            EdgeRoutingAlgorithmTypeEnum.Bundling => new BundleEdgeRouting<TVertex, TEdge, TGraph>(graphArea, graph, positions, rectangles, parameters),
            EdgeRoutingAlgorithmTypeEnum.PathFinder => new PathFinderEdgeRouting<TVertex, TEdge, TGraph>(graph, positions, rectangles, parameters),
            _ => null,
        };
    }

    public IEdgeRoutingParameters CreateEdgeRoutingParameters(EdgeRoutingAlgorithmTypeEnum algorithmType)
    {
        return algorithmType switch
        {
            EdgeRoutingAlgorithmTypeEnum.SimpleER => new SimpleERParameters(),
            EdgeRoutingAlgorithmTypeEnum.Bundling => new BundleEdgeRoutingParameters(),
            EdgeRoutingAlgorithmTypeEnum.PathFinder => new PathFinderEdgeRoutingParameters(),
            _ => null,
        };
    }
    #endregion
}