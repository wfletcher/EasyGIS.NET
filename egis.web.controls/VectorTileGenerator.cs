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

using EGIS.Mapbox.Vector.Tile;
using EGIS.ShapeFileLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace EGIS.Web.Controls
{
    /// <summary>
    /// Utility class to generate Vector Tile data from ShapeFile layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class can be combined with EGIS.Mapbox.Vector.Tile.VectorTileParser to create Mapbox vector tiles.
    /// </para>
    /// </remarks>
    /// <example> Sample code to create a Mapbox Vector Tile from a shapefile. 
    /// <code>        
///public void CreateMapboxTile(List&lt;ShapeFile&gt; mapLayers, string vectorTileFileName)
///{
///    //create a VectorTileGenerator
///    VectorTileGenerator tileGenerator = new VectorTileGenerator();
///    List&lt;VectorTileLayer&gt; tileLayers = tileGenerator.Generate(tileX, tileY, zoomLevel, mapLayers);
///    //encode the vector tile in Mapbox vector tile format
///    using (System.IO.FileStream fs = new System.IO.FileStream(vectorTileFileName, System.IO.FileMode.Create))
///    {
///        EGIS.Mapbox.Vector.Tile.VectorTileParser.Encode(tileLayers, fs);
///    }
///}
    /// </code>                                                   
    /// </example>
    public class VectorTileGenerator
    {

        /// <summary>
        /// delegate to return whether a feature should be output at a given zoom level and tile coordinates
        /// </summary>
        /// <param name="shapeFile"></param>
        /// <param name="recordIndex"></param>
        /// <param name="tileZ"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <returns></returns>
        public delegate bool OutputTileFeatureDelegate(ShapeFile shapeFile, int recordIndex, int tileZ, int tileX, int tileY);

        /// <summary>
        /// VectorTileGenerator constructor 
        /// </summary>
        public VectorTileGenerator()
        {
            TileSize = 512;
            SimplificationPixelThreshold = 1;
            OutputMeasureValues = false;
            MeasuresAttributeName = "_MValues";
        }

        /// <summary>
        /// The size of the vector tiles
        /// </summary>
        public int TileSize
        {
            get;
            set;
        }

        /// <summary>
        /// Simplification Threshold. Default is 1
        /// </summary>
        /// <remarks>
        /// This property will simplify geometry points when the vector data is generated at lower tile
        /// zoom levels. In general this property should not be changed from the default value of 1
        /// </remarks>
        public int SimplificationPixelThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// whether to output PolylineM Measures.
        /// </summary>
        public bool OutputMeasureValues
        {
            get;
            set;
        }

        /// <summary>
        /// Output Measures Attribute name. Default is "_MValues"
        /// </summary>
        public string MeasuresAttributeName
        {
            get;
            set;
        }


        #region ShapeFile Generate members

        /// <summary>
        /// Generates a Vector Tile from ShapeFile layers
        /// </summary>
        /// <param name="tileX">Tile X coordinate</param>
        /// <param name="tileY">Tile Y coordinate</param>
        /// <param name="zoomLevel">Tile zoom level</param>
        /// <param name="layers">List of ShapeFile layers</param>
        /// <param name="outputTileFeature">optional OutputTileFeatureDelegate which will be called with each record feature that will be added to the tile. This delegate is useful to exclude feaures at tile zoom levels</param>
        /// <returns></returns>
        public virtual List<VectorTileLayer> Generate(int tileX, int tileY, int zoomLevel, List<ShapeFile> layers, OutputTileFeatureDelegate outputTileFeature = null)
        {
                       
            List<VectorTileLayer> tileLayers = new List<VectorTileLayer>();

            lock (EGIS.ShapeFileLib.ShapeFile.Sync)
            {
                foreach (ShapeFile shapeFile in layers)
                {
                    if (shapeFile.ShapeType == ShapeType.PolyLine || shapeFile.ShapeType == ShapeType.PolyLineM)
                    {
                        var layer = ProcessLineStringTile(shapeFile, tileX, tileY, zoomLevel, outputTileFeature);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                    else if (shapeFile.ShapeType == ShapeType.Polygon || shapeFile.ShapeType == ShapeType.PolygonZ)
                    {
                        var layer = ProcessPolygonTile(shapeFile, tileX, tileY, zoomLevel, outputTileFeature);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                    else if (shapeFile.ShapeType == ShapeType.Point || shapeFile.ShapeType == ShapeType.MultiPoint ||
                             shapeFile.ShapeType == ShapeType.PointZ || shapeFile.ShapeType == ShapeType.PointM)
                    {
                        var layer = ProcessPointTile(shapeFile, tileX, tileY, zoomLevel, outputTileFeature);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                    else throw new NotImplementedException("Shape Type " + shapeFile.ShapeType + " not implemented yet");
                }
            }

            return tileLayers;
        }




    

        private VectorTileLayer ProcessLineStringTile(ShapeFile shapeFile, int tileX, int tileY, int zoom, OutputTileFeatureDelegate outputTileFeature)
        {
            int tileSize = TileSize;
            RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            List<int> indicies = new List<int>();
            shapeFile.GetShapeIndiciesIntersectingRect(indicies, tileBounds);
            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = -20,
                YMin = -20,
                XMax = tileSize + 20,
                YMax = tileSize + 20
            };
            bool outputMeasureValues = this.OutputMeasureValues && (shapeFile.ShapeType== ShapeType.PolyLineM || shapeFile.ShapeType == ShapeType.PolyLineZ);
            System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
            System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];
            double[] simplifiedMeasures = new double[1024];

            PointD[] pointsBuffer = new PointD[1024];

            VectorTileLayer tileLayer = new VectorTileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
			tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.FilePath);

			if (indicies.Count > 0)
            {

                foreach (int index in indicies)
                {
                    if (outputTileFeature != null && !outputTileFeature(shapeFile, index, zoom, tileX, tileY)) continue;

                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>()
                    };

                    //get the point data
                    var recordPoints = shapeFile.GetShapeDataD(index);
                    System.Collections.ObjectModel.ReadOnlyCollection<double[]> recordMeasures = null;

                    if(outputMeasureValues) recordMeasures = shapeFile.GetShapeMDataD(index);

                    List<double> outputMeasures = new List<double>();
                    int partIndex = 0;
                    foreach (PointD[] points in recordPoints)
                    {
                        double[] measures = recordMeasures != null ? recordMeasures[partIndex] : null;
                        //convert to pixel coordinates;
                        if (pixelPoints.Length < points.Length)
                        {
                            pixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedPixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedMeasures = new double[points.Length + 10];
                        }

                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            pixelPoints[n].X = (int)(x - tilePixelOffset.X);
                            pixelPoints[n].Y = (int)(y - tilePixelOffset.Y);
                        }

                        int outputCount = 0;
                        SimplifyPointData(pixelPoints, measures, points.Length, simplificationFactor, simplifiedPixelPoints, simplifiedMeasures, ref pointsBuffer, ref outputCount);

                        //output count may be zero for short records at low zoom levels as 
                        //the pixel coordinates wil be a single point after simplification
                        if (outputCount > 0)
                        {
                            List<int> clippedPoints = new List<int>();
                            List<int> parts = new List<int>();
                            
                            if (outputMeasureValues)
                            {
                                List<double> clippedMeasures = new List<double>();
                                GeometryAlgorithms.PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts, simplifiedMeasures, clippedMeasures);
                                outputMeasures.AddRange(clippedMeasures);
                            }                            
                            else
                            {
                                GeometryAlgorithms.PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts);
                            }
                            if (parts.Count > 0)
                            {
                                //output the clipped polyline
                                for (int n = 0; n < parts.Count; ++n)
                                {
                                    int index1 = parts[n];
                                    int index2 = n < parts.Count - 1 ? parts[n + 1] : clippedPoints.Count;

                                    List<Coordinate> lineString = new List<Coordinate>();
                                    feature.GeometryType = Tile.GeomType.LineString;
                                    feature.Geometry.Add(lineString);
                                    //clipped points store separate x/y pairs so there will be two values per measure
                                    for (int i = index1; i < index2; i += 2)
                                    {
                                        lineString.Add(new Coordinate(clippedPoints[i], clippedPoints[i + 1]));
                                    }
                                }
                            }
                        }
                        ++partIndex;
                    }

                    //add the record attributes
                    string[] fieldNames = shapeFile.GetAttributeFieldNames();
                    string[] values = shapeFile.GetAttributeFieldValues(index);
                    for (int n = 0; n < values.Length; ++n)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(fieldNames[n], values[n].Trim()));
                    }
                    if (outputMeasureValues)
                    {
                        string s = Newtonsoft.Json.JsonConvert.SerializeObject(outputMeasures, new DoubleFormatConverter(4));
                        feature.Attributes.Add(new AttributeKeyValue(this.MeasuresAttributeName, s));
                    }

                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }

                }
            }
            return tileLayer;
        }

        PointD[] pointsBuffer = new PointD[1024];
        System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
        System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];



        private VectorTileLayer ProcessPolygonTile(ShapeFile shapeFile, int tileX, int tileY, int zoom, OutputTileFeatureDelegate outputTileFeature)
        {
            int tileSize = TileSize;
            RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            List<int> indicies = new List<int>();
            shapeFile.GetShapeIndiciesIntersectingRect(indicies, tileBounds);
            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = -20,
                YMin = -20,
                XMax = tileSize + 20,
                YMax = tileSize + 20
            };

            List<System.Drawing.Point> clippedPolygon = new List<System.Drawing.Point>();

            
            VectorTileLayer tileLayer = new VectorTileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
            tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.FilePath);

            if (indicies.Count > 0)
            {
                foreach (int index in indicies)
                {
                    if (outputTileFeature != null && !outputTileFeature(shapeFile, index, zoom, tileX, tileY)) continue;

                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>(),
                        GeometryType = Tile.GeomType.Polygon
                    };

                    //get the point data
                    var recordPoints = shapeFile.GetShapeDataD(index);                    
                    int partIndex = 0;
                    foreach (PointD[] points in recordPoints)
                    {
                        //convert to pixel coordinates;
                        if (pixelPoints.Length < points.Length)
                        {
                            pixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedPixelPoints = new System.Drawing.Point[points.Length + 10];
                        }
                        int pointCount = 0;
                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            
                            pixelPoints[pointCount].X = (int)(x - tilePixelOffset.X);
                            pixelPoints[pointCount++].Y = (int)(y - tilePixelOffset.Y);
                            
                        }
                        ////check for duplicates points at end after they have been converted to pixel coordinates
                        ////polygons need at least 3 points so don't reduce less than this
                        //while(pointCount > 3 && (pixelPoints[pointCount-1] == pixelPoints[pointCount - 2]))
                        //{
                        //    --pointCount;
                        //}

                        int outputCount = 0;
                        SimplifyPointData(pixelPoints, null, pointCount, simplificationFactor, simplifiedPixelPoints, null, ref pointsBuffer, ref outputCount);                       
                        //simplifiedPixelPoints[outputCount++] = pixelPoints[pointCount-1];
                        
                        if (outputCount > 1)
                        {                            
                            GeometryAlgorithms.PolygonClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPolygon);

                            if (clippedPolygon.Count > 0)
                            {                               
                                //output the clipped polygon                                                                                             
                                List<Coordinate> lineString = new List<Coordinate>();
                                feature.Geometry.Add(lineString);
                                for (int i = clippedPolygon.Count - 1; i >= 0; --i)
                                {
                                    lineString.Add(new Coordinate(clippedPolygon[i].X, clippedPolygon[i].Y));
                                }
                            }
                        }
                        ++partIndex;
                    }

                    //add the record attributes
                    string[] fieldNames = shapeFile.GetAttributeFieldNames();
                    string[] values = shapeFile.GetAttributeFieldValues(index);
                    for (int n = 0; n < values.Length; ++n)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(fieldNames[n], values[n].Trim()));
                    }

                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }
                }
            }

            return tileLayer;
        }

        private VectorTileLayer ProcessPointTile(ShapeFile shapeFile, int tileX, int tileY, int zoom, OutputTileFeatureDelegate outputTileFeature)
        {
            int tileSize = TileSize;
            RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            List<int> indicies = new List<int>();
            shapeFile.GetShapeIndiciesIntersectingRect(indicies, tileBounds);
            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = -20,
                YMin = -20,
                XMax = tileSize + 20,
                YMax = tileSize + 20
            };
          

            VectorTileLayer tileLayer = new VectorTileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
            tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.FilePath);

            if (indicies.Count > 0)
            {
                foreach (int index in indicies)
                {
                    if (outputTileFeature != null && !outputTileFeature(shapeFile, index, zoom, tileX, tileY)) continue;

                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>(),
                        GeometryType = Tile.GeomType.Point
                    };

                    //output the pixel coordinates                                                                                             
                    List<Coordinate> coordinates = new List<Coordinate>();
                    //get the point data
                    var recordPoints = shapeFile.GetShapeDataD(index);                   
                    foreach (PointD[] points in recordPoints)
                    {                                               
                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            coordinates.Add(new Coordinate((int)(x - tilePixelOffset.X), (int)(y - tilePixelOffset.Y)));
                        }                                                                   
                    }
                    if (coordinates.Count > 0)
                    {
                        feature.Geometry.Add(coordinates);
                    }

                    //add the record attributes
                    string[] fieldNames = shapeFile.GetAttributeFieldNames();
                    string[] values = shapeFile.GetAttributeFieldValues(index);
                    for (int n = 0; n < values.Length; ++n)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(fieldNames[n], values[n].Trim()));
                    }

                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }
                }
            }

            return tileLayer;
        }


        private void SimplifyPointData(System.Drawing.Point[] points, double[] measures, int pointCount, int simplificationFactor, System.Drawing.Point[] reducedPoints, double[] reducedMeasures, ref PointD[] pointsBuffer, ref int reducedPointCount)
        {
            System.Drawing.Point endPoint = points[pointCount - 1];
            double endMeasure = measures!= null ? measures[pointCount - 1] : 0;
            bool addEndpoint = endPoint == points[0];
            //check for duplicates points at end after they have been converted to pixel coordinates
            //polygons need at least 3 points so don't reduce less than this
            while (pointCount > 2 && (points[pointCount - 1] == points[0]))
            {
                --pointCount;
            }

            if (pointCount <= 2)
            {
                reducedPoints[0] = points[0];
                if(measures != null) reducedMeasures[0] = measures[0];
                if (pointCount == 2)
                {
                    reducedPoints[1] = points[1];
                    if (measures != null) reducedMeasures[1] = measures[1];
                }
                reducedPointCount = pointCount;
                if (addEndpoint)
                {
                    reducedPoints[reducedPointCount] = endPoint;
                    if (measures != null) reducedMeasures[reducedPointCount] = endMeasure;
                    ++reducedPointCount;
                }
                return;
            }
            if (pointsBuffer.Length < pointCount)
            {
                pointsBuffer = new PointD[pointCount];
            }
            
            for (int n = 0; n < pointCount; ++n)
            {
                pointsBuffer[n].X = points[n].X;
                pointsBuffer[n].Y = points[n].Y;
            }
            List<int> reducedIndices = new List<int>();
            reducedPointCount = GeometryAlgorithms.SimplifyDouglasPeucker(pointsBuffer, reducedIndices, pointCount, simplificationFactor);
            for (int n = 0; n < reducedPointCount; ++n)
            {
                reducedPoints[n] = points[reducedIndices[n]];
                if (measures != null) reducedMeasures[n] = measures[reducedIndices[n]];
            }
            if (addEndpoint)
            {
                reducedPoints[reducedPointCount] = endPoint;
                if (measures != null) reducedMeasures[reducedPointCount] = endMeasure;
                ++reducedPointCount;
            }
        }

      


        #endregion


        public virtual List<VectorTileLayer> Generate(int tileX, int tileY, int zoomLevel, List<ISpatialDataSource> layers)
        {
            List<VectorTileLayer> tileLayers = new List<VectorTileLayer>();
            
            foreach (ISpatialDataSource spatialLayer in layers)
            {
                if (spatialLayer.GeometryType == GeometryType.PolyLine)
                {
                    var layer = ProcessLineStringTile(spatialLayer, tileX, tileY, zoomLevel);
                    if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                    {
                        tileLayers.Add(layer);
                    }
                }
                else if (spatialLayer.GeometryType == GeometryType.Polygon)
                {
                    var layer = ProcessPolygonTile(spatialLayer, tileX, tileY, zoomLevel);
                    if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                    {
                        tileLayers.Add(layer);
                    }
                }
                else if (spatialLayer.GeometryType == GeometryType.Point)
                {
                    var layer = ProcessPointTile(spatialLayer, tileX, tileY, zoomLevel );
                    if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                    {
                        tileLayers.Add(layer);
                    }
                }
                else throw new NotImplementedException("Shape Type " + spatialLayer.GeometryType + " not implemented yet");
            }
            

            return tileLayers;
        }

        private VectorTileLayer ProcessLineStringTile(ISpatialDataSource spatialLayer, int tileX, int tileY, int zoom)
        {
            int tileSize = TileSize;
            RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            using (IEnumerator<ISpatialData> data = spatialLayer.GetData(new BoundingBox()
            {
                MinX = tileBounds.Left,
                MinY = tileBounds.Top,
                MaxX = tileBounds.Right,
                MaxY = tileBounds.Bottom
            }))
            {

                GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
                {
                    XMin = -20,
                    YMin = -20,
                    XMax = tileSize + 20,
                    YMax = tileSize + 20
                };
                bool outputMeasureValues = this.OutputMeasureValues && spatialLayer.HasMeasures;
                System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
                System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];
                double[] simplifiedMeasures = new double[1024];

                PointD[] pointsBuffer = new PointD[1024];

                VectorTileLayer tileLayer = new VectorTileLayer();
                tileLayer.Extent = (uint)tileSize;
                tileLayer.Version = 2;
                tileLayer.Name = spatialLayer.Name;


                //int index = 0;
                //foreach (int index in indicies)
                while (data.MoveNext())
                {

                    ISpatialData spatialData = data.Current;

                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = spatialData.Id,
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>()
                    };

                    //get the point data
                    var recordPoints = spatialData.Geometry;

                    List<double[]> recordMeasures = null;

                    if (outputMeasureValues) recordMeasures = spatialData.Measures;

                    List<double> outputMeasures = new List<double>();
                    int partIndex = 0;
                    foreach (PointD[] points in recordPoints)
                    {
                        double[] measures = recordMeasures != null ? recordMeasures[partIndex] : null;
                        //convert to pixel coordinates;
                        if (pixelPoints.Length < points.Length)
                        {
                            pixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedPixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedMeasures = new double[points.Length + 10];
                        }

                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            pixelPoints[n].X = (int)(x - tilePixelOffset.X);
                            pixelPoints[n].Y = (int)(y - tilePixelOffset.Y);
                        }

                        int outputCount = 0;
                        SimplifyPointData(pixelPoints, measures, points.Length, simplificationFactor, simplifiedPixelPoints, simplifiedMeasures, ref pointsBuffer, ref outputCount);

                        //output count may be zero for short records at low zoom levels as 
                        //the pixel coordinates wil be a single point after simplification
                        if (outputCount > 0)
                        {
                            List<int> clippedPoints = new List<int>();
                            List<int> parts = new List<int>();

                            if (outputMeasureValues)
                            {
                                List<double> clippedMeasures = new List<double>();
                                GeometryAlgorithms.PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts, simplifiedMeasures, clippedMeasures);
                                outputMeasures.AddRange(clippedMeasures);
                            }
                            else
                            {
                                GeometryAlgorithms.PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts);
                            }
                            if (parts.Count > 0)
                            {
                                //output the clipped polyline
                                for (int n = 0; n < parts.Count; ++n)
                                {
                                    int index1 = parts[n];
                                    int index2 = n < parts.Count - 1 ? parts[n + 1] : clippedPoints.Count;

                                    List<Coordinate> lineString = new List<Coordinate>();
                                    feature.GeometryType = Tile.GeomType.LineString;
                                    feature.Geometry.Add(lineString);
                                    //clipped points store separate x/y pairs so there will be two values per measure
                                    for (int i = index1; i < index2; i += 2)
                                    {
                                        lineString.Add(new Coordinate(clippedPoints[i], clippedPoints[i + 1]));
                                    }
                                }
                            }
                        }
                        ++partIndex;
                    }

                    //add the record attributes
                    foreach (var keyValue in spatialData.Attributes)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(keyValue.Key, keyValue.Value));
                    }
                    if (outputMeasureValues)
                    {
                        string s = Newtonsoft.Json.JsonConvert.SerializeObject(outputMeasures, new DoubleFormatConverter(4));
                        feature.Attributes.Add(new AttributeKeyValue(this.MeasuresAttributeName, s));
                    }

                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }
                }
                return tileLayer;
            }

        }

        private VectorTileLayer ProcessPolygonTile(ISpatialDataSource spatialLayer, int tileX, int tileY, int zoom)
        {
            int tileSize = TileSize;
            RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            using (IEnumerator<ISpatialData> data = spatialLayer.GetData(new BoundingBox()
            {
                MinX = tileBounds.Left,
                MinY = tileBounds.Top,
                MaxX = tileBounds.Right,
                MaxY = tileBounds.Bottom
            }))
            {

                GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
                {
                    XMin = -20,
                    YMin = -20,
                    XMax = tileSize + 20,
                    YMax = tileSize + 20
                };

                System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
                System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];
                PointD[] pointsBuffer = new PointD[1024];

                List<System.Drawing.Point> clippedPolygon = new List<System.Drawing.Point>();

                VectorTileLayer tileLayer = new VectorTileLayer();
                tileLayer.Extent = (uint)tileSize;
                tileLayer.Version = 2;
                tileLayer.Name = spatialLayer.Name;

                while (data.MoveNext())
                {

                    ISpatialData spatialData = data.Current;

                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = spatialData.Id,
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>(),
                        GeometryType = Tile.GeomType.Polygon
                    };

                    //get the point data
                    var recordPoints = spatialData.Geometry;
                    int partIndex = 0;
                    foreach (PointD[] points in recordPoints)
                    {
                        //convert to pixel coordinates;
                        if (pixelPoints.Length < points.Length)
                        {
                            pixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedPixelPoints = new System.Drawing.Point[points.Length + 10];                            
                        }

                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            pixelPoints[n].X = (int)(x - tilePixelOffset.X);
                            pixelPoints[n].Y = (int)(y - tilePixelOffset.Y);
                        }

                        int outputCount = 0;
                        SimplifyPointData(pixelPoints, null, points.Length, simplificationFactor, simplifiedPixelPoints, null, ref pointsBuffer, ref outputCount);

                        if (outputCount > 1)
                        {
                            GeometryAlgorithms.PolygonClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPolygon);

                            if (clippedPolygon.Count > 0)
                            {
                                //output the clipped polygon                                                                                             
                                List<Coordinate> lineString = new List<Coordinate>();
                                feature.Geometry.Add(lineString);
                                for (int i = clippedPolygon.Count - 1; i >= 0; --i)
                                {
                                    lineString.Add(new Coordinate(clippedPolygon[i].X, clippedPolygon[i].Y));
                                }
                            }


                        }
                        ++partIndex;
                    }

                    //add the record attributes
                    foreach (var keyValue in spatialData.Attributes)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(keyValue.Key, keyValue.Value));
                    }
                    
                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }
                }
                return tileLayer;
            }

        }

        private VectorTileLayer ProcessPointTile(ISpatialDataSource spatialLayer, int tileX, int tileY, int zoom)
        {
            int tileSize = TileSize;
            RectangleD tileBounds = TileUtil.GetTileLatLonBounds(tileX, tileY, zoom, tileSize);
            //create a buffer around the tileBounds 
            tileBounds.Inflate(tileBounds.Width * 0.05, tileBounds.Height * 0.05);

            int simplificationFactor = Math.Min(10, Math.Max(1, SimplificationPixelThreshold));

            System.Drawing.Point tilePixelOffset = new System.Drawing.Point((tileX * tileSize), (tileY * tileSize));

            using (IEnumerator<ISpatialData> data = spatialLayer.GetData(new BoundingBox()
            {
                MinX = tileBounds.Left,
                MinY = tileBounds.Top,
                MaxX = tileBounds.Right,
                MaxY = tileBounds.Bottom
            }))
            {

                GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
                {
                    XMin = -20,
                    YMin = -20,
                    XMax = tileSize + 20,
                    YMax = tileSize + 20
                };

              
                VectorTileLayer tileLayer = new VectorTileLayer();
                tileLayer.Extent = (uint)tileSize;
                tileLayer.Version = 2;
                tileLayer.Name = spatialLayer.Name;

                while (data.MoveNext())
                {
                    ISpatialData spatialData = data.Current;
                    VectorTileFeature feature = new VectorTileFeature()
                    {
                        Id = spatialData.Id,
                        Geometry = new List<List<Coordinate>>(),
                        Attributes = new List<AttributeKeyValue>(),
                        GeometryType = Tile.GeomType.Point
                    };

                    //output the pixel coordinates     
                    List<Coordinate> coordinates = new List<Coordinate>();
                    var recordPoints = spatialData.Geometry;
                    foreach (PointD[] points in recordPoints)
                    {
                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            coordinates.Add(new Coordinate((int)(x - tilePixelOffset.X), (int)(y - tilePixelOffset.Y)));
                        }
                    }
                    if (coordinates.Count > 0)
                    {
                        feature.Geometry.Add(coordinates);
                    }
                      
                    //add the record attributes
                    foreach (var keyValue in spatialData.Attributes)
                    {
                        feature.Attributes.Add(new AttributeKeyValue(keyValue.Key, keyValue.Value));
                    }

                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }
                }
                return tileLayer;
            }

        }

    }

    class DoubleFormatConverter : JsonConverter
    {
        private string formatString = "{0:F2}";

        public DoubleFormatConverter(int decimalPlaces)
            : base()
        {
            decimalPlaces = Math.Max(0, decimalPlaces);
            formatString = "F" + decimalPlaces;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // writer.WriteRawValue($"{value:0.00}");
            //double doubleValue = (double)value;
            writer.WriteRawValue(((double)value).ToString(formatString, System.Globalization.CultureInfo.InvariantCulture));
        }

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }


    


}
