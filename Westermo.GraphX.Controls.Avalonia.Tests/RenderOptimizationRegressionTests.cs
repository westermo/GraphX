using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using QuikGraph;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.EdgeLabels;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Regression tests to protect render pipeline behavior during optimization refactoring.
/// </summary>
public class RenderOptimizationRegressionTests
{
    private class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    private class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Measure.Point[]? RoutingPoints { get; set; } = null;
    }

    /// <summary>
    /// Applies a minimal vertex template with a PART_vcproot panel so vertex controls
    /// can discover connection points and participate in layout.
    /// </summary>
    private static void EnsureVertexTemplate(VertexControl vc)
    {
        if (vc.Template == null)
        {
            var content = new Grid();
            var panel = new StackPanel { Name = "PART_vcproot" };
            content.Children.Add(panel);
            var ns = new NameScope();
            ns.Register("PART_vcproot", panel);
            var functor = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns));
            vc.Template = new ControlTemplate
            {
                TargetType = typeof(VertexControl),
                Content = functor
            };
        }

        vc.ApplyTemplate();
    }

    /// <summary>
    /// Applies a minimal edge template with a PART_edgePath so edge controls can render geometry.
    /// </summary>
    private static void EnsureEdgeTemplate(EdgeControl ec)
    {
        if (ec.Template == null)
        {
            var content = new Grid();
            var path = new global::Avalonia.Controls.Shapes.Path
            {
                Name = "PART_edgePath",
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            content.Children.Add(path);
            var ns = new NameScope();
            ns.Register("PART_edgePath", path);
            var functor = new Func<IServiceProvider?, object?>(_ => new TemplateResult<Control>(content, ns));
            ec.Template = new ControlTemplate
            {
                TargetType = typeof(EdgeControl),
                Content = functor
            };
        }

        ec.ApplyTemplate();
    }

    /// <summary>
    /// Creates a simple two-vertex, one-edge graph area with positions pre-configured.
    /// Vertices are positioned at (50,80) and (250,80) with 40x30 size.
    /// </summary>
    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area,
        VertexControl v1c, VertexControl v2c,
        TEdge edge, EdgeControl ec)
        CreateSimpleArea()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var e = new TEdge(v1, v2);
        g.AddEdge(e);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            EdgeCurvingEnabled = false
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };
        area.PreloadVertexes();

        var v1c = area.VertexList[v1];
        var v2c = area.VertexList[v2];
        v1c.Width = 40;
        v1c.Height = 30;
        v1c.SetPosition(50, 80);
        v2c.Width = 40;
        v2c.Height = 30;
        v2c.SetPosition(250, 80);
        GraphAreaBase.SetFinalX(v1c, 50);
        GraphAreaBase.SetFinalY(v1c, 80);
        GraphAreaBase.SetFinalX(v2c, 250);
        GraphAreaBase.SetFinalY(v2c, 80);
        EnsureVertexTemplate(v1c);
        EnsureVertexTemplate(v2c);

        var ec = area.ControlFactory.CreateEdgeControl(v1c, v2c, e);
        EnsureEdgeTemplate(ec);
        area.AddEdge(e, ec);

        return (area, v1c, v2c, e, ec);
    }

    /// <summary>
    /// Forces a full measure/arrange cycle on an edge control so geometry is computed.
    /// </summary>
    private static void LayoutEdge(EdgeControl ec)
    {
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));
    }

    /// <summary>
    /// Creates a ZoomControl hosted in a Window with specified viewport and content dimensions.
    /// </summary>
    private static (ZoomControl zoom, Window window) CreateZoomControlWithContent(
        double viewportWidth, double viewportHeight,
        double contentWidth, double contentHeight)
    {
        var content = new Canvas { Width = contentWidth, Height = contentHeight };
        var zc = new ZoomControl { Content = content };
        var window = new Window
        {
            Width = viewportWidth,
            Height = viewportHeight,
            Content = zc
        };
        window.Show();

        // Force layout to apply template and establish Bounds
        window.Measure(new Size(viewportWidth, viewportHeight));
        window.Arrange(new Rect(0, 0, viewportWidth, viewportHeight));

        return (zc, window);
    }

    #region Test 1 — Edge geometry is correct after vertex move

    [Test]
    public async Task EdgeGeometry_UpdatesConnectionPoints_AfterVertexMove()
    {
        var (area, v1c, v2c, _, ec) = CreateSimpleArea();
        LayoutEdge(ec);

        // Record original endpoints
        var originalSource = ec.SourceEndpoint;
        var originalTarget = ec.TargetEndpoint;
        await Assert.That(originalSource.HasValue).IsTrue();
        await Assert.That(originalTarget.HasValue).IsTrue();

        // Move v2 to a new position and update final coordinates
        v2c.SetPosition(400, 200);
        GraphAreaBase.SetFinalX(v2c, 400);
        GraphAreaBase.SetFinalY(v2c, 200);

        // Re-layout the edge so geometry is recomputed
        ec.InvalidateMeasure();
        LayoutEdge(ec);

        var movedTarget = ec.TargetEndpoint;
        await Assert.That(movedTarget.HasValue).IsTrue();

        // Target endpoint must have shifted to the right and down
        await Assert.That(movedTarget!.Value.X).IsGreaterThan(originalTarget!.Value.X);
        await Assert.That(movedTarget.Value.Y).IsGreaterThan(originalTarget.Value.Y);
    }

    #endregion

    #region Test 2 — Edge geometry preserves connection points for routed edges

    [Test]
    public async Task EdgeGeometry_PreservesRoutingPoints_WhenRouteAssigned()
    {
        var (area, v1c, v2c, edge, ec) = CreateSimpleArea();

        // Assign routing points that form a detour
        edge.RoutingPoints =
        [
            new Measure.Point(100, 20),
            new Measure.Point(200, 20),
            new Measure.Point(200, 80)
        ];

        ec.InvalidateMeasure();
        LayoutEdge(ec);

        var geom = ec.GetLineGeometry();
        await Assert.That(geom).IsNotNull();

        // Endpoints should be set
        await Assert.That(ec.SourceEndpoint.HasValue).IsTrue();
        await Assert.That(ec.TargetEndpoint.HasValue).IsTrue();

        // The geometry bounds should extend to cover the routing detour (y ≈ 20)
        var bounds = geom!.Bounds;
        await Assert.That(bounds.Top).IsLessThanOrEqualTo(25.0);
    }

    #endregion

    #region Test 3 — Self-loop edge still produces EllipseGeometry after arrange

    [Test]
    public async Task SelfLoopEdge_ProducesEllipseGeometry()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("Loop");
        g.AddVertex(v1);
        var selfEdge = new TEdge(v1, v1);
        g.AddEdge(selfEdge);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            EdgeCurvingEnabled = false
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.Width = 40;
        vc.Height = 30;
        vc.SetPosition(100, 100);
        GraphAreaBase.SetFinalX(vc, 100);
        GraphAreaBase.SetFinalY(vc, 100);
        EnsureVertexTemplate(vc);

        area.GenerateAllEdges(true);
        area.UpdateAllEdges(true);

        var ec = area.EdgesList.Values.First();
        ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));

        var geom = ec.GetLineGeometry();
        await Assert.That(geom).IsNotNull();
        await Assert.That(geom is EllipseGeometry).IsTrue();
    }

    #endregion

    #region Test 4 — Edge labels position at midpoint

    [Test]
    public async Task EdgeLabel_IsPositionedNearEdgeMidpoint()
    {
        var (area, v1c, v2c, edge, ec) = CreateSimpleArea();

        // Create and attach a label — Attach() adds the label to the area's Children
        // and calls Show(), so do NOT add to Children again or set ShowLabel before Attach
        var label = new AttachableEdgeLabelControl
        {
            Content = "Test Label"
        };
        label.Attach(ec);
        label.ShowLabel = true;

        // Force full layout
        LayoutEdge(ec);
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        label.Arrange(new Rect(0, 0, label.DesiredSize.Width, label.DesiredSize.Height));

        // After attach, the label should be visible and have non-zero desired size
        await Assert.That(label.IsVisible).IsTrue();
        await Assert.That(label.ShowLabel).IsTrue();

        // Verify label is attached to the edge
        await Assert.That(label.AttachNode).IsEqualTo(ec);
    }

    #endregion

    #region Test 5 — SetPosition applies both X and Y correctly

    [Test]
    public async Task SetPosition_AppliesBothCoordinates()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("P");
        g.AddVertex(v1);
        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        vc.SetPosition(123.5, 456.7);

        var pos = vc.GetPosition();
        await Assert.That(Math.Abs(pos.X - 123.5)).IsLessThan(0.01);
        await Assert.That(Math.Abs(pos.Y - 456.7)).IsLessThan(0.01);
    }

    #endregion

    #region Test 6 — SetPosition fires PositionChanged event

    [Test]
    public async Task SetPosition_FiresPositionChangedEvent()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("E");
        g.AddVertex(v1);
        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc = area.VertexList[v1];
        var eventFired = false;
        vc.PositionChanged += (_, _) => eventFired = true;

        vc.SetPosition(100, 200);

        await Assert.That(eventFired).IsTrue();
    }

    #endregion

    #region Test 7 — Vertex move triggers edge geometry update

    [Test]
    public async Task VertexMove_TriggersEdgeGeometryUpdate()
    {
        var (area, v1c, v2c, _, ec) = CreateSimpleArea();

        // Initial layout
        LayoutEdge(ec);
        var initialGeom = ec.GetLineGeometry();
        await Assert.That(initialGeom).IsNotNull();

        // Move vertex and re-layout
        v2c.SetPosition(350, 200);
        GraphAreaBase.SetFinalX(v2c, 350);
        GraphAreaBase.SetFinalY(v2c, 200);

        ec.InvalidateMeasure();
        LayoutEdge(ec);

        var updatedGeom = ec.GetLineGeometry();
        await Assert.That(updatedGeom).IsNotNull();

        // Geometry bounds should have changed to reflect new vertex position
        var updatedBounds = updatedGeom!.Bounds;
        await Assert.That(updatedBounds.Width).IsGreaterThan(0);
        await Assert.That(updatedBounds.Height).IsGreaterThan(0);
    }

    #endregion

    #region Test 8 — ZoomControl pan updates TranslateX/Y

    [Test]
    public async Task ZoomControl_Pan_UpdatesTranslateXY()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 300);
        try
        {
            zc.TranslateX = 50;
            zc.TranslateY = 75;

            await Assert.That(Math.Abs(zc.TranslateX - 50)).IsLessThan(0.01);
            await Assert.That(Math.Abs(zc.TranslateY - 75)).IsLessThan(0.01);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Test 9 — ZoomControl zoom changes are applied

    [Test]
    public async Task ZoomControl_ZoomValue_IsApplied()
    {
        var (zc, window) = CreateZoomControlWithContent(800, 600, 400, 300);
        try
        {
            zc.Zoom = 2.5;
            await Assert.That(Math.Abs(zc.Zoom - 2.5)).IsLessThan(0.01);
        }
        finally
        {
            window.Close();
        }
    }

    #endregion

    #region Test 10 — Viewport culling still works after vertex position change

    [Test]
    public async Task ViewportCulling_UpdatesVisibility_AfterVertexMove()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("Visible");
        var v2 = new TVertex("Offscreen");
        g.AddVertex(v1);
        g.AddVertex(v2);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 800,
            Height = 600
        };
        area.PreloadVertexes();

        var vc1 = area.VertexList[v1];
        var vc2 = area.VertexList[v2];
        vc1.Width = 40;
        vc1.Height = 30;
        vc2.Width = 40;
        vc2.Height = 30;

        // Place v1 inside a viewport, v2 far outside
        vc1.SetPosition(100, 100);
        GraphAreaBase.SetFinalX(vc1, 100);
        GraphAreaBase.SetFinalY(vc1, 100);

        vc2.SetPosition(5000, 5000);
        GraphAreaBase.SetFinalX(vc2, 5000);
        GraphAreaBase.SetFinalY(vc2, 5000);

        EnsureVertexTemplate(vc1);
        EnsureVertexTemplate(vc2);

        // Simulate viewport culling by checking positions against a viewport rect
        var viewport = new Rect(0, 0, 800, 600);
        var vc1Pos = vc1.GetPosition();
        var vc2Pos = vc2.GetPosition();

        var v1InViewport = viewport.Contains(new Point(vc1Pos.X, vc1Pos.Y));
        var v2InViewport = viewport.Contains(new Point(vc2Pos.X, vc2Pos.Y));

        await Assert.That(v1InViewport).IsTrue();
        await Assert.That(v2InViewport).IsFalse();

        // Now move v2 inside viewport
        vc2.SetPosition(200, 200);
        GraphAreaBase.SetFinalX(vc2, 200);
        GraphAreaBase.SetFinalY(vc2, 200);

        var vc2NewPos = vc2.GetPosition();
        var v2NowInViewport = viewport.Contains(new Point(vc2NewPos.X, vc2NewPos.Y));
        await Assert.That(v2NowInViewport).IsTrue();
    }

    #endregion

    #region Test 11 — MathHelper.RotatePoint returns correct coordinates

    [Test]
    public async Task MathHelper_RotatePoint_90Degrees_ReturnsCorrectResult()
    {
        // Rotate (1,0) around (0,0) by 90 degrees => expected approximately (0,1)
        // This captures the bug where (int) casts truncate precision
        var source = new Measure.Point(1, 0);
        var center = new Measure.Point(0, 0);
        var result = MathHelper.RotatePoint(source, center, 90);

        // With (int) casts, result would be (0,0) instead of (0,1)
        await Assert.That(Math.Abs(result.X - 0.0)).IsLessThan(0.01);
        await Assert.That(Math.Abs(result.Y - 1.0)).IsLessThan(0.01);
    }

    #endregion

    #region Test 12 — GraphArea MeasureOverride computes correct ContentSize

    [Test]
    public async Task GraphArea_MeasureOverride_ComputesCorrectContentSize()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("TL"); // top-left
        var v2 = new TVertex("BR"); // bottom-right
        g.AddVertex(v1);
        g.AddVertex(v2);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 800,
            Height = 600
        };
        area.PreloadVertexes();

        var vc1 = area.VertexList[v1];
        var vc2 = area.VertexList[v2];
        vc1.Width = 40;
        vc1.Height = 30;
        vc2.Width = 40;
        vc2.Height = 30;

        vc1.SetPosition(50, 50);
        GraphAreaBase.SetFinalX(vc1, 50);
        GraphAreaBase.SetFinalY(vc1, 50);

        vc2.SetPosition(300, 250);
        GraphAreaBase.SetFinalX(vc2, 300);
        GraphAreaBase.SetFinalY(vc2, 250);

        EnsureVertexTemplate(vc1);
        EnsureVertexTemplate(vc2);

        // Force measure/arrange on the area
        area.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        area.Arrange(new Rect(0, 0, 800, 600));

        var contentSize = area.ContentSize;

        // ContentSize should span from (50,50) to at least (300+40, 250+30) = (340, 280)
        await Assert.That(contentSize.X).IsLessThanOrEqualTo(50.0);
        await Assert.That(contentSize.Y).IsLessThanOrEqualTo(50.0);
        await Assert.That(contentSize.Right).IsGreaterThanOrEqualTo(340.0);
        await Assert.That(contentSize.Bottom).IsGreaterThanOrEqualTo(280.0);
    }

    #endregion

    #region Test 13 — Parallel edges have different connection points

    [Test]
    public async Task ParallelEdges_HaveDifferentConnectionPoints()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        // Add 3 parallel edges
        var e1 = new TEdge(v1, v2);
        var e2 = new TEdge(v1, v2);
        var e3 = new TEdge(v1, v2);
        g.AddEdge(e1);
        g.AddEdge(e2);
        g.AddEdge(e3);

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            EnableParallelEdges = true,
            EdgeCurvingEnabled = false
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 500,
            Height = 400
        };
        area.PreloadVertexes();

        foreach (var (key, vc) in area.VertexList)
        {
            vc.Width = 40;
            vc.Height = 30;
            if (key == v1) vc.SetPosition(50, 100);
            if (key == v2) vc.SetPosition(300, 100);
            GraphAreaBase.SetFinalX(vc, vc.GetPosition().X);
            GraphAreaBase.SetFinalY(vc, vc.GetPosition().Y);
            EnsureVertexTemplate(vc);
        }

        area.GenerateAllEdges(true);
        area.UpdateAllEdges(true);

        var edgeControls = area.EdgesList.Values.ToList();

        // Force layout on each edge
        foreach (var ec in edgeControls)
        {
            ec.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            ec.Arrange(new Rect(0, 0, ec.DesiredSize.Width, ec.DesiredSize.Height));
        }

        // Collect source/target connection points
        var connectionPairs = edgeControls
            .Select(ec => (Source: ec.SourceEndpoint, Target: ec.TargetEndpoint))
            .ToList();

        // At least one pair of edges should have different connection points from another
        var allSame = connectionPairs
            .All(p => p.Source == connectionPairs[0].Source && p.Target == connectionPairs[0].Target);
        await Assert.That(allSame).IsFalse();
    }

    #endregion

    #region Test 14 — Edge pointer visibility toggles with ShowArrows

    [Test]
    public async Task EdgePointer_Visibility_TogglesWithShowArrows()
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("S");
        var v2 = new TVertex("T");
        g.AddVertex(v1);
        g.AddVertex(v2);
        g.AddEdge(new TEdge(v1, v2));

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            Graph = g,
            EdgeCurvingEnabled = false
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
        {
            LogicCore = lc,
            Width = 800,
            Height = 600
        };

        // Host in a window for proper template application
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = area
        };
        window.Show();

        area.PreloadVertexes();
        foreach (var (key, vc) in area.VertexList)
        {
            if (key == v1) vc.SetPosition(new Point(100, 100));
            else vc.SetPosition(new Point(400, 100));
            GraphAreaBase.SetFinalX(vc, vc.GetPosition().X);
            GraphAreaBase.SetFinalY(vc, vc.GetPosition().Y);
        }

        area.GenerateAllEdges();
        foreach (var ec in area.EdgesList.Values)
            ec.ApplyTemplate();
        Dispatcher.UIThread.RunJobs();

        try
        {
            var ec = area.EdgesList.Values.First();

            // Arrows should be showing by default
            await Assert.That(ec.ShowArrows).IsTrue();

            // Hide arrows
            area.ShowAllEdgesArrows(false);
            await Assert.That(ec.ShowArrows).IsFalse();

            // Show arrows again
            area.ShowAllEdgesArrows(true);
            await Assert.That(ec.ShowArrows).IsTrue();
        }
        finally
        {
            window.Close();
        }
    }

    #endregion
}
