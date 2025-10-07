using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Westermo.GraphX.Common.Enums;

namespace Westermo.GraphX.Controls.Avalonia
{
    public static class PrintHelper
    {
        /// <summary>
        /// Gets WPF default DPI
        /// </summary>
        public const double DEFAULT_DPI = 96d;

        /// <summary>
        /// Gets or sets the pixel format of an exported image
        /// </summary>
        public static PixelFormat PixelFormat = PixelFormats.Bgr32;

        /// <summary>
        /// Helper method which calculates estimated image DPI based on the input criterias
        /// </summary>
        /// <param name="vis">GraphArea object</param>
        /// <param name="imgdpi">Desired DPI</param>
        /// <param name="dpiStep">DPI decrease step while estimating</param>
        /// <param name="estPixelCount">Pixel quantity threshold</param>
        public static double CalculateEstimatedDPI(IGraphAreaBase vis, double imgdpi, double dpiStep,
            ulong estPixelCount)
        {
            var result = false;
            var currentDPI = imgdpi;
            while (!result)
            {
                if (CalculateSize(vis.ContentSize.Size, currentDPI) <= estPixelCount)
                    result = true;
                else currentDPI -= dpiStep;
                if (currentDPI < 0) return 0;
            }

            return currentDPI;
        }


        private static ulong CalculateSize(Size desiredSize, double dpi)
        {
            return (ulong)(desiredSize.Width * (dpi / DEFAULT_DPI) + 100) *
                   (ulong)(desiredSize.Height * (dpi / DEFAULT_DPI) + 100);
        }

        /// <summary>
        /// Method exports the GraphArea to an png image.
        /// </summary>
        /// <param name="surface">GraphArea control</param>
        /// <param name="path">Image destination path</param>
        /// <param name="useZoomControlSurface"></param>
        /// <param name="imgdpi">Optional image DPI parameter</param>
        /// <param name="imgQuality">Optional image quality parameter (for some formats like JPEG)</param>
        /// <param name="itype"></param>
        public static void ExportToImage(IGraphAreaBase surface, Uri path, ImageType itype,
            bool useZoomControlSurface = false, double imgdpi = DEFAULT_DPI, int imgQuality = 100)
        {
            if (!useZoomControlSurface)
                surface.SetPrintMode(true, true, 100);
            //Create a render bitmap and push the surface to it
            Visual vis = (Visual)surface;
            if (useZoomControlSurface)
            {
                var canvas = (Canvas)surface;
                if (canvas.Parent is IZoomControl zoomControl)
                    vis = zoomControl.PresenterVisual;
                else
                {
                    var frameworkElement = canvas.Parent as Control;
                    if (frameworkElement?.Parent is IZoomControl)
                        vis = ((IZoomControl)frameworkElement.Parent).PresenterVisual;
                }
            }

            var size = new PixelSize((int)(surface.ContentSize.Width * (imgdpi / DEFAULT_DPI) + 100),
                (int)(surface.ContentSize.Height * (imgdpi / DEFAULT_DPI) + 100));
            using (var renderBitmap =
                   new RenderTargetBitmap(size, new Vector(imgdpi, imgdpi)))
            {
                //Render the graphlayout onto the bitmap.
                renderBitmap.Render(vis);


                //Create a file stream for saving image
                using (var outStream = new FileStream(path.LocalPath, FileMode.Create))
                {
                    renderBitmap.Save(outStream);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (!useZoomControlSurface)
                surface.SetPrintMode(false, true, 100);
        }


        public static void PrintVisualDialog(Visual surface, string description = "", bool compat = false)
        {
            try
            {
                //apply layout rounding
                var isCtrl = surface is Control;
                var oldLR = false;
                if (isCtrl && compat)
                {
                    var ctrl = (Control)surface;
                    oldLR = ctrl.UseLayoutRounding;
                    if (oldLR != true) ctrl.UseLayoutRounding = true;
                }

                if (isCtrl && compat)
                {
                    var ctrl = (Control)surface;
                    ctrl.UseLayoutRounding = oldLR;
                }
            }
            catch (Exception)
            {
                Logger.Sink?.Log(LogEventLevel.Error, nameof(PrintHelper), surface,
                    "Unexpected exception occured while trying to access default printer. Please ensure that default printer is installed in your OS!");
            }
        }

        public static Bitmap PrintWithDPI(IGraphAreaBase ga, string description, double dpi, int margin = 0)
        {
            var visual = (Canvas)ga;
            var bitmap = new RenderTargetBitmap(new PixelSize((int)visual.Width, (int)visual.Height));
            ga.SetPrintMode(true, true, margin);
            //store original scale
            var originalScale = visual.RenderTransform;
            //get scale from DPI
            var scale = dpi / DEFAULT_DPI;
            //Transform the Visual to scale
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(scale, scale));
            visual.RenderTransform = group;
            //update visual
            visual.InvalidateArrange();
            visual.UpdateLayout();

            //now print the visual to printer to fit on the one page.
            bitmap.Render(visual);
            //apply the original transform.
            visual.RenderTransform = originalScale;
            ga.SetPrintMode(false, true, margin);
            return bitmap;
        }
    }
}