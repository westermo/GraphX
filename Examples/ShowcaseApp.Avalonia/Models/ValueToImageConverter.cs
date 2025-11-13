using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ShowcaseApp.Avalonia.Models;

public sealed class ValueToImageConverter : IValueConverter
{
    #region IValueConverter Members

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int id) return null;
        return ImageLoader.GetImageById(id);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Image to Id conversion is not supported!");
    }

    #endregion
}

public sealed class ValueToPersonImageConverter : IValueConverter
{
    #region IValueConverter Members

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int id) return null;
        return ThemedDataStorage.GetImageById(id);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Image to Id conversion is not supported!");
    }

    #endregion
}

public sealed class ValueToEditorImageConverter : IValueConverter
{
    #region IValueConverter Members

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int id) return null;
        return ThemedDataStorage.GetEditorImageById(id);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Image to Id conversion is not supported!");
    }

    #endregion
}

public sealed class BoolToColorConverter : IValueConverter
{
    public IImmutableSolidColorBrush TrueColor { get; set; } = Brushes.LightBlue;
    public IImmutableSolidColorBrush FalseColor { get; set; } = Brushes.Yellow;

    #region IValueConverter Members

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b) return TrueColor;
        return b ? TrueColor : FalseColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Brush brush) return false;
        return ReferenceEquals(brush, TrueColor);
    }

    #endregion
}