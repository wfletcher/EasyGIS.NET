#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.Controls class library of Easy GIS .NET.
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
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using EGIS.ShapeFileLib;

[assembly: CLSCompliant(true)]
namespace EGIS.Controls
{
    public delegate void ProgressLoadStatusHandler(int totalLayers, int numberLayersLoaded);

    public enum PanSelectMode { Pan, SelectRectangle, SelectCircle };

    
    /// <summary>
    /// SFMap (ShapeFile Map) is a .NET ShapeFile Control which displays shapefiles in a .NET Windows Form application
    /// </summary>
    /// <remarks>
    /// This is the main control in the EGIS.Controls namespace
    /// <para>
    /// The SFMap control is a .NET ShapeFile Control which provides methods to add or remove ShapeFile layers to/from a map, zoom, pan
    /// and locate shapes on the map.     
    /// </para>
    /// </remarks>
    public partial class SFMap : UserControl
    {
        /// <summary>
        /// EventArgs class containing data for the TooltipDisplayed event
        /// </summary>
        public class TooltipEventArgs : EventArgs
        {
            private int shape = -1;
            private int record = -1;

            private Point mousePos;

            private PointD gisLocation;

            public TooltipEventArgs(int shapeIndex, int recordIndex, Point mousePt, PointD gisPoint)
            {
                this.shape = shapeIndex;
                this.record = recordIndex;
                this.mousePos = mousePt;
                this.gisLocation = gisPoint;
            }

            /// <summary>
            /// Gets / Sets the zero based index of the shapefile.
            /// </summary>
            /// <remarks>The returned value will be between zero and the number of Shapefiles in the SFMap displaying the tooltip<br/>
            /// If no tooltip is displayed ShapeFileIndex returns -1<br/>
            /// </remarks>
            public int ShapeFileIndex
            {
                get
                {
                    return shape;
                }
                set
                {
                    shape = value;
                }
            }

            /// <summary>
            /// Gets / Sets the zero based index of the shapefile record.
            /// </summary>
            /// <remarks>The returned value will be between zero and the number of records in the Shapefile displaying the tooltip<br/>
            /// If no tooltip is displayed RecordIndex returns -1<br/>
            /// </remarks>
            public int RecordIndex
            {
                get
                {
                    return record;
                }
                set
                {
                    record = value;
                }
            }


            public Point MousePosition
            {
                get
                {
                    return mousePos;
                }
                set
                {
                    mousePos = value;
                }
            }

            public PointD GISLocation
            {
                get
                {
                    return gisLocation;
                }
                set
                {
                    gisLocation = value;
                }
            }

        }



        #region "Instance Variables"

        private List<EGIS.ShapeFileLib.ShapeFile> myShapefiles = new List<EGIS.ShapeFileLib.ShapeFile>();
        private Color _mapBackColor = Color.LightGray;
        private Bitmap screenBuf;
        private Boolean dirtyScreenBuf;
        private PointD _centrePoint;
        private double _zoomLevel = 1d;
        private const bool _useHints = true;
        private bool _useBalloonToolTip = false;

        #endregion

#region events

        /// <summary>
        /// Event indicating that the SFMap ShapeFile Layers has changed (ShapeFile added or removed)
        /// </summary>
        public event EventHandler<EventArgs> ShapeFilesChanged;

