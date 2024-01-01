using Google.Protobuf;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TsMap.Exporter.Mvt.MvtExtensions;
using static TsMap.Exporter.Mvt.Tile.Types;

namespace TsMap.Exporter.Mvt
{
    public class ExportSettings
    {
        public readonly float West, East, North, South;
        public readonly uint Extent;
        public readonly Envelope Envelope;
        public readonly Envelope SearchEnvelope;
        public readonly Exporter Exporter;
        public readonly List<byte> ActiveDlcGuards;

        public float ItemDrawMargin = 500f;
        public readonly float DiscretizationThreshold;

        public const float MinDiscretizationThreshold = 0.01f;

        public ExportSettings(uint z, uint x, uint y, uint maxZoom, TsMapper mapper, Exporter exporter)
        {
            float size = Math.Max(mapper.maxX - mapper.minX, mapper.maxZ - mapper.minZ);
            float tileSize = (float)(size / Math.Pow(2, z));
            this.West = mapper.minX + tileSize * y;
            this.East = West + tileSize;
            this.North = mapper.minZ + tileSize * x;
            this.South = North + tileSize;
            this.DiscretizationThreshold = -x * MinDiscretizationThreshold / maxZoom + MinDiscretizationThreshold;//(float)Math.Pow(Math.E, (-Math.Log(MinDiscretizationThreshold)/maxZoom)*((int)z-maxZoom));
            Extent = (uint)Math.Max(East - West, North - South);
            Envelope = new Envelope(West, East, South, North);
            SearchEnvelope = new Envelope(West - ItemDrawMargin, East + ItemDrawMargin, South + ItemDrawMargin, North - ItemDrawMargin);
            this.ActiveDlcGuards = mapper.GetDlcGuardsForCurrentGame().Where(x => x.Enabled).Select(x => x.Index).ToList();
            Exporter = exporter;
        }

        public uint[] GenerateDeltaFromGame(float x, float y, ref uint cursorX, ref uint cursorY)
        {
            return GenerateDelta((uint)(x - West), (uint)(y - North), ref cursorX, ref cursorY);
        }

        public uint[] GenerateDeltaFromGame(float x, float y)
        {
            return GenerateDelta((uint)(x - West), (uint)(y - North));
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

    }

    public struct Layers
    {
        public ExportSettings settings;
        public Layer roads;
        public Layer prefabs;
        public Layer bounds;
        public Layer ferries;
        public Layer overlays;


        public Layers(ExportSettings settings)
        {
            roads = new Layer { Name = "roads", Extent = settings.Extent };
            bounds = new Layer { Name = "bounds", Extent = settings.Extent };
            prefabs = new Layer { Name = "prefabs", Extent = settings.Extent };
            ferries = new Layer { Name = "ferries", Extent = settings.Extent };
            overlays = new Layer { Name = "overlays", Extent = settings.Extent };
            this.settings = settings;
        }

        public Tile GetTile()
        {
            var vt = new Tile();

            vt.Layers.Add(roads);
            vt.Layers.Add(bounds);
            vt.Layers.Add(prefabs);
            vt.Layers.Add(ferries);
            vt.Layers.Add(overlays);

            return vt;
        }
    }

    public class Exporter
    {

        private readonly TsMapper mapper;

        public readonly Quadtree<MvtExtension> AreaIndex = new();
        public readonly Quadtree<MvtExtension> OverlayIndex = new(), CityIndex = new();

        public Exporter(TsMapper mapper) { this.mapper = mapper; Initialize(); }

