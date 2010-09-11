using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace egis
{
    public partial class RecordAttributesForm : Form
    {

        private string layerName = null;

        private int recordIndex = -1;
        private int shapeIndex = -1;

        private string[] attributeNames = null;

        private string[] attributeValues = null;

        private bool allowClose = false;

        public bool AllowClose
        {
            get { return allowClose; }
            set { allowClose = value; }
        }

        public RecordAttributesForm()
        {
            InitializeComponent();

            SetStyle(ControlStyles.Selectable, false);
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                Visible = false;
            }            
            base.OnClosing(e);
        }


        public void SetRecordData(int shapeIndex, string layerName, int recordIndex, string[] attributeNames, string[] attributeValues)
        {
            this.lblLayerName.Text = "Layer:" +( string.IsNullOrEmpty(layerName) ? "" : layerName);
            this.lblRecordNumber.Text = string.Format("Record:{0}", recordIndex);
            if (shapeIndex < 0 || recordIndex < 0 || attributeNames == null || attributeValues == null)
            {
                this.dataGridView1.DataSource = null;
            }
            else
            {
                CreateDataSource(attributeNames, attributeValues); 

            }
        }

        private void CreateDataSource(string[] names, string[] values)
        {
            BindingSource bs = new BindingSource();
            for(int n = 0; n < names.Length;++n)
            {
                bs.Add(new NameValue(names[n].Trim(), values[n].Trim()));
            }
            this.dataGridView1.DataSource = bs;
            

        }

        private class NameValue
        {

            public NameValue(string name, string value)
            {
                this.Name = name;
                this.Value = value;
            }
            private string name;

            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            private string value;

            public string Value
            {
                get { return this.value; }
                set { this.value = value; }
            }

        }
    }
}