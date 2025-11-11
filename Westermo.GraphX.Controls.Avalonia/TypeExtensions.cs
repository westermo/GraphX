using Avalonia;

namespace Westermo.GraphX.Controls;

public static class TypeExtensions
{
    extension(Point v)
    {
        public Vector ToVector()
        {
            return new Vector(v.X, v.Y);
        }
    }


    extension(Vector v)
    {
        public Point ToPoint()
        {
            return new Point(v.X, v.Y);
        }
    }


    extension(Measure.Point point)
    {
        public Point ToAvalonia()
        {
            return new Point(point.X, point.Y);
        }
    }

    extension(Measure.Vector point)
    {
        public Point ToAvalonia()
        {
            return new Point(point.X, point.Y);
        }
    }

    extension(Measure.Point[]? points)
    {
        public Point[]? ToAvalonia()
        {
            if (points == null) return null;
            var list = new Point[points.Length];
            for (var i = 0; i < points.Length; i++)
                list[i] = points[i].ToAvalonia();
            return list;
        }
    }

    extension(Point[]? points)
    {
        public Measure.Point[]? ToGraphX()
        {
            if (points == null) return null;
            var list = new Measure.Point[points.Length];
            for (var i = 0; i < points.Length; i++)
                list[i] = points[i].ToGraphX();
            return list;
        }
    }

    extension(Rect rect)
    {
        public Size Size()
        {
            return new Size(rect.Width, rect.Height);
        }

        public Measure.Rect ToGraphX()
        {
            return new Measure.Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public Point Center()
        {
            return new Point(rect.X + rect.Width * .5, rect.Y + rect.Height * .5);
        }

        public Point TopLeft()
        {
            return new Point(rect.Left, rect.Top);
        }

        public Point TopRight()
        {
            return new Point(rect.Right, rect.Top);
        }

        public Point BottomRight()
        {
            return new Point(rect.Right, rect.Bottom);
        }

        public Point BottomLeft()
        {
            return new Point(rect.Left, rect.Bottom);
        }
    }

    extension(Point point)
    {
        public Measure.Point ToGraphX()
        {
            return new Measure.Point(point.X, point.Y);
        }
    }

    extension(Size point)
    {
        public Measure.Size ToGraphX()
        {
            return new Measure.Size(point.Width, point.Height);
        }
    }

    extension(Measure.Rect rect)
    {
        public Rect ToAvalonia()
        {
            return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        }
    }

    extension(Point pt)
    {
        public Point Subtract(Point pt2)
        {
            return new Point(pt.X - pt2.X, pt.Y - pt2.Y);
        }

        public Point Div(double value)
        {
            return new Point(pt.X / value, pt.Y / value);
        }

        public Point Mul(double value)
        {
            return new Point(pt.X * value, pt.Y * value);
        }

        public Point Sum(Point pt2)
        {
            return new Point(pt.X + pt2.X, pt.Y + pt2.Y);
        }
    }
}