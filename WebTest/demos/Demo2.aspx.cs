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
using System.Drawing;

namespace WebTest.demos
{
    public partial class Demo2 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RectangleF extent = this.SFMap1.Extent;
                if (extent != Rectangle.Empty)
                {
                    SFMap1.CenterPoint = new PointF(extent.Left + extent.Width / 2, extent.Top + extent.Height / 2);
                }
            }

            MapPanControl1.SetMap(SFMap1);


        }

        protected void Button1_Click(object sender, EventArgs e)
        {           
        }
    }
}
