namespace Westermo.GraphX.Controls.Avalonia.Models
{
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
}