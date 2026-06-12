using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Controls.Controls.Misc;

namespace Westermo.GraphX.Controls;

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


    private static ulong CalculateSize(Size desiredSize, double dpi)
    {
        return (ulong)(desiredSize.Width * (dpi / DEFAULT_DPI) + 100) *
               (ulong)(desiredSize.Height * (dpi / DEFAULT_DPI) + 100);
    }

    extension(IGraphAreaBase surface)
    {
        /// <summary>
        /// Export current graph layout into the PNG image file. layout will be saved in full size.
        /// </summary>
        public async Task ExportAsPng()
        {
            await surface.ExportAsImageDialog(ImageType.PNG);
        }

        /// <summary>
        /// Export current graph layout into the JPEG image file. layout will be saved in full size.
        /// </summary>
        /// <param name="quality">Optional image quality parameter</param>
        public async Task ExportAsJpeg(int quality = 100)
        {
            await surface.ExportAsImageDialog(ImageType.JPEG);
        }

        /// <summary>
        /// Export current graph layout into the chosen image file and format. layout will be saved in full size.
        /// </summary>
        /// <param name="itype">Image format</param>
        /// <param name="dpi">Optional image DPI parameter</param>
        public async Task ExportAsImageDialog(ImageType itype,
            double dpi = DEFAULT_DPI)
        {
            var fileType = itype.ToString();
            var fileExt = itype switch
            {
                ImageType.PNG => "*.png",
                ImageType.JPEG => "*.jpg",
                ImageType.BMP => "*.bmp",
                ImageType.GIF => "*.gif",
                ImageType.TIFF => "*.tiff",
                _ => throw new GX_InvalidDataException("ExportAsImage() -> Unknown output image format specified!"),
            };
            var top = TopLevel.GetTopLevel(surface as Control);
            if (top is null) return;
            var dlg = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Exporting graph as {fileType} image...",
                DefaultExtension = fileExt.Replace("*", ""),
                FileTypeChoices = [new FilePickerFileType(fileType) { Patterns = [fileExt] }]
            });
            if (dlg is null) return;

            surface.ExportAsImage(dlg.Path, dpi);
        }

        public void ExportAsImage(Uri filename, double dpi = DEFAULT_DPI)
        {
            ExportToImage(surface, filename, dpi);
        }

        /// <summary>
        /// Helper method which calculates estimated image DPI based on the input criterias
        /// </summary>
        /// <param name="vis">GraphArea object</param>
        /// <param name="imgdpi">Desired DPI</param>
        /// <param name="dpiStep">DPI decrease step while estimating</param>
        /// <param name="estPixelCount">Pixel quantity threshold</param>
        public double CalculateEstimatedDPI(double imgdpi, double dpiStep,
            ulong estPixelCount)
        {
            var result = false;
            var currentDPI = imgdpi;
            while (!result)
            {
                if (CalculateSize(surface.ContentSize.Size, currentDPI) <= estPixelCount)
                    result = true;
                else currentDPI -= dpiStep;
                if (currentDPI < 0) return 0;
            }

            return currentDPI;
        }

        /// <summary>
        /// Method exports the GraphArea to an png image.
        /// </summary>
        /// <param name="surface">GraphArea control</param>
        /// <param name="path">Image destination path</param>
        /// <param name="imgdpi">Optional image DPI parameter</param>
        public void ExportToImage(Uri path, double imgdpi = DEFAULT_DPI)

        {
            var vis = (Control)surface;
            var offsetX = -surface.ContentSize.Left;
            var offsetY = -surface.ContentSize.Top;
            var size = new PixelSize((int)(surface.ContentSize.Width * (imgdpi / DEFAULT_DPI) + 100),
                (int)(surface.ContentSize.Height * (imgdpi / DEFAULT_DPI) + 100));
            using (var renderBitmap = new RenderTargetBitmap(size, new Vector(imgdpi, imgdpi)))
            {
                var originalTransform = vis.RenderTransform;
                vis.RenderTransform = new TranslateTransform(offsetX, offsetY);
                renderBitmap.Render(vis);
                vis.RenderTransform = originalTransform;


                //Create a file stream for saving image
                using (var outStream = new FileStream(path.LocalPath, FileMode.Create))
                {
                    renderBitmap.Save(outStream);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
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
}