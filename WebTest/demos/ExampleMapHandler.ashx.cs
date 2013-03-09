using System;
using System.Web;
using EGIS.Web.Controls;
using EGIS.ShapeFileLib;
using System.Collections.Generic;
using System.Drawing;

namespace WebTest.demos
{
    
    public class ExampleMapHandler : TiledMapHandler
    {
                               
        protected override bool CacheOnServer
        {
            get
            {
                //return false; //set false during testing
                return true;
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
            
            //set some CustomRenderSettings depending on the render type selected by the user
            int renderSettingsType = 0;
            int.TryParse(context.Request["rendertype"], out renderSettingsType);
            if (renderSettingsType == 0)
            {
                layers[0].RenderSettings.CustomRenderSettings = CreatePopulationRenderSettings(layers[0]);
            }
            else if (renderSettingsType == 1)
            {
                layers[0].RenderSettings.CustomRenderSettings = null;
            }
            else
            {                
                layers[0].RenderSettings.CustomRenderSettings = CustomRenderSettingsUtil.CreateRandomColorCustomRenderSettings(layers[0].RenderSettings, 1);
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
        protected override string  CreateCachePath(HttpContext context, int tileX, int tileY, int zoom)
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
