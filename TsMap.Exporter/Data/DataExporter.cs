using MessagePack;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TsMap.TsItem;

namespace TsMap.Exporter.Data
{
    public class DataExporter : BaseExporter
    {
        public readonly TranslationExporter Translations;
        public readonly TsMapper Mapper;
        public readonly Quadtree<TsCityItem> cityTree = new();
        public readonly KdTree<TsNode> nodeIndex = new();

        private static MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        public DataExporter(TsMapper mapper) : base(mapper)
        {
            Translations = new TranslationExporter(mapper);
            Mapper = mapper;

            foreach (var city in mapper.Cities.Values)
            {
                cityTree.Insert(new Envelope(city.X, city.Z, city.X + city.Width, city.Z + city.Height), city);
            }
        }

        public override void Export(ZipArchive zipArchive)
        {
            Dictionary<ulong, List<ExpOverlay>> overlaysToPrefab = new();

            var activeDlcGuards = Mapper.GetDlcGuardsForCurrentGame().Where(x => x.Enabled).Select(x => x.Index).ToList();
            foreach (var overlay in Mapper.OverlayManager.GetOverlays())
            {
                if (!activeDlcGuards.Contains(overlay.DlcGuard)) continue;
                var ov = ExpOverlay.Create(overlay, this);
                if (ov == null) continue;
                var prefabId = overlay.GetPrefabId();
                if (!overlaysToPrefab.ContainsKey(prefabId))
                {
                    overlaysToPrefab[prefabId] = new();
                }
                overlaysToPrefab[prefabId].Add(ov);
            }
            List<ExpCountry> expCountries = Mapper.GetCountries().Select(x => new ExpCountry(x, this)).ToList();
            List<ExpCity> expCities = Mapper.GetCities().Select(x => new ExpCity(x, this)).ToList();

            ZipArchiveEntry zipFile = zipArchive.CreateEntry(Path.Join("json", "countries.msgpack"), CompressionLevel.Fastest);
            using (var stream = zipFile.Open())
            {
                var s = expCountries.Select(x => x.ExportList()).ToList();
                stream.Write(MessagePackSerializer.Serialize(expCountries.Select(x => x.ExportList()), Options));
                Console.WriteLine($"Exported countries file");
            }

            zipFile = zipArchive.CreateEntry(Path.Join("json", "cities.msgpack"), CompressionLevel.Fastest);
            using (var stream = zipFile.Open())
            {
                stream.Write(MessagePackSerializer.Serialize(expCities.Select(x => x.ExportList()), Options));
                Console.WriteLine($"Exported countries file");
            }

            int i = 0;
            for (i = 0; i < expCountries.Count; i++)
            {
                var c = expCountries[i];
                zipFile = zipArchive.CreateEntry(Path.Join("json", "export", c.GetId().ToString() + ".msgpack"), CompressionLevel.Fastest);
                using (var stream = zipFile.Open())
                {
                    stream.Write(MessagePackSerializer.Serialize(c.ExportDetail(), Options));
                    Console.WriteLine($"Exported country {i + 1}/{expCountries.Count}");
                }
            }

            for (i = 0; i < expCities.Count; i++)
            {
                var c = expCities[i];
                zipFile = zipArchive.CreateEntry(Path.Join("json", "export", c.GetId().ToString() + ".msgpack"), CompressionLevel.Fastest);
                using (var stream = zipFile.Open())
                {
                    stream.Write(MessagePackSerializer.Serialize(c.ExportDetail(), Options));
                    Console.WriteLine($"Exported city {i + 1}/{expCities.Count}");
                }
            }

            i = 0;
            foreach (var (prefabId, overlay) in overlaysToPrefab)
            {
                zipFile = zipArchive.CreateEntry(Path.Join("json", "export", prefabId + ".msgpack"), CompressionLevel.Fastest);
                using (var stream = zipFile.Open())
                {
                    stream.Write(MessagePackSerializer.Serialize(overlay.Select(x => x.ExportDetail()), Options));
                    Console.WriteLine($"Exported overlay {i + 1}/{overlaysToPrefab.Count}");
                }
                i++;
            }

            Translations.Export(zipArchive);
        }
    }
}
