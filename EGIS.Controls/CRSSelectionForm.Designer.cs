namespace EGIS.Controls
{
    partial class CRSSelectionForm
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
            this.crsSelectionControl1 = new EGIS.Controls.CRSSelectionControl();
            this.SuspendLayout();
            // 
            // crsSelectionControl1
            // 
            this.crsSelectionControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.crsSelectionControl1.Location = new System.Drawing.Point(2, 2);
            this.crsSelectionControl1.Name = "crsSelectionControl1";
            this.crsSelectionControl1.Size = new System.Drawing.Size(499, 240);
            this.crsSelectionControl1.TabIndex = 0;
            // 
            // CRSSelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(501, 288);
            this.Controls.Add(this.crsSelectionControl1);
            this.Name = "CRSSelectionForm";
            this.Text = "CRSSelectionForm";
            this.ResumeLayout(false);

        }

        #endregion

        private CRSSelectionControl crsSelectionControl1;
    }
}