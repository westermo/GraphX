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
using System.Windows;
using System.Windows.Data;

namespace Westermo.GraphX.Controls
{
    public class VisibilityToBoolConverter : IValueConverter
    {
        #region Inverted Property

        public bool Inverted { get; set; }

        #endregion

        #region Not Property

        public bool Not { get; set; }

        #endregion

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Inverted ? BoolToVisibility(value) : VisibilityToBool(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Inverted ? VisibilityToBool(value) : BoolToVisibility(value);
        }

        private object VisibilityToBool(object value)
        {
            if (!(value is Visibility))
                throw new InvalidOperationException("SuppliedValueWasNotVisibility");

            return ((Visibility) value == Visibility.Visible) ^ Not;
        }

        private object BoolToVisibility(object value)
        {
            if (!(value is bool))
                throw new InvalidOperationException("SuppliedValueWasNotBool");

            return (bool) value ^ Not ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
