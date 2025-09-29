using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Westermo.GraphX.Controls.Avalonia
{
    public sealed class EqualityToBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool and true)
                return parameter;
            return BindingOperations.DoNothing;
        }
    }
}
