#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2018 Winston Fletcher.
** All rights reserved.
**
** This file is part of Easy GIS .NET Desktop Edition.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/

#endregion


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using EGIS.ShapeFileLib;
using System.Drawing.Printing;

namespace egis
{
    public partial class MainForm : Form
    {
        enum ProjectState { NewProject, UnsavedNewProject, OpenProject, UnsavedOpenProject };

        private ProjectState projectStatus = ProjectState.NewProject;
        private string currentProjectPath = null;

        private RecordAttributesForm recordAttributesForm;
        private ViewAttributesForm viewAttributesForm;

        public MainForm()
        {
            InitializeComponent();

            tscbSearchLayers.SelectedIndex = 0;

            //EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection = false;
            //this.miMercatorProjection.Checked = EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection;
            this.sfMap1.UseMercatorProjection = false;
            this.miMercatorProjection.Checked = false;
            this.useNativeFileMappingToolStripMenuItem.Checked = EGIS.ShapeFileLib.ShapeFile.MapFilesInMemory;
            LoadRecentProjects();

            //double lat1 = -37, lon1 = 116.25;
            //double lat2 = -30, lon2 = 117.25;
            //double dist = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, lat1, lon1, lat2, lon2);
            //Console.Out.WriteLine("dist bw points:" + dist);
            //dist = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPoints(23, lat1, lon1, lat2, lon2);
            //Console.Out.WriteLine("dist bw points:" + dist);       

           // TestLineClipping();
            //TestPolygonClipping();
          
            sfMap1.MapDoubleClick += new EventHandler<EGIS.Controls.SFMap.MapDoubleClickedEventArgs>(sfMap1_MapDoubleClick);
        }

