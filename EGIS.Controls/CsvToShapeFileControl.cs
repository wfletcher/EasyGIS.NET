using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EGIS.ShapeFileLib;

namespace EGIS.Controls
{
    /// <summary>
    /// UserControl to convert XY csv data to a shapefile
    /// </summary>
    public partial class CsvToShapeFileControl : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CsvToShapeFileControl()
        {
            InitializeComponent();
        }

		#region private methods
		private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            if (ofdCsv.ShowDialog(this) == DialogResult.OK)
            {
                SourceDataFile = ofdCsv.FileName;
                if (string.IsNullOrEmpty(DestinationShapeFile))
                {
                    DestinationShapeFile = System.IO.Path.ChangeExtension(SourceDataFile, ".shp");
                }
            }

        }

        private void btnBrowseShapeFile_Click(object sender, EventArgs e)
        {
            if (sfdShapeFile.ShowDialog(this) == DialogResult.OK)
            {
                DestinationShapeFile = sfdShapeFile.FileName;
            }
            
        }

		#endregion

		#region public members

        /// <summary>
        /// Source CSV data file path
        /// </summary>
		public string SourceDataFile
        {
            get
            {
                return this.txtSource.Text;
            }
            set
            {
                this.txtSource.Text = value;
                UpdateSourceFields();
                ValidateConvert();
            }
        }

        /// <summary>
        /// Destination ShapeFile file path
        /// </summary>
        public string DestinationShapeFile
        {
            get
            {
                return this.txtDestination.Text;
            }
            set
            {
                this.txtDestination.Text = value;
                ValidateConvert();
            }

        }

        /// <summary>
        /// ConvertShapeFileProgress event
        /// </summary>
        public event EventHandler<ConvertShapeFileEventArgs> ConvertShapeFileProgressChanged;

		#endregion

		#region private methods

		private void UpdateSourceFields()
        {
            cbXCoordField.Items.Clear();
            cbYCoordField.Items.Clear();

            if (!System.IO.File.Exists(SourceDataFile)) return;

            string[] fieldNames = CsvUtil.ReadFieldHeaders(SourceDataFile);
            if (fieldNames == null || fieldNames.Length == 0) return;

            CsvUtil.TrimValues(fieldNames, new char[] { '"', '\'' });            

            cbXCoordField.Items.AddRange(fieldNames);
            cbYCoordField.Items.AddRange(fieldNames);

            CsvUtil.TrimValues(fieldNames);
            int xIndex = -1, yIndex = -1;
            xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Longitude", StringComparison.OrdinalIgnoreCase) >= 0);
            if (xIndex < 0) xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Easting", StringComparison.OrdinalIgnoreCase) >= 0);
            if (xIndex < 0) xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Lon", StringComparison.OrdinalIgnoreCase) >= 0);
            if (xIndex < 0) xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("East", StringComparison.OrdinalIgnoreCase) >= 0);

            yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Latitude", StringComparison.OrdinalIgnoreCase) >= 0);
            if (yIndex < 0) yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Northing", StringComparison.OrdinalIgnoreCase) >= 0);
            if (yIndex < 0) yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Lat", StringComparison.OrdinalIgnoreCase) >= 0);
            if (yIndex < 0) yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("North", StringComparison.OrdinalIgnoreCase) >= 0);

            if (xIndex >= 0) cbXCoordField.SelectedIndex = xIndex;
            if (yIndex >= 0) cbYCoordField.SelectedIndex = yIndex;
            

        }

        private bool ValidateConvert()
        {
            bool valid =  System.IO.File.Exists(SourceDataFile)
                && cbXCoordField.SelectedIndex >= 0m && cbYCoordField.SelectedIndex >= 0;
            btnConvert.Enabled = valid;
            return valid;
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            if (ValidateConvert())
            {
                try
                {
                    btnConvert.Enabled = false;
                    this.Cursor = Cursors.WaitCursor;
                    CsvUtil.ConvertCsvToShapeFile(SourceDataFile, DestinationShapeFile, cbXCoordField.SelectedItem as string, cbYCoordField.SelectedItem as string, true, OnProgressChanged);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Converting Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnConvert.Enabled = true;
                    this.Cursor = Cursors.Default;
                }
            }
            
        }

        private void cbXCoordField_SelectedIndexChanged(object sender, EventArgs e)
        {
            ValidateConvert();
        }

        private void cbYCoordField_SelectedIndexChanged(object sender, EventArgs e)
        {
            ValidateConvert();
        }

        private void OnProgressChanged(ConvertShapeFileEventArgs args)
        {
            if (ConvertShapeFileProgressChanged != null)
            {
                ConvertShapeFileProgressChanged(this, args);
            }
        }

        #endregion

    }
}
