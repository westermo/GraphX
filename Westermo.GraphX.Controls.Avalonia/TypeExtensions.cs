using Avalonia;

namespace Westermo.GraphX.Controls.Avalonia
{
    public static class TypeExtensions
    {
        public static Vector ToVector(this Point v)
        {
            return new Vector(v.X, v.Y);
        }


        public static Point ToPoint(this Vector v)
        {
            return new Point(v.X, v.Y);
        }


        public static Point ToAvalonia(this Measure.Point point)
        {
            return new Point(point.X, point.Y);
        }

        public static Point ToAvalonia(this Measure.Vector point)
        {
            return new Point(point.X, point.Y);
        }

        public static Point[]? ToAvalonia(this Measure.Point[]? points)
        {
            if (points == null) return null;
            var list = new Point[points.Length];
            for (var i = 0; i < points.Length; i++)
                list[i] = points[i].ToAvalonia();
            return list;
        }

        public static Measure.Point[]? ToGraphX(this Point[]? points)
        {
            if (points == null) return null;
            var list = new Measure.Point[points.Length];
            for (var i = 0; i < points.Length; i++)
                list[i] = points[i].ToGraphX();
            return list;
        }

        public static Size Size(this Rect rect)
        {
            return new Size(rect.Width, rect.Height);
        }

        public static Measure.Point ToGraphX(this Point point)
        {
            return new Measure.Point(point.X, point.Y);
        }

        public static Measure.Size ToGraphX(this Size point)
        {
            return new Measure.Size(point.Width, point.Height);
        }

        public static Measure.Rect ToGraphX(this Rect rect)
        {
            return new Measure.Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Rect ToAvalonia(this Measure.Rect rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public static Point Center(this Rect rect)
        {
            return new Point(rect.X + rect.Width * .5, rect.Y + rect.Height * .5);
        }

        public static Point Subtract(this Point pt, Point pt2)
        {
            return new Point(pt.X - pt2.X, pt.Y - pt2.Y);
        }

        public static Point Div(this Point pt, double value)
        {
            return new Point(pt.X / value, pt.Y / value);
        }

        public static Point Mul(this Point pt, double value)
        {
            return new Point(pt.X * value, pt.Y * value);
        }

        public static Point Sum(this Point pt, Point pt2)
        {
            return new Point(pt.X + pt2.X, pt.Y + pt2.Y);
        }

        public static Point TopLeft(this Rect rect)
        {
            return new Point(rect.Left, rect.Top);
        }

        public static Point TopRight(this Rect rect)
        {
            return new Point(rect.Right, rect.Top);
        }

        public static Point BottomRight(this Rect rect)
        {
            return new Point(rect.Right, rect.Bottom);
        }

        public static Point BottomLeft(this Rect rect)
        {
            return new Point(rect.Left, rect.Bottom);
        }
    }
}