using Emgu.CV.Structure;
using Emgu.CV;
using Eto.Drawing;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using TsMap.TsItem;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace TsMap.Exporter
{
    public class TsMapper : TsMap.TsMapper
    {
        public readonly KdTree<TsItem.TsItem> Index = new();

        public readonly List<List<PointF>> Boundaries = new();

        public TsMapper(string gameDir, List<Mod> mods) : base(gameDir, mods)
        {
        }

        protected virtual void ParseCountriesBounds()
        {
            var entireBackground = new Bitmap(Backgrounds[0].GetBitmap().Width * 2, Backgrounds[0].GetBitmap().Height * 2, PixelFormat.Format32bppRgba);
            using (var g = new Graphics(entireBackground))
            {
                g.DrawImage(Backgrounds[0].GetBitmap(), 0, 0);
                g.DrawImage(Backgrounds[1].GetBitmap(), 0, Backgrounds[0].GetBitmap().Height);
                g.DrawImage(Backgrounds[2].GetBitmap(), Backgrounds[0].GetBitmap().Width, 0);
                g.DrawImage(Backgrounds[3].GetBitmap(), Backgrounds[0].GetBitmap().Width, Backgrounds[0].GetBitmap().Height);
            }
            var tempPath = Path.GetTempFileName();
            entireBackground.Save(tempPath, ImageFormat.Png);
            Console.WriteLine(tempPath);

            var thr = (new Image<Rgba, Byte>(tempPath)).Convert<Gray, Byte>().Convert<Gray,Byte>().ThresholdBinary(new Gray(180), new Gray(255));
            var contours = new VectorOfVectorOfPoint();
            var hierarchy = new Mat();
            CvInvoke.FindContours(thr, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);

            Boundaries = contours.ToArrayOfArray().Select(c => c.Select(p => new PointF(p.X, p.Y)).ToList()).ToList();



        }

        protected virtual void PopulateIndex()
        {
            foreach (TsItem.TsItem item in MapItems.Values)
                Index.Insert(new Coordinate(item.X, item.Z), item);
        }

        public override void Parse()
        {
            base.Parse();

            ParseCountriesBounds();
            PopulateIndex();
        }
    }
}
