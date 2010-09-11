namespace egis
{
    partial class LicenceAgreementForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenceAgreementForm));
            this.rtbLicence = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAccept = new System.Windows.Forms.Button();
            this.btnDontAccept = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // rtbLicence
            // 
            this.rtbLicence.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLicence.DetectUrls = false;
            this.rtbLicence.Location = new System.Drawing.Point(12, 47);
            this.rtbLicence.Name = "rtbLicence";
            this.rtbLicence.ReadOnly = true;
            this.rtbLicence.Size = new System.Drawing.Size(461, 237);
            this.rtbLicence.TabIndex = 0;
            this.rtbLicence.Text = "";
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(461, 37);
            this.label1.TabIndex = 1;
            this.label1.Text = "Please read the following licence agreement. To continue using Easy GIS .NET Desk" +
                "top Edition, you must accept the agreement.";
            // 
            // btnAccept
            // 
            this.btnAccept.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnAccept.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.btnAccept.Location = new System.Drawing.Point(76, 292);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(143, 23);
            this.btnAccept.TabIndex = 2;
            this.btnAccept.Text = "I Agree to Licence Terms";
            this.btnAccept.UseVisualStyleBackColor = true;
            // 
            // btnDontAccept
            // 
            this.btnDontAccept.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnDontAccept.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnDontAccept.Location = new System.Drawing.Point(225, 292);
            this.btnDontAccept.Name = "btnDontAccept";
            this.btnDontAccept.Size = new System.Drawing.Size(183, 23);
            this.btnDontAccept.TabIndex = 3;
            this.btnDontAccept.Text = "I Do NOT Agree to Licence Terms";
            this.btnDontAccept.UseVisualStyleBackColor = true;
            // 
            // LicenceAgreementForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 324);
            this.Controls.Add(this.btnDontAccept);
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.rtbLicence);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LicenceAgreementForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Licence Agreement";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbLicence;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.Button btnDontAccept;
    }
}