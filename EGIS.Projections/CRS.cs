using System;
using System.Collections.Generic;

namespace EGIS.Projections
{
    
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
        int Transform(double[] points, int pointCount);

        int Transform(double[] points, int startIndex, int pointCount);

        unsafe int Transform(double* points, int pointCount);

        
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
