using System;

namespace EGIS.Mapbox.Vector.Tile
{
    /// <summary>
    /// Uitility class to perform ZigZag encoding and decoding
    /// </summary>
    public static class ZigZag
    {
        public static Int32 Decode(Int32 n)
        {
            return (n >> 1) ^ (-(n & 1));
        }

        public static Int32 Encode(Int32 n)
        {
            return (n << 1) ^ (n >> 31);
        }

   }
}