        /// <summary>
        /// Fires the ShapeFilesChanged event
        /// </summary>
        protected void OnShapeFilesChanged()
        {
            if (ShapeFilesChanged != null)
            {
                ShapeFilesChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// Event indicating that the ZoomLevel of the map has changed
        /// </summary>
        public event EventHandler<EventArgs> ZoomLevelChanged;
        
        /// <summary>
        /// Fires the ZoomLevelChanged event
        /// </summary>
        protected void OnZoomLevelChanged()
        {
            if (ZoomLevelChanged != null)
            {
                ZoomLevelChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// Event fired when a tooltip is displayed for a shapefile with the RenderSettings.UseTooltip property
        /// set to true
        /// </summary>
        /// <remarks>The event is also fired when the tooltip is hidden. In this case the TooltipEventArgs ShapeFileIndex and RecordIndex will return -1
        /// This allows receivers to be notified when the mouse is no longer over a shape</remarks>
        /// <see cref="EGIS.ShapeFileLib.RenderSettings.UseToolTip"/>
        /// <seealso cref="GetShapeIndexAtPixelCoord"/>
        public event EventHandler<TooltipEventArgs> TooltipDisplayed;

        /// <summary>
        /// Fires the TooltipDisplayed event
        /// </summary>
        /// <param name="shapeIndex"></param>
        /// <param name="recordIndex"></param>
        /// <param name="mousePt"></param>
        /// <param name="gisPt"></param>
        protected void OnTooltipDisplayed(int shapeIndex, int recordIndex, Point mousePt, PointD gisPt)
        {
            if (TooltipDisplayed != null)
            {                
                TooltipDisplayed(this, new TooltipEventArgs(shapeIndex, recordIndex, mousePt, gisPt));
            }
        }

        /// <summary>
        /// event fired when selected records of a shapefile change        
        /// </summary>
        /// <remarks>To obtain the selected records iterate over each shapefile and call the SelectedRecordIndices method</remarks>
        /// <see cref="EGIS.ShapeFileLib.ShapeFile.SelectedRecordIndices"/>        
        public event EventHandler<EventArgs> SelectedRecordsChanged;

        protected void OnSelectedRecordChanged(EventArgs args)
        {
            if (SelectedRecordsChanged != null)
            {
                SelectedRecordsChanged(this, args);
            }
        }

       

#endregion

        /// <summary>
        /// SFMap contructor
        /// </summary>
        public SFMap()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Selectable, true);
            _mapBackColor = BackColor;
            this.layerTooltip.IsBalloon = _useBalloonToolTip;
            if (_useBalloonToolTip)
            {
                this.toolTipOffset = new Point(5, 5);
            }            
        }

        #region XmlMethods

        /// <summary>
        /// Writes an xml representation of the current project loaded in the SFMap control
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("MapBackColor");
            writer.WriteString(ColorTranslator.ToHtml(MapBackColor));
            writer.WriteEndElement();

            writer.WriteStartElement("layers");

            foreach (EGIS.ShapeFileLib.ShapeFile sf in myShapefiles)
            {
                sf.WriteXml(writer);
            }


            writer.WriteEndElement();

        }

        public void ReadXml(XmlElement projectElement)
        {
            ReadXml(projectElement, null);
        }
        


        public void ReadXml(XmlElement projectElement, ProgressLoadStatusHandler loadingDelegate)
        {
            XmlNodeList colorList = projectElement.GetElementsByTagName("MapBackColor");
            if (colorList != null && colorList.Count > 0)
            {
                MapBackColor = ColorTranslator.FromHtml(colorList[0].InnerText);
            }

            ClearShapeFiles();

            XmlNodeList layerNodeList = projectElement.GetElementsByTagName("layers");
            XmlNodeList sfList = ((XmlElement)layerNodeList[0]).GetElementsByTagName("shapefile");

            if (sfList != null && sfList.Count > 0)
            {

                for (int n = 0; n < sfList.Count; n++)
                {
                    EGIS.ShapeFileLib.ShapeFile sf = new EGIS.ShapeFileLib.ShapeFile();

                    sf.ReadXml((XmlElement)sfList[n]);
                    //sf.MapProjectionType = this.projectionType;

                    myShapefiles.Add(sf);

                    if (loadingDelegate != null)
                    {
                        loadingDelegate(sfList.Count, n + 1);
                    }

                }
                //set centre point to centre of shapefile and adjust zoom level to fit entire shapefile
                RectangleF r = ShapeFile.LLExtentToProjectedExtent(this.Extent, this.projectionType);
                
                this._centrePoint = new PointD(r.Left + r.Width / 2, r.Top + r.Height / 2);
                this._zoomLevel = this.ClientSize.Width / r.Width;
                dirtyScreenBuf = true;
                Refresh();
                OnShapeFilesChanged();
                OnZoomLevelChanged();

            }

        }


        #endregion


        #region "public properties and methods"

        /// <summary>
        /// Gets or sets the current ZoomLevel of the SFMap
        /// </summary>
        /// <remarks>
        /// Changing the ZoomLevel will zoom into or out of the map. Increasing the ZoomLevel will zoom into the map, while decreasing the 
        /// ZoomLevel will zoom out of the map
        /// <para>
        /// The ZoomLevel, CentrePoint and ClientSize of the SFMap determine the location and visible area of the rendered map. The map will be rendered 
        /// centered at the CentrePoint and scaled according to the ZoomLevel.
        /// </para>
        /// </remarks>
        /// <seealso cref="CentrePoint"/>
        /// <exception cref="System.ArgumentException"> if ZoomLevel less than or equal to zero</exception>        
        [Browsable(false)] 
        public double ZoomLevel
        {
            get
            {
                return _zoomLevel;
            }
            set
            {
                if (value < double.Epsilon) throw new ArgumentException("ZoomLevel can not be <= Zero");
                _zoomLevel = value;
                dirtyScreenBuf=true;
                Invalidate();
                OnZoomLevelChanged();            
            }
        }

        /// <summary>
        /// Gets or sets the centre point of the map in mapping coordinates (as used by the shapefiles)
        /// </summary>
        /// <remarks>
        /// Changing the CentrePoint can be used to center the map on a new location without 
        /// changing the map scale
        /// <para>
        /// The ZoomLevel, CentrePoint and ClientSize of the SFMap determine the location and visible area of the rendered map. The map will be rendered 
        /// centered at the CentrePoint and scaled according to the ZoomLevel.
        /// </para>
        /// <para>This property should no longer be used. Use CentrePoint2D instead, which uses double-precision floating-point coordinates</para>
        /// </remarks>
        /// <seealso cref="ZoomLevel"/>
        [Obsolete("Superceded by CentrePoint2D to use double-precision floating-point coordinates"), Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PointF CentrePoint
        {
            get
            {
                return new PointF((float)_centrePoint.X, (float)_centrePoint.Y);
            }
            set
            {
                //_centrePoint = value;
                //dirtyScreenBuf = true;
                //Invalidate();
                CentrePoint2D = new PointD(value.X, value.Y); //v2.5
            }
        }

        /// <summary>
        /// Gets or sets the center point of the map in mapping coordinates (as used by the shapefiles)
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method replaces the obsolete CentrePoint property
        /// </para>
        /// Changing the CenterPoint2D can be used to center the map on a new location without 
        /// changing the map scale
        /// <para>
        /// The ZoomLevel, CentrePoint and ClientSize of the SFMap determine the location and visible area of the rendered map. The map will be rendered 
        /// centered at the CentrePoint and scaled according to the ZoomLevel.
        /// </para>
        /// </remarks>
        /// <seealso cref="ZoomLevel"/>
        [Browsable(false)] 
        public PointD CentrePoint2D
        {
            get
            {
                if(UseMercatorProjection) return ShapeFile.ProjectionToLatLong(_centrePoint);
                return _centrePoint;
            }
            set
            {
                _centrePoint = value;
                if(UseMercatorProjection) _centrePoint = ShapeFile.LatLongToProjection(_centrePoint);
                dirtyScreenBuf = true;
                Invalidate();
            }
        }
        
        /// <summary>
        /// Gets or sets the color on the map background
        /// </summary>
        public Color MapBackColor
        {
            get
            {
                return _mapBackColor;
            }
            set
            {
                _mapBackColor = value;
                dirtyScreenBuf = true;
                Invalidate();
                
            }
        }

        /// <summary>
        /// Gets or sets the RenderQuality to use when rendering the map
        /// </summary>
        /// <remarks>
        /// <para>
        /// Changing the RenderQuality to RenderQuality.Auto will let the SFMap change the RenderQuality dynamically
        /// depending on the size of the ShapeFiles and as the ZoomLevel changes. This is the default RenderQuality setting and in most cases you will not 
        /// need to change this setting.
        /// </para>
        /// <para>
        /// Changing the RenderQuality to RenderQuality.High will render the map using high quality GDI+ Graphics, but
        /// the rendering speed will be slowed down somewhat.
        /// </para>
        /// <para>
        /// Changing the RenderQuality to RenderQuality.Low will render maps much faster, but at the expense of
        /// lower quality graphics. For very large ShapeFiles when drawing the entire ShapeFile choosing this option
        /// may render the maps considerably faster.
        /// </para>        
        /// </remarks>
        /// <seealso cref="EGIS.ShapeFileLib.RenderQuality"/>
        public EGIS.ShapeFileLib.RenderQuality RenderQuality
        {
            get
            {
                return EGIS.ShapeFileLib.ShapeFile.RenderQuality;
            }
            set
            {
                EGIS.ShapeFileLib.ShapeFile.RenderQuality = value;
                dirtyScreenBuf = true;
                Invalidate();
            }
        }

        //private bool useMercProjection = false;
        private EGIS.ShapeFileLib.ProjectionType projectionType = ProjectionType.None;

        /// <summary>
        /// Gets or sets whether to render the map using the MercatorProjection
        /// </summary>
        public bool UseMercatorProjection
        {
            get
            {
               // return EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection;
                return projectionType == ProjectionType.Mercator;
            }
            set
            {
                //if (EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection == value) return;
                //EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection = value;
                if (value == (projectionType == ProjectionType.Mercator)) return;
                projectionType = value?ProjectionType.Mercator:ProjectionType.None;
                if (projectionType == ProjectionType.Mercator)
                {
                    _centrePoint = EGIS.ShapeFileLib.ShapeFile.LLToMercator(_centrePoint);
                }
                else
                {
                    _centrePoint = EGIS.ShapeFileLib.ShapeFile.MercatorToLL(_centrePoint);
                }
                //UpdateLayersProjectionType(projectionType);
                Refresh(true);
            }
        }

        //private void UpdateLayersProjectionType(ProjectionType projectionType)
        //{
        //    foreach (EGIS.ShapeFileLib.ShapeFile sf in myShapefiles)
        //    {
        //        sf.MapProjectionType = projectionType;                
        //    }
        //}

        /// <summary>
        /// Convenience method to set the ZoomLevel and CentrePoint in one method
        /// </summary>
        /// <param name="zoom">The desired ZoomLevel</param>
        /// <param name="centre">The desired CentrePoint</param>
        /// <seealso cref="ZoomLevel"/>
        /// <seealso cref="CentrePoint"/>
        public void SetZoomAndCentre(double zoom,PointD centre)
        {
            if (zoom < double.Epsilon) throw new ArgumentException("ZoomLevel can not be <= Zero");
            _centrePoint = centre;
            if(UseMercatorProjection) _centrePoint = ShapeFile.LatLongToProjection(_centrePoint); // v2.5
            _zoomLevel = zoom;
            dirtyScreenBuf = true;
            Invalidate();
            OnZoomLevelChanged();
        }

        /// <summary>
        /// Centres and scales the map to fit the entire map in the SFControl
        /// </summary>
        /// <remarks>Call this method to apply a "zoom 100%"</remarks>
        public void ZoomToFullExtent()
        {
            RectangleF r = ShapeFile.LLExtentToProjectedExtent(this.Extent,this.projectionType);
            this._centrePoint = new PointD(r.Left + r.Width / 2, r.Top + r.Height / 2);
            double r1 = ClientSize.Width*r.Height;
            double r2 = ClientSize.Height * r.Width;
            if (r1 < r2)
            {
                this._zoomLevel = this.ClientSize.Width / r.Width;                
            }
            else
            {
                this._zoomLevel = this.ClientSize.Height / r.Height;
            }
                
            dirtyScreenBuf = true;
            Refresh();
            OnZoomLevelChanged();                
        }

        /// <summary>
        /// Converts a MousePoint (in pixel coords) to a map coordinate point
        /// </summary>
        /// <remarks>
        /// This method is being deprecated - use PixelCoordToGisPoint
        /// <see cref="PixelCoordToGisPoint"/>
        /// </remarks>
        /// <param name="pt"></param>
        /// <returns></returns>
        public PointD MousePosToGisPoint(Point pt)
        {
            return MousePosToGisPoint(pt.X, pt.Y);
        }

        /// <summary>
        /// Converts a MousePoint's x and y location to a map coordinate point
        /// </summary>
        /// <remarks>
        /// This method is being deprecated - use PixelCoordToGisPoint
        /// <see cref="PixelCoordToGisPoint"/>
        /// </remarks>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        /// <returns></returns>
        public PointD MousePosToGisPoint(int mouseX, int mouseY)
        {
            double s = 1.0 / ZoomLevel;
            return new PointD(_centrePoint.X - (s * ((ClientSize.Width >> 1) - mouseX)), _centrePoint.Y + (s * ((ClientSize.Height >> 1) - mouseY)));
        }

        /// <summary>
        /// Converts pixel coordinates to map coordinates
        /// </summary>
        /// <param name="pixX">pixel x value</param>
        /// <param name="pixY">pixel y value</param>
        /// <returns></returns>
        public PointD PixelCoordToGisPoint(int pixX, int pixY)
        {
            double s = 1.0 / ZoomLevel;
            if (UseMercatorProjection)
            {
                return ShapeFile.ProjectionToLatLong(new PointD(_centrePoint.X - (s * ((ClientSize.Width >> 1) - pixX)), _centrePoint.Y + (s * ((ClientSize.Height >> 1) - pixY))));
            }
            return new PointD(_centrePoint.X - (s * ((ClientSize.Width >> 1) - pixX)), _centrePoint.Y + (s * ((ClientSize.Height >> 1) - pixY)));
        }

        /// <summary>
        /// Converts pixel coordinates to map coordinates
        /// </summary>
        /// <param name="pt">the x,y pixel coords</param>
        /// <returns></returns>
        public PointD PixelCoordToGisPoint(Point pt)
        {
            return PixelCoordToGisPoint(pt.X, pt.Y);
        }        


        /// <summary>
        /// Converts a GIS position to mouse position
        /// </summary>
        /// <param name="x">x-coord of the GIS point</param>
        /// <param name="y">y-coord of the GIS point</param>
        /// <returns></returns>
        public Point GisPointToPixelCoord(double x, double y)
        {
            PointD p = new PointD(x, y);
            if(UseMercatorProjection) p = ShapeFile.LatLongToProjection(p);
            int mx = (ClientSize.Width >> 1) - (int)Math.Round((_centrePoint.X - p.X) * ZoomLevel);
            int my = (ClientSize.Height >> 1) + (int)Math.Round((_centrePoint.Y - p.Y) * ZoomLevel);
            return new Point(mx, my);
        }

        /// <summary>
        /// Converts a GIS position to mouse position
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public Point GisPointToPixelCoord(PointD pt)
        {
            return GisPointToPixelCoord(pt.X, pt.Y);
        }

        /// <summary>
        /// Returns the shape index for a given shapefile located at a point (in pixel/mouse coordinates)
        /// </summary>
        /// <param name="shapeIndex">The zero based index of the shapefile layer</param>
        /// <param name="pt">The pixel point</param>
        /// <param name="pixelDelta">The pixel delta value added to pt. I.E return shape under pt +/- delta pixels. A suitable value for delta is 5 to 8 pixels</param>
        /// <returns>The zero based shape index, or -1 if no shape is located at pt</returns>
        public int GetShapeIndexAtPixelCoord(int shapeIndex, Point pt, int pixelDelta)
        {
            double delta = pixelDelta / ZoomLevel;
            PointD ptd = PixelCoordToGisPoint(pt);                               
            return this[shapeIndex].GetShapeIndexContainingPoint(ptd, delta);            
        }

        /// <summary>
        /// Gets the rectangular extent of the entire map
        /// </summary>
        /// <remarks>Extent is the rectangular extent of the ENTIRE map, regardless of the current ZoomLevel or CentrePoint.
        /// To get the extent of the current visible area of the map call VisibleExtent</remarks>
        /// <seealso cref="VisibleExtent"/>
        [Browsable(false)] 
        public RectangleD Extent
        {
            get
            {
                if (myShapefiles.Count == 0)
                {
                    return RectangleD.Empty;
                }
                else
                {
                    RectangleD r = myShapefiles[0].Extent;
                    foreach (EGIS.ShapeFileLib.ShapeFile sf in myShapefiles)
                    {
                        r = RectangleD.Union(r, sf.Extent);
                    }
                    return r;
                }
            }
        }


        /// <summary>
        /// Gets the rectangular extent of the entire map in projected coordinates
        /// </summary>
        /// <remarks>ProjectedExtent is the rectangular extent of the ENTIRE map, regardless of the current ZoomLevel or CentrePoint.
        /// If MapProjectionType is ProjectionType.none the returned RectangleD is the same as Extent</remarks>
        /// <seealso cref="VisibleExtent"/>
        [Browsable(false)]
        public RectangleD ProjectedExtent
        {
            get
            {
                return ShapeFile.LLExtentToProjectedExtent(Extent, projectionType);
            }
        }


        /// <summary>
        /// Gets the rectangular extent of the current visible area of the map being displayed in the SFMap control
        /// </summary>
        /// <remarks>VisibleExtent is the current visible area of the map, as determind by the current ZoomLevel, CentrePoint and ClientSize of the SFMap control.
        /// To get the extent of the ENTIRE map call Extent</remarks>
        /// <seealso cref="Extent"/>
        [Browsable(false)] 
        public RectangleF VisibleExtent        
        {
            get
            {
                PointD bl = MousePosToGisPoint(0, this.ClientSize.Height - 1);
                PointD tr = MousePosToGisPoint(this.ClientSize.Width - 1, 0);
                return RectangleF.FromLTRB((float)bl.X, (float)bl.Y, (float)tr.X, (float)tr.Y);
            }
        }

        /// <summary>
        /// Finds and returns the ShapeFile which was loaded from the given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The found ShapeFile or null if the ShapeFile could not be found</returns>
        public EGIS.ShapeFileLib.ShapeFile FindShapeFileBypath(string path)
        {
            for (int index = myShapefiles.Count - 1; index >= 0; index--)
            {
                if (string.Compare(myShapefiles[index].FilePath, path, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return myShapefiles[index];
                }
            }
            return null;
        }

        /// <summary>
        /// Finds and returns the index of the ShapeFile which was loaded from the given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The found zero based ShapeFile index or -1 if the ShapeFile could not be found</returns>
        public int IndexOfShapeFileByPath(string path)
        {
            for (int index = myShapefiles.Count - 1; index >= 0; index--)
            {
                if (string.Compare(myShapefiles[index].FilePath, path, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return index;
                }
            }
            return -1;
        }


        /// <summary>
        /// Adds a new ShapeFile layer to the map
        /// </summary>
        /// <param name="path">The file path to the ShapeFile</param>
        /// <param name="name">The "display" name of the ShapeFile.</param>
        /// <param name="labelFieldName">The name of the field in the ShapeFiles's DBF file to use when rendering the shape labels</param>
        /// <returns>Returns the created ShapeFile which was added to the SFMap</returns>
        /// <remarks>
        /// After the shapefile is added to the map, the map will auto fit the entire ShapeFile in the control by adjusting the 
        /// current ZomLevel and CentrePoint accordingly.
        /// </remarks>
        public EGIS.ShapeFileLib.ShapeFile AddShapeFile(string path, string name, string labelFieldName)
        {            
            EGIS.ShapeFileLib.ShapeFile sf = OpenShapeFile(path, name, labelFieldName);

            
            //set centre point to centre of shapefile and adjust zoom level to fit entire shapefile            
            RectangleF r = ShapeFile.LLExtentToProjectedExtent(sf.Extent, this.projectionType);
            if (!r.IsEmpty)
            {                
                this._centrePoint = new PointD(r.Left + r.Width / 2, r.Top + r.Height / 2);
                double r1 = ClientSize.Width * r.Height;
                double r2 = ClientSize.Height * r.Width;
                if (r1 < r2)
                {
                    this.ZoomLevel = this.ClientSize.Width / r.Width;
                }
                else
                {
                    this.ZoomLevel = this.ClientSize.Height / r.Height;
                }
                dirtyScreenBuf = true;
            }
            OnShapeFilesChanged();            
            return sf;
        }

        /// <summary>
        /// Removes all ShapeFile layers from the map
        /// </summary>
        public void ClearShapeFiles()
        {
            for (int n = 0; n < myShapefiles.Count; n++)
            {
                myShapefiles[n].Close();
            }
            myShapefiles.Clear();
            this.Refresh(true);
            this.OnShapeFilesChanged();

        }

        /// <summary>
        /// Removes the specifed ShapeFile from the SFMap control
        /// </summary>
        /// <param name="shapeFile"></param>
        public void RemoveShapeFile(EGIS.ShapeFileLib.ShapeFile shapeFile)
        {            
            if(myShapefiles.Remove(shapeFile))
            {
                OnShapeFilesChanged();
                dirtyScreenBuf = true;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets the ShapeFile at the spcified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EGIS.ShapeFileLib.ShapeFile this[int index]
        {
            get
            {                
                return myShapefiles[index];
            }
        }

        /// <summary>
        /// Gets the ShapeFile with the specified file path
        /// </summary>        
        /// <returns></returns>
        public EGIS.ShapeFileLib.ShapeFile this[string shapeFilePath]
        {
            get
            {
                int index = IndexOfShapeFileByPath(shapeFilePath);
                if (index >= 0) return this[index];
                return null;
            }
        }

        /// <summary>
        /// Gets the number of ShapeFile layers in the SFMap Control
        /// </summary>
        [Browsable(false)] 
        public int ShapeFileCount
        {
            get
            {
                return myShapefiles.Count;
            }
        }

        /// <summary>
        /// Moves the spcified ShapeFile "up" in the ShapeFile layers
        /// </summary>
        /// <param name="shapeFile"></param>
        /// <remarks>
        /// When ShapeFiles are added to the map the order that they are added determines the order that
        /// they will be rendered. This means that the first ShapeFile layer that is rendered may potentially 
        /// be covered or patially covered by subsequent shapefiles.
        /// By calling the MoveShapeFileUp and MoveShapeFileDown methods you can control the order that the 
        /// ShapeFile layers will be rendered.
        /// </remarks>
        /// <seealso cref="MoveShapeFileDown"/>
        public void MoveShapeFileUp(EGIS.ShapeFileLib.ShapeFile shapeFile)
        {
            int index = myShapefiles.IndexOf(shapeFile);
            if (index ==0 ) return;
            myShapefiles.RemoveAt(index);
            myShapefiles.Insert(index - 1, shapeFile);
            dirtyScreenBuf = true;
            Invalidate();
            OnShapeFilesChanged();
        }

        /// <summary>
        /// Moves the spcified ShapeFile "down" in the ShapeFile layers
        /// </summary>
        /// <param name="shapeFile"></param>
        /// <remarks>
        /// When ShapeFiles are added to the map the order that they are added determines the order that
        /// they will be rendered. This means that the first ShapeFile layer that is rendered may potentially 
        /// be covered or patially covered by subsequent shapefiles.
        /// By calling the MoveShapeFileUp and MoveShapeFileDown methods you can control the order that the 
        /// ShapeFile layers will be rendered.
        /// </remarks>
        /// <seealso cref="MoveShapeFileUp"/>
        public void MoveShapeFileDown(EGIS.ShapeFileLib.ShapeFile shapeFile)
        {
            int index = myShapefiles.IndexOf(shapeFile);
            if (index == myShapefiles.Count-1) return;
            myShapefiles.RemoveAt(index);
            myShapefiles.Insert(index + 1, shapeFile);
            dirtyScreenBuf = true;
            Invalidate();
            OnShapeFilesChanged();
        }



        #endregion

        #region "Private methods"

        private EGIS.ShapeFileLib.ShapeFile OpenShapeFile(string path, string name, string renderFieldName)
        {
            if (path.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 4);
            }
            else
            {
                throw new ArgumentException("path does not end in \".shp\"");
            }
            
            EGIS.ShapeFileLib.ShapeFile sf = new EGIS.ShapeFileLib.ShapeFile();
           
            sf.LoadFromFile(path);
            sf.Name = name;
            //sf.MapProjectionType = this.projectionType;
            sf.RenderSettings = new EGIS.ShapeFileLib.RenderSettings(path, renderFieldName, new Font(this.Font.FontFamily, 6f));
            LoadOptimalRenderSettings(sf);
            myShapefiles.Add(sf);
            return sf;
        }

        protected static void LoadOptimalRenderSettings(EGIS.ShapeFileLib.ShapeFile sf)
        {
            RectangleF r = sf.Extent;
            if (r.Top > 90 || r.Bottom < -90)
            {
                //assume UTM
                sf.RenderSettings.PenWidthScale = 15;
            }
            else
            {
                PointF pt = new PointF(r.Left + r.Width/2, r.Top + r.Height/2);
                EGIS.ShapeFileLib.UtmCoordinate utm1 = EGIS.ShapeFileLib.ConversionFunctions.LLToUtm(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, pt.Y, pt.X);
                EGIS.ShapeFileLib.UtmCoordinate utm2 = utm1;
                utm2.Northing += 15;
                EGIS.ShapeFileLib.LatLongCoordinate ll = EGIS.ShapeFileLib.ConversionFunctions.UtmToLL(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, utm2);
                sf.RenderSettings.PenWidthScale = (float)Math.Abs(ll.Latitude - pt.Y);

            }
        }

        private void RenderShapefiles()
        {
            if (screenBuf == null || screenBuf.Size != this.ClientSize)
            {
                if (screenBuf != null)
                {
                    screenBuf.Dispose();
                }
                screenBuf = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            }
            Graphics g = Graphics.FromImage(screenBuf);
            
            try
            {
                g.Clear(MapBackColor);
                foreach (EGIS.ShapeFileLib.ShapeFile sf in myShapefiles)
                {
                    sf.Render(g, screenBuf.Size, this._centrePoint, this._zoomLevel, this.projectionType);
                }
            }
            finally
            {
                g.Dispose();
            }
            dirtyScreenBuf = false;            
        }

        /// <summary>
        /// Utility method that creates and returns a new Bitmap Image of the current map displayed in the SFMap Control
        /// </summary>
        /// <remarks>
        /// The returned image should be disposed when you have finshed using the Bitmap
        /// </remarks>
        /// <returns></returns>
        public Bitmap GetBitmap()
        {
            Bitmap bm = new Bitmap(this.Width, this.Height);

            if (screenBuf != null)
            {
                Graphics g = Graphics.FromImage(bm);
                try
                {
                    g.DrawImage(screenBuf, 0, 0);
                }
                finally
                {
                    g.Dispose();
                }
            }
            return bm;
        }

        #endregion
        
        protected override void OnPaint(PaintEventArgs e)
        {            
            if (dirtyScreenBuf)
            {
                RenderShapefiles();
            }
            if (screenBuf != null)
            {
               // bool selecting = (_panSelectMode != PanSelectMode.Pan);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                if (_panSelectMode == PanSelectMode.SelectRectangle)
                {
                    e.Graphics.DrawImage(screenBuf, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
                    using (Pen p = new Pen(Color.Red, 1))
                    {
                        using(Brush b = new SolidBrush(Color.FromArgb(20, Color.Red)))
                        {
                            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            
                            Rectangle selectRect = new Rectangle(mouseOffPt.X>=0?mouseDownPt.X:mouseDownPt.X+mouseOffPt.X,
                                mouseOffPt.Y>=0?mouseDownPt.Y:mouseDownPt.Y+mouseOffPt.Y,
                                mouseOffPt.X>=0?mouseOffPt.X:-mouseOffPt.X,
                                mouseOffPt.Y>=0?mouseOffPt.Y:-mouseOffPt.Y);
                            
                            e.Graphics.FillRectangle(b, selectRect);
                            e.Graphics.DrawRectangle(p, selectRect);
                        }
                    }
                }
                else if (_panSelectMode == PanSelectMode.SelectCircle)
                {
                    e.Graphics.DrawImage(screenBuf, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
                    using (Pen p = new Pen(Color.Red, 1))
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(20, Color.Red)))
                        {
                            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                            int radius = (int)Math.Round(Math.Sqrt(mouseOffPt.X * mouseOffPt.X + mouseOffPt.Y * mouseOffPt.Y));
                            Rectangle selectRect = new Rectangle(mouseDownPt.X -radius,
                                mouseDownPt.Y -radius,
                                radius*2,
                                radius*2);

                            //e.Graphics.FillRectangle(b, selectRect);
                            //e.Graphics.DrawRectangle(p, selectRect);
                            e.Graphics.FillEllipse(b, selectRect);
                            e.Graphics.DrawEllipse(p, selectRect);
                        }
                    }

                }
                else// if (_panSelectMode == PanSelectMode.Pan)
                {
                    //change this to only draw invalid area
                    if ((mouseOffPt.X == 0) && (mouseOffPt.Y == 0))
                    {
                        e.Graphics.DrawImage(screenBuf, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
                    }
                    else
                    {
                        e.Graphics.DrawImage(screenBuf, mouseOffPt.X, mouseOffPt.Y);
                    }
                }
            }

            System.Drawing.Drawing2D.Matrix m = e.Graphics.Transform;
            try
            {
                System.Drawing.Drawing2D.Matrix m2 = new System.Drawing.Drawing2D.Matrix();
                m2.Translate(mouseOffPt.X, mouseOffPt.Y);
                e.Graphics.Transform = m2;
                base.OnPaint(e);
            }
            finally
            {
                e.Graphics.Transform = m;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            dirtyScreenBuf = true;
        }

        /// <summary>
        /// Causes the map to redraw all of its contents immediately
        /// </summary>
        /// <param name="fullRefresh">Flag to indicate whether to redraw al layers or just re-draw the internal buffer</param>
        /// <remarks>
        /// Internally the SFMap control uses an offsceen buffer to draw the contents of the map to, which provides efficient 
        /// painting when the control is redrawn due to an OS call (i.e. another window is moved accross the SFMap control).
        /// Caling this method with fullRefresh set to true will cause the internal buffer to be re-drawn. You should call this 
        /// method if a ShapeFile's RenderSettings are changed to ensure the layer is redrawn.
        /// </remarks>
        public void Refresh(bool fullRefresh)
        {
            if (fullRefresh) dirtyScreenBuf = true;
            Refresh();
        }

        #region "Mouse and key Handling Methods"

        private void PanLeft()
        {
            RectangleD r = ProjectedExtent;
            PointD pt = CentrePoint2D;
            pt.X -= (ClientSize.Width >> 2) / ZoomLevel; ;// (0.0025f * r.Width);
            CentrePoint2D = pt;
        }

        private void PanRight()
        {
            RectangleD r = ProjectedExtent;
            PointD pt = CentrePoint2D;
            pt.X += (ClientSize.Width >> 2) / ZoomLevel;// (0.0025f * r.Width);
            CentrePoint2D = pt;
        }

        private void PanUp()
        {
            RectangleD r = ProjectedExtent;
            PointD pt = CentrePoint2D;
            pt.Y += (ClientSize.Height >> 2) / ZoomLevel; //(0.0025f * r.Height);
            CentrePoint2D = pt;
        }

        private void PanDown()
        {
            RectangleD r = ProjectedExtent;
            PointD pt = CentrePoint2D;
            pt.Y -= (ClientSize.Height >> 2) / ZoomLevel;
            CentrePoint2D = pt;
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            ctrlDown = (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey);
            shiftDown = (e.KeyCode == Keys.ShiftKey);
            switch (e.KeyCode)
            {
                case Keys.Left:
                    PanLeft();
                    break;
                case Keys.Right:
                    PanRight();
                    break;
                case Keys.Up:
                    PanUp();
                    break;
                case Keys.Down:
                    PanDown();
                    break;
            }            
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            ctrlDown = false;
            shiftDown = false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                    return true;
                case Keys.Right:
                    return true;
                case Keys.Up:
                    return true;
                case Keys.Down:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }            
        }

                
        private PanSelectMode controlPanSelectMode = PanSelectMode.Pan;

        public PanSelectMode PanSelectMode
        {
            get
            {
                return controlPanSelectMode;
            }
            set
            {
                controlPanSelectMode = value;
            }
        }

        /// <summary>
        /// _panSelectMode is set by keys or controlPanSelectMode
        /// </summary>
        private PanSelectMode _panSelectMode = PanSelectMode.Pan;

        private bool ctrlDown = false;
        private bool shiftDown = false;
        private bool toggleSelect = false;

        private MouseButtons mouseDownButton = MouseButtons.None;
        private Point mouseDownPt = Point.Empty;
        private Point mouseOffPt = new Point(0, 0);

        /// <summary>
        /// Handles the MouseDown event. Derived classes overriding this method should call base.OnMouseDown
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mouseDownButton = e.Button;
            mouseDownPt = new Point(e.X, e.Y);

            if (PanSelectMode != PanSelectMode.Pan)
            {
                _panSelectMode = PanSelectMode;
                toggleSelect = !shiftDown && ctrlDown;
            }            
            else
            {
                if (shiftDown)
                {
                    _panSelectMode = (e.Button == MouseButtons.Left) ? PanSelectMode.SelectRectangle : PanSelectMode.SelectCircle;
                    toggleSelect = false;
                }
                else if (ctrlDown)
                {
                    _panSelectMode = (e.Button == MouseButtons.Left) ? PanSelectMode.SelectRectangle : PanSelectMode.SelectCircle;
                    toggleSelect = true;
                }
                else _panSelectMode = PanSelectMode.Pan;
            }
        }

        /// <summary>
        /// Handles the MouseUp event. Derived classes overriding this method should call base.OnMouseUp
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            Cursor oldCursor = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                if (_panSelectMode == PanSelectMode.SelectRectangle)
                {
                    PointD pt1 = PixelCoordToGisPoint(mouseDownPt);
                    PointD pt2 = PixelCoordToGisPoint(e.X, e.Y);
                    RectangleD selRect = RectangleD.FromLTRB(Math.Min(pt1.X, pt2.X),
                        Math.Min(pt1.Y, pt2.Y),
                        Math.Max(pt1.X, pt2.X),
                        Math.Max(pt1.Y, pt2.Y));
                    //if we've just clicked then expand the rectangle
                    if (Math.Abs(e.X - mouseDownPt.X) < 2 && Math.Abs(e.Y - mouseDownPt.Y) < 2)
                    {
                        selRect.Inflate(4f / ZoomLevel, 4f / ZoomLevel);
                    }
                    bool fireEvent = false;
                    for (int n = 0; n < this.ShapeFileCount; ++n)
                    {
                        if (!myShapefiles[n].IsSelectable) continue;
                        fireEvent = true;
                        List<int> ind = new List<int>();
                        myShapefiles[n].GetShapeIndiciesIntersectingRect(ind, selRect);

                        if (toggleSelect)
                        {
                            foreach (int index in ind)
                            {
                                myShapefiles[n].SelectRecord(index, !myShapefiles[n].IsRecordSelected(index));
                            }
                        }
                        else
                        {
                            myShapefiles[n].ClearSelectedRecords();
                            foreach (int index in ind)
                            {
                                myShapefiles[n].SelectRecord(index, true);
                            }
                        }
                    }

                    mouseOffPt = new Point(0, 0);

                    Refresh(true);

                    if(fireEvent) OnSelectedRecordChanged(new EventArgs());
                }
                else if (_panSelectMode == PanSelectMode.SelectCircle)
                {

                    PointD pt1 = PixelCoordToGisPoint(mouseDownPt);
                    PointD pt2 = PixelCoordToGisPoint(e.X, e.Y);
                    double radius = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));

                    //if we've just clicked then expand the radius
                    if (Math.Abs(e.X - mouseDownPt.X) < 2 && Math.Abs(e.Y - mouseDownPt.Y) < 2)
                    {
                        radius += (4f / ZoomLevel);
                    }
                    bool fireEvent = false;
                    for (int n = 0; n < this.ShapeFileCount; ++n)
                    {
                        if (!myShapefiles[n].IsSelectable) continue;
                        fireEvent = true;
                        
                        List<int> ind = new List<int>();
                        myShapefiles[n].GetShapeIndiciesIntersectingCircle(ind, pt1, radius);

                        if (toggleSelect)
                        {
                            foreach (int index in ind)
                            {
                                myShapefiles[n].SelectRecord(index, !myShapefiles[n].IsRecordSelected(index));
                            }
                        }
                        else
                        {
                            myShapefiles[n].ClearSelectedRecords();
                            foreach (int index in ind)
                            {
                                myShapefiles[n].SelectRecord(index, true);
                            }
                        }
                    }


                    mouseOffPt = new Point(0, 0);

                    Refresh(true);

                    if(fireEvent) OnSelectedRecordChanged(new EventArgs());

                }
                else
                {
                    mouseOffPt = new Point(e.X - mouseDownPt.X, e.Y - mouseDownPt.Y);
                    if (!mouseOffPt.IsEmpty)
                    {
                        double s = 1d / ZoomLevel;
                        _centrePoint.X -= (s * mouseOffPt.X);
                        _centrePoint.Y += (s * mouseOffPt.Y);

                        mouseOffPt = new Point(0, 0);

                        Refresh(true);
                    }
                }
            }
            finally
            {
                mouseDownButton = MouseButtons.None;
                Cursor = oldCursor;
            }
            
        }

