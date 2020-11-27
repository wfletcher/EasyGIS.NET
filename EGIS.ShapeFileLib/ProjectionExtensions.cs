using System;

using EGIS.Projections;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// EGIS.Projections Extensions
    /// </summary>
    public static class ProjectionExtensions
    {
        public static RectangleD Transform(this RectangleD @this, ICoordinateTransformation transformation)
        {
            return transformation.Transform(@this);
        }

        public static RectangleD Transform(this RectangleD @this, ICRS source, ICRS target)
        {
            if (source == null || target == null || source.IsEquivalent(target)) return @this;
            using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(source, target))
            {
                return @this.Transform(transformation);
            }
        }

        public static bool IsValidExtent(this RectangleD @this)
        {
            if (double.IsInfinity(@this.Width) || double.IsInfinity(@this.Height) ||
                @this.Width < 0 ||
                double.IsNaN(@this.X) || double.IsNaN(@this.Y) ||
                double.IsNaN(@this.Width) || double.IsNaN(@this.Height)) return false;

            return true;
        }


        public static RectangleD Transform(this EGIS.Projections.ICoordinateTransformation @this, RectangleD rect, Projections.TransformDirection direction = Projections.TransformDirection.Forward)
        {
            //following code was derived from code in QGIS TransformBoundingBox function in QgsCoordinateTransform
            //improves calculated bounding box after transforming to a different CRS when
            //only using the corners of the rectangle will not give an accurate result when transforming to some CRS.
            //This method creates a grid of points over the input rectangle,
            //transforms these points and then calculates the target bounding box from these points.

            const int nPoints = 1000;
            double t = Math.Pow(Math.Sqrt((double)nPoints) - 1, 2.0);
            double d = Math.Sqrt((rect.Width * rect.Height) / Math.Pow(Math.Sqrt((double)nPoints) - 1, 2.0));
            int nXPoints = (int)Math.Min(Math.Ceiling(rect.Width / d) + 1, 1000);
            int nYPoints = (int)Math.Min(Math.Ceiling(rect.Height / d) + 1, 1000);

            int totalPoints = (nXPoints * nYPoints);

            PointD[] pts = new PointD[totalPoints];

            double dx = rect.Width / (double)(nXPoints - 1);
            double dy = rect.Height / (double)(nYPoints - 1);

            double pointY = rect.Top;
            int index = 0;
            for (int i = nYPoints; i > 0; --i)
            {
                double pointX = rect.Left;
                for (int j = nXPoints; j > 0; --j)
                {
                    pts[index].X = pointX;
                    pts[index++].Y = pointY;
                    pointX += dx;
                }
                pointY += dy;
            }
            @this.Transform(pts, direction);
            double minX = double.PositiveInfinity, maxX = double.NegativeInfinity, minY = double.PositiveInfinity, maxY = double.NegativeInfinity;

            for (int n = totalPoints - 1; n >= 0; --n)
            {
                if (double.IsInfinity(pts[n].X) || double.IsInfinity(pts[n].Y)) continue;
                minX = Math.Min(pts[n].X, minX);
                minY = Math.Min(pts[n].Y, minY);
                maxX = Math.Max(pts[n].X, maxX);
                maxY = Math.Max(pts[n].Y, maxY);
            }
            return RectangleD.FromLTRB(minX, minY, maxX, maxY);
        }

        public static unsafe PointD Transform(this EGIS.Projections.ICoordinateTransformation @this, PointD pt, Projections.TransformDirection direction = Projections.TransformDirection.Forward)
        {
            PointD* ptr = &pt;
            @this.Transform((double*)ptr, 1, direction);
            return pt;
        }

        public static unsafe void Transform(this EGIS.Projections.ICoordinateTransformation @this, PointD[] points, Projections.TransformDirection direction = Projections.TransformDirection.Forward)
        {
            fixed (PointD* ptr = points)
            {
                @this.Transform((double*)ptr, points.Length, direction);                
            }
        }

       
    }
}
