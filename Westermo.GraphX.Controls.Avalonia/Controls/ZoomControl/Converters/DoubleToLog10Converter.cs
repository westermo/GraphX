using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Westermo.GraphX.Controls.Controls.ZoomControl.Converters;

public sealed class DoubleToLog10Converter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double d)
            return 0d;
        var val = Math.Log10(d);
        return double.IsNegativeInfinity(val) ? 0 : val;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double d)
            return 0d;
        var val = Math.Pow(10, d);
        return double.IsNegativeInfinity(val) ? 0 : val;
    }
}