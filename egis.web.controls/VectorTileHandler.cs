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

using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Web;

using EGIS.Mapbox.Vector.Tile;
using System.IO;

namespace EGIS.Web.Controls
{

    /// <summary>
    /// Generic IHttpHandler handler that serves Mapbox .mvt Vector Tiles 
    /// </summary>
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
        /// <remarks>Default name is  CacheDirectory/zoom_tileX_tileY.mvt </remarks>
        protected virtual string CreateCachePath(HttpContext context, int tileX, int tileY, int zoom)
        {
            string cacheDirectory = context.Server.MapPath(CacheDirectory);
            return CreateCachePath(cacheDirectory, tileX, tileY, zoom);
        }

        private static string CreateCachePath(string cacheDirectory, int tileX, int tileY, int zoom)
        {
            string file = string.Format("{0}_{1}_{2}.mvt", new object[] {zoom,  tileX, tileY });
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

        /// <summary>
        /// Vector Tile Size. Default is 512 x 512
        /// </summary>
        protected virtual int TileSize
        {
            get
            {
                return 512;
            }
        }

        /// <summary>
        /// Simplification Threshold. Default is 1
        /// </summary>
        /// <remarks>
        /// This property will simplify geometry points when the vector data is generated at lower tile
        /// zoom levels. In general this property should not be changed from the default value of 1
        /// </remarks>
        protected virtual int SimplificationPixelThreshold
        {
            get
            {
                return 1;
            }
        }

        protected virtual bool OutputTileFeature(ShapeFile shapeFile, int recordIndex, int tileZ, int tileX, int tileY)
        {
            return true;
        }

        protected virtual void ProcessGetTileRequest(HttpContext context)
        {
            DateTime dts = DateTime.Now;

            int w = 256 * 3;
            int h = 256 * 3;
            int tileX = 0, tileY = 0, zoomLevel = 0;
           
            bool foundCompulsoryParameters = false;
            if (int.TryParse(context.Request["tx"], out tileX))
            {
                if (int.TryParse(context.Request["ty"], out tileY))
                {
                    if (int.TryParse(context.Request["zoom"], out zoomLevel))
                    {
                        TileUtil.NormaliseTileCoordinates(ref tileX, ref tileY, zoomLevel);
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

            VectorTileGenerator tileGenerator = new VectorTileGenerator()
            {
                TileSize = this.TileSize,
                SimplificationPixelThreshold = this.SimplificationPixelThreshold
            };

            List<VectorTileLayer> tileLayers = tileGenerator.Generate(tileX, tileY, zoomLevel, mapLayers, this.OutputTileFeature);


            if (tileLayers.Count == 0)
            {
                context.Response.StatusCode = 404;
                context.Response.Flush();
                return;
            }
           
            //output the vectortile in Mapbox vector tile format   
            using (MemoryStream ms = new MemoryStream())
            {
                EGIS.Mapbox.Vector.Tile.VectorTileParser.Encode(tileLayers, ms);
                if (useCache)
                {
                    //save the encoded tile to our cache
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


     
    }

}