        void sfMap1_MapDoubleClick(object sender, EGIS.Controls.SFMap.MapDoubleClickedEventArgs e)
        {
           // e.Cancel = true;
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
                    /*
                    int tzl = TileUtil.ScaleToZoomLevel(sfMap1.ZoomLevel);
                    if (tzl >= 0)
                    {
                        sfMap1.ZoomLevel = TileUtil.ZoomLevelToScale(tzl);
                    }
                    else
                    {
                        //not using lat long 
                    }
                     **/
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    MessageBox.Show(this, "Error opening shapefile\n" + ex.Message, "Error opening shapefile", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string name = path.Substring(path.LastIndexOf("\\") + 1);
            EGIS.ShapeFileLib.ShapeFile sf = sfMap1.AddShapeFile(path, name, "NAME");
            this.shapeFileRenderPropertyGrid.SelectedObject = sf.RenderSettings;
        }

        private void SelectAndCenterRecord(int index, ShapeFile sf)
        {
            RectangleF bounds = sf.GetShapeBounds(index);
            if (bounds != RectangleF.Empty)
            {
                if (ClientSize.Width > 0 && ClientSize.Height > 0)
                {
                    sfMap1.CentrePoint2D = new PointD((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
                    double r1 = sfMap1.ClientSize.Width * bounds.Height;
                    double r2 = sfMap1.ClientSize.Height * bounds.Width;
                    if (r1 < r2)
                    {
                        sfMap1.ZoomLevel = 0.5 * (sfMap1.ClientSize.Width / bounds.Width);
                    }
                    else
                    {
                        sfMap1.ZoomLevel = 0.5 * (sfMap1.ClientSize.Height / bounds.Height);
                    }
                }
                sf.SelectRecord(index, true);
            }

        }

        #region "Pan and Zoom methods"

        private void PanLeft()
        {
            RectangleD r = sfMap1.ProjectedExtent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.X -= (sfMap1.ClientSize.Width >> 2) / sfMap1.ZoomLevel; ;// (0.0025f * r.Width);
            sfMap1.CentrePoint2D = pt;
        }

        private void PanRight()
        {
            RectangleD r = sfMap1.ProjectedExtent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.X += (sfMap1.ClientSize.Width >> 2) / sfMap1.ZoomLevel;// (0.0025f * r.Width);
            sfMap1.CentrePoint2D = pt;

        }

        private void PanUp()
        {
            RectangleD r = sfMap1.ProjectedExtent;
            PointD pt = sfMap1.CentrePoint2D;
            pt.Y += (sfMap1.ClientSize.Height >> 2) / sfMap1.ZoomLevel; //(0.0025f * r.Height);
            sfMap1.CentrePoint2D = pt;
        }

        private void PanDown()
        {
            RectangleD r = sfMap1.ProjectedExtent;
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
            if (clearExisting) this.tsTxtFind.AutoCompleteCustomSource.Clear();
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
            for (int n = this.sfMap1.ShapeFileCount - 1; n >= 0; n--)
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
            DialogResult dr = MessageBox.Show(this, "The current project will be closed.\nDo you wish to save your changes before creating a new project?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (dr == DialogResult.Cancel) return;
            if (dr == DialogResult.Yes)
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
                string baseDirectory = System.IO.Path.GetDirectoryName(path);
                this.sfMap1.ReadXml(prjElement, baseDirectory, new EGIS.Controls.ProgressLoadStatusHandler(this.ProjectLoadStatus));
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
            sfdProject.FilterIndex = 1;
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
                            //if (!System.IO.File.Exists(absSubDir + "\\" + shapeFilename + ".shpx"))
                            //{
                            //    System.IO.File.Copy(shapePath + ".shpx", absSubDir + "\\" + shapeFilename + ".shpx");
                            //}
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
                                System.IO.File.Copy(imagePath, absSubDir + "\\" + imageFilename);
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

        private void ExportAndZipProject()
        {

          
            sfdProject.FilterIndex = 2;
            if (this.sfdProject.ShowDialog(this) == DialogResult.OK)
            {
                string tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
                try
                {                                                            
                    if (!System.IO.Directory.Exists(tempDirectory))
                    {
                        System.IO.Directory.CreateDirectory(tempDirectory);
                    }
                    string egpFileName = System.IO.Path.ChangeExtension(System.IO.Path.GetFileNameWithoutExtension(sfdProject.FileName), ".egp");
                    egpFileName = System.IO.Path.Combine(tempDirectory, egpFileName);

                    this.SaveProject(egpFileName);

                    Cursor = Cursors.WaitCursor;
                    string subDir = System.IO.Path.GetFileNameWithoutExtension(egpFileName) + "_files";
                    string absSubDir = System.IO.Path.Combine(tempDirectory, subDir);
                    if (!System.IO.Directory.Exists(absSubDir))
                    {
                        System.IO.Directory.CreateDirectory(absSubDir);
                    }
                    XmlDocument projDoc = new XmlDocument();
                    projDoc.Load(egpFileName);
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
                                shapePath = System.IO.Path.GetDirectoryName(egpFileName) + "/" + shapePath;
                            }

                            //copy the .shpx and dbf files, then update the path in the shapeElement
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
                                imagePath = System.IO.Path.GetDirectoryName(egpFileName) + "/" + imagePath;
                            }

                            //copy the .image file and then update the path in the image Element
                            if (!System.IO.File.Exists(absSubDir + "\\" + imageFilename))
                            {
                                System.IO.File.Copy(imagePath, absSubDir + "\\" + imageFilename);
                            }

                            imgElement.InnerText = subDir + "/" + imageFilename;
                        }
                    }

                    projDoc.Save(egpFileName);

                    //now zip the files
                    string zipFileName = System.IO.Path.ChangeExtension(sfdProject.FileName, ".zip");

                    ZipProjectFiles(egpFileName, zipFileName);


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

                    //delete the temporary directory
                    try
                    {
                        if (System.IO.Directory.Exists(tempDirectory))
                        {
                            System.IO.Directory.Delete(tempDirectory, true);
                        }
                    }
                    catch
                    {
                        //should not happen but ignore if it does
                    }

                }
            }
        }

        private void ZipProjectFiles(string egpPath, string zipPath)
        {
            //check if the xipPath exists and delete first
            if (System.IO.File.Exists(zipPath))
            {
                System.IO.File.Delete(zipPath);
            }
            System.IO.Compression.ZipFile.CreateFromDirectory(System.IO.Path.GetDirectoryName(egpPath),
                zipPath,System.IO.Compression.CompressionLevel.Optimal,false);


        }

        private void ProjectLoadStatus(int totalLayers, int layersLoaded)
        {
            if (mainProgressBar.Maximum != totalLayers) this.mainProgressBar.Maximum = totalLayers;
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
                i.Click += new EventHandler(i_Click);
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
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

            try
            {
                int foundIndex = -1;
                for (int n = Properties.Settings.Default.RecentProjects.Count - 1; foundIndex <= 0 && n >= 0; n--)
                {
                    if (string.Compare(Properties.Settings.Default.RecentProjects[n], path, true) == 0)
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
                w = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse,
                    r.Bottom, r.Left, r.Bottom, r.Right);
                h = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse,
                    r.Bottom, r.Left, r.Top, r.Left);
            }
            else
            {
                //assume coord in meters
                w = r.Width;
                h = r.Height;
            }
            tsLabelVisibleArea.Text = w.ToString("0000000.000m") + " x " + h.ToString("0000000.000m");
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
            if (shapeFileListControl1.SelectedShapeFile.RenderSettings.FieldIndex < 0) return;
            int index = shapeFileListControl1.SelectedShapeFile.RenderSettings.DbfReader.IndexOf(recordValue, shapeFileListControl1.SelectedShapeFile.RenderSettings.FieldIndex, true);
            if (index >= 0)
            {
                RectangleF bounds = shapeFileListControl1.SelectedShapeFile.GetShapeBounds(index);
                if (bounds != RectangleF.Empty)
                {
                    //shapeFileListControl1.SelectedShapeFile.SelectedRecordIndex = index;
                    shapeFileListControl1.SelectedShapeFile.ClearSelectedRecords();
                    shapeFileListControl1.SelectedShapeFile.SelectRecord(index, true);
                    sfMap1.CentrePoint2D = new PointD((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
                }
            }
        }

        private bool IsMapFitForMercator()
        {
            RectangleD ext = sfMap1.Extent;
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
            tsBtnSelectCircle.Checked = false;
            tsBtnSelectRect.Checked = false;
            tsLblSelectMessage.Visible = false;
            sfMap1.PanSelectMode = EGIS.Controls.PanSelectMode.Pan;
        }

        private void tsBtnPanRight_Click(object sender, EventArgs e)
        {
            PanRight();
            tsBtnSelectCircle.Checked = false;
            tsBtnSelectRect.Checked = false;
            tsLblSelectMessage.Visible = false;
            sfMap1.PanSelectMode = EGIS.Controls.PanSelectMode.Pan;
        }

        private void tsBtnPanUp_Click(object sender, EventArgs e)
        {
            PanUp();
            tsBtnSelectCircle.Checked = false;
            tsBtnSelectRect.Checked = false;
            tsLblSelectMessage.Visible = false;
            sfMap1.PanSelectMode = EGIS.Controls.PanSelectMode.Pan;
        }

        private void tsBtnPanDown_Click(object sender, EventArgs e)
        {
            PanDown();
            tsBtnSelectCircle.Checked = false;
            tsBtnSelectRect.Checked = false;
            tsLblSelectMessage.Visible = false;
            sfMap1.PanSelectMode = EGIS.Controls.PanSelectMode.Pan;
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
            string msg = string.Format("[{0:0.00000000},{1:0.00000000}]", new object[] { pt.X, pt.Y });
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
            if (useProjection)
            {
                if (!IsMapFitForMercator())
                {
                    ok = (DialogResult.Yes == MessageBox.Show(this, "Warning: The current project does not appear to be using Lat Long Coords.\nIf you use the Mercator Projection all parts of the map may not be visibe.\nAre you sure you wish to use the Mercator Projection?", "Confirm Mercator Projection", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning));
                }
            }
            if (ok)
            {
                sfMap1.UseMercatorProjection = useProjection;
                miMercatorProjection.Checked = useProjection;
                UpdateVisibleAreaLabel();
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
            this.viewAttributesToolStripMenuItem.Enabled = (sfMap1.ShapeFileCount > 0);
            this.displayShapeAttributesWindowToolStripMenuItem.Enabled = (sfMap1.ShapeFileCount > 0);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (projectStatus == ProjectState.UnsavedNewProject || projectStatus == ProjectState.UnsavedOpenProject)
            {
                DialogResult dr = MessageBox.Show(this, "The current project has changed.\nDo you wish to save your changes before closing?", "Save Project?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dr == DialogResult.Cancel) e.Cancel = true;
                else if (dr == DialogResult.Yes)
                {
                    this.SaveProject(this.currentProjectPath);
                }
            }
            
            base.OnFormClosing(e);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void exportProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportProject();            
        }

        private void exportProjectforUseInWebEditionAsSingleZIPFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportAndZipProject();
        }
        

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (string.Compare(System.IO.Path.GetExtension(files[0]), ".shp", StringComparison.OrdinalIgnoreCase) == 0)
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
            if (recordAttributesForm == null || recordAttributesForm.IsDisposed) return;
            if (e.ShapeFileIndex >= 0 && e.RecordIndex >= 0)
            {                
                string[] names = sfMap1[e.ShapeFileIndex].GetAttributeFieldNames();
                string[] values = sfMap1[e.ShapeFileIndex].GetAttributeFieldValues(e.RecordIndex);
                recordAttributesForm.SetRecordData(e.ShapeFileIndex, sfMap1[e.ShapeFileIndex].Name, e.RecordIndex, names, values);                
            }
            else
            {
                recordAttributesForm.SetRecordData(-1, "", -1, null, null);
            }
        }

        

        private void useNativeFileMappingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.useNativeFileMappingToolStripMenuItem.Checked = !this.useNativeFileMappingToolStripMenuItem.Checked;
            EGIS.ShapeFileLib.ShapeFile.MapFilesInMemory = useNativeFileMappingToolStripMenuItem.Checked;
        }

        private void sfMap1_KeyDown(object sender, KeyEventArgs e)
        {
            this.tsLblSelectMessage.Visible = true;
        }

        private void sfMap1_KeyUp(object sender, KeyEventArgs e)
        {
            this.tsLblSelectMessage.Visible = (tsBtnSelectRect.Checked || tsBtnSelectCircle.Checked);
        }

        private void tsBtnSelectRect_Click(object sender, EventArgs e)
        {
            tsBtnSelectRect.Checked = !tsBtnSelectRect.Checked;
            tsBtnSelectCircle.Checked = false;
            tsLblSelectMessage.Visible = tsBtnSelectRect.Checked;
            sfMap1.PanSelectMode = tsBtnSelectRect.Checked ? EGIS.Controls.PanSelectMode.SelectRectangle : EGIS.Controls.PanSelectMode.Pan;
        }

        private void tsBtnSelectCircle_Click(object sender, EventArgs e)
        {
            tsBtnSelectCircle.Checked = !tsBtnSelectCircle.Checked;
            tsBtnSelectRect.Checked = false;
            tsLblSelectMessage.Visible = tsBtnSelectCircle.Checked;
            sfMap1.PanSelectMode = tsBtnSelectCircle.Checked ? EGIS.Controls.PanSelectMode.SelectCircle : EGIS.Controls.PanSelectMode.Pan;
        }

        private void sfMap1_MouseDown(object sender, MouseEventArgs e)
        {
            this.tsLblSelectMessage.Visible = true;

        }

        private void sfMap1_MouseUp(object sender, MouseEventArgs e)
        {
            this.tsLblSelectMessage.Visible = false;

            //TestLinearReferencing(e.X, e.Y);

        }

        private void sfMap1_SelectedRecordsChanged(object sender, EventArgs e)
        {
            //Console.Out.WriteLine("records changed");

        }

        private void tsLblMapMousePos_Click(object sender, EventArgs e)
        {

        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
            PrintDialog ppd = new PrintDialog();
            try
            {
                pd.DefaultPageSettings.Landscape = true;
                ppd.Document = pd;
                if (ppd.ShowDialog(this) == DialogResult.OK)
                {
                    pd.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Printing Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
            finally
            {
                pd.Dispose();
                ppd.Dispose();
            }
        }


        void pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            //save the current Render Quality settings
            RenderQuality currentRenderQuality = ShapeFile.RenderQuality;
            try
            {
                //printing does not render correctly if quality is not high
                ShapeFile.RenderQuality = RenderQuality.High;
                Graphics g = e.Graphics;
                g.SetClip(e.MarginBounds);
                g.TranslateTransform(e.MarginBounds.Left, e.MarginBounds.Top);
                g.Clear(sfMap1.MapBackColor);
                //calculate scale to ensure the map fills the printer page
                double scale = sfMap1.ZoomLevel * e.MarginBounds.Width / sfMap1.ClientSize.Width;
                PointD centre = sfMap1.CentrePoint2D;
                //CentrePoint2D always returns coorinates in lat/lon check if mercator projection is used and transform if neccessary
                if (sfMap1.UseMercatorProjection) centre = ShapeFile.LatLongToProjection(centre);
                for (int n = 0; n < sfMap1.ShapeFileCount; ++n)
                {
                    sfMap1[n].Render(g, e.MarginBounds.Size, centre, scale, sfMap1.UseMercatorProjection ? ProjectionType.Mercator : ProjectionType.None);
                }
                g.ResetTransform();
                //g.DrawRectangle(Pens.Red, e.MarginBounds);
                e.HasMorePages = false;
            }
            finally
            {
                ShapeFile.RenderQuality = currentRenderQuality;
            }
        }
        

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private void disablePanSelectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sfMap1.PanSelectMode = disablePanSelectToolStripMenuItem.Checked ? EGIS.Controls.PanSelectMode.None : EGIS.Controls.PanSelectMode.Pan;
        }


        private void TestLinearReferencing(int mouseX, int mouseY)
        {
            PointD coords = sfMap1.PixelCoordToGisPoint(mouseX, mouseY);
            PointD coords2 = sfMap1.PixelCoordToGisPoint(mouseX + 8, mouseY + 8);
            double dist = Math.Sqrt((coords.X - coords2.X)*(coords.X - coords2.X) + (coords.Y - coords2.Y)*(coords.Y - coords2.Y));
            //dist = 50;// 50m
            for (int i = sfMap1.ShapeFileCount - 1; i >= 0; --i)
            {
                ShapeFile sf = sfMap1[i];                
                PolylineDistanceInfo polylineDistanceInfo;
                DateTime tick = DateTime.Now;
                int index;
                //for (int n = 0; n < 99; ++n)
                //{
                //    index = sf.GetClosestShape(coords, dist, out polylineDistanceInfo);                    
                //}
                index = sf.GetClosestShape(coords, dist, out polylineDistanceInfo);
                //Console.Out.WriteLine("ClosestPointOnPolyline time: " + DateTime.Now.Subtract(tick).Milliseconds / 100.0 + "ms");
                if (index >= 0)
                {     
                    Console.Out.WriteLine("tVal: " + polylineDistanceInfo.TVal);
                    Console.Out.WriteLine("coords:" + coords);
                    Console.Out.WriteLine("polylinePoint:" + polylineDistanceInfo.PolylinePoint);
                    using (Graphics g = sfMap1.CreateGraphics())
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        Point pt = sfMap1.GisPointToPixelCoord(polylineDistanceInfo.PolylinePoint);
                        g.FillEllipse(Brushes.Red, pt.X - 5, pt.Y - 5, 10, 10);

                        Point mousePoint = new Point(mouseX, mouseY);
                        if ((mousePoint.X - pt.X) * (mousePoint.X - pt.X) + (mousePoint.Y - pt.Y) * (mousePoint.Y - pt.Y) > 50 * 50)
                        {
                            g.FillEllipse(Brushes.Red, mousePoint.X - 2, mousePoint.Y - 2, 4,4);
                            g.DrawLine(Pens.Red, mousePoint, pt);
                        }
                    }

                    if (sf.ShapeType == ShapeType.PolyLineM)
                    {
                        double[] measures = sf.GetShapeMDataD(index)[0];
                        double distance = measures[polylineDistanceInfo.PointIndex] + polylineDistanceInfo.TVal *(measures[polylineDistanceInfo.PointIndex+1] - measures[polylineDistanceInfo.PointIndex]);
                        Console.Out.WriteLine("distance from start of shape: " + distance);
                    }

                }                
            }            
        }


