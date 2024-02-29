using Google.Protobuf;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TsMap.Exporter.Mvt.MvtExtensions;
using TsMap.Helpers.Logger;
using static TsMap.Exporter.Mvt.Tile.Types;

namespace TsMap.Exporter.Mvt
{
    public class ExportSettings
    {
        public readonly float West, East, North, South;
        public readonly float Extent;
        public readonly Envelope Envelope;
        public readonly Envelope SearchEnvelope;
        public readonly List<byte> ActiveDlcGuards;
        public readonly float DiscretizationThreshold;

        private const float ItemDrawMargin = 500f;
        public const float MinDiscretizationThreshold = 0.01f;
        private const uint TileExtent = 4096;

        public ExportSettings(uint z, uint x, uint y, uint maxZoom, TsMapper mapper)
        {
            float size = Math.Max(Math.Abs(mapper.maxX - mapper.minX), Math.Abs(mapper.maxZ - mapper.minZ));
            float tileSize = (float)(size / Math.Pow(2, z));
            West = mapper.minX + tileSize * x;
            East = West + tileSize;
            North = mapper.minZ + tileSize * y;
            South = North + tileSize;
            DiscretizationThreshold =
                -z * MinDiscretizationThreshold / maxZoom +
                MinDiscretizationThreshold; //(float)Math.Pow(Math.E, (-Math.Log(MinDiscretizationThreshold)/maxZoom)*((int)z-maxZoom));
            Extent = (uint)Math.Max(East - West, North - South);
            Envelope = new Envelope(West, East, South, North);
            SearchEnvelope = new Envelope(West - ItemDrawMargin, East + ItemDrawMargin, South + ItemDrawMargin,
                North - ItemDrawMargin);
            this.ActiveDlcGuards = mapper.GetDlcGuardsForCurrentGame().Where(dlcGuard => dlcGuard.Enabled)
                .Select(dlcGuard => dlcGuard.Index).ToList();
        }

        public uint[] GenerateDeltaFromGame(float x, float y, ref uint cursorX, ref uint cursorY)
        {
            return GenerateDelta((uint)((x - West) / Envelope.MaxExtent * TileExtent),
                (uint)(((y - North) / Envelope.MaxExtent) * TileExtent),
                ref cursorX,
                ref cursorY);
        }

        public uint[] GenerateDeltaFromGame(float x, float y)
        {
            return GenerateDelta((uint)((x - West) / Envelope.MaxExtent * TileExtent),
                (uint)((y - North) / Envelope.MaxExtent * TileExtent));
        }

        internal uint GenerateCommandInteger(MapboxCommandType command, int count)
        {
            return VectorTileUtils.GenerateCommandInteger(command, count);
        }

        internal uint GenerateParameterInteger(int value)
        {
            return VectorTileUtils.GenerateParameterInteger(value);
        }

        internal uint[] GenerateDelta(uint x, uint y, ref uint cursorX, ref uint cursorY)
        {
            return VectorTileUtils.GenerateDelta(x, y, ref cursorX, ref cursorY);
        }

        internal uint[] GenerateDelta(uint x, uint y)
        {
            return VectorTileUtils.GenerateDelta(x, y);
        }

        internal Layer CreateLayer(string name)
        {
            return new Layer { Name = name, Extent = TileExtent };
        }
    }

    public struct Layers
    {
        public ExportSettings Settings;
        public Layer Roads;
        public Layer Prefabs;
        public Layer Bounds;
        public Layer Ferries;
        public Layer Overlays;


        public Layers(ExportSettings settings)
        {
            Settings = settings;
            Roads = Settings.CreateLayer("roads");
            Bounds = Settings.CreateLayer("bounds");
            Prefabs = Settings.CreateLayer("prefabs");
            Ferries = Settings.CreateLayer("ferries");
            Overlays = Settings.CreateLayer("overlays");
        }

