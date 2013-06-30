using System;
using System.Web;
using EGIS.Web.Controls;
using EGIS.ShapeFileLib;
using System.Collections.Generic;
using System.Drawing;


namespace WebTest.demos
{
    /// <summary>
    /// IHttpHandler handler for TiledSFMap Demo 3
    /// </summary>
    public class TileDemo3Handler : TiledMapHandler
    {

        protected override bool CacheOnServer
        {
            get
            {
                return false; //set false during testing
                //set true for performance increase when testing complete
            }
        }

        private static ICustomRenderSettings CreatePopulationRenderSettings(ShapeFile sf)
        {

            Color[] populationColors = new Color[]{Color.FromArgb(255,255,128),
                                                Color.FromArgb(220,225,65),
                                                Color.FromArgb(255,245,0),
                                                Color.FromArgb(255,128,0),
                                                Color.FromArgb(255,64,0),
                                                Color.FromArgb(255,0,0)};
            //synchronize the creation. Another thread may be attempting to render
            //the map or create a custom render settings and reading from the 
            //internal DBFReader is not thread safe
            lock (EGIS.ShapeFileLib.ShapeFile.Sync)
            {
                return CustomRenderSettingsUtil.CreateQuantileCustomRenderSettings(
                    sf.RenderSettings,
                    populationColors,
                    "POP90_SQMI");
            }

        }

        protected override List<ShapeFile> CreateMapLayers(HttpContext context)
        {
            List<ShapeFile> layers = new List<ShapeFile>();
            //load the shapefiles                
            string shapeFilePath = context.Server.MapPath("/demos/us_demo_files/counties.shp");
            ShapeFile sf = new ShapeFile(shapeFilePath);
            //set the field name used to label the shapes
            sf.RenderSettings.FieldName = "NAME";
            sf.RenderSettings.CustomRenderSettings = CreatePopulationRenderSettings(sf);
            layers.Add(sf);

            if (context.Request["getshape"] == null)
            {
                //set some CustomRenderSettings depending on the render type selected by the user
                int renderSettingsType = 0;
                int.TryParse(context.Request["rendertype"], out renderSettingsType);
                if (renderSettingsType == 0)
                {
                    layers[0].RenderSettings.CustomRenderSettings = CreatePopulationRenderSettings(layers[0]);
                }
                else if (renderSettingsType == 2)
                {
                    layers[0].RenderSettings.CustomRenderSettings = CustomRenderSettingsUtil.CreateRandomColorCustomRenderSettings(layers[0].RenderSettings, 1);
                }
                else if (renderSettingsType == 3)
                {
                    layers[0].RenderSettings.CustomRenderSettings = null;
                    //here you could select some records based on an SQL query from another table or perhaps
                    //pass in some other parameter and determine what to select
                    //to keep this example simple we will just select the first 100 records
                    int numRecordstoSelect = Math.Min(100, layers[0].RecordCount);
                    for (int n = 0; n < numRecordstoSelect; ++n)
                    {
                        layers[0].SelectRecord(n, true);
                    }
                }
                else if (renderSettingsType == 4)
                {
                    layers[0].RenderSettings.CustomRenderSettings = null;

                    //select records intersecting a circle
                    List<int> ind = new List<int>();
                    PointD centrePoint = new PointD(layers[0].Extent.Left + layers[0].Extent.Width / 2,
                    layers[0].Extent.Top + layers[0].Extent.Height / 2);
                    lock (ShapeFile.Sync) //shapefile not thread safe so need synchronise access. See Shapefile class description
                    {
                        layers[0].GetShapeIndiciesIntersectingCircle(ind, centrePoint, layers[0].Extent.Height / 4);
                    }
                    for (int n = 0; n < ind.Count; ++n)
                    {                            
                        layers[0].SelectRecord(ind[n], true);
                    }                   
                }
                else
                {
                    //not using any custom render settings
                    layers[0].RenderSettings.CustomRenderSettings = null;
                }
            }

            return layers;
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
        /// renderSettingsType parameter. the rendertype parameter is set in the client javascript when
        /// the combobox selection is changed</remarks>
        protected override string CreateCachePath(HttpContext context, int tileX, int tileY, int zoom)
        {
            int renderSettingsType = 0;
            int.TryParse(context.Request["rendertype"], out renderSettingsType);
            return CreateCachePath(context.Server.MapPath(CacheDirectory), tileX, tileY, zoom, renderSettingsType);
        }

        private static string CreateCachePath(string cacheDirectory, int tileX, int tileY, int zoom, int renderType)
        {
            string file = string.Format("{0}_{1}_{2}_{3}.png", new object[] { tileX, tileY, zoom, renderType });
            return System.IO.Path.Combine(cacheDirectory, file);
        }

        protected override void OnBeginRequest(HttpContext context)
        {
            base.OnBeginRequest(context);
        }
    }

}