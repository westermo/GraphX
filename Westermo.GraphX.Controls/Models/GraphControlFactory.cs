using System.Windows;

namespace Westermo.GraphX.Controls.Models
{
    /// <summary>
    /// Factory class responsible for VertexControl and EdgeControl objects creation
    /// </summary>
    public class GraphControlFactory(GraphAreaBase graphArea) : IGraphControlFactory
    {
        public virtual EdgeControl CreateEdgeControl(VertexControl source, VertexControl target, object edge, bool showArrows = true, Visibility visibility = Visibility.Visible)
        {
            var edgectrl = new EdgeControl(source, target, edge, showArrows) { RootArea = FactoryRootArea};
            edgectrl.SetCurrentValue(UIElement.VisibilityProperty, visibility);
            return edgectrl;

        }

        public virtual VertexControl CreateVertexControl(object vertexData)
        {
            return new VertexControl(vertexData) {RootArea = FactoryRootArea};
        }


        public GraphAreaBase FactoryRootArea { get; set; } = graphArea;
    }
}
