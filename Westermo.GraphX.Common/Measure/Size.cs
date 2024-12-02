using System;


namespace Westermo.GraphX.Measure
{
    public struct Size : IEquatable<Size>
    {
        internal double _width;

        public double Width
        {
            get => _width;
            set
            {
                if (IsEmpty)
                    throw new InvalidOperationException("Size_CannotModifyEmptySize");
                if (value < 0.0)
                    throw new ArgumentException("Size_WidthCannotBeNegative");
                _width = value;
            }
        }

        internal double _height;

        public double Height
        {
            get => _height;
            set
            {
                if (IsEmpty)
                    throw new InvalidOperationException("Size_CannotModifyEmptySize");
                if (value < 0.0)
                    throw new ArgumentException("Size_HeightCannotBeNegative");
                _width = value;
                _height = value;
            }
        }

        public static Size Empty { get; }

        public bool IsEmpty => _width < 0.0;

        public Size(double width, double height)
        {
            if (width < 0.0 || height < 0.0)
            {
                throw new ArgumentException("Size_WidthAndHeightCannotBeNegative");
            }

            _width = width;
            _height = height;
        }

        #region Custom operators overload

        public static bool operator ==(Size size1, Size size2)
        {
            return Math.Abs(size1.Width - size2.Width) < 1e-12 && Math.Abs(size1.Height - size2.Height) < 1e-12;
        }

        public static bool operator !=(Size size1, Size size2)
        {
            return !(size1 == size2);
        }

        public static explicit operator Vector(Size size)
        {
            return new Vector(size._width, size._height);
        }

        public static explicit operator Point(Size size)
        {
            return new Point(size._width, size._height);
        }

        #endregion

        private static Size CreateEmptySize()
        {
            return new Size { _width = double.NegativeInfinity, _height = double.NegativeInfinity };
        }

        static Size()
        {
            Empty = CreateEmptySize();
        }

        public static bool Equals(Size size1, Size size2)
        {
            if (size1.IsEmpty)
            {
                return size2.IsEmpty;
            }

            return size1.Width.Equals(size2.Width) && size1.Height.Equals(size2.Height);
        }

        public override bool Equals(object o)
        {
            return o is Size size && Equals(this, size);
        }

        public bool Equals(Size value)
        {
            return Equals(this, value);
        }

        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }

            return Width.GetHashCode() ^ Height.GetHashCode();
        }
    }
}