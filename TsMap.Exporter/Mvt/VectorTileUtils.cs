namespace TsMap.Exporter.Mvt
{
    internal enum MapboxCommandType
    {
        MoveTo = 1,
        LineTo = 2,
        ClosePath = 7
    }

    internal class VectorTileUtils
    {

        internal static uint GenerateCommandInteger(MapboxCommandType command, int count)
        {
            return (uint)(command & MapboxCommandType.ClosePath) | (uint)(count << 3);
        }

        internal static uint GenerateParameterInteger(int value)
        {
            return (uint)(value << 1 ^ value >> 31);
        }

        internal static uint[] GenerateDelta(uint x, uint y, ref uint cursorX, ref uint cursorY)
        {
            var dx = (int)(x - cursorX);
            var dy = (int)(y - cursorY);
            cursorX = x;
            cursorY = y;
            return new uint[] { GenerateParameterInteger(dx), GenerateParameterInteger(dy) };
        }

        internal static uint[] GenerateDelta(uint x, uint y)
        {
            var dx = (int)x;
            var dy = (int)y;
            return new uint[] { GenerateParameterInteger(dx), GenerateParameterInteger(dy) };
        }

        internal static uint[] GenerateDeltaFromGame(float x, float y, float west, float north, ref uint cursorX, ref uint cursorY)
        {
            return GenerateDelta((uint)(x - west), (uint)(y - north), ref cursorX, ref cursorY);
        }

        internal static uint[] GenerateDeltaFromGame(float x, float y, float west, float north)
        {
            return GenerateDelta((uint)(x - west), (uint)(y - north));
        }
    }

}
