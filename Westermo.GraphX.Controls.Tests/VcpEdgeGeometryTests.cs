using System.Windows;
using TUnit.Core.Executors;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

[TestExecutor<STAThreadExecutor>]
public class VcpEdgeGeometryTests
{
    public class TVertex(string name) : VertexBase
    {
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    public class TEdge(TVertex s, TVertex t) : EdgeBase<TVertex>(s, t)
    {
        public override Westermo.GraphX.Measure.Point[]? RoutingPoints { get; set; } = null;
    }


    // [Test]
    // public async Task EdgeGeometry_UsesConnectionPointCirclePerimeter()
    // {
    //     var data = BaseHelpers.CreateAreaWithVcp(VertexShape.Circle, VertexShape.Circle, bothEndpoints: true);
    //     var cp1 = data.cp1;
    //     var cp2 = data.cp2;
    //     var ec = data.ec;
    //
    //     var sourceCenter = new Point(data.vc1.GetPosition().X + cp1.Width / 2,
    //         data.vc1.GetPosition().Y + cp1.Height / 2);
    //     var targetCenter = new Point(data.vc2.GetPosition().X + cp2.Width / 2,
    //         data.vc2.GetPosition().Y + cp2.Height / 2);
    //     var radius = Math.Max(cp1.Width, cp1.Height) / 2;
    //
    //     var expectedSource = new Point(sourceCenter.X + radius, sourceCenter.Y);
    //     var expectedTarget = new Point(targetCenter.X - radius, targetCenter.Y);
    //
    //     var actualSource = ec.SourceEndpoint.GetValueOrDefault();
    //     var actualTarget = ec.TargetEndpoint.GetValueOrDefault();
    //
    //     double eps = 0.01;
    //     await Assert.That(actualSource.X).IsBetween(expectedSource.X - eps, expectedSource.X + eps);
    //     await Assert.That(actualSource.Y).IsBetween(expectedSource.Y - eps, expectedSource.Y + eps);
    //     await Assert.That(actualTarget.X).IsBetween(expectedTarget.X - eps, expectedTarget.X + eps);
    //     await Assert.That(actualTarget.Y).IsBetween(expectedTarget.Y - eps, expectedTarget.Y + eps);
    //
    //     // Ensure endpoints are not at centers (since shape is circle)
    //     await Assert.That(actualSource.X != sourceCenter.X || actualSource.Y != sourceCenter.Y).IsTrue();
    //     await Assert.That(actualTarget.X != targetCenter.X || actualTarget.Y != targetCenter.Y).IsTrue();
    // }
    //
    // [Test]
    // public async Task EdgeGeometry_UsesConnectionPointCenterWhenShapeNone()
    // {
    //     var (_, vc1, vc2, cp1, cp2, _, ec) = BaseHelpers.CreateAreaWithVcp(VertexShape.None, VertexShape.None, bothEndpoints: true);
    //
    //     var sourceCenter = new Point(vc1.GetPosition().X + cp1.Width / 2,
    //         vc1.GetPosition().Y + cp1.Height / 2);
    //     var targetCenter = new Point(vc2.GetPosition().X + cp2.Width / 2,
    //         vc2.GetPosition().Y + cp2.Height / 2);
    //
    //     var actualSource = ec.SourceEndpoint.GetValueOrDefault();
    //     var actualTarget = ec.TargetEndpoint.GetValueOrDefault();
    //
    //     double eps = 0.01;
    //     await Assert.That(actualSource.X).IsBetween(sourceCenter.X - eps, sourceCenter.X + eps);
    //     await Assert.That(actualSource.Y).IsBetween(sourceCenter.Y - eps, sourceCenter.Y + eps);
    //     await Assert.That(actualTarget.X).IsBetween(targetCenter.X - eps, targetCenter.X + eps);
    //     await Assert.That(actualTarget.Y).IsBetween(targetCenter.Y - eps, targetCenter.Y + eps);
    // }
}