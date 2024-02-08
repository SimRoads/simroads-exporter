using System;
using System.Collections.Generic;
using TsMap.Map.Overlays;
using TsMap.TsItem;

namespace TsMap.Exporter
{
    public static class TsIds
    {
        private static readonly Dictionary<Object, ulong> ids = new();
        private static readonly Dictionary<ulong, Object> idsReverse = new();

        private static ulong getId(Object obj, ulong uid = 0)
        {

            if (!ids.ContainsKey(obj))
            {
                if (uid == 0) uid = (ulong)ids.Count;
                while (idsReverse.ContainsKey(uid))
                {
                    uid++;
                }
                ids.Add(obj, uid);
                idsReverse.Add(uid, obj);
            }
            return ids[obj];
        }

        public static ulong GetId(this TsItem.TsItem item)
        {
            return getId(item, item.Uid);
        }

        public static ulong GetId(this TsPrefabItem prefab, int mapPointIndex)
        {
            return getId(new Tuple<ulong, int>(prefab.Uid, mapPointIndex), prefab.Uid + (ulong)mapPointIndex + 1);
        }

        public static ulong GetId(this TsFerryConnection ferryConnection)
        {
            return getId(ferryConnection, ferryConnection.StartPort.Token & ferryConnection.EndPort.Token);
        }

        public static ulong GetId(this MapOverlay overlay)
        {
            if (overlay.ReferenceObj is TsCountry country)
            {
                return GetId(country);
            }
            return getId(overlay);
        }

        public static ulong GetPrefabId(this MapOverlay overlay)
        {
            TsPrefabItem prefab = null;
            if (overlay.ReferenceObj is TsBusStopItem busStop)
            {
                prefab = busStop.PrefabItem;
            }
            else if (overlay.ReferenceObj is TsCompanyItem company)
            {
                prefab = company.PrefabItem;
            }
            else if (overlay.ReferenceObj is TsFerryItem ferry)
            {
                prefab = ferry.PrefabItem;
            }
            else if (overlay.ReferenceObj is TsPrefabItem prefabItem)
            {
                prefab = prefabItem;
            }
            return prefab == null ? overlay.GetId() : prefab.GetId();
        }

        public static ulong GetId(this TsCountry country)
        {
            return getId(country, country.Token);
        }

        public static ulong GetId(this TsCity city)
        {
            return getId(city, city.Token);
        }
    }
}
