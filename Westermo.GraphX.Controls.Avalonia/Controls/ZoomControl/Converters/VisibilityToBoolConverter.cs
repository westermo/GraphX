﻿/*************************************************************************************

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
using Avalonia.Data.Converters;

namespace Westermo.GraphX.Controls.Avalonia
{
    public class VisibilityToBoolConverter : IValueConverter
    {
        public bool Inverted { get; set; }
        public bool Not { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Inverted ? BoolToVisibility(value) : VisibilityToBool(value);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Inverted ? VisibilityToBool(value) : BoolToVisibility(value);
        }

        private object VisibilityToBool(object? value)
        {
            if (value is not bool b)
                return false;
            return (b == true) ^ Not;
        }

        private object BoolToVisibility(object? value)
        {
            if (value is not bool b)
                return false;
            return (b ^ Not);
        }
    }
}
