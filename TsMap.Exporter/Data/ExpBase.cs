using Eto.Drawing;
using MessagePack;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TsMap.Common;
using Point = NetTopologySuite.Geometries.Point;

namespace TsMap.Exporter.Data
{
    public abstract class ExpElement<T> where T : class
    {

        protected readonly DataExporter exporter;
        protected readonly TsMapper mapper;
        protected readonly T expObj;

        public ExpElement(T expObj, DataExporter exp)
        {
            this.expObj = expObj;
            this.exporter = exp;
            this.mapper = exp.Mapper;
        }

        public abstract ulong GetId();
        public abstract (string, string) GetTitle();
        public abstract Envelope GetEnvelope();
        public new abstract string GetType();
        public abstract ulong GetGameId();
        public virtual (string, string) GetSubtitle() { return (null, null); }
        public virtual Bitmap GetIcon() { return null; }
        public virtual TsCountry GetCountry() { return null;}
        public virtual TsCity GetCity() { return null; }
        public virtual Dictionary<string, object> GetAdditionalData() { return new(); }

        public Dictionary<string, object> ExportDetail()
        {
            var (subtitleKey, subtitleDefault) = GetSubtitle();
            var e = ExportList().Concat(GetAdditionalData()).ToLookup(x => x.Key, x => x.Value).ToDictionary(x => x.Key, g => g.First());
            e["type"] = GetType();
            e["subtitle"] = subtitleKey != null ? Localize(subtitleKey, subtitleDefault) : subtitleDefault;
            e["country"] = (GetCountry() is var c && c != null) ? (new ExpCountry(c, this.exporter)).ExportList() : null;
            e["city"] = (GetCity() is var city && city != null) ? (new ExpCity(city, this.exporter)).ExportList() : null;
            e["icon"] = GetPng(GetIcon());
            return e;
        }
        public Dictionary<string, object> ExportList()
        {
            var (titleKey, titleDefault) = GetTitle();
            return new()
            {
                {"id", GetId() },
                {"gameId", Tokenize(GetGameId()) },
                {"title", titleKey != null ? Localize(titleKey, titleDefault) : titleDefault},
                {"envelope", (GetEnvelope() is var e && e.Area == 0) ? GetGeoJson(e) : GetGeoJson(new Point(e.MinX, e.MinY))}
            };
        }


        protected object Localize(string key, string defaultValue = "")
        {
            exporter.Translations.SelectedKeys.Add(key);
            return new { localeKey = key, defaultValue = defaultValue == "" ? mapper.Localization.GetLocaleValue(key, "en_us") : defaultValue };
        }
        protected object Tokenize(ulong token)
        {
            return new { token, stringValue = ScsToken.TokenToString(token) };
        }
        protected object Tokenize(string token)
        {
            return new { token = ScsToken.StringToToken(token), stringValue = token };
        }
        protected object GetGeoJson(object geometry)
        {
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                GeoJsonSerializer.CreateDefault().Serialize(jsonWriter, geometry);
                return JsonHelper.Deserialize(stringWriter.ToString());
            }
        }
        protected byte[] GetPng(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

}