        private void Initialize()
        {
            foreach (var item in mapper.Roads.Values.Select(x => new MvtRoadItem(x, mapper)).Cast<RectMvtExtension>().Concat(
                mapper.Prefabs.Values.Select(x => new MvtPrefabItem(x, mapper))).Concat(
                mapper.Boundaries.QueryAll().Select(x => new MvtBoundary(x, mapper))).Concat(
                mapper.MapAreas.Values.Select(x => new MvtMapAreaItem(x, mapper))).Concat(
                mapper.GetFerryConnections().Select(x => new MvtFerryItem(x, mapper))).Concat(
                mapper.Cities.Values.Select(x => new MvtCityItem(x, mapper))))
            {
                item.AddTo(AreaIndex);
            }


            foreach (var item in mapper.OverlayManager.GetOverlays().Select(x => (new MvtOverlay(x, mapper)))) item.AddTo(OverlayIndex);
            /*foreach (var item in mapper.OverlayManager.GetOverlays().Select(x => (new MvtOverlay(x, mapper)))) item.AddTo(OverlayIndex);
            OverlayIndex.Insert(new Coordinate(overlay.Position.X, overlay.Position.Y), overlay);

            foreach (TsFerryConnection conn in _mapper.GetFerryConnections())
            {
                FerryConnectionIndex.Insert(new Envelope(conn.StartPortLocation.X, conn.StartPortLocation.Y, conn.EndPortLocation.X, conn.EndPortLocation.Y), conn);
            }*/
        }

        private IEnumerable<MvtExtension> GetItems(ExportSettings sett)
        {
            return AreaIndex.Query(sett.SearchEnvelope).Cast<MvtExtension>().Concat(OverlayIndex.Query(sett.SearchEnvelope)).Concat(CityIndex.Query(sett.SearchEnvelope));
        }

        public void ExportMap(string path, uint zoomLimit = 7)
        {
            using (var fileStream = new FileStream(Path.Join(path, "test.zip"), FileMode.Create))
            {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    for (uint i = 0; i <= zoomLimit; i++)
                    {
                        for (uint j = 0; j < Math.Pow(2, i); j++)
                        {
                            for (uint k = 0; k < Math.Pow(2, i); k++)
                            {
                                var zipArchiveEntry = archive.CreateEntry(Path.Join(i.ToString(), j.ToString(), k + ".mvt"), CompressionLevel.Fastest);
                                using (var stream = zipArchiveEntry.Open())
                                {
                                    var settings = new ExportSettings(i, j, k, zoomLimit, mapper, this);
                                    var result = ExportTile(settings, stream);
                                    Console.WriteLine($"Exported tile {i}/{j}/{k}");
                                }

                            }
                        }
                    }
                }
            }

        }

        public ExportSettings ExportTile(ExportSettings settings, Stream output)
        {
            var layers = new Layers(settings);

            foreach (var item in GetItems(settings))
            {
                if (item.Skip(settings) || item.Discretizate(settings)) continue;
                item.SaveMvtLayers(settings, layers);
            }

            layers.GetTile().WriteTo(output);

            return settings;
        }

        /*var geometry = new LineString((new Coordinate[] { new Coordinate(0, 0), new Coordinate(10, 10) }));
        var feature = new Feature(geometry, new AttributesTable { { "uid", 5 } });
        roadsLayer.Features.Add(feature);


        geometry = new LineString((new Coordinate[] { new Coordinate(20, 20), new Coordinate(10, 10) }));
        feature = new Feature(geometry, new AttributesTable { { "uid", 5 } });
        roadsLayer.Features.Add(feature);

        geometry = new LineString((new Coordinate[] { new Coordinate(0, 20), new Coordinate(10, 10) }));
        feature = new Feature(geometry, new AttributesTable { { "uid", 5 } });
        roadsLayer.Features.Add(feature);

        geometry = new LineString((new Coordinate[] { new Coordinate(20, 0), new Coordinate(10, 10) }));
        feature = new Feature(geometry, new AttributesTable { { "uid", 5 } });
        roadsLayer.Features.Add(feature);

        geometry = GeometryFactory.Fixed.CreateLineString(new Coordinate[] { new Coordinate(-20, -20), new Coordinate(100090, 100090) });
        feature = new Feature(geometry, new AttributesTable { { "uid", 5 } });
        roadsLayer.Features.Add(feature);*/


    }
}
