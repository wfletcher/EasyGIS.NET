namespace egis
{
    partial class CsvToShapeFileForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CsvToShapeFileForm));
            this.button1 = new System.Windows.Forms.Button();
            this.csvToShapeFileControl1 = new EGIS.Controls.CsvToShapeFileControl();
            this.convertProgressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(271, 147);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // csvToShapeFileControl1
            // 
            this.csvToShapeFileControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.csvToShapeFileControl1.DestinationShapeFile = "";
            this.csvToShapeFileControl1.Location = new System.Drawing.Point(3, 2);
            this.csvToShapeFileControl1.Name = "csvToShapeFileControl1";
            this.csvToShapeFileControl1.Size = new System.Drawing.Size(343, 138);
            this.csvToShapeFileControl1.SourceDataFile = "";
            this.csvToShapeFileControl1.TabIndex = 0;
            this.csvToShapeFileControl1.ConvertShapeFileProgressChanged += new System.EventHandler<EGIS.ShapeFileLib.ConvertShapeFileEventArgs>(this.csvToShapeFileControl1_ConvertShapeFileProgressChanged);
            this.csvToShapeFileControl1.Load += new System.EventHandler(this.csvToShapeFileControl1_Load);
            // 
            // convertProgressBar
            // 
            this.convertProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.convertProgressBar.Location = new System.Drawing.Point(3, 147);
            this.convertProgressBar.Name = "convertProgressBar";
            this.convertProgressBar.Size = new System.Drawing.Size(262, 23);
            this.convertProgressBar.TabIndex = 2;
            // 
            // CsvToShapeFileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(349, 175);
            this.Controls.Add(this.convertProgressBar);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.csvToShapeFileControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CsvToShapeFileForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add X,Y Data";
            this.ResumeLayout(false);

        }

        #endregion

        private EGIS.Controls.CsvToShapeFileControl csvToShapeFileControl1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ProgressBar convertProgressBar;
    }
}