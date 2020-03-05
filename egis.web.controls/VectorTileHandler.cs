using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Web;

using EGIS.Mapbox.Vector.Tile;
using System.IO;

namespace EGIS.Web.Controls
{
  

    public abstract class VectorTileHandler : IHttpHandler
    {

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            OnBeginRequest(context);
            try
            {
                ProcessGetTileRequest(context);
            }
            finally
            {
                OnEndRequest(context);
            }
        }

        #endregion


        #region virtual Properties and methods

        /// <summary>
        /// Whether or not to cache requested image requests on the server
        /// </summary>
        /// <remarks>Default value is true. Derived classes should override if neccessary</remarks>
        protected virtual bool CacheOnServer
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Name of the directory (relative to the handler) used to store cached images
        /// </summary>
        /// <remarks>Default value is "tilecache". Derived classes should override if a different directory is required.
        /// <para>Note it may be neccessary to grant write permissions on the directory</para></remarks>        
        protected virtual string CacheDirectory
        {
            get
            {
                return "vectortilecache";
            }
        }

        

        /// <summary>
        /// Creates path to a tile request if CacheOnServer is true
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        /// <remarks>Default name is &qt; CacheDirectory/tileX_tileY_zoom.png &qt;</remarks>
        protected virtual string CreateCachePath(HttpContext context, int tileX, int tileY, int zoom)
        {
            string cacheDirectory = context.Server.MapPath(CacheDirectory);
            return CreateCachePath(cacheDirectory, tileX, tileY, zoom);
        }

        private static string CreateCachePath(string cacheDirectory, int tileX, int tileY, int zoom)
        {
            string file = string.Format("{0}_{1}_{2}.mvt", new object[] { tileX, tileY, zoom });
            return System.IO.Path.Combine(cacheDirectory, file);
        }

        /// <summary>
        /// Abstract method to create the Map layers for the request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>Derived classes must implement the CreateMapLayers method</remarks>
        protected abstract List<ShapeFile> CreateMapLayers(HttpContext context);

       

        protected virtual void OnBeginRequest(HttpContext context)
        {
            //System.Diagnostics.Debug.WriteLine("begin request");
        }

        protected virtual void OnEndRequest(HttpContext context)
        {
            //System.Diagnostics.Debug.WriteLine("end request");

        }

        protected virtual int TileSize
        {
            get
            {
                return 1024;
            }
        }

        protected virtual int SimplificationPixelThreshold
        {
            get
            {
                return 1;
            }
        }



