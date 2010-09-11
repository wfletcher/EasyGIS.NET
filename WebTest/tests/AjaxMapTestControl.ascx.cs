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

namespace WebTest.tests
{
    public partial class AjaxMapTestControl : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                this.MapPanControl1.SetMap(this.SFMap1);
                LoadDropDownList();
            }
        }

        private void LoadDropDownList()
        {
            this.DropDownList1.Items.Clear();
            this.DropDownList1.Items.Add("~/demos/demo2.egp");
            this.DropDownList1.Items.Add("~/demos/victorian_accidents.egp");
        }

        public string Project
        {
            get
            {
                return this.SFMap1.ProjectName;
            }
            set
            {
                this.SFMap1.ProjectName = value;
            }
        }


        protected void Button1_Click(object sender, EventArgs e)
        {
            this.Project = this.DropDownList1.SelectedValue;
        }
 
    }
}