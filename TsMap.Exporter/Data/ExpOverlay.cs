using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.Exporter.Overlays;
using TsMap.Helpers;
using TsMap.Map.Overlays;
using TsMap.TsItem;

namespace TsMap.Exporter.Data
{
    public abstract class ExpOverlay : ExpElement<MapOverlay>
    {
        protected ExpOverlay(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public static ExpOverlay Create(MapOverlay obj, DataExporter exp)
        {
            switch (obj.ReferenceObj)
            {
                case TsBusStopItem busStop:
                    return new ExpBusStop(obj, exp);
                case TsCompanyItem company:
                    return new ExpCompany(obj, exp);
                case TsCutsceneItem cutscene:
                    return new ExpCutscene(obj, exp);
                case TsFerryItem ferry:
                    return new ExpFerry(obj, exp);
                case TsPrefabItem prefab:
                    return new ExpPrefab(obj, exp);
                case TsTriggerItem trigger:
                    return new ExpTrigger(obj, exp);
                case TsMapOverlayItem overlayItem:
                    return new ExpOverlayItem(obj, exp);
                default:
                    return null;
            }
        }
    }

    public abstract class ExpOverlay<T> : ExpOverlay where T : class
    {
        protected T refObj;
        protected ExpOverlay(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
            refObj = expObj.ReferenceObj as T;
        }

        public override Image GetIcon()
        {
            return expObj.OverlayImage.GetImage();
        }

        public override ulong GetId()
        {
            return expObj.GetId();
        }

        public override string GetType()
        {
            return "overlay";
        }

        public override Envelope GetEnvelope()
        {
            return new Envelope(expObj.Position.X, expObj.Position.Y, expObj.Position.X, expObj.Position.Y);
        }

        public override TsCountry GetCountry()
        {
            var city = GetCity();
            if (city != null) return mapper.GetCountryByTokenName(city.Country);

            TsCountry country = null;
            if (refObj is TsItem.TsItem item)
            {
                country = item.Nodes.Select(x => mapper.Nodes[x]).Select(x => x.GetCountry()).First(x => x != null);
            }
            country ??= mapper.NodesIndex.NearestNeighbor(new Coordinate(expObj.Position.X, expObj.Position.Y)).Data.GetCountry();
            return country;
        }

        public override TsCity GetCity()
        {
            var env = GetEnvelope();
            var city = exporter.cityTree.Query(env).First(x => (new Envelope(x.X, x.Z, x.X + x.Width, x.Z + x.Height)).Intersects(env)).City;
            return city;
        }

        public override Dictionary<string, object> GetAdditionalData()
        {
            var data = base.GetAdditionalData();
            ulong prefabUid = expObj.GetPrefabId();
            if (prefabUid != expObj.GetId())
            {
                data["prefab"] = prefabUid;
                TsPrefabItem prefabItem = (TsPrefabItem)TsIds.GetObject(prefabUid);
                Envelope prefabArea = new Envelope();

                var originNode = mapper.GetNodeByUid(prefabItem.Nodes[0]);
                var mapPointOrigin = prefabItem.Prefab.PrefabNodes[prefabItem.Origin];
                var rot = (float)(originNode.Rotation - Math.PI -
                                   Math.Atan2(mapPointOrigin.RotZ, mapPointOrigin.RotX) + Math.PI / 2);
                var prefabstartX = originNode.X - mapPointOrigin.X;
                var prefabStartZ = originNode.Z - mapPointOrigin.Z;
                for (var i = 0; i < prefabItem.Prefab.MapPoints.Count; i++)
                {
                    var mapPoint = prefabItem.Prefab.MapPoints[i];
                    var newPoint = RenderHelper.RotatePoint(
                        prefabstartX + mapPoint.X,
                        prefabStartZ + mapPoint.Z, rot, originNode.X,
                        originNode.Z);
                    prefabArea.ExpandToInclude(newPoint.X, newPoint.Y);
                }

                data["prefabArea"] = GetGeoJson(prefabArea);
            }

            return data;
        }

    }

