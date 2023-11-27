using Google.Protobuf;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using TsMap.TsItem;
using VectorTile;
using static VectorTile.Tile.Types;

namespace TsMap.Exporter
{
    public enum MapboxCommandType
    {
        MoveTo = 1,
        LineTo = 2,
        ClosePath = 7
    }

    public class MvtExporter
    {
        private readonly TsMapper _mapper;
        private const float itemDrawMargin = 1000f;
        private const float discretizationThreshold = 0.05f;

        public MvtExporter(TsMapper mapper) { _mapper = mapper; }

        public void ExportMap(string path, int zoomLimit = -1)
        {
            float size = Math.Max(_mapper.maxX - _mapper.minX, _mapper.maxZ - _mapper.minZ) + itemDrawMargin * 2;
            for (int i = 0; i <= zoomLimit; i++)
            {
                float tileSize = (float)(size / Math.Pow(2, i));
                for (int j = 0; j < Math.Pow(2, i); j++)
                {
                    float top = _mapper.minZ - itemDrawMargin + tileSize * j;
                    float bottom = top + tileSize;
                    for (int k = 0; k < Math.Pow(2, i); k++)
                    {
                        float left = _mapper.minX - itemDrawMargin + tileSize * k;
                        float right = left + tileSize;
                        string tilePath = Path.Join(path, i.ToString(), j.ToString());
                        Directory.CreateDirectory(tilePath);
                        if (File.Exists(Path.Join(tilePath, k + ".mvt"))) File.Delete(Path.Join(tilePath, k + ".mvt"));
                        using (var stream = File.OpenWrite(Path.Join(tilePath, k + ".mvt")))
                        {
                            ExportTile(left, right, top, bottom, stream);
                        }
                    }
                }
            }
        }

        public void ExportTile(float west, float east, float north, float south, Stream output)
        {
            var bounds = new Envelope(west - itemDrawMargin, east + itemDrawMargin, south + itemDrawMargin, north - itemDrawMargin);
            var extent = Math.Max(east - west, north - south);
            var roadsLayer = new Layer { Name = "roads", Extent=(uint)Math.Floor(extent) };

            foreach (var item in _mapper.Index.Query(bounds))
            {
                if (item.Data is TsRoadItem road)
                {
                    if (road.IsSecret) continue;

                    uint cursorX = 0, cursorY = 0;
                    var startNode = road.GetStartNode();
                    var endNode = road.GetEndNode();
                    var sx = startNode.X;
                    var sz = startNode.Z;
                    var ex = endNode.X;
                    var ez = endNode.Z;

                    var radius = Math.Sqrt(Math.Pow(sx - ex, 2) + Math.Pow(sz - ez, 2));
                    if (road.RoadLook.IsOneWay() && (radius / extent < discretizationThreshold)) continue;

                    var tanSx = Math.Cos(-(Math.PI * 0.5f - startNode.Rotation)) * radius;
                    var tanEx = Math.Cos(-(Math.PI * 0.5f - endNode.Rotation)) * radius;
                    var tanSz = Math.Sin(-(Math.PI * 0.5f - startNode.Rotation)) * radius;
                    var tanEz = Math.Sin(-(Math.PI * 0.5f - endNode.Rotation)) * radius;

                    int hermiteSteps = Math.Max(2, (int)(16 * (radius / extent)));
                    var points = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };

                    for (var i = 0; i < hermiteSteps; i++)
                    {
                        var s = i / (float)(hermiteSteps - 1);
                        var x = (float)TsRoadLook.Hermite(s, sx, ex, tanSx, tanEx);
                        var z = (float)TsRoadLook.Hermite(s, sz, ez, tanSz, tanEz);
                        if (i == 1) points.Add(GenerateCommandInteger(MapboxCommandType.LineTo, hermiteSteps - 1));
                        points.AddRange(GenerateDeltaFromGame(x, z, west, north, ref cursorX, ref cursorY));
                    }

                    var feature = new Feature() { Type = GeomType.Linestring, Geometry = { points } };
                    roadsLayer.Features.Add(feature);
                }
            }

            var vt = new Tile();

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


            vt.Layers.Add(roadsLayer);

            vt.WriteTo(output);
        }

        private static uint GenerateCommandInteger(MapboxCommandType command, int count)
        {
            return (uint)(command & MapboxCommandType.ClosePath) | (uint)(count << 3);
        }

        private static uint GenerateParameterInteger(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        private static uint[] GenerateDelta(uint x, uint y, ref uint cursorX, ref uint cursorY)
        {
            var dx = (int)(x - cursorX);
            var dy = (int)(y - cursorY);
            cursorX = x;
            cursorY = y;
            return new uint[] { GenerateParameterInteger(dx), GenerateParameterInteger(dy) };
        }

        private uint[] GenerateDeltaFromGame(float x, float y, float west, float north, ref uint cursorX, ref uint cursorY)
        {
            return GenerateDelta((uint)(x-west), (uint)(y-north), ref cursorX, ref cursorY);
        }
    }
}
