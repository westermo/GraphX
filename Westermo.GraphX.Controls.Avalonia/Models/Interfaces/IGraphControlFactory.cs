using Westermo.GraphX.Controls.Controls;

namespace Westermo.GraphX.Controls.Models.Interfaces;

public interface IGraphControlFactory
{
    EdgeControl CreateEdgeControl(VertexControl source, VertexControl target, object edge, bool showArrows = true,
        bool isVisible = true);

    VertexControl CreateVertexControl(object vertexData);

    /// <summary>
    /// Root graph area for the factory
    /// </summary>
    GraphAreaBase FactoryRootArea { get; }
}