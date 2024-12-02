using System;
using System.Collections.Generic;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common.Interfaces;

namespace Westermo.GraphX.Logic.Algorithms.OverlapRemoval
{
    public class FSAAlgorithm<TObject> : FSAAlgorithm<TObject, IOverlapRemovalParameters>
        where TObject : class
    {
        public FSAAlgorithm( IDictionary<TObject, Rect> rectangles, IOverlapRemovalParameters parameters )
            : base( rectangles, parameters )
        {
        }

        public FSAAlgorithm()
            : base(null, null)
        {
        }
    }

    /// <summary>
    /// http://adaptagrams.svn.sourceforge.net/viewvc/adaptagrams/trunk/RectangleOverlapSolver/placement/FSA.java?view=markup
    /// </summary>
    public class FSAAlgorithm<TObject, TParam>(IDictionary<TObject, Rect> rectangles, TParam parameters)
        : OverlapRemovalAlgorithmBase<TObject, TParam>(rectangles, parameters)
        where TObject : class
        where TParam : IOverlapRemovalParameters
    {
        protected override void RemoveOverlap(CancellationToken cancellationToken)
        {
            //DateTime t0 = DateTime.Now;
            var cost = HorizontalImproved(cancellationToken);
            //DateTime t1 = DateTime.Now;

            //Debug.WriteLine( "PFS horizontal: cost=" + cost + " time=" + ( t1 - t0 ) );

            //t1 = DateTime.Now;
            cost = VerticalImproved(cancellationToken);
           // DateTime t2 = DateTime.Now;
           // Debug.WriteLine( "PFS vertical: cost=" + cost + " time=" + ( t2 - t1 ) );
           // Debug.WriteLine( "PFS total: time=" + ( t2 - t0 ) );
        }

        protected Vector Force( Rect vi, Rect vj )
        {
            var f = new Vector( 0, 0 );
            var d = vj.GetCenter() - vi.GetCenter();
            var adx = Math.Abs( d.X );
            var ady = Math.Abs( d.Y );
            var gij = d.Y / d.X;
            var Gij = ( vi.Height + vj.Height ) / ( vi.Width + vj.Width );
            if ( Gij >= gij && gij > 0 || -Gij <= gij && gij < 0 || gij == 0 )
            {
                // vi and vj touch with y-direction boundaries
                f.X = d.X / adx * ( ( vi.Width + vj.Width ) / 2.0 - adx );
                f.Y = f.X * gij;
            }
            if ( Gij < gij && gij > 0 || -Gij > gij && gij < 0 )
            {
                // vi and vj touch with x-direction boundaries
                f.Y = d.Y / ady * ( ( vi.Height + vj.Height ) / 2.0 - ady );
                f.X = f.Y / gij;
            }
            return f;
        }

        protected Vector Force2( Rect vi, Rect vj )
        {
            var f = new Vector( 0, 0 );
            var d = vj.GetCenter() - vi.GetCenter();
            var gij = d.Y / d.X;
            if ( vi.IntersectsWith( vj ) )
            {
                f.X = ( vi.Width + vj.Width ) / 2.0 - d.X;
                f.Y = ( vi.Height + vj.Height ) / 2.0 - d.Y;
                // in the x dimension
                if ( f.X > f.Y && gij != 0 )
                {
                    f.X = f.Y / gij;
                }
                f.X = Math.Max( f.X, 0 );
                f.Y = Math.Max( f.Y, 0 );
            }
            return f;
        }

        protected int XComparison( RectangleWrapper<TObject> r1, RectangleWrapper<TObject> r2 )
        {
            var r1CenterX = r1.CenterX;
            var r2CenterX = r2.CenterX;

            if ( r1CenterX < r2CenterX )
            {
                return -1;
            }
            if ( r1CenterX > r2CenterX )
            {
                return 1;
            }
            return 0;
        }

        /*
        protected void Horizontal(CancellationToken cancellationToken)
        {
            WrappedRectangles.Sort( XComparison );
            int i = 0, n = WrappedRectangles.Count;
            while ( i < n )
            {
                // x_i = x_{i+1} = ... = x_k
                int k = i;
                RectangleWrapper<TObject> u = WrappedRectangles[i];
                //TODO plus 1 check
                for ( int j = i + 1; j < n; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    RectangleWrapper<TObject> v = WrappedRectangles[j];
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
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                double delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Vector f = Force( WrappedRectangles[m].Rectangle, WrappedRectangles[j].Rectangle );
                        if ( f.X > delta )
                        {
                            delta = f.X;
                        }
                    }
                }
                for ( int j = k + 1; j < n; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var r = WrappedRectangles[j];
                    r.Rectangle.Offset( delta, 0 );
                }
                i = k + 1;
            }

        }
        */
        protected double HorizontalImproved(CancellationToken cancellationToken)
        {
            if (WrappedRectangles.Count == 0) return 0;
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

                //i-vel azonos k�z�pponttal rendelkez� t�glalapok meghat�roz�sa
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

                //i-k intervallumban l�v� t�glalapokra er�sz�m�t�s a t�l�k balra l�v�kkel
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
                        var gg =
                            v.Rectangle.Left + ggg < lmin.Rectangle.Left
                                ? sigma
                                : ggg;
                        g = Math.Max( g, gg );
                    }
                }
                //megjegyezz�k az elemek eltol�s�st x t�mbbe
                //bal sz�l� elemet �jra meghat�rozzuk
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

