using System;
using System.Collections.Generic;

[assembly: CLSCompliant(true)]
namespace EGIS.Projections
{
    public struct CRSBoundingBox
    {
        public double WestLongitudeDegrees;
        public double NorthLatitudeDegrees;
        public double EastLongitudeDegrees;
        public double SouthLatitudeDegrees;        

        public bool IsDefined
        {
            get
            {
                //if the area of interest is unknown then parameters will be -1000
                return !(Math.Abs(WestLongitudeDegrees + 1000) < 0.01 ||
                         Math.Abs(NorthLatitudeDegrees + 1000) < 0.01 ||
                         Math.Abs(EastLongitudeDegrees + 1000) < 0.01 ||
                         Math.Abs(SouthLatitudeDegrees + 1000) < 0.01);
            }
        }

        public override string ToString()
        {
            return string.Format("WestLongitudeDegrees:{0}, EastLongitudeDegrees:{1}, NorthLatitudeDegrees:{2}, SouthLatitudeDegrees:{3}",
                WestLongitudeDegrees, EastLongitudeDegrees, NorthLatitudeDegrees, SouthLatitudeDegrees);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
    
    /// <summary>
    /// interface defining a Coordinate Reference System
    /// </summary>
    /// <remarks>
    /// A Coordinate Reference System (CRS) is also known as a Spatial Reference System.
    /// </remarks>
    public interface ICRS
    {
        string WKT
        {
            get;
        }

        string Name
        {
            get;
        }

        string Id
        {
            get;
        }

        string Authority
        {
            get;
        }

        bool IsEquivalent(ICRS other);

        CRSBoundingBox AreaOfUse
        {
            get;
        }
    }

    /// <summary>
    /// interface defining a Geographic Coordinate Reference System
    /// </summary>
    public interface IGeographicCRS : ICRS
    {
    }

    /// <summary>
    /// interface defining a Projected Coordinate Reference System
    /// </summary>
    public interface IProjectedCRS : ICRS
    {
        double UnitsToMeters
        {
            get;
        }
    }


    public enum TransformDirection
    {
        None,
        Forward,
        Inverse
    }
    /// <summary>
    /// interface defining a Coordinate Transformation used to transform a coordinate from a source CRS
    /// to a target CRS.
    /// </summary>
    public interface ICoordinateTransformation : IDisposable
    {
        ICRS SourceCRS
        {
            get;
        }

        ICRS TargetCRS
        {
            get;
        }

        /// <summary>
        /// transforms coordinates in place. The values of the points array will be replaced
        /// with transformed coordinates
        /// </summary>
        /// <param name="points"></param>
        /// <param name="pointCount"></param>
        /// <returns>number of points transformed</returns>
        int Transform(double[] points, int pointCount, TransformDirection direction = TransformDirection.Forward);

        int Transform(double[] points, int startIndex, int pointCount, TransformDirection direction = TransformDirection.Forward);

        unsafe int Transform(double* points, int pointCount, TransformDirection direction = TransformDirection.Forward);

        
    }



    public interface ICRSFactory
    {
        List<IGeographicCRS> GeographicCoordinateSystems
        {
            get;
        }

        List<IProjectedCRS> ProjectedCoordinateSystems
        {
            get;
        }

        ICRS GetCRSById(int id);

        ICRS CreateCRSFromWKT(string wkt);

        ICoordinateTransformation CreateCoordinateTrasformation(ICRS source, ICRS target);

        ICoordinateTransformation CreateCoordinateTrasformation(string sourceWKT, string targetWKT);


    }
}
