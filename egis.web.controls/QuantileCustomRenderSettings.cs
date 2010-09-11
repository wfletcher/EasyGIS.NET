using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using EGIS.ShapeFileLib;
using System.Data;

namespace EGIS.Web.Controls
{
    /// <summary>
    /// Simple Header / FieldName Pair class used to provide custom ToolTips in the QuantileCustomRenderSettings
    /// </summary>
    /// <remarks>
    /// <para>Each TooltipHeaderFieldNamePair object represents a single line that is displayed in a tooltip.</para>
    /// </remarks>
    /// <seealso cref="EGIS.Web.Controls.QuantileCustomRenderSettings"/>
    public class TooltipHeaderFieldNamePair
    {
        private string headerText;
        private string fieldName;

        /// <summary>
        /// Gets or sets the text to appear as the header of a line in a tooltip
        /// </summary>
        public string HeaderText
        {
            get
            {
                return headerText;
            }
            set
            {
                headerText = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the DBF field (or column in joined table) to get the tooltip data from.
        /// </summary>
        public string FieldName
        {
            get
            {
                return fieldName;
            }
            set
            {
                fieldName = value;
            }
        }

        /// <summary>
        /// Constructs a new TooltipHeaderFieldNamePair object
        /// </summary>
        /// <param name="headerText">The text to appear as the header of a line in a tooltip</param>
        /// <param name="fieldName">The name of the DBF field (or column in joined table) to get the tooltip data from </param>
        public TooltipHeaderFieldNamePair(string headerText, string fieldName)
        {
            this.HeaderText = headerText;
            this.FieldName = fieldName;
        }

        internal int FieldIndex = -1;
    }


    /// <summary>
    /// QuantileCustomRenderSettings implements the ICustomRenderSettings interface and is used to 
    /// provide dynamic render settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// QuantileCustomRenderSettings is used to render individual shape colors in a shapefile layer according to a specified range of values. For example,
    /// the color of the rendered shape could be green if field1 is between 0 and 100, yellow if field 1 is between 100 and 200, or red if
    /// it is greater than 200.
    /// </para>
    /// <para>The class also provides the ability to join an external DataTable on a shapefile's DBF data file</para>
    /// </remarks>
    /// <seealso cref="EGIS.ShapeFileLib.ICustomRenderSettings"/>
    public class QuantileCustomRenderSettings : EGIS.ShapeFileLib.ICustomRenderSettings
    {        
        private Color[] rangeColors;
        private int[] recordColorIndex;
        private RenderSettings renderSettings;
        private System.Collections.Generic.Dictionary<int, string> toolTips;

        /// <summary>
        /// Constructs a new QuantileCustomRenderSettings instance
        /// </summary>
        /// <param name="renderSettings">Reference to a ShapeFile RenderSettings</param>
        /// <param name="quantileColors">Array of Colors to use. The number of Color elements should be 1 more than the number of quantile elements</param>
        /// <param name="quantiles">Array of quantile values. Each successive element must be greater than the previous element. Example - {10, 50, 75}</param>
        /// <param name="quantileKey">The name of the column in the imported data used to determine what color to render a shape </param>
        /// <param name="importData">Data to join on the shapefile layer.</param>
        /// <param name="shapeJoinKey">The column in the shapefile layer's dbf file used to join to importData</param>
        /// <param name="importJoinKey">The column in importData used to join on the shapefile layer</param>
        public QuantileCustomRenderSettings(RenderSettings renderSettings, Color[] quantileColors, double[] quantiles, string quantileKey, DataTable importData, string shapeJoinKey, string importJoinKey)
        {
            this.renderSettings = renderSettings;
            this.rangeColors = quantileColors;
            Array.Resize<Color>(ref this.rangeColors, quantileColors.Length + 1);
            this.rangeColors[this.rangeColors.Length - 1] = renderSettings.FillColor;
            SetupRangeSettings(quantiles, quantileKey, importData, shapeJoinKey, importJoinKey);
        }

        /// <summary>
        /// Constructs a new QuantileCustomRenderSettings instance
        /// </summary>
        /// <param name="renderSettings">Reference to a ShapeFile RenderSettings</param>
        /// <param name="quantileColors">Array of Colors to use. The number of Color elements should be 1 more than the number of quantile elements</param>
        /// <param name="quantiles">Array of quantile values. Each successive element must be greater than the previous element. Example - {10, 50, 75}</param>
        /// <param name="quantileKey">The name of the column in the imported data used to determine what color to render a shape </param>
        /// <param name="importData">Data to join on the shapefile layer.</param>
        /// <param name="shapeJoinKey">The column in the shapefile layer's dbf file used to join to importData</param>
        /// <param name="importJoinKey">The column in importData used to join on the shapefile layer</param>
        /// <param name="tooltipHeaderFieldList">List of TooltipHeaderFieldNamePair objects used to create a custom tooltip</param>
        public QuantileCustomRenderSettings(RenderSettings renderSettings, Color[] quantileColors, double[] quantiles, string quantileKey, DataTable importData, string shapeJoinKey, string importJoinKey, System.Collections.Generic.List<TooltipHeaderFieldNamePair> tooltipHeaderFieldList)
        {
            this.renderSettings = renderSettings;
            this.rangeColors = quantileColors;
            Array.Resize<Color>(ref this.rangeColors, quantileColors.Length + 1);
            this.rangeColors[this.rangeColors.Length - 1] = renderSettings.FillColor;

            toolTips = new System.Collections.Generic.Dictionary<int, string>();            
            SetupRangeSettings(quantiles, quantileKey, importData, shapeJoinKey, importJoinKey, tooltipHeaderFieldList);
        }

        /// <summary>
        /// Constructs a new QuantileCustomRenderSettings instance
        /// </summary>
        /// <param name="renderSettings">Reference to a ShapeFile RenderSettings</param>
        /// <param name="quantileColors">Array of Colors to use. The number of Color elements should be 1 more than the number of quantile elements</param>
        /// <param name="quantiles">Array of quantile values. Each successive element must be greater than the previous element. Example - {10, 50, 75}</param>
        /// <param name="shapeFieldName">The name of the shapefile dbf field used to determine what color to render a shape </param>
        public QuantileCustomRenderSettings(RenderSettings renderSettings, Color[] quantileColors, double[] quantiles, string shapeFieldName):this(renderSettings, quantileColors, quantiles, shapeFieldName, null)
        {
        }

        /// <summary>
        /// Constructs a new QuantileCustomRenderSettings instance
        /// </summary>
        /// <param name="renderSettings">Reference to a ShapeFile RenderSettings</param>
        /// <param name="quantileColors">Array of Colors to use. The number of Color elements should be 1 more than the number of quantile elements</param>
        /// <param name="quantiles">Array of quantile values. Each successive element must be greater than the previous element. Example - {10, 50, 75}</param>
        /// <param name="shapeFieldName">The name of the shapefile dbf field used to determine what color to render a shape </param>
        /// <param name="tooltipHeaderFieldList">List of TooltipHeaderFieldNamePair objects used to create a custom tooltip</param>
        public QuantileCustomRenderSettings(RenderSettings renderSettings, Color[] quantileColors, double[] quantiles, string shapeFieldName, System.Collections.Generic.List<TooltipHeaderFieldNamePair> tooltipHeaderFieldList)
        {
            this.renderSettings = renderSettings;
            this.rangeColors = quantileColors;
            Array.Resize<Color>(ref this.rangeColors, quantileColors.Length + 1);
            this.rangeColors[this.rangeColors.Length - 1] = renderSettings.FillColor;

            if (tooltipHeaderFieldList != null) toolTips = new System.Collections.Generic.Dictionary<int, string>();
            SetupRangeSettings(quantiles, shapeFieldName, tooltipHeaderFieldList);
        }        

        private static int FindFieldIndex(string[] fieldNames, string requiredField)
        {
            int fieldIndex = -1;
            for (int n = 0; fieldIndex < 0 && n < fieldNames.Length; n++)
            {
                if (string.Compare(requiredField, fieldNames[n].Trim(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    fieldIndex = n;
                }
            }
            return fieldIndex;

        }

                
        #region ICustomRenderSettings Members

        /// <summary>
        /// Implementation of the ICustomRenderSettings GetRecordFillColor member
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        public System.Drawing.Color GetRecordFillColor(int recordNumber)
        {
            if (rangeColors == null || recordColorIndex == null) return renderSettings.FillColor;
            return rangeColors[recordColorIndex[recordNumber]];
        }

        /// <summary>
        /// Implementation of the ICustomRenderSettings RenderShape member
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        public bool RenderShape(int recordNumber)
        {
            return true;
        }


        private void SetupRangeSettings(double[] ranges, string rangeKey, System.Collections.Generic.List<TooltipHeaderFieldNamePair> tooltipHeaderFieldList)
        {            
            {
                int fieldIndex = -1;
                string[] fieldNames = renderSettings.DbfReader.GetFieldNames();
                fieldIndex = FindFieldIndex(fieldNames, rangeKey);
                if (fieldIndex < 0) return;

                int numRecords = renderSettings.DbfReader.DbfRecordHeader.RecordCount;
                this.recordColorIndex = new int[numRecords];

                if (tooltipHeaderFieldList != null)
                {
                    //find the field indexes
                    foreach (TooltipHeaderFieldNamePair pair in tooltipHeaderFieldList)
                    {
                        if (!string.IsNullOrEmpty(pair.FieldName))
                        {
                            pair.FieldIndex = FindFieldIndex(fieldNames, pair.FieldName);
                        }                        
                    }                    
                }

                for (int n = 0; n < numRecords; n++)
                {
                    string s = renderSettings.DbfReader.GetField(n, fieldIndex).Trim();                    
                    double d;
                    if(!double.TryParse(s,out d))
                    {
                        this.recordColorIndex[n] = this.rangeColors.Length - 1;
                    }
                    else
                    {                        
                        bool added = false;
                        for (int r = 0; !added && (r < ranges.Length); r++)
                        {
                            if (d < ranges[r])
                            {
                                this.recordColorIndex[n] = r;
                                added = true;
                            }
                        }
                        if (!added)
                        {
                            this.recordColorIndex[n] = ranges.Length;                            
                        }
                        if (tooltipHeaderFieldList != null)
                        {
                            //add the tooltip text
                            StringBuilder sb = new StringBuilder();
                            foreach (TooltipHeaderFieldNamePair pair in tooltipHeaderFieldList)
                            {
                                sb.Append(pair.HeaderText);
                                if (pair.FieldIndex >= 0)
                                {
                                    sb.Append(renderSettings.DbfReader.GetField(n, pair.FieldIndex).Trim());
                                }                                                                    
                                sb.Append("<br/>");
                            }
                            toolTips.Add(n, sb.ToString());
                        }
                    }                    
                }
            }
        }


        private void SetupRangeSettings(double[] ranges, string rangeKey, DataTable importData, string shapeJoinKey, string importJoinKey)
        {
            {
                int fieldIndex = -1;
                string[] fieldNames = renderSettings.DbfReader.GetFieldNames();
                for (int n = 0; fieldIndex < 0 && n < fieldNames.Length; n++)
                {
                    if (string.Compare(shapeJoinKey, fieldNames[n].Trim(), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        fieldIndex = n;
                    }
                }
                if (fieldIndex < 0) return;

                int numRecords = renderSettings.DbfReader.DbfRecordHeader.RecordCount;
                this.recordColorIndex = new int[numRecords];
                importData.PrimaryKey = new DataColumn[] { importData.Columns[importJoinKey] };
                //DataColumn[] key = importData.PrimaryKey;

                for (int n = 0; n < numRecords; n++)
                {
                    string s = renderSettings.DbfReader.GetField(n, fieldIndex).Trim();
                    DataRow dr = importData.Rows.Find(s);
                    if (dr != null)
                    {
                        if (dr[rangeKey] == null)
                        {
                            this.recordColorIndex[n] = this.rangeColors.Length - 1;
                        }
                        else
                        {
                            double d = double.Parse(dr[rangeKey].ToString(), System.Globalization.CultureInfo.InvariantCulture);

                            bool added = false;

                            for (int r = 0; !added && (r < ranges.Length); r++)
                            {
                                if (d < ranges[r])
                                {
                                    this.recordColorIndex[n] = r;
                                    added = true;
                                }
                            }
                            if (!added)
                            {
                                this.recordColorIndex[n] = ranges.Length;
                            }
                        }
                    }
                    else
                    {
                        this.recordColorIndex[n] = this.rangeColors.Length - 1;
                    }

                }
            }
        }


        private void SetupRangeSettings(double[] ranges, string rangeKey, DataTable importData, string shapeJoinKey, string importJoinKey, System.Collections.Generic.List<TooltipHeaderFieldNamePair> tooltipHeaderFieldList)
        {
            int fieldIndex = -1;
            string[] fieldNames = renderSettings.DbfReader.GetFieldNames();
            for (int n = 0; fieldIndex < 0 && n < fieldNames.Length; n++)
            {
                if (string.Compare(shapeJoinKey, fieldNames[n].Trim(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    fieldIndex = n;
                }
            }
            if (fieldIndex < 0) return;

            int numRecords = renderSettings.DbfReader.DbfRecordHeader.RecordCount;
            this.recordColorIndex = new int[numRecords];
            importData.PrimaryKey = new DataColumn[] { importData.Columns[importJoinKey] };
            //DataColumn[] key = importData.PrimaryKey;

            for (int n = 0; n < numRecords; n++)
            {
                string s = renderSettings.DbfReader.GetField(n, fieldIndex).Trim();
                DataRow dr = importData.Rows.Find(s);
                if (dr != null)
                {
                    if (dr[rangeKey] == null)
                    {
                        this.recordColorIndex[n] = this.rangeColors.Length - 1;
                    }
                    else
                    {
                        double d = double.Parse(dr[rangeKey].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                        bool added = false;
                        for (int r = 0; !added && (r < ranges.Length); r++)
                        {
                            if (d < ranges[r])
                            {
                                this.recordColorIndex[n] = r;
                                added = true;
                            }
                        }
                        if (!added)
                        {
                            this.recordColorIndex[n] = ranges.Length;
                        }
                        //add the tooltip text
                        StringBuilder sb = new StringBuilder();
                        foreach (TooltipHeaderFieldNamePair pair in tooltipHeaderFieldList)
                        {
                            sb.Append(pair.HeaderText);
                            if (!string.IsNullOrEmpty(pair.FieldName))
                            {
                                object o = dr[pair.FieldName];
                                if (o != null)
                                {
                                    sb.Append(o.ToString());
                                }
                                else
                                {
                                    int indx = FindFieldIndex(fieldNames, pair.FieldName);
                                    if (indx >= 0)
                                    {
                                        sb.Append(renderSettings.DbfReader.GetField(n, fieldIndex).Trim());
                                    }
                                }
                            }
                            sb.Append("<br/>");
                        }
                        toolTips.Add(n, sb.ToString());
                    }
                }
                else
                {
                    this.recordColorIndex[n] = this.rangeColors.Length - 1;
                }

            }
        }

        /// <summary>
        /// Implementation of the ICustomRenderSettings GetRecordToolTip member
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        public string GetRecordToolTip(int recordNumber)
        {
            if (toolTips != null && toolTips.ContainsKey(recordNumber))
            {
                return toolTips[recordNumber];
            }
            return null;

        }

        /// <summary>
        /// Implementation of the ICustomRenderSettings UseCustomTooltips member
        /// </summary>
        public bool UseCustomTooltips
        {
            get
            {
                return toolTips != null;
            }
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

        public Image GetRecordImageSymbol(int recordNumber)
        {
            //throw new Exception("The method or operation is not implemented.");
            return renderSettings.GetImageSymbol();
        }

        #endregion
    }


}
