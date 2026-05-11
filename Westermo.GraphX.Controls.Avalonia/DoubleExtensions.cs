using System;

namespace Westermo.GraphX.Controls;

public static class DoubleExtensions
{
    /// <param name="value"></param>
    extension(double value)
    {
        /// <summary>
        /// Convert angle value from radians to degrees
        /// </summary>
        public double ToDegrees()
        {
            return value * 180 / Math.PI;
        }

        /// <summary>
        /// Convert angle value from degrees to radians
        /// </summary>
        public double ToRadians()
        {
            return value * Math.PI / 180;
        }
    }
}