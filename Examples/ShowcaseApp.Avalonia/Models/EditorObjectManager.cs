using System;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using ShowcaseApp.Avalonia.ExampleModels;

using Westermo.GraphX.Controls.Controls;
using Westermo.GraphX.Controls.Controls.ZoomControl;
using Westermo.GraphX.Controls.Models;

namespace ShowcaseApp.Avalonia.Models;

public class EditorObjectManager : IDisposable
{
    private GraphAreaExample? _graphArea;
    private ZoomControl? _zoomControl;
    private EdgeBlueprint? _edgeBp;
    private readonly Brush _edgeBrush;

    public EditorObjectManager(GraphAreaExample graphArea, ZoomControl zc)
    {
        _graphArea = graphArea;
        _zoomControl = zc;
        _zoomControl.PointerMoved += _zoomControl_MouseMove;
        _edgeBrush = new LinearGradientBrush
        {
            GradientStops = new GradientStops
            {
                new GradientStop(Colors.DeepSkyBlue, 0),
                new GradientStop(Colors.MediumPurple, 1)
            }
        };
    }

    public void CreateVirtualEdge(VertexControl source, Point mousePos)
    {
        _edgeBp = new EdgeBlueprint(source, _edgeBrush);
        _graphArea!.InsertCustomChildControl(0, _edgeBp.EdgePath);
    }

    private void _zoomControl_MouseMove(object? sender, PointerEventArgs e)
    {
        if (_edgeBp == null) return;
        if (_graphArea is null) return;
        var pt = _zoomControl!.TranslatePoint(e.GetPosition(_zoomControl), _graphArea);
        if (pt == null) return;
        var pos = pt.Value + new Vector(2, 2);
        _edgeBp.UpdateTargetPosition(pos);
    }

    private void ClearEdgeBp()
    {
        if (_edgeBp == null) return;
        _graphArea?.RemoveCustomChildControl(_edgeBp.EdgePath);
        _edgeBp.Dispose();
        _edgeBp = null;
    }

    public void Dispose()
    {
        ClearEdgeBp();
        _graphArea = null;
        if (_zoomControl != null)
            _zoomControl.PointerMoved -= _zoomControl_MouseMove;
        _zoomControl = null;
    }

    public void DestroyVirtualEdge()
    {
        ClearEdgeBp();
    }
}

public class EdgeBlueprint : IDisposable
{
    public VertexControl? Source { get; set; }
    public Point TargetPos { get; set; }
    public Path EdgePath { get; set; }

    public EdgeBlueprint(VertexControl source, Brush brush)
    {
        EdgePath = new Path
        {
            Stroke = brush,
            Data = new PathGeometry()
        };
        Source = source;
        Source.PositionChanged += Source_PositionChanged;
    }

    private void Source_PositionChanged(object sender, VertexPositionEventArgs args)
    {
        if (Source == null) return;
        UpdateGeometry(Source.GetCenterPosition(), TargetPos);
    }

    internal void UpdateTargetPosition(Point point)
    {
        TargetPos = point;
        if (Source == null) return;
        UpdateGeometry(Source.GetCenterPosition(), point);
    }

    private void UpdateGeometry(Point start, Point end)
    {
        var pg = new PathGeometry();
        var fig = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
        fig.Segments?.Add(new LineSegment { Point = end });
        pg.Figures?.Add(fig);
        EdgePath.Data = pg;
    }

    public void Dispose()
    {
        if (Source is null) return;
        Source.PositionChanged -= Source_PositionChanged;
        Source = null;
    }
}