using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using SixLabors.ImageSharp.Processing;
using TsMap.Map.Overlays;

namespace TsMap.Exporter.Overlays
{
    public static class OverlayExtension
    {
        private static Dictionary<OverlayImage, Image<Bgra32>> _images = new();

        public static Image<Bgra32> GetImage(this OverlayImage overlay)
        {
            if (!_images.ContainsKey(overlay))
            {
                int width = (int)overlay.Width, height = (int)overlay.Height;
                var bytes = new byte[width * height * 4];
                for (var i = 0; i < overlay.PixelData.Length; ++i)
                {
                    var pixel = overlay.PixelData[i];
                    bytes[i * 4 + 3] = pixel.A;
                    bytes[i * 4] = pixel.B;
                    bytes[i * 4 + 1] = pixel.G;
                    bytes[i * 4 + 2] = pixel.R;
                }

                var im = Image.LoadPixelData<Bgra32>(bytes, width, height);
                if (im.Height * im.Width < 128 * 128)
                {
                    int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
                    for (int x = 0; x < im.Width; x++)
                    {
                        for (int y = 0; y < im.Height; y++)
                        {
                            var pixel = im[x, y];
                            if (pixel.A != 0)
                            {
                                minX = Math.Min(x, minX);
                                minY = Math.Min(y, minY);
                                maxX = Math.Max(x, maxX);
                                maxY = Math.Max(y, maxY);
                            }
                        }
                    }

                    im.Mutate(i => i.Crop(new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1)));
                }

                _images.Add(overlay, im);
            }

            return _images[overlay];
        }
    }
}