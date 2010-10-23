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
using EGIS.Web.Controls;
using System.Drawing;

namespace WebTest.demos
{
    public partial class Demo3 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //link the map to the pan control 
            EGIS.ShapeFileLib.ShapeFile.MapFilesInMemory = false;
            MapPanControl1.SetMap(SFMap1);
        }

        
        protected void Button1_Click(object sender, EventArgs e)
        {
            if (DropDownList1.SelectedIndex == 1)
            {
                SetupPopulationDensity();
            }
            else if (DropDownList1.SelectedIndex == 2)
            {
                SetupAverageSalePrice();
            }
            else if (DropDownList1.SelectedIndex == 3)
            {
                SetupDivorced();
            }
            else if (DropDownList1.SelectedIndex == 4)
            {
                SetupMedianRent();
            }            
        }
        
        //simple method to return quintile quantiles from an array of double samples        
        private static double[] GetQuintiles(double[] samples)
        {
            Array.Sort(samples);
            double[] quintiles = new double[4];
            quintiles[0] = samples[(int)(samples.Length * 0.2)];
            quintiles[1] = samples[(int)(samples.Length * 0.4)];
            quintiles[2] = samples[(int)(samples.Length * 0.6)];
            quintiles[3] = samples[(int)(samples.Length * 0.8)];
            return quintiles;
        }

        private void SetupMedianRent()
        {
            TooltipHeaderFieldNamePair[] tooltipPairs;
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("State: ", "STATE_NAME"),
                new TooltipHeaderFieldNamePair("Median Rent: $", "MEDIANRENT")};
            SetupCustomRenderSettings("MEDIANRENT", 0, tooltipPairs);

            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("County: ", "NAME"),
                new TooltipHeaderFieldNamePair("Median Rent: $", "MEDIANRENT")};

            SetupCustomRenderSettings("MEDIANRENT", 1, tooltipPairs);
        }        

        private void SetupPopulationDensity()
        {
            TooltipHeaderFieldNamePair[] tooltipPairs;
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("State: ", "STATE_NAME"),
                new TooltipHeaderFieldNamePair("Pop per Sqr Mile: ", "POP90_SQMI")};
            SetupCustomRenderSettings("POP90_SQMI", 0, tooltipPairs);
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("County: ", "NAME"),
                new TooltipHeaderFieldNamePair("Pop per Sqr Mile: ", "POP90_SQMI")};
            SetupCustomRenderSettings("POP90_SQMI", 1, tooltipPairs);
        }

        private void SetupAverageSalePrice()
        {
            TooltipHeaderFieldNamePair[] tooltipPairs;
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("State: ", "STATE_NAME"),
                new TooltipHeaderFieldNamePair("1987 Average House Sale: $", "AVG_SALE87")};
            SetupCustomRenderSettings("AVG_SALE87", 0, tooltipPairs);
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("County: ", "NAME"),
                new TooltipHeaderFieldNamePair("1987 Average House Sale: $", "AVG_SALE87")};
            SetupCustomRenderSettings("AVG_SALE87", 1, tooltipPairs);
        }

        private void SetupDivorced()
        {
            TooltipHeaderFieldNamePair[] tooltipPairs;
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("State: ", "STATE_NAME"),
                new TooltipHeaderFieldNamePair("Number Divorced: ", "DIVORCED"),
                new TooltipHeaderFieldNamePair("Number Married: ", "MARRIED")};
            SetupCustomRenderSettings("DIVORCED", 0, tooltipPairs);
            tooltipPairs = new TooltipHeaderFieldNamePair[] { 
                new TooltipHeaderFieldNamePair("County: ", "NAME"),
                new TooltipHeaderFieldNamePair("Number Divorced: ", "DIVORCED"),
                new TooltipHeaderFieldNamePair("Number Married: ", "MARRIED")};
            SetupCustomRenderSettings("DIVORCED", 1, tooltipPairs);
        }
       
        private void SetupCustomRenderSettings(string fieldName, int layerIndex, TooltipHeaderFieldNamePair[] tooltipFields)
        {            
            //get the required layer
            EGIS.ShapeFileLib.RenderSettings renderSettings = SFMap1.GetLayer(layerIndex).RenderSettings;
            int numRecords = SFMap1.GetLayer(layerIndex).RecordCount;
            EGIS.ShapeFileLib.DbfReader dbfReader = renderSettings.DbfReader;
            int fieldIndex = dbfReader.IndexOfFieldName(fieldName);

            double[] samples = new double[numRecords];
            //find the range of population values and obtain the quintile quantiles
            for (int n = 0; n < numRecords; n++)
            {
                double d = double.Parse(dbfReader.GetField(n, fieldIndex), System.Globalization.CultureInfo.InvariantCulture);
                samples[n] = d;
            }            
            double[] ranges = GetQuintiles(samples);
            
            //create the quintile colors - there will be 1 more color than the number of elements in quantiles
            Color[] cols = new Color[] { 
                Color.FromArgb(80, 0, 20), 
                Color.FromArgb(120, 0, 20), 
                Color.FromArgb(180, 0, 20), 
                Color.FromArgb(220, 0, 20),
                Color.FromArgb(250,0,20)};

            //setup the list of tooltip fields
            System.Collections.Generic.List<TooltipHeaderFieldNamePair> tooltipPairList = null;
            if(tooltipFields != null)
            {
                tooltipPairList = new System.Collections.Generic.List<TooltipHeaderFieldNamePair>();
                tooltipPairList.AddRange(tooltipFields);
            }

            //create a new QuantileCustomRenderSettings and add it to the SFMap
            QuantileCustomRenderSettings rcrs = new QuantileCustomRenderSettings(renderSettings, cols, ranges, fieldName, tooltipPairList);
            SFMap1.SetCustomRenderSettings(layerIndex, rcrs);

        }

        

    }
}
