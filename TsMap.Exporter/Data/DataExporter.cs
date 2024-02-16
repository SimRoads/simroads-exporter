using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.TsItem;

namespace TsMap.Exporter.Data
{
    public class DataExporter : BaseExporter
    {
        public readonly TranslationExporter Translations;

        private readonly Quadtree<TsCityItem> cityTree = new();
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

        public object ExportElement(string id)
        {
            ulong idValue = Convert.ToUInt64(id);
            if (expElements.ContainsKey(idValue))
            {
                return expElements[idValue].Select(x => x.ExportDetail()).ToList();
            }
            return null;
        }

        public object ExportCountries()
        {
            return Mapper.GetCountries().Select(x => expElements[x.GetId()].First().ExportList()).ToList();
        }

        public object ExportCities()
        {
            return Mapper.GetCities().Select(x => expElements[x.GetId()].First().ExportList()).ToList();
        }

    }
}
