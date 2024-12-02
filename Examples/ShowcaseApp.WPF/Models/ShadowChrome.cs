using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace ShowcaseApp.WPF.Models
{
    public class ShadowChrome : Decorator
    {
        private static SolidColorBrush backgroundBrush;
        private static LinearGradientBrush rightBrush;
        private static LinearGradientBrush bottomBrush;
        private static RadialGradientBrush bottomRightBrush;
        private static RadialGradientBrush topRightBrush;
        private static RadialGradientBrush bottomLeftBrush;

        // *** Constructors ***
        static ShadowChrome()
        {
            MarginProperty.OverrideMetadata(typeof(ShadowChrome), new FrameworkPropertyMetadata(new Thickness(0, 0, 4, 4)));
            CreateBrushes();
        }

        // *** Overriden base methods ***
        protected override void OnRender(DrawingContext drawingContext)
        {
            // Calculate the size of the shadow
            var shadowSize = Math.Min(Margin.Right, Margin.Bottom);
            // If there is no shadow, or it is bigger than the size of the child, then just return
            if (shadowSize <= 0 || ActualWidth < shadowSize * 2 || ActualHeight < shadowSize * 2)
                return;
            // Draw the background (this may show through rounded corners of the child object)
            var backgroundRect = new Rect(shadowSize, shadowSize, ActualWidth - shadowSize, ActualHeight - shadowSize);
            drawingContext.DrawRectangle(backgroundBrush, null, backgroundRect);
            // Now draw the shadow gradients
            var topRightRect = new Rect(ActualWidth, shadowSize, shadowSize, shadowSize);
            drawingContext.DrawRectangle(topRightBrush, null, topRightRect);
            var rightRect = new Rect(ActualWidth, shadowSize * 2, shadowSize, ActualHeight - shadowSize * 2);
            drawingContext.DrawRectangle(rightBrush, null, rightRect);

            var bottomRightRect = new Rect(ActualWidth, ActualHeight, shadowSize, shadowSize);
            drawingContext.DrawRectangle(bottomRightBrush, null, bottomRightRect);

            var bottomRect = new Rect(shadowSize * 2, ActualHeight, ActualWidth - shadowSize * 2, shadowSize);
            drawingContext.DrawRectangle(bottomBrush, null, bottomRect);

            var bottomLeftRect = new Rect(shadowSize, ActualHeight, shadowSize, shadowSize);
            drawingContext.DrawRectangle(bottomLeftBrush, null, bottomLeftRect);
        }


        // *** Private static methods ***
        private static void CreateBrushes()
        {
            // Get the colors for the shadow
            var shadowColor = Color.FromArgb(128, 0, 0, 0);
            var transparentColor = Color.FromArgb(16, 0, 0, 0);
            // Create a GradientStopCollection from these
            var gradient = new GradientStopCollection(2);
            gradient.Add(new GradientStop(shadowColor, 0.5));
            gradient.Add(new GradientStop(transparentColor, 1.0));
            gradient.Freeze();
            // Create the background brush
            backgroundBrush = new SolidColorBrush(shadowColor);
            backgroundBrush.Freeze();
            // Create the LinearGradientBrushes
            rightBrush = new LinearGradientBrush(gradient, new Point(0.0, 0.0), new Point(1.0, 0.0)); rightBrush.Freeze();
            bottomBrush = new LinearGradientBrush(gradient, new Point(0.0, 0.0), new Point(0.0, 1.0)); bottomBrush.Freeze();
            // Create the RadialGradientBrushes
            bottomRightBrush = new RadialGradientBrush(gradient);
            bottomRightBrush.GradientOrigin = new Point(0.0, 0.0);
            bottomRightBrush.Center = new Point(0.0, 0.0);
            bottomRightBrush.RadiusX = 1.0;
            bottomRightBrush.RadiusY = 1.0;
            bottomRightBrush.Freeze();

            topRightBrush = new RadialGradientBrush(gradient);
            topRightBrush.GradientOrigin = new Point(0.0, 1.0);
            topRightBrush.Center = new Point(0.0, 1.0);
            topRightBrush.RadiusX = 1.0;
            topRightBrush.RadiusY = 1.0;
            topRightBrush.Freeze();

            bottomLeftBrush = new RadialGradientBrush(gradient);
            bottomLeftBrush.GradientOrigin = new Point(1.0, 0.0);
            bottomLeftBrush.Center = new Point(1.0, 0.0);
            bottomLeftBrush.RadiusX = 1.0;
            bottomLeftBrush.RadiusY = 1.0;
            bottomLeftBrush.Freeze();
        }

    }
    
}
