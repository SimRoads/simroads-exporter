using NetTopologySuite.Algorithm.Hull;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using TsMap.Common;
using TsMap.Exporter.Overlays;

namespace TsMap.Exporter
{
    public class TsMapper : TsMap.TsMapper
    {
        public Dictionary<TsCountry, List<Polygon>> Boundaries { get; private set; } = new();
        public KdTree<TsNode> NodesIndex { get; private set; } = new();

        private class CC
        {
            public HashSet<Tuple<short, short>> Borders = new();
            private short MinY, MaxY, MinX, MaxX;
            private List<CC> childs = new();
            private CC parent;

            public CC()
            {
                parent = this;
                MinY = short.MaxValue;
                MaxY = short.MinValue;
                MinX = short.MaxValue;
                MaxX = short.MinValue;
            }

            public bool IsRoot() => parent == this;

            public CC AddRange(CC ccLine)
            {
                CC p1 = parent, p2 = ccLine.parent;
                if (p1 == p2) return p1;
                if (p1.Borders.Count < p2.Borders.Count) (p1, p2) = (p2, p1);
                p1.Borders.UnionWith(p2.Borders);
                foreach (var ccs in p2.childs)
                {
                    ccs.parent = p1;
                    p1.childs.Add(ccs);
                }
                p2.parent = p1;
                p1.childs.Add(p2);
                p1.MinY = Math.Min(p1.MinY, p2.MinY);
                p1.MaxY = Math.Max(p1.MaxY, p2.MaxY);
                p1.MinX = Math.Min(p1.MinX, p2.MinX);
                p1.MaxX = Math.Max(p1.MaxX, p2.MaxX);
                return p1;
            }

            public void Add(int xPos, int yPos, Image<Rgba32> im)
            {
                short x = (short)xPos, y = (short)yPos;
                if ((x > 0 && im[x - 1, y].R == 255) ||
                    (y > 0 && im[x, y - 1].R == 255) ||
                    (y < im.Height - 1 && im[x, y + 1].R == 255) ||
                    (x < im.Width - 1 && im[x + 1, y].R == 255)) parent.Borders.Add(new(x, y));
                if ((x == im.Width - 1 || x == 0 || y == 0 || y == im.Height - 1))
                {
                    parent.Borders.Add(new(x, y));
                }
                MinY = Math.Min(MinY, y);
                MaxY = Math.Max(MaxY, y);
                MinX = Math.Min(MinX, x);
                MaxX = Math.Max(MaxX, x);
            }

            public Polygon GetPolygon()
            {
                if (MaxX - MinX > 1 && MaxY - MinY > 1)
                {
                    // Find concave hull
                    var points = new MultiPoint(parent.Borders.Select(p => new NetTopologySuite.Geometries.Point(p.Item1, p.Item2)).ToArray());
                    return (Polygon)ConcaveHull.ConcaveHullByLength(points, 2);
                }
                return null;
            }

        }

        public TsMapper(string gameDir, List<Mod> mods) : base(gameDir, mods)
        {
        }

        protected virtual void ParseCountriesBounds()
        {
            var image = new Image<Rgba32>((int)Backgrounds[0].Width * 2, (int)Backgrounds[0].Height * 2);
            for (int i = 0; i < Backgrounds.Length; i++)
            {
                image.Mutate(x => x.DrawImage(
                    Backgrounds[i].GetImage(),
                    new SixLabors.ImageSharp.Point((int)Backgrounds[i].Width * (i / 2), (int)Backgrounds[i].Height * (i % 2)), 1)
                );
            }
            image.Mutate(im => im.BackgroundColor(Color.White).BinaryThreshold(0.5f, BinaryThresholdMode.Luminance));
            //Find connected components
            CC[] ccLine = new CC[image.Width];
            List<CC> ccs = new();

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image[x, y].R == 255)
                    {
                        ccLine[x] = null;
                    }
                    else
                    {
                        if (x > 0 && ccLine[x] != null && ccLine[x - 1] != null)
                        {
                            ccLine[x] = ccLine[x].AddRange(ccLine[x - 1]);
                        }
                        else if (x > 0 && ccLine[x] == null && ccLine[x - 1] != null)
                        {
                            ccLine[x] = ccLine[x - 1];
                        }
                        else if (ccLine[x] == null)
                        {
                            ccLine[x] = new CC();
                            ccs.Add(ccLine[x]);
                        }
                        ccLine[x].Add(x, y, image);
                    }
                }
            }

            foreach (var cc in ccs)
            {
                if (cc.IsRoot())
                {
                    var polyImage = cc.GetPolygon();
                    if (polyImage != null)
                    {
                        var poly = new Polygon(new LinearRing(polyImage.ExteriorRing.Coordinates.Select(p => new Coordinate(
                            BackgroundPos.X + (p.X / (float)image.Width) * BackgroundPos.Width,
                            BackgroundPos.Y + (p.Y / (float)image.Height) * BackgroundPos.Height
                        )).ToArray()));
                        var country = NodesIndex.NearestNeighbor(poly.Centroid.Coordinate).Data.GetCountry();
                        if (!Boundaries.ContainsKey(country)) Boundaries.Add(country, new List<Polygon>());
                        Boundaries[country].Add(poly);
                    }
                }
            }

        }

        protected virtual void PopulateIndexes()
        {
            foreach (var node in Nodes.Values)
            {
                if (node.GetCountry() != null) NodesIndex.Insert(new Coordinate(node.X, node.Z), node);
            }
        }

        public override void Parse()
        {
            base.Parse();

            PopulateIndexes();
            ParseCountriesBounds();
        }

        public override List<DlcGuard> GetDlcGuardsForCurrentGame()
        {
            return Environment.GetEnvironmentVariable("DLC_GUARDS")?.Split(";").Select(x =>
            {
                var dlc_data = x.Split(",");
                return new DlcGuard(dlc_data[0], (byte)int.Parse(dlc_data[1]), dlc_data.Length > 2 ? bool.Parse(dlc_data[2]) : true);
            }).ToList() ?? base.GetDlcGuardsForCurrentGame();
        }

        public IEnumerable<TsCountry> GetCountries()
        {
            return _countriesLookup.Values;
        }

        public IEnumerable<TsCity> GetCities()
        {
            return _citiesLookup.Values;
        }
    }
}
