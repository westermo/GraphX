using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ShowcaseApp.Avalonia.Models
{
    public static class ImageLoader
    {
        private static readonly List<Bitmap?> Images = [];

        static ImageLoader()
        {
            TryAdd("circle_red.png");
            TryAdd("circle_blue.png");
            TryAdd("circle_green.png");
        }

        private static void TryAdd(string fileName)
        {
            try
            {
                var uri = new Uri($"avares://ShowcaseApp.Avalonia/Assets/{fileName}");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    Images.Add(new Bitmap(stream));
                    return;
                }
            }
            catch
            {
                /* ignore and add null placeholder */
            }

            Images.Add(null);
        }

        public static Bitmap? GetImageById(int id)
        {
            return id < 0 || id >= Images.Count ? null : Images[id];
        }
    }
}