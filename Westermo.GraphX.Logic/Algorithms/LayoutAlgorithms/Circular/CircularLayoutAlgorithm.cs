using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Interfaces;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms
{
    public class CircularLayoutAlgorithm<TVertex, TEdge, TGraph>(
        TGraph visitedGraph,
        IDictionary<TVertex, Point> vertexPositions,
        IDictionary<TVertex, Size> vertexSizes,
        CircularLayoutParameters parameters)
        : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, CircularLayoutParameters>(visitedGraph,
            vertexPositions, parameters)
        where TVertex : class, IIdentifiableGraphDataObject
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        /// <summary>
        /// Gets if current algorithm supports vertex freeze feature (part of VAESPS)
        /// </summary>
        public override bool SupportsObjectFreeze => false;

        public override void ResetGraph(IEnumerable<TVertex> vertices, IEnumerable<TEdge> edges)
        {
            //
        }

        public override void Compute(CancellationToken cancellationToken)
        {
            //calculate the size of the circle
            double perimeter = 0;
            var usableVertices = VisitedGraph.Vertices.Where(v => v.SkipProcessing != ProcessingOptionEnum.Freeze).ToList();
            //if we have empty input positions list we have to fill positions for frozen vertices manualy
            if(VertexPositions.Count == 0)
                foreach(var item in VisitedGraph.Vertices.Where(v => v.SkipProcessing == ProcessingOptionEnum.Freeze))
                    VertexPositions.Add(item, new Point());
            var halfSize = new double[usableVertices.Count];
            var i = 0;
            foreach ( var v in usableVertices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var s = vertexSizes[v];
                halfSize[i] = Math.Sqrt( s.Width * s.Width + s.Height * s.Height ) * 0.5;
                perimeter += halfSize[i] * 2;
                i++;
            }

            var radius = perimeter / ( 2 * Math.PI );

            //
            //precalculation
            //
            double angle = 0, a;
            i = 0;
            foreach (var v in usableVertices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                a = Math.Sin( halfSize[i] * 0.5 / radius ) * 2;
                angle += a;
                //if ( ReportOnIterationEndNeeded )
                    VertexPositions[v] = new Point( Math.Cos( angle ) * radius + radius, Math.Sin( angle ) * radius + radius );
                angle += a;
            }

            //if ( ReportOnIterationEndNeeded )
            //    OnIterationEnded( 0, 50, "Precalculation done.", false );

            //recalculate radius
            radius = angle / ( 2 * Math.PI ) * radius;

            //calculation
            angle = 0;
            i = 0;
            foreach (var v in usableVertices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                a = Math.Sin( halfSize[i] * 0.5 / radius ) * 2;
                angle += a;
                VertexPositions[v] = new Point( Math.Cos( angle ) * radius + radius, Math.Sin( angle ) * radius + radius );
                angle += a;
            }
        }
    }
}