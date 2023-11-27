using Eto.Drawing;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using TsMap.TsItem;

namespace TsMap.Exporter
{
    public class NavExporter
    {

            private OsmStreamTarget Target;
            private TsMapper Mapper;

            private Dictionary<ulong, uint> NodeMapping = new Dictionary<ulong, uint>();
            private uint LastNodeId = 1;
            private uint LastWayId = 1;

            public const string Username = "junctio";
            public const int UserId = 1;
            public const int Version = 1;

            public float Scale;
            public List<Node> Nodes = new List<Node>();
            public List<Way> Ways = new List<Way>();


            public NavExporter(Stream target, TsMapper mapper)
            {
                this.Target = new XmlOsmStreamTarget(target);
                this.Target.Initialize();
                this.Mapper = mapper;

                this.Scale = Math.Max(Math.Max(Math.Abs(Mapper.maxX), Math.Abs(Mapper.minX)), Math.Max(Math.Abs(Mapper.maxZ), Math.Abs(Mapper.minZ)));

                foreach (var node in mapper.Nodes.Values)
                {
                    AddOsmNode(x: node.X, z: node.Z, id: node.Uid);
                }
            }

            public void TranslateItemToOsm(TsItem.TsItem item)
            {
                switch (item)
                {
                    case TsPrefabItem prefab:
                        TranslatePrefabs(prefab);
                        break;
                    case TsRoadItem road:
                        TranslateRoad(road);
                        break;
                    case TsFerryItem ferry:
                        TranslateFerry(ferry);
                        break;
                }
            }

            public void Flush()
            {
                Nodes.ForEach((x) => Target.AddNode(x));
                Ways.ForEach((x) => Target.AddWay(x));
                Nodes.Clear();
                Ways.Clear();
                this.Target.Flush();
            }

            public void Close()
            {
                this.Target.Close();
            }

            private ulong AddOsmNode(float x, float z, Dictionary<string, string> tags = null, ulong id = 0)
            {
                NodeMapping[id] = LastNodeId++;
                id = LastNodeId - 1;

                if (tags == null) tags = new Dictionary<string, string>();
                Nodes.Add(new Node()
                {
                    Id = (long)id,
                    ChangeSetId = 1,
                    Longitude = (x * 90f) / this.Scale,
                    Latitude = (z * -90f) / this.Scale,
                    TimeStamp = DateTime.Now,
                    Tags = new TagsCollection(tags),
                    UserId = UserId,
                    UserName = Username,
                    Version = Version,
                    Visible = true
                });
                return id;
            }

            private ulong AddOsmNode(PointF point, Dictionary<string, string> tags = null, ulong id = 0)
            {
                return AddOsmNode(point.X, point.Y, tags, id);
            }

            private void AddOsmWay(ulong[] nodes, Dictionary<string, string> tags = null, bool visible = true)
            {
                if (tags == null) tags = new Dictionary<string, string>();
                var realNodes = new List<ulong>(nodes).ConvertAll<long>(x => NodeMapping.ContainsKey(x) ? (long)NodeMapping[x] : (long)x);
                Ways.Add(new Way()
                {
                    Id = LastWayId++,
                    ChangeSetId = 1,
                    Nodes = realNodes.ToArray(),
                    TimeStamp = DateTime.Now,
                    Tags = new TagsCollection(tags),
                    UserId = UserId,
                    UserName = Username,
                    Version = Version,
                    Visible = visible
                });
            }

            private void TranslateRoad(TsRoadItem road)
            {
                if (road.GetStartNode() == null || road.GetEndNode() == null) return;
                Dictionary<string, string> props = new Dictionary<string, string> {
                { "highway", "tertiary" },
                { "lanes", (road.RoadLook.LanesLeft.Count + road.RoadLook.LanesRight.Count).ToString() }
            };

                /*
                TsCountry country = road.GetStartNode().GetCountry() ?? road.GetEndNode().GetCountry();
                var roadClass = road.RoadLook.GetRoadType() != null ? Mapper.SpeedClasses[road.RoadLook.GetRoadType()] : "";
                if (country != null && country.Speeds[TsVehicleType.Car].ContainsKey(roadClass))
                {
                    var speedType = Mapper.GetCity(road) != null && country.Speeds[TsVehicleType.Car][roadClass].ContainsKey(TsSpeedType.UrbanLimit) ? TsSpeedType.UrbanLimit : TsSpeedType.Limit;
                    props["maxspeed"] = country.Speeds[TsVehicleType.Car][roadClass][speedType].ToString();
                    props["maxspeed"] = Mapper.IsEts2 ? props["maxspeed"] : props["maxspeed"] + " mph";
                }
                */

                /*switch (roadClass)
                {
                    case "local_road":
                        props["highway"] = "tertiary";
                        break;
                    case "expressway":
                        props["highway"] = "primary";
                        break;
                    case "divided_road":
                        props["highway"] = "primary";
                        break;
                    case "freeway":
                        props["highway"] = "motorway";
                        break;
                    case "motorway":
                        props["highway"] = "motorway";
                        break;
                }*/

                if (road.RoadLook.IsOneWay())
                {
                    props["oneway"] = "yes";
                }
                else
                {
                    props["driving_side"] = road.LeftDriving ? "left" : "right";
                    if (road.LeftDriving)
                    {
                        props["lanes:forward"] = road.RoadLook.LanesLeft.Count.ToString();
                        props["lanes:backward"] = road.RoadLook.LanesRight.Count.ToString();
                    }
                    else
                    {
                        props["lanes:forward"] = road.RoadLook.LanesRight.Count.ToString();
                        props["lanes:backward"] = road.RoadLook.LanesLeft.Count.ToString();
                    }
                }


                var startNode = road.GetStartNode();
                var endNode = road.GetEndNode();

                var OsmNodes = new List<ulong> { startNode.Uid };

                if (!road.HasPoints())
                {
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
                        OsmNodes.Add(AddOsmNode(x, z));
                    }
                }
                OsmNodes.Add(endNode.Uid);

