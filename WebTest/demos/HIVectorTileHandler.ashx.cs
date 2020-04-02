using EGIS.ShapeFileLib;
using EGIS.Web.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebTest.demos
{
    /// <summary>
    /// Summary description for HIVectorTileHandler
    /// </summary>
    public class HIVectorTileHandler : VectorTileHandler
    {

        protected override bool CacheOnServer
        {
            get
            {
                return true;
            }
        }

        protected override string CacheDirectory
        {
            get
            {
                return "hi_vectortilecache";
            }
        }
        
        protected override void OnBeginRequest(HttpContext context)
        {
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        }

        private const string MyLayersId = "MyVectorTiles";

        protected override List<ShapeFile> CreateMapLayers(HttpContext context)
        {
            //what zoom level is the request?
            int zoomLevel = 0;
            int.TryParse(context.Request["zoom"], out zoomLevel);

            string zoomLayersId = MyLayersId;

            List<ShapeFile> layers = null;

            if (zoomLevel <= 10)
            {
                zoomLayersId += "_section";
                layers = context.Application[zoomLayersId] as List<ShapeFile>;
                if (layers != null) return layers;

                layers = new List<ShapeFile>();                          
                string shapeFilePath = context.Server.MapPath("/demos/HI_data/data/section_data.shp");
                ShapeFile sf = new ShapeFile(shapeFilePath);
                layers.Add(sf);
            }
            else if (zoomLevel <= 12)
            {
                zoomLayersId += "_km";
                layers = context.Application[zoomLayersId] as List<ShapeFile>;
                if (layers != null) return layers;

                layers = new List<ShapeFile>();
                string shapeFilePath = context.Server.MapPath("/demos/HI_data/data/1km_data.shp");
                ShapeFile sf = new ShapeFile(shapeFilePath);
                layers.Add(sf);
            }
            else if (zoomLevel <= 15)
            {
                zoomLayersId += "_100m";
                layers = context.Application[zoomLayersId] as List<ShapeFile>;
                if (layers != null) return layers;

                layers = new List<ShapeFile>();
                string shapeFilePath = context.Server.MapPath("/demos/HI_data/data/100m_data.shp");
                ShapeFile sf = new ShapeFile(shapeFilePath);
                layers.Add(sf);
            }
            else
            {
                layers = context.Application[zoomLayersId] as List<ShapeFile>;
                if (layers != null) return layers;

                layers = new List<ShapeFile>();
                string shapeFilePath = context.Server.MapPath("/demos/HI_data/data/10m_data.shp");
                ShapeFile sf = new ShapeFile(shapeFilePath);
                layers.Add(sf);
            }

            
            context.Application[MyLayersId] = layers;

            return layers;
        }

    }
}