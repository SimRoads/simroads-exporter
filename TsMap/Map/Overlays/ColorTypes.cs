namespace TsMap.Map.Overlays
{
    public struct Color8888
    {
        public byte A { get; private set; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public Color8888(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public Color8888(Color565 color565, byte a)
        {
            A = a;
            R = (byte)(color565.R << 3);
            G = (byte)(color565.G << 2);
            B = (byte)(color565.B << 3);
        }

        public void SetAlpha(byte a)
        {
            A = a;
        }
    }

    public struct Color565
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        private Color565(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public Color565(ushort color) : this((byte)((color >> 11) & 0x1F), (byte)((color >> 5) & 0x3F),
            (byte)(color & 0x1F))
        {
        }

        public static Color565 operator +(Color565 col1, Color565 col2)
        {
            return new Color565((byte)(col1.R + col2.R), (byte)(col1.G + col2.G), (byte)(col1.B + col2.B));
        }

        public static Color565 operator *(Color565 col1, double val)
        {
            return new Color565((byte)(col1.R * val), (byte)(col1.G * val), (byte)(col1.B * val));
        }

        public static Color565 operator *(double val, Color565 col1)
        {
            return col1 * val;
        }
    }
}