using NetTopologySuite.Geometries;
using System.Collections.Generic;
using TsMap.Map.Overlays;
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
            var (sx, sz) = Mapper.MapSettings.Correct(City.X, City.Z);
            var (ex, ez) = Mapper.MapSettings.Correct(City.X + City.Width, City.Z + City.Height);
            return new Envelope(sx, ex, sz, ez);
        }

        protected override bool SaveMvtLayersInternal(ExportSettings sett, Layers layers)
        {
            uint cursorX = 0, cursorY = 0;
            var points = ((Polygon)GeometryFactory.Default.ToGeometry(Envelope)).Coordinates;

            var geometry = new List<uint>() {  GenerateCommandInteger(MapboxCommandType.MoveTo, 1), };
            for (int j = 0; j < points.Length; j++)
            {
                if (j == 1) geometry.Add(GenerateCommandInteger(MapboxCommandType.LineTo, points.Length - 1));
                geometry.AddRange(sett.GenerateDeltaFromGame((float)points[j].X, (float)points[j].Y, ref cursorX, ref cursorY));
            }
            geometry.Add(GenerateCommandInteger(MapboxCommandType.ClosePath, 1));

            layers.overlays.Features.Add(new Feature { Id = City.GetId(), Type = GeomType.Polygon, Geometry = { geometry } });
            return true;
        }
    }
}
