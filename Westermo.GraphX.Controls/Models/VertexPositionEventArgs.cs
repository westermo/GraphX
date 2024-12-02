using System;
using System.Windows;

namespace Westermo.GraphX.Controls.Models
{
    public sealed class VertexPositionEventArgs(Point offset, Point pos, VertexControlBase vc) : EventArgs
    {
        /// <summary>
        /// Vertex control
        /// </summary>
        public VertexControlBase VertexControl { get; private set; } = vc;

        /// <summary>
        /// Attached coordinates X and Y 
        /// </summary>
        public Point Position { get; private set; } = pos;

        /// <summary>
        /// Offset of the vertex control within the GraphArea
        /// </summary>
        public Point OffsetPosition { get; private set; } = offset;
    }
}
