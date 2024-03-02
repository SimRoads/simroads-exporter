using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TsMap.FileSystem;
using TsMap.Helpers;
using TsMap.Helpers.Logger;
using TsMap.TsItem;

namespace TsMap.Exporter.Data
{
    public class DataExporter : MsgPackExporter
    {
        public readonly TranslationExporter Translations;
        public readonly SearchExporter Search;
        public readonly Quadtree<TsCityItem> CityTree = new();
        public new readonly TsMapper Mapper;
        public readonly ExportSettings ExportSettings;
        public Dictionary<ulong, List<ExpOverlay>> OverlaysByPrefab = new();
        public List<ExpCity> Cities = new();
        public List<ExpCountry> Countries = new();

        public DataExporter(TsMapper mapper, ExportSettings settings) : base(mapper)
        {
            Translations = new TranslationExporter(mapper);
            Search = new SearchExporter(this);
            Mapper = mapper;
            ExportSettings = settings;

            foreach (var city in mapper.Cities.Values)
            {
                CityTree.Insert(new Envelope(city.X, city.X + city.Width, city.Z, city.Z + city.Height), city);
            }

            var activeDlcGuards =
                Mapper.GetDlcGuardsForCurrentGame().Where(x => x.Enabled).Select(x => x.Index).ToList();
            foreach (var overlay in Mapper.OverlayManager.GetOverlays())
            {
                if (!activeDlcGuards.Contains(overlay.DlcGuard)) continue;
                var ov = ExpOverlay.Create(overlay, this);
                if (ov == null) continue;
                var prefabId = overlay.GetPrefabId();
                if (!OverlaysByPrefab.ContainsKey(prefabId))
                {
                    OverlaysByPrefab[prefabId] = new();
                }

                OverlaysByPrefab[prefabId].Add(ov);
            }

            Countries = Mapper.GetCountries().Select(x => new ExpCountry(x, this)).ToList();
            Cities = Mapper.GetCities().Select(x => new ExpCity(x, this)).ToList();
        }

        public Dictionary<string, object> ExportGameDetails()
        {
            var dlcManifests = UberFileSystem.Instance.GetDirectory("").GetFiles("dlc_*.manifest.sii").Select(x =>
                Encoding.UTF8.GetString(UberFileSystem.Instance.GetFile(x).Entry.Read()));
            var dlcs = new List<string>();
            foreach (var manifest in dlcManifests)
            {
                manifest.Split('\n').ToList().ForEach(x =>
                {
                    var (validLine, key, value) = SiiHelper.ParseLine(x);
                    if (!validLine) return;
                    if (key == "display_name")
                    {
                        dlcs.Add(value.Split("\"")[1]);
                    }
                });
            }

            var modsManifests = UberFileSystem.Instance.GetDirectory("").GetFiles("manifest.sii").Select(x =>
                Encoding.UTF8.GetString(UberFileSystem.Instance.GetFile(x).Entry.Read()));
            var mods = new List<string>();
            foreach (var manifest in modsManifests)
            {
                bool validMod = false;
                manifest.Split('\n').ToList().ForEach(x =>
                {
                    var (validLine, key, value) = SiiHelper.ParseLine(x);
                    if (!validLine) return;
                    if (key == "display_name")
                    {
                        if (validMod) mods.Add(value.Split("\"")[1]);
                    }
                    else if (key == "mod_package")
                    {
                        validMod = true;
                    }
                });
            }

            return new Dictionary<string, object>
            {
                { "mapName", ExportSettings.MapName },
                { "game", Mapper.IsEts2 ? "eut_2" : "ats" },
                { "dlc", dlcs },
                {
                    "version",
                    Encoding.UTF8.GetString(UberFileSystem.Instance.GetFile("version.txt").Entry.Read()).Trim('\n')
                },
                { "mods", mods },
                {
                    "size",
                    new Dictionary<string, float>
                    {
                        { "maxX", Mapper.maxX }, { "minX", Mapper.minX }, { "maxZ", Mapper.maxZ },
                        { "minZ", Mapper.minZ }
                    }
                },
                { "zoomLimit", ExportSettings.ZoomLimit },
                { "timestamp", DateTime.Now.ToFileTime() },
                {
                    "mapSettings", new Dictionary<string, object>
                    {
                        { "scale", Mapper.MapSettings.Scale }, { "uiCorrections", Mapper.MapSettings.HasUICorrections }
                    }
                }
            };
        }

        public override void Export(ZipArchive zipArchive)
        {
            WriteMsgPack(zipArchive, Path.Join("json", "map.msgpack"), ExportGameDetails());
            Logger.Instance.Info($"Exported map file");

            WriteMsgPack(zipArchive, Path.Join("json", "countries.msgpack"),
                Countries.Select(x => x.ExportList()).ToList());
            Logger.Instance.Info($"Exported countries file");

            WriteMsgPack(zipArchive, Path.Join("json", "cities.msgpack"),
                Cities.Select(x => x.ExportList()).ToList());
            Logger.Instance.Info($"Exported countries file");

            int i;
            for (i = 0; i < Countries.Count; i++)
            {
                var c = Countries[i];
                WriteMsgPack(zipArchive, Path.Join("json", "overlays", c.GetId() + ".msgpack"), c.ExportDetail());
                Logger.Instance.Info($"Exported country {i + 1}/{Countries.Count}");
            }

            for (i = 0; i < Cities.Count; i++)
            {
                var c = Cities[i];
                WriteMsgPack(zipArchive, Path.Join("json", "overlays", c.GetId() + ".msgpack"), c.ExportDetail());
                Logger.Instance.Info($"Exported city {i + 1}/{Cities.Count}");
            }

            i = 0;
            foreach (var (prefabId, overlay) in OverlaysByPrefab)
            {
                WriteMsgPack(zipArchive, Path.Join("json", "overlays", prefabId + ".msgpack"),
                    overlay.Select(x => x.ExportDetail()).ToList());
                Logger.Instance.Info($"Exported overlay {i + 1}/{OverlaysByPrefab.Count}");
                i++;
            }

            Translations.Export(zipArchive);
            Search.Export(zipArchive);
        }
    }
}