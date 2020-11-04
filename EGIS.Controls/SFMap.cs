#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2016 Winston Fletcher.
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
using EGIS.Projections;
using System.IO;

[assembly: CLSCompliant(true)]
namespace EGIS.Controls
{
    /// <summary>
    /// delegate to called to handle progress project loading
    /// </summary>
    /// <param name="totalLayers"></param>
    /// <param name="numberLayersLoaded"></param>
    public delegate void ProgressLoadStatusHandler(int totalLayers, int numberLayersLoaded);

    
    /// <summary>
    /// Pan/Select model enumeration
    /// </summary>
    public enum PanSelectMode { 
        /// <summary>
        /// None defined
        /// </summary>
        None,
        /// <summary>
        /// Pan mode
        /// </summary>
        Pan, 
        /// <summary>
        /// Rectangular select mode
        /// </summary>
        SelectRectangle, 
        /// <summary>
        /// Circular select mode
        /// </summary>
        SelectCircle,
        /// <summary>
        /// Zoom map to selected rectangle
        /// </summary>
        ZoomRectangle,
        /// <summary>
        /// Zoom map to fit selected circle
        /// </summary>
        ZoomCircle
    };
    
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

        /// <summary>
        /// event fired when the map bacground is rendered. Handle this event to perform and painting before
        /// the map layers are rendered
        /// </summary>
        public event EventHandler<PaintEventArgs> PaintMapBackground;

        protected void OnPaintMapBackground(PaintEventArgs args)
        {
            if (PaintMapBackground != null)
            {
                PaintMapBackground(this, args);               
            }
        }

        public class MapDoubleClickedEventArgs : EventArgs
        {
            public MapDoubleClickedEventArgs()
            {
                Cancel = false;
            }

            /// <summary>
            /// get set whether to cancel the event. Set true to disable core zooming functinonality that occurs when
            /// the map is double clicked
            /// </summary>
            public bool Cancel
            {
                get;
                set;
            }
        }

        //event fired when the map is double clicked. Handle this event rather than the standard DoubleClick
        //event if you wish to cancel the default zooming on the map
        public event EventHandler<MapDoubleClickedEventArgs> MapDoubleClick;

        protected void OnMapDoubleClick(MapDoubleClickedEventArgs args)
        {
            if (MapDoubleClick != null)
            {
                MapDoubleClick(this, args);
            }
        }


        /// <summary>
        /// Event fired when the map's Coordinate Reference System is changed
        /// </summary>
        public event EventHandler<EventArgs> CoordinateReferenceSystemChanged;


