#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2020 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.ShapeFileLib class library of Easy GIS .NET.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/

#endregion

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
            return string.Format(System.Globalization.CultureInfo.CurrentCulture,"WestLongitudeDegrees:{0}, EastLongitudeDegrees:{1}, NorthLatitudeDegrees:{2}, SouthLatitudeDegrees:{3}",
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
        /// <summary>
        /// Well Known Text representation of the Coordinate Reference System (WKT2 2018 format)
        /// </summary>
        string WKT
        {
            get;
        }

        /// <summary>
        /// The name of the CRS
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// The CRS ID (EPSG)
        /// </summary>
        string Id
        {
            get;
        }

        /// <summary>
        /// CRS Authority
        /// </summary>
        string Authority
        {
            get;
        }

        /// <summary>
        /// Tests if the CRS is equivalent to a given CRS
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        bool IsEquivalent(ICRS other);

        /// <summary>
        /// The CRS Area of use. Area of use is defined in geodetic lat/lon coordinates regardless of the 
        /// units used by the CRS
        /// </summary>
        CRSBoundingBox AreaOfUse
        {
            get;
        }

        /// <summary>
        /// Gets the WKT of the CRS in given WKT format
        /// </summary>
        /// <param name="wktType"></param>
        /// <param name="indentText"></param>
        /// <returns></returns>
        string GetWKT(PJ_WKT_TYPE wktType, bool indentText);
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

		ICRS CreateCRSFromPrjFile(string prjFile);       

    }


#pragma warning disable CA1707

    /// <summary>
    /// Enumeration defining WKT type
    /// </summary>
    public enum PJ_WKT_TYPE
    {        
        /// <summary>
        /// WKT2 2015 format        
        /// </summary>
        PJ_WKT2_2015,
        /// <summary>
        /// WKT2 simplified 2015 format
        /// </summary>
        PJ_WKT2_2015_SIMPLIFIED,
        /// <summary>
        /// WKT2 2018 format 
        /// </summary>
        PJ_WKT2_2018,
        /// <summary>
        /// WKT2 simplified 2018 format
        /// </summary>
        PJ_WKT2_2018_SIMPLIFIED,
        /// <summary>
        /// WKT1 GDAL format
        /// </summary>
        PJ_WKT1_GDAL,
       /// <summary>
       /// WKT1 ESRI format
       /// </summary>
        PJ_WKT1_ESRI
    }

#pragma warning restore CA1709

}
