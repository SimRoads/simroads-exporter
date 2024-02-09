using NetTopologySuite.Geometries;
using TsMap.Map.Overlays;
using static TsMap.Exporter.Mvt.Tile.Types;
using static TsMap.Exporter.Mvt.VectorTileUtils;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtOverlay : PointMvtExtension
    {
        public readonly MapOverlay Overlay;

        public MvtOverlay(MapOverlay overlay, TsMapper mapper) : base(mapper)
        {
            Overlay = overlay;
        }
        public override bool Skip(ExportSettings sett)
        {
            return Overlay.IsSecret || !sett.ActiveDlcGuards.Contains(Overlay.DlcGuard);
        }

        protected override Coordinate CalculateCoordinate()
        {
            return new Coordinate(Overlay.Position.X, Overlay.Position.Y);
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            var pos = Mapper.MapSettings.Correct(Overlay.Position);
            var feature = new Feature
            {
                Id = Overlay.GetId(),
                Type = GeomType.Point,
                Geometry = { GenerateCommandInteger(MapboxCommandType.MoveTo, 1), sett.GenerateDeltaFromGame(pos.X, pos.Y) },
                Tags = {
                    layers.overlays.GetOrCreateTag("overlay", Overlay.OverlayName),
                    layers.overlays.GetOrCreateTag("prefab", Overlay.GetPrefabId())
                }
            };
            if (Overlay.ReferenceObj is TsCountry country)
            {
                foreach (var locale in Mapper.Localization.GetLocales()) feature.Tags.Add(
                    layers.overlays.GetOrCreateTag($"name_{locale}", Mapper.Localization.GetLocaleValue(country.LocalizationToken, locale) ?? country.Name));
            }

            layers.overlays.Features.Add(feature);

            return true;
        }
    }
}
