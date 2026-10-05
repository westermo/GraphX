using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Models;
using MPoint = Westermo.GraphX.Measure.Point;
using MRect = Westermo.GraphX.Measure.Rect;
using MSize = Westermo.GraphX.Measure.Size;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Tests for <see cref="AlgorithmFactory{TVertex,TEdge,TGraph}"/>, which decides which layout,
/// overlap-removal and edge-routing passes run and with which parameters.
/// </summary>
public class AlgorithmFactoryTests
{
    public class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    public class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override MPoint[]? RoutingPoints { get; set; } = null;
    }

    public class TGraph : BidirectionalGraph<TVertex, TEdge>;

    private static readonly AlgorithmFactory<TVertex, TEdge, TGraph> Factory = new();

    private static TGraph BuildTriangle(out TVertex a, out TVertex b, out TVertex c)
    {
        a = new TVertex("A");
        b = new TVertex("B");
        c = new TVertex("C");
        var g = new TGraph();
        g.AddVertexRange([a, b, c]);
        g.AddEdge(new TEdge(a, b));
        g.AddEdge(new TEdge(b, c));
        g.AddEdge(new TEdge(c, a));
        return g;
    }

    #region Need* flags

    [Test]
    [Arguments(LayoutAlgorithmTypeEnum.Tree, true)]
    [Arguments(LayoutAlgorithmTypeEnum.Circular, true)]
    [Arguments(LayoutAlgorithmTypeEnum.EfficientSugiyama, true)]
    [Arguments(LayoutAlgorithmTypeEnum.Sugiyama, true)]
    [Arguments(LayoutAlgorithmTypeEnum.CompoundFDP, true)]
    [Arguments(LayoutAlgorithmTypeEnum.SimpleRandom, true)]
    [Arguments(LayoutAlgorithmTypeEnum.FR, false)]
    [Arguments(LayoutAlgorithmTypeEnum.BoundedFR, false)]
    [Arguments(LayoutAlgorithmTypeEnum.KK, false)]
    [Arguments(LayoutAlgorithmTypeEnum.ISOM, false)]
    [Arguments(LayoutAlgorithmTypeEnum.LinLog, false)]
    [Arguments(LayoutAlgorithmTypeEnum.Custom, false)]
    public async Task NeedSizes_MatchesAlgorithmRequirements(LayoutAlgorithmTypeEnum type, bool expected)
    {
        await Assert.That(Factory.NeedSizes(type)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(LayoutAlgorithmTypeEnum.Sugiyama, false)]
    [Arguments(LayoutAlgorithmTypeEnum.EfficientSugiyama, false)]
    [Arguments(LayoutAlgorithmTypeEnum.Tree, true)]
    [Arguments(LayoutAlgorithmTypeEnum.Circular, true)]
    [Arguments(LayoutAlgorithmTypeEnum.KK, true)]
    [Arguments(LayoutAlgorithmTypeEnum.Custom, true)]
    public async Task NeedEdgeRouting_IsFalseOnlyForSugiyamaFamily(LayoutAlgorithmTypeEnum type, bool expected)
    {
        await Assert.That(Factory.NeedEdgeRouting(type)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(LayoutAlgorithmTypeEnum.Sugiyama, false)]
    [Arguments(LayoutAlgorithmTypeEnum.EfficientSugiyama, false)]
    [Arguments(LayoutAlgorithmTypeEnum.Circular, false)]
    [Arguments(LayoutAlgorithmTypeEnum.Tree, false)]
    [Arguments(LayoutAlgorithmTypeEnum.FR, true)]
    [Arguments(LayoutAlgorithmTypeEnum.KK, true)]
    [Arguments(LayoutAlgorithmTypeEnum.LinLog, true)]
    [Arguments(LayoutAlgorithmTypeEnum.Custom, true)]
    public async Task NeedOverlapRemoval_IsFalseForNonOverlappingLayouts(LayoutAlgorithmTypeEnum type, bool expected)
    {
        await Assert.That(Factory.NeedOverlapRemoval(type)).IsEqualTo(expected);
    }

    #endregion

    #region Parameter factories

    [Test]
    [Arguments(LayoutAlgorithmTypeEnum.Tree, "SimpleTreeLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.Circular, "CircularLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.FR, "FreeFRLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.BoundedFR, "BoundedFRLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.KK, "KKLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.ISOM, "ISOMLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.LinLog, "LinLogLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.EfficientSugiyama, "EfficientSugiyamaLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.Sugiyama, "SugiyamaLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.CompoundFDP, "CompoundFDPLayoutParameters")]
    [Arguments(LayoutAlgorithmTypeEnum.SimpleRandom, "RandomLayoutAlgorithmParams")]
    public async Task CreateLayoutParameters_ReturnsMatchingType(LayoutAlgorithmTypeEnum type, string expectedTypeName)
    {
        var p = Factory.CreateLayoutParameters(type);
        await Assert.That(p).IsNotNull();
        await Assert.That(p.GetType().Name).IsEqualTo(expectedTypeName);
    }

    [Test]
    public async Task CreateLayoutParameters_Custom_ReturnsNull()
    {
        await Assert.That(Factory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.Custom)).IsNull();
    }

    [Test]
    public async Task CreateLayoutParameters_ReturnsFreshInstanceEachCall()
    {
        // Shared parameter instances would leak settings between graph areas.
        var p1 = Factory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.KK);
        var p2 = Factory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.KK);
        await Assert.That(p1).IsNotSameReferenceAs(p2);
    }

    [Test]
    [Arguments(OverlapRemovalAlgorithmTypeEnum.FSA, "OverlapRemovalParameters")]
    [Arguments(OverlapRemovalAlgorithmTypeEnum.OneWayFSA, "OneWayFSAParameters")]
    public async Task CreateOverlapRemovalParameters_ReturnsMatchingType(OverlapRemovalAlgorithmTypeEnum type, string expectedTypeName)
    {
        await Assert.That(Factory.CreateOverlapRemovalParameters(type)!.GetType().Name).IsEqualTo(expectedTypeName);
    }

    [Test]
    public async Task CreateOverlapRemovalParameters_None_ReturnsNull()
    {
        await Assert.That(Factory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.None)).IsNull();
    }

    [Test]
    [Arguments(EdgeRoutingAlgorithmTypeEnum.SimpleER, "SimpleERParameters")]
    [Arguments(EdgeRoutingAlgorithmTypeEnum.Bundling, "BundleEdgeRoutingParameters")]
    [Arguments(EdgeRoutingAlgorithmTypeEnum.PathFinder, "PathFinderEdgeRoutingParameters")]
    public async Task CreateEdgeRoutingParameters_ReturnsMatchingType(EdgeRoutingAlgorithmTypeEnum type, string expectedTypeName)
    {
        await Assert.That(Factory.CreateEdgeRoutingParameters(type)!.GetType().Name).IsEqualTo(expectedTypeName);
    }

    [Test]
    public async Task CreateEdgeRoutingParameters_None_ReturnsNull()
    {
        await Assert.That(Factory.CreateEdgeRoutingParameters(EdgeRoutingAlgorithmTypeEnum.None)).IsNull();
    }

    #endregion

    #region Layout algorithm factory

    [Test]
    public async Task CreateLayoutAlgorithm_NullGraph_ReturnsNull()
    {
        await Assert.That(Factory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.KK, null!)).IsNull();
    }

    [Test]
    public async Task CreateLayoutAlgorithm_Custom_ReturnsNull()
    {
        var g = BuildTriangle(out _, out _, out _);
        await Assert.That(Factory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.Custom, g)).IsNull();
    }

    [Test]
    [Arguments(LayoutAlgorithmTypeEnum.Tree)]
    [Arguments(LayoutAlgorithmTypeEnum.SimpleRandom)]
    [Arguments(LayoutAlgorithmTypeEnum.Circular)]
    [Arguments(LayoutAlgorithmTypeEnum.FR)]
    [Arguments(LayoutAlgorithmTypeEnum.BoundedFR)]
    [Arguments(LayoutAlgorithmTypeEnum.KK)]
    [Arguments(LayoutAlgorithmTypeEnum.ISOM)]
    [Arguments(LayoutAlgorithmTypeEnum.LinLog)]
    [Arguments(LayoutAlgorithmTypeEnum.EfficientSugiyama)]
    [Arguments(LayoutAlgorithmTypeEnum.Sugiyama)]
    [Arguments(LayoutAlgorithmTypeEnum.CompoundFDP)]
    public async Task CreateLayoutAlgorithm_AllBuiltIns_AreCreatedOnACopyOfTheGraph(LayoutAlgorithmTypeEnum type)
    {
        var g = BuildTriangle(out var a, out var b, out var c);
        // Sugiyama dereferences sizes in its constructor, so a sizes dictionary is always supplied.
        var sizes = new Dictionary<TVertex, MSize> { [a] = new(10, 10), [b] = new(10, 10), [c] = new(10, 10) };
        var alg = Factory.CreateLayoutAlgorithm(type, g, sizes: sizes);

        await Assert.That(alg).IsNotNull();
        // The algorithm must work on its own copy so removing excluded items never mutates the user's graph.
        await Assert.That(alg!.VisitedGraph).IsNotSameReferenceAs(g);
        await Assert.That(alg.VisitedGraph.VertexCount).IsEqualTo(3);
        await Assert.That(alg.VisitedGraph.EdgeCount).IsEqualTo(3);
    }

    [Test]
    public async Task CreateLayoutAlgorithm_ExcludesSkippedItems_WithoutMutatingSourceGraph()
    {
        var g = BuildTriangle(out var a, out _, out var c);
        a.SkipProcessing = ProcessingOptionEnum.Exclude;
        var ca = g.Edges.Single(e => e.Source == c && e.Target == a);
        var bc = g.Edges.Single(e => e.Target == c);
        bc.SkipProcessing = ProcessingOptionEnum.Exclude;

        var alg = Factory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.KK, g)!;

        // Excluded vertex A (and its edges AB, CA) plus excluded edge BC are removed from the copy.
        await Assert.That(alg.VisitedGraph.ContainsVertex(a)).IsFalse();
        await Assert.That(alg.VisitedGraph.VertexCount).IsEqualTo(2);
        await Assert.That(alg.VisitedGraph.EdgeCount).IsEqualTo(0);
        await Assert.That(alg.VisitedGraph.ContainsEdge(ca)).IsFalse();

        // The source graph is untouched.
        await Assert.That(g.VertexCount).IsEqualTo(3);
        await Assert.That(g.EdgeCount).IsEqualTo(3);
    }

    [Test]
    public async Task CreateLayoutAlgorithm_FreezeOption_IsNotExcluded()
    {
        var g = BuildTriangle(out var a, out _, out _);
        a.SkipProcessing = ProcessingOptionEnum.Freeze;

        var alg = Factory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.KK, g)!;

        await Assert.That(alg.VisitedGraph.ContainsVertex(a)).IsTrue();
    }

    [Test]
    public async Task CreateLayoutAlgorithm_UsesSuppliedParameters()
    {
        var g = BuildTriangle(out _, out _, out _);
        var p = Factory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.SimpleRandom);
        var alg = Factory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.SimpleRandom, g, parameters: p);

        await Assert.That(alg).IsNotNull();
    }

    [Test]
    public async Task CreateLayoutAlgorithm_SimpleRandom_ComputesPositionForEveryVertex()
    {
        var g = BuildTriangle(out var a, out var b, out var c);
        var sizes = new Dictionary<TVertex, MSize>
        {
            [a] = new(10, 10), [b] = new(10, 10), [c] = new(10, 10),
        };
        var alg = Factory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.SimpleRandom, g, sizes: sizes)!;
        // The factory does not forward sizes to the random algorithm; GXLogicCore assigns them via NeedVertexSizes.
        await Assert.That(alg.NeedVertexSizes).IsTrue();
        alg.VertexSizes = sizes;

        alg.Compute(CancellationToken.None);

        await Assert.That(alg.VertexPositions.Count).IsEqualTo(3);
        foreach (var v in new[] { a, b, c })
        {
            var p = alg.VertexPositions[v];
            await Assert.That(double.IsNaN(p.X) || double.IsNaN(p.Y)).IsFalse();
        }
    }

    #endregion

    #region Overlap removal factory

    [Test]
    public async Task CreateOverlapRemovalAlgorithm_None_ReturnsNull()
    {
        await Assert.That(Factory.CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum.None)).IsNull();
    }

    [Test]
    [Arguments(OverlapRemovalAlgorithmTypeEnum.FSA, "OverlapRemovalParameters")]
    [Arguments(OverlapRemovalAlgorithmTypeEnum.OneWayFSA, "OneWayFSAParameters")]
    public async Task CreateOverlapRemovalAlgorithm_UsesDefaultParameters(OverlapRemovalAlgorithmTypeEnum type, string expectedParams)
    {
        var alg = Factory.CreateOverlapRemovalAlgorithm(type);
        await Assert.That(alg).IsNotNull();
        await Assert.That(alg!.GetParameters().GetType().Name).IsEqualTo(expectedParams);
    }

    [Test]
    public async Task CreateOverlapRemovalAlgorithm_OneWayFSA_WithWrongParameterType_FallsBackToDefaults()
    {
        // A plain OverlapRemovalParameters is not OneWayFSAParameters, so the factory must substitute
        // a correctly-typed default rather than passing null into the algorithm.
        var wrong = Factory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
        var alg = Factory.CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum.OneWayFSA, null!, wrong);
        await Assert.That(alg!.GetParameters().GetType().Name).IsEqualTo("OneWayFSAParameters");
    }

    [Test]
    public async Task CreateFSAA_AppliesGaps()
    {
        var alg = Factory.CreateFSAA<TVertex>(7f, 3f);
        await Assert.That(alg.Parameters.HorizontalGap).IsEqualTo(7f);
        await Assert.That(alg.Parameters.VerticalGap).IsEqualTo(3f);
    }

    [Test]
    public async Task FSA_Compute_RemovesOverlapsInPlace()
    {
        var a = new TVertex("A");
        var b = new TVertex("B");
        var c = new TVertex("C");
        // Three heavily overlapping rectangles.
        var rects = new Dictionary<TVertex, MRect>
        {
            [a] = new(0, 0, 50, 50),
            [b] = new(10, 10, 50, 50),
            [c] = new(20, 5, 50, 50),
        };

        var alg = Factory.CreateFSAA(rects, 0f, 0f);
        alg.Compute(CancellationToken.None);

        // The algorithm writes results back into the caller's dictionary.
        await Assert.That(alg.Rectangles).IsSameReferenceAs(rects);
        var list = rects.Values.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            // Sizes must be preserved; only positions change.
            await Assert.That(list[i].Width).IsEqualTo(50d);
            await Assert.That(list[i].Height).IsEqualTo(50d);
            for (var j = i + 1; j < list.Count; j++)
            {
                var overlapW = System.Math.Min(list[i].Right, list[j].Right) - System.Math.Max(list[i].Left, list[j].Left);
                var overlapH = System.Math.Min(list[i].Bottom, list[j].Bottom) - System.Math.Max(list[i].Top, list[j].Top);
                await Assert.That(overlapW <= 1e-6 || overlapH <= 1e-6).IsTrue();
            }
        }
    }

    [Test]
    public async Task FSA_Compute_WithGaps_KeepsMinimumSpacing()
    {
        var a = new TVertex("A");
        var b = new TVertex("B");
        var rects = new Dictionary<TVertex, MRect>
        {
            [a] = new(0, 0, 20, 20),
            [b] = new(5, 0, 20, 20),
        };

        var alg = Factory.CreateFSAA(rects, 10f, 10f);
        alg.Compute(CancellationToken.None);

        var ra = rects[a];
        var rb = rects[b];
        var gapX = System.Math.Max(rb.Left - ra.Right, ra.Left - rb.Right);
        var gapY = System.Math.Max(rb.Top - ra.Bottom, ra.Top - rb.Bottom);
        // After removing gaps, rectangles are separated by at least the configured gap on one axis.
        await Assert.That(gapX >= 10 - 1e-6 || gapY >= 10 - 1e-6).IsTrue();
        await Assert.That(ra.Width).IsEqualTo(20d);
        await Assert.That(rb.Width).IsEqualTo(20d);
    }

    [Test]
    public async Task FSA_Compute_NonOverlappingInput_IsUnchanged()
    {
        var a = new TVertex("A");
        var b = new TVertex("B");
        var rects = new Dictionary<TVertex, MRect>
        {
            [a] = new(0, 0, 10, 10),
            [b] = new(100, 100, 10, 10),
        };

        Factory.CreateFSAA(rects, 0f, 0f).Compute(CancellationToken.None);

        await Assert.That(rects[a]).IsEqualTo(new MRect(0, 0, 10, 10));
        await Assert.That(rects[b]).IsEqualTo(new MRect(100, 100, 10, 10));
    }

    #endregion

    #region Edge routing factory

    [Test]
    public async Task CreateEdgeRoutingAlgorithm_None_ReturnsNull()
    {
        var g = BuildTriangle(out _, out _, out _);
        var alg = Factory.CreateEdgeRoutingAlgorithm(EdgeRoutingAlgorithmTypeEnum.None, new MRect(0, 0, 100, 100), g,
            new Dictionary<TVertex, MPoint>(), new Dictionary<TVertex, MRect>());
        await Assert.That(alg).IsNull();
    }

    [Test]
    [Arguments(EdgeRoutingAlgorithmTypeEnum.SimpleER, "SimpleEdgeRouting")]
    [Arguments(EdgeRoutingAlgorithmTypeEnum.Bundling, "BundleEdgeRouting")]
    [Arguments(EdgeRoutingAlgorithmTypeEnum.PathFinder, "PathFinderEdgeRouting")]
    public async Task CreateEdgeRoutingAlgorithm_ReturnsMatchingType(EdgeRoutingAlgorithmTypeEnum type, string expectedPrefix)
    {
        var g = BuildTriangle(out var a, out var b, out var c);
        var positions = new Dictionary<TVertex, MPoint> { [a] = new(0, 0), [b] = new(100, 0), [c] = new(50, 100) };
        var rects = positions.ToDictionary(kv => kv.Key, kv => new MRect(kv.Value, new MSize(10, 10)));

        var alg = Factory.CreateEdgeRoutingAlgorithm(type, new MRect(0, 0, 200, 200), g, positions, rects);

        await Assert.That(alg).IsNotNull();
        await Assert.That(alg!.GetType().Name.StartsWith(expectedPrefix)).IsTrue();
    }

    #endregion
}
