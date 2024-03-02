using NetTopologySuite.Geometries;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using System.Linq;
using TsMap.Exporter.Overlays;
using TsMap.Map.Overlays;
using TsMap.TsItem;

namespace TsMap.Exporter.Data
{
    public class ExpCity : ExpElement<TsCity>
    {
        public ExpCity(TsCity expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override ulong GetId()
        {
            return expObj.GetId();
        }

        public override ulong GetRefId()
        {
            return expObj.GetId();
        }

        public override (string, string) GetTitle()
        {
            return (expObj.LocalizationToken, expObj.Name);
        }

        public override Envelope GetEnvelope()
        {
            Envelope area = new();
            foreach (var c in mapper.Cities.Values.Where(x => x.City == expObj))
            {
                area.ExpandToInclude(new Envelope(c.X, c.X + c.Width, c.Z, c.Z + c.Height));
            }

            return area;
        }

        public override string GetType()
        {
            return "city";
        }

        public override Image GetIcon()
        {
            return mapper.OverlayManager.GetOrCreateOverlayImage(GetCountry().CountryCode, OverlayType.Flag).GetImage();
        }

        public override TsCountry GetCountry()
        {
            return mapper.GetCountryByTokenName(expObj.Country);
        }

        public override Dictionary<string, object> GetAdditionalData()
        {
            Envelope all = new();
            object[] singles = mapper.Cities.Values.Where(x => x.City == expObj).Select(x =>
            {
                var e = new Envelope(x.X, x.X + x.Width, x.Z, x.Z + x.Height);
                all.ExpandToInclude(e);
                return GetGeoJson(e);
            }).ToArray();
            var visibleCity = mapper.Cities.Values.Where(x => expObj == x.City && !x.Hidden).FirstOrDefault();

            return new()
            {
                ["areas"] = singles,
                ["containedArea"] = GetGeoJson(all),
                ["visibleArea"] = visibleCity != default(TsCityItem)
                    ? GetGeoJson(new Envelope(visibleCity.X, visibleCity.X + visibleCity.Width, visibleCity.Z,
                        visibleCity.Z + visibleCity.Height))
                    : null,
            };
        }

        public override ulong GetGameId()
        {
            return expObj.Token;
        }
    }
}