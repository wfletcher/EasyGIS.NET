using System;
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
    public class EGPExampleMapHandler : TiledMapHandler
    {
        protected override Color MapBackgroundColor
        {
            get
            {
                return Color.White;
            }
        }
        protected override bool CacheOnServer
        {
            get
            {
                //return false; //set false during testing
                return true;
            }
        }

        private const string EGPName = "demo2.egp";
       
        protected override List<ShapeFile> CreateMapLayers(HttpContext context)
        {
            MapProject project = SFMap.ReadEGPProject(context.Server.MapPath(EGPName));
            List<ShapeFile> layers =  project.Layers;
            //update the layer's RenderSettings as the RenderSettings may have been created in wgs84 geodetic coordinates
            //byt we are going to render using Web Mercator 
            SFMap.UpdateRenderSettingsForWebMercator(layers, project.MapCoordinateReferenceSystem);
            return layers;
        }

       
        protected override void GetCustomTooltipText(HttpContext context, ShapeFile layer, int recordIndex, ref string tooltipText)
        {
            //override the default ToolTip text - we will just return all attributes for the record
            if (recordIndex >= 0)
            {
                string[] fieldNames = layer.RenderSettings.DbfReader.GetFieldNames();
                string[] values = layer.RenderSettings.DbfReader.GetFields(recordIndex);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("<table>");
                for (int n = 0; n < fieldNames.Length; ++n)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>").Append(fieldNames[n]).Append("</td>");
                    sb.Append("<td>").Append(values[n]).Append("</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
                tooltipText = sb.ToString();
            }

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
            return CreateCachePath(context.Server.MapPath(CacheDirectory), tileX, tileY, zoom, EGPName);
        }

        private static string CreateCachePath(string cacheDirectory, int tileX, int tileY, int zoom, string projectName)
        {
            string file = string.Format("{0}_{1}_{2}_{3}.png", new object[] { tileX, tileY, zoom, projectName });
            return System.IO.Path.Combine(cacheDirectory, file);
        }

    }

}
