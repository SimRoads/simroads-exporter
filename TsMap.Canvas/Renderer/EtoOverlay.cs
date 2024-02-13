using Eto.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using TsMap.Map.Overlays;

namespace TsMap.Canvas.Renderer
{
    public static class OverlayImageExtension
    {
        private static Dictionary<OverlayImage, Bitmap> bitmaps = new();

        public static Bitmap GetBitmap(this OverlayImage overlayImage)
        {
            if (!bitmaps.ContainsKey(overlayImage))
            {
                int width = (int)overlayImage.Width, height = (int)overlayImage.Height;
                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgba);
                var bd = bitmap.Lock();
                var ptr = bd.Data;


                var bytes = new byte[width * height * 4];
                for (var i = 0; i < overlayImage.PixelData.Length; ++i)
                {
                    var pixel = overlayImage.PixelData[i];
                    bytes[i * 4 + 3] = pixel.A;
                    bytes[i * 4] = pixel.B;
                    bytes[i * 4 + 1] = pixel.G;
                    bytes[i * 4 + 2] = pixel.R;
                }

                Marshal.Copy(bytes, 0, ptr, bitmap.Width * bitmap.Height * 0x4);

                bd.Dispose();
                bitmaps.Add(overlayImage, bitmap);
            }
            return bitmaps[overlayImage];
        }

    }
}
