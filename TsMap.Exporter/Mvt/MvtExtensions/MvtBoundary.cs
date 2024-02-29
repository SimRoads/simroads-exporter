using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Simplify;
using static TsMap.Exporter.Mvt.Tile.Types;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtBoundary : RectMvtExtension
    {
        public readonly Polygon Polygon;
        public readonly TsCountry Country;

        public MvtBoundary(Polygon polygon, TsCountry country, TsMapper mapper) : base(mapper)
        {
            Polygon = polygon;
            Country = country;
        }

        public override bool Skip(ExportSettings sett)
        {
            return base.Skip(sett) || !Polygon.Intersects(GeometryFactory.Default.ToGeometry(sett.Envelope)) ||
                   Polygon.NumPoints < 3;
        }

        protected override Envelope CalculateEnvelope()
        {
            return Polygon.EnvelopeInternal;
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            var polys = GetPolygon(sett);
            bool inserted = false;
            foreach (var poly in polys)
            {
                uint cursorX = 0, cursorY = 0;
                var points = new List<uint>() { sett.GenerateCommandInteger(MapboxCommandType.MoveTo, 1) };
                var coords = Polygon.Coordinates;
                for (var i = 0; i < coords.Length - 1; i++)
                {
                    var point = coords[i];
                    if (i == 1)
                        points.Add(
                            sett.GenerateCommandInteger(MapboxCommandType.LineTo, coords.Length - 2));
                    points.AddRange(
                        sett.GenerateDeltaFromGame((float)point.X, (float)point.Y, ref cursorX, ref cursorY));
                }

                points.Add(sett.GenerateCommandInteger(MapboxCommandType.ClosePath, 1));
                var feature = new Feature() { Type = GeomType.Polygon, Geometry = { points } };
                if (Country != null)
                {
                    foreach (var locale in Mapper.Localization.GetLocales())
                        feature.Tags.Add(
                            layers.Bounds.GetOrCreateTag($"name_{locale}",
                                Mapper.Localization.GetLocaleValue(Country.LocalizationToken, locale) ?? Country.Name));
                }

                layers.Bounds.Features.Add(feature);
                inserted = true;
            }

            return inserted;
        }

        private Polygon[] GetPolygon(ExportSettings settings)
        {
            /*var result = TopologyPreservingSimplifier.Simplify(
                Polygon.Intersection(GeometryFactory.Default.ToGeometry(settings.Envelope)),
                (double)(settings.Extent * settings.DiscretizationThreshold));*/
            var result = Polygon.Intersection(GeometryFactory.Default.ToGeometry(settings.Envelope));

            if (result is Polygon poly) return new[] { poly };
            else if (result is MultiPolygon multiPoly) return multiPoly.Geometries.Cast<Polygon>().ToArray();
            else throw new Exception("Unexpected geometry type");
        }
    }
}