    public class ExpBusStop : ExpOverlay<TsBusStopItem>
    {
        public ExpBusStop(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override (string, string) GetTitle()
        {
            return (null, "%Bus Stop%");
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override string GetType()
        {
            return "bus_stop";
        }
    }

    public class ExpCompany : ExpOverlay<TsCompanyItem>
    {
        public ExpCompany(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override (string, string) GetTitle()
        {
            return (null, refObj.Company.Name);
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override string GetType()
        {
            return "company";
        }

        public override (string, string) GetSubtitle()
        {
            return ("job_offer_title", "%Company%");
        }
    }

    public class ExpCutscene : ExpOverlay<TsCutsceneItem>
    {
        public ExpCutscene(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override (string, string) GetTitle()
        {
            return (refObj.LocalizationToken, null);
        }

        public override (string, string) GetSubtitle()
        {
            return ("viewpoint_title", "%Landmark%");
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override string GetType()
        {
            return "landmark";
        }
    }

    public class ExpFerry : ExpOverlay<TsFerryItem>
    {
        public ExpFerry(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override (string, string) GetSubtitle()
        {
            return refObj.IsTrain ? ("mapl_train", "%EuroTunnel%") : ("mapl_port", "%Ferry Port%");
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override string GetType()
        {
            return refObj.IsTrain ? "train" : "ferry";
        }

        public override (string, string) GetTitle()
        {
            return (refObj.Ferry.LocalizationToken, refObj.Ferry.Name);
        }
    }

    public class ExpPrefab : ExpOverlay<TsPrefabItem>
    {
        public ExpPrefab(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override (string, string) GetTitle()
        {
            string token = null, defaultName = "Undefined prefab";
            switch (expObj.OverlayName)
            {
                case "gas_ico":
                    token = "mapl_gas";
                    defaultName = "%Gas Station%";
                    break;
                case "service_ico":
                    token = "mapl_service";
                    defaultName = "%Service%";
                    break;
                case "dealer_ico":
                    token = "mapl_dealer";
                    defaultName = "%Dealer%";
                    break;
                case "weigh_station_ico":
                    token = "mapl_weighst";
                    defaultName = "%Weight Station%";
                    break;
                case "garage_large_ico":
                    token = "mapl_garage";
                    defaultName = "%Garage%";
                    break;
                case "recruitment_ico":
                    token = "mapl_recruit";
                    defaultName = "%Recruitment Agency%";
                    break;
                case "parking_ico":
                    token = "mapl_parking";
                    defaultName = "%Parking%";
                    break;
            }
            return (token, defaultName);
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override string GetType()
        {
            switch (expObj.OverlayName)
            {
                case "gas_ico":
                    return "gas";
                case "service_ico":
                    return "service";
                case "dealer_ico":
                    return "dealer";
                case "weigh_station_ico":
                    return "weight_station";
                case "garage_large_ico":
                    return "garage";
                case "recruitment_ico":
                    return "recruitment";
                case "parking_ico":
                    return "parking";
            }
            return "prefab_element";
        }
    }

    public class ExpTrigger : ExpOverlay<TsTriggerItem>
    {
        public ExpTrigger(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override (string, string) GetTitle()
        {
            string token = null, defaultName = "Undefined trigger";
            switch (expObj.OverlayName)
            {
                case "parking_ico":
                    token = "mapl_parking";
                    defaultName = "%Parking%";
                    break;
            }
            return (token, defaultName);
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override string GetType()
        {
            switch (expObj.OverlayName)
            {
                case "parking_ico":
                    return "parking";
            }
            return "trigger";
        }
    }

    public class ExpOverlayItem : ExpOverlay<TsMapOverlayItem>
    {
        private string roadNumber = null;

        public ExpOverlayItem(MapOverlay expObj, DataExporter exp) : base(expObj, exp)
        {
            if (expObj.OverlayType == OverlayType.Road && !expObj.OverlayName.EndsWith("_ico") && expObj.OverlayName != "quarry")
            {
                roadNumber = (expObj.OverlayName.Contains('_') ? String.Join(' ', expObj.OverlayName.Split('_').Skip(1)) : expObj.OverlayName).ToUpper();
            }
        }

        public override ulong GetGameId()
        {
            return refObj.Uid;
        }

        public override (string, string) GetSubtitle()
        {
            if (roadNumber != null)
            {
                return ("mapl_road_numbers", "%Road Numbers%");
            }
            return (null, null);
        }

        public override (string, string) GetTitle()
        {
            switch (expObj.OverlayName)
            {
                case "quarry":
                    return ("mapl_quarry", "%Quarry%");
                case "toll_ico":
                    return ("mapl_toll", "%Toll Booth%");
                case "border_ico":
                    return ("mapl_border", "%Border Crossing%");
                case "weigh_ico":
                    return ("mapl_weighst", "%Weight Station%");
            }
            if (roadNumber != null)
            {
                return (null, roadNumber);
            }
            else
            {
                return (null, "Undefined overlay");
            }
        }

        public override string GetType()
        {
            switch (expObj.OverlayName)
            {
                case "quarry":
                    return "quarry";
                case "toll_ico":
                    return "toll";
                case "border_ico":
                    return "border";
                case "weigh_ico":
                    return "weigh";
            }
            return "road_number";
        }
    }
}
