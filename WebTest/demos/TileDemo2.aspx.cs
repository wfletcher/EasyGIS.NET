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
    public partial class TileDemo2 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (TiledSFMap1.LayerCount > 0)
                {
                    System.Drawing.RectangleF r = TiledSFMap1.Extent;
                    TiledSFMap1.CenterPoint = EGIS.ShapeFileLib.ShapeFile.LLToMercator(new EGIS.ShapeFileLib.PointD(r.Left+r.Width*0.5f, r.Top+r.Height*0.5));
                    TiledSFMap1.Zoom = (float)EGIS.ShapeFileLib.TileUtil.ZoomLevelToScale(5);// 2500;
                }
            }
        }
    }
}
