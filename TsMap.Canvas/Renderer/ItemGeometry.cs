using Eto.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TsMap.TsItem;

namespace TsMap.Canvas.Renderer
{
    public abstract class ItemGeometry
    {
        public TsItem.TsItem Item { get; private set; }
        public List<PointF> Points { get; private set; }

        protected ItemGeometry(TsItem.TsItem item)
        {
            Item = item;
            Points = new List<PointF>();
        }

        protected ItemGeometry(TsItem.TsItem item, List<PointF> points)
        {
            Item = item;
            Points = points;
        }

        public void AddPoint(PointF p)
        {
            Points.Add(p);
        }

        public bool HasPoints()
        {
            return Points.Count > 0;
        }

        public PointF[] GetPoints()
        {
            return Points.ToArray();
        }

    }

    public class RoadGeometry : ItemGeometry
    {
        private static Dictionary<TsRoadItem, RoadGeometry> geoms = new();

        protected RoadGeometry(TsRoadItem item) : base(item)
        {
        }
        public static RoadGeometry GetGeometry(TsRoadItem item)
        {
            if (!geoms.ContainsKey(item))
            {
                geoms[item] = new RoadGeometry(item);
            }
            return geoms[item];
        }
    }

    public abstract class PrefabGeometry : ItemGeometry
    {
        private static Dictionary<TsItem.TsItem, List<PrefabGeometry>> geoms = new();

        public int ZIndex { get; set; }
        public Brush Color { get; set; }

        protected PrefabGeometry(TsItem.TsItem item) : base(item)
        {
            AddGeometry(this);
        }

        protected PrefabGeometry(TsItem.TsItem item, List<PointF> points) : base(item, points)
        {
            AddGeometry(this);
        }

        private void AddGeometry(PrefabGeometry geom)
        {
            var item = geom.Item;
            if (!geoms.ContainsKey(item))
            {
                geoms[item] = new List<PrefabGeometry>();
            }
            geoms[item].Add(this);
        }

        public static IEnumerable<PrefabGeometry> GetGeometries(TsItem.TsItem item)
        {
            if (!geoms.ContainsKey(item)) return Enumerable.Empty<PrefabGeometry>();
            return geoms[item]?.OrderBy(x => x.ZIndex);
        }

        public abstract void Draw(Graphics g);
    }

    public class RoadPrefabGeometry : PrefabGeometry
    {
        public float Width { private get; set; }

        public RoadPrefabGeometry(TsPrefabItem item) : base(item)
        {
            ZIndex = 1;
        }

        public override void Draw(Graphics g)
        {
            g.DrawLines(new Pen(Color, Width), Points.ToArray());
        }
    }

    public class PolyPrefabGeometry : PrefabGeometry
    {
        public PolyPrefabGeometry(TsPrefabItem item, List<PointF> points) : base(item, points)
        {
        }

        public override void Draw(Graphics g)
        {
            g.FillPolygon(Color, Points.ToArray());
        }
    }


    public class PolyAreaGeometry : PrefabGeometry
    {
        public PolyAreaGeometry(TsMapAreaItem item, List<PointF> points) : base(item, points)
        {
        }

        public override void Draw(Graphics g)
        {
            g.FillPolygon(Color, Points.ToArray());
        }
    }
}
