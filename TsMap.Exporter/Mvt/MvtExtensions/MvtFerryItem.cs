using System;
using System.Collections.Generic;
using TsMap.TsItem;
using static TsMap.Exporter.Mvt.VectorTileUtils;
using static TsMap.Exporter.Mvt.Tile.Types;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtFerryItem : RectMvtExtension
    {
        public readonly TsFerryConnection Conn;

        public MvtFerryItem(TsFerryConnection Conn, TsMapper mapper) : base(mapper)
        {
            this.Conn = Conn;
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            uint cursorX = 0, cursorY = 0;

            var points = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };
            var (x, z) = Mapper.MapSettings.Correct(Conn.StartPortLocation.X, Conn.StartPortLocation.Y);
            points.AddRange(sett.GenerateDeltaFromGame(x, z, ref cursorX, ref cursorY));

            points.Add(GenerateCommandInteger(MapboxCommandType.LineTo, Conn.Connections.Count + 1));
            foreach (var point in Conn.Connections)
            {
                (x, z) = Mapper.MapSettings.Correct(point.X, point.Z);
                points.AddRange(sett.GenerateDeltaFromGame(x, z, ref cursorX, ref cursorY));
            }
            (x, z) = Mapper.MapSettings.Correct(Conn.EndPortLocation.X, Conn.EndPortLocation.Y);
            points.AddRange(sett.GenerateDeltaFromGame(x, z, ref cursorX, ref cursorY));

            layers.ferries.Features.Add(new Feature { Id =Conn.GetId(), Type = GeomType.Linestring, Geometry = { points } });
            return true;
        }

        protected override Envelope CalculateEnvelope()
        {
            var mapSettings = Mapper.MapSettings;
            var (startX, startY) = mapSettings.Correct(Conn.StartPortLocation.X, Conn.StartPortLocation.Y);
            var (endX, endY) = mapSettings.Correct(Conn.EndPortLocation.X, Conn.EndPortLocation.Y);
            return new Envelope(startX, endX, startY, endY);
        }
    }
}
