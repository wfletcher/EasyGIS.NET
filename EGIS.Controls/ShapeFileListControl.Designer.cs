namespace EGIS.Controls
{
    partial class ShapeFileListControl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShapeFileListControl));
            this.lstShapefiles = new System.Windows.Forms.ListBox();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.layerContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miRemoveLayer = new System.Windows.Forms.ToolStripMenuItem();
            this.addLayerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomToLayerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomToSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.layerContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstShapefiles
            // 
            this.lstShapefiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lstShapefiles.FormattingEnabled = true;
            this.lstShapefiles.HorizontalScrollbar = true;
            this.lstShapefiles.Location = new System.Drawing.Point(3, 6);
            this.lstShapefiles.Name = "lstShapefiles";
            this.lstShapefiles.Size = new System.Drawing.Size(232, 108);
            this.lstShapefiles.TabIndex = 0;
            this.lstShapefiles.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lstShapefiles_MouseClick);
            this.lstShapefiles.SelectedIndexChanged += new System.EventHandler(this.lstShapefiles_SelectedIndexChanged);
            this.lstShapefiles.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lstShapefiles_MouseUp);
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnMoveUp.Image = ((System.Drawing.Image)(resources.GetObject("btnMoveUp.Image")));
            this.btnMoveUp.Location = new System.Drawing.Point(6, 116);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(32, 32);
            this.btnMoveUp.TabIndex = 1;
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnMoveDown.Image = ((System.Drawing.Image)(resources.GetObject("btnMoveDown.Image")));
            this.btnMoveDown.Location = new System.Drawing.Point(56, 116);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(32, 32);
            this.btnMoveDown.TabIndex = 2;
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Image = ((System.Drawing.Image)(resources.GetObject("btnRemove.Image")));
            this.btnRemove.Location = new System.Drawing.Point(202, 116);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(32, 32);
            this.btnRemove.TabIndex = 3;
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
            this.button1.Location = new System.Drawing.Point(164, 116);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(32, 32);
            this.button1.TabIndex = 4;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // layerContextMenu
            // 
            this.layerContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addLayerToolStripMenuItem,
            this.miRemoveLayer,
            this.zoomToLayerToolStripMenuItem,
            this.zoomToSelectionToolStripMenuItem});
            this.layerContextMenu.Name = "layerContextMenu";
            this.layerContextMenu.Size = new System.Drawing.Size(172, 114);
            this.layerContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.layerContextMenu_Opening);
            // 
            // miRemoveLayer
            // 
            this.miRemoveLayer.Name = "miRemoveLayer";
            this.miRemoveLayer.Size = new System.Drawing.Size(151, 22);
            this.miRemoveLayer.Text = "Remove Layer";
            this.miRemoveLayer.Click += new System.EventHandler(this.miRemoveLayer_Click);
            // 
            // addLayerToolStripMenuItem
            // 
            this.addLayerToolStripMenuItem.Name = "addLayerToolStripMenuItem";
            this.addLayerToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.addLayerToolStripMenuItem.Text = "Add Layer";
            this.addLayerToolStripMenuItem.Click += new System.EventHandler(this.addLayerToolStripMenuItem_Click);
            // 
            // zoomToLayerToolStripMenuItem
            // 
            this.zoomToLayerToolStripMenuItem.Name = "zoomToLayerToolStripMenuItem";
            this.zoomToLayerToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.zoomToLayerToolStripMenuItem.Text = "Zoom to Layer";
            this.zoomToLayerToolStripMenuItem.Click += new System.EventHandler(this.zoomToLayerToolStripMenuItem_Click);
            // 
            // zoomToSelectionToolStripMenuItem
            // 
            this.zoomToSelectionToolStripMenuItem.Name = "zoomToSelectionToolStripMenuItem";
            this.zoomToSelectionToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            this.zoomToSelectionToolStripMenuItem.Text = "Zoom to Selection";
            this.zoomToSelectionToolStripMenuItem.Click += new System.EventHandler(this.zoomToSelectionToolStripMenuItem_Click);
            // 
            // ShapeFileListControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnMoveDown);
            this.Controls.Add(this.btnMoveUp);
            this.Controls.Add(this.lstShapefiles);
            this.Name = "ShapeFileListControl";
            this.Size = new System.Drawing.Size(238, 150);
            this.layerContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lstShapefiles;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ContextMenuStrip layerContextMenu;
        private System.Windows.Forms.ToolStripMenuItem miRemoveLayer;
        private System.Windows.Forms.ToolStripMenuItem addLayerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomToLayerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomToSelectionToolStripMenuItem;

    }
}
