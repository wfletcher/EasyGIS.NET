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

        

        private void AddCustomRenderSettingsToLayers(int n)
        {
            ShapeFile sf = SFMap1.GetLayer(11);
            SFMap1.SetCustomRenderSettings(11, new TestCustomRenderSettings(SFMap1.GetLayer(11).RenderSettings, "TURPOP2006", n));
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            AddCustomRenderSettingsToLayers(100);

        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            AddCustomRenderSettingsToLayers(500);
        }    
    
    }


    class TestCustomRenderSettings : EGIS.ShapeFileLib.ICustomRenderSettings
    {

        private string fieldName;
        private int fieldIndex = -1;
        private float maxValue;

        private RenderSettings renderSettings;

        #region ICustomRenderSettings Members

        public TestCustomRenderSettings(RenderSettings renderSettings, string fieldName, float maxValue)
        {
            this.maxValue = maxValue;
            this.renderSettings = renderSettings;
            this.fieldName = fieldName;
            string[] fieldNames = renderSettings.DbfReader.GetFieldNames();

            int index = fieldNames.Length - 1;
            while (index >= 0)
            {
                if (string.Compare(fieldNames[index].Trim(), fieldName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    break;
                }
                index--;
            }
            fieldIndex = index;

        }

        public System.Drawing.Color GetRecordFillColor(int recordNumber)
        {
            if (fieldIndex < 0) return renderSettings.FillColor; ;
            float f;
            if (float.TryParse(this.renderSettings.DbfReader.GetField(recordNumber, this.fieldIndex).Trim(), out f))
            {
                f /= maxValue;
                if (f >= 1) f = 1;
                if (f < 0) f = 0;
                int c = (int)Math.Round(f * 255);
                return System.Drawing.Color.FromArgb(10, 10, c);

            }
            else
            {
                return renderSettings.FillColor;
            }
        }



        public bool RenderShape(int recordNumber)
        {
            return true;
        }



        public bool UseCustomTooltips
        {
            get
            {
                return false;
            }
        }

        public string GetRecordToolTip(int recordNumber)
        {
            return null;
        }

        #endregion

        #region ICustomRenderSettings Members


        public Color GetRecordOutlineColor(int recordNumber)
        {
            //throw new Exception("The method or operation is not implemented.");
            return renderSettings.OutlineColor;
        }

        public Color GetRecordFontColor(int recordNumber)
        {
            //throw new Exception("The method or operation is not implemented.");
            return renderSettings.FontColor;
        }

        public bool UseCustomImageSymbols
        {
            //get { throw new Exception("The method or operation is not implemented."); }
            get
            {
                return false;
            }
        }

        public System.Drawing.Image GetRecordImageSymbol(int recordNumber)
        {
            //throw new Exception("The method or operation is not implemented.");
            return renderSettings.GetImageSymbol();
        }

        #endregion
    }

}
