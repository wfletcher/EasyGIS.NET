namespace egis
{
    partial class ViewAttributesForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            if (disposing)
            {
                if (this.dataTable != null)
                {                
                    dataGridView1.DataSource = null;
                    dataGridView1.Rows.Clear();
                    dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
                    dataGridView1.CellValueNeeded -= dataGridView1_CellValueNeeded;
                    this.dataView.Dispose();
                    this.dataTable.Clear();
                    this.dataTable.Rows.Clear();
                    this.dataTable.Dispose();
                }
                this.dataTable = null;
                this.dataView = null;
                if (mapReference != null)
                {
                    mapReference.ShapeFilesChanged -= mapReference_ShapeFilesChanged;
                    mapReference.SelectedRecordsChanged -= mapReference_SelectedRecordsChanged;
                }
            }
            this.mapReference = null;
            this.shapeFileReference = null;
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewAttributesForm));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.formStatusStrip = new System.Windows.Forms.StatusStrip();
            this.tslblRecords = new System.Windows.Forms.ToolStripStatusLabel();
            this.pbLoading = new System.Windows.Forms.ToolStripProgressBar();
            this.tslblErrors = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtSelect = new System.Windows.Forms.TextBox();
            this.btnSelect = new System.Windows.Forms.Button();
            this.btnFilter = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cbCurrentLayer = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.formStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(13, 67);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(632, 242);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridView1_CellValueNeeded);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // formStatusStrip
            // 
            this.formStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslblRecords,
            this.pbLoading,
            this.tslblErrors});
            this.formStatusStrip.Location = new System.Drawing.Point(0, 321);
            this.formStatusStrip.Name = "formStatusStrip";
            this.formStatusStrip.Size = new System.Drawing.Size(657, 22);
            this.formStatusStrip.TabIndex = 1;
            this.formStatusStrip.Text = "statusStrip1";
            // 
            // tslblRecords
            // 
            this.tslblRecords.Name = "tslblRecords";
            this.tslblRecords.Size = new System.Drawing.Size(87, 17);
            this.tslblRecords.Text = "[Total Records]";
            // 
            // pbLoading
            // 
            this.pbLoading.Name = "pbLoading";
            this.pbLoading.Size = new System.Drawing.Size(100, 16);
            this.pbLoading.Visible = false;
            // 
            // tslblErrors
            // 
            this.tslblErrors.Name = "tslblErrors";
            this.tslblErrors.Size = new System.Drawing.Size(0, 17);
            // 
            // txtSelect
            // 
            this.txtSelect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSelect.Location = new System.Drawing.Point(93, 41);
            this.txtSelect.Name = "txtSelect";
            this.txtSelect.Size = new System.Drawing.Size(387, 20);
            this.txtSelect.TabIndex = 2;
            // 
            // btnSelect
            // 
            this.btnSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelect.Location = new System.Drawing.Point(489, 38);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(75, 23);
            this.btnSelect.TabIndex = 3;
            this.btnSelect.Text = "Select";
            this.btnSelect.UseVisualStyleBackColor = true;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // btnFilter
            // 
            this.btnFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilter.Location = new System.Drawing.Point(570, 38);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(75, 23);
            this.btnFilter.TabIndex = 4;
            this.btnFilter.Text = "Filter";
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Attributes Layer";
            // 
            // cbCurrentLayer
            // 
            this.cbCurrentLayer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbCurrentLayer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCurrentLayer.FormattingEnabled = true;
            this.cbCurrentLayer.Location = new System.Drawing.Point(93, 10);
            this.cbCurrentLayer.Name = "cbCurrentLayer";
            this.cbCurrentLayer.Size = new System.Drawing.Size(552, 21);
            this.cbCurrentLayer.TabIndex = 6;
            this.cbCurrentLayer.SelectedIndexChanged += new System.EventHandler(this.cbCurrentLayer_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Where Clause";
            // 
            // ViewAttributesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(657, 343);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbCurrentLayer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnFilter);
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.txtSelect);
            this.Controls.Add(this.formStatusStrip);
            this.Controls.Add(this.dataGridView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ViewAttributesForm";
            this.Text = "Attribute List";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.formStatusStrip.ResumeLayout(false);
            this.formStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.StatusStrip formStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel tslblRecords;
        private System.Windows.Forms.ToolStripProgressBar pbLoading;
        private System.Windows.Forms.TextBox txtSelect;
        private System.Windows.Forms.ToolStripStatusLabel tslblErrors;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnFilter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbCurrentLayer;
        private System.Windows.Forms.Label label2;
    }
}