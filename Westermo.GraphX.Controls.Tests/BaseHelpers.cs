using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QuikGraph;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Logic.Models;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class BaseHelpers
{
    public static GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> CreateArea(int edges = 1)
    {
        var g = new BidirectionalGraph<TVertex, TEdge>();
        var v1 = new TVertex("A");
        var v2 = new TVertex("B");
        g.AddVertex(v1);
        g.AddVertex(v2);
        for (int i = 0; i < edges; i++) g.AddEdge(new TEdge(v1, v2));

        var lc = new GXLogicCore<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { Graph = g };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> { LogicCore = lc };
        area.PreloadVertexes();
        // Assign positions & template
        foreach (var kv in area.VertexList)
        {
            kv.Value.Width = 40;
            kv.Value.Height = 30;
            kv.Value.SetPosition(kv.Key == v1 ? 50 : 200, 80);
            GraphAreaBase.SetFinalX(kv.Value, kv.Value.GetPosition().X);
            GraphAreaBase.SetFinalY(kv.Value, kv.Value.GetPosition().Y);
            BaseHelpers.EnsureVertexTemplate(kv.Value);
        }

        // create edges manually
        foreach (var e in g.Edges)
        {
            var ec = area.ControlFactory.CreateEdgeControl(area.VertexList[e.Source], area.VertexList[e.Target], e);
            BaseHelpers.EnsureEdgeTemplate(ec);
            area.AddEdge(e, ec);
            ec.UpdateEdge(true);
        }

        return area;
    }

    public static StaticVertexConnectionPoint EnsureVertexTemplateWithCp(VertexControl vc, int cpId,
        VertexShape cpShape)
    {
        var template = new ControlTemplate(typeof(VertexControl));
        var grid = new FrameworkElementFactory(typeof(Grid));
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(FrameworkElement.NameProperty, "PART_vcproot");
        var connection = new FrameworkElementFactory(typeof(StaticVertexConnectionPoint));
        connection.Name = "PART_connectionPoint";
        connection.SetValue(FrameworkElement.WidthProperty, 20.0);
        connection.SetValue(FrameworkElement.HeightProperty, 20.0);
        connection.SetValue(StaticVertexConnectionPoint.ShapeProperty, cpShape);
        connection.SetValue(StaticVertexConnectionPoint.IdProperty, cpId);
        panel.AppendChild(connection);
        grid.AppendChild(panel);
        template.VisualTree = grid;
        vc.Template = template;

        vc.ApplyTemplate();
        // After template apply, VCP should be discoverable.
        var cpInstance = vc.VertexConnectionPointsList.First() as StaticVertexConnectionPoint;
        // Force layout update for accurate RectangularSize
        cpInstance!.Update();
        return cpInstance;
    }

    public static (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, VertexControl vc1, VertexControl
        vc2,
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
                var template = new ControlTemplate(typeof(EdgeControl));
                var grid = new FrameworkElementFactory(typeof(Grid));
                var panel = new FrameworkElementFactory(typeof(StackPanel));
                panel.SetValue(FrameworkElement.NameProperty, "PART_vcproot");
                grid.AppendChild(panel);
                template.VisualTree = grid;
                vc2.Template = template;
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

    public static void EnsureVertexTemplate(VertexControl vc)
    {
        if (vc.Template == null)
        {
            var template = new ControlTemplate(typeof(VertexControl));
            var grid = new FrameworkElementFactory(typeof(Grid));
            var panel = new FrameworkElementFactory(typeof(StackPanel));
            panel.SetValue(FrameworkElement.NameProperty, "PART_vcproot");
            grid.AppendChild(panel);
            template.VisualTree = grid;
            vc.Template = template;
        }

        vc.ApplyTemplate();
    }

    public static void EnsureEdgeTemplate(EdgeControl ec)
    {
        if (ec.Template == null)
        {
            var template = new ControlTemplate(typeof(EdgeControl));
            var factory = new FrameworkElementFactory(typeof(Grid));
            var pathFactory = new FrameworkElementFactory(typeof(Path))
            {
                Name = "PART_edgePath"
            };
            pathFactory.SetValue(Shape.StrokeProperty, Brushes.Black);
            pathFactory.SetValue(Shape.StrokeThicknessProperty, 1.0);
            factory.AppendChild(pathFactory);
            template.VisualTree = factory;
            ec.Template = template;
        }

        ec.ApplyTemplate();
    }

    public static (GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>> area, VertexControl v1c, VertexControl
        v2c,
        TEdge e, EdgeControl ec)
        CreateSimpleArea(bool curving = false)
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
            EdgeCurvingEnabled = curving
        };
        var area = new GraphArea<TVertex, TEdge, BidirectionalGraph<TVertex, TEdge>>
            { LogicCore = lc, Width = 500, Height = 400 };
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
        ec.UpdateEdge(true);
        return (area, v1c, v2c, e, ec);
    }
}