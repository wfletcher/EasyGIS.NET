using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace egis
{
    public partial class CsvToShapeFileForm : Form
    {
        public CsvToShapeFileForm()
        {
            InitializeComponent();
        }

        private void csvToShapeFileControl1_ConvertShapeFileProgressChanged(object sender, EGIS.ShapeFileLib.ConvertShapeFileEventArgs e)
        {
            this.convertProgressBar.Value = Math.Min(e.ProgressPercent, 100);
            Refresh();
        }

        private void csvToShapeFileControl1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
