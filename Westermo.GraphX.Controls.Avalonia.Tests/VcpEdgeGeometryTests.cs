using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Models;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

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

    private static void EnsureEdgeTemplate(EdgeControl ec)
    {
        if (ec.Template == null)
        {
            var content = new Grid();
            var path = new global::Avalonia.Controls.Shapes.Path()
            {
                Name = "PART_edgePath",
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            content.Children.Add(path);
            var ns = new NameScope();
            ns.Register("PART_edgePath", path);
            var functor = new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
            ec.Template = new ControlTemplate
            {
                TargetType = typeof(EdgeControl),
                Content = functor
            };
        }

        ec.ApplyTemplate();
    }

    private static StaticVertexConnectionPoint EnsureVertexTemplateWithCp(VertexControl vc, int cpId,
        VertexShape cpShape)
    {
        if (vc.Template == null)
        {
            var content = new Grid();
            var panel = new StackPanel() { Name = "PART_vcproot" };
            var cp = new StaticVertexConnectionPoint
            {
                Id = cpId,
                Width = 20,
                Height = 20,
                Shape = cpShape
            };
            panel.Children.Add(cp);
            content.Children.Add(panel);
            var ns = new NameScope();
            ns.Register("PART_vcproot", panel);
            var functor = new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
            vc.Template = new ControlTemplate
            {
                TargetType = typeof(VertexControl),
                Content = functor
            };
        }

        vc.ApplyTemplate();
        // After template apply, VCP should be discoverable.
        var cpInstance = vc.VertexConnectionPointsList.Values.First() as StaticVertexConnectionPoint;
        // Force layout update for accurate RectangularSize
        cpInstance!.Update();
        return cpInstance;
    }

    private (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, VertexControl vc1, VertexControl vc2,
        StaticVertexConnectionPoint cp1, StaticVertexConnectionPoint cp2, TEdge edge, EdgeControl ec)
        CreateAreaWithVcp(VertexShape sourceCpShape, VertexShape targetCpShape, bool bothEndpoints)
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        var e = new TEdge(v1, v2);
        g.AddEdge(e);

        e.SourceConnectionPointId = 1;
        if (bothEndpoints)
        {
            e.TargetConnectionPointId = 1;
        }

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();

        var vc1 = area.VertexList[v1];
        var vc2 = area.VertexList[v2];
        vc1.Width = 40;
        vc1.Height = 30;
        vc1.SetPosition(50, 80);
        GraphAreaBase.SetFinalX(vc1, 50);
        GraphAreaBase.SetFinalY(vc1, 80);
        vc2.Width = 40;
        vc2.Height = 30;
        vc2.SetPosition(200, 80);
        GraphAreaBase.SetFinalX(vc2, 200);
        GraphAreaBase.SetFinalY(vc2, 80);

        var cp1 = EnsureVertexTemplateWithCp(vc1, 1, sourceCpShape);
        StaticVertexConnectionPoint cp2;
        if (bothEndpoints)
            cp2 = EnsureVertexTemplateWithCp(vc2, 1, targetCpShape);
        else
        {
            // Simple vertex template with no VCPs for target
            if (vc2.Template == null)
            {
                var content = new Grid();
                var panel = new StackPanel() { Name = "PART_vcproot" };
                content.Children.Add(panel);
                var ns = new NameScope();
                ns.Register("PART_vcproot", panel);
                var functor =
                    new Func<IServiceProvider?, object?>(provider => new TemplateResult<Control>(content, ns));
                vc2.Template = new ControlTemplate
                {
                    TargetType = typeof(VertexControl),
                    Content = functor
                };
            }

            vc2.ApplyTemplate();
            cp2 = null!; // unused
        }

        var ec = area.ControlFactory.CreateEdgeControl(vc1, vc2, e);
        EnsureEdgeTemplate(ec);
        area.AddEdge(e, ec);
        ec.UpdateEdge();

        return (area, vc1, vc2, cp1, cp2, e, ec);
    }

    [Test]
    public async Task EdgeGeometry_UsesConnectionPointCirclePerimeter()
    {
        var data = CreateAreaWithVcp(VertexShape.Circle, VertexShape.Circle, bothEndpoints: true);
        var cp1 = data.cp1;
        var cp2 = data.cp2;
        var ec = data.ec;

        var sourceCenter = new Point(data.vc1.GetPosition().X + cp1.Width / 2,
            data.vc1.GetPosition().Y + cp1.Height / 2);
        var targetCenter = new Point(data.vc2.GetPosition().X + cp2.Width / 2,
            data.vc2.GetPosition().Y + cp2.Height / 2);
        var radius = Math.Max(cp1.Width, cp1.Height) / 2;

        var expectedSource = new Point(sourceCenter.X + radius, sourceCenter.Y);
        var expectedTarget = new Point(targetCenter.X - radius, targetCenter.Y);

        var actualSource = ec.SourceEndpoint.GetValueOrDefault();
        var actualTarget = ec.TargetEndpoint.GetValueOrDefault();

        double eps = 0.01;
        await Assert.That(actualSource.X).IsBetween(expectedSource.X - eps, expectedSource.X + eps);
        await Assert.That(actualSource.Y).IsBetween(expectedSource.Y - eps, expectedSource.Y + eps);
        await Assert.That(actualTarget.X).IsBetween(expectedTarget.X - eps, expectedTarget.X + eps);
        await Assert.That(actualTarget.Y).IsBetween(expectedTarget.Y - eps, expectedTarget.Y + eps);

        // Ensure endpoints are not at centers (since shape is circle)
        await Assert.That(actualSource.X != sourceCenter.X || actualSource.Y != sourceCenter.Y).IsTrue();
        await Assert.That(actualTarget.X != targetCenter.X || actualTarget.Y != targetCenter.Y).IsTrue();
    }

    [Test]
    public async Task EdgeGeometry_UsesConnectionPointCenterWhenShapeNone()
    {
        var data = CreateAreaWithVcp(VertexShape.None, VertexShape.None, bothEndpoints: true);
        var cp1 = data.cp1;
        var cp2 = data.cp2;
        var ec = data.ec;

        var sourceCenter = new Point(data.vc1.GetPosition().X + cp1.Width / 2,
            data.vc1.GetPosition().Y + cp1.Height / 2);
        var targetCenter = new Point(data.vc2.GetPosition().X + cp2.Width / 2,
            data.vc2.GetPosition().Y + cp2.Height / 2);

        var actualSource = ec.SourceEndpoint.GetValueOrDefault();
        var actualTarget = ec.TargetEndpoint.GetValueOrDefault();

        double eps = 0.01;
        await Assert.That(actualSource.X).IsBetween(sourceCenter.X - eps, sourceCenter.X + eps);
        await Assert.That(actualSource.Y).IsBetween(sourceCenter.Y - eps, sourceCenter.Y + eps);
        await Assert.That(actualTarget.X).IsBetween(targetCenter.X - eps, targetCenter.X + eps);
        await Assert.That(actualTarget.Y).IsBetween(targetCenter.Y - eps, targetCenter.Y + eps);
    }
}