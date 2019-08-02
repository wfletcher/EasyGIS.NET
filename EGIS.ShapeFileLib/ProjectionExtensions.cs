using System;

using EGIS.Projections;

namespace EGIS.ShapeFileLib
{
    public static class ProjectionExtensions
    {
        public static RectangleD Transform(this RectangleD @this, ICoordinateTransformation transformation)
        {
            double[] pts = new double[8];
            pts[0] = @this.Left; pts[1] = @this.Bottom;
            pts[2] = @this.Right; pts[3] = @this.Bottom;
            pts[4] = @this.Right; pts[5] = @this.Top;
            pts[6] = @this.Left; pts[7] = @this.Top;
            transformation.Transform(pts, 4);
            return RectangleD.FromLTRB(Math.Min(pts[0], pts[6]),
                Math.Min(pts[5], pts[7]), Math.Max(pts[2], pts[4]),
                Math.Max(pts[1], pts[3]));
        }

        public static RectangleD Transform(this RectangleD @this, ICRS source, ICRS target)
        {
            if (source == null || target == null || source.IsEquivalent(target)) return @this;
            using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(source, target))
            {
                double[] pts = new double[8];
                pts[0] = @this.Left; pts[1] = @this.Bottom;
                pts[2] = @this.Right; pts[3] = @this.Bottom;
                pts[4] = @this.Right; pts[5] = @this.Top;
                pts[6] = @this.Left; pts[7] = @this.Top;                
                transformation.Transform(pts, 4);
                
                return RectangleD.FromLTRB(Math.Min(pts[0], pts[6]),
                    Math.Min(pts[5], pts[7]), Math.Max(pts[2], pts[4]),
                    Math.Max(pts[1], pts[3]));
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
            double[] pts = new double[8];
            pts[0] = rect.Left; pts[1] = rect.Bottom;
            pts[2] = rect.Right; pts[3] = rect.Bottom;
            pts[4] = rect.Right; pts[5] = rect.Top;
            pts[6] = rect.Left; pts[7] = rect.Top;
            @this.Transform(pts, 4, direction);
            return RectangleD.FromLTRB(Math.Min(pts[0], pts[6]),
                Math.Min(pts[5], pts[7]), Math.Max(pts[2], pts[4]),
                Math.Max(pts[1], pts[3]));
        }

        public static unsafe PointD Transform(this EGIS.Projections.ICoordinateTransformation @this, PointD pt, Projections.TransformDirection direction = Projections.TransformDirection.Forward)
        {
            PointD* ptr = &pt;
            @this.Transform((double*)ptr, 1, direction);
            return pt;
        }

    }
}
