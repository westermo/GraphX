using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Common.Models
{

    public class WeightedEdge<TVertex>(TVertex source, TVertex target, double weight) : IWeightedEdge<TVertex>
    {
		public double Weight { get; set; } = weight;

		public WeightedEdge(TVertex source, TVertex target)
			: this(source, target, 1) {}

		/// <summary>
        /// Source vertex data
        /// </summary>
        public TVertex Source { get; set; } = source;

        /// <summary>
        /// Target vertex data
        /// </summary>
        public TVertex Target { get; set; } = target;

        /// <summary>
        /// Update vertices (probably needed for serialization TODO)
        /// </summary>
        /// <param name="source">Source vertex data</param>
        /// <param name="target">Target vertex data</param>
        public void UpdateVertices(TVertex source, TVertex target)
        {
            Source = source; Target = target;
        }

    }
}