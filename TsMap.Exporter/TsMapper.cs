using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using NetTopologySuite.Index.Quadtree;
using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.Map.Overlays;

namespace TsMap.Exporter
{
    public class TsMapper : TsMap.TsMapper
    {
        public Quadtree<Polygon> Boundaries { get; private set; } = new();

        public TsMapper(string gameDir, List<Mod> mods) : base(gameDir, mods)
        {
        }

        protected virtual void ParseCountriesBounds()
        {
            var image = new Image<Rgb, Byte>(Backgrounds[0].GetBitmap().Width * 2, Backgrounds[0].GetBitmap().Height * 2);
            for (int i = 0; i < Backgrounds.Length; i++)
            {
                var part = Backgrounds[i].GetBitmap();
                var partLock = part.Lock();
                var emguImage = new Image<Rgba, Byte>(part.Width, part.Height);
                CvInvoke.MixChannels(new Image<Rgba, Byte>(part.Width, part.Height, 4, partLock.Data), emguImage, new int[] { 0, 0, 1, 1, 2, 2, 3, 3 });
                var finalImage = new Image<Rgb, Byte>(part.Width, part.Height);
                CvInvoke.CvtColor(emguImage.Add(emguImage.Split()[3].Not().ThresholdBinary(new Gray(127), new Gray(255)).Convert<Rgba, byte>()), finalImage, ColorConversion.Rgba2Rgb);

                image.ROI = new System.Drawing.Rectangle((i / 2) * part.Width, (i % 2) * part.Height, part.Width, part.Height);
                finalImage.CopyTo(image);
                image.ROI = System.Drawing.Rectangle.Empty;
            }

            var thr = image.Convert<Gray, Byte>().ThresholdBinaryInv(new Gray(127), new Gray(255));
            var labels = new Mat();
            int ll = CvInvoke.ConnectedComponents(thr, labels, LineType.FourConnected);
            var labelsImg = labels.ToImage<Gray, Byte>();
            for (int i = 1; i < ll; i++)
            {
                var v = new VectorOfVectorOfPoint();
                var hierarchy = new Mat();
                CvInvoke.FindContours(labelsImg.InRange(new Gray(i), new Gray(i)), v, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxTc89Kcos);
                for (int j = 0; j < v.Size; j++)
                {
                    var ring = new List<Coordinate>(v[j].ToArray().Select(p => new Coordinate(
                        BackgroundPos.X + (p.X / (float)image.Width) * BackgroundPos.Width,
                        BackgroundPos.Y + (p.Y / (float)image.Height) * BackgroundPos.Height
                    )));
                    ring.Add(ring[0]);
                    if (ring.Count >= 3)
                    {
                        var poly = new Polygon(new LinearRing(ring.ToArray()));
                        Boundaries.Insert(poly.EnvelopeInternal, poly);
                    }
                }
            }
            image.Dispose();
        }

        protected virtual void PopulateIndexes()
        {
        }

        public override void Parse()
        {
            base.Parse();

            PopulateIndexes();
            //ParseCountriesBounds();
        }

        public List<TsFerryConnection> GetFerryConnections()
        {
            return _ferryConnectionLookup;
        }
    }
}
