using Eto.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.Common;
using TsMap.Helpers;
using TsMap.Map.Overlays;

namespace TsMap
{
    public class TsMapRenderer
    {
        private readonly TsMapper _mapper;
        private const float itemDrawMargin = 1000f;

        private int[] zoomCaps = { 1000, 5000, 18500, 45000 };

        private readonly Font _defaultFont = new Font("Arial", 10.0f, FontStyle.Bold);
        private readonly SolidBrush _cityShadowColor = new SolidBrush(Color.FromArgb(210, 0, 0, 0));

        public TsMapRenderer(TsMapper mapper)
        {
            _mapper = mapper;
        }

        public void Render(Graphics g, Rectangle clip, float scale, PointF startPoint, MapPalette palette, RenderFlags renderFlags = RenderFlags.All)
        {
            var startTime = DateTime.Now.Ticks;
            g.FillRectangle(palette.Background, new Rectangle(0, 0, clip.Width, clip.Height));
            g.SaveTransform();

            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-startPoint.X, -startPoint.Y);
            g.ImageInterpolation = ImageInterpolation.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.AntiAlias = true;

            if (_mapper == null)
            {
                g.DrawText(_defaultFont, palette.Error, 5, 5, "Map object not initialized");
                return;
            }

            var dlcGuards = _mapper.GetDlcGuardsForCurrentGame();

            var activeDlcGuards = dlcGuards.Where(x => x.Enabled).Select(x => x.Index).ToList();

            var zoomIndex = RenderHelper.GetZoomIndex(clip, scale);

            var rectangle = new RectangleF(startPoint.X - itemDrawMargin, startPoint.Y - itemDrawMargin, clip.Width / scale + itemDrawMargin, clip.Height / scale + itemDrawMargin);
            var MapSettings = _mapper.MapSettings;

            var backgroundStartTime = DateTime.Now.Ticks;
            var backPos = _mapper.BackgroundPos;
            var tileWidth = (backPos.Width / 2);
            var tileHeight = (backPos.Height / 2);
            for (int i = 0; i < 4; i++)
            {
                g.DrawImage(_mapper.Backgrounds[i].GetBitmap(), (i / 2) * tileWidth + backPos.X, (i % 2) * tileHeight + backPos.Y, tileWidth + 35, tileHeight + 35);
            }
            var backgroundTime = DateTime.Now.Ticks - backgroundStartTime;


