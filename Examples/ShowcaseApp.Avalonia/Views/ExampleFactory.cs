using System;
using ShowcaseApp.Avalonia.Pages;
using Westermo.GraphX;
using Westermo.GraphX.Controls.Avalonia;
using Westermo.GraphX.Controls.Avalonia.Models;

namespace ShowcaseApp.Avalonia.Views;

internal class ExampleFactory : IGraphControlFactory
{
    public GraphAreaBase FactoryRootArea => throw new NotImplementedException();

    public EdgeControl CreateEdgeControl(VertexControl source, VertexControl target, object edge,
        bool showArrows = true, bool isVisible = true)
    {
        return new FunnyEdgeControl
        {
            Source = source,
            Target = target,
            Edge = edge,
            ShowArrows = showArrows,
            IsVisible = isVisible
        };
    }

    public VertexControl CreateVertexControl(object vertexData)
    {
        return new VertexControl(vertexData);
    }
}