                AddOsmWay(OsmNodes.ToArray(), props);
            }

            private void TranslatePrefabs(TsPrefabItem prefab)
            {
                var roadProps = new Dictionary<string, string> {
                { "oneway", "yes" },
                { "highway", "tertiary" },
            };

                var originNode = Mapper.GetNodeByUid(prefab.Nodes[0]);
                if (originNode == null) return;
                var mapPointOrigin = prefab.Prefab.PrefabNodes[prefab.Origin];

                var prefabStartX = originNode.X - mapPointOrigin.X;
                var prefabStartZ = originNode.Z - mapPointOrigin.Z;

                var rot = (float)(originNode.Rotation - Math.PI - Math.Atan2(mapPointOrigin.RotZ, mapPointOrigin.RotX) + Math.PI / 2);

                var roadType = "local_road";
                foreach (var nodeUid in prefab.Nodes)
                {
                    var road = prefab.GetNextRoad(nodeUid);
                    //if (road == null || road.RoadLook.GetRoadType() == null || !Mapper.SpeedClasses.ContainsKey(road.RoadLook.GetRoadType())) continue;
                    //roadType = Mapper.SpeedClasses[road.RoadLook.GetRoadType()];
                }

                /*switch (roadType)
                {
                    case "local_road":
                        roadProps["highway"] = "tertiary_link";
                        break;
                    case "expressway":
                        roadProps["highway"] = "primary_link";
                        break;
                    case "divided_road":
                        roadProps["highway"] = "primary_link";
                        break;
                    case "freeway":
                        roadProps["highway"] = "motorway_link";
                        break;
                    case "motorway":
                        roadProps["highway"] = "motorway_link";
                        break;
                }*/

                Dictionary<int, ulong> nodes = new Dictionary<int, ulong>();

                var listNodes = new List<ulong>(prefab.Nodes);

                for (var i = 0; i < prefab.Origin; i++)
                {
                    var el = listNodes[listNodes.Count - 1];
                    listNodes.RemoveAt(listNodes.Count - 1);
                    listNodes.Insert(0, el);
                }

                foreach (var (navN, index) in prefab.Prefab.NavNodes.Select((value, i) => ( value, i )))
                {
                    if (navN.Type == TsNavNodeType.BorderNode)
                    {
                        nodes[index] = listNodes[navN.ReferenceIndex];
                    }
                    else if (navN.Connections.Count > 0)
                    {
                        var curve = navN.Connections[0].CurvePath[0];
                        var pos =                    RenderHelper.RotatePoint(curve.StartX + prefabStartX, curve.StartZ + prefabStartZ, rot, originNode.X, originNode.Z);

                        var navNuid = AddOsmNode(pos.X, pos.Y);
                        nodes[index] = navNuid;
                    }
                    else
                    {
                        nodes[index] = 0; // TODO ?
                        Console.WriteLine(prefab.Prefab.FilePath + " has dead end AI node!");
                    }
                }

                foreach (var (navN, index) in prefab.Prefab.NavNodes.Select((value, i) => (value, i)))
                {
                    foreach (var connection in navN.Connections)
                    {
                        var curveNodes = new List<ulong> { nodes[index], nodes[connection.TargetNodeIndex] };

                        for (var i = 0; i < connection.CurvePath.Count; i++)
                        {
                            var curve = connection.CurvePath[i];
                            if (i != 0)
                            {
                                var pos = RenderHelper.RotatePoint(curve.StartX + prefabStartX, curve.StartZ + prefabStartZ, rot, originNode.X, originNode.Z);
                                curveNodes.Insert(curveNodes.Count - 1, AddOsmNode(pos.X, pos.Y));

                            }
                            if (i != connection.CurvePath.Count - 1)
                            {
                                var pos = RenderHelper.RotatePoint(curve.EndX + prefabStartX, curve.EndZ + prefabStartZ, rot, originNode.X, originNode.Z);
                                curveNodes.Insert(curveNodes.Count - 1, AddOsmNode(pos.X, pos.Y));
                            }

                        }

                        AddOsmWay(nodes: curveNodes.ToArray(), roadProps);
                    }
                }

            }