            var ferryStartTime = DateTime.Now.Ticks;
            if (renderFlags.IsActive(RenderFlags.FerryConnections))
            {
                var ferryPen = new Pen(palette.FerryLines, 50) { DashStyle = new DashStyle(0, new[] { 10f, 10f }) };

                foreach (var ferryConnection in _mapper.FerryConnections.Values)
                {
                    var connections = _mapper.LookupFerryConnection(ferryConnection.FerryPortId);

                    foreach (var conn in connections)
                    {
                        if (conn.Connections.Count == 0) // no extra nodes -> straight line
                        {
                            g.DrawLine(ferryPen, MapSettings.Correct(conn.StartPortLocation), MapSettings.Correct(conn.EndPortLocation));
                            continue;
                        }

                        var startYaw = Math.Atan2(conn.Connections[0].Z - conn.StartPortLocation.Y, // get angle of the start port to the first node
                            conn.Connections[0].X - conn.StartPortLocation.X);
                        var bezierNodes = RenderHelper.GetBezierControlNodes(conn.StartPortLocation.X,
                            conn.StartPortLocation.Y, startYaw, conn.Connections[0].X, conn.Connections[0].Z,
                            conn.Connections[0].Rotation);

                        var bezierPoints = new GraphicsPath();
                        PointF last = new PointF(conn.Connections[0].X, conn.Connections[0].Z);
                        bezierPoints.AddBezier(
                            MapSettings.Correct(new PointF(conn.StartPortLocation.X, conn.StartPortLocation.Y)),
                            MapSettings.Correct(new PointF(conn.StartPortLocation.X + bezierNodes.Item1.X, conn.StartPortLocation.Y + bezierNodes.Item1.Y)),
                            MapSettings.Correct(new PointF(conn.Connections[0].X - bezierNodes.Item2.X, conn.Connections[0].Z - bezierNodes.Item2.Y)),
                            last
                        );

                        for (var i = 0; i < conn.Connections.Count - 1; i++) // loop all extra nodes
                        {
                            var ferryPoint = conn.Connections[i];
                            var nextFerryPoint = conn.Connections[i + 1];

                            bezierNodes = RenderHelper.GetBezierControlNodes(ferryPoint.X, ferryPoint.Z, ferryPoint.Rotation,
                                nextFerryPoint.X, nextFerryPoint.Z, nextFerryPoint.Rotation);

                            bezierPoints.AddBezier(
                                last,
                                MapSettings.Correct(new PointF(ferryPoint.X + bezierNodes.Item1.X, ferryPoint.Z + bezierNodes.Item1.Y)),
                                MapSettings.Correct(new PointF(nextFerryPoint.X - bezierNodes.Item2.X, nextFerryPoint.Z - bezierNodes.Item2.Y)),
                                last = MapSettings.Correct(new PointF(nextFerryPoint.X, nextFerryPoint.Z))
                            );
                        }

                        var lastFerryPoint = conn.Connections[conn.Connections.Count - 1];
                        var endYaw = Math.Atan2(conn.EndPortLocation.Y - lastFerryPoint.Z, // get angle of the last node to the end port
                            conn.EndPortLocation.X - lastFerryPoint.X);

                        bezierNodes = RenderHelper.GetBezierControlNodes(lastFerryPoint.X,
                            lastFerryPoint.Z, lastFerryPoint.Rotation, conn.EndPortLocation.X, conn.EndPortLocation.Y,
                            endYaw);

                        bezierPoints.AddBezier(
                            last,
                                MapSettings.Correct(new PointF(lastFerryPoint.X + bezierNodes.Item1.X, lastFerryPoint.Z + bezierNodes.Item1.Y)),
                                MapSettings.Correct(new PointF(conn.EndPortLocation.X - bezierNodes.Item2.X, conn.EndPortLocation.Y - bezierNodes.Item2.Y)),
                                MapSettings.Correct(new PointF(conn.EndPortLocation.X, conn.EndPortLocation.Y))
                            );

                        g.DrawPath(ferryPen, bezierPoints);
                    }
                }
                ferryPen.Dispose();
            }
            var ferryTime = DateTime.Now.Ticks - ferryStartTime;

            var mapAreaStartTime = DateTime.Now.Ticks;
            if (renderFlags.IsActive(RenderFlags.MapAreas))
            {
                var drawingQueue = new List<TsPrefabPolyLook>();
                foreach (var mapArea in _mapper.MapAreas.Values)
                {
                    if (!activeDlcGuards.Contains(mapArea.DlcGuard) ||
                        mapArea.IsSecret && !renderFlags.IsActive(RenderFlags.SecretRoads) ||
                        !rectangle.Contains(MapSettings.Correct(RenderHelper.GetPoint(mapArea.X, mapArea.Z)))
                    )
                    {
                        continue;
                    }

                    var points = new List<PointF>();

                    foreach (var mapAreaNode in mapArea.NodeUids)
                    {
                        var node = _mapper.GetNodeByUid(mapAreaNode);
                        if (node == null) continue;
                        points.Add(new PointF(node.X, node.Z));
                    }

                    Brush fillColor = palette.PrefabRoad;
                    var zIndex = mapArea.DrawOver ? 10 : 0;
                    if ((mapArea.ColorIndex & 0x03) == 3)
                    {
                        fillColor = palette.PrefabGreen;
                        zIndex = mapArea.DrawOver ? 13 : 3;
                    }
                    else if ((mapArea.ColorIndex & 0x02) == 2)
                    {
                        fillColor = palette.PrefabDark;
                        zIndex = mapArea.DrawOver ? 12 : 2;
                    }
                    else if ((mapArea.ColorIndex & 0x01) == 1)
                    {
                        fillColor = palette.PrefabLight;
                        zIndex = mapArea.DrawOver ? 11 : 1;
                    }

                    points = MapSettings.Correct(points).ToList();
                    drawingQueue.Add(new TsPrefabPolyLook(points)
                    {
                        Color = fillColor,
                        ZIndex = zIndex
                    });
                }

                foreach (var mapArea in drawingQueue.OrderBy(p => p.ZIndex))
                {
                    mapArea.Draw(g);
                }
            }
            var mapAreaTime = DateTime.Now.Ticks - mapAreaStartTime;

