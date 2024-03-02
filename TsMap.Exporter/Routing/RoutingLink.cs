using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.TsItem;

namespace TsMap.Exporter.Routing;

public abstract class RoutingLink
{
    protected RoutingNode StartNode;
    protected RoutingNode EndNode;

    protected ushort Length;
    protected byte RoadSize;
    protected ulong FeatureId;

    protected RoutingLink(RoutingNode startNode, RoutingNode endNode, ulong featureId)
    {
        StartNode = startNode;
        EndNode = endNode;
        FeatureId = featureId;
    }

    public abstract string GetLinkType();

    public virtual object[] Serialize()
    {
        return [GetLinkType(), StartNode.Id, EndNode.Id, Length, RoadSize, FeatureId];
    }
}

public class RoadRoutingLink : RoutingLink
{
    protected TsRoadItem Road;

    public RoadRoutingLink(TsRoadItem road, RoutingNode startNode, RoutingNode endNode) : base(startNode, endNode,
        road.GetId())
    {
        Road = road;

        Length = (ushort)(Math.Sqrt(Math.Pow(startNode.Node.X - endNode.Node.X, 2) +
                                    Math.Pow(startNode.Node.Z - endNode.Node.Z, 2)) * 10);
        RoadSize = (byte)(road.LeftDriving ? road.RoadLook.LanesLeft.Count : road.RoadLook.LanesRight.Count);
    }

    public override string GetLinkType()
    {
        return "road";
    }
}

public class PrefabRoutingLink : RoutingLink
{
    protected TsPrefabItem Prefab;
    protected List<ulong> ElementsIds = new();

    public PrefabRoutingLink(TsPrefabItem prefab, RoutingNode startNode, RoutingNode endNode) : base(startNode,
        endNode, prefab.GetId())
    {
        Prefab = prefab;

        var pNodes = new List<ulong>(prefab.Nodes);
        for (var i = 0; i < prefab.Origin; i++)
        {
            var el = pNodes.Last();
            pNodes.RemoveAt(pNodes.Count - 1);
            pNodes.Insert(0, el);
        }

        var startNodeIndex = pNodes.IndexOf(startNode.Node.Uid);
        var endIndex = pNodes.IndexOf(endNode.Node.Uid);

        var startMapPoint =
            prefab.Prefab.MapPoints.FindIndex(x => x.ControlNodeIndex == startNodeIndex);
        if (startMapPoint == -1)
        {
            Length = 0;
            RoadSize = 1;
        }
        else
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
                    Length = 0;
                    RoadSize = (byte)(prefab.Prefab.MapPoints[path[0]].LaneCount);
                    for (int i = 1; i < path.Count; i++)
                    {
                        var (prev, curr) = (prefab.Prefab.MapPoints[path[i - 1]],
                            prefab.Prefab.MapPoints[path[i]]);
                        Length += (ushort)(Math.Sqrt(Math.Pow(prev.X - curr.X, 2) +
                                                     Math.Pow(prev.Z - curr.Z, 2)) * 10);
                        RoadSize = (byte)Math.Min(RoadSize, curr.LaneCount);
                        ElementsIds.Add(prefab.GetId(path[i], path[i - 1]));
                    }

                    break;
                }
                else
                {
                    var neighbours = last.Neighbours.Except(path).ToArray();
                    foreach (var conn in neighbours)
                    {
                        var mp = prefab.Prefab.MapPoints[conn];
                        if (!mp.Hidden &&
                            (neighbours.Length == 1 || mp.DestinationNodes.Any(x => x == endIndex) ||
                             mp.ControlNodeIndex == endIndex))
                        {
                            points.Enqueue([..path, conn]);
                        }
                    }
                }
            }
        }
    }

    public override string GetLinkType()
    {
        return "prefab";
    }

    public override object[] Serialize()
    {
        return [..base.Serialize(), ElementsIds.ToArray()];
    }
}

public class FerryRoutingLink : RoutingLink
{
    protected TsFerryConnection FerryConnection;

    public FerryRoutingLink(TsFerryConnection ferryConn, RoutingNode startNode, RoutingNode endNode) : base(startNode,
        endNode, ferryConn.GetId())
    {
        FerryConnection = ferryConn;

        Length = (ushort)(Math.Sqrt(Math.Pow(startNode.Node.X - endNode.Node.X, 2) +
                                    Math.Pow(startNode.Node.Z - endNode.Node.Z, 2)) * 10);
        RoadSize = 1;
    }

    public override string GetLinkType()
    {
        return "ferry";
    }
}