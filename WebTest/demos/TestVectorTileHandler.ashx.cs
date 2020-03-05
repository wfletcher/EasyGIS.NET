using EGIS.ShapeFileLib;
using EGIS.Web.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebTest
{
    /// <summary>
    /// Summary description for TestVectorTileHandler
    /// </summary>
    public class TestVectorTileHandler :  VectorTileHandler
    {

        protected override bool CacheOnServer
        {
            get
            {
                return true;
            }
        }

        protected override void OnBeginRequest(HttpContext context)
        {
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        }

        private const string MyLayersId = "MyVectorTiles";

        protected override List<ShapeFile> CreateMapLayers(HttpContext context)
        {
            List<ShapeFile> layers = context.Application[MyLayersId] as List<ShapeFile>;

            if (layers != null) return layers;

            layers = new List<ShapeFile>();
            //load the shapefiles                

            string shapeFilePath = context.Server.MapPath("/demos/new_hampshire_files/new_hampshire_natural.shp");
            ShapeFile sf = new ShapeFile(shapeFilePath);

            layers.Add(sf);

            shapeFilePath = context.Server.MapPath("/demos/new_hampshire_files/new_hampshire_highway.shp");
            sf = new ShapeFile(shapeFilePath);

            layers.Add(sf);

            shapeFilePath = context.Server.MapPath("/demos/demo2_files/j5505_nativevegetationareas.shp");
            sf = new ShapeFile(shapeFilePath);

            layers.Add(sf);


            context.Application[MyLayersId] = layers;

            return layers;
        }


    }
}