using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.EdgeRouting
{
    public class PathFinderEdgeRoutingParameters : IEdgeRoutingParameters
    {
        /// <summary>
        /// Controls horizontal grid density. Smaller the value more points will be available.
        /// </summary>
        public double HorizontalGridSize { get; set; } = 100;

        /// <summary>
        /// Controls vertical grid density. Smaller the value more points will be available.
        /// </summary>
        public double VerticalGridSize { get; set; } = 100;

        /// <summary>
        /// Offset from the each side to enlarge grid and leave additional space for routing.
        /// </summary>
        public double SideGridOffset { get; set; } = 200;

        /// <summary>
        /// Use diagonal point connections while searching for the path.
        /// </summary>
        public bool UseDiagonals { get; set; } = true;

        /// <summary>
        /// Algorithm used to search for the path.
        /// </summary>
        public PathFindAlgorithm PathFinderAlgorithm { get; set; } = PathFindAlgorithm.Manhattan;

        /// <summary>
        /// Gets or sets if direction change is unpreferable
        /// </summary>
        public bool PunishChangeDirection { get; set; } = false;

        /// <summary>
        /// Gets or sets if diagonal shortcuts must be preferred
        /// </summary>
        public bool UseHeavyDiagonals { get; set; } = false;

        /// <summary>
        /// Heuristic level
        /// </summary>
        public int Heuristic { get; set; } = 2;

        /// <summary>
        /// Use special formula for tie breaking
        /// </summary>
        public bool UseTieBreaker { get; set; } = false;

        /// <summary>
        /// Maximum number of tries
        /// </summary>
        public int SearchTriesLimit { get; set; } = 50000;
    }

    public enum PathFindAlgorithm
    {
        Manhattan = 1,
        MaxDXDY = 2,
        DiagonalShortCut = 3,
        Euclidean = 4,
        EuclideanNoSQR = 5,
        Custom1 = 6
    }
}
