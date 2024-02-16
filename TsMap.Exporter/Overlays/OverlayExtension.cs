using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TsMap.Map.Overlays;

namespace TsMap.Exporter.Overlays
{
    public static class OverlayExtension
    {
        private static Dictionary<OverlayImage, Image> images = new();

        public static Image GetImage(this OverlayImage overlay)
        {
            if (!images.ContainsKey(overlay))
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

                images.Add(overlay, Image.LoadPixelData<Rgba32>(bytes, width, height));
            }
            return images[overlay];
        }
    }
}
