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
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Westermo.GraphX.Controls.Avalonia
{
    public static class VisualTreeHelperEx
    {
        public static Visual? FindDescendantByName(Visual? element, string name)
        {
            if (element is Control frameworkElement && frameworkElement.Name == name)
                return element;

            Visual? foundElement = null;
            if (element is Control frameworkElementWithTemplate)
                frameworkElementWithTemplate.ApplyTemplate();

            if (element is null) return null;
            foreach (var visual in element.GetVisualChildren())
            {
                foundElement = FindDescendantByName(visual, name);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }

        public static Visual? FindDescendantByType(Visual? element, Type type, bool specificTypeOnly = true)
        {
            if (element == null)
                return null;

            if (specificTypeOnly
                    ? element.GetType() == type
                    : element.GetType() == type || element.GetType().IsSubclassOf(type))
                return element;

            Visual? foundElement = null;
            if (element is Control frameworkElement)
                frameworkElement.ApplyTemplate();

            foreach (var visual in element.GetVisualChildren())
            {
                foundElement = FindDescendantByType(visual, type, specificTypeOnly);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }

        #region Find descendants of type

        public static IEnumerable<T> FindDescendantsOfType<T>(this Visual? element) where T : class
        {
            if (element == null) yield break;
            if (element is T)
                yield return (T)(object)element;

            if (element is Control frameworkElement)
                frameworkElement.ApplyTemplate();

            foreach (var visual in element.GetVisualChildren())
            {
                foreach (var item in visual.FindDescendantsOfType<T>())
                    yield return item;
            }
        }

        #endregion

        public static T? FindDescendantByType<T>(Visual element) where T : Visual
        {
            var temp = FindDescendantByType(element, typeof(T));

            return (T?)temp;
        }

        public static Visual? FindDescendantWithPropertyValue(Visual? element, AvaloniaProperty dp, object value)
        {
            if (element == null)
                return null;

            if (element.GetValue(dp)?.Equals(value) == true)
                return element;

            Visual? foundElement = null;
            if (element is Control frameworkElement)
                frameworkElement.ApplyTemplate();
            foreach (var visual in element.GetVisualChildren())
            {
                foundElement = FindDescendantWithPropertyValue(visual, dp, value);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }
    }
}