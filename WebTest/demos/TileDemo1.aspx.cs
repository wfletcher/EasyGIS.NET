using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace WebTest.demos
{
    public partial class TileDemo1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                SFMap1.CenterPoint = EGIS.ShapeFileLib.ShapeFile.LLToMercator(new EGIS.ShapeFileLib.PointD(144.99, -37.8));
                SFMap1.Zoom = (float)EGIS.ShapeFileLib.TileUtil.ZoomLevelToScale(10);// 2500;
            }

           
        }



        protected void Button1_Click(object sender, EventArgs e)
        {
        
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
        //    AddCustomRenderSettingsToLayers(500);

        }

        
    }
}
