using Eto.Drawing;
using sd = System.Drawing;

// https://github.com/picoe/Eto/blob/d7ad8feb94b341a8d01861cfd75fc1e30315a119/src/Eto.WinForms/WinConversions.shared.cs

namespace TsMap.Canvas.Renderer
{
    #if LINUX
    public static class EtoConversions
    {
        public static Point ToEto(this sd.Point point)
        {
            return new Point(point.X, point.Y);
        }

        public static PointF ToEto(this sd.PointF point)
        {
            return new PointF(point.X, point.Y);
        }

        public static sd.PointF ToSD(this PointF point)
        {
            return new sd.PointF(point.X, point.Y);
        }

        public static sd.Point ToSDPoint(this PointF point)
        {
            return new sd.Point((int)point.X, (int)point.Y);
        }

        public static Size ToEto(this sd.Size size)
        {
            return new Size(size.Width, size.Height);
        }

        public static sd.Size ToSD(this Size size)
        {
            return new sd.Size(size.Width, size.Height);
        }

        public static Size ToEtoF(this sd.SizeF size)
        {
            return new Size((int)size.Width, (int)size.Height);
        }

        public static SizeF ToEto(this sd.SizeF size)
        {
            return new SizeF(size.Width, size.Height);
        }

        public static sd.SizeF ToSD(this SizeF size)
        {
            return new sd.SizeF(size.Width, size.Height);
        }

        public static Rectangle ToEto(this sd.Rectangle rect)
        {
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static RectangleF ToEto(this sd.RectangleF rect)
        {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static sd.Rectangle ToSD(this Rectangle rect)
        {
            return new sd.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static sd.RectangleF ToSD(this RectangleF rect)
        {
            return new sd.RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static sd.Rectangle ToSDRectangle(this RectangleF rect)
        {
            return new sd.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }

        internal static sd.Point[] ToSD(this Point[] points)
        {
            var result =
                new sd.Point[points.Length];

            for (var i = 0;
                i < points.Length;
                ++i)
            {
                var p = points[i];
                result[i] =
                    new sd.Point(p.X, p.Y);
            }

            return result;
        }

        internal static sd.PointF[] ToSD(this PointF[] points)
        {
            var result =
                new sd.PointF[points.Length];

            for (var i = 0;
                i < points.Length;
                ++i)
            {
                var p = points[i];
                result[i] =
                    new sd.PointF(p.X, p.Y);
            }

            return result;
        }

        internal static PointF[] ToEto(this sd.PointF[] points)
        {
            var result =
                new PointF[points.Length];

            for (var i = 0;
                i < points.Length;
                ++i)
            {
                var p = points[i];
                result[i] =
                    new PointF(p.X, p.Y);
            }

            return result;
        }
    }
    #endif
}