using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using TsMap.TsItem;

namespace TsMap.Exporter.Data
{
    public partial class DataExporter : BaseExporter
    {
        public readonly TranslationExporter Translations;
        public readonly Quadtree<TsCityItem> cityTree = new();

        private Dictionary<ulong, List<ExpElement>> expElements = new();

        public DataExporter(TsMapper mapper) : base(mapper)
        {
            Translations = new TranslationExporter(mapper);

            foreach (var city in mapper.Cities.Values)
            {
                cityTree.Insert(new Envelope(city.X, city.Z, city.X + city.Width, city.Z + city.Height), city);
            }
        }

        public override void Prepare()
        {
            var activeDlcGuards = Mapper.GetDlcGuardsForCurrentGame().Where(x => x.Enabled).Select(x => x.Index).ToList();
            foreach (var overlay in Mapper.OverlayManager.GetOverlays())
            {
                if (!activeDlcGuards.Contains(overlay.DlcGuard)) continue;
                var ov = ExpOverlay.Create(overlay, this);
                if (ov == null) continue;
                var prefabId = overlay.GetPrefabId();
                if (!expElements.ContainsKey(prefabId))
                {
                    expElements[prefabId] = new();
                }
                expElements[prefabId].Add(ov);
            }

            foreach (var country in Mapper.GetCountries())
            {
                expElements[country.GetId()] = new List<ExpElement> { new ExpCountry(country, this) };
            }

            foreach (var city in Mapper.GetCities())
            {
                expElements[city.GetId()] = new List<ExpElement> { new ExpCity(city, this) };
            }

            foreach (var el in expElements.Values)
            {
                el.ForEach(x => x.ExportDetail());
            }

            Translations.Prepare();
        }

        public IEnumerable<Dictionary<string, object>> ExportElement(string id)
        {
            ulong idValue = Convert.ToUInt64(id);
            if (expElements.ContainsKey(idValue))
            {
                return expElements[idValue].Select(x => x.ExportDetail());
            }
            return null;
        }

        public IEnumerable<Dictionary<string, object>> ExportCountries()
        {
            return Mapper.GetCountries().Select(x => expElements[x.GetId()].First().ExportList());
        }

        public IEnumerable<Dictionary<string,object>> ExportCities()
        {
            return Mapper.GetCities().Select(x => expElements[x.GetId()].First().ExportList());
        }
    }

    public static partial class JSDataExporter
    {
        public static DataExporter Instance { get; private set; }

        [JSExport]
        public static void Load()
        {
            Instance = new(JSBaseExporter.MapperInstance);
            Instance.Prepare();
        }

        [JSExport]
        [return: JSMarshalAs<JSType.MemoryView>]
        public static Span<Byte> ExportElement(string id)
        {
            return JSBaseExporter.GetMsgPack(Instance.ExportElement(id));
        }

        [JSExport]
        [return: JSMarshalAs<JSType.MemoryView>]
        public static Span<Byte> ExportCountries()
        {
            return JSBaseExporter.GetMsgPack(Instance.ExportCountries());
        }

        [JSExport]
        [return: JSMarshalAs<JSType.MemoryView>]
        public static Span<Byte> ExportCities()
        {
            return JSBaseExporter.GetMsgPack(Instance.ExportCities());
        }
    }
}
