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
            for (int i = offset; i < numPoints - 1; i++)
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

            for (int i = offset; i < numPoints - 1; i++)
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
            
            for (int i = offset; i < numPoints - 1; i++)
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

        internal static double LineSegPointDist(ref PointD a, ref PointD b, ref PointD c, ref PolylineDistanceInfo polylineDistanceInfo)
        {
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
