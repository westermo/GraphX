using Avalonia;
using Avalonia.Media;

namespace ShowcaseApp.Avalonia.Models
{
    /// <summary>
    /// Contains helpful attached properties for VertexControl class
    /// </summary>
    public class VCTemplateBehaviour
    {
        public static readonly StyledProperty<SolidColorBrush> BackgroundColorProperty =
            AvaloniaProperty.RegisterAttached<VCTemplateBehaviour, AvaloniaObject, SolidColorBrush>("BackgroundColor",
                SolidColorBrush.Parse("#00FF00"));

        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            AvaloniaProperty.RegisterAttached<VCTemplateBehaviour, AvaloniaObject, Thickness>("BorderThickness",
                new Thickness(2));

        public static Thickness GetBorderThickness(AvaloniaObject dependencyObject)
        {
            return (Thickness)dependencyObject.GetValue(BorderThicknessProperty);
        }

        public static void SetBorderThickness(AvaloniaObject dependencyObject, Thickness value)
        {
            dependencyObject.SetValue(BorderThicknessProperty, value);
        }

        public static SolidColorBrush GetBackgroundColor(AvaloniaObject dependencyObject)
        {
            return (SolidColorBrush)dependencyObject.GetValue(BackgroundColorProperty);
        }

        public static void SetBackgroundColor(AvaloniaObject dependencyObject, SolidColorBrush value)
        {
            dependencyObject.SetValue(BackgroundColorProperty, value);
        }
    }
}