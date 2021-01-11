using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Utility factory class which creates ICustomRenderSettings objects
    /// </summary>
    public class CustomRenderSettingsUtil
    {

        private CustomRenderSettingsUtil() { }

        /// <summary>
        /// Creates an ICustomRenderSettings object which renders shape colors using quantiles
        /// </summary>
        /// <remarks>
        /// <para>
        /// QuantileCustomRenderSettings are used to render individual shape colors in a shapefile layer according to a specified range of values. For example,
        /// the color of the rendered shape could be green if field1 is between 0 and 100, yellow if field 1 is between 100 and 200, or red if
        /// it is greater than 200.
        /// </para>
        /// </remarks>    
        /// <param name="renderSettings"></param>
        /// <param name="quantileColors">The colors used for each quantile. If 5 colors are used then 5 quantiles will be setup </param>
        /// <param name="shapeFieldName">The field name to use for the quantiles</param>
        /// <returns></returns>
        public static ICustomRenderSettings CreateQuantileCustomRenderSettings(RenderSettings renderSettings, System.Drawing.Color[] quantileColors, string shapeFieldName)
        {
            int numRecords = renderSettings.DbfReader.DbfRecordHeader.RecordCount;
            int fieldIndex = renderSettings.DbfReader.IndexOfFieldName(shapeFieldName);

            double[] samples = new double[numRecords];
            //find the range of population values and obtain the quintile quantiles
            for (int n = 0; n < numRecords; n++)
            {
                double d = double.Parse(renderSettings.DbfReader.GetField(n, fieldIndex), System.Globalization.CultureInfo.InvariantCulture);
                samples[n] = d;
            }
            double[] ranges = QuantileRenderSettings.GetQuantiles(samples, quantileColors.Length-1);
            return new QuantileRenderSettings(renderSettings, quantileColors, ranges, shapeFieldName);
        }

        /// <summary>
        /// Creates an ICustomRenderSettings object which renders shapes with a random color
        /// </summary>
        /// <param name="renderSettings">The shapefile's RenderSettings object</param>
        /// <param name="seed">Random seed used to create the colors. By supplying a seed the same colors will be used when rendering the shapefile in successive drawing operations </param>
        /// <returns></returns>
        public static ICustomRenderSettings CreateRandomColorCustomRenderSettings(RenderSettings renderSettings, int seed)
        {            
            return new RandomColorRenderSettings(renderSettings, seed);            
        }

       

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
    /// </remarks>
    /// <seealso cref="EGIS.ShapeFileLib.ICustomRenderSettings"/>
    class QuantileRenderSettings : EGIS.ShapeFileLib.ICustomRenderSettings
    {
        private Color[] rangeColors;
        private int[] recordColorIndex;
        private RenderSettings renderSettings;                

        /// <summary>
        /// Constructs a new QuantileCustomRenderSettings instance
        /// </summary>
        /// <param name="renderSettings">Reference to a ShapeFile RenderSettings</param>
        /// <param name="quantileColors">Array of Colors to use. The number of Color elements should be 1 more than the number of quantile elements</param>
        /// <param name="quantiles">Array of quantile values. Each successive element must be greater than the previous element. Example - {10, 50, 75}</param>
        /// <param name="shapeFieldName">The name of the shapefile dbf field used to determine what color to render a shape </param>
        public QuantileRenderSettings(RenderSettings renderSettings, Color[] quantileColors, double[] quantiles, string shapeFieldName)
        {
            this.renderSettings = renderSettings;
            this.rangeColors = quantileColors;
            Array.Resize<Color>(ref this.rangeColors, quantileColors.Length + 1);
            this.rangeColors[this.rangeColors.Length - 1] = renderSettings.FillColor; //default color            
            SetupRangeSettings(quantiles, shapeFieldName);
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


        private void SetupRangeSettings(double[] ranges, string rangeKey)
        {
            
                int fieldIndex = -1;
                string[] fieldNames = renderSettings.DbfReader.GetFieldNames();
                fieldIndex = FindFieldIndex(fieldNames, rangeKey);
                if (fieldIndex < 0) return;

                int numRecords = renderSettings.DbfReader.DbfRecordHeader.RecordCount;
                this.recordColorIndex = new int[numRecords];


                for (int n = 0; n < numRecords; n++)
                {
                    string s = renderSettings.DbfReader.GetField(n, fieldIndex).Trim();
                    double d;
                    if (!double.TryParse(s, out d))
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
            return null;
        }

        /// <summary>
        /// Implementation of the ICustomRenderSettings UseCustomTooltips member
        /// </summary>
        public bool UseCustomTooltips
        {
            get
            {
                return false;
            }
        }        

        public Color GetRecordOutlineColor(int recordNumber)
        {
            return renderSettings.OutlineColor;
        }

        public Color GetRecordFontColor(int recordNumber)
        {
            return renderSettings.FontColor;
        }

        public bool UseCustomImageSymbols
        {
            get
            {
                return false;
            }
        }

        public Image GetRecordImageSymbol(int recordNumber)
        {
            return renderSettings.GetImageSymbol();
        }

        public int GetDirection(int recordNumber)
        {
            return 0;
        }

        #endregion



        public static double[] GetQuantiles(double[] samples, int numQuantiles)
        {
            Array.Sort(samples);
            double[] quantiles = new double[numQuantiles];
            double interval = 1.0 / (numQuantiles + 1);
            double q = interval;
            for (int n = 0; n < numQuantiles; ++n)
            {
                quantiles[n] = samples[Math.Min((int)(samples.Length * q),samples.Length-1)];
                q += interval;
            }
            return quantiles;
        }
    }


    class RandomColorRenderSettings : ICustomRenderSettings
    {
        #region ICustomRenderSettings Members
        private Color[] colors;
        private RenderSettings renderSettings;
        public RandomColorRenderSettings(RenderSettings renderSettings, int seed)
        {
            int numRecords = renderSettings.DbfReader.DbfRecordHeader.RecordCount;
            Random r = new Random(seed);
            colors = new Color[numRecords];
            for (int n = numRecords - 1; n >= 0; --n)
            {
                colors[n] = Color.FromArgb(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255));
            }
            this.renderSettings = renderSettings;
            
        }

        public Color GetRecordFillColor(int recordNumber)
        {
            return colors[recordNumber];
        }

        public Color GetRecordOutlineColor(int recordNumber)
        {
            return renderSettings.OutlineColor;
        }

        public Color GetRecordFontColor(int recordNumber)
        {
            return renderSettings.FontColor;
        }

        public bool RenderShape(int recordNumber)
        {
            return true;
        }

        public bool UseCustomTooltips
        {
            get { return false; }
        }

        public string GetRecordToolTip(int recordNumber)
        {
            return "";
        }

        public bool UseCustomImageSymbols
        {
            get { return false; }
        }

        public Image GetRecordImageSymbol(int recordNumber)
        {
            return null;
        }

        public int GetDirection(int recordNumber)
        {
            return 0;
        }

        #endregion
    }

    class CategorizedRenderSettings : ICustomRenderSettings
    {
        #region ICustomRenderSettings Members

        public Color GetRecordFillColor(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Color GetRecordOutlineColor(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Color GetRecordFontColor(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool RenderShape(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool UseCustomTooltips
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string GetRecordToolTip(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool UseCustomImageSymbols
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public Image GetRecordImageSymbol(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int GetDirection(int recordNumber)
        {
            return 0;
        }

        #endregion
    }

    class GraduatedColorRenderSettings : ICustomRenderSettings
    {
        //graduated linear mapping of values from start color to end color
        //Color = C0 + (C1-C0) * t, 0<=t<=1. performed separately for each RGB component
        //can convert to HSV before interpolating and convert back again for better representation

        #region ICustomRenderSettings Members

        public Color GetRecordFillColor(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Color GetRecordOutlineColor(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Color GetRecordFontColor(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool RenderShape(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool UseCustomTooltips
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public string GetRecordToolTip(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool UseCustomImageSymbols
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public Image GetRecordImageSymbol(int recordNumber)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int GetDirection(int recordNumber)
        {
            return 0;
        }

        #endregion
    }

}
