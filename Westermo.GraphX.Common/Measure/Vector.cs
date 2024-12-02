using System;

namespace Westermo.GraphX.Measure
{
    public struct Vector(double x, double y)
    {
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

        public static Vector Zero { get; } = new Vector();

        #region Overloaded operators

        public static bool operator ==(Vector vector1, Vector vector2) =>
            Math.Abs(vector1.X - vector2.X) < 1e-12 && Math.Abs(vector1.Y - vector2.Y) < 1e-12;

        public static bool operator !=(Vector vector1, Vector vector2) => !(vector1 == vector2);


        public static double operator *(Vector vector1, Vector vector2) =>
            vector1._x * vector2._x + vector1._y * vector2._y;

        public static Vector operator *(double scalar, Vector vector) => new(vector._x * scalar, vector._y * scalar);

        public static Vector operator *(Vector vector, double scalar) => new(vector._x * scalar, vector._y * scalar);

        public static Vector operator *(int value1, Vector value2) => new(value1 * value2.X, value1 * value2.Y);

        public static Vector operator +(Vector value1, Vector value2) => new(value1.X + value2.X, value1.Y + value2.Y);

        public static Vector operator -(Vector value1, Vector value2) => new(value1.X - value2.X, value1.Y - value2.Y);

        public static Vector operator /(Vector vector, double scalar) => vector * (1.0 / scalar);

        public static Vector operator -(Vector value1) => new(-value1.X, -value1.Y);


        public static Point operator +(Vector value1, Point value2) => new(value1.X + value2.X, value1.Y + value2.Y);

        /* public static Vector operator /(Vector value1, Vector value2)
         {
             return new Vector(value1.X / value2.X, value1.Y / value2.Y);
         }*/

        public static Vector operator -(Vector value1, Point value2) => new(value1.X - value2.X, value1.Y - value2.Y);

        #endregion

        public static bool Equals(Vector vector1, Vector vector2) =>
            vector1.X.Equals(vector2.X) && vector1.Y.Equals(vector2.Y);

        public override bool Equals(object o)
        {
            return o is Vector vector && Equals(this, vector);
        }

        public bool Equals(Vector value)
        {
            return Equals(this, value);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public double Length => Math.Sqrt(LengthSquared);
        public double LengthSquared => _x * _x + _y * _y;

        public void Normalize()
        {
            var v = this / Math.Max(Math.Abs(_x), Math.Abs(_y));
            v = this / Length;
            _x = v._x;
            _y = v._y;
        }

        public static double CrossProduct(Vector vector1, Vector vector2)
        {
            return vector1._x * vector2._y - vector1._y * vector2._x;
        }

        public static double AngleBetween(Vector vector1, Vector vector2)
        {
            var y = vector1._x * vector2._y - vector2._x * vector1._y;
            var x = vector1._x * vector2._x + vector1._y * vector2._y;
            return Math.Atan2(y, x) * 57.295779513082323;
        }

        public void Negate()
        {
            _x = -_x;
            _y = -_y;
        }

        public override string ToString()
        {
            return $"{_x}:{_y}";
        }

        public Point ToPoint => new Point(_x, _y);
    }
}