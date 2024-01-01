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
            layers.overlays.Features.Add(new Feature { Id = Overlay.GetId(), Type = GeomType.Point, Geometry = { GenerateCommandInteger(MapboxCommandType.MoveTo, 1), sett.GenerateDeltaFromGame(pos.X, pos.Y) } });
            return true;
        }
    }
}
