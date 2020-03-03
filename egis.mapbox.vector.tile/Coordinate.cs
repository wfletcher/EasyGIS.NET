using System;

namespace EGIS.Mapbox.Vector.Tile
{
    public struct Coordinate
    {
        public Int32 X;
        public Int32 Y;

        public Coordinate(Int32 x, Int32 y)
        {
            X = x;
            Y = y;
        }
    }
}
