using System;
using System.Collections.Generic;
using TsMap.Map.Overlays;
using TsMap.TsItem;

namespace TsMap.Exporter
{
    public static class TsIds
    {
        private static readonly Dictionary<Object, ulong> ids = new();

        private static ulong getId(Object obj)
        {
            if (!ids.ContainsKey(obj))
            {
                ids.Add(obj, (ulong)ids.Count);
            }
            return ids[obj];
        }

        public static ulong GetId(this TsRoadItem road)
        {
            return getId(road.Uid);
        }

        public static ulong GetId(this TsPrefabItem prefab, int mapPointIndex)
        {
            return getId(new Tuple<ulong,int>(prefab.Uid, mapPointIndex));
        }

        public static ulong GetId(this TsFerryConnection ferryConnection)
        {
            return getId(ferryConnection);
        }

        public static ulong GetId(this TsMapAreaItem mapArea)
        {
            return getId(mapArea.Uid);
        }

        public static ulong GetId(this MapOverlay overlay)
        {
            if (overlay.ReferenceObj is TsItem.TsItem item)
            {
                return getId(item.Uid);
            } else if (overlay.ReferenceObj is TsCountry country){
                return getId(country);
            }
            return getId(overlay.ReferenceObj);
        }

        public static ulong GetId(this TsCityItem city)
        {
            return getId(city.Uid);
        }
    }
}
