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
            lock (ids)
            {
                if (!ids.ContainsKey(obj))
                {
                    if (uid == 0) uid = (ulong)ids.Count;
                    uid &= (ulong)(Math.Pow(2, 53) - 1);
                    while (idsReverse.ContainsKey(uid))
                    {
                        if (uid == (ulong)(Math.Pow(2, 53) - 1)) uid = (ulong)ids.Count;
                        uid++;
                        uid &= (ulong)(Math.Pow(2, 53) - 1);
                    }

                    ids[obj] = uid;
                    idsReverse[uid] = obj;
                }
            }

            return ids[obj];
        }

        public static object GetObject(ulong id)
        {
            return idsReverse[id];
        }

        public static ulong GetId(this TsItem.TsItem item)
        {
            return getId(item, item.Uid);
        }

        public static ulong GetId(this TsPrefabItem prefab, int mapPointStart,  int mapPointEnd)
        {
            if (mapPointStart > mapPointEnd) (mapPointStart, mapPointEnd) = (mapPointEnd, mapPointStart);
            return getId(new Tuple<ulong, int, int>(prefab.Uid, mapPointStart, mapPointEnd));
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