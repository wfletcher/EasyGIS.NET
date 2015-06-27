using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EGIS.ShapeFileLib;

namespace egis
{
    public partial class ViewAttributesForm : Form
    {
        private EGIS.Controls.SFMap mapReference;

        public ViewAttributesForm(EGIS.Controls.SFMap mapReference)
        {            
            InitializeComponent();
            this.mapReference = mapReference;
            if (mapReference == null) throw new NullReferenceException("null mapReference");
            mapReference.ShapeFilesChanged += mapReference_ShapeFilesChanged;
            mapReference.SelectedRecordsChanged += mapReference_SelectedRecordsChanged;
            mapReference_ShapeFilesChanged(this, EventArgs.Empty);
        }

        void mapReference_SelectedRecordsChanged(object sender, EventArgs e)
        {
            LoadSelectedRecords();
        }

        void mapReference_ShapeFilesChanged(object sender, EventArgs e)
        {
            this.cbCurrentLayer.Items.Clear();
            for (int n = 0; n < mapReference.ShapeFileCount; ++n)
            {
                this.cbCurrentLayer.Items.Add(mapReference[n]);
            }
            if (cbCurrentLayer.Items.Count > 0)
            {
                cbCurrentLayer.SelectedIndex = 0;
            }
            else
            {
                ClearData();
            }

        }

        private DataTable dataTable = null;
        private DataView dataView = null;

        private ShapeFile shapeFileReference = null;
        
        public void LoadAttributes(ShapeFile shapeFile)
        {
            this.shapeFileReference = shapeFile;
            try
            {
                selectingRecords = true;
                if (this.dataTable != null)
                {
                    this.dataGridView1.DataSource = null;
                    this.dataTable.Dispose();
                    this.dataView.Dispose();
                }
                dataTable = CreateDataTable(shapeFile.Name, shapeFile.RenderSettings.DbfReader);
                this.dataView = new DataView(dataTable);
                this.dataGridView1.DataSource = dataView;
                this.dataGridView1.Columns[0].Visible = false;
                this.tslblRecords.Text = string.Format("{0} records loaded", dataTable.Rows.Count);
            }
            finally
            {
                LoadSelectedRecords();
                selectingRecords = false;
            }

                        
        }

        private DataTable CreateDataTable(string tableName, DbfReader reader)
        {
            DataTable dataTable = new DataTable(tableName);

            DbfFieldDesc[] fieldDescriptions = reader.DbfRecordHeader.GetFieldDescriptions();

            //create the columns
            dataTable.Columns.Add("RecordIndex", typeof(Int32));
            foreach (DbfFieldDesc fieldDescription in fieldDescriptions)
            {
                dataTable.Columns.Add(fieldDescription.FieldName, typeof(string));            
            }
           

            //add the data
            object[] rowValues = new object[fieldDescriptions.Length + 1];
            for (int n = 0; n < reader.DbfRecordHeader.RecordCount; ++n)
            {
                string[] values = reader.GetFields(n);
                rowValues[0] = n;
                Array.Copy(values, 0, rowValues, 1, values.Length);
                TrimArrayValues(values);
                DataRow row = dataTable.NewRow();
                row.ItemArray = rowValues;
                
                dataTable.Rows.Add(row);                
            }
            return dataTable;

        }

        private static void TrimArrayValues(string[] values)
        {
            for (int n = values.Length - 1; n >= 0; --n)
            {
                values[n] = values[n].Trim();
            }
        }

        
        protected override void OnClosing(CancelEventArgs e)
        {
            isClosing = true;
            base.OnClosing(e);
        }

        private bool isClosing = false;

        private void ClearData()
        {
            if (this.dataTable != null)
            {
                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                this.dataView.Dispose();
                this.dataTable.Clear();
                this.dataTable.Rows.Clear();
                this.dataTable.Dispose();
            }
            this.dataTable = null;
            this.dataView = null;
            this.shapeFileReference = null;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            ClearData();
            GC.Collect();                
        }

        private void FilterRecords()
        {
            string filter = dataView.RowFilter;
            try
            {
                dataView.RowFilter = txtSelect.Text;
            }
            catch (Exception ex)
            {
                dataView.RowFilter = filter;
                tslblErrors.Text = ex.Message;
            }
            this.tslblRecords.Text = string.Format("{0} records of {1} selected [total records:{2}]", dataGridView1.SelectedRows.Count, dataGridView1.Rows.Count, dataTable.Rows.Count);
        }


        private bool selectingRecords = false;
        private void SelectRecords()
        {
            try
            {
                selectingRecords = true;
                dataView.RowFilter = "";
                dataGridView1.ClearSelection();
                DataRow[] rows = dataTable.Select(txtSelect.Text);
                if (rows != null)
                {
                    Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
                    foreach (DataRow row in rows)
                    {
                        dictionary[(int)row[0]] = true;                            
                    }
                    for (int n = dataGridView1.Rows.Count - 1; n >= 0; --n)
                    {
                        if (dictionary.ContainsKey((int)dataGridView1[0,n].Value)) dataGridView1.Rows[n].Selected = true;
                    }
                }

            }
            catch (Exception ex)
            {
                tslblErrors.Text = ex.Message;
            }
            finally
            {
                selectingRecords = false;
                dataGridView1_SelectionChanged(this, EventArgs.Empty);
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            SelectRecords();                       
        }

        private bool activated = false;
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            LoadSelectedRecords();
            activated = true;
        }

        private void LoadSelectedRecords()
        {
            if (this.shapeFileReference == null) return;
            System.Collections.ObjectModel.ReadOnlyCollection<int> selectedRecordIndicies = this.shapeFileReference.SelectedRecordIndices;
            if (selectedRecordIndicies != null)
            {
                try
                {
                    selectingRecords = true;
                    dataGridView1.ClearSelection();
                    Dictionary<int, bool> dictionary = new Dictionary<int, bool>();                    
                    foreach (int index in selectedRecordIndicies)
                    {
                        dictionary[index] = true;
                    }
                    for (int n = dataGridView1.Rows.Count - 1; n >= 0; --n)
                    {
                        if (dictionary.ContainsKey((int)dataGridView1[0, n].Value)) dataGridView1.Rows[n].Selected = true;
                    }
                }
                finally
                {
                    selectingRecords = false;
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (!(this.isClosing || this.selectingRecords) && activated)
            {
                shapeFileReference.ClearSelectedRecords();
                foreach (DataGridViewRow row in this.dataGridView1.SelectedRows)
                {
                    shapeFileReference.SelectRecord((int)row.Cells[0].Value, true);
                }
                if (mapReference != null) mapReference.Refresh(true);
                this.tslblRecords.Text = string.Format("{0} records of {1} selected", dataGridView1.SelectedRows.Count, dataGridView1.Rows.Count);
            }
        }

               

        private void btnFilter_Click(object sender, EventArgs e)
        {
            FilterRecords();
        }

       
        private void cbCurrentLayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbCurrentLayer.SelectedIndex >= 0 && cbCurrentLayer.SelectedIndex < mapReference.ShapeFileCount)
            {
                LoadAttributes(mapReference[cbCurrentLayer.SelectedIndex]);
            }

        }
    }


 

}
