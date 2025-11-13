using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Westermo.GraphX.Controls.Controls.ZoomControl.Converters;

public sealed class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter : BindingOperations.DoNothing;
    }
}