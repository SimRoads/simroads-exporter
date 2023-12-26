using Emgu.CV.ML;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Operation.Distance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsMap.Exporter.Mvt.MvtExtensions
{
    public abstract class MvtExtension
    {
        protected readonly TsMapper Mapper;

        public MvtExtension(TsMapper mapper)
        {
            Mapper = mapper;
        }
        public bool SaveMvtLayers(ExportSettings sett, Layers layers)
        {
            if (Skip(sett)) return false;
            return SaveMvtLayersInternal(sett, layers);
        }
        public virtual bool Discretizate(ExportSettings sett)
        {
            return false;
        }
        public virtual bool Skip(ExportSettings sett)
        {
            return false;
        }
        public abstract void AddTo(Quadtree<MvtExtension> container);
        protected abstract bool SaveMvtLayersInternal(ExportSettings sett, Layers layers);
    }

    public abstract class RectMvtExtension : MvtExtension
    {
        public Envelope Envelope { get { return envelope ?? (envelope = CalculateEnvelope()); } }

        private Envelope envelope;

        public RectMvtExtension(TsMapper mapper) : base(mapper)
        {
        }

        protected abstract Envelope CalculateEnvelope();

        public override bool Skip(ExportSettings sett)
        {
            return !Envelope.Intersects(sett.Envelope);
        }
        public override void AddTo(Quadtree<MvtExtension> container)
        {
            container.Insert(Envelope, this);
        }
        public override bool Discretizate(ExportSettings sett)
        {
            if (Skip(sett) || Envelope.Diameter < sett.DiscretizationThreshold) return false;
            return Envelope.Diameter / sett.Extent < sett.DiscretizationThreshold;
        }
    }

    public abstract class PointMvtExtension : MvtExtension
    {
        public Coordinate Coordinate { get { return coordinate ?? (coordinate = CalculateCoordinate()); } }

        private Quadtree<MvtExtension> elements;
        private Coordinate coordinate;

        public PointMvtExtension(TsMapper mapper) : base(mapper)
        {
        }

        protected abstract Coordinate CalculateCoordinate();


        public override bool Discretizate(ExportSettings sett)
        {
            if (Skip(sett)) return false;
            float outArea = (sett.DiscretizationThreshold * sett.Extent) / 2, inArea = sett.DiscretizationThreshold / 2;
            var outEnv = new Envelope(Coordinate.X - outArea, Coordinate.X + outArea, Coordinate.Y - outArea, Coordinate.Y + outArea);
            var inEnv = new Envelope(Coordinate.X - inArea, Coordinate.X + inArea, Coordinate.Y - inArea, Coordinate.Y + inArea);
            return elements.Query(outEnv).Where(x => x != this && outEnv.Contains(((PointMvtExtension)x).Coordinate) && !inEnv.Contains(((PointMvtExtension)x).Coordinate)).Count() > 1;
        }

        public override void AddTo(Quadtree<MvtExtension> elements)
        {
            this.elements = elements;
            elements.Insert(new Envelope(Coordinate), this);
        }
    }
}
