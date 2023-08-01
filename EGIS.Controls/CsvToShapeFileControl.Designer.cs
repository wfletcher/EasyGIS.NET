namespace EGIS.Controls
{
    partial class CsvToShapeFileControl
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
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbXCoordField = new System.Windows.Forms.ComboBox();
            this.cbYCoordField = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.btnBrowseSource = new System.Windows.Forms.Button();
            this.btnBrowseShapeFile = new System.Windows.Forms.Button();
            this.txtDestination = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnConvert = new System.Windows.Forms.Button();
            this.ofdCsv = new System.Windows.Forms.OpenFileDialog();
            this.sfdShapeFile = new System.Windows.Forms.SaveFileDialog();
            this.lblCrsId = new System.Windows.Forms.Label();
            this.btnSelectCRS = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 65);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "X-Coord Field";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(178, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Y-Coord Field";
            // 
            // cbXCoordField
            // 
            this.cbXCoordField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbXCoordField.FormattingEnabled = true;
            this.cbXCoordField.Location = new System.Drawing.Point(3, 81);
            this.cbXCoordField.Name = "cbXCoordField";
            this.cbXCoordField.Size = new System.Drawing.Size(160, 21);
            this.cbXCoordField.TabIndex = 2;
            this.cbXCoordField.SelectedIndexChanged += new System.EventHandler(this.cbXCoordField_SelectedIndexChanged);
            // 
            // cbYCoordField
            // 
            this.cbYCoordField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbYCoordField.FormattingEnabled = true;
            this.cbYCoordField.Location = new System.Drawing.Point(178, 81);
            this.cbYCoordField.Name = "cbYCoordField";
            this.cbYCoordField.Size = new System.Drawing.Size(160, 21);
            this.cbYCoordField.TabIndex = 3;
            this.cbYCoordField.SelectedIndexChanged += new System.EventHandler(this.cbYCoordField_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Source File";
            // 
            // txtSource
            // 
            this.txtSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSource.Location = new System.Drawing.Point(69, 5);
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new System.Drawing.Size(190, 20);
            this.txtSource.TabIndex = 5;
            // 
            // btnBrowseSource
            // 
            this.btnBrowseSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseSource.Location = new System.Drawing.Point(265, 3);
            this.btnBrowseSource.Name = "btnBrowseSource";
            this.btnBrowseSource.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseSource.TabIndex = 6;
            this.btnBrowseSource.Text = "Browse..";
            this.btnBrowseSource.UseVisualStyleBackColor = true;
            this.btnBrowseSource.Click += new System.EventHandler(this.btnBrowseSource_Click);
            // 
            // btnBrowseShapeFile
            // 
            this.btnBrowseShapeFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseShapeFile.Location = new System.Drawing.Point(265, 32);
            this.btnBrowseShapeFile.Name = "btnBrowseShapeFile";
            this.btnBrowseShapeFile.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseShapeFile.TabIndex = 9;
            this.btnBrowseShapeFile.Text = "Browse..";
            this.btnBrowseShapeFile.UseVisualStyleBackColor = true;
            this.btnBrowseShapeFile.Click += new System.EventHandler(this.btnBrowseShapeFile_Click);
            // 
            // txtDestination
            // 
            this.txtDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestination.Location = new System.Drawing.Point(69, 34);
            this.txtDestination.Name = "txtDestination";
            this.txtDestination.Size = new System.Drawing.Size(190, 20);
            this.txtDestination.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 37);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Destination";
            // 
            // btnConvert
            // 
            this.btnConvert.Enabled = false;
            this.btnConvert.Location = new System.Drawing.Point(115, 137);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(112, 23);
            this.btnConvert.TabIndex = 10;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // ofdCsv
            // 
            this.ofdCsv.Filter = "csv files(*.csv)|*.csv|All files(*.*)|*.*";
            // 
            // sfdShapeFile
            // 
            this.sfdShapeFile.Filter = "ShapeFile(*.shp)|*.shp";
            // 
            // lblCrsId
            // 
            this.lblCrsId.AutoSize = true;
            this.lblCrsId.Location = new System.Drawing.Point(4, 113);
            this.lblCrsId.Name = "lblCrsId";
            this.lblCrsId.Size = new System.Drawing.Size(72, 13);
            this.lblCrsId.TabIndex = 12;
            this.lblCrsId.Text = "[CRS ID xxxx]";
            // 
            // btnSelectCRS
            // 
            this.btnSelectCRS.Location = new System.Drawing.Point(82, 108);
            this.btnSelectCRS.Name = "btnSelectCRS";
            this.btnSelectCRS.Size = new System.Drawing.Size(64, 23);
            this.btnSelectCRS.TabIndex = 13;
            this.btnSelectCRS.Text = "Select..";
            this.btnSelectCRS.UseVisualStyleBackColor = true;
            this.btnSelectCRS.Click += new System.EventHandler(this.btnSelectCRS_Click);
            // 
            // CsvToShapeFileControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnSelectCRS);
            this.Controls.Add(this.lblCrsId);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.btnBrowseShapeFile);
            this.Controls.Add(this.txtDestination);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnBrowseSource);
            this.Controls.Add(this.txtSource);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbYCoordField);
            this.Controls.Add(this.cbXCoordField);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "CsvToShapeFileControl";
            this.Size = new System.Drawing.Size(343, 165);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbXCoordField;
        private System.Windows.Forms.ComboBox cbYCoordField;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSource;
        private System.Windows.Forms.Button btnBrowseSource;
        private System.Windows.Forms.Button btnBrowseShapeFile;
        private System.Windows.Forms.TextBox txtDestination;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.OpenFileDialog ofdCsv;
        private System.Windows.Forms.SaveFileDialog sfdShapeFile;
        private System.Windows.Forms.Label lblCrsId;
        private System.Windows.Forms.Button btnSelectCRS;
    }
}
