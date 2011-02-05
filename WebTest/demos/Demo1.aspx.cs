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
using EGIS.ShapeFileLib;

namespace WebTest
{
    public partial class _Demo1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
            if (!IsPostBack)
            {
                SFMap1.CenterPoint = new PointF(144.95f, -37.8f);
                SFMap1.Zoom = 2500;
            }

            
        }
       
    }
    
}
