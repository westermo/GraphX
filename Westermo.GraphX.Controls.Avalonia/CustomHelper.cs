using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Westermo.GraphX.Controls.Avalonia
{
    public static partial class CustomHelper
    {
        public static bool IsIntegerInput(string text)
        {
            return text != "\r" && Integer().IsMatch(text);
        }

        public static bool IsDoubleInput(string text)
        {
            return text != "\r" && Double().IsMatch(text);
        }

        public static ScaleTransform? GetScaleTransform(Control target)
        {
            var transform = target.RenderTransform as ScaleTransform;
            if (transform != null) return transform;
            var transformGroup = target.RenderTransform as TransformGroup;
            if (transformGroup != null)
                transform = transformGroup.Children[0] as ScaleTransform;
            if (transformGroup == null || transform == null)
                transform = target.RenderTransform as ScaleTransform;
            return transform;
        }

        public static Control? FindDescendantByName(this Control? element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (name.Equals(element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }

            foreach (var child in element.GetVisualChildren())
            {
                var result = (child as Control)?.FindDescendantByName(name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static bool IsInDesignMode(Control? ctrl = null)
        {
            return ctrl is not null && Application.Current?.ApplicationLifetime is null;
        }

        [GeneratedRegex("[^0-9.]+")]
        private static partial Regex Double();

        [GeneratedRegex("[^0-9]+")]
        private static partial Regex Integer();
    }
}