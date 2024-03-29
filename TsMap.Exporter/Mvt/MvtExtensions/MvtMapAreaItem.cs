﻿using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Drawing;
using TsMap.TsItem;
using static TsMap.Exporter.Mvt.Tile.Types;
using static TsMap.Exporter.Mvt.VectorTileUtils;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtMapAreaItem : RectMvtExtension
    {
        public readonly TsMapAreaItem MapArea;

        public MvtMapAreaItem(TsMapAreaItem mapArea, TsMapper mapper) : base(mapper)
        {
            this.MapArea = mapArea;
        }

        public override int Order()
        {
            int zIndex = MapArea.DrawOver ? 10 : 0;
            if ((MapArea.ColorIndex & 0x03) == 3)
            {
                zIndex = MapArea.DrawOver ? 13 : 3;
            }
            else if ((MapArea.ColorIndex & 0x02) == 2)
            {
                zIndex = MapArea.DrawOver ? 12 : 2;
            }
            else if ((MapArea.ColorIndex & 0x01) == 1)
            {
                zIndex = MapArea.DrawOver ? 11 : 1;
            }

            return zIndex;
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            uint cursorX = 0, cursorY = 0;
            var points = new List<PointF>();

            foreach (var mapAreaNode in MapArea.NodeUids)
            {
                var node = Mapper.GetNodeByUid(mapAreaNode);
                if (node == null) continue;
                points.Add(Mapper.MapSettings.Correct(new PointF(node.X, node.Z)));
            }

            string areaType = "road";
            if ((MapArea.ColorIndex & 0x03) == 3)
            {
                areaType = "grass";
            }
            else if ((MapArea.ColorIndex & 0x02) == 2)
            {
                areaType = "building";
            }
            else if ((MapArea.ColorIndex & 0x01) == 1)
            {
                areaType = "field";
            }

            var geometry = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };
            for (int j = 0; j < points.Count; j++)
            {
                if (j == 1) geometry.Add(GenerateCommandInteger(MapboxCommandType.LineTo, points.Count - 1));
                geometry.AddRange(sett.GenerateDeltaFromGame(points[j].X, points[j].Y, ref cursorX, ref cursorY));
            }
            geometry.Add(GenerateCommandInteger(MapboxCommandType.ClosePath, 1));

            layers.Prefabs.Features.Add(new Feature
            {
                Id = MapArea.GetId(),
                Type = GeomType.Polygon,
                Geometry = { geometry },
                Tags = { layers.Prefabs.GetOrCreateTag("area", areaType) }
            });
            return true;
        }

        protected override Envelope CalculateEnvelope()
        {
            float maxX = float.MinValue, maxY = float.MinValue, minX = float.MaxValue, minY = float.MaxValue;
            foreach (var mapAreaNode in MapArea.NodeUids)
            {
                var node = Mapper.GetNodeByUid(mapAreaNode);
                if (node == null) continue;
                var (x, y) = Mapper.MapSettings.Correct(node.X, node.Z);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
            }
            return new Envelope(minX, maxX, minY, maxY);
        }

        public override bool Skip(ExportSettings sett)
        {
            return base.Skip(sett) || MapArea.Hidden || MapArea.IsSecret || !sett.ActiveDlcGuards.Contains(MapArea.DlcGuard);
        }
    }
}
