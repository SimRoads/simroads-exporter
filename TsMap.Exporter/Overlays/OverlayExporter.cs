using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TsMap.Map.Overlays;

namespace TsMap.Exporter.Overlays
{
    public class OverlayExporter : BaseExporter
    {
        public int WidthLimit = 2048;

        public OverlayExporter(TsMapper mapper) : base(mapper)
        {
        }

        public override void Export(ZipArchive zipArchive)
        {
            ExportSprite(zipArchive, mapper.OverlayManager.GetOverlays(), "overlays");

        }

        private void ExportSprite(ZipArchive zipArchive, IEnumerable<MapOverlay> overlays, string name)
        {
            int width = 0, height = 0, x = 0, y = 0;
            var reference = new Dictionary<string, Dictionary<string, int>>();
            var bitmaps = new Dictionary<string, Eto.Drawing.Bitmap>();
            foreach (var item in overlays.Where(x => !x.IsSecret).DistinctBy(x => x.OverlayName).OrderByDescending(x => x.GetBitmap().Height))
            {
                var bitmap = item.GetBitmap();
                reference[item.OverlayName] = new Dictionary<string, int>()
                {
                    { "x", x },
                    { "y", y },
                    { "width", bitmap.Width },
                    { "height", bitmap.Height }
                };
                bitmaps[item.OverlayName] = bitmap;
                x += bitmap.Width;
                width = Math.Max(width, x);
                height = Math.Max(height, y + bitmap.Height);
                if (x > WidthLimit)
                {
                    x = 0;
                    y = height;
                }
            }

            var image = new Eto.Drawing.Bitmap(width, height, Eto.Drawing.PixelFormat.Format32bppRgba);
            var graphics = new Eto.Drawing.Graphics(image);
            foreach (var (key, item) in bitmaps)
            {
                graphics.DrawImage(item, reference[key]["x"], reference[key]["y"]);
            }
            graphics.Dispose();

            var zipArchiveEntry = zipArchive.CreateEntry(Path.Join("overlays", $"{name}.json"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reference)));
            }
            zipArchiveEntry = zipArchive.CreateEntry(Path.Join("overlays", $"{name}.png"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                image.Save(stream, Eto.Drawing.ImageFormat.Png);
            }

        }
    }
}
