using System;
using System.Collections.Generic;
using TsMap.TsItem;
using static TsMap.Exporter.Mvt.VectorTileUtils;
using static TsMap.Exporter.Mvt.Tile.Types;
using NetTopologySuite.Geometries;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtRoadItem : RectMvtExtension
    {
        public readonly TsRoadItem Road;

        public MvtRoadItem(TsRoadItem road, TsMapper mapper) : base(mapper)
        {
            Road = road;
        }

        public override bool Skip(ExportSettings sett)
        {
            return base.Skip(sett) || Road.Hidden || Road.IsSecret || !sett.ActiveDlcGuards.Contains(Road.DlcGuard);
        }

        public override bool Discretizate(ExportSettings sett)
        {
            return base.Discretizate(sett) && (Road.RoadLook.LanesRight.Count <= 1 || Road.RoadLook.LanesLeft.Count >= 1);
        }

        protected override Envelope CalculateEnvelope()
        {
            var startNode = Road.GetStartNode();
            var endNode = Road.GetEndNode();
            var (sx, sz) = Mapper.MapSettings.Correct(startNode.X, startNode.Z);
            var (ex, ez) = Mapper.MapSettings.Correct(endNode.X, endNode.Z);
            return new Envelope(sx, ex, sz, ez);
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            uint cursorX = 0, cursorY = 0;
            var startNode = Road.GetStartNode();
            var endNode = Road.GetEndNode();
            var sx = startNode.X;
            var sz = startNode.Z;
            var ex = endNode.X;
            var ez = endNode.Z;

            var tanSx = Math.Cos(-(Math.PI * 0.5f - startNode.Rotation)) * Envelope.Diameter;
            var tanEx = Math.Cos(-(Math.PI * 0.5f - endNode.Rotation)) * Envelope.Diameter;
            var tanSz = Math.Sin(-(Math.PI * 0.5f - startNode.Rotation)) * Envelope.Diameter;
            var tanEz = Math.Sin(-(Math.PI * 0.5f - endNode.Rotation)) * Envelope.Diameter;

            int hermiteSteps = Math.Max(2, (int)(8 * (Envelope.Diameter / sett.Extent)));
            var points = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };

            for (var i = 0; i < hermiteSteps; i++)
            {
                var s = i / (float)(hermiteSteps - 1);
                var x = (float)TsRoadLook.Hermite(s, sx, ex, tanSx, tanEx);
                var z = (float)TsRoadLook.Hermite(s, sz, ez, tanSz, tanEz);
                (x, z) = Mapper.MapSettings.Correct(x, z);
                if (i == 1) points.Add(GenerateCommandInteger(MapboxCommandType.LineTo, hermiteSteps - 1));
                points.AddRange(sett.GenerateDeltaFromGame(x, z, ref cursorX, ref cursorY));
            }

            layers.roads.Features.Add(new Feature { Id= Road.GetId(), Type = GeomType.Linestring, Geometry = { points } });
            return true;
        }
    }
}
