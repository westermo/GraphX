using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Common.Models
{
    public class EdgeRoutingVisualData
    {
        public bool HaveTemplate { get; set; }
        public bool IsEdgeVisible { get; set; }
        public bool IsEdgeSelfLooped { get; set; }

        public Point SourcePosition { get; set; }
        public Size SourceSize { get; set; }
        public Point TargetPosition { get; set; }
        public Size TargetSize { get; set; }

        private IRoutingInfo Edge { get; set; }
    }
}