        private void displayShapeAttributesWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.displayShapeAttributesWindowToolStripMenuItem.Checked = !displayShapeAttributesWindowToolStripMenuItem.Checked;

            if (this.displayShapeAttributesWindowToolStripMenuItem.Checked)
            {
                if (recordAttributesForm != null && !recordAttributesForm.IsDisposed) return; //already viewing                                
                if (recordAttributesForm == null || recordAttributesForm.IsDisposed)
                {
                    recordAttributesForm = new RecordAttributesForm();
                    recordAttributesForm.Owner = this;
                    recordAttributesForm.FormClosing += recordAttributesForm_FormClosing;
                }
                recordAttributesForm.Show(this);
            }
            else
            {
                if (recordAttributesForm != null && !recordAttributesForm.IsDisposed)
                {
                    recordAttributesForm.Close();
                }
            }
            
        }

        void recordAttributesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.displayShapeAttributesWindowToolStripMenuItem.Checked = false;
            recordAttributesForm.FormClosing -= recordAttributesForm_FormClosing;
            recordAttributesForm = null;
        }        

        private void viewAttributesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewAttributesToolStripMenuItem.Checked = !viewAttributesToolStripMenuItem.Checked;
            if (viewAttributesToolStripMenuItem.Checked)
            {
                if (sfMap1.ShapeFileCount > 0)
                {
                    if (viewAttributesForm != null && !viewAttributesForm.IsDisposed) return; //already viewing
                    if (viewAttributesForm == null || viewAttributesForm.IsDisposed)
                    {
                        viewAttributesForm = new ViewAttributesForm(this.sfMap1);
                        viewAttributesForm.Owner = this;
                        viewAttributesForm.FormClosing += viewAttributesForm_FormClosing;
                    }
                    //viewAttributesForm.LoadAttributes(sfMap1[0]);
                    viewAttributesForm.Show(this);
                }
            }
            else
            {
                if (viewAttributesForm != null && !viewAttributesForm.IsDisposed)
                {
                    viewAttributesForm.Close();
                }
            }
        }

        void viewAttributesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            viewAttributesForm.FormClosing -= viewAttributesForm_FormClosing;
            viewAttributesForm = null;
            viewAttributesToolStripMenuItem.Checked = false;
        }

        private void miZoomToExtentWhenCtrlkeyDown_Click(object sender, EventArgs e)
        {
            miZoomToExtentWhenCtrlkeyDown.Checked = !miZoomToExtentWhenCtrlkeyDown.Checked;
            sfMap1.ZoomToSelectedExtentWhenCtrlKeydown = miZoomToExtentWhenCtrlkeyDown.Checked;
        }

        private void addXYDatacsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CsvToShapeFileForm form = new CsvToShapeFileForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(form.ConvertedShapeFilePath))
                    {
                        try
                        {
                            OpenShapeFile(form.ConvertedShapeFilePath);
                        }
                        catch (Exception ex)
                        {
                            Log(ex.ToString());
                            MessageBox.Show(this, "Error opening shapefile\n" + ex.Message, "Error opening shapefile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }


        private void TestLineClipping()
        {
            EGIS.ShapeFileLib.GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = 100,
                XMax = 200,
                YMin = 100,
                YMax = 200
            };

            PointD[] points = new PointD[10];
            points[0] = new PointD(0, 0);
            points[1] = new PointD(110, 110);
            points[2] = new PointD(150, 110);
            points[3] = new PointD(150, 150);
            points[4] = new PointD(250, 150);
            points[5] = new PointD(250, 190);
            points[6] = new PointD(250, 250);
            points[7] = new PointD(190, 190);
            points[8] = new PointD(150, 190);
            points[9] = new PointD(150, 210);

            List<double> clippedPoints = new List<double>();
            List<int> parts = new List<int>();

            EGIS.ShapeFileLib.GeometryAlgorithms.PolyLineClip(points, 10, clipBounds, clippedPoints, parts);

            points = new PointD[10];
            points[0] = new PointD(20, 120);
            points[1] = new PointD(220, 120);
            points[2] = new PointD(220, 140);
            points[3] = new PointD(20, 140);
            points[4] = new PointD(20, 150);
            points[5] = new PointD(250, 150);
            points[6] = new PointD(250, 180);
            points[7] = new PointD(120, 180);
            points[8] = new PointD(120, 190);
            points[9] = new PointD(190, 190);

            List<double> clippedPoints2 = new List<double>();
            List<int> parts2 = new List<int>();

            EGIS.ShapeFileLib.GeometryAlgorithms.PolyLineClip(points, 10, clipBounds, clippedPoints2, parts2);


            using (Bitmap bm = new Bitmap(120, 120)) 
            {
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.Clear(Color.White);
                    g.DrawRectangle(Pens.Red, 10, 10, 100, 100);
                    for (int n = 0; n < parts.Count; ++n)
                    {
                        int index1 = parts[n];
                        int index2 = n < parts.Count - 1 ? parts[n + 1] : clippedPoints.Count;
                        Console.Out.WriteLine("part " + n);
                        PointF[] pts = new PointF[(index2 - index1) >> 1];
                        int index = 0;
                        for (int i = index1; i < index2; i += 2)
                        {
                            Console.Out.WriteLine(string.Format("[{0:0.00000}, {1:0.00000}]", clippedPoints[i], clippedPoints[i + 1]));
                            pts[index++] = new PointF(-90+(float)clippedPoints[i], -90+(float)clippedPoints[i + 1]);
                        }
                        using (Pen p = new Pen(Color.Red, 3))
                        {
                            g.DrawLines(p, pts);
                        }
                    }

                    for (int n = 0; n < parts2.Count; ++n)
                    {
                        int index1 = parts2[n];
                        int index2 = n < parts2.Count - 1 ? parts2[n + 1] : clippedPoints2.Count;
                        Console.Out.WriteLine("part " + n);
                        PointF[] pts = new PointF[(index2 - index1) >> 1];
                        int index = 0;
                        for (int i = index1; i < index2; i += 2)
                        {
                            Console.Out.WriteLine(string.Format("[{0:0.00000}, {1:0.00000}]", clippedPoints2[i], clippedPoints2[i + 1]));
                            pts[index++] = new PointF(-90 + (float)clippedPoints2[i], -90 + (float)clippedPoints2[i + 1]);
                        }
                        using (Pen p = new Pen(Color.Blue, 2))
                        {
                            g.DrawLines(p, pts);
                        }
                    }
                }
                bm.Save(@"c:\temp\clippedBitmap.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            }
            

        }

        private void TestPolygonClipping()
        {
            PointD[] polygon = new PointD[6];
            polygon[0] = new PointD(5, 5);
            polygon[1] = new PointD(4, 4);
            polygon[2] = new PointD(-2, 3);
            polygon[3] = new PointD(-4, 10);
            polygon[4] = new PointD(6, 10);
            polygon[5] = polygon[0];
            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = 0,
                XMax = 20,
                YMin = 0,
                YMax = 8
            };

            List<PointD> clippedPolygon = new List<PointD>();

            GeometryAlgorithms.PolygonClip(polygon, 6, clipBounds, clippedPolygon);

            for (int n = 0; n < polygon.Length; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(polygon[n]);
            }
            Console.WriteLine();
            for (int n = 0; n < clippedPolygon.Count; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(clippedPolygon[n]);
            }
            Console.WriteLine();

            GeometryAlgorithms.PolygonClip(polygon, 5, clipBounds, clippedPolygon);

            for (int n = 0; n < clippedPolygon.Count; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(clippedPolygon[n]);
            }
            Console.WriteLine();



            polygon = new PointD[7];
            polygon[0] = new PointD(10, 0);
            polygon[1] = new PointD(0, 0);
            polygon[2] = new PointD(0, 10);
            polygon[3] = new PointD(9, 10);
            polygon[4] = new PointD(4, 6);
            polygon[5] = new PointD(10, 8);            
            polygon[6] = polygon[0];

            PointD[] hole = new PointD[5];
            hole[0] = new PointD(6, 1);            
            hole[1] = new PointD(8, 1);
            hole[2] = new PointD(8, 4.5);            
            hole[3] = new PointD(6, 4.5);
            hole[4] = hole[0];

            List<PointD> clippedHole = new List<PointD>();

            clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = 5,
                XMax = 9,
                YMin = 4,
                YMax = 9
            };

            clippedPolygon = new List<PointD>();

            GeometryAlgorithms.PolygonClip(polygon, 7, clipBounds, clippedPolygon);

            GeometryAlgorithms.PolygonClip(hole, 5, clipBounds, clippedHole);
            
            //test reversing the hole
            //clippedHole.Reverse();

            for (int n = 0; n < polygon.Length; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(polygon[n]);
            }
            Console.WriteLine();
            for (int n = 0; n < clippedPolygon.Count; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(clippedPolygon[n]);
            }
            Console.WriteLine();
            for (int n = 0; n < clippedPolygon.Count - 1; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(clippedPolygon[n]);
            }
            Console.WriteLine();


            const int Scale = 50;
            using (Bitmap bm = new Bitmap(505, 505))
            {
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.Clear(Color.White);

                    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

                    PointF[] pts = new PointF[polygon.Length];
                    for (int n = 0; n < pts.Length; ++n)
                    {
                        pts[n] = new PointF((float)polygon[n].X * Scale, (float)polygon[n].Y * Scale);
                    }                    
                    PointF[] holePts = new PointF[hole.Length];
                    for (int n = 0; n < holePts.Length; ++n)
                    {
                        holePts[n] = new PointF((float)hole[n].X * Scale, (float)hole[n].Y * Scale);
                    }
                    //because we're drawing upside down we need to reverse the points to correct the winding order
                    Array.Reverse(pts);
                    Array.Reverse(holePts);


                    path.AddPolygon(pts);
                    path.AddPolygon(holePts);

                    g.FillPath(Brushes.Blue, path);

                    
                    

                    pts = new PointF[clippedPolygon.Count];
                    for (int n = 0; n < pts.Length; ++n)
                    {
                        pts[n] = new PointF((float)clippedPolygon[n].X * Scale, (float)clippedPolygon[n].Y * Scale);
                    }
                    holePts = new PointF[clippedHole.Count];
                    for (int n = 0; n < holePts.Length; ++n)
                    {
                        holePts[n] = new PointF((float)clippedHole[n].X * Scale, (float)clippedHole[n].Y * Scale);
                    }
                    path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

                    //because we're drawing upside down we need to reverse the points to correct the winding order
                    Array.Reverse(pts);
                    Array.Reverse(holePts);

                    path.AddPolygon(pts);
                    path.AddPolygon(holePts);

                    g.FillPath(Brushes.Yellow, path);

                    //using (Pen p = new Pen(Color.Green, 2))
                    //{
                    //    g.DrawPath(p, path);
                    //}

                    //draw the clipping rectangle
                    using (Pen p = new Pen(Color.Red, 2))
                    {
                        g.DrawRectangle(p, new Rectangle((int)clipBounds.XMin * Scale, (int)clipBounds.YMin * Scale,
                            (int)(clipBounds.XMax - clipBounds.XMin) * Scale,
                            (int)(clipBounds.YMax - clipBounds.YMin) * Scale));
                    }

                    for (int n = 0; n < pts.Length; ++n)
                    {
                        g.DrawString(n.ToString(), this.Font, Brushes.Black, pts[n]);
                    }
                    
                }
                bm.Save(@"c:\temp\clippedpolygon.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            }



        }
    }
  
}