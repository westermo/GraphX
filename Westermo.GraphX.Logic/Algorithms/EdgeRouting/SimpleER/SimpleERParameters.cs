using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.EdgeRouting
{
    public class SimpleERParameters : IEdgeRoutingParameters
    {
        /// <summary>
        /// Get or set side step value when searching for way around vertex
        /// </summary>
        public double SideStep { get; set; } = 5;

        /// <summary>
        /// Get or set backward step when intersection is met
        /// </summary>
        public double BackStep { get; set; } = 10;
    }
}