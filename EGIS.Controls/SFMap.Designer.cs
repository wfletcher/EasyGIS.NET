namespace EGIS.Controls
{
    partial class SFMap
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
            if (disposing)
            {
                if(components != null) components.Dispose();
                if(screenBuf != null) screenBuf.Dispose();
                if (backgroundBuffer != null) backgroundBuffer.Dispose();
                if (foregroundBuffer != null) foregroundBuffer.Dispose();
                
                if (this._backgroundShapeFiles != null)
                {
                    for (int n = 0; n < _backgroundShapeFiles.Count; ++n)
                    {
                        _backgroundShapeFiles[n].Dispose();
                    }
                    _backgroundShapeFiles = null;
                }
                if (this._foregroundShapeFiles != null)
                {
                    for (int n = 0; n < _foregroundShapeFiles.Count; ++n)
                    {
                        _foregroundShapeFiles[n].Dispose();
                    }
                    _foregroundShapeFiles = null;
                }
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
			this.SuspendLayout();
			// 
			// SFMap
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.DoubleBuffered = true;
			this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.Name = "SFMap";
			this.Size = new System.Drawing.Size(607, 398);
			this.ResumeLayout(false);

        }

        #endregion
    }
}
