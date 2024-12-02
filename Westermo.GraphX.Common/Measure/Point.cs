using System;

// These types are aliased to match the unmanaged names used in interop

namespace Westermo.GraphX.Measure
{
    /// <summary>
    /// Custom PCL implementation of Point class
    /// </summary>
    public struct Point(double x, double y)
    {
        public static Point Zero { get; } = new();

        internal double _x = x;
        internal double _y = y;

        public double X
        {
            get => _x;
            set => _x = value;
        }

        public double Y
        {
            get => _y;
            set => _y = value;
        }

        #region Custom operator overloads

        /// <summary>
        /// Compares two Point instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Point instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='point1'>The first Point to compare</param>
        /// <param name='point2'>The second Point to compare</param>
        public static bool operator ==(Point point1, Point point2)
        {
            return Math.Abs(point1.X - point2.X) < 1e-12 &&
                   Math.Abs(point1.Y - point2.Y) < 1e-12;
        }

        /// <summary>
        /// Compares two Point instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Point instances are exactly unequal, false otherwise
        /// </returns>
        /// <param name='point1'>The first Point to compare</param>
        /// <param name='point2'>The second Point to compare</param>
        public static bool operator !=(Point point1, Point point2)
        {
            return !(point1 == point2);
        }

        /// <summary>
        /// Compares two Point instances for object equality.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the two Point instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='point1'>The first Point to compare</param>
        /// <param name='point2'>The second Point to compare</param>
        public static bool Equals(Point point1, Point point2)
        {
            return point1.X.Equals(point2.X) &&
                   point1.Y.Equals(point2.Y);
        }

        /// <summary>
        /// Equals - compares this Point with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of Point and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override bool Equals(object o)
        {
            if (null == o || !(o is Point))
            {
                return false;
            }

            var value = (Point)o;
            return Equals(this, value);
        }

        /// <summary>
        /// Equals - compares this Point with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The Point to compare to "this"</param>
        public bool Equals(Point value)
        {
            return Equals(this, value);
        }

        /// <summary>
        /// Returns the HashCode for this Point
        /// </summary>
        /// <returns>
        /// int - the HashCode for this Point
        /// </returns>
        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return X.GetHashCode() ^
                   Y.GetHashCode();
        }
        ///// OTHER CLASSES CONVERSIONS

        public static implicit operator Point(Size size)
        {
            return new Point(size.Width, size.Height);
        }

        public static implicit operator Point(Vector size)
        {
            return new Point(size.X, size.Y);
        }

        public static explicit operator Size(Point point)
        {
            return new Size(Math.Abs(point._x), Math.Abs(point._y));
        }

        public static explicit operator Vector(Point point)
        {
            return new Vector(point._x, point._y);
        }

        ///// OTHER CLASSES ARITHM + CONVERSIONS

        public static Point operator +(Point point, Vector vector)
        {
            return new Point(point._x + vector._x, point._y + vector._y);
        }

        public static Point operator -(Point point, Vector vector)
        {
            return new Point(point._x - vector._x, point._y - vector._y);
        }

        /// ARITHMETIC
        public static Vector operator -(Point value1, Point value2)
        {
            return new Vector(value1._x - value2._x, value1._y - value2._y);
        }

        public static Point operator +(Point value1, Point value2)
        {
            return new Point(value1._x + value2._x, value1._y + value2._y);
        }

        public static Point operator *(double value1, Point value2)
        {
            return new Point(value1 * value2.X, value1 * value2.Y);
        }

        public static Point operator *(Point value1, double value2)
        {
            return new Point(value1.X * value2, value1.Y * value2);
        }

        public static Point operator /(Point value1, double value2)
        {
            return new Point(value1.X * value2, value1.Y * value2);
        }

        public void Offset(double offsetX, double offsetY)
        {
            _x += offsetX;
            _y += offsetY;
        }

        #endregion

        public override string ToString()
        {
            return $"{_x}:{_y}";
        }
    }
}