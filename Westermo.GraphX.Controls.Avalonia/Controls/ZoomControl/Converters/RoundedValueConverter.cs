/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Westermo.GraphX.Controls.Controls.ZoomControl.Converters;

public class RoundedValueConverter : IValueConverter
{
    #region Precision Property

    public int Precision
    {
        get => _precision;
        set => _precision = value;
    }

    private int _precision;

    #endregion

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double v)
        {
            return Math.Round(v, _precision);
        }
        else if (value is Point point)
        {
            return new Point(Math.Round(point.X, _precision), Math.Round(point.Y, _precision));
        }
        else
        {
            return value;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}