        /// <summary>
        /// Raises the CoordinateReferenceSystemChanged event.
        /// </summary>
        /// <param name="args">EventArgs object</param>
        protected void OnCoordinateReferenceSystemChanged(EventArgs args)
        {            
            if (CoordinateReferenceSystemChanged != null)
            {
                CoordinateReferenceSystemChanged(this, args);
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
            //set default CRS to WGS84
            try
            {
                var crs = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode);
                MapCoordinateReferenceSystem = crs;
            }
            catch
            {
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

        /// <summary>
        /// reads and loads XML representatino of a .EGP project
        /// </summary>
        /// <param name="projectElement"></param>
        public void ReadXml(XmlElement projectElement, string baseDirectory)
        {
            ReadXml(projectElement, baseDirectory, null);
        }


        /// <summary>
        /// reads and loads XML representation of a .EGP project
        /// </summary>
        /// <param name="projectElement"></param>
        /// <param name="loadingDelegate"></param>
        public void ReadXml(XmlElement projectElement, string baseDirectory, ProgressLoadStatusHandler loadingDelegate)
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

                    sf.ReadXml((XmlElement)sfList[n], baseDirectory, this.UseMemoryStreams);
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
                if(this.ClientSize.Width > 0) this._zoomLevel = this.ClientSize.Width / r.Width;
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

                //avoid overflow if zooming to less than 1mm
                if (value > this._zoomLevel && this.mapCoordinateReferenceSystem is IProjectedCRS && (1/value < 0.00001)) return;

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


        private ICRS mapCoordinateReferenceSystem = null;
        /// <summary>
        /// Get/Set the map Coordinate Reference System
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICRS MapCoordinateReferenceSystem
        {
            get
            {
                return mapCoordinateReferenceSystem;
            }
            set
            {
                //convert the current visible extent to equivalent extent of the new CRS
                bool fireEvent = (this.mapCoordinateReferenceSystem != null && value == null) || (this.mapCoordinateReferenceSystem == null && value != null) ||
                                   (this.mapCoordinateReferenceSystem != null && value != null && !this.mapCoordinateReferenceSystem.IsEquivalent(value));
                RectangleD currentExtent = this.VisibleExtent;
                currentExtent = currentExtent.Transform(this.mapCoordinateReferenceSystem, value);                
                this.mapCoordinateReferenceSystem = value;
                if (currentExtent.IsValidExtent())
                {
                    FitToExtent(currentExtent);
                }
                else
                {
                    ZoomToFullExtent();
                }
                if (fireEvent) OnCoordinateReferenceSystemChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Get/Set whether any ShapeFiless added to the map should use MemoryStreams rather than 
        /// reading directly from disk.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is false.
        /// </para>
        /// <para>
        /// If UseMemoryStreams is changed it will only apply to any new ShapeFiles added to the map.        
        /// </para>
        /// </remarks>
        public bool UseMemoryStreams
        {
            get;
            set;
        }

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

            //avoid overflow if zooming to less than 1mm
            if (zoom > this._zoomLevel && this.mapCoordinateReferenceSystem is IProjectedCRS && (1 / zoom < 0.00001)) return;

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
            RectangleD r = ShapeFile.LLExtentToProjectedExtent(this.Extent,this.projectionType);

            this._centrePoint = new PointD(r.Left + r.Width / 2, r.Top + r.Height / 2);
            Size cs = ClientSize;
            //eliminate possible div by zero
            if (cs.Width <= 0 || cs.Height <= 0) cs = new System.Drawing.Size(100, 100);
            double r1 = cs.Width*r.Height;
            double r2 = cs.Height * r.Width;
            
            if (r1 < r2)
            {
                this._zoomLevel = cs.Width / r.Width;                
            }
            else
            {
                this._zoomLevel = cs.Height / r.Height;
            }
                
            dirtyScreenBuf = true;
            Refresh();
            OnZoomLevelChanged();                
        }

        /// <summary>
        /// Zooms and cetres the map to fit given extent
        /// </summary>
        /// <param name="extent"></param>
        public void FitToExtent(RectangleD extent)
        {
            if (extent.IsEmpty) return;

            extent = RestrictExtentToCRS(extent);

            MouseOffsetPoint = Point.Empty;            
            RectangleD r = ShapeFile.LLExtentToProjectedExtent(extent, this.projectionType);
            this._centrePoint = new PointD(r.Left + r.Width / 2, r.Top + r.Height / 2);

            if(this.mapCoordinateReferenceSystem is IProjectedCRS && (r.Width < 0.0001 || r.Height < 0.0001)) return;
            Size cs = ClientSize;
            //eliminate possible div by zero
            if (cs.Width <= 0 || cs.Height <= 0) cs = new System.Drawing.Size(100, 100);
            double r1 = cs.Width * r.Height;
            double r2 = cs.Height * r.Width;

            if (r1 < r2)
            {
                this._zoomLevel = cs.Width / r.Width;
            }
            else
            {
                this._zoomLevel = cs.Height / r.Height;
            }
            //Console.Out.WriteLine("r=" + r);
            //Console.Out.WriteLine("_zoomLevel = " + _zoomLevel);

            dirtyScreenBuf = true;
            Refresh();
            OnZoomLevelChanged();           
        }

        /// <summary>
        /// zooms to selected record bounds in zero based layer on the map 
        /// </summary>
        /// <param name="shapefileIndex"></param>
        public void ZoomToSelection(int shapefileIndex)
        {
            if (shapefileIndex < 0 || shapefileIndex >= this.ShapeFileCount) return;
            ZoomToSelection(this[shapefileIndex]);
        }

        /// <summary>
        /// zooms the given layer's selected records bounds
        /// </summary>
        /// <param name="layer"></param>
        public void ZoomToSelection(ShapeFile layer)
        {
            System.Collections.ObjectModel.ReadOnlyCollection<int> selectedIndicies = layer.SelectedRecordIndices;
            if (selectedIndicies.Count > 0)
            {
                RectangleD extent = layer.GetShapeBoundsD(selectedIndicies[0]);
                for (int n = 1; n < selectedIndicies.Count; ++n)
                {
                    extent = RectangleD.Union(extent, layer.GetShapeBoundsD(selectedIndicies[n]));                    
                }
                extent = extent.Transform(layer.CoordinateReferenceSystem, this.MapCoordinateReferenceSystem);
                FitToExtent(extent);
            }
        }

        /// <summary>
        /// Zooms to the selected records bounds of all layers loaded in the control
        /// </summary>
        public void ZoomToSelection()
        {
            RectangleD extent = RectangleD.Empty;
            bool extentSet = false;
            foreach (ShapeFile layer in myShapefiles)
            {
                System.Collections.ObjectModel.ReadOnlyCollection<int> selectedIndicies = layer.SelectedRecordIndices;
                if (selectedIndicies.Count > 0)
                {
                    for (int n = 0; n < selectedIndicies.Count; ++n)
                    {
                        if (extentSet)
                        {
                            extent = RectangleD.Union(extent, layer.GetShapeBoundsD(selectedIndicies[n]));
                        }
                        else
                        {
                            extent = layer.GetShapeBoundsD(selectedIndicies[n]);
                            extentSet = true;
                        }                        
                    }
                }
            }
            if(extentSet) FitToExtent(extent);
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
            return this[shapeIndex].GetShapeIndexContainingPoint(ptd, delta, this.MapCoordinateReferenceSystem);            
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
                   // RectangleD r1 = myShapefiles[0].Extent;
                    //Console.Out.WriteLine("r1 = " + r1);

                    RectangleD r = myShapefiles[0].Extent.Transform(myShapefiles[0].CoordinateReferenceSystem, this.MapCoordinateReferenceSystem);

                    if (double.IsInfinity(r.Width) || double.IsInfinity(r.Height))
                    {
                        r = RestrictExtentToCRS(r);
                    }
                    //RectangleD r2 = ShapeFile.ConvertExtent(r, this.MapCoordinateReferenceSystem, myShapefiles[0].CoordinateReferenceSystem);
                     
                    foreach (EGIS.ShapeFileLib.ShapeFile sf in myShapefiles)
                    {
                        var extent = sf.Extent.Transform(sf.CoordinateReferenceSystem, this.MapCoordinateReferenceSystem);
                        if (double.IsInfinity(extent.Width) || double.IsInfinity(extent.Height))
                        {
                            extent = RestrictExtentToCRS(extent);
                        }
                        r = RectangleD.Union(r, extent);
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
        public RectangleD VisibleExtent        
        {
            get
            {
                //PointD bl = MousePosToGisPoint(0, this.ClientSize.Height - 1);
                //PointD tr = MousePosToGisPoint(this.ClientSize.Width - 1, 0);
                PointD bl = PixelCoordToGisPoint(0, this.ClientSize.Height - 1);
                PointD tr = PixelCoordToGisPoint(this.ClientSize.Width - 1, 0);
                return RectangleD.FromLTRB(bl.X, bl.Y, tr.X, tr.Y);
            }
        }

        public RectangleD RestrictExtentToCRS(RectangleD extent)
        {
            if (this.MapCoordinateReferenceSystem != null && this.MapCoordinateReferenceSystem.AreaOfUse.IsDefined)
            {
                if (double.IsInfinity(extent.Width) || double.IsInfinity(extent.Height))
                {
                    //convert the area of use from lat/lon degrees to the shapefile CRS
                    ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
                    RectangleD areaOfUse = RectangleD.FromLTRB(this.MapCoordinateReferenceSystem.AreaOfUse.WestLongitudeDegrees,
                        this.MapCoordinateReferenceSystem.AreaOfUse.SouthLatitudeDegrees,
                        this.MapCoordinateReferenceSystem.AreaOfUse.EastLongitudeDegrees,
                        this.MapCoordinateReferenceSystem.AreaOfUse.NorthLatitudeDegrees);
                    areaOfUse = areaOfUse.Transform(wgs84, this.MapCoordinateReferenceSystem);

                    return areaOfUse;
                }
            }
            return extent;
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
		/// <param name="useMemoryStreams">Optional parameter indicating whether to open the ShapeFile using MemoryStreams. Default is false</param>
		/// <param name="fitMapToLayerExtent">optional parameter to fit the map to the shapefile's extent. Default is true</param>
		/// <returns>Returns the created ShapeFile which was added to the SFMap</returns>
		/// <remarks>
		/// After the shapefile is added to the map, the map will auto fit the entire ShapeFile in the control by adjusting the 
		/// current ZomLevel and CentrePoint accordingly.
		/// </remarks>
		public EGIS.ShapeFileLib.ShapeFile AddShapeFile(string path, string name, string labelFieldName, bool useMemoryStreams=false, bool fitMapToLayerExtent=true)
        {            
            EGIS.ShapeFileLib.ShapeFile sf = OpenShapeFile(path, name, labelFieldName, useMemoryStreams);

			if (fitMapToLayerExtent)
			{
				RectangleD extent = sf.Extent.Transform(sf.CoordinateReferenceSystem, this.MapCoordinateReferenceSystem);
				FitToExtent(extent);
			}
			else
			{
				//refresh so the layer is drawn
				Refresh(true);
			}
			OnShapeFilesChanged();            
            return sf;
        }

		/// <summary>
		/// Adds a new ShapeFile layer to the map
		/// </summary>
		/// <param name="shxStream">stream to the ShapeFile's shx file</param>
		/// <param name="shpStream">stream to the ShapeFile's shp file</param>
		/// <param name="dbfStream">stream to the ShapeFile's dbf file</param>
		/// <param name="prjStream">stream to the ShapeFile's prj file</param>
		/// <param name="name">The "display" name of the ShapeFile. </param>
		/// <param name="labelFieldName">The name of the field in the ShapeFiles's DBF file to use when rendering the shape labels</param>
		/// <param name="fitMapToLayerExtent">optional parameter to fit the map to the shapefile's extent. Default is true</param>
		/// <returns>Returns the created ShapeFile which was added to the SFMap</returns>
		public EGIS.ShapeFileLib.ShapeFile AddShapeFile(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, string name, string labelFieldName, bool fitMapToLayerExtent = true)
		{
			EGIS.ShapeFileLib.ShapeFile sf = OpenShapeFile(shxStream, shpStream, dbfStream, prjStream, name, labelFieldName);

			if (fitMapToLayerExtent)
			{ 
				RectangleD extent = sf.Extent.Transform(sf.CoordinateReferenceSystem, this.MapCoordinateReferenceSystem);
				FitToExtent(extent);
			}
			else
			{
				//refrresh so the layer is drawn
				Refresh(true);
			}
			OnShapeFilesChanged();
            return sf;
        }

		/// <summary>
		/// Adds a new ShapeFile layer to the map
		/// </summary>
		/// <param name="shapeFile">ShapeFile object to add to the map</param>
		/// <param name="fitMapToLayerExtent">optional parameter to fit the map to the shapefile's extent. Default is true</param>
		/// <returns>Returns the shapeFile which was added to the SFMap</returns>
		public EGIS.ShapeFileLib.ShapeFile AddShapeFile(EGIS.ShapeFileLib.ShapeFile shapeFile, bool fitMapToLayerExtent = true)
		{
			myShapefiles.Add(shapeFile);

			if (fitMapToLayerExtent)
			{
				RectangleD extent = shapeFile.Extent.Transform(shapeFile.CoordinateReferenceSystem, this.MapCoordinateReferenceSystem);
				FitToExtent(extent);
			}
			else
			{
				//refresh so the layer is drawn
				Refresh(true);
			}
			OnShapeFilesChanged();
			return shapeFile;
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

        /// <summary>
        /// Creates a Graphics object from the SFMap's internal Image used for double buffering. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// The SFMap control uses double buffering for all drawing. All drawing is performed on an 
        /// image buffer before painting the control. This method creates a Graphics object from the 
        /// control's Image buffer.
        /// </para>
        /// <para>
        /// Every call to this method will create a new Graphics obect and the returned Graphics object should be disposed by the caller. 
        /// If the control is resized the internal buffer will be re-created and a Graphics object returned from this method 
        /// previously may not be valid.
        /// </para>
        /// </remarks>
        public Graphics CreateMapGraphics()
        {
            return Graphics.FromImage(this.screenBuf);
        }

        /// <summary>
        /// Gets the SFMap's internal Image used for double buffering. All drawing is performed on this
        /// image before painting the control
        /// </summary>
        protected Image ScreenBuffer
        {
            get
            {
                return this.screenBuf;
            }
        }

		private Cursor defaultCursor = Cursors.Default;

		/// <summary>
		/// The default Cursor of the control
		/// </summary>
		public Cursor DefaultMapCursor
		{
			set
			{
				this.defaultCursor = value != null ? value : Cursors.Default;
				this.Cursor = defaultCursor;
			}
			get
			{
				return this.defaultCursor;
			}
		}

		private Cursor selectionCursor = Cursors.Hand;

		/// <summary>
		/// The default Cursor of the control when the mouse is over a shape that can be selected. Default is Cursors.Hand
		/// </summary>
		public Cursor DefaultSelectionCursor
		{
			set
			{
				this.selectionCursor = value != null ? value : Cursors.Hand;				
			}
			get
			{
				return this.selectionCursor;
			}
		}



		#endregion

		#region "Private methods"

		private EGIS.ShapeFileLib.ShapeFile OpenShapeFile(string path, string name, string renderFieldName, bool useMemoryStreams = false)
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
            sf.LoadFromFile(path, useMemoryStreams || this.UseMemoryStreams);
            sf.Name = name;
            if (sf.RenderSettings != null) sf.RenderSettings.Dispose();
            sf.RenderSettings = new EGIS.ShapeFileLib.RenderSettings(path, renderFieldName, new Font(this.Font.FontFamily, 6f));
            LoadOptimalRenderSettings(sf);
            myShapefiles.Add(sf);
            return sf;
        }

        private EGIS.ShapeFileLib.ShapeFile OpenShapeFile(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, string name, string renderFieldName)
        {            
            EGIS.ShapeFileLib.ShapeFile sf = new EGIS.ShapeFileLib.ShapeFile();
            sf.LoadFromFile(shxStream,shpStream,dbfStream,prjStream);
            sf.Name = name;
            sf.RenderSettings.FieldName = renderFieldName;
			sf.RenderSettings.Font = new Font(this.Font.FontFamily, 8f);
            LoadOptimalRenderSettings(sf);
            myShapefiles.Add(sf);
            return sf;
        }


        
        /// <summary>
        /// Load optimal render settings
        /// </summary>
        /// <param name="sf"></param>
        protected void LoadOptimalRenderSettings(EGIS.ShapeFileLib.ShapeFile sf)
        {
            try
            {
                sf.RenderSettings.MaxPixelPenWidth = 10;

                if (sf.CoordinateReferenceSystem == null)
                {
                    sf.RenderSettings.MinPixelPenWidth = 1;
                    return;
                }

                if (this.MapCoordinateReferenceSystem is EGIS.Projections.IProjectedCRS)
                {
                    //assume projected coordinates
                    sf.RenderSettings.PenWidthScale = 5;
                }
                else
                {
                    RectangleD r = sf.Extent;
                    double[] pt = new double[]{r.Left + r.Width / 2, r.Top + r.Height / 2};
                    
                    using (ICoordinateTransformation coordTransformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sf.CoordinateReferenceSystem, this.MapCoordinateReferenceSystem))
                    {
                        coordTransformation.Transform(pt, 1);
                    }
                    EGIS.ShapeFileLib.UtmCoordinate utm1 = EGIS.ShapeFileLib.ConversionFunctions.LLToUtm(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, pt[1], pt[0]);
                    EGIS.ShapeFileLib.UtmCoordinate utm2 = utm1;
                    utm2.Northing += 15;
                    EGIS.ShapeFileLib.LatLongCoordinate ll = EGIS.ShapeFileLib.ConversionFunctions.UtmToLL(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, utm2);
                    double penWidthScale = Math.Abs(ll.Latitude - pt[1]);
                    if (double.IsNaN(penWidthScale) || double.IsInfinity(penWidthScale))
                    {
                        sf.RenderSettings.PenWidthScale = -1;
                        sf.RenderSettings.MinPixelPenWidth = 1;
                    }
                    else
                    {
                        sf.RenderSettings.PenWidthScale = (float)penWidthScale;
                    }
                    
                }
            }
            catch
            {
                sf.RenderSettings.PenWidthScale = -1;
                sf.RenderSettings.MinPixelPenWidth = 1;
            }
        }

        private void RenderShapefiles()
        {
            if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0) return;

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
                this.OnPaintMapBackground(new PaintEventArgs(g, new Rectangle(0,0,this.ClientSize.Width, this.ClientSize.Height)));
                foreach (EGIS.ShapeFileLib.ShapeFile sf in myShapefiles)
                {
                    //this is an expensive operation
                    //using (ICoordinateTransformation coordinateTransformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sf.CoordinateReferenceSystem, MapCoordinateReferenceSystem))
                    //{
                    //}
                    //using (ICoordinateTransformation coordinateTransformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sf.CoordinateReferenceSystem, CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode)))
                    //{
                    //}
                    
                    sf.Render(g, screenBuf.Size, this._centrePoint, this._zoomLevel, this.projectionType, this.MapCoordinateReferenceSystem);
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
            Bitmap bm = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);

            if (screenBuf != null)
            {
                Graphics g = Graphics.FromImage(bm);
                try
                {
                    g.Clear(MapBackColor);
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
               // bool selecting = (InternalPanSelectMode != PanSelectMode.Pan);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                //if (InternalPanSelectMode == PanSelectMode.SelectRectangle)
                if ((InternalPanSelectMode == PanSelectMode.SelectRectangle) || (InternalPanSelectMode == PanSelectMode.ZoomRectangle))
                {
                    e.Graphics.DrawImage(screenBuf, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
                    using (Pen p = new Pen(Color.Red, 1))
                    {
                        using(Brush b = new SolidBrush(Color.FromArgb(20, Color.Red)))
                        {
                            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            
                            Rectangle selectRect = new Rectangle(MouseOffsetPoint.X>=0?MouseDownPoint.X:MouseDownPoint.X+MouseOffsetPoint.X,
                                MouseOffsetPoint.Y>=0?MouseDownPoint.Y:MouseDownPoint.Y+MouseOffsetPoint.Y,
                                MouseOffsetPoint.X>=0?MouseOffsetPoint.X:-MouseOffsetPoint.X,
                                MouseOffsetPoint.Y>=0?MouseOffsetPoint.Y:-MouseOffsetPoint.Y);
                            
                            e.Graphics.FillRectangle(b, selectRect);
                            e.Graphics.DrawRectangle(p, selectRect);

                            if (!MouseOffsetPoint.IsEmpty)
                            {
                                PointD p0 = PixelCoordToGisPoint(selectRect.Location);
                                PointD p1 = PixelCoordToGisPoint(new Point(selectRect.Right, selectRect.Top ));
                                PointD p2 = PixelCoordToGisPoint(new Point(selectRect.Left, selectRect.Bottom));

                                double dist1 = DistanceBetweenPoints(p0, p1);
                                double dist2 = DistanceBetweenPoints(p0, p2);

                                e.Graphics.DrawString(string.Format("{0:0.000}m", dist1), this.Font, Brushes.Red, new PointF(0.5F * (selectRect.Left + selectRect.Right), selectRect.Top));
                                e.Graphics.DrawString(string.Format("{0:0.000}m", dist2), this.Font, Brushes.Red, new PointF(selectRect.Left, 0.5F * (selectRect.Top + selectRect.Bottom)));

                            }

                        }
                    }
                }
                //else if (InternalPanSelectMode == PanSelectMode.SelectCircle)
                else if ((InternalPanSelectMode == PanSelectMode.SelectCircle) || (InternalPanSelectMode == PanSelectMode.ZoomCircle))
                {
                    e.Graphics.DrawImage(screenBuf, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
                    using (Pen p = new Pen(Color.Red, 1))
                    {
                        using (Brush b = new SolidBrush(Color.FromArgb(20, Color.Red)))
                        {
                            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                            int radius = (int)Math.Round(Math.Sqrt(MouseOffsetPoint.X * MouseOffsetPoint.X + MouseOffsetPoint.Y * MouseOffsetPoint.Y));
                            Rectangle selectRect = new Rectangle(MouseDownPoint.X -radius,
                                MouseDownPoint.Y -radius,
                                radius*2,
                                radius*2);

                            e.Graphics.FillEllipse(b, selectRect);
                            e.Graphics.DrawEllipse(p, selectRect);

                            if(!MouseOffsetPoint.IsEmpty)
                            {
                                PointD p0 = PixelCoordToGisPoint(MouseDownPoint);
                                PointD p1 = PixelCoordToGisPoint(new Point(MouseDownPoint.X + MouseOffsetPoint.X, MouseDownPoint.Y + MouseOffsetPoint.Y));
                                double dist = DistanceBetweenPoints(p0, p1);

                                e.Graphics.DrawLine(p, MouseDownPoint, new Point(MouseDownPoint.X + MouseOffsetPoint.X, MouseDownPoint.Y + MouseOffsetPoint.Y));
                                e.Graphics.DrawString(string.Format("{0:0.000}m", dist), this.Font, Brushes.Red, new PointF(0.5F * (MouseDownPoint.X + MouseDownPoint.X + MouseOffsetPoint.X), 0.5F * (MouseDownPoint.Y + MouseDownPoint.Y + MouseOffsetPoint.Y)));
                            }
                        }
                    }

                }
                else// if (InternalPanSelectMode == PanSelectMode.Pan)
                {
                    //change this to only draw invalid area
                    if ((MouseOffsetPoint.X == 0) && (MouseOffsetPoint.Y == 0))
                    {
                        e.Graphics.DrawImage(screenBuf, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
                    }
                    else
                    {
                        e.Graphics.DrawImage(screenBuf, MouseOffsetPoint.X, MouseOffsetPoint.Y);
                    }
                }
            }

            System.Drawing.Drawing2D.Matrix m = e.Graphics.Transform;
            try
            {
                if (InternalPanSelectMode == PanSelectMode.Pan)
                {
                    System.Drawing.Drawing2D.Matrix m2 = new System.Drawing.Drawing2D.Matrix();
                    m2.Translate(MouseOffsetPoint.X, MouseOffsetPoint.Y);
                    e.Graphics.Transform = m2;
                }                
                base.OnPaint(e);
            }
            finally
            {
                e.Graphics.Transform = m;
            }
        }

        /// <summary>
        /// override
        /// </summary>
        /// <param name="e"></param>
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

        /// <summary>
        /// Clears the map and calls Invalidate. 
        /// </summary>
        /// <remarks>
        /// This method performs the same functionality
        /// as Refresh(true) but can be called from background threads as it calls Invalidate rather
        /// than Refresh
        /// </remarks>
        public void InvalidateAndClearBackground()
        {            
            this.dirtyScreenBuf = true;
            Invalidate();
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

            CtrlKeyDown = (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey);
            ShiftKeyDown = (e.KeyCode == Keys.ShiftKey);
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
            CtrlKeyDown = false;
            ShiftKeyDown = false;
        }

        /// <summary>
        /// override 
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
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

        /// <summary>
        /// get/set current PanSelectMode
        /// </summary>
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

        private bool _ctrlDragToZoom = false;

        //get/set whether to zoom to selection if control key is down. If false default behaviour is to select records
        public bool ZoomToSelectedExtentWhenCtrlKeydown
        {
            get { return _ctrlDragToZoom; }
            set { _ctrlDragToZoom = value; }
        }

        private PanSelectMode _panSelectMode = PanSelectMode.Pan;

        private bool _ctrlDown = false;
        private bool _shiftDown = false;
        private bool _toggleSelect = false;

        private MouseButtons _mouseDownButton = MouseButtons.None;
        private Point _mouseDownPt = Point.Empty;
        private Point _mouseOffPt = new Point(0, 0);

        #region protected mouse handling properties

        protected bool CtrlKeyDown
        {
            get
            {
                return _ctrlDown;
            }
            set
            {
                _ctrlDown = value;
            }
        }

        protected bool ShiftKeyDown
        {
            get
            {
                return _shiftDown;
            }
            set
            {
                _shiftDown = value;
            }
        }

        protected bool ToggleSelect
        {
            get
            {
                return _toggleSelect;
            }
            set
            {
                _toggleSelect = value;
            }
        }

        protected MouseButtons MouseDownButton
        {
            get
            {
                return _mouseDownButton;
            }
            set
            {
                _mouseDownButton = value;
            }
        }

        protected Point MouseDownPoint
        {
            get
            {
                return _mouseDownPt;
            }
            set
            {
                _mouseDownPt = value;
            }
        }


        protected Point MouseOffsetPoint
        {
            get
            {
                return _mouseOffPt;
            }
            set
            {
                _mouseOffPt = value;
            }
        }

        /// <summary>
        /// InternalPanSelectMode is set by keys or controlPanSelectMode
        /// </summary>
        protected PanSelectMode InternalPanSelectMode
        {
            get
            {
                return _panSelectMode;
            }
            set
            {
                _panSelectMode = value;
            }
        }


        #endregion


        /// <summary>
        /// Handles the MouseDown event. Derived classes overriding this method should call base.OnMouseDown
        /// to ensure the SFMap control handles the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
               
            if (PanSelectMode == EGIS.Controls.PanSelectMode.None) return;

            MouseDownButton = e.Button;
            MouseDownPoint = new Point(e.X, e.Y);

            if (PanSelectMode != PanSelectMode.Pan)
            {
                InternalPanSelectMode = PanSelectMode;
                ToggleSelect = !ShiftKeyDown && CtrlKeyDown;
            }            
            else
            {
                if (ShiftKeyDown)
                {
                    InternalPanSelectMode = (e.Button == MouseButtons.Left) ? PanSelectMode.SelectRectangle : PanSelectMode.SelectCircle;
                    ToggleSelect = false;
                }
                else if (CtrlKeyDown)
                {
                    if (ZoomToSelectedExtentWhenCtrlKeydown)
                    {
                        InternalPanSelectMode = (e.Button == MouseButtons.Left) ? PanSelectMode.ZoomRectangle : PanSelectMode.ZoomCircle;
                        ToggleSelect = false;
                    }
                    else
                    {
                        InternalPanSelectMode = (e.Button == MouseButtons.Left) ? PanSelectMode.SelectRectangle : PanSelectMode.SelectCircle;
                        ToggleSelect = true;
                    }
                }
                else InternalPanSelectMode = PanSelectMode.Pan;
            }
        }

        /// <summary>
        /// Handles the MouseUp event. Derived classes overriding this method should call base.OnMouseUp
        /// to ensure the SFMap control raises the event correctly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (PanSelectMode == EGIS.Controls.PanSelectMode.None || MouseDownButton == MouseButtons.None) return;
            Cursor oldCursor = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                if (InternalPanSelectMode == PanSelectMode.ZoomRectangle)
                 {
                     PointD pt1 = PixelCoordToGisPoint(MouseDownPoint);
                     PointD pt2 = PixelCoordToGisPoint(e.X, e.Y);
                    RectangleD extent = RectangleD.FromLTRB(Math.Min(pt1.X, pt2.X),
                        Math.Min(pt1.Y, pt2.Y),
                        Math.Max(pt1.X, pt2.X),
                        Math.Max(pt1.Y, pt2.Y));
                    //if we've just clicked then expand the rectangle
                    if (Math.Abs(e.X - MouseDownPoint.X) < 2 && Math.Abs(e.Y - MouseDownPoint.Y) < 2)
                    {
                        extent.Inflate(4f / ZoomLevel, 4f / ZoomLevel);
                    }
                    FitToExtent(extent);
                }
                else if (InternalPanSelectMode == PanSelectMode.ZoomCircle)
                {

                    PointD pt1 = PixelCoordToGisPoint(MouseDownPoint);
                    PointD pt2 = PixelCoordToGisPoint(e.X, e.Y);
                    double radius = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));

                    //if we've just clicked then expand the radius
                    if (Math.Abs(e.X - MouseDownPoint.X) < 2 && Math.Abs(e.Y - MouseDownPoint.Y) < 2)
                    {
                        radius += (4f / ZoomLevel);
                    }

                    RectangleD extent = RectangleD.FromLTRB(
                        pt1.X - radius,
                        pt1.Y - radius,
                        pt1.X + radius,
                        pt1.Y + radius);
                    FitToExtent(extent);
                }
                else if (InternalPanSelectMode == PanSelectMode.SelectRectangle)
                {
                    PointD pt1 = PixelCoordToGisPoint(MouseDownPoint);
                    PointD pt2 = PixelCoordToGisPoint(e.X, e.Y);
                    RectangleD selRect = RectangleD.FromLTRB(Math.Min(pt1.X, pt2.X),
                        Math.Min(pt1.Y, pt2.Y),
                        Math.Max(pt1.X, pt2.X),
                        Math.Max(pt1.Y, pt2.Y));
                    //if we've just clicked then expand the rectangle
                    if (Math.Abs(e.X - MouseDownPoint.X) < 2 && Math.Abs(e.Y - MouseDownPoint.Y) < 2)
                    {
                        selRect.Inflate(4f / ZoomLevel, 4f / ZoomLevel);
                    }
                    bool fireEvent = false;
                    for (int n = 0; n < this.ShapeFileCount; ++n)
                    {
                        if (!myShapefiles[n].IsSelectable) continue;
                        fireEvent = true;
                        List<int> ind = new List<int>();
                        myShapefiles[n].GetShapeIndiciesIntersectingRect(ind, selRect, this.MapCoordinateReferenceSystem);

                        if (ToggleSelect)
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

                    MouseOffsetPoint = new Point(0, 0);

                    Refresh(true);

                    if(fireEvent) OnSelectedRecordChanged(new EventArgs());
                }
                else if (InternalPanSelectMode == PanSelectMode.SelectCircle)
                {

                    PointD pt1 = PixelCoordToGisPoint(MouseDownPoint);
                    PointD pt2 = PixelCoordToGisPoint(e.X, e.Y);
                    double radius = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));

                    //if we've just clicked then expand the radius
                    if (Math.Abs(e.X - MouseDownPoint.X) < 2 && Math.Abs(e.Y - MouseDownPoint.Y) < 2)
                    {
                        radius += (4f / ZoomLevel);
                    }
                    bool fireEvent = false;
                    for (int n = 0; n < this.ShapeFileCount; ++n)
                    {
                        if (!myShapefiles[n].IsSelectable) continue;
                        fireEvent = true;
                        
                        List<int> ind = new List<int>();
                        myShapefiles[n].GetShapeIndiciesIntersectingCircle(ind, pt1, radius, this.MapCoordinateReferenceSystem);

                        if (ToggleSelect)
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


                    MouseOffsetPoint = new Point(0, 0);

                    Refresh(true);

                    if(fireEvent) OnSelectedRecordChanged(new EventArgs());

                }
                else
                {
                    MouseOffsetPoint = new Point(e.X - MouseDownPoint.X, e.Y - MouseDownPoint.Y);
                    if (!MouseOffsetPoint.IsEmpty)
                    {
                        double s = 1d / ZoomLevel;
                        _centrePoint.X -= (s * MouseOffsetPoint.X);
                        _centrePoint.Y += (s * MouseOffsetPoint.Y);
                        MouseOffsetPoint = new Point(0, 0);
                        Refresh(true);
                    }
                }
            }
            finally
            {
                MouseDownButton = MouseButtons.None;
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
            if (PanSelectMode == EGIS.Controls.PanSelectMode.None) return;
            if (e.Button != MouseButtons.None && MouseDownButton != System.Windows.Forms.MouseButtons.None)
            {
                MouseOffsetPoint = new Point(e.X - MouseDownPoint.X, e.Y - MouseDownPoint.Y);
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
                //zoom in
                double z = ZoomLevel;
                double x = ((ClientSize.Width * 0.5) + e.X) * 0.5;
                double y = ((ClientSize.Height * 0.5) + e.Y) * 0.5;
                SetZoomAndCentre(z * 2d, PixelCoordToGisPoint((int)Math.Round(x), (int)Math.Round(y)));
            }
            else if (e.Delta < 0)
            {
                //zoom out
                double z = ZoomLevel;
                int x = ClientSize.Width - e.X;
                int y = ClientSize.Height - e.Y;
                SetZoomAndCentre(z * 0.5, PixelCoordToGisPoint(x, y));
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
            //PointF ptf = new PointF((float)pt.X, (float)pt.Y);
            for (int l = ShapeFileCount - 1; l >= 0; l--)
            {
                bool useToolTip = (_useHints && this[l].RenderSettings != null && this[l].RenderSettings.UseToolTip);
                bool useCustomToolTip = (useToolTip && this[l].RenderSettings.CustomRenderSettings != null && this[l].RenderSettings.CustomRenderSettings.UseCustomTooltips);
                //RectangleD layerExtent = this[l].Extent;//.GetActualExtent();
                //layerExtent.Inflate(delta, delta);
                if ((this[l].IsSelectable || useToolTip) /*&& layerExtent.Contains(pt)*/ && this[l].IsVisibleAtZoomLevel((float)ZoomLevel))
                {
                    int selectedIndex = this[l].GetShapeIndexContainingPoint(pt, delta, this.MapCoordinateReferenceSystem);
                    if (selectedIndex >= 0)
                    {
                        if (this[l].IsSelectable) Cursor = DefaultSelectionCursor;
                        if (_useHints)
                        {
                            if (useCustomToolTip)
                            {
                                string s = this[l].RenderSettings.CustomRenderSettings.GetRecordToolTip(selectedIndex);
                                if (!string.IsNullOrEmpty(s))
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
			Cursor = DefaultMapCursor;
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

            MapDoubleClickedEventArgs mapDoubleClickEventArgs = new MapDoubleClickedEventArgs();
            this.OnMapDoubleClick(mapDoubleClickEventArgs);
            if(mapDoubleClickEventArgs.Cancel) return;

            if (MouseDownButton == MouseButtons.Left)
            {
                //zoom in
                double z = ZoomLevel;
                double x = ((ClientSize.Width*0.5) + MouseDownPoint.X)*0.5;
                double y = ((ClientSize.Height*0.5) + MouseDownPoint.Y)*0.5;
                SetZoomAndCentre(z*2d, PixelCoordToGisPoint((int)Math.Round(x), (int)Math.Round(y)));               
            }
            else if (MouseDownButton == MouseButtons.Right)
            {
                //zoom out
                double z = ZoomLevel;
                double x = ClientSize.Width - MouseDownPoint.X;
                double y = ClientSize.Height - MouseDownPoint.Y;
                SetZoomAndCentre(z * 0.5, PixelCoordToGisPoint((int)Math.Round(x), (int)Math.Round(y)));
            }
        }

        #endregion


        #region distance calculations

        private double DistanceBetweenPoints(PointD p0, PointD p1)
        {
            double distance = double.NaN;
            if ((this.MapCoordinateReferenceSystem as IGeographicCRS) != null)
            {
                distance = EGIS.ShapeFileLib.ConversionFunctions.DistanceBetweenLatLongPointsHaversine(ConversionFunctions.Wgs84RefEllipse, p0.Y, p0.X, p1.Y, p1.X);
                
            }
            else if ((this.MapCoordinateReferenceSystem as IProjectedCRS) != null)
            {
                //calculate euclidean distance
                distance = Math.Sqrt((p1.X - p0.X) * (p1.X - p0.X) + (p1.Y - p0.Y) * (p1.Y - p0.Y));
            }
            return distance;
        }

        #endregion

    }
}