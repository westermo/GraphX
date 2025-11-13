using System.Windows.Media;
using TUnit.Core.Executors;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

[TestExecutor<STAThreadExecutor>]
public class EdgeControlAdvancedTests
{
    // [Test]
    // public async Task Edge_UpdateAfterVertexMove_ChangesEndpoints()
    // {
    //     var data = BaseHelpers.CreateSimpleArea();
    //     var ec = data.ec;
    //     var originalSource = ec.SourceEndpoint;
    //     var originalTarget = ec.TargetEndpoint;
    //     await Assert.That(originalSource.HasValue && originalTarget.HasValue).IsTrue();
    //
    //     // move target vertex to the right and update
    //     data.v2c.SetPosition(350, 120);
    //     GraphAreaBase.SetFinalX(data.v2c, 350);
    //     GraphAreaBase.SetFinalY(data.v2c, 120);
    //     ec.UpdateEdge(true);
    //
    //     var movedSource = ec.SourceEndpoint;
    //     var movedTarget = ec.TargetEndpoint;
    //     await Assert.That(movedTarget!.Value.X).IsGreaterThan(originalTarget!.Value.X);
    //     await Assert.That(movedTarget.Value.Y).IsNotEqualTo(originalTarget.Value.Y);
    //     // Source endpoint direction should also change slightly due to angle change
    //     await Assert.That(movedSource!.Value.Y).IsNotEqualTo(originalSource!.Value.Y);
    // }
    //
    // [Test]
    // public async Task Edge_ManualRoutingPoints_IncreasePathSegments()
    // {
    //     var (_, _, _, e, ec) = BaseHelpers.CreateSimpleArea();
    //     // baseline geometry
    //     var baseGeom = ec.GetLineGeometry() as PathGeometry;
    //     await Assert.That(baseGeom).IsNotNull();
    //     var baseSegCount = baseGeom!.Figures[0].Segments.Count;
    //
    //     // assign routing points to create a polyline with bends
    //     e.RoutingPoints =
    //     [
    //         new Measure.Point(150, 20),
    //         new Measure.Point(150, 30),
    //         new Measure.Point(200, 160)
    //     ];
    //     ec.UpdateEdge(true);
    //     var routedGeom = ec.GetLineGeometry() as PathGeometry;
    //     await Assert.That(routedGeom).IsNotNull();
    //     var routedSegCount = routedGeom!.Figures[0].Segments.Count;
    //     await Assert.That(routedSegCount).IsGreaterThan(baseSegCount);
    // }

    [Test]
    public async Task Edge_CurvingEnabled_StillProducesGeometry()
    {
        var data = BaseHelpers.CreateSimpleArea(curving: true);
        var geom = data.ec.GetLineGeometry();
        // We don’t assert exact segment types; just ensure geometry exists under curving mode
        await Assert.That(geom).IsNotNull();
    }
}