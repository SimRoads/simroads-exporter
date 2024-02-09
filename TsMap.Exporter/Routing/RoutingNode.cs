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

            Length = (ushort)(Math.Sqrt(Math.Pow(startNode.Node.X - endNode.Node.X, 2) + Math.Pow(startNode.Node.Z - endNode.Node.Z, 2)) * 10);
            RoadSize = (byte)(road.LeftDriving ? road.RoadLook.LanesLeft.Count : road.RoadLook.LanesRight.Count);
            ElementsIds.Add(road.GetId());
        }

        public RoutingLink(TsPrefabItem prefab, RoutingNode startNode, RoutingNode endNode)
        {
            StartNode = startNode;
            EndNode = endNode;

            var (start, end) = (prefab.Nodes.IndexOf(startNode.Node.Uid), prefab.Nodes.IndexOf(endNode.Node.Uid));
            var (startMapPoint, endMapPoint) = (prefab.Prefab.MapPoints.FindIndex(x => x.ControlNodeIndex == start), prefab.Prefab.MapPoints.FindIndex(x => x.ControlNodeIndex == end));
            Stack<List<int>> q = new();
            q.Push(new List<int>() { startMapPoint });
            while (q.Count > 0)
            {
                var el = q.Pop();
                if (el.Last() == endMapPoint)
                {
                    Length = 0;
                    RoadSize = (byte)(prefab.Prefab.MapPoints[el[0]].LaneCount);
                    ElementsIds.Add(prefab.GetId(el[0]));
                    for (int i = 1; i < el.Count; i++)
                    {
                        var (prev, curr) = (prefab.Prefab.MapPoints[el[i - 1]], prefab.Prefab.MapPoints[el[i]]);
                        Length += (ushort)(Math.Sqrt(Math.Pow(prev.X - curr.X, 2) + Math.Pow(prev.Z - curr.Z, 2)) * 10);
                        RoadSize = (byte)Math.Min(RoadSize, curr.LaneCount);
                        ElementsIds.Add(prefab.GetId(el[i]));
                    }
                    return;
                }
                else
                {
                    foreach (var next in prefab.Prefab.MapPoints[el.Last()].Neighbours)
                    {
                        if (!el.Contains(next) && (prefab.Prefab.MapPoints[el.Last()].DestinationNodes.Contains((sbyte)endMapPoint) || prefab.Prefab.MapPoints[el.Last()].Neighbours.Count == 1))
                        {
                            q.Push(new List<int>(el) { next });
                        }
                    }
                }
            }
        }

        public object[] Serialize()
        {
            return new object[] { StartNode.Id, EndNode.Id, ElementsIds, Length, RoadSize };
        }

    }

    public class RoutingNode
    {
        public readonly ushort Id;
        public readonly TsNode Node;

        private List<RoutingLink> Links = new();
        private static ushort NextId = 0;

        public RoutingNode(TsNode node)
        {
            Node = node;
            Id = NextId++;
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

            foreach (var (uid, rNode) in nodes)
            {
                var node = rNode.Node;
                TsItem.TsItem[] items = new TsItem.TsItem[] { node.BackwardItem, node.ForwardItem };

                foreach (var item in items)
                {
                    if (item is TsRoadItem road)
                    {
                        var (start, end) = (road.GetStartNode().Uid, road.GetEndNode().Uid);
                        if (road.RoadLook.IsOneWay() && start != node.Uid) continue;
                        if (start == node.Uid && !road.LeftDriving) rNode.Links.Add(new RoutingLink(road, rNode, nodes[end]));
                        else if (end == node.Uid && road.LeftDriving) rNode.Links.Add(new RoutingLink(road, rNode, nodes[start]));
                    }
                    else if (item is TsPrefabItem prefab)
                    {
                        foreach (var n in prefab.Nodes.Where(x => x != node.Uid))
                            rNode.Links.Add(new RoutingLink(prefab, rNode, nodes[n]));
                    }
                }
            }

            return nodes;
        }

        public List<RoutingLink> GetLinks()
        {
            return Links;
        }
    }
}