            var prefabStartTime = DateTime.Now.Ticks;
            if (renderFlags.IsActive(RenderFlags.Prefabs))
            {
                List<TsPrefabLook> drawingQueue = new List<TsPrefabLook>();

                foreach (var prefabItem in _mapper.Prefabs.Values)
                {
                    if (!activeDlcGuards.Contains(prefabItem.DlcGuard) ||
                        prefabItem.IsSecret && !renderFlags.IsActive(RenderFlags.SecretRoads) ||
                         !rectangle.Contains(MapSettings.Correct(RenderHelper.GetPoint(prefabItem)))
                    )
                    {
                        continue;
                    }

                    var originNode = _mapper.GetNodeByUid(prefabItem.Nodes[0]);
                    if (prefabItem.Prefab.PrefabNodes == null) continue;

                    if (true)
                    {
                        var mapPointOrigin = prefabItem.Prefab.PrefabNodes[prefabItem.Origin];

                        var rot = (float)(originNode.Rotation - Math.PI -
                                           Math.Atan2(mapPointOrigin.RotZ, mapPointOrigin.RotX) + Math.PI / 2);

                        var prefabstartX = originNode.X - mapPointOrigin.X;
                        var prefabStartZ = originNode.Z - mapPointOrigin.Z;

                        List<int> pointsDrawn = new List<int>();

                        for (var i = 0; i < prefabItem.Prefab.MapPoints.Count; i++)
                        {
                            var mapPoint = prefabItem.Prefab.MapPoints[i];
                            pointsDrawn.Add(i);

                            if (mapPoint.LaneCount == -1) // non-road Prefab
                            {
                                Dictionary<int, PointF> polyPoints = new Dictionary<int, PointF>();
                                var nextPoint = i;
                                do
                                {
                                    if (prefabItem.Prefab.MapPoints[nextPoint].Neighbours.Count == 0) break;

                                    foreach (var neighbour in prefabItem.Prefab.MapPoints[nextPoint].Neighbours)
                                    {
                                        if (!polyPoints.ContainsKey(neighbour)) // New Polygon Neighbour
                                        {
                                            nextPoint = neighbour;
                                            var newPoint = RenderHelper.RotatePoint(
                                                prefabstartX + prefabItem.Prefab.MapPoints[nextPoint].X,
                                                prefabStartZ + prefabItem.Prefab.MapPoints[nextPoint].Z, rot, originNode.X,
                                                originNode.Z);

                                            polyPoints.Add(nextPoint, new PointF(newPoint.X, newPoint.Y));
                                            break;
                                        }
                                        nextPoint = -1;
                                    }
                                } while (nextPoint != -1);

                                if (polyPoints.Count < 2) continue;

                                var visualFlag = prefabItem.Prefab.MapPoints[polyPoints.First().Key].PrefabColorFlags;

                                Brush fillColor = palette.PrefabLight;
                                var roadOver = MemoryHelper.IsBitSet(visualFlag, 0); // Road Over flag
                                var zIndex = roadOver ? 10 : 0;
                                if (MemoryHelper.IsBitSet(visualFlag, 1))
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

                                var points = MapSettings.Correct(polyPoints.Values).ToList();
                                var prefabLook = new TsPrefabPolyLook(points)
                                {
                                    ZIndex = zIndex,
                                    Color = fillColor
                                };

                                prefabItem.AddLook(prefabLook);
                                continue;
                            }

                            var mapPointLaneCount = mapPoint.LaneCount;

                            foreach (var neighbourPointIndex in mapPoint.Neighbours) // TODO: Fix connection between road segments
                            {
                                if (pointsDrawn.Contains(neighbourPointIndex)) continue;
                                var neighbourPoint = prefabItem.Prefab.MapPoints[neighbourPointIndex];

                                if ((mapPoint.Hidden || neighbourPoint.Hidden) && prefabItem.Prefab.PrefabNodes.Count + 1 <
                                    prefabItem.Prefab.MapPoints.Count) continue;

                                var roadYaw = Math.Atan2(neighbourPoint.Z - mapPoint.Z, neighbourPoint.X - mapPoint.X);

                                var neighbourLaneCount = neighbourPoint.LaneCount;

                                var cornerCoords = new List<PointF>();

                                cornerCoords.AddRange(RenderHelper.GetRoundedCornerCoords(prefabstartX + mapPoint.X, prefabStartZ + mapPoint.Z,
                                    (Consts.LaneWidth * mapPointLaneCount + mapPoint.LaneOffset) / 2f, roadYaw + Math.PI - Math.PI / 2, roadYaw + Math.PI + Math.PI/2, 4));
                                cornerCoords.AddRange(RenderHelper.GetRoundedCornerCoords(prefabstartX + neighbourPoint.X, prefabStartZ + neighbourPoint.Z,
                                    (Consts.LaneWidth * neighbourLaneCount + neighbourPoint.LaneOffset) / 2f, roadYaw  -  Math.PI / 2 , roadYaw  + Math.PI / 2, 4));

                                cornerCoords = MapSettings.Correct(cornerCoords.Select(p => RenderHelper.RotatePoint(p.X, p.Y, rot, originNode.X, originNode.Z))).ToList();
                                TsPrefabLook prefabLook = new TsPrefabPolyLook(cornerCoords)
                                {
                                    Color = palette.PrefabRoad,
                                    ZIndex = MemoryHelper.IsBitSet(mapPoint.PrefabColorFlags, 0) ? 13 : 3,
                                }; 
                                
                                prefabItem.AddLook(prefabLook);
                            }
                        }
                    }

                    prefabItem.GetLooks().ForEach(x => drawingQueue.Add(x));
                }

                foreach (var prefabLook in drawingQueue.OrderBy(p => p.ZIndex))
                {
                    prefabLook.Draw(g);
                }
            }
            var prefabTime = DateTime.Now.Ticks - prefabStartTime;

