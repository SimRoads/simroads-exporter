using System;
using System.Drawing;

namespace TsMap.Helpers
{
    public static class RenderHelper
    {
        public static PointF RotatePoint(float x, float z, float angle, float rotX, float rotZ)
        {
            var s = Math.Sin(angle);
            var c = Math.Cos(angle);
            double newX = x - rotX;
            double newZ = z - rotZ;
            return new PointF((float)((newX * c) - (newZ * s) + rotX), (float)((newX * s) + (newZ * c) + rotZ));
        }

        public static PointF GetCornerCoords(float x, float z, float width, double angle)
        {
            return new PointF(
                (float)(x + width * Math.Cos(angle)),
                (float)(z + width * Math.Sin(angle))
            );
        }
        public static double Hypotenuse(float x, float y)
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        // https://stackoverflow.com/a/45881662
        public static Tuple<PointF, PointF> GetBezierControlNodes(float startX, float startZ, double startRot, float endX, float endZ, double endRot)
        {
            var len = Hypotenuse(endX - startX, endZ - startZ);
            var ax1 = (float)(Math.Cos(startRot) * len * (1 / 3f));
            var az1 = (float)(Math.Sin(startRot) * len * (1 / 3f));
            var ax2 = (float)(Math.Cos(endRot) * len * (1 / 3f));
            var az2 = (float)(Math.Sin(endRot) * len * (1 / 3f));
            return new Tuple<PointF, PointF>(new PointF(ax1, az1), new PointF(ax2, az2));
        }

        public static int GetZoomIndex(Rectangle clip, float scale)
        {
            var smallestSize = (clip.Width > clip.Height) ? clip.Height / scale : clip.Width / scale;
            if (smallestSize < 1000) return 0;
            if (smallestSize < 5000) return 1;
            if (smallestSize < 18500) return 2;
            return 3;
        }
    }
}
