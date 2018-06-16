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
    public partial class CsvToShapeFileControl : UserControl
    {
        public CsvToShapeFileControl()
        {
            InitializeComponent();
        }

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

        public event EventHandler<ConvertShapeFileEventArgs> ConvertShapeFileProgressChanged;


        private void UpdateSourceFields()
        {
            cbXCoordField.Items.Clear();
            cbYCoordField.Items.Clear();

            if (!System.IO.File.Exists(SourceDataFile)) return;

            string[] fieldNames = CsvUtil.ReadFieldHeaders(SourceDataFile);
            if (fieldNames == null || fieldNames.Length == 0) return;

            cbXCoordField.Items.AddRange(fieldNames);
            cbYCoordField.Items.AddRange(fieldNames);

            CsvUtil.TrimValues(fieldNames);
            int xIndex = -1, yIndex = -1;
            xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Longitude", StringComparison.OrdinalIgnoreCase) >= 0);
            if (xIndex < 0) xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Easting", StringComparison.OrdinalIgnoreCase) >= 0);
            if (xIndex < 0) xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Lat", StringComparison.OrdinalIgnoreCase) >= 0);
            if (xIndex < 0) xIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("East", StringComparison.OrdinalIgnoreCase) >= 0);

            yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Latitude", StringComparison.OrdinalIgnoreCase) >= 0);
            if (yIndex < 0) yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Northing", StringComparison.OrdinalIgnoreCase) >= 0);
            if (yIndex < 0) yIndex = Array.FindIndex<string>(fieldNames, s => s.IndexOf("Lon", StringComparison.OrdinalIgnoreCase) >= 0);
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
                    CsvUtil.ConvertCsvToShapeFile(SourceDataFile, DestinationShapeFile, cbXCoordField.SelectedItem as string, cbYCoordField.SelectedItem as string, OnProgressChanged);
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


    }
}
