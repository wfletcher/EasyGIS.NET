using System;
using System.Collections.Generic;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using EGIS.ShapeFileLib;
using System.IO;


namespace EGIS.Web.Controls
{
    /// <summary>
    /// Generic IHttpHandler for handling TiledMap requests
    /// </summary>
    public abstract class TiledMapHandler : IHttpHandler
    {

        #region .net core middleware

            //below code needed if migrating this handler to .net core

            // ASP.NET Core middleware migrated from a handler
            // Must have constructor with this signature, otherwise exception at run time
            //public TiledMapHandler(RequestDelegate next)
            //{
            //    // This is an HTTP Handler, so no need to store next
            //}

            //public async Task Invoke(HttpContext context)
            //{
            //    OnBeginRequest(context);
            //    try
            //    {
            //        ProcessRequestCore(context);
            //    }
            //    finally
            //    {
            //        OnEndRequest(context);
            //    }
            //}

           

        //public static class MyHandlerExtensions
        //{
        //    public static IApplicationBuilder UseMyHandler(this IApplicationBuilder builder)
        //    {
        //        return builder.UseMiddleware<MyHandlerMiddleware>();
        //    }
        //}
    


        #endregion


        #region IHttpHandler Members

        public virtual bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {            
            OnBeginRequest(context);
            try
            {
                ProcessRequestCore(context);
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
                return "tilecache";
            }
        }

        /// <summary>
        /// Color used to render the background of map tile images
        /// </summary>
        protected virtual Color MapBackgroundColor
        {
            get
            {
                return Color.LightGray;
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
            string file = string.Format("{0}_{1}_{2}.png", new object[] { tileX, tileY, zoom });
            return System.IO.Path.Combine(cacheDirectory, file);
        }

        /// <summary>
        /// Abstract method to create the Map layers for the request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>Derived classes must implement the CreateMapLayers method</remarks>
        protected abstract List<ShapeFile> CreateMapLayers(HttpContext context);

        /// <summary>
        /// virtual method to create the Map layers and custom render settings for the request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>The default method just calls CreateMapLayers(context) and sets renderSettings to null</para>
        /// <para>If you are using custom render settings layers.Count must equal renderSettings.Count. If renderSettings is null
        /// then Custom Render settings will not be used. If any items in renbderSettings are null then the corresponding layer will not
        /// use CustomRenderSettings</para>
        /// <para>
        /// Not used yet!
        /// </para>
        /// </remarks>
        //protected /*virtual*/ void CreateMapLayers(HttpContext context, out List<ShapeFile> layers, out List<ICustomRenderSettings> renderSettings)
        //{
        //    layers = CreateMapLayers(context);
        //    renderSettings = null;

        //}

        protected virtual void OnBeginRequest(HttpContext context)
        {
            //System.Diagnostics.Debug.WriteLine("begin request");
        }

        protected virtual void OnEndRequest(HttpContext context)
        {
            //System.Diagnostics.Debug.WriteLine("end request");

        }

        protected virtual bool SupportTransparency
        {
            get { return true; }
        }

        #endregion


        #region private and static members

        private static void RenderMap(Graphics g, List<EGIS.ShapeFileLib.ShapeFile> layers, int w, int h, PointD centerPt, double zoom)
        {
            lock (EGIS.ShapeFileLib.ShapeFile.Sync)
            {
                var crs = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);
                for (int n = 0; n < layers.Count; n++)
                {
                    EGIS.ShapeFileLib.ShapeFile sf = layers[n];
                    //render layers using Mercator ProjectionType for the tiled images
                    sf.Render(g, new Size(w, h), centerPt, zoom, ProjectionType.None, crs);
                }   
                
            }
        }

