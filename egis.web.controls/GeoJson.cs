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
using System.Linq;
using System.Text;

namespace EGIS.Web.Controls
{

    #region "GeoJson classes"

    /// <summary>
    /// abstract base GeoJSON class
    /// </summary>
    public abstract class GeoJsonObject
    {
        public abstract string type
        {
            get;
        }

    }

    /// <summary>
    /// GeoJSON FeatureCollection
    /// </summary>
    public class FeatureCollection : GeoJsonObject
    {
        public override string type
        {
            get { return "FeatureCollection"; }
        }

        public List<Feature> features = new List<Feature>();

    }

    /// <summary>
    /// GeoJSON Feaure
    /// </summary>
    public class Feature : GeoJsonObject
    {
        public override string type
        {
            get { return "Feature"; }
        }

        public Geometry geometry
        {
            get;
            set;
        }

        public object properties
        {
            get;
            set;
        }

    }

    /// <summary>
    /// abstract GeoJSON base Geometry class
    /// </summary>
    public abstract class Geometry : GeoJsonObject
    {

    }

    /// <summary>
    /// GeoJSON Point
    /// </summary>
    public class Point : Geometry
    {
        private double x, y;

        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public override string type
        {
            get { return "Point"; }
        }

        public double[] coordinates
        {
            get
            {
                return new double[] { x, y };
            }
        }
    }

    /// <summary>
    /// GeoJSON LineString
    /// </summary>
    public class LineString : Geometry
    {
        private double[][] coords;


        public LineString(EGIS.ShapeFileLib.PointD[] points)
        {
            this.coords = new double[points.Length][];
            for (int n = points.Length - 1; n >= 0; --n)
            {
                coords[n] = new double[] { points[n].X, points[n].Y };
            }
        }

        public override string type
        {
            get { return "LineString"; }
        }

        public double[][] coordinates
        {
            get
            {
                return coords;
            }
        }

    }

    /// <summary>
    /// GeoJSON Polygon
    /// </summary>
    public class Polygon : Geometry
    {
        private double[][] coords;


        public Polygon(EGIS.ShapeFileLib.PointD[] points)
        {
            this.coords = new double[points.Length][];
            for (int n = points.Length - 1; n >= 0; --n)
            {
                coords[n] = new double[] { points[n].X, points[n].Y };
            }
        }

        public override string type
        {
            get { return "Polygon"; }
        }

        public List<double[][]> coordinates
        {
            get
            {
                List<double[][]> coordsList = new List<double[][]>();
                coordsList.Add(coords);
                return coordsList;
            }
        }

    }

    /// <summary>
    /// GeoJSON equivalent of google.maps.Data.StyleOptions object specification
    /// </summary>
    public class StyleOptions
    {
        public bool clickable = true;
        //public string   cursor          = " ";
        public string fillColor = "";
        public float fillOpacity = 1.0f;
        public string icon = "";
        public string strokeColor = "";
        public float strokeOpacity = 1.0f;
        public float strokeWeight = 1.0f;
        public string title = "";
        public bool visible = true;
        public int zIndex = 100;
    }



    #endregion

}
