using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.TsItem;

namespace TsMap.Exporter.Routing
{
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
            return [Id, Node.X, Node.Z];
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
                            rNode._links.Add(new RoadRoutingLink(road, rNode, nodes[end]));
                        else if (end == node.Uid &&
                                 ((road.RoadLook.IsOneWay() && road.LeftDriving) || !road.RoadLook.IsOneWay()))
                            rNode._links.Add(new RoadRoutingLink(road, rNode, nodes[start]));
                    }
                    else if (item is TsPrefabItem prefab)
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


                        foreach (var endIndex in endIndexes)
                        {
                            rNode._links.Add(new PrefabRoutingLink(prefab, rNode, nodes[pNodes[endIndex]]));
                        }
                    }
                }
            }

            Dictionary<TsFerry, TsFerryItem> ferryToItem = new();
            foreach (var f in mapper.FerryPorts.Values)
            {
                ferryToItem[f.Ferry] = f;
            }

            foreach (var (port, connection) in mapper.FerryPorts.Values.SelectMany(x =>
                         x.Ferry.GetConnections().Where(ferry => ferry.StartPort.Token > ferry.EndPort.Token)
                             .Select(conn => (x, conn))
                     ))
            {
                var startNodes = port.PrefabItem.Nodes.Where(n => nodes[n].GetLinks().Count > 0).Select(x => nodes[x]);
                var endNodes = ferryToItem[connection.EndPort].PrefabItem.Nodes
                    .Where(n => nodes[n].GetLinks().Count > 0).Select(x => nodes[x]);
                foreach (var start in startNodes)
                {
                    foreach (var end in endNodes)
                    {
                        start._links.Add(new FerryRoutingLink(connection, start, end));
                        end._links.Add(new FerryRoutingLink(connection, end, start));
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