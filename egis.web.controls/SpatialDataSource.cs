using EGIS.ShapeFileLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EGIS.Web.Controls
{
    public enum GeometryType { Point, PolyLine, Polygon };

    public struct BoundingBox
    {
        public double MinX;
        public double MinY;
        public double MaxX;
        public double MaxY;
    }

    public interface ISpatialDataSource
    {
        GeometryType GeometryType { get; }

        IEnumerator<ISpatialData> GetData(BoundingBox bounds);

        bool HasMeasures { get; }

        string Name { get; }

    }

    public interface ISpatialData
    {
        string Id { get; }
        List<PointD[]> Geometry { get; }
        List<double[]> Measures { get; }
        List<KeyValuePair<string, string>> Attributes { get; }
    }


    public class ShapeFileSpatialDataSource : ISpatialDataSource
    {
        private ShapeFile shapeFile;
        public ShapeFileSpatialDataSource(ShapeFile shapeFile)
        {
            this.shapeFile = shapeFile;

            if (shapeFile.ShapeType == ShapeType.PolyLine || shapeFile.ShapeType == ShapeType.PolyLineM)
            {
                this.GeometryType = GeometryType.PolyLine;
            }
            else if (shapeFile.ShapeType == ShapeType.Polygon || shapeFile.ShapeType == ShapeType.PolygonZ)
            {
                this.GeometryType = GeometryType.Polygon;
            }
            else if (shapeFile.ShapeType == ShapeType.Point || shapeFile.ShapeType == ShapeType.MultiPoint ||
                     shapeFile.ShapeType == ShapeType.PointZ || shapeFile.ShapeType == ShapeType.PointM)
            {
                this.GeometryType = GeometryType.Point;
            }

            this.Name = shapeFile.Name;
        }

        //private GeometryType geometryType;

        public GeometryType GeometryType
        {
            get;
            private set;
        }

        public IEnumerator<ISpatialData> GetData(BoundingBox bounds)
        {
            return new ShapeFileSpatialDataEnumerator(this.shapeFile, bounds);
        }

        public bool HasMeasures
        {
            get
            {
                return this.shapeFile.ShapeType == ShapeType.PolyLineM || this.shapeFile.ShapeType == ShapeType.PolyLineZ;
            }
        }

        public string Name
        {
            get;
            set;
        }
    }

    class SpatialData : ISpatialData
    {

        public string Id { get; set; }

        public List<PointD[]> Geometry
        {
            get;
            set;
        }

        public List<double[]> Measures
        {
            get;
            set;
        }

        public List<KeyValuePair<string, string>> Attributes
        {
            get;
            set;
        }
    }

    class ShapeFileSpatialDataEnumerator : IEnumerator<ISpatialData>
    {
        private ShapeFile shapeFile;
        private int currentIndex = -1;
        private List<int> indicies = new List<int>();

        private ISpatialData current = null;

        public ShapeFileSpatialDataEnumerator(ShapeFile shapeFile, BoundingBox bounds)
        {
            this.shapeFile = shapeFile;
            RectangleD r = RectangleD.FromLTRB(bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY);
            lock (EGIS.ShapeFileLib.ShapeFile.Sync)
            {
                shapeFile.GetShapeIndiciesIntersectingRect(indicies, r);
            }
        }

        public ISpatialData Current
        {
            get
            {
                return current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public void Dispose()
        {
            this.shapeFile = null;
        }

        public bool MoveNext()
        {
            if (++currentIndex >= indicies.Count)
            {
                return false;
            }
            else
            {
                // Set current box to next item in collection.
                SpatialData data = new SpatialData();
                lock (EGIS.ShapeFileLib.ShapeFile.Sync)
                {
                    data.Id = indicies[currentIndex].ToString(System.Globalization.CultureInfo.InvariantCulture);
                    data.Geometry = shapeFile.GetShapeDataD(indicies[currentIndex]).ToList();
                    if (shapeFile.ShapeType == ShapeType.PolyLineM)
                    {
                        data.Measures = shapeFile.GetShapeMDataD(indicies[currentIndex]).ToList();
                    }
                    else
                    {
                        data.Measures = null;
                    }
                    data.Attributes = new List<KeyValuePair<string, string>>();
                    string[] fieldNames = shapeFile.GetAttributeFieldNames();
                    string[] values = shapeFile.GetAttributeFieldValues(indicies[currentIndex]);

                    for (int n = 0; n < values.Length; ++n)
                    {
                        data.Attributes.Add(new KeyValuePair<string, string>(fieldNames[n], values[n].Trim()));
                    }
                }
                this.current = data;
            }
            return true;

        }

        public void Reset()
        {
            this.currentIndex = -1;
        }
    }


}
