using System;
using System.Collections.Generic;
using System.Threading;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
    public class OneWayFSAAlgorithm<TObject>(IDictionary<TObject, Rect> rectangles, OneWayFSAParameters parameters)
        : FSAAlgorithm<TObject, OneWayFSAParameters>(rectangles, parameters)
        where TObject : class
    {
        protected override void RemoveOverlap(CancellationToken cancellationToken)
        {
            switch ( Parameters.Way )
            {
                case OneWayFSAWayEnum.Horizontal:
                    HorizontalImproved(cancellationToken);
                    break;
                case OneWayFSAWayEnum.Vertical:
                    VerticalImproved(cancellationToken);
                    break;
            }
        }

        protected new double HorizontalImproved(CancellationToken cancellationToken)
        {
            WrappedRectangles.Sort( XComparison );
            int i = 0, n = WrappedRectangles.Count;

            //bal szelso
            var lmin = WrappedRectangles[0];
            double sigma = 0, x0 = lmin.CenterX;
            var gamma = new double[WrappedRectangles.Count];
            var x = new double[WrappedRectangles.Count];
            while ( i < n )
            {
                var u = WrappedRectangles[i];

                //i-vel azonos középponttal rendelkező téglalapok meghatározása
                var k = i;
                for ( var j = i + 1; j < n; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var v = WrappedRectangles[j];
                    if ( u.CenterX == v.CenterX )
                    {
                        u = v;
                        k = j;
                    }
                    else
                    {
                        break;
                    }
                }
                double g = 0;

                //ne legyenek ugyanabban a pontban
                for ( var z = i + 1; z <= k; z++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var v = WrappedRectangles[z];
                    v.Rectangle.X += ( z - i ) * 0.0001;
                }

                //i-k intervallumban lévő téglalapokra erőszámítás a tőlük balra lévőkkel
                if ( u.CenterX > x0 )
                {
                    for ( var m = i; m <= k; m++ )
                    {
                        double ggg = 0;
                        for ( var j = 0; j < i; j++ )
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var f = Force( WrappedRectangles[j].Rectangle, WrappedRectangles[m].Rectangle );
                            ggg = Math.Max( f.X + gamma[j], ggg );
                        }
                        var v = WrappedRectangles[m];
                        var gg = v.Rectangle.Left + ggg < lmin.Rectangle.Left ? sigma : ggg;
                        g = Math.Max( g, gg );
                    }
                }
                //megjegyezzük az elemek eltolásást x tömbbe
                //bal szélő elemet újra meghatározzuk
                for ( var m = i; m <= k; m++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    gamma[m] = g;
                    var r = WrappedRectangles[m];
                    x[m] = r.Rectangle.Left + g;
                    if ( r.Rectangle.Left < lmin.Rectangle.Left )
                    {
                        lmin = r;
                    }
                }

                //az i-k intervallum négyzeteitől jobbra lévőkkel erőszámítás, legnagyobb erő tárolása
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                double delta = 0;
                for ( var m = i; m <= k; m++ )
                {
                    for ( var j = k + 1; j < n; j++ )
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var f = Force( WrappedRectangles[m].Rectangle, WrappedRectangles[j].Rectangle );
                        if ( f.X > delta )
                        {
                            delta = f.X;
                        }
                    }
                }
                sigma += delta;
                i = k + 1;
            }
            double cost = 0;
            for ( i = 0; i < n; i++ )
            {
                var r = WrappedRectangles[i];
                var oldPos = r.Rectangle.Left;
                var newPos = x[i];

                r.Rectangle.X = newPos;

                var diff = oldPos - newPos;
                cost += diff * diff;
            }
            return cost;
        }
    }
}