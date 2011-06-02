using System;
using System.Data;
using System.Web;
using EGIS.Web.Controls;
using EGIS.ShapeFileLib;
using System.Collections.Generic;
using System.Drawing;

namespace WebTest.demos
{
    /// <summary>
    /// Summary description for EGPExampleMapHandler
    /// </summary>
    public class EGPMapTileHandler : TiledMapHandler
    {

        protected override bool CacheOnServer
        {
            get
            {
                //return false; //set false during testing
                return true;
            }
        }


        protected override List<ShapeFile> CreateMapLayers(HttpContext context)
        {
            string mapid = context.Request["mapid"];
            if (string.IsNullOrEmpty(mapid)) throw new InvalidOperationException("mapid parameters not set");
            MapProject project = SFMap.ReadEGPProject(context.Server.MapPath(mapid));
            return project.Layers;
        }

        /// <summary>
        /// override the CreateCachePath member
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        /// <remarks>This member is overriden because we want to create a unique cache path based on the 
        /// name of the egp project name</remarks>
        protected override string CreateCachePath(HttpContext context, int tileX, int tileY, int zoom)
        {
            string mapid = context.Request["mapid"];
            if (string.IsNullOrEmpty(mapid)) throw new InvalidOperationException("mapid parameters not set");            
            return CreateCachePath(context.Server.MapPath(CacheDirectory), tileX, tileY, zoom, mapid);
        }

        private static string CreateCachePath(string cacheDirectory, int tileX, int tileY, int zoom, string projectName)
        {
            string file = string.Format("{0}_{1}_{2}_{3}.png", new object[] { tileX, tileY, zoom, projectName });
            return System.IO.Path.Combine(cacheDirectory, file);
        }

    }

}
