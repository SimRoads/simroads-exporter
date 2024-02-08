using Eto.Drawing;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using TsMap.Map.Overlays;

namespace TsMap.Exporter.Data
{
    public class ExpCountry : ExpElement<TsCountry>
    {
        public ExpCountry(TsCountry expObj, DataExporter exp) : base(expObj, exp)
        {
        }

        public override ulong GetId()
        {
            return expObj.GetId();
        }


        public override (string, string) GetTitle()
        {
            return (expObj.LocalizationToken, expObj.Name);
        }

        public override Envelope GetEnvelope()
        {
            return new Envelope(expObj.X, expObj.Y, expObj.X, expObj.Y);
        }

        public override Bitmap GetIcon()
        {
            return mapper.OverlayManager.GetOrCreateOverlayImage(expObj.CountryCode, OverlayType.Flag).GetBitmap();
        }

        public override Dictionary<string, object> GetAdditionalData()
        {
            return new()
            {
                { "cities", mapper.Cities.Values.Where(x => !x.Hidden && mapper.GetCountryByTokenName(x.City.Country) == expObj).Select(x=> (new ExpCity(x.City, exporter)).ExportList()).ToList() },
                { "licensePlateCode", expObj.LicensePlateCode },
                { "countryCode", expObj.CountryCode },
                { "speeds", expObj.Speeds.Select(x => new { vehicleType = x.Key, speeds = x.Value.Select(y => new { roadType = y.Key, speeds = y.Value.Select(z => new { speedType = z.Key, speed = z.Value }) }) }).ToList() },
            };
        }

        public override string GetType()
        {
            return "country";
        }

        public override ulong GetGameId()
        {
            return expObj.Token;
        }

    }
}
