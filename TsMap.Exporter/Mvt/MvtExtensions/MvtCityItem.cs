using System;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using TsMap.TsItem;
using static TsMap.Exporter.Mvt.Tile.Types;
using static TsMap.Exporter.Mvt.VectorTileUtils;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtCityItem : RectMvtExtension
    {
        public readonly TsCityItem City;

        public MvtCityItem(TsCityItem city, TsMapper mapper) : base(mapper)
        {
            City = city;
        }
        public override bool Skip(ExportSettings sett)
        {
            return City.Hidden;
        }

        protected override Envelope CalculateEnvelope()
        {
            Envelope env = new();
            Mapper.Cities.Values.Where(x => x.City == City.City).ToList().ForEach(x =>
            {
                var (sx, sz) = Mapper.MapSettings.Correct(x.X, x.Z);
                var (ex, ez) = Mapper.MapSettings.Correct(x.X + x.Width, x.Z + x.Height);
                env.ExpandToInclude(new Envelope(sx, ex, sz, ez));
            });
            return env;
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            uint cursorX = 0, cursorY = 0;
            var (sx, sz) = Mapper.MapSettings.Correct(City.X, City.Z);
            var (ex, ez) = Mapper.MapSettings.Correct(City.X + City.Width, City.Z + City.Height);
            var points = new Coordinate[] { new Coordinate(sx, sz), new Coordinate(sx, ez), new Coordinate(ex, ez), new Coordinate(ex, sz) };

            var geometry = new List<uint>() { GenerateCommandInteger(MapboxCommandType.MoveTo, 1), };
            for (int j = 0; j < points.Length; j++)
            {
                if (j == 1) geometry.Add(GenerateCommandInteger(MapboxCommandType.LineTo, points.Length - 1));
                geometry.AddRange(sett.GenerateDeltaFromGame((float)points[j].X, (float)points[j].Y, ref cursorX, ref cursorY));
            }

            geometry.Add(GenerateCommandInteger(MapboxCommandType.ClosePath, 1));
            var feature = new Feature
            {
                Id = City.City.GetId(),
                Type = GeomType.Polygon,
                Geometry = { geometry }
            };
            foreach (var locale in Mapper.Localization.GetLocales()) feature.Tags.Add(
                layers.Overlays.GetOrCreateTag($"name_{locale}", Mapper.Localization.GetLocaleValue(City.City.LocalizationToken, locale) ?? City.City.Name));

            layers.Overlays.Features.Add(feature);
            return true;
        }
    }
}
