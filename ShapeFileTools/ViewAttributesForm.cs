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

        private bool useVirtualMode = false;

        public ViewAttributesForm(EGIS.Controls.SFMap mapReference)
        {            
            InitializeComponent();
            this.mapReference = mapReference;
            if (mapReference == null) throw new NullReferenceException("null mapReference");
            mapReference.ShapeFilesChanged += mapReference_ShapeFilesChanged;
            mapReference.SelectedRecordsChanged += mapReference_SelectedRecordsChanged;
            mapReference_ShapeFilesChanged(this, EventArgs.Empty);

            this.dataGridView1.VirtualMode = useVirtualMode;
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
                    if (useVirtualMode)
                    {
                        this.dataGridView1.RowCount = 0;
                    }
                    this.dataGridView1.Columns.Clear();

                    this.dataGridView1.DataSource = null;
                    this.dataTable.Dispose();
                    this.dataView.Dispose();
                }
                dataTable = CreateDataTable(shapeFile.Name, shapeFile.RenderSettings.DbfReader);
                this.dataView = new DataView(dataTable);

                if (useVirtualMode)
                {
                    for (int n = 0; n < dataTable.Columns.Count; ++n)
                    {
                        this.dataGridView1.Columns.Add(dataTable.Columns[n].ColumnName, dataTable.Columns[n].ColumnName);
                    }
                    this.dataGridView1.RowCount = this.dataView.Count;
                }
                else
                {
                    this.dataGridView1.DataSource = dataView;
                }
                this.dataGridView1.Columns[0].Visible = false;
                
                this.tslblRecords.Text = string.Format("{0} records loaded", dataTable.Rows.Count);
            }
            finally
            {
                LoadSelectedRecords();
                selectingRecords = false;
            }            
        }

        private const string ShapeFileRecordIndexColumnName = "SFRecordIndex";

        private DataTable CreateDataTable(string tableName, DbfReader reader)
        {
            DataTable dataTable = new DataTable(tableName);

            DbfFieldDesc[] fieldDescriptions = reader.DbfRecordHeader.GetFieldDescriptions();

            //create the columns
            dataTable.Columns.Add(ShapeFileRecordIndexColumnName, typeof(Int32));
            dataTable.Columns.AddRange(DataColumnsFromDbfFields(fieldDescriptions));
           
            //add the data
            object[] rowValues = new object[fieldDescriptions.Length + 1];
            for (int n = 0; n < reader.DbfRecordHeader.RecordCount; ++n)
            {
                string[] values = reader.GetFields(n);
                TrimArrayValues(values);                
                rowValues[0] = n;
                GetDataValues(fieldDescriptions, values, rowValues, 1);

                DataRow row = dataTable.NewRow();
                row.ItemArray = rowValues;                
                dataTable.Rows.Add(row);                
            }
           
            return dataTable;
        }

        private static DataColumn[] DataColumnsFromDbfFields(DbfFieldDesc[] fieldDescriptions)
        {
            DataColumn[] dataColumns = new DataColumn[fieldDescriptions.Length];
            for (int n = 0; n < fieldDescriptions.Length; ++n)
            {
                Type dataType;
                switch(fieldDescriptions[n].FieldType)
                {
                    case DbfFieldType.Number:
                        if(fieldDescriptions[n].DecimalCount == 0) dataType = typeof(Int32);
                        else dataType = typeof(Double);
                        break;
                    case DbfFieldType.FloatingPoint:
                        dataType = typeof(Double);
                        break;
                    default:
                        dataType = typeof(string);
                        break;
                }
                dataColumns[n] = new DataColumn(fieldDescriptions[n].FieldName, dataType);                
            }
            return dataColumns;
        }

        private static void GetDataValues(DbfFieldDesc[] fieldDescriptions, string[] dbfFields, object[] dataValues, int dataValuesOffset)
        {
            for (int n = 0; n < dbfFields.Length; ++n)
            {
                switch (fieldDescriptions[n].FieldType)
                {
                    case DbfFieldType.Number:
                        if (fieldDescriptions[n].DecimalCount == 0)
                        {
                            int intValue;
                            if (Int32.TryParse(dbfFields[n], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out intValue))
                            {
                                dataValues[dataValuesOffset + n] = intValue;
                            }
                            else
                            {
                                dataValues[dataValuesOffset + n] = DBNull.Value;
                            }                            
                        }
                        else
                        {
                            double doubleValue;
                            if (double.TryParse(dbfFields[n], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out doubleValue))
                            {
                                dataValues[dataValuesOffset + n] = doubleValue;
                            }
                            else
                            {
                                dataValues[dataValuesOffset + n] = DBNull.Value;
                            }
                        }
                        break;
                    case DbfFieldType.FloatingPoint:
                        double doubleValue2;
                        if (double.TryParse(dbfFields[n], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out doubleValue2))
                        {
                            dataValues[dataValuesOffset + n] = doubleValue2;
                        }
                        else
                        {
                            dataValues[dataValuesOffset + n] = DBNull.Value;
                        }
                        break;
                    default:
                        dataValues[dataValuesOffset + n] = dbfFields[n];
                        break;
                }
                
            }
            
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
                if (useVirtualMode)
                {
                    this.dataGridView1.RowCount = 0;
                }
                    
                dataView.RowFilter = txtSelect.Text;                
            }
            catch (Exception ex)
            {
                dataView.RowFilter = filter;
                tslblErrors.Text = ex.Message;
            }
            if (useVirtualMode)
            {
                dataGridView1.RowCount = dataView.Count;
            }
            this.tslblRecords.Text = string.Format("{0} records of {1} selected [total records:{2}]", dataGridView1.SelectedRows.Count, dataView.Count, dataTable.Rows.Count);
        }


        private bool selectingRecords = false;
        private void SelectRecords()
        {
            try
            {
                dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
                   
                selectingRecords = true;
                dataView.RowFilter = "";
                dataGridView1.ClearSelection();
                if (useVirtualMode)
                {
                    this.dataGridView1.RowCount = 0;
                    this.dataGridView1.RowCount = dataView.Count;
                }
                DataRow[] rows = dataTable.Select(txtSelect.Text);
                if (rows != null)
                {
                    Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
                    int colIndex = dataTable.Columns.IndexOf(ShapeFileRecordIndexColumnName);
                   
                    foreach (DataRow row in rows)
                    {                        
                        dictionary[(int)row[colIndex]] = true;                            
                    }
                    
                    dataGridView1.SuspendLayout();
                    for (int n = dataView.Count - 1; n >= 0; --n)
                    {
                        int rowIndex = (int)dataView[n][colIndex];
                        if (dictionary.ContainsKey(rowIndex)) dataGridView1.Rows[n].Selected = true;
                    }
                }               
            }
            catch (Exception ex)
            {
                tslblErrors.Text = ex.Message;
            }
            finally
            {
                dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                    
                dataGridView1.ResumeLayout();
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
                    dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
                    selectingRecords = true;
                    dataGridView1.ClearSelection();
                    Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
                    foreach (int index in selectedRecordIndicies)
                    {
                        dictionary[index] = true;
                    }
                    int colIndex = dataTable.Columns.IndexOf(ShapeFileRecordIndexColumnName);
                    dataGridView1.SuspendLayout();
                    for (int n = dataView.Count - 1; n >= 0; --n)
                    {
                        int rowIndex = (int)dataView[n][colIndex];
                        if (dictionary.ContainsKey(rowIndex)) dataGridView1.Rows[n].Selected = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error loading selected records", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                finally
                {
                    dataGridView1.ResumeLayout();
                    dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
                    selectingRecords = false;
                }
            }            
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (!(this.isClosing || this.selectingRecords) && activated)
            {
                shapeFileReference.ClearSelectedRecords();
                int colIndex = dataTable.Columns.IndexOf(ShapeFileRecordIndexColumnName);
                    
                foreach (DataGridViewRow row in this.dataGridView1.SelectedRows)
                {
                    shapeFileReference.SelectRecord((int)row.Cells[colIndex].Value, true);
                }
                if (mapReference != null) mapReference.InvalidateAndClearBackground();
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
                try
                {
                    LoadAttributes(mapReference[cbCurrentLayer.SelectedIndex]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error loading attributes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cbCurrentLayer.SelectedIndex = -1;
                    ClearData();
                }
            }

        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            e.Value = this.dataView[e.RowIndex][e.ColumnIndex];
        }
    }


 

}