            var roadStartTime = DateTime.Now.Ticks;
            if (renderFlags.IsActive(RenderFlags.Roads))
            {
                foreach (var road in _mapper.Roads.Values)
                {
                    if (!activeDlcGuards.Contains(road.DlcGuard) ||
                        road.IsSecret && !renderFlags.IsActive(RenderFlags.SecretRoads) ||
                         !rectangle.Contains(MapSettings.Correct(RenderHelper.GetPoint(road)))
                    )
                    {
                        continue;
                    }

                    var startNode = road.GetStartNode();
                    var endNode = road.GetEndNode();

                    if (!road.HasPoints())
                    {
                        var newPoints = new List<PointF>();

                        var sx = startNode.X;
                        var sz = startNode.Z;
                        var ex = endNode.X;
                        var ez = endNode.Z;

                        var radius = Math.Sqrt(Math.Pow(sx - ex, 2) + Math.Pow(sz - ez, 2));

                        var tanSx = Math.Cos(-(Math.PI * 0.5f - startNode.Rotation)) * radius;
                        var tanEx = Math.Cos(-(Math.PI * 0.5f - endNode.Rotation)) * radius;
                        var tanSz = Math.Sin(-(Math.PI * 0.5f - startNode.Rotation)) * radius;
                        var tanEz = Math.Sin(-(Math.PI * 0.5f - endNode.Rotation)) * radius;

                        for (var i = 0; i < 8; i++)
                        {
                            var s = i / (float)(8 - 1);
                            var x = (float)TsRoadLook.Hermite(s, sx, ex, tanSx, tanEx);
                            var z = (float)TsRoadLook.Hermite(s, sz, ez, tanSz, tanEz);
                            newPoints.Add(new PointF(x, z));
                        }
                        newPoints = MapSettings.Correct(newPoints).ToList();
                        road.AddPoints(newPoints);
                    }

                    var roadWidth = road.RoadLook.GetWidth();
                    Pen roadPen;
                    if (road.IsSecret)
                    {
                        if (zoomIndex < 3)
                        {
                            roadPen = new Pen(palette.Road, roadWidth) { DashStyle = new DashStyle(0, new[] { 1f, 1f }) };
                        }
                        else // zoomed out with DashPattern causes OutOfMemory Exception
                        {
                            roadPen = new Pen(palette.Road, roadWidth);
                        }
                    }
                    else
                    {
                        roadPen = new Pen(palette.Road, roadWidth);
                    }

                    roadPen.LineJoin = PenLineJoin.Round;
                    roadPen.LineCap = PenLineCap.Round;

                    var curvePoints = new GraphicsPath();
                    curvePoints.AddCurve(road.GetPoints()?.ToArray());
                    g.DrawPath(roadPen, curvePoints);
                    roadPen.Dispose();
                }
            }
            var roadTime = DateTime.Now.Ticks - roadStartTime;

