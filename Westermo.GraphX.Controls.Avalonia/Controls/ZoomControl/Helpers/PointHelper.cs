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
using Avalonia;

namespace Westermo.GraphX.Controls.Controls.ZoomControl.Helpers;

public static class PointHelper
{
    public static double DistanceBetween(this Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static Point Empty => new(double.NaN, double.NaN);

    public static bool IsEmpty(Point point)
    {
        return double.IsNaN(point.X) && double.IsNaN(point.Y);
    }
}
