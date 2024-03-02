#nullable enable
using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TsMap.Common;
using Point = NetTopologySuite.Geometries.Point;

namespace TsMap.Exporter.Data
{
    public abstract class ExpElement<T> : ExpElement where T : class
    {
        protected readonly T expObj;

        public ExpElement(T expObj, DataExporter exp) : base(exp)
        {
            this.expObj = expObj;
        }
    }

    public abstract class ExpElement
    {
        protected readonly DataExporter exporter;
        protected readonly TsMapper mapper;

        public ExpElement(DataExporter exp)
        {
            this.exporter = exp;
            this.mapper = exp.Mapper;
        }

        public abstract ulong GetId();
        public abstract ulong GetRefId();
        public abstract (string?, string?) GetTitle();
        public abstract Envelope GetEnvelope();
        public new abstract string GetType();
        public abstract ulong GetGameId();

        public virtual (string?, string?) GetSubtitle()
        {
            return (null, null);
        }

        public virtual Image? GetIcon()
        {
            return null;
        }

        public virtual TsCountry? GetCountry()
        {
            return null;
        }

        public virtual TsCity? GetCity()
        {
            return null;
        }

        public virtual Dictionary<string, object> GetAdditionalData()
        {
            return new();
        }

        private ExpCity? GetExpCity()
        {
            return (GetCity() is var city && city != null)
                ? (new ExpCity(city, this.exporter))
                : null;
        }

        private ExpCountry? GetExpCountry()
        {
            return (GetCountry() is var c && c != null)
                ? (new ExpCountry(c, this.exporter))
                : null;
        }

        public Dictionary<string, object> ExportDetail()
        {
            var e = ExportList().Concat(GetAdditionalData()).ToLookup(x => x.Key, x => x.Value)
                .ToDictionary(x => x.Key, g => g.First());
            e["type"] = GetType();
            e["subtitle"] = Localize(GetSubtitle());
            e["country"] = GetExpCountry()?.ExportList();
            e["city"] = GetExpCity()?.ExportList();
            e["icon"] = (GetIcon() is var i && i != null) ? GetPng(i) : null;
            return e;
        }

        public Dictionary<string, object> ExportList()
        {
            return new()
            {
                { "id", GetId() },
                { "gameId", Tokenize(GetGameId()) },
                { "title", Localize(GetTitle()) },
                { "envelope", GetGeoJson(GetEnvelope()) }
            };
        }

        public Dictionary<string, object> ExportIndex()
        {
            return new()
            {
                { "id", GetId() },
                { "refId", GetRefId() },
                { "title", GetTitle() },
                { "subtitle", GetSubtitle() },
                { "city", GetExpCity()?.GetTitle() },
                { "country", GetExpCountry()?.GetTitle() }
            };
        }

        protected object Localize((string?, string?) data)
        {
            var (key, defValue) = data;
            if (key != null)
            {
                exporter.Translations.AddKey(key);
                return new { localeKey = key, defaultValue = defValue };
            }
            else
            {
                return defValue;
            }
        }

        protected object Tokenize(ulong token)
        {
            return new { token = token.ToString(), stringValue = ScsToken.TokenToString(token) };
        }

        protected object Tokenize(string token)
        {
            return new { token = ScsToken.StringToToken(token).ToString(), stringValue = token };
        }

        protected object GetGeoJson(object geometry)
        {
            if (geometry is Envelope e)
            {
                if (e.Area == 0) geometry = new Point(e.MinX, e.MinY);
                else geometry = GeometryFactory.Default.ToGeometry(e);
            }

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                GeoJsonSerializer.CreateDefault().Serialize(jsonWriter, geometry);
                return JsonHelper.Deserialize(stringWriter.ToString());
            }
        }

        protected byte[] GetPng(Image im)
        {
            using var ms = new MemoryStream();
            im.Save(ms, new PngEncoder());
            return ms.ToArray();
        }
    }
}