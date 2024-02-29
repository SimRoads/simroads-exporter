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
using System.Text;
using TsMap.Map.Overlays;

namespace TsMap.Exporter.Overlays
{
    public class OverlayExporter(TsMapper mapper) : BaseExporter(mapper)
    {
        public const int WidthLimit = 2048;

        public override void Export(ZipArchive zipArchive)
        {
            ExportSprite(zipArchive, Mapper.OverlayManager.GetOverlays(), "sprite");
        }

        private void ExportSprite(ZipArchive zipArchive, IEnumerable<MapOverlay> overlays, string name)
        {
            int width = 0, height = 0, x = 0, y = 0;
            var reference = new Dictionary<string, Dictionary<string, int>>();
            var bitmaps = new Dictionary<string, Image>();
            foreach (var item in overlays.Where(overlay => !overlay.IsSecret).DistinctBy(overlay => overlay.OverlayName)
                         .OrderByDescending(overlay => overlay.OverlayImage.GetImage().Height))
            {
                var bitmap = item.OverlayImage.GetImage().Clone<Bgra32>();
                if (bitmap.Height * bitmap.Width > 2048)
                {
                    var newWidth = Math.Sqrt(2048 / ((float)bitmap.Height / bitmap.Width));
                    bitmap.Mutate(context =>
                        context.Resize((int)newWidth, (int)(newWidth * ((float)bitmap.Height / bitmap.Width))));
                }

                reference[item.OverlayName] = new Dictionary<string, int>()
                {
                    { "x", x },
                    { "y", y },
                    { "width", bitmap.Width },
                    { "height", bitmap.Height },
                    { "pixelRatio", 1 }
                };
                bitmaps[item.OverlayName] = bitmap;
                x += bitmap.Width;
                width = Math.Max(width, x);
                height = Math.Max(height, y + bitmap.Height);
                if (x <= WidthLimit) continue;
                x = 0;
                y = height;
            }

            var image = new Image<Rgba32>(width, height);
            foreach (var (key, item) in bitmaps)
            {
                image.Mutate(context =>
                    context.DrawImage(item, new Point(reference[key]["x"], reference[key]["y"]), 1));
            }

            var zipArchiveEntry =
                zipArchive.CreateEntry(Path.Join("overlays", $"{name}.json"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reference)));
            }

            zipArchiveEntry = zipArchive.CreateEntry(Path.Join("overlays", $"{name}.png"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                image.Save(stream, new PngEncoder() { CompressionLevel = PngCompressionLevel.Level9 });
            }
        }
    }
}