            private void TranslateFerry(TsFerryItem ferryItem)
            {
                var connections = Mapper.LookupFerryConnection(ferryItem.FerryPortId);

                foreach (var conn in connections)
                {
                    ulong startUID = 0;
                    ulong endUID = 0;
                
                    var pr = Mapper.Prefabs[Mapper.FerryPorts[conn.StartPortToken].PrefabUid];
                    foreach (var nodeUid in pr.Nodes)
                    {
                        var road = pr.GetNextRoad(nodeUid);
                        if (road != null) startUID = road.GetStartNode().Uid;
                    }
                    if (!Mapper.FerryPorts.ContainsKey(conn.EndPortToken)) continue;
                    pr = Mapper.Prefabs[Mapper.FerryPorts[conn.EndPortToken].PrefabUid];
                    foreach (var nodeUid in pr.Nodes)
                    {
                        var road = pr.GetNextRoad(nodeUid);
                        if (road != null) endUID = road.GetStartNode().Uid;
                    }
                    if (startUID == 0 || endUID == 0) { Console.WriteLine("ferry with no road"); continue; }

                    var OSMNodes = new List<ulong> { startUID, endUID };

                    if (conn.Connections.Count == 0) // no extra nodes -> straight line
                    {
                        OSMNodes.Insert(OSMNodes.Count - 1, AddOsmNode(conn.StartPortLocation));
                        OSMNodes.Insert(OSMNodes.Count - 1, AddOsmNode(conn.EndPortLocation));
                        AddOsmWay(OSMNodes.ToArray(), new Dictionary<string, string> {
                        { "route", "ferry" },
                        { "motorcar", "yes" }
                    });
                        continue;
                    }

                    var startYaw = Math.Atan2(conn.Connections[0].Z - conn.StartPortLocation.Y, // get angle of the start port to the first node
                        conn.Connections[0].X - conn.StartPortLocation.X);
                    var bezierNodes = RenderHelper.GetBezierControlNodes(conn.StartPortLocation.X,
                        conn.StartPortLocation.Y, startYaw, conn.Connections[0].X, conn.Connections[0].Z,
                        conn.Connections[0].Rotation);

                    var osmPoints = new List<ulong>
                {
                    AddOsmNode(conn.StartPortLocation.X, conn.StartPortLocation.Y), // start
                    AddOsmNode(conn.StartPortLocation.X + bezierNodes.Item1.X, conn.StartPortLocation.Y + bezierNodes.Item1.Y), // control1
                    AddOsmNode(conn.Connections[0].X - bezierNodes.Item2.X, conn.Connections[0].Z - bezierNodes.Item2.Y), // control2
                    AddOsmNode(conn.Connections[0].X, conn.Connections[0].Z)
                };

                    for (var i = 0; i < conn.Connections.Count - 1; i++) // loop all extra nodes
                    {
                        var ferryPoint = conn.Connections[i];
                        var nextFerryPoint = conn.Connections[i + 1];

                        bezierNodes = RenderHelper.GetBezierControlNodes(ferryPoint.X, ferryPoint.Z, ferryPoint.Rotation,
                            nextFerryPoint.X, nextFerryPoint.Z, nextFerryPoint.Rotation);

                        osmPoints.Add(AddOsmNode(ferryPoint.X + bezierNodes.Item1.X, ferryPoint.Z + bezierNodes.Item1.Y)); // control1
                        osmPoints.Add(AddOsmNode(nextFerryPoint.X - bezierNodes.Item2.X, nextFerryPoint.Z - bezierNodes.Item2.Y)); // control2
                        osmPoints.Add(AddOsmNode(nextFerryPoint.X, nextFerryPoint.Z)); // end
                    }

                    var lastFerryPoint = conn.Connections[conn.Connections.Count - 1];
                    var endYaw = Math.Atan2(conn.EndPortLocation.Y - lastFerryPoint.Z, // get angle of the last node to the end port
                        conn.EndPortLocation.X - lastFerryPoint.X);

                    bezierNodes = RenderHelper.GetBezierControlNodes(lastFerryPoint.X,
                        lastFerryPoint.Z, lastFerryPoint.Rotation, conn.EndPortLocation.X, conn.EndPortLocation.Y,
                        endYaw);

                    osmPoints.Add(AddOsmNode(lastFerryPoint.X + bezierNodes.Item1.X, lastFerryPoint.Z + bezierNodes.Item1.Y)); // control1
                    osmPoints.Add(AddOsmNode(conn.EndPortLocation.X - bezierNodes.Item2.X, conn.EndPortLocation.Y - bezierNodes.Item2.Y)); // control2
                    osmPoints.Add(AddOsmNode(conn.EndPortLocation.X, conn.EndPortLocation.Y)); // end

                    OSMNodes.AddRange(osmPoints);
                    OSMNodes.Add(OSMNodes[1]);
                    OSMNodes.RemoveAt(1);
                    AddOsmWay(OSMNodes.ToArray(), new Dictionary<string, string> {
                    { "route", "ferry" },
                    { "motorcar", "yes" }
                });
                }
            }

        }
    }