            var mapOverlayStartTime = DateTime.Now.Ticks;
            if (renderFlags.IsActive(RenderFlags.MapOverlays))
            {
                foreach (var mapOverlay in _mapper.OverlayManager.GetOverlays())
                {
                    if (!activeDlcGuards.Contains(mapOverlay.DlcGuard) ||
                        mapOverlay.IsSecret && !renderFlags.IsActive(RenderFlags.SecretRoads) ||
                        !rectangle.Contains(MapSettings.Correct(RenderHelper.GetPoint(mapOverlay.Position.X, mapOverlay.Position.Y)))
                    )
                    {
                        continue;
                    }

                    var b = mapOverlay.GetBitmap();

                    if (b == null || !renderFlags.IsActive(RenderFlags.BusStopOverlay) && mapOverlay.OverlayType == OverlayType.BusStop) continue;

                    var pos = MapSettings.Correct(mapOverlay.Position);
                    if (mapOverlay.OverlayType == OverlayType.Flag)
                    {
                        var width = b.Width / scale;
                        var height = b.Height / scale;
                        g.DrawImage(b, pos.X - (width / 2f), pos.Y - (height / 2f), width, height);
                    }
                    else
                    {
                        g.DrawImage(b, pos.X - (b.Width / 2f), pos.Y - (b.Height / 2f), b.Width, b.Height);
                    }

                }
            }
            var mapOverlayTime = DateTime.Now.Ticks - mapOverlayStartTime;

            var cityStartTime = DateTime.Now.Ticks;
            if (renderFlags.IsActive(RenderFlags.CityNames)) // TODO: Fix position and scaling
            {
                var cityFont = new Font("Arial", 100 + zoomCaps[zoomIndex] / 100, FontStyle.Bold);

                foreach (var city in _mapper.Cities.Values)
                {
                    var name = _mapper.Localization.GetLocaleValue(city.City.LocalizationToken) ?? city.City.Name;
                    var node = _mapper.GetNodeByUid(city.NodeUid);
                    var coords = (node == null) ? new PointF(city.X, city.Z) : new PointF(node.X, node.Z);
                    if (city.City.XOffsets.Count > zoomIndex && city.City.YOffsets.Count > zoomIndex)
                    {
                        coords.X += city.City.XOffsets[zoomIndex] / (scale * zoomCaps[zoomIndex]);
                        coords.Y += city.City.YOffsets[zoomIndex] / (scale * zoomCaps[zoomIndex]);
                    }

                    var textSize = g.MeasureString(cityFont, name);
                    coords = MapSettings.Correct(coords);
                    g.DrawText(cityFont, _cityShadowColor, coords.X + 2, coords.Y + 2, name);
                    g.DrawText(cityFont, palette.CityName, coords.X, coords.Y, name);
                }
                cityFont.Dispose();
            }
            var cityTime = DateTime.Now.Ticks - cityStartTime;

            g.RestoreTransform();
            var elapsedTime = DateTime.Now.Ticks - startTime;
            if (renderFlags.IsActive(RenderFlags.TextOverlay))
            {
                g.DrawText(_defaultFont, Brushes.WhiteSmoke, 5, 5, $"DrawTime: {elapsedTime / TimeSpan.TicksPerMillisecond} ms, x: {startPoint.X}, y: {startPoint.Y}, scale: {scale}");

                //g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 5, 20, 150, 150);
                //g.DrawString(_defaultFont, Brushes.White, 10, 40, $"Road: {roadTime / TimeSpan.TicksPerMillisecond}ms");
                //g.DrawString(_defaultFont, Brushes.White, 10, 55, $"Prefab: {prefabTime / TimeSpan.TicksPerMillisecond}ms");
                //g.DrawString(_defaultFont, Brushes.White, 10, 70, $"Ferry: {ferryTime / TimeSpan.TicksPerMillisecond}ms");
                //g.DrawString(_defaultFont, Brushes.White, 10, 85, $"MapOverlay: {mapOverlayTime / TimeSpan.TicksPerMillisecond}ms");
                //g.DrawString(_defaultFont, Brushes.White, 10, 115, $"MapArea: {mapAreaTime / TimeSpan.TicksPerMillisecond}ms");
                //g.DrawString(_defaultFont, Brushes.White, 10, 130, $"City: {cityTime / TimeSpan.TicksPerMillisecond}ms");
            }

        }
    }
}