        /// <summary>
        /// Handles the MouseMove event. Derived classes overriding this method should call base.OnMouseMove
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button != MouseButtons.None)
            {
                mouseOffPt = new Point(e.X - mouseDownPt.X, e.Y - mouseDownPt.Y);
                Invalidate();
            }
            else
            {
                if(this.ShapeFileCount > 0)
                {
                    PointD pt = PixelCoordToGisPoint(e.X, e.Y);
                    LocateShape(pt, new Point(e.X,e.Y));                    
                }
            }
        }

        /// <summary>
        /// Handles the MouseLeave event. Derived classes overriding this method should call base.OnMouseLeave
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (layerTooltipVisible)
            {
                layerTooltip.Hide(this);
                layerTooltipVisible = false;
                OnTooltipDisplayed(-1, -1, Point.Empty, PointD.Empty);                
            }            
        }

        /// <summary>
        /// Handles the MouseWheel event. Derived classes overriding this method should call base.OnMouseWheel
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
            {
                double z = ZoomLevel;
                ZoomLevel = z * 2d;
            }
            else if (e.Delta < 0)
            {
                double z = ZoomLevel;
                ZoomLevel = z * 0.5d;
            }
        }

        private ToolTip layerTooltip = new ToolTip();
        private bool layerTooltipVisible = false;
        private Point toolTipOffset = new Point(15, 3);
        private Point lastLocateMousePos = Point.Empty;
        private void LocateShape(PointD pt, Point mousePos)
        {
            if (lastLocateMousePos.Equals(mousePos))
            {
                //need to test against last mousepos because Windows fires multiple mousemove events when
                // a balloon is used. Vista fires multiple mousemove events regardless of whether a balloon is used
                return;
            }
            lastLocateMousePos = mousePos;
            double delta = 8.0 / ZoomLevel;
            PointF ptf = new PointF((float)pt.X, (float)pt.Y);
            for (int l = ShapeFileCount - 1; l >= 0; l--)
            {
                bool useToolTip = (_useHints && this[l].RenderSettings != null && this[l].RenderSettings.UseToolTip);
                bool useCustomToolTip = (useToolTip && this[l].RenderSettings.CustomRenderSettings != null && this[l].RenderSettings.CustomRenderSettings.UseCustomTooltips);
                RectangleF layerExtent = this[l].GetActualExtent();
                layerExtent.Inflate((float)delta, (float)delta);
                if ((this[l].IsSelectable || useToolTip) && layerExtent.Contains(ptf) && this[l].IsVisibleAtZoomLevel((float)ZoomLevel))
                {
                    int selectedIndex = this[l].GetShapeIndexContainingPoint(pt, delta);
                    if (selectedIndex >= 0)
                    {
                        if (this[l].IsSelectable) Cursor = Cursors.Hand;
                        if (_useHints)
                        {
                            if (useCustomToolTip)
                            {
                                string s = this[l].RenderSettings.CustomRenderSettings.GetRecordToolTip(selectedIndex);
                                if (s != null)
                                {
                                    layerTooltip.Show(s, this, mousePos.X + toolTipOffset.X, mousePos.Y + toolTipOffset.Y);
                                    layerTooltipVisible = true;
                                    OnTooltipDisplayed(l, selectedIndex, mousePos, pt);
                                    return;
                                }
                            }
                            else
                            {
                                string s = "record : " + selectedIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                if (this[l].RenderSettings.ToolTipFieldIndex >= 0)
                                {
                                    string temp = this[l].RenderSettings.DbfReader.GetField(selectedIndex, this[l].RenderSettings.ToolTipFieldIndex).Trim();
                                    if (temp.Length > 0)
                                    {
                                        s += "\n" + temp;
                                    }
                                }
                                layerTooltip.Show(s, this, mousePos.X + toolTipOffset.X, mousePos.Y + toolTipOffset.Y);//, 5000);  
                                layerTooltipVisible = true;
                                OnTooltipDisplayed(l, selectedIndex, mousePos, pt);
                            }
                        }
                        return;
                    }
                }                
            }
            Cursor = Cursors.Default;
            if (_useHints)
            {                
                if (layerTooltipVisible)
                {
                    OnTooltipDisplayed(-1, -1, mousePos, pt);
                    layerTooltip.Hide(this);
                    layerTooltipVisible = false;
                }
            }
        }

        /// <summary>
        /// Handles the DoubleClick event. Derived classes overriding this method should call base.OnDoubleClick
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDoubleClick(EventArgs e)
        {            
            base.OnDoubleClick(e);
            if (mouseDownButton == MouseButtons.Left)
            {
                //zoom in
                PointD pt = this._centrePoint;// ShapeFile.LatLongToProjection(CentrePoint2D);
                double z = ZoomLevel;
                pt.Y -= (mouseDownPt.Y-(ClientSize.Height >> 1)) / z;
                pt.X += (mouseDownPt.X - (ClientSize.Width >> 1)) / z;
                if (UseMercatorProjection)
                {
                    pt = ShapeFile.ProjectionToLatLong(pt);
                }
                SetZoomAndCentre(z * 2, pt);
            }
            else if (mouseDownButton == MouseButtons.Right)
            {
                //zoom out
                PointD pt = this._centrePoint;// ShapeFile.LatLongToProjection(CentrePoint2D);
                double z = ZoomLevel;
                pt.Y -= (mouseDownPt.Y - (ClientSize.Height >> 1)) / z;
                pt.X += (mouseDownPt.X - (ClientSize.Width >> 1)) / z;
                if (UseMercatorProjection)
                {
                    pt = ShapeFile.ProjectionToLatLong(pt);
                }
                SetZoomAndCentre(z / 2, pt);
            }
        }

        #endregion

    }
}