        public Tile GetTile()
        {
            var vt = new Tile();

            vt.Layers.Add(Roads);
            vt.Layers.Add(Bounds);
            vt.Layers.Add(Prefabs);
            vt.Layers.Add(Ferries);
            vt.Layers.Add(Overlays);

            return vt;
        }
    }

    public class MvtExporter : BaseExporter
    {
        public readonly Quadtree<MvtExtension> AreaIndex = new();
        public readonly Quadtree<MvtExtension> OverlayIndex = new(), CityIndex = new();

        public uint ZoomLimit;

        private static uint _maxTasks = (uint)Environment.ProcessorCount;

        public MvtExporter(TsMapper mapper, uint zoomLimit = 7) : base(mapper)
        {
            this.ZoomLimit = zoomLimit;
            Initialize();
        }

        private void Initialize()
        {
            foreach (var item in Mapper.Roads.Values.Select(x => new MvtRoadItem(x, Mapper)).Cast<RectMvtExtension>()
                         .Concat(
                             Mapper.Prefabs.Values.Select(x => new MvtPrefabItem(x, Mapper))).Concat(
                             Mapper.Boundaries.SelectMany(pair =>
                                 pair.Value.Select(x => new MvtBoundary(x, pair.Key, Mapper)))).Concat(
                             Mapper.MapAreas.Values.Select(x => new MvtMapAreaItem(x, Mapper))).Concat(
                             Mapper.FerryPorts.Values.SelectMany(x =>
                                 x.Ferry.GetConnections().Where(ferry => ferry.StartPort.Token > ferry.EndPort.Token)
                                     .Select(ferry => new MvtFerryItem(ferry, Mapper)))).Concat(
                             Mapper.Cities.Values.Select(x => new MvtCityItem(x, Mapper))))
            {
                item.AddTo(AreaIndex);
            }

            foreach (var item in Mapper.OverlayManager.GetOverlays().Select(x => (new MvtOverlay(x, Mapper))))
                item.AddTo(OverlayIndex);
        }

        private IEnumerable<MvtExtension> GetItems(ExportSettings sett)
        {
            return AreaIndex.Query(sett.SearchEnvelope).Concat(OverlayIndex.Query(sett.SearchEnvelope))
                .Concat(CityIndex.Query(sett.SearchEnvelope));
        }

        public override void Export(ZipArchive archive)
        {
            var runs = new List<Task>();
            for (uint i = 0; i <= ZoomLimit; i++)
            {
                if (runs.Count == _maxTasks)
                {
                    Task.WaitAny(runs.ToArray());
                    runs.Where(x => x.IsCompleted).ToList().ForEach(x => runs.Remove(x));
                }

                var i1 = i;
                runs.Add(Task.Run(() => ExportZoomLevel(i1, archive)));
            }

            Task.WaitAll(runs.ToArray());
        }

        private void ExportZoomLevel(uint z, ZipArchive archive)
        {
            var max = (int)Math.Pow(2, z);
            for (uint j = 0; j < max; j++)
            {
                for (uint k = 0; k < max; k++)
                {
                    ZipArchiveEntry zipArchiveEntry;
                    lock (archive)
                    {
                        zipArchiveEntry = archive.CreateEntry(
                            Path.Join("mvt", z.ToString(), j.ToString(), k + ".mvt"),
                            CompressionLevel.Fastest);
                    }

                    using var stream = zipArchiveEntry.Open();
                    var settings = new ExportSettings(z, j, k, this.ZoomLimit, Mapper);
                    ExportTile(settings, stream);
                }

                Logger.Instance.Info($"Exported tile {z}/{j}/*");
            }
        }

        private ExportSettings ExportTile(ExportSettings settings, Stream output)
        {
            var layers = new Layers(settings);

            foreach (var item in GetItems(settings).Where(i => !(i.Skip(settings) || i.Discretizate(settings)))
                         .OrderBy(i => i.Order()))
            {
                item.SaveMvtLayers(settings, layers);
            }

            layers.GetTile().WriteTo(output);

            return settings;
        }
    }
}