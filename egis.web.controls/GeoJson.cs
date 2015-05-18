using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGIS.Web.Controls
{

    #region "GeoJson classes"

    public abstract class GeoJsonObject
    {
        public abstract string type
        {
            get;
        }

    }


    public class FeatureCollection : GeoJsonObject
    {
        public override string type
        {
            get { return "FeatureCollection"; }
        }

        public List<Feature> features = new List<Feature>();

    }

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

    public abstract class Geometry : GeoJsonObject
    {

    }

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
    /// equivalent of google.maps.Data.StyleOptions object specification
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
