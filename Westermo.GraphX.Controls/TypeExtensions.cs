using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

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

    extension(Timeline tl)
    {
        public void SetDesiredFrameRate(int fps)
        {
            Timeline.SetDesiredFrameRate(tl, fps);
        }
    }

    extension(Vector v)
    {
        public Point ToPoint()
        {
            return new Point(v.X, v.Y);
        }
    }

    extension(FrameworkElement el)
    {
        public bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(el);
        }
    }

    extension(Point point)
    {
        public void Offset(Point value)
        {
            point.X += value.X;
            point.Y += value.Y;
        }
    }

    /// <param name="obj"></param>
    extension(DependencyObject obj)
    {
        /// <summary>
        /// Not for METRO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> FindLogicalChildren<T>()
            where T : DependencyObject
        {
            if (obj == null) yield break;
            if (obj is T child) yield return child;

            foreach (var c in LogicalTreeHelper.GetChildren(obj)
                         .OfType<DependencyObject>()
                         .SelectMany(FindLogicalChildren<T>))
                yield return c;
        }
    }

    extension(Measure.Point point)
    {
        public Point ToWindows()
        {
            return new Point(point.X, point.Y);
        }
    }

    extension(Measure.Vector point)
    {
        public Point ToWindows()
        {
            return new Point(point.X, point.Y);
        }
    }

    extension(IEnumerable<Point> points)
    {
        public PointCollection ToPointCollection()
        {
            var list = new PointCollection();
            foreach (var item in points)
                list.Add(item);
            return list;
        }
    }


    extension(Measure.Point[]? points)
    {
        public Point[]? ToWindows()
        {
            if (points == null) return null;
            var list = new Point[points.Length];
            for (var i = 0; i < points.Length; i++)
                list[i] = points[i].ToWindows();
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

    extension(Rect rect)
    {
        public Measure.Rect ToGraphX()
        {
            return new Measure.Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    extension(Measure.Rect rect)
    {
        public Rect ToWindows()
        {
            return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        }
    }

    extension(Rect rect)
    {
        public Point Center()
        {
            return new Point(rect.X + rect.Width * .5, rect.Y + rect.Height * .5);
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

    extension(Rect rect)
    {
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
}