        private void ProcessRequestCore(HttpContext context)
        {
            if (context.Request.Params["getshape"] != null || context.Request.Params["gettooltip"] != null)
            {
                ProcessGetShapeRequest(context);                
            }
            else if (context.Request.Params["getattributes"] != null)
            {
                ProcessGetShapeAttributesRequest(context);
            }
            else
            {
                ProcessGetTileRequest(context);
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
                        //centerPoint = TileUtil.GetMercatorCenterPointFromTile(tileX, tileY, zoomLevel);
                        //zoom = TileUtil.ZoomLevelToScale(zoomLevel);
                        centerPoint = TileUtil.GetWebMercatorCenterPointFromTile(tileX, tileY, zoomLevel);
                        zoom = TileUtil.ZoomLevelToWebMercatorScale(zoomLevel);
                        foundCompulsoryParameters = true;
                    }
                }
            }

            if (!foundCompulsoryParameters) throw new InvalidOperationException("compulsory parameters 'tx','ty' or 'zoom' missing");


            string cachePath = "";
            bool useCache = CacheOnServer;

            if (useCache) cachePath = CreateCachePath(context, tileX, tileY, zoomLevel);

            if (string.IsNullOrEmpty(cachePath)) useCache = false;

            context.Response.ContentType = "image/x-png";
            //is the image cached on the server?
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
            //check if any layers are Point layers
            bool containsPointLayer = false;
            for (int n = mapLayers.Count - 1; n >= 0; --n)
            {
                if (mapLayers[n].ShapeType == ShapeType.Point || mapLayers[n].ShapeType == ShapeType.PointM || mapLayers[n].ShapeType == ShapeType.PointZ)
                {
                    containsPointLayer = true;
                }
            }
            if (containsPointLayer)
            {
                //draw to an image w x h so that labels overlapping tiles are rendered
                Bitmap bm = new Bitmap(256, 256, SupportTransparency ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
                Bitmap bm2 = new Bitmap(w, h, SupportTransparency ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
                try
                {
                    Graphics g = Graphics.FromImage(bm);
                    Graphics g2 = Graphics.FromImage(bm2);
                    try
                    {

                        g2.Clear(MapBackgroundColor);
                        RenderMap(g2, mapLayers, w, h, centerPoint, zoom);
                        g.DrawImage(bm2, Rectangle.FromLTRB(0, 0, 256, 256), Rectangle.FromLTRB(256, 256, 512, 512), GraphicsUnit.Pixel);

                        //perform custom painting
                    }
                    finally
                    {
                        g.Dispose();
                        g2.Dispose();
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        if (useCache)
                        {
                            try
                            {
                                bm.Save(cachePath, ImageFormat.Png);
                            }
                            catch { }
                        }
                        bm.Save(ms, ImageFormat.Png);

                        context.Response.Cache.SetCacheability(HttpCacheability.Public);
                        context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));
                        ms.WriteTo(context.Response.OutputStream);
                    }
                }
                finally
                {
                    bm.Dispose();
                    bm2.Dispose();
                }
            }
            else
            {
                Bitmap bm = new Bitmap(256, 256, SupportTransparency ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
                try
                {
                    Graphics g = Graphics.FromImage(bm);
                    try
                    {

                        g.Clear(MapBackgroundColor);
                        RenderMap(g, mapLayers, 256, 256, centerPoint, zoom);

                        //perform custom painting
                    }
                    finally
                    {
                        g.Dispose();
                    }
                    using (MemoryStream ms = new MemoryStream())
                    {
                        if (useCache)
                        {
                            try
                            {
                                bm.Save(cachePath, ImageFormat.Png);
                            }
                            catch { }
                        }
                        bm.Save(ms, ImageFormat.Png);

                        context.Response.Cache.SetCacheability(HttpCacheability.Public);
                        context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));
                        ms.WriteTo(context.Response.OutputStream);
                    }
                }
                finally
                {
                    bm.Dispose();
                }
            }
            context.Response.Flush();
        }


        protected virtual void ProcessGetShapeRequest(HttpContext context)
        {
            double x, y;
            PointD centerPoint = PointD.Empty;
            int zoomLevel = -1;
            string dcrsSessionKey;
            string tooltipText = "";

            if (!double.TryParse(context.Request["x"], out x))
            {
                throw new ArgumentException("invalid x point");
            }
            if (!double.TryParse(context.Request["y"], out y))
            {
                throw new ArgumentException("invalid y point");
            }
            centerPoint = new PointD(x, y);

            //V3.3 zoom now sent as zoom level
            if (!int.TryParse(context.Request["zoom"], out zoomLevel))
            {
                throw new ArgumentException("zoom");
            }
            
            dcrsSessionKey = context.Request["dcrs"];

            int layerIndex=-1;
            int recordIndex = -1;
            List<ShapeFile> layers = CreateMapLayers(context);
            if (layers != null && layers.Count>0)
            {
                lock (EGIS.ShapeFileLib.ShapeFile.Sync)
                {
                    tooltipText = LocateShape(centerPoint, layers, zoomLevel, ref layerIndex, ref recordIndex);                    
                }
            }

            context.Response.ContentType = "text/plain";
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));


            if (!string.IsNullOrEmpty(tooltipText))
            {
                context.Response.Write("true\n");
                context.Response.Write(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}\n", x, y));
                if (layerIndex >= 0 && recordIndex >= 0)
                {
                    lock (EGIS.ShapeFileLib.ShapeFile.Sync)
                    {
                        GetCustomTooltipText(context, layers[layerIndex], recordIndex, ref tooltipText);
                    }
                }
                context.Response.Write(tooltipText);
            }
            else
            {
                context.Response.Write("false\n");
            }
            context.Response.Flush();
            //context.Response.End();
        }

        protected virtual void GetCustomTooltipText(HttpContext context, ShapeFile layer, int recordIndex, ref string tooltipText)
        {
            
        }

        private static string LocateShape(PointD pt, List<EGIS.ShapeFileLib.ShapeFile> layers, int zoomLevel, ref int layerIndex, ref int recordIndex)
        {
            //changed V3.3 - coords now sent in lat/long
            //convert pt to lat long from merc
            //pt = ShapeFile.MercatorToLL(pt);
            //System.Diagnostics.Debug.WriteLine(pt);
            double zoom = TileUtil.ZoomLevelToScale(zoomLevel);            
            double delta = 8.0 / zoom;

            //pt is geodetic lat/lon coords
            var crsWgs84 = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode);
                       
            for (int l = layers.Count - 1; l >= 0; l--)
            {
                RectangleD extent = layers[l].Extent;
                extent.Inflate(delta, delta);
                bool useToolTip = (layers[l].RenderSettings != null && layers[l].RenderSettings.UseToolTip);
                bool useCustomToolTip = (useToolTip && layers[l].RenderSettings.CustomRenderSettings != null && layers[l].RenderSettings.CustomRenderSettings.UseCustomTooltips);
                if ((extent.Contains(pt) || layers[l].ShapeType == ShapeType.Point
                    || layers[l].ShapeType == ShapeType.PointM || layers[l].ShapeType == ShapeType.PointZ)
                    && layers[l].IsVisibleAtZoomLevel((float)zoom) && useToolTip)
                {
                    int selectedIndex = layers[l].GetShapeIndexContainingPoint(pt, delta, crsWgs84);
                    if (selectedIndex >= 0)
                    {
                        layerIndex = l;
                        recordIndex = selectedIndex;
                        if (useCustomToolTip)
                        {
                            return layers[l].RenderSettings.CustomRenderSettings.GetRecordToolTip(selectedIndex);
                        }
                        else
                        {
                            string s = "record : " + selectedIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            if (layers[l].RenderSettings.ToolTipFieldIndex >= 0)
                            {
                                string temp = layers[l].RenderSettings.DbfReader.GetField(selectedIndex, layers[l].RenderSettings.ToolTipFieldIndex).Trim();
                                if (temp.Length > 0)
                                {
                                    s += "<br/>" + temp;
                                }
                            }
                            return s;
                        }
                    }
                }
            }            
            return null;
        }


        #endregion


        #region Get shape Attributes

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        protected virtual void ProcessGetShapeAttributesRequest(HttpContext context)
        {
            double x, y;
            PointD centerPoint = PointD.Empty;
            int zoomLevel = -1;

            if (!double.TryParse(context.Request["x"], out x))
            {
                throw new ArgumentException("invalid x point");
            }
            if (!double.TryParse(context.Request["y"], out y))
            {
                throw new ArgumentException("invalid y point");
            }
            centerPoint = new PointD(x, y);

            if (!int.TryParse(context.Request["zoom"], out zoomLevel))
            {
                throw new ArgumentException("zoom");
            }

            int layerIndex = -1;
            int recordIndex = -1;
            //List<ShapeFile> layers;
            //List<ICustomRenderSettings> renderSettings;
            //CreateMapLayers(context, out layers, out renderSettings);
            List<ShapeFile> layers = CreateMapLayers(context);
            bool shapeLocated = false;
            if (layers != null && layers.Count > 0)
            {
                lock (EGIS.ShapeFileLib.ShapeFile.Sync)
                {
                    shapeLocated = FindShape(centerPoint, layers, zoomLevel, ref layerIndex, ref recordIndex);
                }
            }

            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetExpires(DateTime.Now.AddDays(7));


            if (shapeLocated && layerIndex >= 0 && recordIndex >= 0)
            {
                string json = "";
                lock (EGIS.ShapeFileLib.ShapeFile.Sync)
                {
                    List<KeyValuePair<string,string>> attributes= GetRecordAttributes(context, layers[layerIndex], recordIndex);
                    //json = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(attributes);
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(attributes);//, new DoubleFormatConverter(4));

                }
                context.Response.Write(json);
            }
            else
            {
                context.Response.Write("[]");
            }
            context.Response.Flush();
            //context.Response.End();
        }

        private static bool FindShape(PointD pt, List<EGIS.ShapeFileLib.ShapeFile> layers, int zoomLevel, ref int layerIndex, ref int recordIndex)
        {
            //convert zoomLevel to map scale            
            double scale = TileUtil.ZoomLevelToScale(zoomLevel);// ZoomLevelToWebMercatorScale(zoomLevel);
            double delta = 8.0 / scale;
           
            //pt is geodetic lat/lon coords
            var crsWgs84 = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode);            

            for (int l = layers.Count - 1; l >= 0; l--)
            {
                RectangleD extent = layers[l].Extent;
                extent.Inflate(delta, delta);
                if ((extent.Contains(pt) || layers[l].ShapeType == ShapeType.Point
                        || layers[l].ShapeType == ShapeType.PointM || layers[l].ShapeType == ShapeType.PointZ)
                        && layers[l].IsVisibleAtZoomLevel((float)scale))
                {
                    int selectedIndex = layers[l].GetShapeIndexContainingPoint(pt, delta,crsWgs84);
                    if (selectedIndex >= 0)
                    {
                        layerIndex = l;
                        recordIndex = selectedIndex;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// returns a list of key/values for each attributes of a record
        /// </summary>
        /// <param name="context"></param>
        /// <param name="layer"></param>
        /// <param name="recordIndex"></param>
        /// <returns></returns>
        /// <remarks>
        /// The default implementation will return a key/value for every attributes.
        /// If you wish to add additional attributes, perhaps by joining on another table, or exclude
        /// some attributes override this method
        /// </remarks>
        protected virtual List<KeyValuePair<string, string>> GetRecordAttributes(HttpContext context, ShapeFile layer, int recordIndex)
        {
            List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();
            if (recordIndex >= 0)
            {
                string[] fieldNames = layer.RenderSettings.DbfReader.GetFieldNames();
                string[] values = layer.RenderSettings.DbfReader.GetFields(recordIndex);

                for (int n = 0; n < fieldNames.Length; ++n)
                {
                    keyValues.Add(new KeyValuePair<string, string>(fieldNames[n].Trim(), values[n].Trim()));
                }
            }
            return keyValues;
        }


        #endregion


    }

}
