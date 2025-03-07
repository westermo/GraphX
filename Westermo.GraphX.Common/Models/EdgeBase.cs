﻿using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Common.Models
{
    /// <summary>
    /// Base class for graph edge
    /// </summary>
    /// <typeparam name="TVertex">Vertex class</typeparam>
    public abstract class EdgeBase<TVertex>(TVertex source, TVertex target, double weight = 1) : IGraphXEdge<TVertex>
    {
        /// <summary>
        /// Skip edge in algo calc and visualization
        /// </summary>
        public ProcessingOptionEnum SkipProcessing { get; set; }

        /// <summary>
        /// Unique edge ID
        /// </summary>
        public long ID { get; set; } = -1;

        /// <summary>
        /// Returns true if Source vertex equals Target vertex
        /// </summary>
        public bool IsSelfLoop => Source.Equals(Target);

        /// <summary>
        /// Optional parameter to bind edge to static vertex connection point
        /// </summary>
        public int? SourceConnectionPointId { get; set; }

        /// <summary>
        /// Optional parameter to bind edge to static vertex connection point
        /// </summary>
        public int? TargetConnectionPointId { get; set; }

        /// <summary>
        /// Routing points collection used to make Path visual object
        /// </summary>
        public virtual Point[] RoutingPoints { get; set; }

        /// <summary>
        /// Source vertex
        /// </summary>
        public TVertex Source { get; set; } = source;

        /// <summary>
        /// Target vertex
        /// </summary>
        public TVertex Target { get; set; } = target;

        /// <summary>
        /// Edge weight that can be used by some weight-related layout algorithms
        /// </summary>
        public double Weight { get; set; } = weight;

        /// <summary>
		/// Reverse the calculated routing path points.
		/// </summary>
		public bool ReversePath { get; set; }
    }
}
