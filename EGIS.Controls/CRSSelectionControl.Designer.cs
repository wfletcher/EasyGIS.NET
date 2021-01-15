namespace EGIS.Controls
{
    partial class CRSSelectionControl
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnFindEPGS = new System.Windows.Forms.Button();
            this.nudEPGS = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.cbSelectedCRS = new System.Windows.Forms.ComboBox();
            this.rbProjected = new System.Windows.Forms.RadioButton();
            this.rbGeographic = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnParseWKT = new System.Windows.Forms.Button();
            this.txtWKT = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lstRecentCRS = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudEPGS)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnFindEPGS);
            this.groupBox1.Controls.Add(this.nudEPGS);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cbSelectedCRS);
            this.groupBox1.Controls.Add(this.rbProjected);
            this.groupBox1.Controls.Add(this.rbGeographic);
            this.groupBox1.Location = new System.Drawing.Point(3, 90);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(346, 102);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Predefined Coordinate Reference Systems";
            // 
            // btnFindEPGS
            // 
            this.btnFindEPGS.Location = new System.Drawing.Point(168, 70);
            this.btnFindEPGS.Name = "btnFindEPGS";
            this.btnFindEPGS.Size = new System.Drawing.Size(75, 23);
            this.btnFindEPGS.TabIndex = 5;
            this.btnFindEPGS.Text = "Find";
            this.btnFindEPGS.UseVisualStyleBackColor = true;
            this.btnFindEPGS.Click += new System.EventHandler(this.btnFindEPGS_Click);
            // 
            // nudEPGS
            // 
            this.nudEPGS.Location = new System.Drawing.Point(76, 71);
            this.nudEPGS.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudEPGS.Name = "nudEPGS";
            this.nudEPGS.Size = new System.Drawing.Size(85, 20);
            this.nudEPGS.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "EPSG Code";
            // 
            // cbSelectedCRS
            // 
            this.cbSelectedCRS.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSelectedCRS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSelectedCRS.FormattingEnabled = true;
            this.cbSelectedCRS.Location = new System.Drawing.Point(6, 42);
            this.cbSelectedCRS.Name = "cbSelectedCRS";
            this.cbSelectedCRS.Size = new System.Drawing.Size(334, 21);
            this.cbSelectedCRS.TabIndex = 2;
            this.cbSelectedCRS.SelectedIndexChanged += new System.EventHandler(this.cbSelectedCRS_SelectedIndexChanged);
            // 
            // rbProjected
            // 
            this.rbProjected.AutoSize = true;
            this.rbProjected.Location = new System.Drawing.Point(92, 19);
            this.rbProjected.Name = "rbProjected";
            this.rbProjected.Size = new System.Drawing.Size(70, 17);
            this.rbProjected.TabIndex = 1;
            this.rbProjected.Text = "Projected";
            this.rbProjected.UseVisualStyleBackColor = true;
            this.rbProjected.CheckedChanged += new System.EventHandler(this.rbProjected_CheckedChanged);
            // 
            // rbGeographic
            // 
            this.rbGeographic.AutoSize = true;
            this.rbGeographic.Checked = true;
            this.rbGeographic.Location = new System.Drawing.Point(6, 19);
            this.rbGeographic.Name = "rbGeographic";
            this.rbGeographic.Size = new System.Drawing.Size(80, 17);
            this.rbGeographic.TabIndex = 0;
            this.rbGeographic.TabStop = true;
            this.rbGeographic.Text = "Geographic";
            this.rbGeographic.UseVisualStyleBackColor = true;
            this.rbGeographic.CheckedChanged += new System.EventHandler(this.rbGeographic_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnParseWKT);
            this.groupBox2.Controls.Add(this.txtWKT);
            this.groupBox2.Location = new System.Drawing.Point(3, 198);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(346, 123);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "From WKT (ESRI)";
            // 
            // btnParseWKT
            // 
            this.btnParseWKT.Location = new System.Drawing.Point(6, 91);
            this.btnParseWKT.Name = "btnParseWKT";
            this.btnParseWKT.Size = new System.Drawing.Size(75, 23);
            this.btnParseWKT.TabIndex = 1;
            this.btnParseWKT.Text = "Parse";
            this.btnParseWKT.UseVisualStyleBackColor = true;
            // 
            // txtWKT
            // 
            this.txtWKT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtWKT.Location = new System.Drawing.Point(6, 19);
            this.txtWKT.Multiline = true;
            this.txtWKT.Name = "txtWKT";
            this.txtWKT.Size = new System.Drawing.Size(334, 66);
            this.txtWKT.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.lstRecentCRS);
            this.groupBox3.Location = new System.Drawing.Point(3, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(346, 81);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Recent";
            // 
            // lstRecentCRS
            // 
            this.lstRecentCRS.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstRecentCRS.FormattingEnabled = true;
            this.lstRecentCRS.Location = new System.Drawing.Point(6, 20);
            this.lstRecentCRS.Name = "lstRecentCRS";
            this.lstRecentCRS.Size = new System.Drawing.Size(334, 56);
            this.lstRecentCRS.TabIndex = 0;
            this.lstRecentCRS.SelectedIndexChanged += new System.EventHandler(this.lstRecentCRS_SelectedIndexChanged);
            // 
            // CRSSelectionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "CRSSelectionControl";
            this.Size = new System.Drawing.Size(352, 327);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudEPGS)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbProjected;
        private System.Windows.Forms.RadioButton rbGeographic;
        private System.Windows.Forms.Button btnFindEPGS;
        private System.Windows.Forms.NumericUpDown nudEPGS;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbSelectedCRS;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnParseWKT;
        private System.Windows.Forms.TextBox txtWKT;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListBox lstRecentCRS;
    }
}
