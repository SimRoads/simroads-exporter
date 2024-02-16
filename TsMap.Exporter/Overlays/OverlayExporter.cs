using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using TsMap.Map.Overlays;

namespace TsMap.Exporter.Overlays
{
    public class OverlayExporter : BaseExporter
    {
        public int WidthLimit = 2048;

        private Dictionary<string, Dictionary<string, int>> overlays = new();
        private Image<Rgba32> sprite;


        public OverlayExporter(TsMapper mapper) : base(mapper)
        {
        }

        public override void Prepare()
        {
            int width = 0, height = 0, x = 0, y = 0;
            var images = new Dictionary<string, Image>();
            foreach (var item in Mapper.OverlayManager.GetOverlays().Where(x => !x.IsSecret).DistinctBy(x => x.OverlayName).OrderByDescending(x => x.OverlayImage.GetImage().Height))
            {
                var bitmap = item.OverlayImage.GetImage();
                overlays[item.OverlayName] = new ()
                {
                    { "x", x },
                    { "y", y },
                    { "width", bitmap.Width },
                    { "height", bitmap.Height }
                };
                images[item.OverlayName] = bitmap;
                x += bitmap.Width;
                width = Math.Max(width, x);
                height = Math.Max(height, y + bitmap.Height);
                if (x > WidthLimit)
                {
                    x = 0;
                    y = height;
                }
            }

            sprite = new Image<Rgba32>(width, height);
            foreach (var (key, item) in images)
            {
                sprite.Mutate(x => x.DrawImage(item, new Point(overlays[key]["x"], overlays[key]["y"]), 1));
            }
        }

        public object ExportReference()
        {
            return overlays;
        }

        public byte[] ExportSprite()
        {
            using (var stream = new MemoryStream())
            {
                sprite.Save(stream, new PngEncoder() { CompressionLevel = PngCompressionLevel.Level9 });
                return stream.ToArray();
            }
        }
    }
}