                //az i-k intervallum n�gyzeteit�l jobbra l�v�kkel er�sz�m�t�s, legnagyobb er� t�rol�sa
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
                cancellationToken.ThrowIfCancellationRequested();

                var r = WrappedRectangles[i];
                var oldPos = r.Rectangle.Left;
                var newPos = x[i];

                r.Rectangle.X = newPos;

                var diff = oldPos - newPos;
                cost += diff * diff;
            }
            return cost;
        }

        protected int YComparison( RectangleWrapper<TObject> r1, RectangleWrapper<TObject> r2 )
        {
            var r1CenterY = r1.CenterY;
            var r2CenterY = r2.CenterY;

            if ( r1CenterY < r2CenterY )
            {
                return -1;
            }
            if ( r1CenterY > r2CenterY )
            {
                return 1;
            }
            return 0;
        }
        /*
        protected void Vertical(CancellationToken cancellationToken)
        {
            WrappedRectangles.Sort( YComparison );
            int i = 0, n = WrappedRectangles.Count;
            while ( i < n )
            {
                // y_i = y_{i+1} = ... = y_k
                int k = i;
                RectangleWrapper<TObject> u = WrappedRectangles[i];
                for ( int j = i; j < n; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RectangleWrapper<TObject> v = WrappedRectangles[j];
                    if ( u.CenterY == v.CenterY )
                    {
                        u = v;
                        k = j;
                    }
                    else
                    {
                        break;
                    }
                }
                // delta = max(0, max{f.y(m,j)|i<=m<=k<j<n})
                double delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Vector f = Force2( WrappedRectangles[m].Rectangle, WrappedRectangles[j].Rectangle );
                        if ( f.Y > delta )
                        {
                            delta = f.Y;
                        }
                    }
                }
                for ( int j = k + 1; j < n; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RectangleWrapper<TObject> r = WrappedRectangles[j];
                    r.Rectangle.Offset( 0, delta );
                }
                i = k + 1;
            }

        }
        */
        protected double VerticalImproved(CancellationToken cancellationToken)
        {
            if (WrappedRectangles.Count == 0) return 0;
            WrappedRectangles.Sort( YComparison );
            int i = 0, n = WrappedRectangles.Count;
            var lmin = WrappedRectangles[0];
            double sigma = 0, y0 = lmin.CenterY;
            var gamma = new double[WrappedRectangles.Count];
            var y = new double[WrappedRectangles.Count];
            while ( i < n )
            {
                var u = WrappedRectangles[i];
                var k = i;
                for ( var j = i + 1; j < n; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var v = WrappedRectangles[j];
                    if ( u.CenterY == v.CenterY )
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
                if ( u.CenterY > y0 )
                {
                    for ( var m = i; m <= k; m++ )
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        double ggg = 0;
                        for ( var j = 0; j < i; j++ )
                        {
                            var f = Force2( WrappedRectangles[j].Rectangle, WrappedRectangles[m].Rectangle );
                            ggg = Math.Max( f.Y + gamma[j], ggg );
                        }
                        var v = WrappedRectangles[m];
                        var gg =
                            v.Rectangle.Top + ggg < lmin.Rectangle.Top
                                ? sigma
                                : ggg;
                        g = Math.Max( g, gg );
                    }
                }
                for ( var m = i; m <= k; m++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    gamma[m] = g;
                    var r = WrappedRectangles[m];
                    y[m] = r.Rectangle.Top + g;
                    if ( r.Rectangle.Top < lmin.Rectangle.Top )
                    {
                        lmin = r;
                    }
                }
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                double delta = 0;
                for ( var m = i; m <= k; m++ )
                {
                    for ( var j = k + 1; j < n; j++ )
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var f = Force( WrappedRectangles[m].Rectangle, WrappedRectangles[j].Rectangle );
                        if ( f.Y > delta )
                        {
                            delta = f.Y;
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
                var oldPos = r.Rectangle.Top;
                var newPos = y[i];

                r.Rectangle.Y = newPos;

                var diff = oldPos - newPos;
                cost += diff * diff;
            }
            return cost;
        }
    }
}