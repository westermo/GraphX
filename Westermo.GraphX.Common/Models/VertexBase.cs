using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Common.Models
{
    public abstract class VertexBase: IGraphXVertex
    {
        /// <summary>
        /// Gets or sets custom angle associated with the vertex
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Gets or sets optional group identificator
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Skip vertex in algo calc and visualization
        /// </summary>
        public ProcessingOptionEnum SkipProcessing { get; set; }

        /// <summary>
        /// Unique vertex ID
        /// </summary>
        public long ID { get; set; } = -1;

        public bool Equals(IGraphXVertex other)
        {
            return Equals(this, other);
        }
    }
}
