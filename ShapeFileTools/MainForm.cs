using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using EGIS.ShapeFileLib;

namespace egis
{
    public partial class MainForm : Form
    {
        enum ProjectState { NewProject, UnsavedNewProject, OpenProject, UnsavedOpenProject };

        private ProjectState projectStatus = ProjectState.NewProject;
        private string currentProjectPath = null;

        private RecordAttributesForm recordAttributesForm;

        public MainForm()
        {
            InitializeComponent();

            tscbSearchLayers.SelectedIndex = 0;

            EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection = false;
            this.miMercatorProjection.Checked = EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection;

            LoadRecentProjects();

            EGIS.ShapeFileLib.UtmCoordinate utm =  EGIS.ShapeFileLib.ConversionFunctions.LLToUtm(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, -37.45678, 145.123456);
            Console.Out.WriteLine("{0},{1}", utm.Northing, utm.Easting);
            Console.Out.WriteLine("{0},{1}", (float)utm.Northing, (float)utm.Easting);

            EGIS.ShapeFileLib.LatLongCoordinate ll = EGIS.ShapeFileLib.ConversionFunctions.UtmToLL(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, utm);
            Console.Out.WriteLine("{0},{1}", ll.Latitude, ll.Longitude);
            Console.Out.WriteLine("{0},{1}", (float)ll.Latitude, (float)ll.Longitude);
            EGIS.ShapeFileLib.LatLongCoordinate ll2 = new LatLongCoordinate();
            ll2.Latitude = (float)ll.Latitude;
            ll2.Longitude = (float)ll.Longitude;
            double dist = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, ll, ll2);
            Console.Out.WriteLine("dist = " + dist);

            recordAttributesForm = new RecordAttributesForm();
            //recordAttributesForm.Show(this);
            recordAttributesForm.Owner = this;
            recordAttributesForm.VisibleChanged += new EventHandler(recordAttributesForm_VisibleChanged);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckLicence();
        }


        private void CheckLicence()
        {
            if (!Properties.Settings.Default.LicenceAccepted)
            {
                LicenceAgreementForm laf = new LicenceAgreementForm();
                try
                {
                    if (laf.ShowDialog() == DialogResult.Yes)
                    {
                        Properties.Settings.Default.LicenceAccepted = true;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        Close();
                    }
                }
                finally
                {
                    laf.Dispose();
                }
            }
        }

        private void miOpenShapeFile_Click(object sender, EventArgs e)
        {
            this.OpenShapeFile();
        }

