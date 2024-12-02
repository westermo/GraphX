using System;

namespace Westermo.GraphX.Measure
{
    public readonly struct Thickness(double left, double top, double right, double bottom)
    {
        public readonly double Left = left;
        public readonly double Top = top;
        public readonly double Bottom = bottom;
        public readonly double Right = right;

        public static bool operator !=(Thickness t1, Thickness t2)
        {
            return !(Math.Abs(t1.Left - t2.Left) < 1e-12 && Math.Abs(t1.Top - t2.Top) < 1e-12 &&
                     Math.Abs(t1.Right - t2.Right) < 1e-12 && Math.Abs(t1.Bottom - t2.Bottom) < 1e-12);
        }

        public static bool operator ==(Thickness t1, Thickness t2)
        {
            return Math.Abs(t1.Left - t2.Left) < 1e-12 && Math.Abs(t1.Top - t2.Top) < 1e-12 &&
                   Math.Abs(t1.Right - t2.Right) < 1e-12 && Math.Abs(t1.Bottom - t2.Bottom) < 1e-12;
        }

        public override bool Equals(object o)
        {
            return o is Thickness thickness && Equals(this, thickness);
        }

        public bool Equals(Thickness value)
        {
            return Equals(this, value);
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() ^ Top.GetHashCode() ^ Right.GetHashCode() ^ Bottom.GetHashCode();
        }
    }
}