        protected virtual void ProcessGetTileRequest(HttpContext context)
        {
            DateTime dts = DateTime.Now;

            int w = 256 * 3;
            int h = 256 * 3;
            int tileX = 0, tileY = 0, zoomLevel = 0;
            PointD centerPoint = PointD.Empty;
            double zoom = -1;

            bool foundCompulsoryParameters = false;
            if (int.TryParse(context.Request["tx"], out tileX))
            {
                if (int.TryParse(context.Request["ty"], out tileY))
                {
                    if (int.TryParse(context.Request["zoom"], out zoomLevel))
                    {
                        TileUtil.NormaliseTileCoordinates(ref tileX, ref tileY, zoomLevel);
                        centerPoint = TileUtil.GetMercatorCenterPointFromTile(tileX, tileY, zoomLevel);
                        zoom = TileUtil.ZoomLevelToScale(zoomLevel);
                        foundCompulsoryParameters = true;
                    }
                }
            }

            if (!foundCompulsoryParameters) throw new InvalidOperationException("compulsory parameters 'tx','ty' or 'zoom' missing");


            string cachePath = "";
            bool useCache = CacheOnServer;

            if (useCache) cachePath = CreateCachePath(context, tileX, tileY, zoomLevel);

            if (string.IsNullOrEmpty(cachePath)) useCache = false;

            context.Response.ContentType = "application/vnd.mapbox-vector-tile";
            //is the tile cached on the server?
            if (useCache && System.IO.File.Exists(cachePath))
            {
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));
                context.Response.WriteFile(cachePath);
                context.Response.Flush();
                return;
            }

            //render the tile
            List<ShapeFile> mapLayers = CreateMapLayers(context);
            if (mapLayers == null) throw new InvalidOperationException("No Map Layers");


            List<VectorTileLayer> tileLayers = new List<VectorTileLayer>();

            lock (EGIS.ShapeFileLib.ShapeFile.Sync)
            {
                foreach (ShapeFile shapeFile in mapLayers)
                {
                    if (shapeFile.ShapeType == ShapeType.PolyLine || shapeFile.ShapeType == ShapeType.PolyLineM)
                    {
                        var layer = ProcessLineStringTile(shapeFile, tileX, tileY, zoomLevel);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }                   
                    else if (shapeFile.ShapeType == ShapeType.Polygon || shapeFile.ShapeType == ShapeType.PolygonZ)
                    {
                        var layer = ProcessPolygonTile(shapeFile, tileX, tileY, zoomLevel);
                        if (layer.VectorTileFeatures != null && layer.VectorTileFeatures.Count > 0)
                        {
                            tileLayers.Add(layer);
                        }
                    }
                   
                }
            }

            if (tileLayers.Count == 0)
            {
                context.Response.StatusCode = 404;
                context.Response.Flush();
                return;
            }
           
               
            using (MemoryStream ms = new MemoryStream())
            {
                EGIS.Mapbox.Vector.Tile.VectorTileParser.Encode(tileLayers, ms);
                if (useCache)
                {
                    try
                    {
                        using (System.IO.FileStream fs = new FileStream(cachePath, FileMode.Create))
                        {
                            ms.WriteTo(fs);
                        }
                        
                    }
                    catch { }
                    finally
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                    }
                }
              
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));
                ms.WriteTo(context.Response.OutputStream);
            }
                
           
            context.Response.Flush();
        }



        #endregion


        #region private members

        private VectorTileLayer ProcessLineStringTile(ShapeFile shapeFile, int tileX, int tileY, int zoom)
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

            System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
            System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];

            PointD[] pointsBuffer = new PointD[1024];

            VectorTileLayer tileLayer = new VectorTileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
            

            if (indicies.Count > 0)
            {
                                
                foreach (int index in indicies)
                {
                    VectorTileFeature feature = new VectorTileFeature() { Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Geometry = new List<List<Coordinate>>(),
                    Attributes = new List<AttributeKeyValue>()};

                    //get the point data
                    var recordPoints = shapeFile.GetShapeDataD(index);
                    //var recordMeasures = shapeFile.GetShapeMDataD(index);

                    int partIndex = 0;
                    foreach (PointD[] points in recordPoints)
                    {
                        //double[] measures = recordMeasures[partIndex];
                        //convert to pixel coordinates;
                        if (pixelPoints.Length < points.Length)
                        {
                            pixelPoints = new System.Drawing.Point[points.Length + 10];
                            simplifiedPixelPoints = new System.Drawing.Point[points.Length + 10];
                            //simplifiedMeasures = new double[points.Length + 10];
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
                      
                        //output count may be zero for short records at low zoom levels as 
                        //the pixel coordinates wil be a single point after simplification
                        if (outputCount > 0)
                        {
                            List<int> clippedPoints = new List<int>();
                            List<int> parts = new List<int>();
                            //List<double> clippedMeasures = new List<double>();
                            //GeometryAlgorithms.PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts, simplifiedMeasures, clippedMeasures);
                            GeometryAlgorithms.PolyLineClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPoints, parts);                           

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
                    
                    if (feature.Geometry.Count > 0)
                    {
                        tileLayer.VectorTileFeatures.Add(feature);
                    }

                }
            }
            return tileLayer;
        }

        private VectorTileLayer ProcessPolygonTile(ShapeFile shapeFile, int tileX, int tileY, int zoom)
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

            System.Drawing.Point[] pixelPoints = new System.Drawing.Point[1024];
            System.Drawing.Point[] simplifiedPixelPoints = new System.Drawing.Point[1024];
            List<System.Drawing.Point> clippedPolygon = new List<System.Drawing.Point>();

            PointD[] pointsBuffer = new PointD[1024];

            VectorTileLayer tileLayer = new VectorTileLayer();
            tileLayer.Extent = (uint)tileSize;
            tileLayer.Version = 2;
            tileLayer.Name = !string.IsNullOrEmpty(shapeFile.Name) ? shapeFile.Name : System.IO.Path.GetFileNameWithoutExtension(shapeFile.FilePath);

            if (indicies.Count > 0)
            {
                foreach (int index in indicies)
                {
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

                        for (int n = 0; n < points.Length; ++n)
                        {
                            Int64 x, y;
                            TileUtil.LLToPixel(points[n], zoom, out x, out y, tileSize);
                            pixelPoints[n].X = (int)(x - tilePixelOffset.X);
                            pixelPoints[n].Y = (int)(y - tilePixelOffset.Y);
                        }

                        int outputCount = 0;
                        SimplifyPointData(pixelPoints, null, points.Length-1, simplificationFactor, simplifiedPixelPoints, null, ref pointsBuffer, ref outputCount);
                        simplifiedPixelPoints[outputCount++] = pixelPoints[points.Length - 1];
                        if (outputCount > 1)
                        {                                                        
                            GeometryAlgorithms.PolygonClip(simplifiedPixelPoints, outputCount, clipBounds, clippedPolygon);

                            if (clippedPolygon.Count > 0)
                            {
                                //output the clipped polygon                                                                                             
                                List<Coordinate> lineString = new List<Coordinate>();
                                feature.Geometry.Add(lineString);
                                for (int i = clippedPolygon.Count-1; i>=0;--i)
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


        private void SimplifyPointData(System.Drawing.Point[] points, double[] measures, int pointCount, int simplificationFactor, System.Drawing.Point[] reducedPoints, double[] reducedMeasures, ref PointD[] pointsBuffer, ref int reducedPointCount)
        {
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
                if(measures != null) reducedMeasures[n] = measures[reducedIndices[n]];
            }
        }


        #endregion

    }

}
