using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.ShapeFileLib
{

    public enum ProjectionType
    {
        None,
        Mercator
    };

    public interface IMapProjection
    {
        PointD ProjectionToLatLong(PointD pt);
        void ProjectionToLatLong(ref PointD ptProj, ref PointD ptLL);

        PointD LatLongtoProjection(PointD pt);
        void LatLongtoProjection(ref PointD ptLL, ref PointD ptProj);
    }



    public class MapProjectionCreator
    {
        public static IMapProjection CreateMapProjection(ProjectionType projectionType)
        {
            switch (projectionType)
            {
                case ProjectionType.None:
                    return new LatLongProjection();
                case ProjectionType.Mercator:
                    return new MercatorProjection();
                default:
                    throw new ArgumentException("Unknown ProjectionType");
            }
            
        }

    }


    public class LatLongProjection : IMapProjection
    {
        #region IMapProjection Members

        public PointD ProjectionToLatLong(PointD pt)
        {
            return pt;
        }

        public void ProjectionToLatLong(ref PointD ptProj, ref PointD ptLL)
        {
            ptLL = ptProj;
        }

        public PointD LatLongtoProjection(PointD pt)
        {
            return pt;
        }

        public void LatLongtoProjection(ref PointD ptLL, ref PointD ptProj)
        {
            ptProj = ptLL;
        }

        #endregion
    }


    public class MercatorProjection : IMapProjection
    {
        private const double MaxLLMercProjD = 85.0511287798066;

        #region IMapProjection Members

        public PointD ProjectionToLatLong(PointD pt)
        {
            double d = (Math.PI / 180) * pt.Y;
            d = Math.Atan(Math.Sinh(d));
            d = d * (180 / Math.PI);
            return new PointD(pt.X, d);
        }

        public PointD LatLongtoProjection(PointD pt)
        {
            if (pt.Y > MaxLLMercProjD)
            {
                pt.Y = MaxLLMercProjD;
            }
            else if (pt.Y < -MaxLLMercProjD)
            {
                pt.Y = -MaxLLMercProjD;
            }
            double d = (Math.PI / 180) * pt.Y;
            double sd = Math.Sin(d);

            d = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
            return new PointD(pt.X, d);
        }

                       
        //internal static void LLToProjection(ref double ptx, ref double pty, out double px, out double py)
        //{
        //    px = ptx;
            
        //    double d;
        //    if (pty > MaxLLMercProjD)
        //    {
        //        d = (Math.PI / 180) * MaxLLMercProjD;
        //    }
        //    else if (pty < -MaxLLMercProjD)
        //    {
        //        d = (Math.PI / 180) * (-MaxLLMercProjD);
        //    }
        //    else
        //    {
        //        d = (Math.PI / 180) * pty;
        //    }
        //    double sd = Math.Sin(d);
        //    py = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
            
        //}

        
       
        public void ProjectionToLatLong(ref PointD ptProj, ref PointD ptLL)
        {
            double d = (Math.PI / 180) * ptProj.Y;
            d = Math.Atan(Math.Sinh(d));
            d = d * (180 / Math.PI);
            ptLL.X = ptProj.X;
            ptLL.Y = d;
        }

        public void LatLongtoProjection(ref PointD ptLL, ref PointD ptProj)
        {
            ptProj.X = ptLL.X;

            double d;
            if (ptLL.Y > MaxLLMercProjD)
            {
                d = (Math.PI / 180) * MaxLLMercProjD;
            }
            else if (ptLL.Y < -MaxLLMercProjD)
            {
                d = (Math.PI / 180) * (-MaxLLMercProjD);
            }
            else
            {
                d = (Math.PI / 180) * ptLL.Y;
            }
            double sd = Math.Sin(d);
            ptProj.Y = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));

        }

        #endregion


    }
}
