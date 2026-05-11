using Avalonia;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls.Controls;

public static class ConnectionPointExtensions
{
    
    extension(IVertexConnectionPoint cp)
    {
        public Point GetEndpoint(Point center, Point target)
        {
            // If the connection point (cp) doesn't have any shape, the edge comes from its center, otherwise find the location
            // on its perimeter that the edge should come from.
            return cp.Shape == VertexShape.None
                ? center
                : GeometryHelper.GetEdgeEndpoint(center, cp.RectangularSize, target, cp.Shape);
        }
    }
}