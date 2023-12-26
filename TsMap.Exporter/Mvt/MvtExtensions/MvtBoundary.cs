using NetTopologySuite.Geometries;
using NetTopologySuite.Operation;
using NetTopologySuite.Simplify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TsMap.Exporter.Mvt.Tile.Types;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    internal class MvtBoundary : RectMvtExtension
    {
        public readonly Polygon Polygon;

        public MvtBoundary(Polygon polygon, TsMapper mapper) : base(mapper)
        {
            Polygon = polygon;
        }

        public override bool Skip(ExportSettings sett)
        {
            return base.Skip(sett) || !Polygon.Intersects(GeometryFactory.Default.ToGeometry(sett.Envelope)) || Polygon.NumPoints < 3;
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
                for (var i = 0; i < Polygon.Coordinates.Length - 1; i++)
                {
                    var point = Polygon.Coordinates[i];
                    if (i == 1) points.Add(sett.GenerateCommandInteger(MapboxCommandType.LineTo, Polygon.Coordinates.Length - 2));
                    points.AddRange(sett.GenerateDeltaFromGame((float)point.X, (float)point.Y, ref cursorX, ref cursorY));
                }
                points.Add(sett.GenerateCommandInteger(MapboxCommandType.ClosePath, 1));
                var feature = new Feature() { Type = GeomType.Polygon, Geometry = { points } };
                layers.bounds.Features.Add(feature);
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
