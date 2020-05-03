using System.Collections.Generic;


namespace EGIS.Mapbox.Vector.Tile
{
    internal enum CommandId : System.UInt32
    {
        MoveTo = 1,
        LineTo = 2,
        ClosePath = 7
    }

    internal static class GeometryParser
    {


        public static List<List<Coordinate>> ParseGeometry(List<uint> geom, Tile.GeomType geomType)
        {
            
            int x = 0;
            int y = 0;
            var coordsList = new List<List<Coordinate>>();
            List<Coordinate> coords = null;
            var geometryCount = geom.Count;
            uint length = 0;
            uint command = 0;
            var i = 0;
            while (i < geometryCount)
            {
                if (length <= 0)
                {
                    length = geom[i++];
                    command = length & ((1 << 3) - 1);
                    length = length >> 3;
                }

                if (length > 0)
                {
                    if (command == (uint)CommandId.MoveTo)
                    {
                        coords = new List<Coordinate>();
                        coordsList.Add(coords);
                    }
                }

                if (command ==  (uint)CommandId.ClosePath)
                {
                    if (geomType != Tile.GeomType.Point && !(coords.Count == 0))
                    {
                        coords.Add(coords[0]);
                    }
                    length--;
                    continue;
                }

                var dx = geom[i++];
                var dy = geom[i++];

                length--;

                var ldx = ZigZag.Decode((int)dx);
                var ldy = ZigZag.Decode((int)dy);

                x = x + ldx;
                y = y + ldy;

                var  coord = new Coordinate() { X = x, Y = y };
                coords.Add(coord);
            }
            return coordsList;
        }


        public static void EncodeGeometry( List<List<Coordinate>> coordList, Tile.GeomType geomType, List<System.UInt32> geometry)
        {
            switch (geomType)
            {
                case Tile.GeomType.Point:
                    EncodePointGeometry(coordList, geometry);
                    break;
                case Tile.GeomType.LineString:
                    EncodeLineGeometry(coordList, geometry);
                    break;
                case Tile.GeomType.Polygon:
                    EncodePolygonGeometry(coordList, geometry);
                    break;
                default:
                    throw new System.Exception(string.Format("Unknown geometry type:{0}", geomType));
            }
        }


        private static void EncodePointGeometry(List<List<Coordinate>> coordList, List<System.UInt32> geometry)
        {
            Coordinate prevCoord = new Coordinate() { X = 0, Y = 0 };

            foreach (List<Coordinate> points in coordList)
            {
                if (points.Count == 0) throw new System.Exception(string.Format("unexpected point count encoding point geometry. Count is {0}", points.Count));

                System.UInt32 commandInteger = ((uint)CommandId.MoveTo & 0x7) | ((uint)points.Count << 3);
                geometry.Add(commandInteger);
                for(int n=0; n< points.Count;++n)
                {
                    int dx = points[n].X - prevCoord.X;
                    int dy = points[n].Y - prevCoord.Y;
                    int parameter = ZigZag.Encode(dx);
                    geometry.Add((System.UInt32)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((System.UInt32)parameter);
                    prevCoord = points[n];
                }
            }          
        }

        private static void EncodeLineGeometry(List<List<Coordinate>> coordList, List<System.UInt32> geometry)
        {
            Coordinate prevCoord = new Coordinate() { X = 0, Y = 0 };
            foreach (List<Coordinate> points in coordList)
            {
                if (points.Count == 0) throw new System.Exception(string.Format("unexpected point count encoding line geometry. Count is {0}", points.Count));

                //start of linestring
                System.UInt32 commandInteger = ((uint)CommandId.MoveTo & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                int dx = points[0].X - prevCoord.X;
                int dy = points[0].Y - prevCoord.Y;

                int parameter = ZigZag.Encode(dx);
                geometry.Add((System.UInt32)parameter);
                parameter = ZigZag.Encode(dy);
                geometry.Add((System.UInt32)parameter);

                //encode the rest of the points
                commandInteger = ((uint)CommandId.LineTo & 0x7) | ((uint)(points.Count-1) << 3);
                geometry.Add(commandInteger);
                for (int n = 1; n < points.Count; ++n)
                {
                    dx = points[n].X - points[n - 1].X;
                    dy = points[n].Y - points[n - 1].Y;
                    parameter = ZigZag.Encode(dx);
                    geometry.Add((System.UInt32)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((System.UInt32)parameter);
                }
                prevCoord = points[points.Count - 1];
            }            
        }


        private static void EncodePolygonGeometry(List<List<Coordinate>> coordList, List<System.UInt32> geometry)
        {
            Coordinate prevCoord = new Coordinate() { X = 0, Y = 0 };
            foreach (List<Coordinate> points in coordList)
            {
                if (points.Count == 0) throw new System.Exception(string.Format("unexpected point count encoding polygon geometry. Count is {0}", points.Count));

                //start of ring
                System.UInt32 commandInteger = ((uint)CommandId.MoveTo & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                int dx = points[0].X - prevCoord.X;
                int dy = points[0].Y - prevCoord.Y;

                int parameter = ZigZag.Encode(dx);
                geometry.Add((System.UInt32)parameter);
                parameter = ZigZag.Encode(dy);
                geometry.Add((System.UInt32)parameter);

                bool lastPointRepeated = (points[points.Count - 1].X == points[0].X && points[points.Count - 1].Y == points[0].Y);

                int pointCount = lastPointRepeated ? points.Count - 2 : points.Count - 1;

                //encode the rest of the points
                commandInteger = ((uint)CommandId.LineTo & 0x7) | ((uint)(pointCount) << 3);
                geometry.Add(commandInteger);
                for (int n = 1; n <= pointCount; ++n)
                {
                    dx = points[n].X - points[n - 1].X;
                    dy = points[n].Y - points[n - 1].Y;
                    parameter = ZigZag.Encode(dx);
                    geometry.Add((System.UInt32)parameter);
                    parameter = ZigZag.Encode(dy);
                    geometry.Add((System.UInt32)parameter);
                }                

                //close path
                commandInteger = ((uint)CommandId.ClosePath & 0x7) | (1 << 3);
                geometry.Add(commandInteger);

                prevCoord = points[pointCount];
            }

        }


    }
}
