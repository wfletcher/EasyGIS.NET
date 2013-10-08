namespace egis
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.miNewProject = new System.Windows.Forms.ToolStripMenuItem();
            this.newProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miOpenProject = new System.Windows.Forms.ToolStripMenuItem();
            this.recentProjectsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.miOpenShapeFile = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.miSaveProject = new System.Windows.Forms.ToolStripMenuItem();
            this.saveProjectAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.saveMapImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miMapBackgroundColor = new System.Windows.Forms.ToolStripMenuItem();
            this.miMercatorProjection = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.renderQualityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.highToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.useNativeFileMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.displayShapeAttributesWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disablePanSelectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ofdShapeFile = new System.Windows.Forms.OpenFileDialog();
            this.shapeFileRenderPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.ofdProject = new System.Windows.Forms.OpenFileDialog();
            this.sfdProject = new System.Windows.Forms.SaveFileDialog();
            this.mapColorDialog = new System.Windows.Forms.ColorDialog();
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.newToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.openToolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.saveToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnPanLeft = new System.Windows.Forms.ToolStripButton();
            this.tsBtnPanRight = new System.Windows.Forms.ToolStripButton();
            this.tsBtnPanUp = new System.Windows.Forms.ToolStripButton();
            this.tsBtnPanDown = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnZoomIn = new System.Windows.Forms.ToolStripButton();
            this.tsBtnZoomOut = new System.Windows.Forms.ToolStripButton();
            this.tsBtnZoomFull = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsBtnSelectRect = new System.Windows.Forms.ToolStripButton();
            this.tsBtnSelectCircle = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tsTxtFind = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.tscbSearchLayers = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.helpToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsLabelCurrentZoom = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsLabelVisibleArea = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsLblMapMousePos = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.tsLblSelectMessage = new System.Windows.Forms.ToolStripStatusLabel();
            this.shapeFileListControl1 = new EGIS.Controls.ShapeFileListControl();
            this.sfMap1 = new EGIS.Controls.SFMap();
            this.sfdMapImage = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1.SuspendLayout();
            this.mainToolStrip.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miNewProject,
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(949, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // miNewProject
            // 
            this.miNewProject.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newProjectToolStripMenuItem,
            this.miOpenProject,
            this.recentProjectsMenuItem,
            this.toolStripSeparator5,
            this.miOpenShapeFile,
            this.toolStripSeparator4,
            this.miSaveProject,
            this.saveProjectAsToolStripMenuItem,
            this.exportProjectToolStripMenuItem,
            this.toolStripSeparator7,
            this.saveMapImageToolStripMenuItem,
            this.toolStripSeparator11,
            this.printToolStripMenuItem});
            this.miNewProject.Name = "miNewProject";
            this.miNewProject.Size = new System.Drawing.Size(37, 20);
            this.miNewProject.Text = "File";
            // 
            // newProjectToolStripMenuItem
            // 
            this.newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            this.newProjectToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.newProjectToolStripMenuItem.Text = "New Project";
            this.newProjectToolStripMenuItem.Click += new System.EventHandler(this.newProjectToolStripMenuItem_Click);
            // 
            // miOpenProject
            // 
            this.miOpenProject.Name = "miOpenProject";
            this.miOpenProject.Size = new System.Drawing.Size(272, 22);
            this.miOpenProject.Text = "Open Project";
            this.miOpenProject.Click += new System.EventHandler(this.miOpenProject_Click);
            // 
            // recentProjectsMenuItem
            // 
            this.recentProjectsMenuItem.Name = "recentProjectsMenuItem";
            this.recentProjectsMenuItem.Size = new System.Drawing.Size(272, 22);
            this.recentProjectsMenuItem.Text = "Recent Projects";
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(269, 6);
            // 
            // miOpenShapeFile
            // 
            this.miOpenShapeFile.Name = "miOpenShapeFile";
            this.miOpenShapeFile.Size = new System.Drawing.Size(272, 22);
            this.miOpenShapeFile.Text = "Add Shape File";
            this.miOpenShapeFile.Click += new System.EventHandler(this.miOpenShapeFile_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(269, 6);
            // 
            // miSaveProject
            // 
            this.miSaveProject.Name = "miSaveProject";
            this.miSaveProject.Size = new System.Drawing.Size(272, 22);
            this.miSaveProject.Text = "Save Project";
            this.miSaveProject.Click += new System.EventHandler(this.miSaveProject_Click);
            // 
            // saveProjectAsToolStripMenuItem
            // 
            this.saveProjectAsToolStripMenuItem.Name = "saveProjectAsToolStripMenuItem";
            this.saveProjectAsToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.saveProjectAsToolStripMenuItem.Text = "Save Project As";
            this.saveProjectAsToolStripMenuItem.Click += new System.EventHandler(this.saveProjectAsToolStripMenuItem_Click);
            // 
            // exportProjectToolStripMenuItem
            // 
            this.exportProjectToolStripMenuItem.Name = "exportProjectToolStripMenuItem";
            this.exportProjectToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.exportProjectToolStripMenuItem.Text = "Export Project (for use in web edition)";
            this.exportProjectToolStripMenuItem.Click += new System.EventHandler(this.exportProjectToolStripMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(269, 6);
            // 
            // saveMapImageToolStripMenuItem
            // 
            this.saveMapImageToolStripMenuItem.Name = "saveMapImageToolStripMenuItem";
            this.saveMapImageToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.saveMapImageToolStripMenuItem.Text = "Save Map Image";
            this.saveMapImageToolStripMenuItem.Click += new System.EventHandler(this.saveMapImageToolStripMenuItem_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(269, 6);
            // 
            // printToolStripMenuItem
            // 
            this.printToolStripMenuItem.Name = "printToolStripMenuItem";
            this.printToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.printToolStripMenuItem.Text = "Print Map";
            this.printToolStripMenuItem.Click += new System.EventHandler(this.printToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miMapBackgroundColor,
            this.miMercatorProjection,
            this.toolStripSeparator8,
            this.renderQualityToolStripMenuItem,
            this.useNativeFileMappingToolStripMenuItem,
            this.toolStripSeparator9,
            this.displayShapeAttributesWindowToolStripMenuItem,
            this.disablePanSelectToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // miMapBackgroundColor
            // 
            this.miMapBackgroundColor.Name = "miMapBackgroundColor";
            this.miMapBackgroundColor.Size = new System.Drawing.Size(248, 22);
            this.miMapBackgroundColor.Text = "Map Background Color";
            this.miMapBackgroundColor.Click += new System.EventHandler(this.miMapBackgroundColor_Click);
            // 
            // miMercatorProjection
            // 
            this.miMercatorProjection.Name = "miMercatorProjection";
            this.miMercatorProjection.Size = new System.Drawing.Size(248, 22);
            this.miMercatorProjection.Text = "Mercator Projection";
            this.miMercatorProjection.Click += new System.EventHandler(this.miMercatorProjection_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(245, 6);
            // 
            // renderQualityToolStripMenuItem
            // 
            this.renderQualityToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.highToolStripMenuItem,
            this.lowToolStripMenuItem,
            this.autoToolStripMenuItem});
            this.renderQualityToolStripMenuItem.Name = "renderQualityToolStripMenuItem";
            this.renderQualityToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.renderQualityToolStripMenuItem.Text = "Render Quality";
            // 
            // highToolStripMenuItem
            // 
            this.highToolStripMenuItem.Name = "highToolStripMenuItem";
            this.highToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.highToolStripMenuItem.Text = "High";
            this.highToolStripMenuItem.Click += new System.EventHandler(this.highToolStripMenuItem_Click);
            // 
            // lowToolStripMenuItem
            // 
            this.lowToolStripMenuItem.Name = "lowToolStripMenuItem";
            this.lowToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.lowToolStripMenuItem.Text = "Low";
            this.lowToolStripMenuItem.Click += new System.EventHandler(this.lowToolStripMenuItem_Click);
            // 
            // autoToolStripMenuItem
            // 
            this.autoToolStripMenuItem.Checked = true;
            this.autoToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoToolStripMenuItem.Name = "autoToolStripMenuItem";
            this.autoToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.autoToolStripMenuItem.Text = "Auto";
            this.autoToolStripMenuItem.Click += new System.EventHandler(this.autoToolStripMenuItem_Click);
            // 
            // useNativeFileMappingToolStripMenuItem
            // 
            this.useNativeFileMappingToolStripMenuItem.Name = "useNativeFileMappingToolStripMenuItem";
            this.useNativeFileMappingToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.useNativeFileMappingToolStripMenuItem.Text = "Use Native File Mapping";
            this.useNativeFileMappingToolStripMenuItem.Click += new System.EventHandler(this.useNativeFileMappingToolStripMenuItem_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(245, 6);
            // 
            // displayShapeAttributesWindowToolStripMenuItem
            // 
            this.displayShapeAttributesWindowToolStripMenuItem.Name = "displayShapeAttributesWindowToolStripMenuItem";
            this.displayShapeAttributesWindowToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.displayShapeAttributesWindowToolStripMenuItem.Text = "Display shape Attributes Window";
            this.displayShapeAttributesWindowToolStripMenuItem.Click += new System.EventHandler(this.displayShapeAttributesWindowToolStripMenuItem_Click);
            // 
            // disablePanSelectToolStripMenuItem
            // 
            this.disablePanSelectToolStripMenuItem.Checked = true;
            this.disablePanSelectToolStripMenuItem.CheckOnClick = true;
            this.disablePanSelectToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.disablePanSelectToolStripMenuItem.Name = "disablePanSelectToolStripMenuItem";
            this.disablePanSelectToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.disablePanSelectToolStripMenuItem.Text = "DisablePanSelect";
            this.disablePanSelectToolStripMenuItem.Visible = false;
            this.disablePanSelectToolStripMenuItem.Click += new System.EventHandler(this.disablePanSelectToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutMenuItem
            // 
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(181, 22);
            this.aboutMenuItem.Text = "About Easy GIS .NET";
            this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
            // 
            // ofdShapeFile
            // 
            this.ofdShapeFile.Filter = "shape files|*.shp";
            this.ofdShapeFile.Multiselect = true;
            this.ofdShapeFile.RestoreDirectory = true;
            this.ofdShapeFile.Title = "Add New Layer to Project";
            // 
            // shapeFileRenderPropertyGrid
            // 
            this.shapeFileRenderPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.shapeFileRenderPropertyGrid.Location = new System.Drawing.Point(6, 311);
            this.shapeFileRenderPropertyGrid.Name = "shapeFileRenderPropertyGrid";
            this.shapeFileRenderPropertyGrid.Size = new System.Drawing.Size(251, 225);
            this.shapeFileRenderPropertyGrid.TabIndex = 15;
            this.shapeFileRenderPropertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.shapeFileRenderPropertyGrid_PropertyValueChanged);
            // 
            // ofdProject
            // 
            this.ofdProject.Filter = "Easy GIS Project File (*.egp) |*.egp";
            this.ofdProject.RestoreDirectory = true;
            this.ofdProject.Title = "Open Easy GIS .NET Project";
            // 
            // sfdProject
            // 
            this.sfdProject.Filter = "Easy GIS Project File (*.egp) |*.egp";
            this.sfdProject.RestoreDirectory = true;
            this.sfdProject.Title = "Save Easy GIS .NET Project";
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripButton,
            this.openToolStripButton1,
            this.saveToolStripButton,
            this.toolStripSeparator,
            this.tsBtnPanLeft,
            this.tsBtnPanRight,
            this.tsBtnPanUp,
            this.tsBtnPanDown,
            this.toolStripSeparator3,
            this.tsBtnZoomIn,
            this.tsBtnZoomOut,
            this.tsBtnZoomFull,
            this.toolStripSeparator1,
            this.tsBtnSelectRect,
            this.tsBtnSelectCircle,
            this.toolStripSeparator10,
            this.toolStripLabel2,
            this.tsTxtFind,
            this.toolStripLabel3,
            this.tscbSearchLayers,
            this.toolStripSeparator2,
            this.helpToolStripButton});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 24);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Size = new System.Drawing.Size(949, 39);
            this.mainToolStrip.TabIndex = 17;
            this.mainToolStrip.Text = "Main Tool Strip";
            // 
            // newToolStripButton
            // 
            this.newToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.newToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripButton.Image")));
            this.newToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripButton.Name = "newToolStripButton";
            this.newToolStripButton.Size = new System.Drawing.Size(36, 36);
            this.newToolStripButton.Text = "&New";
            this.newToolStripButton.ToolTipText = "New Project";
            this.newToolStripButton.Click += new System.EventHandler(this.newToolStripButton_Click);
            // 
            // openToolStripButton1
            // 
            this.openToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.openToolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripButton1.Image")));
            this.openToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripButton1.Name = "openToolStripButton1";
            this.openToolStripButton1.Size = new System.Drawing.Size(36, 36);
            this.openToolStripButton1.Text = "&Open";
            this.openToolStripButton1.ToolTipText = "Open Project";
            this.openToolStripButton1.Click += new System.EventHandler(this.openToolStripButton1_Click);
            // 
            // saveToolStripButton
            // 
            this.saveToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.saveToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripButton.Image")));
            this.saveToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripButton.Name = "saveToolStripButton";
            this.saveToolStripButton.Size = new System.Drawing.Size(36, 36);
            this.saveToolStripButton.Text = "&Save";
            this.saveToolStripButton.ToolTipText = "Save Project";
            this.saveToolStripButton.Click += new System.EventHandler(this.saveToolStripButton_Click);
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(6, 39);
            // 
            // tsBtnPanLeft
            // 
            this.tsBtnPanLeft.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnPanLeft.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnPanLeft.Image")));
            this.tsBtnPanLeft.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnPanLeft.Name = "tsBtnPanLeft";
            this.tsBtnPanLeft.Size = new System.Drawing.Size(36, 36);
            this.tsBtnPanLeft.Text = "toolStripButton1";
            this.tsBtnPanLeft.ToolTipText = "Pan Left";
            this.tsBtnPanLeft.Click += new System.EventHandler(this.tsBtnPanLeft_Click);
            // 
            // tsBtnPanRight
            // 
            this.tsBtnPanRight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnPanRight.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnPanRight.Image")));
            this.tsBtnPanRight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnPanRight.Name = "tsBtnPanRight";
            this.tsBtnPanRight.Size = new System.Drawing.Size(36, 36);
            this.tsBtnPanRight.Text = "toolStripButton2";
            this.tsBtnPanRight.ToolTipText = "Pan Right";
            this.tsBtnPanRight.Click += new System.EventHandler(this.tsBtnPanRight_Click);
            // 
            // tsBtnPanUp
            // 
            this.tsBtnPanUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnPanUp.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnPanUp.Image")));
            this.tsBtnPanUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnPanUp.Name = "tsBtnPanUp";
            this.tsBtnPanUp.Size = new System.Drawing.Size(36, 36);
            this.tsBtnPanUp.Text = "toolStripButton3";
            this.tsBtnPanUp.ToolTipText = "Pan Up";
            this.tsBtnPanUp.Click += new System.EventHandler(this.tsBtnPanUp_Click);
            // 
            // tsBtnPanDown
            // 
            this.tsBtnPanDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnPanDown.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnPanDown.Image")));
            this.tsBtnPanDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnPanDown.Name = "tsBtnPanDown";
            this.tsBtnPanDown.Size = new System.Drawing.Size(36, 36);
            this.tsBtnPanDown.Text = "toolStripButton4";
            this.tsBtnPanDown.ToolTipText = "Pan Down";
            this.tsBtnPanDown.Click += new System.EventHandler(this.tsBtnPanDown_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 39);
            // 
            // tsBtnZoomIn
            // 
            this.tsBtnZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnZoomIn.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnZoomIn.Image")));
            this.tsBtnZoomIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnZoomIn.Name = "tsBtnZoomIn";
            this.tsBtnZoomIn.Size = new System.Drawing.Size(36, 36);
            this.tsBtnZoomIn.Text = "Zoom In";
            this.tsBtnZoomIn.ToolTipText = "Zoom In";
            this.tsBtnZoomIn.Click += new System.EventHandler(this.tsBtnZoomIn_Click);
            // 
            // tsBtnZoomOut
            // 
            this.tsBtnZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnZoomOut.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnZoomOut.Image")));
            this.tsBtnZoomOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnZoomOut.Name = "tsBtnZoomOut";
            this.tsBtnZoomOut.Size = new System.Drawing.Size(36, 36);
            this.tsBtnZoomOut.Text = "Zoom Out";
            this.tsBtnZoomOut.ToolTipText = "Zoom Out";
            this.tsBtnZoomOut.Click += new System.EventHandler(this.tsBtnZoomOut_Click);
            // 
            // tsBtnZoomFull
            // 
            this.tsBtnZoomFull.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnZoomFull.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnZoomFull.Image")));
            this.tsBtnZoomFull.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnZoomFull.Name = "tsBtnZoomFull";
            this.tsBtnZoomFull.Size = new System.Drawing.Size(36, 36);
            this.tsBtnZoomFull.Text = "Zoom Full";
            this.tsBtnZoomFull.ToolTipText = "Zoom to Full Extents of Map";
            this.tsBtnZoomFull.Click += new System.EventHandler(this.tsBtnZoomFull_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 39);
            // 
            // tsBtnSelectRect
            // 
            this.tsBtnSelectRect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnSelectRect.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnSelectRect.Image")));
            this.tsBtnSelectRect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnSelectRect.Name = "tsBtnSelectRect";
            this.tsBtnSelectRect.Size = new System.Drawing.Size(36, 36);
            this.tsBtnSelectRect.Text = "Select Rectangle";
            this.tsBtnSelectRect.Visible = false;
            this.tsBtnSelectRect.Click += new System.EventHandler(this.tsBtnSelectRect_Click);
            // 
            // tsBtnSelectCircle
            // 
            this.tsBtnSelectCircle.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsBtnSelectCircle.Image = ((System.Drawing.Image)(resources.GetObject("tsBtnSelectCircle.Image")));
            this.tsBtnSelectCircle.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnSelectCircle.Name = "tsBtnSelectCircle";
            this.tsBtnSelectCircle.Size = new System.Drawing.Size(36, 36);
            this.tsBtnSelectCircle.Text = "Select Circle";
            this.tsBtnSelectCircle.Visible = false;
            this.tsBtnSelectCircle.Click += new System.EventHandler(this.tsBtnSelectCircle_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(6, 39);
            this.toolStripSeparator10.Visible = false;
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(30, 36);
            this.toolStripLabel2.Text = "Find";
            this.toolStripLabel2.Visible = false;
            // 
            // tsTxtFind
            // 
            this.tsTxtFind.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tsTxtFind.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tsTxtFind.Name = "tsTxtFind";
            this.tsTxtFind.Size = new System.Drawing.Size(130, 39);
            this.tsTxtFind.Visible = false;
            this.tsTxtFind.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tsTxtFind_KeyDown);
            this.tsTxtFind.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tsTxtFind_KeyPress);
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStripLabel3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripLabel3.Margin = new System.Windows.Forms.Padding(2, 1, 2, 2);
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.Size = new System.Drawing.Size(17, 36);
            this.toolStripLabel3.Text = "in";
            this.toolStripLabel3.Visible = false;
            // 
            // tscbSearchLayers
            // 
            this.tscbSearchLayers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscbSearchLayers.Items.AddRange(new object[] {
            "selected layer",
            "all layers"});
            this.tscbSearchLayers.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.tscbSearchLayers.Name = "tscbSearchLayers";
            this.tscbSearchLayers.Size = new System.Drawing.Size(90, 39);
            this.tscbSearchLayers.Visible = false;
            this.tscbSearchLayers.SelectedIndexChanged += new System.EventHandler(this.tscbSearchLayers_SelectedIndexChanged);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 39);
            this.toolStripSeparator2.Visible = false;
            // 
            // helpToolStripButton
            // 
            this.helpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.helpToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("helpToolStripButton.Image")));
            this.helpToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.helpToolStripButton.Name = "helpToolStripButton";
            this.helpToolStripButton.Size = new System.Drawing.Size(36, 36);
            this.helpToolStripButton.Text = "He&lp";
            this.helpToolStripButton.ToolTipText = "About Easy GIS .NET";
            this.helpToolStripButton.Click += new System.EventHandler(this.helpToolStripButton_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.tsLabelCurrentZoom,
            this.tsLabelVisibleArea,
            this.tsLblMapMousePos,
            this.mainProgressBar,
            this.tsLblSelectMessage});
            this.statusStrip1.Location = new System.Drawing.Point(0, 543);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(949, 22);
            this.statusStrip1.TabIndex = 18;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(85, 17);
            this.toolStripStatusLabel1.Text = "Current Zoom:";
            // 
            // tsLabelCurrentZoom
            // 
            this.tsLabelCurrentZoom.Name = "tsLabelCurrentZoom";
            this.tsLabelCurrentZoom.Size = new System.Drawing.Size(22, 17);
            this.tsLabelCurrentZoom.Text = "1.0";
            // 
            // tsLabelVisibleArea
            // 
            this.tsLabelVisibleArea.Name = "tsLabelVisibleArea";
            this.tsLabelVisibleArea.Size = new System.Drawing.Size(60, 17);
            this.tsLabelVisibleArea.Text = "[0m x 0m]";
            // 
            // tsLblMapMousePos
            // 
            this.tsLblMapMousePos.AutoSize = false;
            this.tsLblMapMousePos.Margin = new System.Windows.Forms.Padding(5, 3, 3, 2);
            this.tsLblMapMousePos.Name = "tsLblMapMousePos";
            this.tsLblMapMousePos.Size = new System.Drawing.Size(250, 17);
            this.tsLblMapMousePos.Text = "[0,0]";
            this.tsLblMapMousePos.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.tsLblMapMousePos.Click += new System.EventHandler(this.tsLblMapMousePos_Click);
            // 
            // mainProgressBar
            // 
            this.mainProgressBar.Name = "mainProgressBar";
            this.mainProgressBar.Size = new System.Drawing.Size(150, 16);
            this.mainProgressBar.Visible = false;
            // 
            // tsLblSelectMessage
            // 
            this.tsLblSelectMessage.AutoSize = false;
            this.tsLblSelectMessage.Name = "tsLblSelectMessage";
            this.tsLblSelectMessage.Size = new System.Drawing.Size(350, 17);
            this.tsLblSelectMessage.Text = "Hold Shift to select. Hold Ctrl to toggle select";
            this.tsLblSelectMessage.Visible = false;
            // 
            // shapeFileListControl1
            // 
            this.shapeFileListControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.shapeFileListControl1.Location = new System.Drawing.Point(6, 68);
            this.shapeFileListControl1.Map = this.sfMap1;
            this.shapeFileListControl1.Name = "shapeFileListControl1";
            this.shapeFileListControl1.Size = new System.Drawing.Size(251, 237);
            this.shapeFileListControl1.TabIndex = 14;
            this.shapeFileListControl1.SelectedShapeFileChanged += new System.EventHandler<System.EventArgs>(this.shapeFileListControl1_SelectedShapeFileChanged);
            this.shapeFileListControl1.AddLayerClicked += new System.EventHandler(this.shapeFileListControl1_AddLayerClicked);
            // 
            // sfMap1
            // 
            this.sfMap1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.sfMap1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sfMap1.CentrePoint2D = ((EGIS.ShapeFileLib.PointD)(resources.GetObject("sfMap1.CentrePoint2D")));
            this.sfMap1.Location = new System.Drawing.Point(267, 68);
            this.sfMap1.MapBackColor = System.Drawing.SystemColors.Control;
            this.sfMap1.Name = "sfMap1";
            this.sfMap1.PanSelectMode = EGIS.Controls.PanSelectMode.Pan;
            this.sfMap1.RenderQuality = EGIS.ShapeFileLib.RenderQuality.Auto;
            this.sfMap1.Size = new System.Drawing.Size(674, 468);
            this.sfMap1.TabIndex = 7;
            this.sfMap1.UseMercatorProjection = true;
            this.sfMap1.ZoomLevel = 1D;
            this.sfMap1.ShapeFilesChanged += new System.EventHandler<System.EventArgs>(this.sfMap1_ShapeFilesChanged);
            this.sfMap1.ZoomLevelChanged += new System.EventHandler<System.EventArgs>(this.sfMap1_ZoomLevelChanged);
            this.sfMap1.TooltipDisplayed += new System.EventHandler<EGIS.Controls.SFMap.TooltipEventArgs>(this.sfMap1_TooltipDisplayed);
            this.sfMap1.SelectedRecordsChanged += new System.EventHandler<System.EventArgs>(this.sfMap1_SelectedRecordsChanged);
            this.sfMap1.ClientSizeChanged += new System.EventHandler(this.sfMap1_ClientSizeChanged);
            this.sfMap1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.sfMap1_KeyDown);
            this.sfMap1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.sfMap1_KeyUp);
            this.sfMap1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.sfMap1_MouseDown);
            this.sfMap1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.sfMap1_MouseMove);
            this.sfMap1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.sfMap1_MouseUp);
            // 
            // sfdMapImage
            // 
            this.sfdMapImage.Filter = "PNG|*.PNG";
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(949, 565);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.mainToolStrip);
            this.Controls.Add(this.shapeFileRenderPropertyGrid);
            this.Controls.Add(this.shapeFileListControl1);
            this.Controls.Add(this.sfMap1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Easy GIS .NET Desktop Edition";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem miNewProject;
        private System.Windows.Forms.OpenFileDialog ofdShapeFile;
        private EGIS.Controls.SFMap sfMap1;
        private EGIS.Controls.ShapeFileListControl shapeFileListControl1;
        private System.Windows.Forms.PropertyGrid shapeFileRenderPropertyGrid;
        private System.Windows.Forms.OpenFileDialog ofdProject;
        private System.Windows.Forms.ToolStripMenuItem miOpenProject;
        private System.Windows.Forms.ToolStripMenuItem miSaveProject;
        private System.Windows.Forms.SaveFileDialog sfdProject;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem miMapBackgroundColor;
        private System.Windows.Forms.ColorDialog mapColorDialog;
        private System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton newToolStripButton;
        private System.Windows.Forms.ToolStripButton saveToolStripButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
        private System.Windows.Forms.ToolStripButton helpToolStripButton;
        private System.Windows.Forms.ToolStripTextBox tsTxtFind;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton tsBtnZoomIn;
        private System.Windows.Forms.ToolStripButton tsBtnZoomOut;
        private System.Windows.Forms.ToolStripComboBox tscbSearchLayers;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.ToolStripButton openToolStripButton1;
        private System.Windows.Forms.ToolStripButton tsBtnPanLeft;
        private System.Windows.Forms.ToolStripButton tsBtnPanRight;
        private System.Windows.Forms.ToolStripButton tsBtnPanUp;
        private System.Windows.Forms.ToolStripButton tsBtnPanDown;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel tsLabelCurrentZoom;
        private System.Windows.Forms.ToolStripProgressBar mainProgressBar;
        private System.Windows.Forms.ToolStripButton tsBtnZoomFull;
        private System.Windows.Forms.ToolStripMenuItem miOpenShapeFile;
        private System.Windows.Forms.ToolStripStatusLabel tsLblMapMousePos;
        private System.Windows.Forms.ToolStripStatusLabel tsLabelVisibleArea;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem miMercatorProjection;
        private System.Windows.Forms.ToolStripMenuItem renderQualityToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem highToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem saveProjectAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentProjectsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem saveMapImageToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog sfdMapImage;
        private System.Windows.Forms.ToolStripMenuItem displayShapeAttributesWindowToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem useNativeFileMappingToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripStatusLabel tsLblSelectMessage;
        private System.Windows.Forms.ToolStripButton tsBtnSelectRect;
        private System.Windows.Forms.ToolStripButton tsBtnSelectCircle;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem disablePanSelectToolStripMenuItem;
    }
}

