using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
        /// determines if a polygon is a "hole"
        /// </summary>
        /// <param name="points"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// Vertices of a rings defining holes in polygons are in a counterclockwise direction. 
        /// </para>
        /// </remarks>
        public static unsafe bool IsPolygonHole(IList<PointD> points, int numPoints)
        {
            //if we are detecting holes then we need to calculate the area
            double area = 0;
            int j = numPoints - 1;
            for (int i = 0; i < numPoints; ++i)
            {
                area += (points[j].X * points[i].Y - points[i].X * points[j].Y);
                j = i;
            }
           
            return area > 0;
        }

        internal static unsafe bool IsPolygonHole(IList<System.Drawing.Point> points, int numPoints)
        {
            //if we are detecting holes then we need to calculate the area
            double area = 0;
            int j = numPoints - 1;
            for (int i = 0; i < numPoints; ++i)
            {
                area += (points[j].X * points[i].Y - points[i].X * points[j].Y);
                j = i;
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
        public static unsafe bool PolygonCircleIntersects(byte[] data, int offset, int numPoints, PointD centre, double radius, bool ignoreHoles, out bool withinHole)
        {
            withinHole = false;
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
            if (PointInPolygon(data, offset, numPoints, centre.X, centre.Y, false, ref isHole))
            {
                withinHole = isHole;
                return !isHole;
            }
            return false;
        }


        public static unsafe bool PolygonCircleIntersects(byte[] data, int offset, int numPoints, PointD centre, double radius)
        {
            bool withinHole;
            return PolygonCircleIntersects(data, offset, numPoints, centre, radius, true, out withinHole);    
        }


        public static unsafe double DistanceToPolygon(byte[] data, int offset, int numPoints, PointD point, bool ignoreHoles)
        {
            //test 1A : check if the point is inside the polygon
            if (ignoreHoles)
            {
                if (PointInPolygon(data, offset, numPoints, point.X, point.Y))
                {
                    return 0;
                }
            }
            else
            {
                //test 1B: check if if point is in polygon checking for holes
                bool isHole = false;
                if (PointInPolygon(data, offset, numPoints, point.X, point.Y, false, ref isHole))
                {
                    if (!isHole) return 0;
                }
            }

            //test 2 : check distance each polygon edge to point
            double minDistance = double.PositiveInfinity;
            int j = numPoints - 1;
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints; ++i)
                {
                    //could optimize further by working with Distance Squared, but for the moment use the 
                    //distance
                    double segmentDistance = LineSegPointDist(ref points[i], ref points[j], ref point);
                    if(segmentDistance < minDistance)
                    {
                        minDistance = segmentDistance;
                    }
                    j = i;
                }
            }
            return minDistance;
        }

        public static unsafe double DistanceToPolygon(byte[] data, int offset, int numPoints, PointD point)
        {
            return DistanceToPolygon(data, offset, numPoints, point, true);            
        }

        public static double DistanceToPolygon(PointD[] points, PointD point, bool ignoreHoles)
        {
            //test 1A : check if the point is inside the polygon
            if (ignoreHoles)
            {               
                if (PointInPolygon(points, point.X, point.Y))
                {
                    return 0;
                }
            }
            else
            {
                //test 1B: check if if point is in polygon checking for holes
                bool isHole = false;
                if (PointInPolygon(points, point.X, point.Y, false, ref isHole))
                {
                    if (!isHole) return 0;
                }
            }

            //test 2 : check distance each polygon edge to point
            double minDistance = double.PositiveInfinity;
            int numPoints = points.Length;
            int j = numPoints-1;
            for (int i = 0; i < numPoints; ++i)
            {
                //could optimize further by working with Distance Squared, but for the moment use the 
                //distance
                double segmentDistance = LineSegPointDist(ref points[i], ref points[j], ref point);
                if (segmentDistance < minDistance)
                {
                    minDistance = segmentDistance;
                }
                j = i;
            }            
            return minDistance;
        }


        public static bool PolygonPolygonIntersect(PointD[] points1, int points1Count, PointD[] points2, int points2Count)
        {
            return NativeMethods.PolygonPolygonIntersect(points1, points1Count, points2, points2Count);
        }

        public static bool PolyLinePolygonIntersect(PointD[] polyLinePoints, int polyLinePointsCount, PointD[] polygonPoints, int polygonPointsCount)
        {
            return NativeMethods.PolyLinePolygonIntersect(polyLinePoints, polyLinePointsCount, polygonPoints, polygonPointsCount);
        }

        /// <summary>
        /// returns true if polygon1 "touches" polygon 2
        /// </summary>
        /// <param name="polygonPoints1"></param>
        /// <param name="polygonPoints1Count"></param>
        /// <param name="polygonPoints2"></param>
        /// <param name="polygonPoints2Count"></param>
        /// <returns>
        /// This method can be used to find neighbouring polygons
        /// </returns>
        public static bool PolygonTouchesPolygon(PointD[] polygonPoints1, int polygonPoints1Count, PointD[] polygonPoints2, int polygonPoints2Count)
        {
            return NativeMethods.PolygonTouchesPolygon(polygonPoints1, polygonPoints1Count, polygonPoints2, polygonPoints2Count);
        }
        

        #endregion


        #region Line Algorithms



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

        public static unsafe bool PointOnPolyline(PointD[] points, int offset, int numPoints, PointD pt, double minDist)
        {            
            for (int i = offset; i < numPoints - 1; i++)
            {
                if (LineSegPointDist(ref points[i], ref points[i + 1], ref pt) <= minDist)
                {
                    return true;
                }
            }
            return false;
        }

        public static double ClosestPointOnPolyline(PointD[] points, int offset, int numPoints, PointD pt)
        {
            double closestDistance = double.PositiveInfinity;
            int end = offset + numPoints - 1;
            for (int i = offset; i < end; i++)
            {
                double segmentDistance = LineSegPointDist(ref points[i], ref points[i + 1], ref pt);
                if (segmentDistance < closestDistance) closestDistance = segmentDistance;
            }
            return closestDistance;
        }

        public static unsafe double ClosestPointOnPolyline(byte[] data, int offset, int numPoints, PointD pt)
        {
            double closestDistance = double.PositiveInfinity;            
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);

                for (int i = 0; i < numPoints - 1; i++)
                {
                    double segmentDistance = LineSegPointDist(ref points[i], ref points[i + 1], ref pt);
                    if (segmentDistance < closestDistance) closestDistance = segmentDistance;
                }
            }
            return closestDistance;
        }

        public static unsafe void ClosestPointOnPolyline(PointD[] points, int offset, int numPoints, PointD pt, out PointD closestPoint, out double distance, out int segmentIndex, out double tVal)
        {
            distance = double.PositiveInfinity;
            tVal = double.PositiveInfinity;
            closestPoint = PointD.Empty;
            segmentIndex = offset;

            //for (int i = offset; i < numPoints - 1; i++)
            int end = offset + numPoints - 1;
            for (int i = offset; i < end; i++)
            {
                double t;
                PointD segmentPoint;
                double segmentDistance = LineSegPointDist(ref points[i], ref points[i + 1], ref pt, out t, out segmentPoint);
                if(segmentDistance < distance)
                {
                    distance = segmentDistance;
                    tVal = t;
                    closestPoint = segmentPoint;                    
                    segmentIndex = i;                    
                }
            }            
        }

        public static unsafe double ClosestPointOnPolyline(PointD[] points, int offset, int numPoints, PointD pt, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            
            //for (int i = offset; i < numPoints - 1; i++)
            int end = offset + numPoints - 1;
            for (int i = offset; i < end; i++)
            {
                double segmentDistance = LineSegPointDist(ref points[i], ref points[i + 1], ref pt, ref polylineDistanceInfo);
                if (segmentDistance < polylineDistanceInfo.Distance)
                {
                    polylineDistanceInfo.Distance = segmentDistance;
                    polylineDistanceInfo.PointIndex = i;
                }
            }
            return polylineDistanceInfo.Distance;
        }

        public static unsafe void ClosestPointOnPolyline(byte[] data, int offset, int numPoints, PointD pt, out PointD closestPoint, out double distance, out int segmentIndex, out double tVal)
        {
            distance = double.PositiveInfinity;
            tVal = double.PositiveInfinity;
            closestPoint = PointD.Empty;
            segmentIndex = offset;

            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints - 1; i++)
                {
                    double t;
                    PointD segmentPoint;
                    double segmentDistance = LineSegPointDist(ref points[i], ref points[i + 1], ref pt, out t, out segmentPoint);
                    if (segmentDistance < distance)
                    {
                        distance = segmentDistance;
                        tVal = t;
                        closestPoint = segmentPoint;
                        segmentIndex = i;
                    }
                }
            }
        }

        public static unsafe double ClosestPointOnPolyline(byte[] data, int offset, int numPoints, PointD pt, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            fixed (byte* bPtr = data)
            {
                PointD* points = (PointD*)(bPtr + offset);
                for (int i = 0; i < numPoints - 1; i++)
                {
                    double segmentDistance = LineSegPointDist(ref points[i], ref points[i + 1], ref pt, ref polylineDistanceInfo);
                    if (segmentDistance < polylineDistanceInfo.Distance)
                    {
                        polylineDistanceInfo.Distance = segmentDistance;
                        polylineDistanceInfo.PointIndex = i;                        
                    }
                }
            }
            return polylineDistanceInfo.Distance;
        }

        
          
        /// <summary>
        /// Computes the dot product AB*BC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double Dot(ref PointD a, ref PointD b, ref PointD c)
        {
            PointD ab = new PointD(b.X - a.X, b.Y - a.Y);
            PointD bc = new PointD(c.X - b.X, c.Y - b.Y);
            return (ab.X * bc.X) + (ab.Y * bc.Y);
        }

        
        /// <summary>
        /// Computes the cross product AB x AC 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double Cross(ref PointD a, ref PointD b, ref PointD c)
        {
            PointD ab = new PointD(b.X - a.X, b.Y - a.Y);
            PointD ac = new PointD(c.X - a.X, c.Y - a.Y);
            return (ab.X * ac.Y) - (ab.Y * ac.X);
        }

        /// <summary>
        /// returns the Euclidean distance between two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(ref PointD a, ref PointD b)
        {
            double d1 = a.X - b.X;
            double d2 = a.Y - b.Y;
            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }

        /// <summary>
        /// return counter-clockwise vector perpindicular to v
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static PointD PerpindicularVectorCCW(ref PointD v)
        {
            return new PointD(-v.Y, v.X);  //counter cw
            //return new PointD(v.Y, -v.X);  //cw
        }
        
        /// <summary>
        /// return clockwise vector perpindicular to v
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static PointD PerpindicularVectorCW(ref PointD v)
        {
            //return new PointD(-v.Y, v.X);  //counter cw
            return new PointD(v.Y, -v.X);  //cw
        }
        
        /// <summary>
        /// Compute the distance from segment AB to C
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
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

        public static double LineSegPointDist(ref PointD a, ref PointD b, ref PointD c, out double tVal, out PointD pointOnSegment)
        {
            //float dist = cross(a,b,c) / distance(a,b);

            if (Dot(ref a, ref b, ref c) > 0)
            {
                tVal = 1; //actually >=1. need to derive
                pointOnSegment = b;
                return Distance(ref b, ref c);
            }
            if (Dot(ref b, ref a, ref c) > 0)
            {
                tVal = 0; //actually <=0. Need to derive
                pointOnSegment = a;
                return Distance(ref a, ref c);
            }
            tVal = 0.5;
            //to calculate the tVal need to check Cross result. if >0 create perp vector ccw
            //else create per vector cw
            //normalize perp vector (i.e. unit vector) by dividing by Distance AB used below and multiply 
            //by returned distance below. tVal = ((c.x + perp.x)-a.x)/(b.x-a.x)

            double cross = Cross(ref a, ref b, ref c);
            double distAB = Distance(ref a, ref b);
            double pointSegmentDistance = Math.Abs(cross / distAB);
            if (cross < 0)
            {
                PointD perpVector = new PointD(pointSegmentDistance * (a.Y - b.Y) / distAB, pointSegmentDistance*(b.X - a.X) / distAB);
                pointOnSegment = new PointD((c.X + perpVector.X), (c.Y + perpVector.Y));
                if (Math.Abs(a.X - b.X) < double.Epsilon)
                {
                    tVal = (c.Y + perpVector.Y - a.Y) / (b.Y - a.Y);
                }
                else
                {
                    tVal = (c.X + perpVector.X - a.X) / (b.X - a.X);
                }                
            }
            else
            {
                PointD perpVector = new PointD(pointSegmentDistance * (b.Y - a.Y) / distAB, pointSegmentDistance*(a.X - b.X) / distAB);
                pointOnSegment = new PointD((c.X + perpVector.X), (c.Y + perpVector.Y));
                if (Math.Abs(a.X - b.X) < double.Epsilon)                
                {
                    tVal = (c.Y + perpVector.Y - a.Y) / (b.Y - a.Y);
                }
                else
                {
                    tVal = (c.X + perpVector.X - a.X) / (b.X - a.X);
                }                
            }
            return pointSegmentDistance;
        }

        public static double LineSegPointDist(ref PointD a, ref PointD b, ref PointD c, ref PolylineDistanceInfo polylineDistanceInfo)
        {

            //see this http://geomalgorithms.com/a02-_lines.html and check if we can simplify this function
            double pointSegmentDistance;
            if (Dot(ref a, ref b, ref c) > 0)
            {
                pointSegmentDistance = Distance(ref b, ref c);
                if (pointSegmentDistance < polylineDistanceInfo.Distance)
                {
                    //polylineDistanceInfo.Distance = pointSegmentDistance;
                    polylineDistanceInfo.PolylinePoint = b;
                    polylineDistanceInfo.TVal = 1;
                    polylineDistanceInfo.LineSegmentSide = LineSegmentSide.EndOfSegment;
                }
            }
            else if (Dot(ref b, ref a, ref c) > 0)
            {
                pointSegmentDistance = Distance(ref a, ref c);
                if (pointSegmentDistance < polylineDistanceInfo.Distance)
                {
                    //polylineDistanceInfo.Distance = pointSegmentDistance;
                    polylineDistanceInfo.PolylinePoint = a;
                    polylineDistanceInfo.TVal = 0; //actually <=0. Need to derive
                    polylineDistanceInfo.LineSegmentSide = LineSegmentSide.StartOfSegment;
                }
            }
            else
            {
                double cross = Cross(ref a, ref b, ref c);
                double distAB = Distance(ref a, ref b);
                pointSegmentDistance = Math.Abs(cross / distAB);
                if (pointSegmentDistance < polylineDistanceInfo.Distance)
                {
                    //to calculate the tVal need to check Cross result. if <0 create perp vector ccw
                    //else create per vector cw
                    //normalize perp vector (i.e. unit vector) by dividing by Distance AB used below and multiply 
                    //by pointSegmentDistance. tVal = ((c.x + perp.x)-a.x)/(b.x-a.x)  
                    PointD perpVector;
                    if (cross < 0)
                    {
                        polylineDistanceInfo.LineSegmentSide = LineSegmentSide.RightOfSegment;
                        perpVector = new PointD(pointSegmentDistance * (a.Y - b.Y) / distAB, pointSegmentDistance * (b.X - a.X) / distAB);                        
                    }
                    else
                    {
                        polylineDistanceInfo.LineSegmentSide = cross < double.Epsilon ? LineSegmentSide.OnSegment : LineSegmentSide.LeftOfSegment;
                        perpVector = new PointD(pointSegmentDistance * (b.Y - a.Y) / distAB, pointSegmentDistance * (a.X - b.X) / distAB);
                    }
                    //polylineDistanceInfo.Distance = pointSegmentDistance;
                    polylineDistanceInfo.PolylinePoint = new PointD((c.X + perpVector.X), (c.Y + perpVector.Y));
                    if (Math.Abs(a.X - b.X) < double.Epsilon)
                    {
                        polylineDistanceInfo.TVal = (polylineDistanceInfo.PolylinePoint.Y - a.Y ) / (b.Y - a.Y);
                    }
                    else
                    {
                        polylineDistanceInfo.TVal = (polylineDistanceInfo.PolylinePoint.X - a.X) / (b.X - a.X);
                    }
                }
            }
            return pointSegmentDistance;
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


        /// <summary>
        /// returns whether two (infinite) lines intersect and calculates intersection point if the lines intersect 
        /// </summary>
        /// <param name="p0">first point defining line 1</param>
        /// <param name="p1">second point defining line 1</param>
        /// <param name="p2">first point defining line 2</param>
        /// <param name="p3">second point defining line 2</param>
        /// <param name="intersectionPoint">the calculated intersectino point (PointD.Empty if lines do not intersect)</param>
        /// <param name="tVal">The parametric tValue where the interection ocurrs on line 1. See remarks</param>
        /// <param name="vVal">The parametric vValue where the interection ocurrs on line 2. See remarks</param>
        /// <returns>true if the two lines intersect</returns>
        /// <remarks>
        /// <para>
        /// This method tests "infinite" lines defined by two points on the line. Line 1 is defined as the line passing through points p0 and p1.
        /// Line 2 is defined as the line passing through points p2 and p3
        /// </para>
        /// <para>
        /// The two lines will not intersect if the lines are parallel and the method will return false. If either p0 == p1 or p2 == p3 then a line cannot be defined
        /// and the method will return false;
        /// </para>
        /// <para>
        /// The tVal and vVal are from the parametric representations of the lines in the equations: <br/>
        /// line1   : (X,Y) = p0 + (t * (p1 - p0)) <br/>
        ///         : X = p0.X + (t * (p1.X-p0.X)) <br/>
        ///         : Y = p0.Y + (t * (p1.Y-p0.Y)) <br/>
        /// The tVal returned indicates where on Line1 the intersection ocurrs. <br/>
        /// If tVal &lt; 0 the intersection ocurrs before p0 on the line. <br/>
        /// If tVal &gt; 1 the intersection ocurrs after p1 on the line. <br/>
        /// If tVal is between 0 and 1 the intersection ocurrs between p0 and p1 on the line
        /// </para>
        /// </remarks>
        public static bool LineLineIntersection(ref PointD p0, ref PointD p1, ref PointD p2, ref PointD p3, out PointD intersectionPoint, out double tVal, out double vVal)
        {
            intersectionPoint = PointD.Empty;
            tVal = vVal = 0;

            //derive parametric representation of lines
            // line 0 (X,Y) = p0 + (t * vec0)
            //         X    = p0.X + (t * vec0.X)
            //         Y    = p0.Y + (t * vec0.Y)
            // line 1 (X,Y) = p2 + (v * vec1)
            //         X    = p2.X + (v * vec1.X)
            //         Y    = p2.Y + (v * vec1.Y)
            PointD vec0 = new PointD(p1.X - p0.X, p1.Y - p0.Y);
            PointD vec1 = new PointD(p3.X - p2.X, p3.Y - p2.Y);

            if (vec0.IsEmpty || vec1.IsEmpty) return false;

            //when lines intersect they will have the same (X,Y) value
            // p0.X + (t * vec0.X) = p2.X + (v * vec1.X)
            // p0.Y + (t * vec0.Y) = p2.Y + (v * vec1.Y)

            //re-arrange and solve for t and v using determinants
            // (vec0.X * t) - (vec1.X * v) = p2.X - p0.X
            // (vec0.Y * t) - (vec1.Y * v) = p2.Y - p0.Y

            //double detDenom = (vec0.X * -vec1.Y) - (vec0.Y * -vec1.X);
            double detDenom =   (vec0.Y * vec1.X) - (vec0.X * vec1.Y);

            if (Math.Abs(detDenom) <= double.Epsilon) return false; //denominator determinant is zero => not solvable (lines parallel)

            //double detT = ((p2.X - p0.X) * -vec1.Y) - ( (p2.Y - p0.Y) * -(vec1.X));
            double detT = ((p2.Y - p0.Y) * (vec1.X)) - ((p2.X - p0.X) * vec1.Y);

            double detV = (vec0.X * (p2.Y - p0.Y)) - (vec0.Y * (p2.X - p0.X));
            
            tVal = detT / detDenom;
            vVal = detV / detDenom;

            //calculate the intersectin point using the calculated tVal
            intersectionPoint.X = p0.X + (tVal * vec0.X);
            intersectionPoint.Y = p0.Y + (tVal * vec0.Y);


            return true;

        }


        #endregion

        /// <summary> 
        /// Rectangle Circle intersection test
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

        /// <summary> 
        /// Rectangle Circle intersection test
        /// </summary>
        /// <param name="r"></param>
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        /// <remarks>This method is untested</remarks>        
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


        #region polyline simplification


        /// <summary>
        /// Douglas Peucker line simplification
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputCount"></param>
        /// <param name="tolerance"></param>
        /// <param name="output"></param>
        /// <param name="outputCount"></param>
        public static void SimplifyDouglasPeucker(System.Drawing.Point[] input, int inputCount, int tolerance, System.Drawing.Point[] output, ref int outputCount)
        {
            NativeMethods.SimplifyDouglasPeucker(input, inputCount, tolerance, output, ref outputCount);
        }

        /// <summary>
        /// Douglas Peucker line simplification
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputCount"></param>
        /// <param name="tolerance"></param>
        /// <param name="output"></param>
        /// <param name="outputCount"></param>
        public static void SimplifyDouglasPeucker(PointD[] input, int inputCount, double tolerance, PointD[] output, ref int outputCount)
        {
            NativeMethods.SimplifyDouglasPeucker(input, inputCount, tolerance, output, ref outputCount);
        }

              
        /// <summary>
        /// Uses the Douglas Peucker algorithm to reduce the number of points.
        /// </summary>
        /// <param name="inputPoints">The input points to be reduced</param>
        /// <param name="reducedPointIndicies">List to populate the indices of the points in the simplified polyline</param>
        /// <param name="inputPointCount">The number of points in inputPoints</param>
        /// <param name="tolerance">the tolerance before discarding points - see DP algorithm for explanation</param>
        /// <returns>number of reduced points</returns>
        public static int SimplifyDouglasPeucker(EGIS.ShapeFileLib.PointD[] inputPoints, List<int> reducedPointIndicies, int inputPointCount, double tolerance)
        {
            reducedPointIndicies.Clear();
            if (inputPointCount < 3)
            {
                for (int i = 0; i < inputPointCount; ++i)
                {
                    reducedPointIndicies.Add(i);
                }
                return inputPointCount;
            }


            Int32 firstPoint = 0;
            Int32 lastPoint = inputPointCount - 1;

            //Add the first and last index to the keepers
            reducedPointIndicies.Add(firstPoint);
            reducedPointIndicies.Add(lastPoint);

            //ensure first and last point not the same
            while (lastPoint >= 0 && inputPoints[firstPoint].Equals(inputPoints[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(inputPoints, firstPoint, lastPoint,
            tolerance, reducedPointIndicies);

            reducedPointIndicies.Sort();


            //if only two points check if both points the same
            if (reducedPointIndicies.Count == 2)
            {
                if (Math.Abs(inputPoints[reducedPointIndicies[0]].X - inputPoints[reducedPointIndicies[1]].X) < double.Epsilon && Math.Abs(inputPoints[reducedPointIndicies[0]].Y - inputPoints[reducedPointIndicies[1]].Y) < double.Epsilon) return 0;
            }


            return reducedPointIndicies.Count;
        }


        private static void DouglasPeuckerReduction(EGIS.ShapeFileLib.PointD[] points, Int32 firstPoint, Int32 lastPoint, Double tolerance,
            List<Int32> pointIndexsToKeep)
        {
            double maxDistance = 0;
            int indexMax = 0;

            for (int index = firstPoint; index < lastPoint; ++index)
            {
                double distance =  LineSegPointDist(ref points[firstPoint], ref points[lastPoint], ref points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexMax = index;
                }
            }

            if (maxDistance > tolerance && indexMax != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexMax);

                DouglasPeuckerReduction(points, firstPoint, indexMax, tolerance, pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexMax, lastPoint, tolerance, pointIndexsToKeep);
            }
        }


        #endregion



        #region Cohen-Sutherland Line Clipping

        public struct ClipBounds
        {
            public double XMin;
            public double XMax;
            public double YMin;
            public double YMax;
        }

        [Flags]
        enum OutCode { Inside = 0, Left = 1, Right = 2, Bottom = 4, Top = 8 };

        private const int INSIDE = 0; // 0000
        private const int LEFT = 1;   // 0001
        private const int RIGHT = 2;  // 0010
        private const int BOTTOM = 4; // 0100
        private const int TOP = 8;    // 1000

        // Compute the bit code for a point (x, y) using the clip rectangle
        // bounded diagonally by (xmin, ymin), and (xmax, ymax)

        // ASSUME THAT xmax, xmin, ymax and ymin are global constants.

        private static OutCode ComputeOutCode(double x, double y, ref ClipBounds clipBounds)
        {
            OutCode code = OutCode.Inside;


            if (x < clipBounds.XMin)           // to the left of clip window
                code |= OutCode.Left;
            else if (x > clipBounds.XMax)      // to the right of clip window
                code |= OutCode.Right;
            if (y < clipBounds.YMin)           // below the clip window
                code |= OutCode.Bottom;
            else if (y > clipBounds.YMax)      // above the clip window
                code |= OutCode.Top;

            return code;
        }

        // Cohen–Sutherland clipping algorithm clips a line from
        // P0 = (x0, y0) to P1 = (x1, y1) against a rectangle with 
        // diagonal from (xmin, ymin) to (xmax, ymax).
        static bool CohenSutherlandLineClip(ref double x0, ref double y0, ref double x1, ref double y1, ref ClipBounds clipBounds)
        {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            OutCode outcode0 = ComputeOutCode(x0, y0, ref clipBounds);
            OutCode outcode1 = ComputeOutCode(x1, y1, ref clipBounds);
            bool accept = false;

            while (true)
            {
                if ((outcode0 | outcode1) == OutCode.Inside)
                {
                    // bitwise OR is 0: both points inside window; trivially accept and exit loop
                    accept = true;
                    break;
                }
                else if ((outcode0 & outcode1) != OutCode.Inside)
                {
                    // bitwise AND is not 0: both points share an outside zone (LEFT, RIGHT, TOP,
                    // or BOTTOM), so both must be outside window; exit loop (accept is false)
                    break;
                }
                else
                {
                    // failed both tests, so calculate the line segment to clip
                    // from an outside point to an intersection with clip edge
                    double x = 0, y = 0;

                    // At least one endpoint is outside the clip rectangle; pick it.
                    OutCode outcodeOut = outcode0 != OutCode.Inside ? outcode0 : outcode1;

                    // Now find the intersection point;
                    // use formulas:
                    //   slope = (y1 - y0) / (x1 - x0)
                    //   x = x0 + (1 / slope) * (ym - y0), where ym is ymin or ymax
                    //   y = y0 + slope * (xm - x0), where xm is xmin or xmax
                    // No need to worry about divide-by-zero because, in each case, the
                    // outcode bit being tested guarantees the denominator is non-zero
                    if ((outcodeOut & OutCode.Top) == OutCode.Top)
                    {           // point is above the clip window
                        x = x0 + (x1 - x0) * (clipBounds.YMax - y0) / (y1 - y0);
                        y = clipBounds.YMax;
                    }
                    else if ((outcodeOut & OutCode.Bottom) == OutCode.Bottom)
                    { // point is below the clip window
                        x = x0 + (x1 - x0) * (clipBounds.YMin - y0) / (y1 - y0);
                        y = clipBounds.YMin;// ymin;
                    }
                    else if ((outcodeOut & OutCode.Right) == OutCode.Right)
                    {  // point is to the right of clip window
                        y = y0 + (y1 - y0) * (clipBounds.XMax - x0) / (x1 - x0);
                        x = clipBounds.XMax;// xmax;
                    }
                    else if ((outcodeOut & OutCode.Left) == OutCode.Left)
                    {   // point is to the left of clip window
                        y = y0 + (y1 - y0) * (clipBounds.XMin - x0) / (x1 - x0);
                        x = clipBounds.XMin;// xmin;
                    }

                    // Now we move outside point to intersection point to clip
                    // and get ready for next pass.
                    if (outcodeOut == outcode0)
                    {
                        x0 = x;
                        y0 = y;
                        outcode0 = ComputeOutCode(x0, y0, ref clipBounds);
                    }
                    else
                    {
                        x1 = x;
                        y1 = y;
                        outcode1 = ComputeOutCode(x1, y1, ref clipBounds);
                    }
                }
            }
            //if (accept)
            //{
            //    // Following functions are left for implementation by user based on
            //    // their platform (OpenGL/graphics.h etc.)
            //    // DrawRectangle(xmin, ymin, xmax, ymax);
            //    // LineSegment(x0, y0, x1, y1);
            //    clippedPoints.Add(x0);
            //    clippedPoints.Add(y0);
            //    clippedPoints.Add(x1);
            //    clippedPoints.Add(y1);
            //}
            return accept;
        }

        public static void PolyLineClip(PointD[] input, int inputCount, ClipBounds clipBounds, List<double> clippedPoints, List<int> parts)
        {
            bool inside = false;            
            for (int n = 0; n < inputCount-1; ++n)
            {
                double x0 = input[n].X, y0 = input[n].Y, x1 = input[n + 1].X, y1 = input[n + 1].Y;

                bool insideBounds = CohenSutherlandLineClip(ref x0, ref y0, ref x1, ref y1, ref clipBounds);
                if (insideBounds)
                {
                    //new part
                    if (!inside)
                    {
                        parts.Add(clippedPoints.Count);
                        clippedPoints.Add(x0);
                        clippedPoints.Add(y0);                        
                    }
                    clippedPoints.Add(x1);
                    clippedPoints.Add(y1);

                }
                inside = insideBounds;
            }

        }

        public static void PolyLineClip(System.Drawing.Point[] input, int inputCount, ClipBounds clipBounds, List<int> clippedPoints, List<int> parts)
        {
            bool inside = false;
            for (int n = 0; n < inputCount - 1; ++n)
            {
                double x0 = input[n].X, y0 = input[n].Y, x1 = input[n + 1].X, y1 = input[n + 1].Y;

                bool insideBounds = CohenSutherlandLineClip(ref x0, ref y0, ref x1, ref y1, ref clipBounds);
                if (insideBounds)
                {
                    //new part
                    if (!inside)
                    {
                        parts.Add(clippedPoints.Count);
                        clippedPoints.Add((int)Math.Round(x0));
                        clippedPoints.Add((int)Math.Round(y0));
                    }
                    clippedPoints.Add((int)Math.Round(x1));
                    clippedPoints.Add((int)Math.Round(y1));

                }
                inside = insideBounds;
            }

        }

        public static void PolyLineClip(System.Drawing.Point[] input, int inputCount, ClipBounds clipBounds, List<int> clippedPoints, List<int> parts, double[] measures, List<double> clippedMeasures)
        {
            bool inside = false;
            for (int n = 0; n < inputCount - 1; ++n)
            {
                double x0 = input[n].X, y0 = input[n].Y, x1 = input[n + 1].X, y1 = input[n + 1].Y;

                bool insideBounds = CohenSutherlandLineClip(ref x0, ref y0, ref x1, ref y1, ref clipBounds);
                if (insideBounds)
                {
                    int dx = input[n + 1].X - input[n].X;
                    int dy = input[n + 1].Y - input[n].Y;
                                 

                    //new part
                    if (!inside)
                    {
                        parts.Add(clippedPoints.Count);
                        clippedPoints.Add((int)Math.Round(x0));
                        clippedPoints.Add((int)Math.Round(y0));

                        if (dx != 0)
                        {
                            double r = ((double)(x0 - input[n].X)) / dx;
                            clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                        }
                        else if (dy != 0)
                        {
                            double r = ((double)(y0 - input[n].Y)) / dy;
                            clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                        }
                        else
                        {
                            //point
                            clippedMeasures.Add(measures[n]);
                        }                                               

                    }
                    clippedPoints.Add((int)Math.Round(x1));
                    clippedPoints.Add((int)Math.Round(y1));

                    if (dx != 0)
                    {
                        double r = ((double)(x1 - input[n].X)) / dx;
                        clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                    }
                    else if (dy != 0)
                    {
                        double r = ((double)(y1 - input[n].Y)) / dy;
                        clippedMeasures.Add(measures[n] + r * (measures[n + 1] - measures[n]));
                    }
                    else
                    {
                        //point
                        clippedMeasures.Add(measures[n+1]);
                    }

                }
                inside = insideBounds;
            }

        }


        public static void PolygonClip(PointD[] inputPolygon, int inputCount, ClipBounds clipBounds, List<PointD> clippedPolygon)
        {
            List<PointD> inputList = new List<PointD>(inputCount);
            List<PointD> outputList = clippedPolygon;
            bool previousInside;
            for (int n = 0; n < inputCount; ++n)
            {
                inputList.Add(inputPolygon[n]);
            }

            bool inputPolygonIsHole = IsPolygonHole(inputPolygon, inputCount);
            
            
            //test left
            previousInside = inputList[inputList.Count - 1].X >= clipBounds.XMin; 
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointD currentPoint = inputList[n];
                bool currentInside = currentPoint.X >= clipBounds.XMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointD prevPoint = n==0 ? inputList[inputList.Count-1] : inputList[n-1];
                    double x = clipBounds.XMin;
                    double y = prevPoint.Y + (currentPoint.Y-prevPoint.Y)*(x-prevPoint.X)/(currentPoint.X-prevPoint.X);
                    outputList.Add(new PointD(x, y));   
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            //test top
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].Y <= clipBounds.YMax; ;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointD currentPoint = inputList[n];
                bool currentInside = currentPoint.Y <= clipBounds.YMax;                
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointD prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double y = clipBounds.YMax;
                    double x = prevPoint.X + (currentPoint.X - prevPoint.X) * (y - prevPoint.Y) / (currentPoint.Y - prevPoint.Y);
                    outputList.Add(new PointD(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            //test right
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].X <= clipBounds.XMax;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointD currentPoint = inputList[n];
                bool currentInside = currentPoint.X <= clipBounds.XMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointD prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double x = clipBounds.XMax;
                    double y = prevPoint.Y + (currentPoint.Y - prevPoint.Y) * (x - prevPoint.X) / (currentPoint.X - prevPoint.X);
                    outputList.Add(new PointD(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            //test bottom
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].Y >= clipBounds.YMin;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                PointD currentPoint = inputList[n];
                bool currentInside = currentPoint.Y >= clipBounds.YMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    PointD prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double y = clipBounds.YMin;
                    double x = prevPoint.X + (currentPoint.X - prevPoint.X) * (y - prevPoint.Y) / (currentPoint.Y - prevPoint.Y);
                    outputList.Add(new PointD(x, y));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            bool clippedPolygonIsHole = IsPolygonHole(outputList, outputList.Count);
            if (clippedPolygonIsHole != inputPolygonIsHole) outputList.Reverse();
            //clippedPolygon
        }

        public static void PolygonClip(System.Drawing.Point[] inputPolygon, int inputCount, ClipBounds clipBounds, List<System.Drawing.Point> clippedPolygon)
        {
            List<System.Drawing.Point> inputList = new List<System.Drawing.Point>(inputCount);
            List<System.Drawing.Point> outputList = clippedPolygon;
            bool previousInside;
            for (int n = 0; n < inputCount; ++n)
            {
                inputList.Add(new System.Drawing.Point(inputPolygon[n].X, inputPolygon[n].Y));
            }

            bool inputPolygonIsHole = IsPolygonHole(inputList, inputCount);


            //test left
            previousInside = inputList[inputList.Count - 1].X >= clipBounds.XMin;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                System.Drawing.Point currentPoint = inputList[n];
                bool currentInside = currentPoint.X >= clipBounds.XMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    System.Drawing.Point prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double x = clipBounds.XMin;
                    double y = prevPoint.Y + (double)(currentPoint.Y - prevPoint.Y) * (x - prevPoint.X) / (double)(currentPoint.X - prevPoint.X);
                    outputList.Add(new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y)));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint );
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            //test top
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].Y <= clipBounds.YMax; ;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                System.Drawing.Point currentPoint = inputList[n];
                bool currentInside = currentPoint.Y <= clipBounds.YMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    System.Drawing.Point prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double y = clipBounds.YMax;
                    double x = prevPoint.X + (double)(currentPoint.X - prevPoint.X) * (y - prevPoint.Y) / (double)(currentPoint.Y - prevPoint.Y);
                    outputList.Add(new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y)));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            //test right
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].X <= clipBounds.XMax;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                System.Drawing.Point currentPoint = inputList[n];
                bool currentInside = currentPoint.X <= clipBounds.XMax;
                if (currentInside != previousInside)
                {
                    //add intersection
                    System.Drawing.Point prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double x = clipBounds.XMax;
                    double y = prevPoint.Y + (double)(currentPoint.Y - prevPoint.Y) * (x - prevPoint.X) / (double)(currentPoint.X - prevPoint.X);
                    outputList.Add(new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y)));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            //test bottom
            inputList = outputList.ToList();
            previousInside = inputList[inputList.Count - 1].Y >= clipBounds.YMin;
            outputList.Clear();
            for (int n = 0; n < inputList.Count; ++n)
            {
                System.Drawing.Point currentPoint = inputList[n];
                bool currentInside = currentPoint.Y >= clipBounds.YMin;
                if (currentInside != previousInside)
                {
                    //add intersection
                    System.Drawing.Point prevPoint = n == 0 ? inputList[inputList.Count - 1] : inputList[n - 1];
                    double y = clipBounds.YMin;
                    double x = prevPoint.X + (double)(currentPoint.X - prevPoint.X) * (y - prevPoint.Y) / (double)(currentPoint.Y - prevPoint.Y);
                    outputList.Add(new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y)));
                }
                if (currentInside)
                {
                    outputList.Add(currentPoint);
                }
                previousInside = currentInside;
            }
            if (outputList.Count == 0) return;

            bool clippedPolygonIsHole = IsPolygonHole(outputList, outputList.Count);
            if (clippedPolygonIsHole != inputPolygonIsHole) outputList.Reverse();
            //clippedPolygon
        }


        #endregion



    }

    public enum LineSegmentSide { None, OnSegment, StartOfSegment, LeftOfSegment, RightOfSegment, EndOfSegment };

    /// <summary>
    /// Encapsulates point from polyline distance information 
    /// </summary>
    public struct PolylineDistanceInfo
    {
        PointD polylinePoint;
        double distance;
        double tVal;
        int pointIndex;
        LineSegmentSide lineSegmentSide;
        
        public PolylineDistanceInfo(PointD polylinePoint, double distance, double tVal, int pointIndex, LineSegmentSide lineSegmentSide)
        {
            this.polylinePoint = polylinePoint;
            this.distance = distance;
            this.tVal = tVal;
            this.pointIndex = pointIndex;
            this.lineSegmentSide = lineSegmentSide;
        }

        public static PolylineDistanceInfo Empty = new PolylineDistanceInfo(PointD.Empty, double.PositiveInfinity, double.NaN, -1, LineSegmentSide.None);
        

        /// <summary>
        /// The point on the poyline closest to the given point
        /// </summary>
        public PointD PolylinePoint
        {
            get { return polylinePoint; }
            set { polylinePoint = value; }
        }

        /// <summary>
        /// The distance from the given point to the polyline
        /// </summary>
        public double Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        /// <summary>
        /// The zero based point index at the start of the line segment in the poyline closest to the given point
        /// </summary>
        /// <remarks>
        /// A polyline of N points has (N-1) line segments. PointIndex will be between 0 to (N-2) inclusive.
        /// </remarks>
        public int PointIndex
        {
            get { return pointIndex; }
            set { pointIndex = value; }
        }

        /// <summary>
        /// A value between 0..1 indicating the location within the closest line segment
        /// </summary>
        /// <remarks>
        /// <para>
        /// A TVal of 0 indicates the position on the line segment closest the given point is at the start of the line segment <br/>
        /// A TVal of 1 indicates the position on the line segment closest the given point is at the end of the line segment <br/>
        /// </para>
        /// <para>
        /// The TVal property is useful when used with polylines containing measures. For example if Segmentindex is N then
        /// the dervied measure at the point on the polyline closest to a given point would be measure[N] + tVal x (measure[N+1] - measure[N])
        /// </para>
        /// </remarks>
        public double TVal
        {
            get { return tVal; }
            set { tVal = value; }
        }

        public LineSegmentSide LineSegmentSide
        {
            get { return lineSegmentSide; }
            set { lineSegmentSide = value; }
        }
    }

}
