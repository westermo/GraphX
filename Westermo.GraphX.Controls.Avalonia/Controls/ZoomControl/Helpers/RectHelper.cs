/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using Avalonia;

namespace Westermo.GraphX.Controls.Avalonia;

public static class RectHelper
{
    public static bool IsEmpty(this Rect rect)
    {
        if (rect == default) return true;
        if (rect.Width <= double.Epsilon || rect.Height <= double.Epsilon) return true;
        return double.IsNaN(rect.X) || double.IsNaN(rect.Y) || double.IsNaN(rect.Width) || double.IsNaN(rect.Height);
    }

    public static Rect Empty => new(double.NaN, double.NaN, double.NaN, double.NaN);
}