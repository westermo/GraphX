using Avalonia;

namespace Westermo.GraphX.Controls.Avalonia.Models
{
    /// <summary>
    /// Factory class responsible for VertexControl and EdgeControl objects creation
    /// </summary>
    public class GraphControlFactory(GraphAreaBase graphArea) : IGraphControlFactory
    {
        public virtual EdgeControl CreateEdgeControl(VertexControl source, VertexControl target, object edge,
            bool showArrows = true, bool isVisible = true)
        {
            var control = new EdgeControl(source, target, edge, showArrows) { RootArea = FactoryRootArea };
            control.SetCurrentValue(Visual.IsVisibleProperty, isVisible);
            return control;
        }

        public virtual VertexControl CreateVertexControl(object vertexData)
        {
            return new VertexControl(vertexData) { RootArea = FactoryRootArea };
        }


        public GraphAreaBase FactoryRootArea { get; set; } = graphArea;
    }
}