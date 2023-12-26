using Eto.Drawing;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.Common;
using TsMap.Helpers;
using TsMap.TsItem;
using static TsMap.Exporter.Mvt.Tile.Types;
using static TsMap.Exporter.Mvt.VectorTileUtils;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtPrefabItem : RectMvtExtension
    {
        public readonly TsPrefabItem Prefab;

        public MvtPrefabItem(TsPrefabItem prefab, TsMapper mapper) : base(mapper)
        {
            this.Prefab = prefab;
        }

        public override bool Skip(ExportSettings sett)
        {
            return base.Skip(sett) || Prefab.IsSecret || !sett.ActiveDlcGuards.Contains(Prefab.DlcGuard) || Mapper.GetNodeByUid(Prefab.Nodes[0]) == null;
        }

        protected override Envelope CalculateEnvelope()
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            var originNode = Mapper.GetNodeByUid(Prefab.Nodes[0]);
            if (originNode == null) return null;
            var mapPointOrigin = Prefab.Prefab.PrefabNodes[Prefab.Origin];
            var rot = (float)(originNode.Rotation - Math.PI - Math.Atan2(mapPointOrigin.RotZ, mapPointOrigin.RotX) + Math.PI / 2);

            var prefabStartX = originNode.X - mapPointOrigin.X;
            var prefabStartZ = originNode.Z - mapPointOrigin.Z;
            foreach (var mapPoint in Prefab.Prefab.MapPoints)
            {
                var point = Mapper.MapSettings.Correct(RenderHelper.RotatePoint(prefabStartX + mapPoint.X, prefabStartZ + mapPoint.Z, rot, originNode.X, originNode.Z));
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }
            return new Envelope(minX, maxX, minY, maxY);
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            var originNode = Mapper.GetNodeByUid(Prefab.Nodes[0]);
            var mapPointOrigin = Prefab.Prefab.PrefabNodes[Prefab.Origin];
            var rot = (float)(originNode.Rotation - Math.PI - Math.Atan2(mapPointOrigin.RotZ, mapPointOrigin.RotX) + Math.PI / 2);
            var prefabStartX = originNode.X - mapPointOrigin.X;
            var prefabStartZ = originNode.Z - mapPointOrigin.Z;

            List<int> pointsDrawn = new List<int>();
            List<Feature> roads = new(), prefabs = new();

            for (var i = 0; i < Prefab.Prefab.MapPoints.Count; i++)
            {
                var mapPoint = Prefab.Prefab.MapPoints[i];
                pointsDrawn.Add(i);

                if (mapPoint.LaneCount == -1) // non-road Prefab
                {
                    uint cursorX = 0, cursorY = 0;

                    Dictionary<int, PointF> polyPoints = new Dictionary<int, PointF>();
                    var nextPoint = i;
                    do
                    {
                        if (Prefab.Prefab.MapPoints[nextPoint].Neighbours.Count == 0) break;

                        foreach (var neighbour in Prefab.Prefab.MapPoints[nextPoint].Neighbours)
                        {
                            if (!polyPoints.ContainsKey(neighbour)) // New Polygon Neighbour
                            {
                                nextPoint = neighbour;
                                var newPoint = RenderHelper.RotatePoint(
                                    prefabStartX + Prefab.Prefab.MapPoints[nextPoint].X,
                                    prefabStartZ + Prefab.Prefab.MapPoints[nextPoint].Z, rot, originNode.X,
                                    originNode.Z);

                                polyPoints.Add(nextPoint, new PointF(newPoint.X, newPoint.Y));
                                break;
                            }
                            nextPoint = -1;
                        }
                    } while (nextPoint != -1);

                    if (polyPoints.Count < 2) continue;

                    var visualFlag = Prefab.Prefab.MapPoints[polyPoints.First().Key].PrefabColorFlags;

                    var roadOver = MemoryHelper.IsBitSet(visualFlag, 0); // Road Over flag
                    var zIndex = roadOver ? 10 : 0;
                    /*if (MemoryHelper.IsBitSet(visualFlag, 1))
                    {
                        fillColor = palette.PrefabLight;
                    }
                    else if (MemoryHelper.IsBitSet(visualFlag, 2))
                    {
                        fillColor = palette.PrefabDark;
                        zIndex = roadOver ? 11 : 1;
                    }
                    else if (MemoryHelper.IsBitSet(visualFlag, 3))
                    {
                        fillColor = palette.PrefabGreen;
                        zIndex = roadOver ? 12 : 2;
                    }
                    // else fillColor = _palette.Error; // Unknown
                    */

                    var points = Mapper.MapSettings.Correct(polyPoints.Values).ToList();

                    var geometry = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (j == 1) geometry.Add(GenerateCommandInteger(MapboxCommandType.LineTo, points.Count - 1));
                        geometry.AddRange(sett.GenerateDeltaFromGame(points[j].X, points[j].Y, ref cursorX, ref cursorY));
                    }
                    geometry.Add(GenerateCommandInteger(MapboxCommandType.ClosePath, 1));

                    prefabs.Add(new Feature { Type = GeomType.Polygon, Geometry = { geometry } });

                    continue;
                }

                var mapPointLaneCount = mapPoint.LaneCount;

                foreach (var neighbourPointIndex in mapPoint.Neighbours)
                {
                    uint cursorX = 0, cursorY = 0;

                    if (pointsDrawn.Contains(neighbourPointIndex)) continue;
                    var neighbourPoint = Prefab.Prefab.MapPoints[neighbourPointIndex];

                    if ((mapPoint.Hidden || neighbourPoint.Hidden) && Prefab.Prefab.PrefabNodes.Count + 1 <
                        Prefab.Prefab.MapPoints.Count) continue;

                    var roadYaw = Math.Atan2(neighbourPoint.Z - mapPoint.Z, neighbourPoint.X - mapPoint.X);

                    var neighbourLaneCount = neighbourPoint.LaneCount;
                    var mapPointSize = (Consts.LaneWidth * mapPointLaneCount + mapPoint.LaneOffset) / 2f;
                    var neighbourMapPointSize = (Consts.LaneWidth * neighbourLaneCount + neighbourPoint.LaneOffset) / 2f;
                    int roundingSteps = Math.Max(mapPointSize, neighbourMapPointSize) / sett.Extent < sett.DiscretizationThreshold ? 2 : 4;

                    var cornerCoords = new List<PointF>();

                    cornerCoords.AddRange(RenderHelper.GetRoundedCornerCoords(prefabStartX + mapPoint.X, prefabStartZ + mapPoint.Z,
                        mapPointSize, roadYaw + Math.PI - Math.PI / 2, roadYaw + Math.PI + Math.PI / 2, roundingSteps));
                    cornerCoords.AddRange(RenderHelper.GetRoundedCornerCoords(prefabStartX + neighbourPoint.X, prefabStartZ + neighbourPoint.Z,
                        neighbourMapPointSize, roadYaw - Math.PI / 2, roadYaw + Math.PI / 2, roundingSteps));

                    cornerCoords = Mapper.MapSettings.Correct(cornerCoords.Select(p => RenderHelper.RotatePoint(p.X, p.Y, rot, originNode.X, originNode.Z))).ToList();

                    var points = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };
                    for (int j = 0; j < cornerCoords.Count; j++)
                    {
                        if (j == 1) points.Add(GenerateCommandInteger(MapboxCommandType.LineTo, cornerCoords.Count - 1));
                        points.AddRange(sett.GenerateDeltaFromGame(cornerCoords[j].X, cornerCoords[j].Y, ref cursorX, ref cursorY));
                    }
                    points.Add(GenerateCommandInteger(MapboxCommandType.ClosePath, 1));

                    roads.Add(new Feature { Type = GeomType.Polygon, Geometry = { points } });
                }
            }

            layers.roads.Features.AddRange(roads);
            layers.prefabs.Features.AddRange(prefabs);
            return roads.Count > 0 || prefabs.Count > 0;
        }
    }
}