        private void OpenShapeFile()
        {
            if (ofdShapeFile.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    //OpenShapeFile(ofdShapeFile.FileName);
                    foreach (string file in ofdShapeFile.FileNames)
                    {                        
                        OpenShapeFile(file);
                    }
                    int tzl = TileUtil.ScaleToZoomLevel(sfMap1.ZoomLevel);
                    if (tzl >= 0)
                    {
                        sfMap1.ZoomLevel = TileUtil.ZoomLevelToScale(tzl);
                    }
                    else
                    {
                        //not using lat long 
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    MessageBox.Show(this, "Error opening shapefile\n" + ex.Message, "Error opening shapefile",MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Log(string msg)
        {
            //this.rtbStatus.AppendText(DateTime.Now.ToString() + ": " + msg + "\n");
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + ": " + msg + "\n");
        }

        /// <summary>
        /// Opens the specified shapefile
        /// </summary>
        /// <param name="path">path to the shapefile to be opened</param>
        public void OpenShapeFile(string path)
        {
            string name = path.Substring(path.LastIndexOf("\\")+1);
            EGIS.ShapeFileLib.ShapeFile sf = sfMap1.AddShapeFile(path, name, "NAME");
            //RectangleF r = sfMap1.Extent;
            //sf.RenderSettings.MinRenderLabelZoom = 25.0f * 512f / r.Width;
            this.shapeFileRenderPropertyGrid.SelectedObject = sf.RenderSettings;
        }

        #region "Pan and Zoom methods"

        private void PanLeft()
        {
            RectangleF r = sfMap1.Extent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.X -= (sfMap1.ClientSize.Width >> 2) / sfMap1.ZoomLevel; ;// (0.0025f * r.Width);
            sfMap1.CentrePoint2D = pt;
        }
       
        private void PanRight()
        {
            RectangleF r = sfMap1.Extent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.X += (sfMap1.ClientSize.Width >> 2) / sfMap1.ZoomLevel;// (0.0025f * r.Width);
            sfMap1.CentrePoint2D = pt;
            
        }

        private void PanUp()
        {
            RectangleF r = sfMap1.Extent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.Y += (sfMap1.ClientSize.Height >> 2) / sfMap1.ZoomLevel; //(0.0025f * r.Height);
            sfMap1.CentrePoint2D = pt;
        }
        
        private void PanDown()
        {
            RectangleF r = sfMap1.Extent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.Y -= (sfMap1.ClientSize.Height >> 2) / sfMap1.ZoomLevel;
            sfMap1.CentrePoint2D = pt;


        }

        
        private void ZoomIn()
        {
            double z = sfMap1.ZoomLevel;
            sfMap1.ZoomLevel = z * 2d;            
        }

        private void ZoomOut()
        {
            double z = sfMap1.ZoomLevel;
            sfMap1.ZoomLevel = z / 2d;            
        }

        private void ZoomFull()
        {
            sfMap1.ZoomToFullExtent();

        }

        #endregion

        
        private void shapeFileListControl1_SelectedShapeFileChanged(object sender, EventArgs args)
        {
            shapeFileRenderPropertyGrid.SelectedObject = shapeFileListControl1.SelectedShapeFile.RenderSettings;
            LoadFindTextAutoCompleteData(shapeFileListControl1.SelectedShapeFile);
        }

        private void LoadFindTextAutoCompleteData(EGIS.ShapeFileLib.ShapeFile shapeFile)
        {
            LoadFindTextAutoCompleteData(shapeFile, true);
        }

        private void LoadFindTextAutoCompleteData(EGIS.ShapeFileLib.ShapeFile shapeFile, bool clearExisting)
        {
            if(clearExisting) this.tsTxtFind.AutoCompleteCustomSource.Clear();
            if (shapeFile == null || shapeFile.RenderSettings == null) return;
            if (shapeFile.RenderSettings.FieldIndex >= 0 && shapeFile.RenderSettings.IsSelectable)
            {
                string[] records = shapeFile.GetRecords(shapeFile.RenderSettings.FieldIndex);
                this.tsTxtFind.AutoCompleteCustomSource.AddRange(records);
            }
        }

        private void LoadFindTextAutoCompleteDataFromAllLayers()
        {
            this.tsTxtFind.AutoCompleteCustomSource.Clear();
            for (int n = this.sfMap1.ShapeFileCount-1; n >=0; n--)
            {
                LoadFindTextAutoCompleteData(sfMap1[n], false);
            }
        }


        private void shapeFileRenderPropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.sfMap1.Refresh(true);
            ProjectChanged();
        }


        #region "Project Open/Save methods"

        private void NewProject()
        {
            DialogResult dr = MessageBox.Show(this, "The current project will be closed.\nDo you wish to save your changes before creating a new project?", "Save Project?",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
            if(dr == DialogResult.Cancel) return;
            if(dr == DialogResult.Yes)
            {
                if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
                {
                    this.SaveProject(this.currentProjectPath);
                }
            }
            sfMap1.ClearShapeFiles();
            this.currentProjectPath = null;
            this.projectStatus = ProjectState.NewProject;
            this.Text = "Easy GIS .NET Desktop Edition";
            System.GC.Collect();

        }
        
        private void WriteProject(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;

            XmlWriter writer = XmlWriter.Create(path, settings);
            try
            {
                // Write the project element.
                writer.WriteStartElement("sfproject");
                writer.WriteAttributeString("version", "1.0");
                sfMap1.WriteXml(writer);
                // Write the close tag for the root element.
                writer.WriteEndElement();
            }
            finally
            {
                // Write the XML and close the writer.
                writer.Close();
            }
        }

        private void ReadProject(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            try
            {
                this.Cursor = Cursors.WaitCursor;
                XmlElement prjElement = (XmlElement)doc.GetElementsByTagName("sfproject").Item(0);
                string version = prjElement.GetAttribute("version");
                mainProgressBar.Visible = true;

                this.sfMap1.ReadXml(prjElement, new EGIS.Controls.ProgressLoadStatusHandler(this.ProjectLoadStatus));
            }
            finally
            {
                mainProgressBar.Visible = false;
                this.Cursor = Cursors.Default;
            }            
        }

        private void OpenProject()
        {
            if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
            {
                DialogResult dr = MessageBox.Show(this, "The current project will be closed.\nDo you wish to save your changes before opening a new project?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.Cancel) return;
                if (dr == DialogResult.Yes)
                {                    
                    this.SaveProject(this.currentProjectPath);                    
                }
            }

            if (this.ofdProject.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    ReadProject(ofdProject.FileName);
                    this.projectStatus = ProjectState.OpenProject;
                    this.currentProjectPath = ofdProject.FileName;
                    AddToRecentProjects(currentProjectPath);
                    this.Text = "Easy GIS .NET Desktop Edition - " + currentProjectPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error reading project :" + ex.ToString());
                    MessageBox.Show(this, "Error opening project\n" + ex.Message, "Error opening project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void OpenProject(string projectPath)
        {
            if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
            {
                DialogResult dr = MessageBox.Show(this, "The current project will be closed.\nDo you wish to save your changes before opening a new project?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.Cancel) return;
                if (dr == DialogResult.Yes)
                {
                    this.SaveProject(this.currentProjectPath);
                }
            }

            
            try
            {
                ReadProject(projectPath);
                this.projectStatus = ProjectState.OpenProject;
                this.currentProjectPath = projectPath;
                AddToRecentProjects(currentProjectPath);
                this.Text = "Easy GIS .NET Desktop Edition - " + currentProjectPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading project :" + ex.ToString());
                MessageBox.Show(this, "Error opening project\n" + ex.Message, "Error opening project", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }


        private void ProjectChanged()
        {
            if (this.projectStatus == ProjectState.OpenProject)
            {
                this.projectStatus = ProjectState.UnsavedOpenProject;
            }
            else if (projectStatus == ProjectState.NewProject)
            {
                this.projectStatus = ProjectState.UnsavedNewProject;
            }
        }

        private void SaveProject()
        {
            SaveProject(this.currentProjectPath);
        }

        private void SaveProject(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    WriteProject(path);
                    projectStatus = ProjectState.OpenProject;
                    currentProjectPath = path;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error writing project :" + ex.ToString());
                    MessageBox.Show(this, "Error saving project\n" + ex.Message, "Error saving project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (this.sfdProject.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        WriteProject(sfdProject.FileName);
                        projectStatus = ProjectState.OpenProject;
                        currentProjectPath = sfdProject.FileName;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error writing project :" + ex.ToString());
                        MessageBox.Show(this, "Error saving project\n" + ex.Message, "Error saving project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            
        }


        private void ExportProject()
        {
            
            if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
            {
                DialogResult dr = MessageBox.Show(this, "Do you wish to save your changes before exporting the project?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.Cancel) return;
                if (dr == DialogResult.Yes)
                {
                    this.SaveProject(this.currentProjectPath);
                }
            }
            if (currentProjectPath == null) return;

            if (this.sfdProject.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    string subDir = System.IO.Path.GetFileNameWithoutExtension(sfdProject.FileName) + "_files";
                    string absSubDir = System.IO.Path.GetDirectoryName(sfdProject.FileName) + "\\" + subDir;
                    if (!System.IO.Directory.Exists(absSubDir))
                    {
                        System.IO.Directory.CreateDirectory(absSubDir);
                    }
                    XmlDocument projDoc = new XmlDocument();
                    projDoc.Load(this.currentProjectPath);
                    XmlNodeList shapeNodes = projDoc.GetElementsByTagName("shapefile");
                    if (shapeNodes != null && shapeNodes.Count > 0)
                    {

                        for (int n = 0; n < shapeNodes.Count; n++)
                        {
                            XmlElement shapeElement = shapeNodes[n] as XmlElement;
                            string shapePath = shapeElement.GetElementsByTagName("path")[0].InnerText;
                            string shapeFilename = System.IO.Path.GetFileName(shapePath);
                            if (!System.IO.Path.IsPathRooted(shapePath))
                            {
                                shapePath = System.IO.Path.GetDirectoryName(this.currentProjectPath) + "/" + shapePath;
                            }
                            
                            //copy the .shpx and dbf files, then update the path in the shapeElement
                            if (!System.IO.File.Exists(absSubDir + "\\" + shapeFilename + ".shpx"))
                            {
                                System.IO.File.Copy(shapePath + ".shpx", absSubDir + "\\" + shapeFilename + ".shpx");
                            }
                            if (!System.IO.File.Exists(absSubDir + "\\" + shapeFilename + ".shp"))
                            {
                                System.IO.File.Copy(shapePath + ".shp", absSubDir + "\\" + shapeFilename + ".shp");
                            }
                            if (!System.IO.File.Exists(absSubDir + "\\" + shapeFilename + ".shx"))
                            {
                                System.IO.File.Copy(shapePath + ".shx", absSubDir + "\\" + shapeFilename + ".shx");
                            }
                            if (!System.IO.File.Exists(absSubDir + "\\" + shapeFilename + ".dbf"))
                            {
                                System.IO.File.Copy(shapePath + ".dbf", absSubDir + "\\" + shapeFilename + ".dbf", false);
                            }
                            shapeElement.GetElementsByTagName("path")[0].InnerText = subDir + "/" + shapeFilename;
                        }
                    }

                    XmlNodeList imageNodes = projDoc.GetElementsByTagName("PointImageSymbol");
                    if (imageNodes != null && imageNodes.Count > 0)
                    {

                        for (int n = 0; n < imageNodes.Count; n++)
                        {
                            XmlElement imgElement = imageNodes[n] as XmlElement;
                            string imagePath = imgElement.InnerText;
                            string imageFilename = System.IO.Path.GetFileName(imagePath);
                            if (!System.IO.Path.IsPathRooted(imagePath))
                            {
                                imagePath = System.IO.Path.GetDirectoryName(this.currentProjectPath) + "/" + imagePath;
                            }

                            //copy the .image file and then update the path in the image Element
                            if (!System.IO.File.Exists(absSubDir + "\\" + imageFilename))
                            {
                                System.IO.File.Copy(imagePath , absSubDir + "\\" + imageFilename);
                            }
                            
                            imgElement.InnerText = subDir + "/" + imageFilename;
                        }
                    }

                    projDoc.Save(sfdProject.FileName);
                    MessageBox.Show(this, "Project Exported Successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error exporting project :" + ex.ToString());
                    MessageBox.Show(this, "Error exporting project\n" + ex.Message, "Error exporting project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }


        private void ProjectLoadStatus(int totalLayers, int layersLoaded)
        {
            if(mainProgressBar.Maximum != totalLayers) this.mainProgressBar.Maximum = totalLayers;
            this.mainProgressBar.Value = layersLoaded;
            this.mainProgressBar.Visible = (totalLayers != layersLoaded);
        }

        private const int MaxRecentProjects = 5;

        private void LoadRecentProjects()
        {
            if (Properties.Settings.Default.RecentProjects == null)
            {
                Properties.Settings.Default.RecentProjects = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Save();
                return;
            }
            recentProjectsMenuItem.DropDownItems.Clear();
            foreach (string s in Properties.Settings.Default.RecentProjects)
            {
                ToolStripMenuItem i = new ToolStripMenuItem(s);
                i.Click+=new EventHandler(i_Click);
                recentProjectsMenuItem.DropDownItems.Add(i);
            }
        }

        void i_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item != null)
            {
                if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
                {
                    DialogResult dr = MessageBox.Show(this, "The current project will be closed.\nDo you wish to save your changes before opening a new project?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (dr == DialogResult.Cancel) return;
                    if (dr == DialogResult.Yes)
                    {
                        this.SaveProject(this.currentProjectPath);
                    }
                }                
                try
                {
                    ReadProject(item.Text);
                    this.projectStatus = ProjectState.OpenProject;
                    this.currentProjectPath = item.Text;
                    AddToRecentProjects(currentProjectPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error reading project :" + ex.ToString());
                    MessageBox.Show(this, "Error opening project " + item.Text + "\n" + ex.Message, "Error opening project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }            
            }
        }

        private void AddToRecentProjects(string path)
        {
            if(string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

            try
            {
                int foundIndex = -1;
                for (int n = Properties.Settings.Default.RecentProjects.Count - 1; foundIndex <= 0 && n >= 0; n--)
                {
                    if (string.Compare(Properties.Settings.Default.RecentProjects[n], path, true)==0)
                    {
                        foundIndex = n;
                    }
                }
                if (foundIndex >= 0)
                {
                    //adjust the project
                    Properties.Settings.Default.RecentProjects.RemoveAt(foundIndex);
                    Properties.Settings.Default.RecentProjects.Insert(0, path);
                }
                else
                {
                    Properties.Settings.Default.RecentProjects.Insert(0, path);
                    while (Properties.Settings.Default.RecentProjects.Count > MaxRecentProjects)
                    {
                        Properties.Settings.Default.RecentProjects.RemoveAt(Properties.Settings.Default.RecentProjects.Count - 1);
                    }
                }
            }
            finally
            {
                Properties.Settings.Default.Save();
                LoadRecentProjects();
            }


        }

        #endregion


        private void miOpenProject_Click(object sender, EventArgs e)
        {
            OpenProject();            
        }

        private void miSaveProject_Click(object sender, EventArgs e)
        {
            SaveProject();
        }


        private void UpdateVisibleAreaLabel()
        {
            RectangleF r = this.sfMap1.VisibleExtent;
            double w, h;
            if (IsMapFitForMercator())
            {
                //assume using latitude longitude
                w = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse,
                    r.Bottom, r.Left, r.Bottom, r.Right);
                h = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse,
                    r.Bottom, r.Left, r.Top, r.Left);
            }
            else
            {
                //assume coord in meters
                w = r.Width;
                h = r.Height;
            }
            tsLabelVisibleArea.Text = w.ToString("0000000.0m") + " x " + h.ToString("0000000.0m");
        }

        private void sfMap1_ZoomLevelChanged(object sender, EventArgs args)
        {
            this.tsLabelCurrentZoom.Text = sfMap1.ZoomLevel.ToString("0.00000");
            UpdateVisibleAreaLabel();
        }

        private void sfMap1_ClientSizeChanged(object sender, EventArgs e)
        {
            UpdateVisibleAreaLabel();

        }



        private void miMapBackgroundColor_Click(object sender, EventArgs e)
        {
            mapColorDialog.Color = sfMap1.BackColor;
            if (this.mapColorDialog.ShowDialog(this) == DialogResult.OK)
            {
                sfMap1.MapBackColor = mapColorDialog.Color;
                ProjectChanged();

            }
        }


        private void tscbSearchLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                if (tscbSearchLayers.SelectedIndex == 0)
                {
                    LoadFindTextAutoCompleteData(shapeFileListControl1.SelectedShapeFile);
                }
                else
                {
                    LoadFindTextAutoCompleteDataFromAllLayers();
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void tsTxtFind_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void tsTxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FindShape(tsTxtFind.Text);
            }
        }

        private void FindShape(string recordValue)
        {
            if (shapeFileListControl1.SelectedShapeFile == null) return;
            if(shapeFileListControl1.SelectedShapeFile.RenderSettings.FieldIndex < 0) return;
            int index = shapeFileListControl1.SelectedShapeFile.RenderSettings.DbfReader.IndexOf(recordValue,shapeFileListControl1.SelectedShapeFile.RenderSettings.FieldIndex, true);
            if (index >= 0)
            {
                RectangleF bounds = shapeFileListControl1.SelectedShapeFile.GetShapeBounds(index);
                if (bounds != RectangleF.Empty)
                {
                    shapeFileListControl1.SelectedShapeFile.SelectedRecordIndex = index;
                    sfMap1.CentrePoint2D = new PointD((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);                    
                }
            }
        }

        private bool IsMapFitForMercator()
        {
            RectangleF ext = sfMap1.Extent;
            return (ext.Top <= 90 && ext.Bottom >= -90);
        }


        private void tsBtnZoomIn_Click(object sender, EventArgs e)
        {
            ZoomIn();

        }

        private void tsBtnZoomOut_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void newToolStripButton_Click(object sender, EventArgs e)
        {
            NewProject();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            SaveProject();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            OpenShapeFile();
        }

        private void openToolStripButton1_Click(object sender, EventArgs e)
        {
            OpenProject();
        }

        private void helpToolStripButton_Click(object sender, EventArgs e)
        {
            DisplayAbout();            
        }

        private void DisplayAbout()
        {
            AboutForm f = new AboutForm();
            try
            {
                f.ShowDialog(this);
            }
            finally
            {
                f.Dispose();
            }
        }

        private void tsBtnPanLeft_Click(object sender, EventArgs e)
        {
            PanLeft();
        }

        private void tsBtnPanRight_Click(object sender, EventArgs e)
        {
            PanRight();            
        }

        private void tsBtnPanUp_Click(object sender, EventArgs e)
        {
            PanUp();
        }

        private void tsBtnPanDown_Click(object sender, EventArgs e)
        {
            PanDown();
        }

        private void tsBtnZoomFull_Click(object sender, EventArgs e)
        {
            ZoomFull();
        }

        private void shapeFileListControl1_AddLayerClicked(object sender, EventArgs e)
        {
            OpenShapeFile();
        }

        private void sfMap1_MouseMove(object sender, MouseEventArgs e)
        {
            PointD pt = sfMap1.PixelCoordToGisPoint(new Point(e.X, e.Y));

            string msg = string.Format("[{0},{1}]", new object[] { pt.X, pt.Y});
            tsLblMapMousePos.Text = msg;            
        }
        
        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            DisplayAbout();
        }        

        private void miMercatorProjection_Click(object sender, EventArgs e)
        {
            bool useProjection = !sfMap1.UseMercatorProjection;
            bool ok = true;
            if(useProjection)
            {
                if(!IsMapFitForMercator())
                {
                    ok = (DialogResult.Yes == MessageBox.Show(this, "Warning: The current project does not appear to be using Lat Long Coords.\nIf you use the Mercator Projection all parts of the map may not be visibe.\nAre you sure you wish to use the Mercator Projection?", "Confirm Mercator Projection", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning));
                }
            }
            if (ok)
            {
                sfMap1.UseMercatorProjection = useProjection;
                miMercatorProjection.Checked = useProjection;
            }
        }

        private void highToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lowToolStripMenuItem.Checked = false;
            highToolStripMenuItem.Checked = true;
            autoToolStripMenuItem.Checked = false;
            this.sfMap1.RenderQuality = EGIS.ShapeFileLib.RenderQuality.High;

        }

        private void lowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lowToolStripMenuItem.Checked = true;
            highToolStripMenuItem.Checked = false;
            autoToolStripMenuItem.Checked = false;
            this.sfMap1.RenderQuality = EGIS.ShapeFileLib.RenderQuality.Low;
        }

        private void autoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lowToolStripMenuItem.Checked = false;
            highToolStripMenuItem.Checked = false;
            autoToolStripMenuItem.Checked = true;
            this.sfMap1.RenderQuality = EGIS.ShapeFileLib.RenderQuality.Auto;

        }

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewProject();

        }

        private void saveProjectAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProject(null);
        }

        private void sfMap1_ShapeFilesChanged(object sender, EventArgs args)
        {
            ProjectChanged();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
            {
                DialogResult dr = MessageBox.Show(this, "The current project has changed.\nDo you wish to save your changes before closing?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.Cancel) e.Cancel=true;
                else if (dr == DialogResult.Yes)
                {
                    this.SaveProject(this.currentProjectPath);
                }
            }
            this.recordAttributesForm.AllowClose = !e.Cancel;
        }

        private void exportProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportProject();
        }

        //private void testToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    //tests renaming the current exe and then replacing it with a new version
        //    //this can be used to auto update an application
        //    try
        //    {
        //        System.IO.File.Delete("egis.exe2");
        //        System.IO.File.Move("egis.exe", "egis.exe2"); 
        //        System.IO.File.Move("./../Debug/egis.exe", "./egis.exe");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (string.Compare(System.IO.Path.GetExtension(files[0]), ".shp", StringComparison.OrdinalIgnoreCase)==0)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }

        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    this.mainProgressBar.Maximum = files.Length;
                    this.mainProgressBar.Value = 0;
                    this.mainProgressBar.Visible = true;
                    for (int n = 0; n < files.Length; n++)
                    {
                        if (string.Compare(System.IO.Path.GetExtension(files[n]), ".shp", StringComparison.OrdinalIgnoreCase) == 0)
                            this.OpenShapeFile(files[n]);
                        this.mainProgressBar.Increment(1);
                        Refresh();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    this.mainProgressBar.Visible = false;
                }
            }
        }

        private void saveMapImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sfdMapImage.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    SaveMapBitmap(sfdMapImage.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            
        }

        private void SaveMapBitmap(string path)
        {
            Bitmap bm = sfMap1.GetBitmap();
            try
            {
                bm.Save(path);
            }
            finally
            {
                bm.Dispose();
            }
        }

        private void sfMap1_TooltipDisplayed(object sender, EGIS.Controls.SFMap.TooltipEventArgs e)
        {
            if (e.ShapeFileIndex >= 0 && e.RecordIndex >= 0)
            {
                if(recordAttributesForm.Visible)
                {
                    string[] names = sfMap1[e.ShapeFileIndex].GetAttributeFieldNames();
                    string[] values = sfMap1[e.ShapeFileIndex].GetAttributeFieldValues(e.RecordIndex);
                    recordAttributesForm.SetRecordData(e.ShapeFileIndex, sfMap1[e.ShapeFileIndex].Name,e.RecordIndex,names, values); 
                }
                //PointD ptd = sfMap1.PixelCoordToGisPoint(e.MousePosition);
                //int recIndex = sfMap1[e.ShapeFileIndex].GetShapeIndexContainingPoint(new PointF((float)ptd.X, (float)ptd.Y), 0.00001F);
                //Console.Out.WriteLine("rec: " + recIndex);
            }
            else
            {
                recordAttributesForm.SetRecordData(-1, "", -1, null, null);
            }
        }

        private void displayShapeAttributesWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.displayShapeAttributesWindowToolStripMenuItem.Checked = !displayShapeAttributesWindowToolStripMenuItem.Checked;
            this.recordAttributesForm.Visible = displayShapeAttributesWindowToolStripMenuItem.Checked;
        }

        void recordAttributesForm_VisibleChanged(object sender, EventArgs e)
        {
            this.displayShapeAttributesWindowToolStripMenuItem.Checked = recordAttributesForm.Visible;            
        }


                                
        
    }
   
}