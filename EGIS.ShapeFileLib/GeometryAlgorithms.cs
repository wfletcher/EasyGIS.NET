using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Collection of 2D Geometry algorithms
    /// </summary>
    public class GeometryAlgorithms
    {
        private GeometryAlgorithms() { }


        #region Polygon Algorithms

        /// <summary>
        /// Tests whether point is inside a polygon
        /// </summary>
        /// <param name="points"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="ignoreHoles"></param>
        /// <param name="isHole"></param>
        /// <returns></returns>
        public static bool PointInPolygon(PointD[] points, double x, double y, bool ignoreHoles, ref bool isHole)
        {
            if (ignoreHoles) return PointInPolygon(points, x, y);

            //if we are detecting holes then we need to calculate the area
            double area = 0;
            //latitude = y
            int j = points.Length - 1;
            bool inPoly = false;

            for (int i = 0; i < points.Length; ++i)
            {
                if (points[i].X < x && points[j].X >= x || points[j].X < x && points[i].X >= x)
                {
                    if (points[i].Y + (x - points[i].X) / (points[j].X - points[i].X) * (points[j].Y - points[i].Y) < y)
                    {
                        inPoly = !inPoly;
                    }
                }

                area += (points[j].X * points[i].Y - points[i].X * points[j].Y);

                j = i;
            }
            area *= 0.5;
            //Console.Out.WriteLine("area = " + area);
            isHole = area > 0;
            return inPoly;// && isHole;             
        }

        public static bool PointInPolygon(PointD[] points, double x, double y)
        {
            int j = points.Length - 1;
            bool inPoly = false;

            for (int i = 0; i < points.Length; ++i)
            {
                if (points[i].X < x && points[j].X >= x || points[j].X < x && points[i].X >= x)
                {
                    if (points[i].Y + (x - points[i].X) / (points[j].X - points[i].X) * (points[j].Y - points[i].Y) < y)
                    {
                        inPoly = !inPoly;
                    }
                }
                j = i;
            }

            return inPoly;
        }

        public static unsafe bool PointInPolygon(byte[] data, int offset, int numPoints, double x, double y, bool ignoreHoles, ref bool isHole)
        {
            if (ignoreHoles) return PointInPolygon(data, offset, numPoints, x, y);

            //if we are detecting holes then we need to calculate the area
            double area = 0;
            int j = numPoints - 1;
            bool inPoly = false;

            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints; ++i)
                {
                    if (points[i].X < x && points[j].X >= x || points[j].X < x && points[i].X >= x)
                    {
                        if (points[i].Y + (x - points[i].X) / (points[j].X - points[i].X) * (points[j].Y - points[i].Y) < y)
                        {
                            inPoly = !inPoly;
                        }
                    }
                    area += (points[j].X * points[i].Y - points[i].X * points[j].Y);
                    j = i;
                }
            }
            area *= 0.5;
            isHole = area > 0;
            return inPoly;
        }

        public static unsafe bool PointInPolygon(byte[] data, int offset, int numPoints, double x, double y)
        {
            int j = numPoints - 1;
            bool inPoly = false;
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints; ++i)
                {
                    if (points[i].X < x && points[j].X >= x || points[j].X < x && points[i].X >= x)
                    {
                        if (points[i].Y + (x - points[i].X) / (points[j].X - points[i].X) * (points[j].Y - points[i].Y) < y)
                        {
                            inPoly = !inPoly;
                        }
                    }
                    j = i;
                }
            }
            return inPoly;
        }

        public static unsafe bool IsPolygonHole(byte[] data, int offset, int numPoints)
        {
            //if we are detecting holes then we need to calculate the area
            double area = 0;
            int j = numPoints - 1;
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints; ++i)
                {
                    area += (points[j].X * points[i].Y - points[i].X * points[j].Y);
                    j = i;
                }
            }
            return area > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="numPoints"></param>6
        /// +
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <param name="ignoreHoles"></param>
        /// <returns></returns>
        /// <remarks>Not tested</remarks>
        public static unsafe bool PolygonCircleIntersects(byte[] data, int offset, int numPoints, PointD centre, double radius, bool ignoreHoles)
        {
            //test 1 : check if polygon intersects or is inside the circle
            //test the dist from each polygon edge to circle centre. If < radius then intersects
            int j = numPoints - 1;
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints; ++i)
                {
                    //could optimize further by working with Distance Squared, but for the moment use the 
                    //distance
                    if (LineSegPointDist(ref points[i], ref points[j], ref centre) <= radius) return true;                    
                    j = i;
                }
            }

            //test 2 : check if the circle is inside the polygon
            if(ignoreHoles) return PointInPolygon(data, offset, numPoints, centre.X, centre.Y);   
            //if a polygon is a hole then it doesn't intersect
            bool isHole = false;
            if (PointInPolygon(data, offset, numPoints, centre.X, centre.Y, false, ref isHole)) return !isHole;
            return false;
        }


        public static unsafe bool PolygonCircleIntersects(byte[] data, int offset, int numPoints, PointD centre, double radius)
        {
            return PolygonCircleIntersects(data, offset, numPoints, centre, radius, true);    
        }


        #endregion


        #region Line Segment Algorithms



        public static unsafe bool PointOnPolyline(byte[] data, int offset, int numPoints, PointD pt, double minDist)
        {
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);

                for (int i = 0; i < numPoints - 1; i++)
                {
                    if (LineSegPointDist(ref points[i], ref points[i + 1], ref pt) <= minDist)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //compute the dot product AB*BC         
        public static double Dot(ref PointD a, ref PointD b, ref PointD c)
        {
            PointD ab = new PointD(b.X - a.X, b.Y - a.Y);
            PointD bc = new PointD(c.X - b.X, c.Y - b.Y);
            return (ab.X * bc.X) + (ab.Y * bc.Y);
        }

        //Compute the cross product AB x AC
        public static double Cross(ref PointD a, ref PointD b, ref PointD c)
        {
            PointD ab = new PointD(b.X - a.X, b.Y - a.Y);
            PointD ac = new PointD(c.X - a.X, c.Y - a.Y);
            return (ab.X * ac.Y) - (ab.Y * ac.X);
        }

        public static double Distance(ref PointD a, ref PointD b)
        {
            double d1 = a.X - b.X;
            double d2 = a.Y - b.Y;
            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }

        //Compute the distance from segment AB to C
        public static double LineSegPointDist(ref PointD a, ref PointD b, ref PointD c)
        {
            //float dist = cross(a,b,c) / distance(a,b);

            if (Dot(ref a, ref b, ref c) > 0)
            {
                return Distance(ref b, ref c);
            }
            if (Dot(ref b, ref a, ref c) > 0)
            {
                return Distance(ref a, ref c);
            }
            return Math.Abs(Cross(ref a, ref b, ref c) / Distance(ref a, ref b));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="numPoints"></param>
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        /// <remarks>Not tested</remarks>
        public static unsafe bool PolylineCircleIntersects(byte[] data, int offset, int numPoints, PointD centre, double radius)
        {
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);

                for (int i = 0; i < numPoints - 1; i++)
                {
                    if (LineSegPointDist(ref points[i], ref points[i + 1], ref centre) <= radius)
                    {
                        return true;
                    }
                }
            }
            return false;

        }
        

        #endregion

        /// <summary>        
        /// </summary>
        /// <param name="r"></param>
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        /// <remarks>This method is untested</remarks>
        public static bool RectangleCircleIntersects(ref RectangleD r, ref PointD centre, double radius)
        {
            //following code obtained from http://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection

            // clamp(value, min, max) - limits value to the range min..max

            // Find the closest point to the circle within the rectangle
            //float closestX = clamp(circle.X, rectangle.Left, rectangle.Right);
            //float closestY = clamp(circle.Y, rectangle.Top, rectangle.Bottom);

            //// Calculate the distance between the circle's center and this closest point
            //float distanceX = circle.X - closestX;
            //float distanceY = circle.Y - closestY;

            //// If the distance is less than the circle's radius, an intersection occurs
            //float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
            //return distanceSquared < (circle.Radius * circle.Radius);
            
            double closestX = Math.Max(Math.Min(centre.X, r.Right), r.Left);
            double closestY = Math.Max(Math.Min(centre.Y, r.Bottom), r.Top);

            // Calculate the distance between the circle's center and this closest point
            double distanceX = centre.X - closestX;
            double distanceY = centre.Y - closestY;

            // If the distance is less than the circle's radius, an intersection occurs
            return ((distanceX * distanceX) + (distanceY * distanceY)) <= (radius*radius);
            


        }


        public static bool RectangleCircleIntersects(ref System.Drawing.RectangleF r, ref PointD centre, double radius)
        {
            //following code obtained from http://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection

            // clamp(value, min, max) - limits value to the range min..max

            // Find the closest point to the circle within the rectangle
            //float closestX = clamp(circle.X, rectangle.Left, rectangle.Right);
            //float closestY = clamp(circle.Y, rectangle.Top, rectangle.Bottom);

            //// Calculate the distance between the circle's center and this closest point
            //float distanceX = circle.X - closestX;
            //float distanceY = circle.Y - closestY;

            //// If the distance is less than the circle's radius, an intersection occurs
            //float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
            //return distanceSquared < (circle.Radius * circle.Radius);

            double closestX = Math.Max(Math.Min(centre.X, r.Right), r.Left);
            double closestY = Math.Max(Math.Min(centre.Y, r.Bottom), r.Top);

            // Calculate the distance between the circle's center and this closest point
            double distanceX = centre.X - closestX;
            double distanceY = centre.Y - closestY;

            // If the distance is less than the circle's radius, an intersection occurs
            return ((distanceX * distanceX) + (distanceY * distanceY)) <= (radius * radius);



        }

        
    }

}
