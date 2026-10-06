using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Models;
using MPoint = Westermo.GraphX.Measure.Point;
using MSize = Westermo.GraphX.Measure.Size;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for <see cref="GXLogicCore{TVertex,TEdge,TGraph}"/>: the central business-logic object that owns the graph,
/// filter pipeline (graph data mutation), algorithm selection and the layout/overlap/edge-routing compute pipeline.
/// </summary>
public class GXLogicCoreTests
{
    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override MPoint[]? RoutingPoints { get; set; } = null;
    }

    private class TGraph : BidirectionalGraph<TVertex, TEdge>;

    private class TLogic : GXLogicCore<TVertex, TEdge, TGraph>
    {
        public TLogic() { }
        public TLogic(TGraph g) : base(g) { }
    }

    /// <summary>
    /// Filter that removes every vertex whose name matches the supplied predicate. Records the graph it received
    /// so the tests can verify which graph instance the pipeline fed to each filter.
    /// </summary>
    private class RemoveVertexFilter(Func<TVertex, bool> predicate) : IGraphFilter<TVertex, TEdge, TGraph>
    {
        public TGraph? Input { get; private set; }

        public TGraph ProcessFilter(TGraph inputGraph)
        {
            Input = inputGraph;
            inputGraph.RemoveVertexIf(v => predicate(v));
            return inputGraph;
        }
    }

    /// <summary>
    /// Builds a simple chain A -> B -> C -> D.
    /// </summary>
    private static (TGraph graph, TVertex[] vertices) CreateChain()
    {
        var g = new TGraph();
        var vs = new[] { new TVertex("A"), new TVertex("B"), new TVertex("C"), new TVertex("D") };
        g.AddVertexRange(vs);
        for (var i = 0; i < vs.Length - 1; i++) g.AddEdge(new TEdge(vs[i], vs[i + 1]));
        return (g, vs);
    }

    #region Construction / defaults

    [Test]
    public async Task Constructor_Default_InitializesDocumentedDefaults()
    {
        using var core = new TLogic();

        await Assert.That(core.Graph).IsNotNull();
        await Assert.That(core.AlgorithmFactory).IsNotNull();
        await Assert.That(core.AlgorithmStorage).IsNotNull();
        await Assert.That(core.EdgeCurvingEnabled).IsTrue();
        await Assert.That(core.EdgeCurvingTolerance).IsEqualTo(8d);
        await Assert.That(core.ParallelEdgeDistance).IsEqualTo(5);
        await Assert.That(core.IsFiltered).IsFalse();
        await Assert.That(core.Filters.Count).IsEqualTo(0);
        // The enum default (0) is BoundedFR, so a fresh core is NOT a custom layout.
        await Assert.That(core.DefaultLayoutAlgorithm).IsEqualTo(LayoutAlgorithmTypeEnum.BoundedFR);
        await Assert.That(core.IsCustomLayout).IsFalse();
    }

    [Test]
    public async Task Constructor_WithGraph_UsesSuppliedGraphInstance()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        await Assert.That(ReferenceEquals(core.Graph, g)).IsTrue();
    }

    [Test]
    public async Task Dispose_ReleasesGraphAlgorithmsAndFactory()
    {
        var (g, _) = CreateChain();
        var core = new TLogic(g);
        core.Dispose();

        await Assert.That(core.Graph).IsNull();
        await Assert.That(core.AlgorithmFactory).IsNull();
        await Assert.That(core.AlgorithmStorage).IsNull();
        await Assert.That(core.ExternalLayoutAlgorithm).IsNull();
        await Assert.That(core.ExternalOverlapRemovalAlgorithm).IsNull();
        await Assert.That(core.ExternalEdgeRoutingAlgorithm).IsNull();
    }

    [Test]
    public async Task Clear_EmptiesGraphAndRecreatesStorage()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        var oldStorage = core.AlgorithmStorage;

        core.Clear();

        await Assert.That(core.Graph.VertexCount).IsEqualTo(0);
        await Assert.That(core.Graph.EdgeCount).IsEqualTo(0);
        await Assert.That(ReferenceEquals(core.AlgorithmStorage, oldStorage)).IsFalse();
    }

    [Test]
    public async Task Clear_WithoutStorages_KeepsExistingStorage()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        var oldStorage = core.AlgorithmStorage;

        core.Clear(clearStorages: false);

        await Assert.That(core.Graph.VertexCount).IsEqualTo(0);
        await Assert.That(ReferenceEquals(core.AlgorithmStorage, oldStorage)).IsTrue();
    }

    #endregion

    #region Algorithm selection

    [Test]
    public async Task SettingDefaultLayoutAlgorithm_CreatesMatchingParameters()
    {
        using var core = new TLogic();
        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;

        await Assert.That(core.DefaultLayoutAlgorithmParams).IsNotNull();
        await Assert.That(core.DefaultLayoutAlgorithmParams!.GetType().Name).IsEqualTo("KKLayoutParameters");
        await Assert.That(core.IsCustomLayout).IsFalse();
    }

    [Test]
    public async Task SettingDefaultLayoutAlgorithm_PreservesUserSuppliedParameters()
    {
        // SetDefaultParams only creates parameters when none exist. This means switching algorithm keeps
        // the previous algorithm's parameter object, which is the documented behaviour, but is a source of
        // surprise: the KK parameters below will silently be fed to the FR algorithm (and cast to null there).
        using var core = new TLogic();
        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        var parameters = core.DefaultLayoutAlgorithmParams;

        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.FR;

        await Assert.That(ReferenceEquals(core.DefaultLayoutAlgorithmParams, parameters)).IsTrue();
    }

    [Test]
    public async Task SettingDefaultOverlapAndEdgeRouting_CreatesMatchingParameters()
    {
        using var core = new TLogic();
        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
        core.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;

        await Assert.That(core.DefaultOverlapRemovalAlgorithmParams!.GetType().Name).IsEqualTo("OverlapRemovalParameters");
        await Assert.That(core.DefaultEdgeRoutingAlgorithmParams!.GetType().Name).IsEqualTo("SimpleERParameters");
    }

    [Test]
    public async Task SwitchingOverlapRemovalFromFSAToOneWayFSA_ConvertsParametersKeepingGaps()
    {
        using var core = new TLogic();
        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
        core.DefaultOverlapRemovalAlgorithmParams!.HorizontalGap = 33f;
        core.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 44f;

        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.OneWayFSA;

        // The stored params must now be usable by OneWayFSA (so Way is configurable) and keep the gaps.
        var p = core.DefaultOverlapRemovalAlgorithmParams;
        await Assert.That(p.GetType().Name).IsEqualTo("OneWayFSAParameters");
        await Assert.That(p.HorizontalGap).IsEqualTo(33f);
        await Assert.That(p.VerticalGap).IsEqualTo(44f);
    }

    [Test]
    public async Task SwitchingOverlapRemovalFromOneWayFSAToFSA_KeepsSameParameters()
    {
        using var core = new TLogic();
        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.OneWayFSA;
        var p = core.DefaultOverlapRemovalAlgorithmParams;

        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;

        // OneWayFSAParameters is a valid FSA parameter set, so nothing needs replacing.
        await Assert.That(core.DefaultOverlapRemovalAlgorithmParams).IsSameReferenceAs(p);
    }

    [Test]
    public async Task IsEdgeRoutingEnabled_ReflectsDefaultAlgorithm()
    {
        using var core = new TLogic();
        core.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
        await Assert.That(core.IsEdgeRoutingEnabled).IsFalse();

        core.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
        await Assert.That(core.IsEdgeRoutingEnabled).IsTrue();
    }

    [Test]
    public async Task AreOverlapNeeded_FalseForLayoutsThatHandleOverlapThemselves()
    {
        using var core = new TLogic();
        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;

        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Tree;
        await Assert.That(core.AreOverlapNeeded()).IsFalse();

        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        await Assert.That(core.AreOverlapNeeded()).IsTrue();

        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
        await Assert.That(core.AreOverlapNeeded()).IsFalse();
    }

    [Test]
    public async Task AreVertexSizesNeeded_TrueWhenLayoutRequiresSizesOrEdgeRoutingEnabled()
    {
        using var core = new TLogic();
        core.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;

        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        await Assert.That(core.AreVertexSizesNeeded()).IsFalse();

        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Tree;
        await Assert.That(core.AreVertexSizesNeeded()).IsTrue();

        core.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK;
        core.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
        await Assert.That(core.AreVertexSizesNeeded()).IsTrue();
    }

    [Test]
    public async Task GenerateEdgeRoutingAlgorithm_NoneAndNoExternal_ReturnsNull()
    {
        using var core = new TLogic();
        core.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
        await Assert.That(core.GenerateEdgeRoutingAlgorithm(new MSize(100, 100))).IsNull();
    }

    [Test]
    public async Task GenerateOverlapRemovalAlgorithm_NoneAndNoExternal_ReturnsNull()
    {
        using var core = new TLogic();
        core.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
        await Assert.That(core.GenerateOverlapRemovalAlgorithm()).IsNull();
    }

    [Test]
    public async Task GenerateLayoutAlgorithm_Custom_ReturnsNull()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g) { DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Custom };
        await Assert.That(core.IsCustomLayout).IsTrue();
        await Assert.That(core.GenerateLayoutAlgorithm(new Dictionary<TVertex, MSize>(), new Dictionary<TVertex, MPoint>())).IsNull();
    }

    [Test]
    public async Task GenerateLayoutAlgorithm_ExternalAlgorithm_TakesPrecedenceOverDefault()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g) { DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK };
        var external = core.AlgorithmFactory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.Circular, g)!;
        core.ExternalLayoutAlgorithm = external;

        await Assert.That(core.IsCustomLayout).IsFalse();
        await Assert.That(ReferenceEquals(core.GenerateLayoutAlgorithm(new Dictionary<TVertex, MSize>(), null!), external)).IsTrue();
    }

    [Test]
    public async Task GenerateLayoutAlgorithm_NeedsSizes_AssignsSuppliedSizes()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g) { DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.SimpleRandom };
        var sizes = vs.ToDictionary(v => v, _ => new MSize(10, 10));

        var alg = core.GenerateLayoutAlgorithm(sizes, null!);

        await Assert.That(alg).IsNotNull();
        await Assert.That(alg!.NeedVertexSizes).IsTrue();
        await Assert.That(ReferenceEquals(alg.VertexSizes, sizes)).IsTrue();
    }

    #endregion

    #region Filters (graph data mutation)

    [Test]
    public async Task PushFilters_AppliesFilterAndPreservesOriginalGraph()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g);
        core.Filters.Enqueue(new RemoveVertexFilter(v => v.Name == "B"));

        core.PushFilters();

        await Assert.That(core.IsFiltered).IsTrue();
        await Assert.That(core.IsFilterRemoved).IsFalse();
        await Assert.That(core.Filters.Count).IsEqualTo(0);
        await Assert.That(core.Graph.VertexCount).IsEqualTo(3);
        await Assert.That(core.Graph.ContainsVertex(vs[1])).IsFalse();
        // The original graph must remain intact so the filter can be undone.
        await Assert.That(core.OriginalGraph.VertexCount).IsEqualTo(4);
        await Assert.That(core.OriginalGraph.EdgeCount).IsEqualTo(3);
        // The user's input graph is not mutated by the filter.
        await Assert.That(g.VertexCount).IsEqualTo(4);
    }

    [Test]
    public async Task PushFilters_ChainsFiltersInFifoOrder()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        var first = new RemoveVertexFilter(v => v.Name == "A");
        var second = new RemoveVertexFilter(v => v.Name == "D");
        core.Filters.Enqueue(first);
        core.Filters.Enqueue(second);

        core.PushFilters();

        // The first filter operates on a copy of the original graph; subsequent filters receive the previous output.
        await Assert.That(ReferenceEquals(first.Input, core.OriginalGraph)).IsFalse();
        await Assert.That(ReferenceEquals(second.Input, first.Input)).IsTrue();
        await Assert.That(core.Graph.Vertices.Select(v => v.Name).OrderBy(n => n).ToArray()).IsEquivalentTo(new[] { "B", "C" });
        await Assert.That(core.Graph.EdgeCount).IsEqualTo(1);
    }

    [Test]
    public async Task PushFilters_Twice_RefiltersFromOriginalGraph()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        core.Filters.Enqueue(new RemoveVertexFilter(v => v.Name == "A"));
        core.PushFilters();

        // Second push replaces the previous filter rather than stacking on top of it.
        core.Filters.Enqueue(new RemoveVertexFilter(v => v.Name == "D"));
        core.PushFilters();

        var names = core.Graph.Vertices.Select(v => v.Name).OrderBy(n => n).ToArray();
        await Assert.That(names).IsEquivalentTo(new[] { "A", "B", "C" });
        await Assert.That(core.OriginalGraph.VertexCount).IsEqualTo(4);
    }

    [Test]
    public async Task PopFilters_RestoresOriginalGraph()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        core.Filters.Enqueue(new RemoveVertexFilter(v => v.Name == "B"));
        core.PushFilters();

        core.PopFilters();

        await Assert.That(core.IsFiltered).IsFalse();
        await Assert.That(core.IsFilterRemoved).IsTrue();
        await Assert.That(core.Graph.VertexCount).IsEqualTo(4);
        await Assert.That(core.Graph.EdgeCount).IsEqualTo(3);
    }

    [Test]
    public async Task PushFilters_EmptyQueueWhileFiltered_PopsFilters()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        core.Filters.Enqueue(new RemoveVertexFilter(v => v.Name == "B"));
        core.PushFilters();

        core.PushFilters();

        await Assert.That(core.IsFiltered).IsFalse();
        await Assert.That(core.IsFilterRemoved).IsTrue();
        await Assert.That(core.Graph.VertexCount).IsEqualTo(4);
    }

    [Test]
    public async Task ApplyFilters_MakesFilteredGraphPermanent()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);
        core.Filters.Enqueue(new RemoveVertexFilter(v => v.Name == "B"));
        core.PushFilters();

        core.ApplyFilters();

        await Assert.That(core.IsFiltered).IsFalse();
        await Assert.That(core.IsFilterRemoved).IsTrue();
        await Assert.That(ReferenceEquals(core.OriginalGraph, core.Graph)).IsTrue();

        // Pop is now a no-op since the filtered state was committed.
        core.PopFilters();
        await Assert.That(core.Graph.VertexCount).IsEqualTo(3);
    }

    [Test]
    public async Task ApplyAndPopFilters_WhenNotFiltered_AreNoOps()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);

        core.ApplyFilters();
        core.PopFilters();

        await Assert.That(ReferenceEquals(core.Graph, g)).IsTrue();
        await Assert.That(core.OriginalGraph).IsNull();
        await Assert.That(core.IsFilterRemoved).IsFalse();
    }

    [Test]
    public async Task PushFilters_EmptyQueueWhenNotFiltered_DoesNothing()
    {
        var (g, _) = CreateChain();
        using var core = new TLogic(g);

        core.PushFilters();

        await Assert.That(core.IsFiltered).IsFalse();
        await Assert.That(ReferenceEquals(core.Graph, g)).IsTrue();
    }

    #endregion

    #region GetVertexSizeRectangles

    [Test]
    public async Task GetVertexSizeRectangles_NullArguments_Throw()
    {
        using var core = new TLogic();
        Assert.Throws<GX_InvalidDataException>(() => core.GetVertexSizeRectangles(null!, new Dictionary<TVertex, MSize>()));
        Assert.Throws<GX_InvalidDataException>(() => core.GetVertexSizeRectangles(new Dictionary<TVertex, MPoint>(), null!));
        await Task.CompletedTask;
    }

    [Test]
    public async Task GetVertexSizeRectangles_TopLeft_BuildsRectsFromPositionAndSize()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g);
        var positions = vs.ToDictionary(v => v, v => new MPoint(10, 20));
        var sizes = vs.ToDictionary(v => v, _ => new MSize(30, 40));

        var rects = core.GetVertexSizeRectangles(positions, sizes);

        await Assert.That(rects.Count).IsEqualTo(4);
        var r = rects[vs[0]];
        await Assert.That(r.X).IsEqualTo(10d);
        await Assert.That(r.Y).IsEqualTo(20d);
        await Assert.That(r.Width).IsEqualTo(30d);
        await Assert.That(r.Height).IsEqualTo(40d);
    }

    [Test]
    public async Task GetVertexSizeRectangles_CenterPoints_OffsetsByHalfSize()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g);
        var positions = vs.ToDictionary(v => v, _ => new MPoint(100, 100));
        var sizes = vs.ToDictionary(v => v, _ => new MSize(30, 40));

        var rects = core.GetVertexSizeRectangles(positions, sizes, getCenterPoints: true);

        await Assert.That(rects[vs[2]].X).IsEqualTo(85d);
        await Assert.That(rects[vs[2]].Y).IsEqualTo(80d);
    }

    [Test]
    public async Task GetVertexSizeRectangles_SkipsExcludedAndIncompleteVertices()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g);
        vs[0].SkipProcessing = ProcessingOptionEnum.Exclude;
        var positions = vs.Where(v => v.Name != "B").ToDictionary(v => v, _ => new MPoint(0, 0));
        var sizes = vs.Where(v => v.Name != "C").ToDictionary(v => v, _ => new MSize(1, 1));

        var rects = core.GetVertexSizeRectangles(positions, sizes);

        // A is excluded, B has no position, C has no size => only D remains.
        await Assert.That(rects.Keys.Select(k => k.Name).ToArray()).IsEquivalentTo(new[] { "D" });
    }

    #endregion

    #region Compute pipeline

    [Test]
    public async Task Compute_WithoutGraph_Throws()
    {
        using var core = new TLogic();
        core.Graph = null!;
        Assert.Throws<GX_InvalidDataException>(() => core.Compute(CancellationToken.None));
        await Task.CompletedTask;
    }

    [Test]
    public async Task ComputeEdgeRoutesByVertex_WithoutEdgeRoutingStorage_Throws()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g);
        Assert.Throws<GX_InvalidDataException>(() => core.ComputeEdgeRoutesByVertex(vs[0]));
        await Task.CompletedTask;
    }

    [Test]
    public async Task GenerateAlgorithmStorage_CustomLayout_ComputeReturnsSuppliedPositions()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g)
        {
            DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Custom,
            DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None
        };
        var positions = vs.Select((v, i) => (v, i)).ToDictionary(p => p.v, p => new MPoint(p.i * 10, p.i * 5));
        var sizes = vs.ToDictionary(v => v, _ => new MSize(10, 10));

        var ok = core.GenerateAlgorithmStorage(sizes, positions);
        var result = core.Compute(CancellationToken.None);

        await Assert.That(ok).IsTrue();
        await Assert.That(core.AlgorithmStorage.Layout).IsNull();
        await Assert.That(result[vs[3]].X).IsEqualTo(30d);
        await Assert.That(result[vs[3]].Y).IsEqualTo(15d);
    }

    [Test]
    public async Task GenerateAlgorithmStorage_RealLayout_ComputesPositionForEveryIncludedVertex()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g)
        {
            DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Circular,
            DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None
        };
        var positions = vs.ToDictionary(v => v, _ => new MPoint(0, 0));
        var sizes = vs.ToDictionary(v => v, _ => new MSize(20, 20));

        var ok = core.GenerateAlgorithmStorage(sizes, positions);
        var result = core.Compute(CancellationToken.None);

        await Assert.That(ok).IsTrue();
        await Assert.That(core.AlgorithmStorage.Layout).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(4);
        // A circular layout must spread vertices out; they cannot all share one coordinate.
        await Assert.That(result.Values.Distinct().Count()).IsEqualTo(4);
    }

    [Test]
    public async Task GenerateAlgorithmStorage_ExcludedVertices_AreNotLaidOut()
    {
        var (g, vs) = CreateChain();
        vs[1].SkipProcessing = ProcessingOptionEnum.Exclude;
        using var core = new TLogic(g)
        {
            DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Circular,
            DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None
        };
        // No seed positions: any position in the result must have been produced by the layout itself.
        var positions = new Dictionary<TVertex, MPoint>();
        var sizes = vs.ToDictionary(v => v, _ => new MSize(20, 20));

        core.GenerateAlgorithmStorage(sizes, positions);
        var result = core.Compute(CancellationToken.None);

        await Assert.That(result.ContainsKey(vs[1])).IsFalse();
        await Assert.That(result.Count).IsEqualTo(3);
        // The source graph itself must not be mutated by exclusion handling.
        await Assert.That(g.VertexCount).IsEqualTo(4);
    }

    [Test]
    public async Task GenerateAlgorithmStorage_ExcludedVertex_KeepsItsSeededPosition()
    {
        var (g, vs) = CreateChain();
        vs[1].SkipProcessing = ProcessingOptionEnum.Exclude;
        using var core = new TLogic(g)
        {
            DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Circular,
            DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None
        };
        // Seed positions are copied into the algorithm, so an excluded vertex passes through untouched.
        var positions = vs.ToDictionary(v => v, _ => new MPoint(123, 456));
        var sizes = vs.ToDictionary(v => v, _ => new MSize(20, 20));

        core.GenerateAlgorithmStorage(sizes, positions);
        var result = core.Compute(CancellationToken.None);

        await Assert.That(result[vs[1]]).IsEqualTo(new MPoint(123, 456));
        // The caller's dictionary is copied, not mutated in place.
        await Assert.That(positions[vs[0]]).IsEqualTo(new MPoint(123, 456));
    }

    [Test]
    public async Task GenerateAlgorithmStorage_WithOverlapRemoval_ProducesNonOverlappingRects()
    {
        var (g, vs) = CreateChain();
        using var core = new TLogic(g)
        {
            DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.Custom,
            DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA,
            DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None
        };
        // Custom layout never needs overlap removal per factory rules, so supply the algorithm externally
        // to exercise the OR branch of Compute with all vertices stacked at the same position.
        core.ExternalOverlapRemovalAlgorithm = core.AlgorithmFactory.CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum.FSA, null, core.DefaultOverlapRemovalAlgorithmParams);
        var positions = vs.Select((v, i) => (v, i)).ToDictionary(p => p.v, p => new MPoint(p.i * 0.1, 0));
        var sizes = vs.ToDictionary(v => v, _ => new MSize(20, 20));

        core.GenerateAlgorithmStorage(sizes, positions);
        var result = core.Compute(CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(4);
        var rects = result.ToDictionary(kv => kv.Key, kv => new Westermo.GraphX.Measure.Rect(kv.Value, sizes[kv.Key]));
        foreach (var a in vs)
        foreach (var b in vs.Where(b => !ReferenceEquals(a, b)))
        {
            var inter = Westermo.GraphX.Measure.Rect.Intersect(rects[a], rects[b]);
            // Touching edges are allowed, overlapping areas are not.
            await Assert.That(inter.Width * inter.Height).IsLessThan(1e-6);
        }
    }

    #endregion
}
