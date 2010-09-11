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
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            this.SFMap1.Zoom = (float)EGIS.ShapeFileLib.TileUtil.ZoomLevelToScale(8);
            SFMap1.CenterPoint = EGIS.ShapeFileLib.ShapeFile.LLToMercator(new EGIS.ShapeFileLib.PointD(144.99, -37.8));

        }
    }
}
