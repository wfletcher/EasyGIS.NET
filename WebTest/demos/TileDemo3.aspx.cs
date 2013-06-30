using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using EGIS.Web.Controls;
using System.Drawing;


namespace WebTest.demos
{
    public partial class TileDemo3 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //link the map to the pan control 
            
            MapPanControl1.SetMap(SFMap1);
        }

        /// <summary>
        /// event handler for button1. Note this method will not be called if the OnClientClick handler is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Button1_Click(object sender, EventArgs e)
        {
            SFMap1.HttpHandlerName = "TileDemo3Handler.ashx?rendertype=" + DropDownList1.SelectedIndex;            
        }

    }



}