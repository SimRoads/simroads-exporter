using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.TsItem;

namespace TsMap.Exporter.Routing
{
    public class RoutingLink
    {
        public readonly RoutingNode StartNode;
        public readonly RoutingNode EndNode;
        public readonly List<ulong> ElementsIds = new();

        public readonly ushort Length;
        public readonly byte RoadSize;

        public RoutingLink(TsRoadItem road, RoutingNode startNode, RoutingNode endNode)
        {
            StartNode = startNode;
            EndNode = endNode;

            Length = (ushort)(Math.Sqrt(Math.Pow(startNode.Node.X - endNode.Node.X, 2) +
                                        Math.Pow(startNode.Node.Z - endNode.Node.Z, 2)) * 10);
            RoadSize = (byte)(road.LeftDriving ? road.RoadLook.LanesLeft.Count : road.RoadLook.LanesRight.Count);
            ElementsIds.Add(road.GetId());
        }

        public RoutingLink(RoutingNode startNode, RoutingNode endNode, List<ulong> elementsIds, ushort length,
            byte roadSize)
        {
            StartNode = startNode;
            EndNode = endNode;
            ElementsIds = elementsIds;
            Length = length;
            RoadSize = roadSize;
        }

        public object[] Serialize()
        {
            return new object[] { StartNode.Id, EndNode.Id, ElementsIds, Length, RoadSize };
        }
    }

    public class RoutingNode
    {
        public readonly uint Id;
        public readonly TsNode Node;

        private List<RoutingLink> _links = new();
        private static uint _nextId;

        private RoutingNode(TsNode node)
        {
            Node = node;
            Id = _nextId++;
        }

        public object[] Serialize()
        {
            return new object[] { Id, Node.X, Node.Z };
        }

        public static Dictionary<ulong, RoutingNode> GetNetwork(TsMapper mapper)
        {
            var nodes = mapper.Nodes.Where(x =>
                x.Value.BackwardItem is TsRoadItem ||
                x.Value.BackwardItem is TsPrefabItem ||
                x.Value.ForwardItem is TsRoadItem ||
                x.Value.ForwardItem is TsPrefabItem).ToDictionary(x => x.Key, x => new RoutingNode(x.Value));

            foreach (var (_, rNode) in nodes)
            {
                var node = rNode.Node;
                TsItem.TsItem[] items = [node.BackwardItem, node.ForwardItem];

                foreach (var item in items)
                {
                    if (item is TsRoadItem road)
                    {
                        var (start, end) = (road.GetStartNode().Uid, road.GetEndNode().Uid);
                        if (start == node.Uid &&
                            ((road.RoadLook.IsOneWay() && !road.LeftDriving) || !road.RoadLook.IsOneWay()))
                            nodes[end]._links.Add(new RoutingLink(road, nodes[end], rNode));
                        else if (end == node.Uid &&
                                 ((road.RoadLook.IsOneWay() && road.LeftDriving) || !road.RoadLook.IsOneWay()))
                            nodes[start]._links.Add(new RoutingLink(road, nodes[start], rNode));
                    }
                }
            }

            var linksToAdd = new Dictionary<ulong, List<RoutingLink>>();
            foreach (var (_, rNode) in nodes)
            {
                var node = rNode.Node;
                TsItem.TsItem[] items = [node.BackwardItem, node.ForwardItem];

                foreach (var item in items)
                {
                    if (item is TsPrefabItem prefab)
                    {
                        var pNodes = new List<ulong>(prefab.Nodes);
                        for (var i = 0; i < prefab.Origin; i++)
                        {
                            var el = pNodes.Last();
                            pNodes.RemoveAt(pNodes.Count - 1);
                            pNodes.Insert(0, el);
                        }

                        var startNodeIndex = pNodes.IndexOf(rNode.Node.Uid);
                        var endIndexes = new List<ushort>();
                        Stack<List<int>> paths = [];
                        paths.Push([
                            prefab.Prefab.NavNodes.FindIndex(x =>
                                x.ReferenceIndex == startNodeIndex && x.Type == TsNavNodeType.BorderNode)
                        ]);
                        while (paths.Count > 0)
                        {
                            var path = paths.Pop();
                            var last = prefab.Prefab.NavNodes[path.Last()];
                            if (path.Count > 1 && last.Type == TsNavNodeType.BorderNode)
                            {
                                endIndexes.Add(last.ReferenceIndex);
                            }
                            else
                            {
                                foreach (var conn in last.Connections)
                                {
                                    if (!path.Contains(conn.TargetNodeIndex))
                                    {
                                        paths.Push([..path, conn.TargetNodeIndex]);
                                    }
                                }
                            }
                        }

                        var startMapPoint =
                            prefab.Prefab.MapPoints.FindIndex(x => x.ControlNodeIndex == startNodeIndex);
                        if (startMapPoint == -1)
                        {
                            foreach (var endIndex in endIndexes)
                            {
                                nodes[pNodes[endIndex]]._links.Add(new RoutingLink(nodes[pNodes[endIndex]], rNode,
                                    [], 0, 1));
                            }

                            continue;
                        }

                        foreach (var endIndex in endIndexes)
                        {
                            Queue<List<int>> points = new();
                            points.Enqueue([startMapPoint]);
                            while (points.Count > 0)
                            {
                                var path = points.Dequeue();
                                var last = prefab.Prefab.MapPoints[path.Last()];
                                if (path.Count > 1 && last.ControlNodeIndex != -1)
                                {
                                    if (last.ControlNodeIndex != endIndex) continue;
                                    ushort length = 0;
                                    var roadSize = (byte)(prefab.Prefab.MapPoints[path[0]].LaneCount);
                                    var elementsIds = new List<ulong> { prefab.GetId(path[0]) };
                                    for (int i = 1; i < path.Count; i++)
                                    {
                                        var (prev, curr) = (prefab.Prefab.MapPoints[path[i - 1]],
                                            prefab.Prefab.MapPoints[path[i]]);
                                        length += (ushort)(Math.Sqrt(Math.Pow(prev.X - curr.X, 2) +
                                                                     Math.Pow(prev.Z - curr.Z, 2)) * 10);
                                        roadSize = (byte)Math.Min(roadSize, curr.LaneCount);
                                        elementsIds.Add(prefab.GetId(path[i]));
                                    }

                                    //if (!linksToAdd.ContainsKey(rNode.Node.Uid)) linksToAdd[pNodes[endIndex]] = new();
                                    nodes[pNodes[endIndex]]._links.Add(new RoutingLink(nodes[pNodes[endIndex]], rNode,
                                        elementsIds,
                                        length, roadSize));
                                }
                                else
                                {
                                    var neighbours = last.Neighbours.Except(path).ToArray();
                                    foreach (var conn in neighbours)
                                    {
                                        var mp = prefab.Prefab.MapPoints[conn];
                                        if (neighbours.Length == 1 || mp.DestinationNodes.Any(x => x == endIndex) ||
                                            mp.ControlNodeIndex == endIndex)
                                        {
                                            points.Enqueue([..path, conn]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var (startNodeUid, links) in linksToAdd)
                {
                    foreach (var link in links)
                    {
                        nodes[startNodeUid]._links.Add(link);
                    }
                }
            }

            return nodes;
        }

        public List<RoutingLink> GetLinks()
        {
            return _links;
        }
    }
}