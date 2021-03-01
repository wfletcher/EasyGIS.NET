#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2019 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.ShapeFileLib class library of Easy GIS .NET.
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
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Xml;
using System.Security.Permissions;
using EGIS.Projections;

[assembly: CLSCompliant(true)]
//give the EGIS.Controls access to the internal methods
//[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EGIS.Controls,     PublicKey=0024000004800000940000000602000000240000525341310004000001000100ad413f7f4a7f27fbb045d205cfc65fe64665694533fc72b0d82433368f98f7bd82c18b98ee2f5fe417ed1427a9e6ff84e5dce034638bb7761ea22c9881b8fa09ac621ad78ebb3002b3dbb876f479fa0b2bccd95fc1d54c7fc87b5dc084d575fb304387c9bbd4ce6a5bf91328ae3ecc3f5472a14ce8e572d7d01d01483fe1f2d0")]
//[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EGIS.Web.Controls, PublicKey=0024000004800000940000000602000000240000525341310004000001000100ad413f7f4a7f27fbb045d205cfc65fe64665694533fc72b0d82433368f98f7bd82c18b98ee2f5fe417ed1427a9e6ff84e5dce034638bb7761ea22c9881b8fa09ac621ad78ebb3002b3dbb876f479fa0b2bccd95fc1d54c7fc87b5dc084d575fb304387c9bbd4ce6a5bf91328ae3ecc3f5472a14ce8e572d7d01d01483fe1f2d0")]
namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Enumeration to indicate render quality.
    /// </summary>
    public enum RenderQuality { 
        /// <summary>
        /// Rendering is performed favouring speed over quality
        /// </summary>
        Low,
        /// <summary>
        /// Rendering is performed favouring quality over speed
        /// </summary>
        High,
        /// <summary>
        /// Rendering quality is adjusted automatically. In most cases this is the preferred choice
        /// </summary>        
        Auto };


    


	/// <summary>    
	/// .NET ShapeFile class used to load, render and query data in an ESRI Shape File.
    /// The ShapeFile class is the main class of the EGIS.ShapeFileLib namespace    
	/// </summary>
    /// <remarks>    
    /// The class is designed to open very large shapefiles, providing a low memory implementation which will not load the contents of the
    /// shapefile in memory.    
    /// <para>The class also provides an option to map the shapefile in memory using native file mapping, which will provide faster
    /// rendering speed</para>
    /// <para>
    /// NOTE THAT MANY OF THE METHODS IN THIS CLASS ARE NOT THREAD SAFE (due to internal shared buffers). If multiple threads require access to ShapeFile objects
    /// then the static (Shared in VB) Sync object should be used to provide mutual access to the shapefiles
    /// </para>
    /// <para>
    /// An ESRI Shape File consists of 3 files - the ShapeFile index file (.shx), the shape file (.shp) and a DBF file (.dbf). For a more detailed
    /// desription of the ShapeFile format consult the ESRI ShapeFile file specification.</para>
    /// <para>
    /// Currently supported Shape Types are Point, PointZ, PolyLine, PolyLineM, Polygon and PolygonZ</para>   
    /// </remarks>        
    public sealed class ShapeFile : IDisposable
	{
        
		const int MAIN_HEADER_LENGTH = 100;
      
        private string filePath;				
		private RenderSettings myRenderer;

        //2.6
        private ShapeFileMainHeader mainHeader;
        private SFRecordCol sfRecordCol;
        private Stream shapeFileStream;

		private DbfReader dbfReader;
        

        private static RenderQuality renderQuality = RenderQuality.Auto;

        #region public methods and properties
        /// <summary>
        /// Gets or Sets the RenderQuality to use when rendering shapefiles
        /// </summary>
        public static RenderQuality RenderQuality
        {
            get
            {
                return renderQuality;
            }
            set
            {
                renderQuality = value;                
            }
        }

        /// <summary>
        /// Gets or Sets whether to map files in memory.        
        /// </summary>
        /// <remarks>
        /// If MapFilesInMemory is set to true then shapefiles will be mapped in memory using native file mapping.
        /// When files are mapped in memory rendering performance will be improved; however they will use more
        /// memory.
        /// </remarks>
        public static bool MapFilesInMemory
        {
            get
            {
                return SFRecordCol.MapFilesInMemory;
            }
            set
            {
                SFRecordCol.MapFilesInMemory = value;
            }
        }

        /// <summary>
        /// Static property to get/set maximum levels used by internal quad tree data structure
        /// </summary>
        /// <remarks>
        /// <para>
        /// Internally a quad tree data structure is used as an in memory spatial index to speed up methods
        /// to find the closest shape or intersecting records. Use this method to control the maximum number of levels used 
        /// in the quad tree.
        /// </para>
        /// <para>
        /// Setting a higher level will generally produde increased performance, but will use more memory and take longer
        /// to create the quad tree.
        /// </para>
        /// <para>
        /// The default number of levels is 8.
        /// </para>
        /// </remarks>
        public static int QuadTreeMaxLevels
        {
            get
            {
                return QTNode.MaxLevels;
            }
            set
            {
                QTNode.MaxLevels = value;
            }            
        }

        
        /// <summary>
        /// Gets the ShapeType of the ShapeFile
        /// 
        /// </summary>
        public ShapeType ShapeType
        {
            get
            {
                if (sfRecordCol != null) return sfRecordCol.MainHeader.ShapeType;
                return ShapeType.NullShape;
            }
        }

        private static object syncObj = new object();

        /// <summary>
        /// Gets an object that can be used to synchronize access to the ShapeFile
        /// </summary>
        /// <remarks>
        /// NOTE: Many of the methods in this class are not Thread Safe (due to internal shared buffers). If multiple threads require access to ShapeFile objects
        /// then the static (Shared in VB) Sync object should be used to provide mutual access to the shapefiles
        /// </remarks>
        public static object Sync
        {
            get
            {
                return syncObj;
            }
        }

       
        /// <summary>
        /// Gets or sets the RenderSettings used by the ShapeFile when Rendering
        /// </summary>
		public RenderSettings RenderSettings
		{
			get
			{
				return myRenderer;
			}
			set
			{
				myRenderer = value;
			}
		}

        private string myName = "";

        /// <summary>
        /// Gets the path to the ShapeFile, not including the .shp file extension
        /// </summary>
        public string FilePath
        {
            get
            {
                return filePath;
            }
        }

        /// <summary>
        /// The name of this ShapeFile.
        /// </summary>
        /// <remarks>
        /// This is common name used to identify a shapefile and is not the name
        /// of the file path of this shapefile
        /// </remarks>        
        public string Name
        {
            get
            {
                return myName;
            }
            set
            {
                myName = value;
            }
        }

        private string projectionWkt;

        /// <summary>
        /// The WKT representation of the ShapeFile's Coordinate Reference System as read from the .prj file
        /// </summary>
        public string ProjectionWKT
        {
            get
            {
                return this.projectionWkt;
            }
            private set
            {
                this.projectionWkt = value;
                if (!string.IsNullOrEmpty(value))
                {
                    this.CoordinateReferenceSystem = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCRSFromWKT(value);                   
                }
                else
                {
                    CoordinateReferenceSystem = null;
                }
            }
        }

        /// <summary>
        /// The Coordinate Reference System of the ShapeFile as read from the .prj file WKT
        /// </summary>
        public ICRS CoordinateReferenceSystem
        {
            get;
            private set;
        }

        public static RectangleD LLExtentToProjectedExtent(RectangleD rectLL, ProjectionType projectionType)
        {
            switch(projectionType)
            {
                case ProjectionType.None:
                    return rectLL; 
                case ProjectionType.Mercator:
                    PointD tl = SFRecordCol.LLToProjection(new PointD(rectLL.Left, rectLL.Top));
                    PointD br = SFRecordCol.LLToProjection(new PointD(rectLL.Right,rectLL.Bottom));
                    return RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                
                default:
                    throw new NotSupportedException("Unknown or unsupported ProjectionType");
            }
            
        }

        /// <summary>
        /// Gets the Rectangular extent of the shapefile
        /// </summary>
       	public RectangleD Extent
		{
			get
			{
                if (sfRecordCol != null)
                {                    
                    RectangleD r = RectangleD.FromLTRB(sfRecordCol.MainHeader.Xmin, sfRecordCol.MainHeader.Ymin, sfRecordCol.MainHeader.Xmax, sfRecordCol.MainHeader.Ymax);                    
                    return r;                    
                }
				else
				{
					return RectangleD.Empty;
				}
			}
		}
       

        /// <summary>
        /// Returns a projected point to its Lat Long equivalent 
        /// </summary>
        /// <remarks>
        /// If UseMercatorProjection is true then returned point will be transformed
        /// to Lat Long.
        /// <para>If UseMercatorProjection is false then projPoint is returned</para>
        /// </remarks>
        /// <param name="projPoint"></param>
        /// <returns></returns>
        public static PointD ProjectionToLatLong(PointD projPoint)
        {
            return SFRecordCol.ProjectionToLL(projPoint);            
        }

        /// <summary>
        /// Transforms a point using MercatorProjection
        /// </summary>
        /// <remarks>
        /// If UseMercatorProjection is true then the returned point is transformed using MercatorProjection
        /// <para>If UseMercatorProjection is false then latLong will be returned (no transformation is performed)</para>
        /// </remarks>
        /// <param name="latLong"></param>
        /// <returns></returns>
        public static PointD LatLongToProjection(PointD latLong)
        {
           
            return SFRecordCol.LLToProjection(latLong);
        }

        private ICRS wgs84Crs = null;

        /// <summary>
        /// Convert source extent to WGS84 extent
        /// </summary>
        /// <param name="sourceExtent"></param>
        /// <param name="source"></param>
        /// <param name="ensure"></param>
        /// <returns></returns>
        /// <remarks>
        /// For an area of interest crossing the anti-meridian, where west_lon_degree will be greater than east_lon_degree
        /// 360 degrees is added to the returned extent.Right to ensure the rectangle width is not negative
        /// </remarks>
        public RectangleD ConvertExtentToWgs84(RectangleD sourceExtent, ICRS source, bool ensureWidthPositive=true)
        {
            if(wgs84Crs == null) wgs84Crs = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
            ICRS target = wgs84Crs;
            
            //RectangleD extent = ConvertExtent(sourceExtent, source, target);
            RectangleD extent = sourceExtent.Transform(source, target);
            if (ensureWidthPositive && extent.Right < extent.Left)
            {
                extent = RectangleD.FromLTRB(extent.Left, extent.Top, extent.Right + 360, extent.Bottom);
            }

            return extent;
        }

        
        /// <summary>
        /// Gets the rectangular extent of each shape in the shapefile in double precision form
        /// </summary>
        /// <returns>RectangleD[] representing the extent of each shape in the shapefile</returns>
        public RectangleD[] GetShapeExtentsD()
        {            
            RectangleD[] extents = new RectangleD[sfRecordCol.RecordHeaders.Length];
            int index = 0;
            int numRecords = sfRecordCol.RecordHeaders.Length;
            while (index < numRecords)
            {
                extents[index] = sfRecordCol.GetRecordBoundsD(index, shapeFileStream);
                ++index;
            }
            return extents;        
        }

        /// <summary>
        /// Indicates whether the ShapeFile is selectable. 
        /// </summary>
        /// <remarks>For a ShapeFile to be selectable, the ShapeFile's RenderSettings must not be null
        /// and the RenderSettings must be selectable</remarks>
        public bool IsSelectable
        {
            get
            {
                return (myRenderer != null) && myRenderer.IsSelectable;
            }
        }

        /// <summary>
        /// Checks whether the ShapeFile will be visible if rendered at given zoom level, as determined by the
        /// ShapeFile RenderSettings
        /// </summary>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public bool IsVisibleAtZoomLevel(float zoom)
        {
            if (myRenderer == null ) return true;
            bool ok = !(zoom < myRenderer.MinZoomLevel || (myRenderer.MaxZoomLevel > 0 && zoom > myRenderer.MaxZoomLevel));           
            return ok;
        }
        
        /// <summary>
        /// returns whether a shapefile record is currently selected
        /// </summary>
        /// <param name="index">zero based index of the shapefile record to query</param>
        /// <returns></returns>
        public bool IsRecordSelected(int index)
        {
            if (sfRecordCol != null) return sfRecordCol.IsRecordSelected(index);
            return false;
        }

        /// <summary>
        /// Selects or de-selects a shapefile record.
        /// </summary>
        /// <remarks>
        /// <para>Selected shapefiles will be rednered in the color specified by the RenderSettings SelectedFillColor and SelectedOutlineColor</para>
        /// </remarks>
        /// <param name="index">zero based index of the record to select or de-select</param>
        /// <param name="selected"></param>
        public void SelectRecord(int index, bool selected)
        {
            if (sfRecordCol != null) sfRecordCol.SelectRecord(index, selected);
        }

        /// <summary>
        /// Clears any selected shapefile records
        /// </summary>
        public void ClearSelectedRecords()
        {
            if (sfRecordCol != null) sfRecordCol.ClearSelectedRecords();
        }

        /// <summary>
        /// Returns a collection of all the selected record indices
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<int> SelectedRecordIndices
        {
            get
            {
                List<int> selectedRecords = new List<int>();
                int numRecs = RecordCount;
                for (int n = 0; n < numRecs; ++n)
                {
                    if (sfRecordCol.IsRecordSelected(n)) selectedRecords.Add(n);
                }
                return new System.Collections.ObjectModel.ReadOnlyCollection<int>(selectedRecords);
            }
        }


        #region record visibility

        /// <summary>
        /// returns whether a shapefile record is currently visible
        /// </summary>
        /// <param name="index">zero based index of the shapefile record to query</param>
        /// <returns></returns>
        /// <remarks>
        /// This method returns the Visible of an individual record. If the entire shapefile is not visible
        /// due to RenderSettings this will still return true if the given record is visible 
        /// </remarks>
        public bool IsRecordVisible(int index)
        {
            if (sfRecordCol != null) return sfRecordCol.IsRecordVisible(index);
            return false;
        }

        /// <summary>
        /// Sets the visibility of a shapefile record.
        /// </summary>
        /// <param name="index">zero based index of the record to select or de-select</param>
        /// <param name="visible"></param>
        public void SetRecordVisibility(int index, bool visible)
        {
            if (sfRecordCol != null) sfRecordCol.SetRecordVisible(index, visible);
        }

        /// <summary>
        /// Sets all shapefile records to be visible
        /// </summary>
        public void ClearRecordsVisibility()
        {
            if (sfRecordCol != null) sfRecordCol.ClearRecordsVisibility();
        }

        /// <summary>
        /// Returns a collection of all the visible record indices
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<int> VisibleRecordIndices
        {
            get
            {
                List<int> visibleRecords = new List<int>();
                int numRecs = RecordCount;
                for (int n = 0; n < numRecs; ++n)
                {
                    if (sfRecordCol.IsRecordVisible(n)) visibleRecords.Add(n);
                }
                return new System.Collections.ObjectModel.ReadOnlyCollection<int>(visibleRecords);
            }
        }

        #endregion


        /// <summary>
        /// Gets the number of records(shapes) in the ShapeFile
        /// </summary>
        public int RecordCount
        {
            get
            {
                return sfRecordCol.RecordHeaders.Length;
            }
        }

        /// <summary>
        /// Gets all of the records at fieldIndex in the DBF file
        /// </summary>
        /// <param name="fieldIndex">zero based index of the required field</param>
        /// <returns></returns>
        public string[] GetRecords(int fieldIndex)
        {
            if (myRenderer == null) return null;
            return myRenderer.DbfReader.GetRecords(fieldIndex);
        }

        /// <summary>
        /// Gets all of the Attribute Names contained in the shapefile's DBF file
        /// </summary>        
        /// <returns></returns>
        public string[] GetAttributeFieldNames()
        {
            if (myRenderer == null) return null;
            return myRenderer.DbfReader.GetFieldNames();
        }

        /// <summary>
        /// Gets all of the Attribute values contained in the shapefile's DBF file        
        /// </summary>
        /// <param name="recordNumber">zero based index of the required record/shape</param>
        /// <returns></returns>
        public string[] GetAttributeFieldValues(int recordNumber)
        {
            if (myRenderer == null) return null;
            return myRenderer.DbfReader.GetFields(recordNumber);
        }

		/// <summary>
		/// returns a DbfReader to read the shapefile's dbf record attributes file
		/// </summary>
		public DbfReader DbfReader
		{
			get
			{
				return this.dbfReader;
			}
		}

		/// <summary>
		/// property to tag a ShapeFile with a generic object.
		/// </summary>
		public object Tag
		{
			get;
			set;
		}

#endregion

        #region Render Methods

        //void Render(Graphics g, Size clientArea, RectangleF extent, ProjectionType projectionType)
        //{
        //    Render(g, clientArea, extent, RenderSettings, projectionType);
        //}

        void Render(Graphics g, Size clientArea, RectangleD extent, RenderSettings renderSettings, ProjectionType projectionType)
        {
            RectangleD thisExtent = ShapeFile.LLExtentToProjectedExtent(Extent, projectionType);
            if (!extent.IntersectsWith(ShapeFile.LLExtentToProjectedExtent(Extent, projectionType))) return;
            if (sfRecordCol != null)
            {
                sfRecordCol.paint(g, clientArea, extent, shapeFileStream, RenderSettings, projectionType);
            }
        }

        void Render(Graphics g, Size clientArea, RectangleD extent, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation)
        {
            //convert extent from the target coordinates to the shapefile coordinates
            //using (ICoordinateTransformation invTransformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS))
            {
                //covert the target extent and the shapefile's extent to geographic WGS84 and test if they intersect
                if (false)
                {
                    if (wgs84Crs == null) wgs84Crs = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);

                    RectangleD targetWgs84 = ConvertExtentToWgs84(extent, coordinateTransformation.TargetCRS);

                    RectangleD shapeFileExtentWgs84 = ConvertExtentToWgs84(this.Extent, this.CoordinateReferenceSystem);

                    if (!shapeFileExtentWgs84.IntersectsWith(targetWgs84)) return;

                    RectangleD intersectingExtent = RectangleD.Intersect(targetWgs84, shapeFileExtentWgs84);
                    //convert the intersecting geographic coordinates to the shapefile coordinates

                    RectangleD r = intersectingExtent.Transform(wgs84Crs, this.CoordinateReferenceSystem);
                }

                //extent.Transform(coordinateTransformation );
                RectangleD shapeFileExtent = coordinateTransformation.Transform(extent, TransformDirection.Inverse);
                if (shapeFileExtent.IsValidExtent() && this.Extent.IsValidExtent())
                {
                    shapeFileExtent = RectangleD.Intersect(Extent, shapeFileExtent);
                }

                if (sfRecordCol != null)
                {
                    //sfRecordCol.paint(g, clientArea, r, shapeFileStream, RenderSettings, projectionType, coordinateTransformation, extent);
                    sfRecordCol.paint(g, clientArea, shapeFileExtent, shapeFileStream, RenderSettings, projectionType, coordinateTransformation, extent);
                }

                //double[] pts = new double[8];
                //RectangleD r = extent;
                //pts[0] = r.Left; pts[1] = r.Bottom;
                //pts[2] = r.Right; pts[3] = r.Bottom;
                //pts[4] = r.Right; pts[5] = r.Top;
                //pts[6] = r.Left; pts[7] = r.Top;
                ////invTransformation.Transform(pts, 4);
                //coordinateTransformation.Transform(pts, 4, TransformDirection.Inverse);
                //r = RectangleD.FromLTRB(Math.Min(pts[0], pts[6]),
                //    Math.Min(pts[5], pts[7]), Math.Max(pts[2], pts[4]),
                //    Math.Max(pts[1], pts[3]));

                //if (!r.IntersectsWith(ShapeFile.LLExtentToProjectedExtent(Extent, projectionType))) return;
                //if (sfRecordCol != null)
                //{
                //    sfRecordCol.paint(g, clientArea, r, shapeFileStream, RenderSettings, projectionType, coordinateTransformation, extent);
                //}
            }
        }



        /// <summary>
        /// Renders the shapefile centered at given point and zoom
        /// </summary>
        /// <param name="graphics">The Graphics device to render to</param>
        /// <param name="clientArea">The client area in pixels to draw </param>
        /// <param name="centre">The centre point in the ShapeFiles coordinates</param>
        /// <param name="zoom">The scaling to apply</param>
        /// <param name="renderSetings">render settings to apply when rendering the shapefile</param>
        /// <remarks>
        /// If zoom is 1 and the width of the ShapeFile's extent is N units wide, then the
        /// ShapeFile wil be rendered N pixels wide. If zoom is 2 then shapefile will be rendered 2N pixels wide
        /// </remarks>
        public void Render(Graphics graphics, Size clientArea, PointF centre, float zoom, RenderSettings renderSetings, ProjectionType projectionType)
        {
            if (!IsVisibleAtZoomLevel(zoom))
            {
                return;
            }
            if (zoom <= float.Epsilon) throw new ArgumentException("zoom can not be <= zero");
            zoom = 1f / zoom;
            float sx = clientArea.Width*zoom;
            float sy = clientArea.Height*zoom;
            RectangleF r = RectangleF.FromLTRB(centre.X - (sx *0.5f), centre.Y - (sy * 0.5f), centre.X + (sx *0.5f), centre.Y + (sy *0.5f));
            Render(graphics, clientArea, r, renderSetings, projectionType);            
        }

               
        /// <summary>
        /// Renders the shapefile centered at given point and zoom
        /// </summary>
        /// <param name="graphics">The Graphics device to render to</param>
        /// <param name="clientArea">The client area in pixels to draw </param>
        /// <param name="centre">The centre point in the ShapeFiles coordinates</param>
        /// <param name="zoom">The scaling to apply</param>
        /// <param name="projectionType">The Map Projection to use when rendering the shapefile</param>
        /// <remarks>
        /// If zoom is 1 and the width of the ShapeFile's extent is N units wide, then the
        /// ShapeFile wil be rendered N pixels wide. If zoom is 2 then shapefile will be rendered 2N pixels wide
        /// </remarks>
        public void Render(Graphics graphics, Size clientArea, PointD centre, double zoom, ProjectionType projectionType)
        {
            this.RenderInternal(graphics, clientArea, centre, zoom, this.RenderSettings, projectionType);
        }

        /// <summary>
        /// Renders the shapefile centered at given point and zoom
        /// </summary>
        /// <param name="graphics">The Graphics device to render to</param>
        /// <param name="clientArea">The client area in pixels to draw </param>
        /// <param name="centre">The centre point in the ShapeFiles coordinates</param>
        /// <param name="zoom">The scaling to apply</param>
        /// <param name="projectionType">The Map Projection to use when rendering the shapefile</param>
        /// <param name="targetCRS"></param>
        /// <remarks>
        /// If zoom is 1 and the width of the ShapeFile's extent is N units wide, then the
        /// ShapeFile wil be rendered N pixels wide. If zoom is 2 then shapefile will be rendered 2N pixels wide
        /// </remarks>
        public void Render(Graphics graphics, Size clientArea, PointD centre, double zoom, ProjectionType projectionType, ICRS targetCRS)
        {
            if(targetCRS == null || this.CoordinateReferenceSystem == null ||  this.CoordinateReferenceSystem.IsEquivalent(targetCRS))
            {
                //either target or this CRS is null or they are equivalent
                this.RenderInternal(graphics, clientArea, centre, zoom, this.RenderSettings, projectionType);
            }
            else
            {
                using (ICoordinateTransformation coordinateTransformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(this.CoordinateReferenceSystem, targetCRS))
                {
                    this.RenderInternal(graphics, clientArea, centre, zoom, this.RenderSettings, projectionType, coordinateTransformation);
                }
            }
        }


        internal void RenderInternal(Graphics graphics, Size clientArea, PointF centre, float zoom, RenderSettings renderSetings, ProjectionType projectionType)
        {
            if (!IsVisibleAtZoomLevel(zoom))
            {
                return;
            }
            if (zoom <= float.Epsilon) throw new ArgumentException("zoom can not be <= zero");
            zoom = 1f / zoom;
            float sx = clientArea.Width * zoom;
            float sy = clientArea.Height * zoom;
            RectangleF r = RectangleF.FromLTRB(centre.X - (sx * 0.5f), centre.Y - (sy * 0.5f), centre.X + (sx * 0.5f), centre.Y + (sy * 0.5f));
            Render(graphics, clientArea, r, renderSetings, projectionType);
        }

        internal void RenderInternal(Graphics graphics, Size clientArea, PointD centre, double zoom, RenderSettings renderSetings, ProjectionType projectionType)
        {
            if (!IsVisibleAtZoomLevel((float)zoom))
            {
                return;
            }
            if (zoom <= double.Epsilon) throw new ArgumentException("zoom can not be <= zero");
            zoom = 1d / zoom;
            double sx = clientArea.Width * zoom;
            double sy = clientArea.Height * zoom;
            RectangleD r = RectangleD.FromLTRB((centre.X - (sx * 0.5f)), (centre.Y - (sy * 0.5f)), (centre.X + (sx * 0.5f)), (centre.Y + (sy * 0.5f)));
            Render(graphics, clientArea, r, renderSetings, projectionType);
        }

        internal void RenderInternal(Graphics graphics, Size clientArea, PointD centre, double zoom, RenderSettings renderSetings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation)
        {
            if (!IsVisibleAtZoomLevel((float)zoom))
            {
                return;
            }
            if (zoom <= double.Epsilon) throw new ArgumentException("zoom can not be <= zero");
            zoom = 1d / zoom;
            double sx = clientArea.Width * zoom;
            double sy = clientArea.Height * zoom;
            RectangleD r = RectangleD.FromLTRB((centre.X - (sx * 0.5)), (centre.Y - (sy * 0.5)), (centre.X + (sx * 0.5)), (centre.Y + (sy * 0.5)));
            Render(graphics, clientArea, r, renderSetings, projectionType, coordinateTransformation);
        }


        #endregion

        #region Constructors

        /// <summary>
        /// Empty constructor. 
        /// </summary>
        /// <remarks>If a ShapeFile is constructed using the empty constructor then it should be followed by calling Load or ReadXml</remarks>
        public ShapeFile()
		{
									
		}

        /// <summary>
        /// Constructs a ShapeFile using a path to a .shp shape file
        /// </summary>
        /// <param name="shapeFilePath">The path to the ".shp" shape file</param>
        public ShapeFile(string shapeFilePath)
            : this(shapeFilePath, false)
        {
            
        }

        /// <summary>
        /// Constructs a ShapeFile using a path to a .shp shape file
        /// </summary>
        /// <param name="shapeFilePath">The path to the ".shp" shape file</param>
        /// <param name="useMemoryStreams">Whether to load shp and dbf into a MemoryStream</param>
        /// <remarks>
        /// <para>
        /// Setting useMemoryStreams to true can improve performance if reading records
        /// multiple times in scenarios such as linear referencing
        /// </para>
        /// </remarks>
        public ShapeFile(string shapeFilePath, bool useMemoryStreams)
        {
            LoadFromFile(shapeFilePath, useMemoryStreams);
        }

        /// <summary>
        /// Constructs a ShapeFile using individual streams for the Shapefiles .shx, .shp, .dbf and .prj files
        /// </summary>
        /// <param name="shxStream">stream opened from the shx file</param>
        /// <param name="shpStream">stream opened from the shp file</param>
        /// <param name="dbfStream">stream opened from the dbf file</param>
        /// <param name="prjStream">stream opened from the prj file. If the prjStream is null then the shapefile will not use the 
        /// projection information and no CRS transformations will be performed</param>
        public ShapeFile(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream)
        {
            LoadFromFile(shxStream, shpStream, dbfStream, prjStream);
        }

        #endregion


        private RecordHeader[] LoadIndexfile(string path)
        {
            //read record headers from the index file            
            using (var shxStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return LoadIndexfile(shxStream);
            }
        }

        private RecordHeader[] LoadIndexfile(System.IO.Stream shxStream)
        {
            //read record headers from the index file
            RecordHeader[] recordHeaders = null;
            shxStream.Seek(0, SeekOrigin.Begin);
            using (System.IO.BinaryReader bReader = new BinaryReader(shxStream))                
            {
                this.mainHeader = new ShapeFileMainHeader(bReader.ReadBytes(100));
                int totalRecords = (mainHeader.FileLength - 100) >> 3;
                recordHeaders = new RecordHeader[totalRecords];
                int numRecs = 0;
                //now read the record headers
                byte[] data = new byte[mainHeader.FileLength - 100];
                bReader.Read(data, 0, data.Length);
                while (numRecs < totalRecords)
                {
                    RecordHeader recHead = new RecordHeader(numRecs + 1);
                    recHead.readFromIndexFile(data, numRecs << 3);
                    recordHeaders[numRecs++] = recHead;
                }
                data = null;
            }                
            return recordHeaders;
        }


        private RenderSettings CreateRenderSettings(string fieldName)
        {
            RectangleF r = this.Extent;
			this.dbfReader = new DbfReader(this.filePath);
            RenderSettings renderSettings = new RenderSettings(this.dbfReader, fieldName, new Font("Arial", 10)); 
            //create the PenWidthScale to be approx 15m
            if (r.Top > 90 || r.Bottom < -90)
            {
                //assume UTM
                renderSettings.PenWidthScale = 15;
            }
            else
            {
                PointF pt = new PointF(r.Left + r.Width / 2, r.Top + r.Height / 2);
                EGIS.ShapeFileLib.UtmCoordinate utm1 = EGIS.ShapeFileLib.ConversionFunctions.LLToUtm(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, pt.Y, pt.X);
                EGIS.ShapeFileLib.UtmCoordinate utm2 = utm1;
                utm2.Northing += 15;
                EGIS.ShapeFileLib.LatLongCoordinate ll = EGIS.ShapeFileLib.ConversionFunctions.UtmToLL(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, utm2);
                renderSettings.PenWidthScale = (float)Math.Abs(ll.Latitude - pt.Y);
            }
            return renderSettings;
        }

        private RenderSettings CreateRenderSettings(string fieldName, System.IO.Stream dbfStream)
        {
            RectangleF r = this.Extent;
			this.dbfReader = new DbfReader(dbfStream);
            RenderSettings renderSettings = new RenderSettings(this.dbfReader, fieldName, new Font("Arial", 10));
            //create the PenWidthScale to be approx 15m
            if (r.Top > 90 || r.Bottom < -90)
            {
                //assume UTM
                renderSettings.PenWidthScale = 15;
            }
            else
            {
                PointF pt = new PointF(r.Left + r.Width / 2, r.Top + r.Height / 2);
                EGIS.ShapeFileLib.UtmCoordinate utm1 = EGIS.ShapeFileLib.ConversionFunctions.LLToUtm(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, pt.Y, pt.X);
                EGIS.ShapeFileLib.UtmCoordinate utm2 = utm1;
                utm2.Northing += 15;
                EGIS.ShapeFileLib.LatLongCoordinate ll = EGIS.ShapeFileLib.ConversionFunctions.UtmToLL(EGIS.ShapeFileLib.ConversionFunctions.RefEllipse, utm2);
                renderSettings.PenWidthScale = (float)Math.Abs(ll.Latitude - pt.Y);
            }
            return renderSettings;
        }

        /// <summary>
        /// Loads a ShapeFile using a path to a .shp shape file
        /// </summary>
        /// <param name="shapeFilePath">The path to the ".shp" shape file</param>
        /// <param name="useMemoryStreams">Whether to load shp and dbf into a MemoryStream</param>
        /// <remarks>
        /// <para>
        /// Setting useMemoryStreams to true can improve performance if reading records
        /// multiple times in scenarios such as linear referencing
        /// </para>
        /// </remarks>
        private void LoadFromFile2(string shapeFilePath, bool useMemoryStreams)
        {
            if (shapeFilePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                shapeFilePath = shapeFilePath.Substring(0, shapeFilePath.Length - 4);
            }
            this.filePath = shapeFilePath;

            DateTime start = DateTime.Now;

            RecordHeader[] recordHeaders=LoadIndexfile(shapeFilePath+".shx");

            //Console.Out.WriteLine("Time to load index file: " + ((TimeSpan)DateTime.Now.Subtract(start)).TotalMilliseconds);

            //open the main file and adjust the mainheader file length
            if (useMemoryStreams)
            {
                this.shapeFileStream = new MemoryStream();
                using (var fs = new FileStream(shapeFilePath + ".shp", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.CopyTo(this.shapeFileStream);
                    this.shapeFileStream.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                this.shapeFileStream = new FileStream(shapeFilePath + ".shp", FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            this.mainHeader.FileLength = (int)shapeFileStream.Length;            

            switch (mainHeader.ShapeType)
            {
                case ShapeType.Point:
                    this.sfRecordCol = new SFPointCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.Polygon:
                    this.sfRecordCol = new SFPolygonCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolyLine:
                    this.sfRecordCol = new SFPolyLineCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolyLineM:
                    this.sfRecordCol = new SFPolyLineMCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolygonZ:
                    this.sfRecordCol = new SFPolygonZCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PointZ:
                    this.sfRecordCol = new SFPointZCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolyLineZ:
                    this.sfRecordCol = new SFPolyLineZCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.MultiPoint:
                    this.sfRecordCol = new SFMultiPointCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.MultiPointZ:
                    this.sfRecordCol = new SFMultiPointZCol(recordHeaders, ref mainHeader);
                    break;
                default:
                    this.Close();
                    throw new NotSupportedException("ShapeType: " + mainHeader.ShapeType + " not supported");
                    
            }
           
            if (double.IsInfinity(this.mainHeader.Xmax) || double.IsInfinity(this.mainHeader.Ymax))
            {
                FixHeaderRecordBounds();                
            }
            
                        
            //DateTime end = DateTime.Now;
            //System.Diagnostics.Debug.WriteLine("Shape Type : " + this.mainHeader.ShapeType);
            //System.Diagnostics.Debug.WriteLine("total number of records = " + recordHeaders.Length);
            //System.Diagnostics.Debug.WriteLine("Total time to read shapefile is " + end.Subtract(start).ToString());
            //data = null;
        }

        /// <summary>
        /// Loads a ShapeFile using a path to a .shp shape file
        /// </summary>        
        private void LoadFromFile2(System.IO.Stream shxStream, System.IO.Stream shpStream)
        {            
            DateTime start = DateTime.Now;

            RecordHeader[] recordHeaders = LoadIndexfile(shxStream);

            //Console.Out.WriteLine("Time to load index file: " + ((TimeSpan)DateTime.Now.Subtract(start)).TotalMilliseconds);

            //open the main file and adjust the mainheader file length
            this.shapeFileStream = shpStream;
            this.mainHeader.FileLength = (int)shapeFileStream.Length;
                      
            switch (mainHeader.ShapeType)
            {
                case ShapeType.Point:
                    this.sfRecordCol = new SFPointCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.Polygon:
                    this.sfRecordCol = new SFPolygonCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolyLine:
                    this.sfRecordCol = new SFPolyLineCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolyLineM:
                    this.sfRecordCol = new SFPolyLineMCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolygonZ:
                    this.sfRecordCol = new SFPolygonZCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PointZ:
                    this.sfRecordCol = new SFPointZCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.PolyLineZ:
                    this.sfRecordCol = new SFPolyLineZCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.MultiPoint:
                    this.sfRecordCol = new SFMultiPointCol(recordHeaders, ref mainHeader);
                    break;
                case ShapeType.MultiPointZ:
                    this.sfRecordCol = new SFMultiPointZCol(recordHeaders, ref mainHeader);
                    break;
                default:
                    this.Close();
                    throw new NotSupportedException("ShapeType: " + mainHeader.ShapeType + " not supported");
            }            
        }



        private void FixHeaderRecordBounds()
        {

            double xMin = this.mainHeader.Xmin;
            double xMax = this.mainHeader.Xmax;
            double yMin = this.mainHeader.Ymin;
            double yMax = this.mainHeader.Ymax;
            if (this.CoordinateReferenceSystem != null && this.CoordinateReferenceSystem.AreaOfUse.IsDefined)
            {
                //convert the area of use from lat/lon degrees to the shapefile CRS
                ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
                RectangleD areaOfUse = RectangleD.FromLTRB(this.CoordinateReferenceSystem.AreaOfUse.WestLongitudeDegrees,
                    this.CoordinateReferenceSystem.AreaOfUse.SouthLatitudeDegrees,
                    this.CoordinateReferenceSystem.AreaOfUse.EastLongitudeDegrees,
                    this.CoordinateReferenceSystem.AreaOfUse.NorthLatitudeDegrees);
                
                areaOfUse = areaOfUse.Transform(wgs84, this.CoordinateReferenceSystem);
                
                //check for infinite bounds (common when geographic coords have been converted to 
                //a world mercator projection for areas such as Antarctica
                if (double.IsInfinity(xMin))
                {
                    xMin = areaOfUse.Left;
                }
                if (double.IsInfinity(xMax))
                {
                    xMax = areaOfUse.Right;
                }
                if (double.IsInfinity(yMin))
                {
                    yMin = areaOfUse.Top;
                }
                if (double.IsInfinity(yMax))
                {
                    yMax = areaOfUse.Bottom;
                }                
            }
            this.mainHeader.Xmin = this.sfRecordCol.MainHeader.Xmin = xMin;
            this.mainHeader.Xmax = this.sfRecordCol.MainHeader.Xmax = xMax;
            this.mainHeader.Ymin = this.sfRecordCol.MainHeader.Ymin = yMin;
            this.mainHeader.Ymax = this.sfRecordCol.MainHeader.Ymax = yMax;

            //RectangleD bounds = RectangleD.Empty;//this.GetShapeBoundsD(0);
            //for (int n = 0; n < this.RecordCount; ++n)
            //{
            //    RectangleD r = this.GetShapeBoundsD(n);
            //    if (double.IsInfinity(r.Width) || double.IsInfinity(r.Height))
            //    {
            //        //yikes! the shapefile has a bad record bounds
            //        var shapeData = this.GetShapeDataD(n);
            //        double minX, minY, maxX, maxY;
            //        minX = minY = double.PositiveInfinity;
            //        maxX = maxY = double.NegativeInfinity;
            //        foreach (var part in shapeData)
            //        {
            //            for (int i = 0; i < part.Length; ++i)
            //            {
            //                if (!(double.IsInfinity(part[i].X) || double.IsInfinity(part[i].Y)))
            //                {
            //                    minX = Math.Min(minX, part[i].X);
            //                    maxX = Math.Max(maxX, part[i].X);

            //                    minY = Math.Min(minY, part[i].Y);
            //                    maxY = Math.Max(maxY, part[i].Y);
            //                }
            //            }
            //        }
            //        bounds = RectangleD.Union(bounds, RectangleD.FromLTRB(minX, minY, maxX, maxY));


            //    }
            //    else
            //    {
            //        bounds = RectangleD.Union(bounds, r);
            //    }
            //}
            //this.mainHeader.Xmax = bounds.Right;
            //this.mainHeader.Ymax = bounds.Bottom;
            //this.sfRecordCol.MainHeader.Xmax = bounds.Right;
            //this.sfRecordCol.MainHeader.Ymax = bounds.Bottom;
        }

        /// <summary>
        /// Loads a ShapeFile using a path to a .shp shape file
        /// </summary>
        /// <param name="shapeFilePath">The path to the ".shp" shape file</param>
        /// <param name="useMemoryStreams">Whether to load shp and dbf into a MemoryStream</param>
        /// <remarks>
        /// <para>
        /// Setting useMemoryStreams to true can improve performance if reading records
        /// multiple times in scenarios such as linear referencing
        /// </para>
        /// </remarks>
        public unsafe void LoadFromFile(string shapeFilePath, bool useMemoryStreams = false)
        {
            //read the projection first
            ReadPrjFile(shapeFilePath);
            
            LoadFromFile2(shapeFilePath, useMemoryStreams);

            //create a default RenderSettings object

            if (useMemoryStreams)
            {
                string dbfFilePath = this.filePath;
                if (dbfFilePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
                {
                    dbfFilePath = System.IO.Path.ChangeExtension(filePath, "dbf");
                }
                else if (!dbfFilePath.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase))
                {
                    dbfFilePath += ".dbf";
                }
                MemoryStream dbfStream = new MemoryStream();
                using (var fs = new FileStream(dbfFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096))
                {
                    fs.CopyTo(dbfStream);
                    dbfStream.Seek(0, SeekOrigin.Begin);
                }
                this.RenderSettings = CreateRenderSettings("", dbfStream);
            }
            else
            {                
                this.RenderSettings = CreateRenderSettings("");
            }            
        }

        /// <summary>
        /// Loads a shapefile from the individual streams from the shapefile's .shx, .shp, .dbf and .prj files
        /// </summary>
        /// <param name="shxStream"></param>
        /// <param name="shpStream"></param>
        /// <param name="dbfStream"></param>
        /// <param name="prjStream"></param>
        public void LoadFromFile(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream)
        {
			LoadFromStreams(shxStream, shpStream, dbfStream, prjStream);
        }

		/// <summary>
		/// Loads a shapefile from the individual streams from the shapefile's .shx, .shp, .dbf and .prj files
		/// </summary>
		/// <param name="shxStream"></param>
		/// <param name="shpStream"></param>
		/// <param name="dbfStream"></param>
		/// <param name="prjStream"></param>
		public void LoadFromStreams(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream)
		{
			this.ProjectionWKT = "";
			if (prjStream != null)
			{
				ReadProjection(prjStream);
			}
			LoadFromFile2(shxStream, shpStream);
			//create a default RenderSettings object
			this.RenderSettings = CreateRenderSettings("", dbfStream);
		}


		#region Read Projection WKT

		private void ReadPrjFile(string shapeFilePath)
        {
            if (shapeFilePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                shapeFilePath = shapeFilePath.Substring(0, shapeFilePath.Length - 4);
            }

            string prjFilePath = shapeFilePath + ".prj";
            if (System.IO.File.Exists(prjFilePath))
            {
                using (System.IO.FileStream fs = new FileStream(prjFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    ReadProjection(fs);
                }
            }
            else
            {
                ProjectionWKT = "";
            }
        }

        private void ReadProjection(System.IO.Stream prjStream)
        {
            using (System.IO.StreamReader reader = new StreamReader(prjStream))
            {
                this.ProjectionWKT = reader.ReadToEnd().Trim();
            }
        }

        #endregion

        /// <summary>
        /// Disposes of the ShapeFile
        /// </summary>
        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
       
        /// <summary>
        /// Closes the underlying stream of the ShapeFile and clears all of the internal data.
        /// After calling this method the ShapeFile can no longer be rendered
        /// </summary>
        public void Close()
        {
			if (this.dbfReader != null)
			{
				this.dbfReader.Dispose();
				this.dbfReader = null;
			}
            if (this.RenderSettings != null)
            {
                RenderSettings.Dispose();
                this.RenderSettings = null;
            }
            if (this.shapeFileStream != null)
            {
                shapeFileStream.Close();
                shapeFileStream = null;
            }
            shapeQuadTree = null;
            filePath = null;
            sfRecordCol = null;
            //System.GC.Collect();
        }

        /// <summary>
        /// overridden ToString method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.shapeFileStream == null) return "Empty";
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} [{1}]", new object[] { myName, ShapeType.ToString() });
        }

        #region "XML methods"
        /// <summary>
        /// Writes an Xml representation of the ShapeFile.        
        /// </summary>
        /// <param name="writer"></param>
        /// <remarks>Use ReadXml to load a ShapeFile from the Xml representation writen by this method</remarks>
        /// <seealso cref="ReadXml"/>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("shapefile");

            writer.WriteStartElement("name");
            writer.WriteString(Name);
            writer.WriteFullEndElement();
            writer.WriteStartElement("path");
            writer.WriteString(this.filePath);
            writer.WriteFullEndElement();

            if (this.RenderSettings != null)
            {
                this.RenderSettings.WriteXml(writer);
            }

            writer.WriteEndElement();

        }

        /// <summary>
        /// Reads and and loads a ShapeFile from Xml representation of the ShapeFile (as output by WriteXml)
        /// </summary>
        /// <param name="element"></param>
        /// <seealso cref="WriteXml"/>
        public void ReadXml(XmlElement element, string baseDirectory, bool useMemoryStreams=false)
        {            
            string path = element.GetElementsByTagName("path")[0].InnerText;
            //check if path is relative to project
            if (!Path.IsPathRooted(path))
            {
                path = System.IO.Path.Combine(baseDirectory, path);
            }

            this.LoadFromFile(path);

            this.Name = element.GetElementsByTagName("name")[0].InnerText;

            XmlNodeList renderList = element.GetElementsByTagName("renderer");
            if (renderList != null && renderList.Count > 0)
            {
                this.RenderSettings = new RenderSettings(path);
                RenderSettings.ReadXml(renderList[0] as XmlElement);
            }
        }

        #endregion


        
        /// <summary>
        /// Gets the raw shape data at given record index.        
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator</remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> GetShapeData(int recordIndex)
        {
            List<PointF[]> data = sfRecordCol.GetShapeData(recordIndex, this.shapeFileStream);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<PointF[]>(data);
        }

        /// <summary>
        /// Gets the raw shape data at given record index, using a supplied dataBuffer to read the data.         
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <param name="dataBuffer"> A supplied data buffer to use when reading the raw shape data from the shapefile. The buffer must be large enough to read the raw
        /// shape data.</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator</remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> GetShapeData(int recordIndex, byte[] dataBuffer)
        {
            List<PointF[]> data = sfRecordCol.GetShapeData(recordIndex, this.shapeFileStream, dataBuffer);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<PointF[]>(data);
        }

        /// <summary>
        /// Gets the raw shape data in double precision format at given record index.        
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator</remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> GetShapeDataD(int recordIndex)
        {
            List<PointD[]> data = sfRecordCol.GetShapeDataD(recordIndex, this.shapeFileStream);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<PointD[]>(data);
        }

        /// <summary>
        /// Gets the raw shape data in double precision format at given record index, using a supplied dataBuffer to read the data.         
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <param name="dataBuffer"> A supplied data buffer to use when reading the raw shape data from the shapefile. The buffer must be large enough to read the raw
        /// shape data.</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator</remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> GetShapeDataD(int recordIndex, byte[] dataBuffer)
        {
            List<PointD[]> data = sfRecordCol.GetShapeDataD(recordIndex, this.shapeFileStream, dataBuffer);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<PointD[]>(data);
        }


        /// <summary>
        /// Gets the raw shape Z(height) data at given record index.        
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator
        /// <para>If the shapefile does not contain any Z values then this method will return null</para>
        /// </remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<float[]> GetShapeZData(int recordIndex)
        {
            List<float[]> data = sfRecordCol.GetShapeHeightData(recordIndex, this.shapeFileStream);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<float[]>(data);
        }

        /// <summary>
        /// Gets the raw shape Z(height) data at given record index, using a supplied dataBuffer to read the data.         
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <param name="dataBuffer"> A supplied data buffer to use when reading the raw shape data from the shapefile. The buffer must be large enough to read the raw
        /// shape data.</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator
        /// <para>If the shapefile does not contain any Z values then this method will return null</para>
        /// </remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<float[]> GetShapeZData(int recordIndex, byte[] dataBuffer)
        {
            List<float[]> data = sfRecordCol.GetShapeHeightData(recordIndex, this.shapeFileStream, dataBuffer);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<float[]>(data);
        }


        /// <summary>
        /// Gets the raw shape Z(height) data in double precision format at given record index, using a supplied dataBuffer to read the data.         
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <param name="dataBuffer"> A supplied data buffer to use when reading the raw shape data from the shapefile. The buffer must be large enough to read the raw
        /// shape data.</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator
        /// <para>If the shapefile does not contain any Z values then this method will return null</para>
        /// </remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.ReadOnlyCollection<double[]> GetShapeZDataD(int recordIndex, byte[] dataBuffer)
        {
            List<double[]> data = sfRecordCol.GetShapeHeightDataD(recordIndex, this.shapeFileStream, dataBuffer);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<double[]>(data);
        }



        /// <summary>
        /// Gets the raw shape M(Measures) data in double precision format at given record index, using a supplied dataBuffer to read the data.         
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <param name="dataBuffer"> A supplied data buffer to use when reading the raw shape data from the shapefile. The buffer must be large enough to read the raw
        /// shape data.</param>
        /// <returns></returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<double[]> GetShapeMDataD(int recordIndex, byte[] dataBuffer)
        {
            List<double[]> data = sfRecordCol.GetShapeMDataD(recordIndex, this.shapeFileStream, dataBuffer);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<double[]>(data);
        }

        /// <summary>
        /// Gets the raw shape M(Measures) data in double precision format at given record index, using a supplied dataBuffer to read the data.         
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <returns></returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<double[]> GetShapeMDataD(int recordIndex)
        {
            List<double[]> data = sfRecordCol.GetShapeMDataD(recordIndex, this.shapeFileStream);
            if (data == null) return null;
            return new System.Collections.ObjectModel.ReadOnlyCollection<double[]>(data);
        }


        /// <summary>
        /// Gets the rectangular bounds of an individual shape in the ShapeFile
        /// </summary>
        /// <param name="recordIndex">Zero based index of shape bounds to return</param>
        /// <returns></returns>
        public RectangleF GetShapeBounds(int recordIndex)
        {
            return sfRecordCol.GetRecordBounds(recordIndex, shapeFileStream);
        }

        /// <summary>
        /// Gets the rectangular bounds of an individual shape in the ShapeFile in double precision coordinates
        /// </summary>
        /// <param name="recordIndex">Zero based index of shape bounds to return</param>
        /// <returns></returns>
        public RectangleD GetShapeBoundsD(int recordIndex)
        {
            return sfRecordCol.GetRecordBoundsD(recordIndex, shapeFileStream);
        }

        #region "GetShapeIndexContainingPoint"

        /// <summary>
        /// returns the index of the shape containing Point pt
        /// </summary>
        /// <param name="pt">The location of the point. (in sourceCRS coordinate units or in the shapefile's coordinate units if sourceCRS is null)</param>
        /// <param name="minDistance">the min distance (in sourceCRS coordinate units or in the shapefile's coordinate units if sourceCRS is null)
        /// between pt and a shape when searching point or line shapes that will return a "hit". </param>
        ///  <param name="sourceCRS">The source CRS that pt and minDistance is defined in. If this parameter is null then the shapefile's coordinate units will be used, which was the 
        ///  default behaviour prior to version 4.6.x. To support backwards compatibility the default value of sourceCRS is set to null</param>
        /// <returns>zero based index of the shape containing pt or -1 if no shape contains the point</returns>
        /// <remarks>When searching in a Point ShapeFile a point will be defined as contining pt if the distance between the found point and pt is less than or equal to minDistance
        /// <para>When searching in a PolyLine ShapeFile a Line will be defined as containing the point if the distance between a line segment in the found shape and pt is less than or equal to minDistance</para>
        /// </remarks>
        public int GetShapeIndexContainingPoint(PointD pt, double minDistance, ICRS sourceCRS = null)
        {
            if (sourceCRS != null && this.CoordinateReferenceSystem != null &&
                !this.CoordinateReferenceSystem.IsEquivalent(sourceCRS))
            {
                //transform pt to the shapefile's coordinates and calculate the equivalent minDistance                        
                using (ICoordinateTransformation transformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sourceCRS, this.CoordinateReferenceSystem))
                {
                   unsafe
                    {
                        PointD ptY = new PointD(pt.X, pt.Y + minDistance);
                        PointD ptX = new PointD(pt.X + minDistance, pt.Y);
                        //PointD* ptPtr = &pt;
                        transformation.Transform((double*)&pt, 1);
                        transformation.Transform((double*)&ptY, 1);
                        transformation.Transform((double*)&ptX, 1);
                        minDistance = Math.Max(Math.Sqrt((ptX.X - pt.X) * (ptX.X - pt.X) + (ptX.Y - pt.Y) * (ptX.Y - pt.Y)),
                                               Math.Sqrt((ptY.X - pt.X) * (ptY.X - pt.X) + (ptY.Y - pt.Y) * (ptY.Y - pt.Y)));
                    }
                }
            }

            //first check the entire shapefile's Extent
            RectangleD extent = Extent;// GetActualExtent();
            extent.Inflate(minDistance, minDistance);
            if (extent.Contains(pt))
            {
                switch (sfRecordCol.MainHeader.ShapeType)
                {
                    case ShapeType.Point:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFPointCol, this.shapeFileStream);
                    case ShapeType.PointZ:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFPointZCol, this.shapeFileStream);                        
                    case ShapeType.Polygon:
                        return GetShapeIndexContainingPoint(pt, sfRecordCol as SFPolygonCol);
                    case ShapeType.PolygonZ:
                        return GetShapeIndexContainingPoint(pt, sfRecordCol as SFPolygonZCol);
                    case ShapeType.PolyLine:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFPolyLineCol);
                    case ShapeType.PolyLineM:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFPolyLineMCol);
                    case ShapeType.PolyLineZ:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFPolyLineZCol);
                    case ShapeType.MultiPoint:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFMultiPointCol, this.shapeFileStream);
                    case ShapeType.MultiPointZ:
                        return GetShapeIndexContainingPoint(pt, minDistance, sfRecordCol as SFMultiPointZCol, this.shapeFileStream);                        
                    
                    default:
                        return -1;
                }                
            }
            return -1;
        }


        private QuadTree shapeQuadTree;

        private void CreateQuadTree(SFRecordCol col)
        {
            RectangleD r = Extent;
            r.Inflate(r.Width * 0.05, r.Height * 0.05);
            //if (r.Width <= double.Epsilon) r.Width = double.Epsilon;
            //if (r.Height <= double.Epsilon) r.Height = double.Epsilon;
            int startLevel = 0;
            if (col.RecordHeaders.Length < 10)
            {
                //if the shapefile has less than 10 records then set the startLevel to MaxLevels -1
                //as it's not efficient to create a deep number of levels in this case 
                startLevel = QTNode.MaxLevels-1;
            }
            shapeQuadTree = new QuadTree(r, startLevel);
            QTNodeHelper helper = (QTNodeHelper)col;
            for (int n = 0; n < col.RecordHeaders.Length; n++)
            {
                shapeQuadTree.Insert(n, helper, this.shapeFileStream);
            }
        }

       
        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFPointCol col, Stream shapefileStream)
        {
            if (col.RecordHeaders.Length < 200)
            {
                double distSqr = minDistance * minDistance;
                int numRecs = col.RecordHeaders.Length;
                PointD ptd = pt;// new PointD(pt.X, pt.Y);
                for (int n = 0; n < numRecs; n++)
                {
                    if (!col.IsRecordVisible(n)) continue;

                    PointD shapePt = col.GetPointD(n, shapefileStream);
                    double x = (ptd.X - shapePt.X);
                    double y = (ptd.Y - shapePt.Y);
                    double d = x * x + y * y;
                    if (d <= distSqr)
                    {
                        return n;
                    }
                }
                return -1;
            }
            else
            {
                if (shapeQuadTree == null)
                {
                    CreateQuadTree(col);
                }
                double distSqr = minDistance * minDistance;
                // PointF ptf = new PointF((float)pt.X, (float)pt.Y);
                List<int> indices = shapeQuadTree.GetIndices(pt);
                if (indices != null)
                {
                    byte[] buffer = SFRecordCol.SharedBuffer;
                    for (int n = 0; n < indices.Count; n++)
                    {
                        if (!col.IsRecordVisible(indices[n])) continue;

                        PointD shapePt = col.GetPointD(indices[n], shapefileStream);
                        double x = (pt.X - shapePt.X);
                        double y = (pt.Y - shapePt.Y);
                        double d = x * x + y * y;
                        if (d <= distSqr)
                        {
                            return indices[n];
                        }
                    }
                }
                return -1;
            }
        }

        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFPointZCol col, Stream shapefileStream)
        {
            if (col.RecordHeaders.Length < 200)
            {
                double distSqr = minDistance * minDistance;
                int numRecs = col.RecordHeaders.Length;
                PointD ptd = pt;// new PointD(pt.X, pt.Y);
                for (int n = 0; n < numRecs; n++)
                {
                    if (!col.IsRecordVisible(n)) continue;

                    PointD shapePt = col.GetPointD(n, shapefileStream);
                    double x = (ptd.X - shapePt.X);
                    double y = (ptd.Y - shapePt.Y);
                    double d = x * x + y * y;
                    if (d <= distSqr)
                    {
                        return n;
                    }
                }
                return -1;
            }
            else
            {
                if (shapeQuadTree == null)
                {
                    CreateQuadTree(col);
                }
                double distSqr = minDistance * minDistance;
                //PointF ptf = new PointF((float)pt.X, (float)pt.Y);
                List<int> indices = shapeQuadTree.GetIndices(pt);
                if (indices != null)
                {
                    byte[] buffer = SFRecordCol.SharedBuffer;
                    for (int n = 0; n < indices.Count; n++)
                    {
                        if (!col.IsRecordVisible(indices[n])) continue;

                        PointD shapePt = col.GetPointD(indices[n], shapefileStream);
                        double x = (pt.X - shapePt.X);
                        double y = (pt.Y - shapePt.Y);
                        double d = x * x + y * y;
                        if (d <= distSqr)
                        {
                            return indices[n];
                        }
                    }
                }
                return -1;
            }
        }

        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFMultiPointCol col, Stream shapefileStream)
        {
            //if (col.RecordHeaders.Length < 200)
            {
                double distSqr = minDistance * minDistance;
                int numRecs = col.RecordHeaders.Length;
                for (int n = 0; n < numRecs; n++)
                {
                    if (!col.IsRecordVisible(n)) continue;

                    List<PointD[]> pts = col.GetShapeDataD(n, shapeFileStream);
                    if (pts.Count > 0)
                    {
                        for (int p = pts[0].Length-1; p >= 0; --p)
                        {
                            PointD shapePt = pts[0][p];
                            double x = (pt.X - shapePt.X);
                            double y = (pt.Y - shapePt.Y);
                            double d = x * x + y * y;
                            if (d <= distSqr)
                            {
                                return n;
                            }
                        }
                    }
                }
                return -1;
            }
            
        }

        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFMultiPointZCol col, Stream shapefileStream)
        {
            //if (col.RecordHeaders.Length < 200)
            {
                double distSqr = minDistance * minDistance;
                int numRecs = col.RecordHeaders.Length;
                for (int n = 0; n < numRecs; n++)
                {
                    if (!col.IsRecordVisible(n)) continue;

                    List<PointD[]> pts = col.GetShapeDataD(n, shapeFileStream);
                    if (pts.Count > 0)
                    {
                        for (int p = pts[0].Length - 1; p >= 0; --p)
                        {
                            PointD shapePt = pts[0][p];
                            double x = (pt.X - shapePt.X);
                            double y = (pt.Y - shapePt.Y);
                            double d = x * x + y * y;
                            if (d <= distSqr)
                            {
                                return n;
                            }
                        }
                    }
                }
                return -1;
            }

        }

        private int GetShapeIndexContainingPoint(PointD pt, SFPolygonCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> indices = shapeQuadTree.GetIndices(pt);
            if (indices != null)
            {
                for (int n = 0; n < indices.Count; n++)
                {
                    if (!col.IsRecordVisible(indices[n])) continue;

                    if (col.GetRecordBoundsD(indices[n],this.shapeFileStream).Contains(pt))
                    {
                        if (col.ContainsPoint(indices[n], pt, shapeFileStream))
                        {
                            return indices[n];
                        }
                    }
                }
            }
            return -1;// foundIndex;           
        }

        private int GetShapeIndexContainingPoint(PointD pt, SFPolygonZCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> indices = shapeQuadTree.GetIndices(pt);
            if (indices != null)
            {
                for (int n = 0; n < indices.Count; n++)
                {
                    if (!col.IsRecordVisible(indices[n])) continue;

                    if (col.GetRecordBoundsD(indices[n], this.shapeFileStream).Contains(pt))
                    {
                        if (col.ContainsPoint(indices[n], pt, shapeFileStream))
                        {
                            return indices[n];
                        }
                    }
                }
            }
            return -1;// foundIndex;           
        }

        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFPolyLineCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> indices = shapeQuadTree.GetIndices(pt);
            if (indices != null)
            {
                byte[] buffer = SFRecordCol.SharedBuffer;
                RectangleD r = new RectangleD(pt.X - minDistance, pt.Y - minDistance, minDistance * 2, minDistance * 2);
                for (int n = 0; n < indices.Count; n++)
                {
                    if (!col.IsRecordVisible(indices[n])) continue;

                    if (col.GetRecordBounds(indices[n], this.shapeFileStream).IntersectsWith(r))                    
                    {
                        if (col.ContainsPoint(indices[n], pt, shapeFileStream, buffer, minDistance))
                        {
                            return indices[n];
                        }
                    }
                }
            }
            return -1;
        }

        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFPolyLineMCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> indices = shapeQuadTree.GetIndices(pt);
            if (indices != null)
            {
                byte[] buffer = SFRecordCol.SharedBuffer;
                RectangleD r = new RectangleD(pt.X - minDistance, pt.Y - minDistance, minDistance * 2, minDistance * 2);
                for (int n = 0; n < indices.Count; n++)
                {
                    if (!col.IsRecordVisible(indices[n])) continue;

                    if (col.GetRecordBounds(indices[n], this.shapeFileStream).IntersectsWith(r))
                    {
                        if (col.ContainsPoint(indices[n], pt, shapeFileStream, buffer, minDistance))
                        {
                            return indices[n];
                        }
                    }
                }
            }
            return -1;
        }
         
        private int GetShapeIndexContainingPoint(PointD pt, double minDistance, SFPolyLineZCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> indices = shapeQuadTree.GetIndices(pt);
            if (indices != null)
            {
                byte[] buffer = SFRecordCol.SharedBuffer;
                RectangleD r = new RectangleD(pt.X - minDistance, pt.Y - minDistance, minDistance * 2, minDistance * 2);
                for (int n = 0; n < indices.Count; n++)
                {
                    if (!col.IsRecordVisible(indices[n])) continue;

                    if (col.GetRecordBounds(indices[n], this.shapeFileStream).IntersectsWith(r))
                    {
                        if (col.ContainsPoint(indices[n], pt, shapeFileStream, buffer, minDistance))
                        {
                            return indices[n];
                        }
                    }
                }
            }
            return -1;
        }

#endregion

        #region "Find shapes by position methods"

        /// <summary>
        /// Gets records intersecting a given rectangle
        /// </summary>
        /// <param name="indicies"></param>
        /// <param name="rect">rectangular extent in source CRS coordinates if sourcCRS not null else in the shapefile coordinates</param>
        /// <remarks>For backward compatibility sourceCRS is null by default</remarks>
        public void GetShapeIndiciesIntersectingRect(List<int> indicies, RectangleD rect, ICRS sourceCRS = null)
        {
            if (indicies == null) return;

            if (sourceCRS != null && this.CoordinateReferenceSystem != null &&
                !this.CoordinateReferenceSystem.IsEquivalent(sourceCRS))
            {
                //transform rect to the shapefile's coordinates 
                //rect = ShapeFile.ConvertExtent(rect, sourceCRS, this.CoordinateReferenceSystem);                
                rect = rect.Transform(sourceCRS, this.CoordinateReferenceSystem);
            }


            if (!rect.IntersectsWith(this.Extent)) return;

            GetShapeIndiciesIntersectingRect(indicies, rect, sfRecordCol);            
        }

        private void GetShapeIndiciesIntersectingRect(List<int> indices, RectangleD rect, SFRecordCol col)
        {
            
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> shapeIndicies = shapeQuadTree.GetIndices(ref rect);
            if (shapeIndicies != null)
            {
                for (int n = shapeIndicies.Count - 1; n >= 0; --n)
                {
                    if (!col.IsRecordVisible(shapeIndicies[n])) continue;

                    if (col.IntersectRect(shapeIndicies[n], ref rect, shapeFileStream))
                    {
                        indices.Add(shapeIndicies[n]);
                    }
                }
            }            
        }

        
        public bool ShapeIntersectsRect(int shapeIndex, ref RectangleD rect)
        {
            switch (sfRecordCol.MainHeader.ShapeType)
            {
                case ShapeType.Polygon:
                    return (sfRecordCol as SFPolygonCol).IntersectRect(shapeIndex, ref rect, shapeFileStream);
                case ShapeType.PolygonZ:
                    return (sfRecordCol as SFPolygonZCol).IntersectRect(shapeIndex, ref rect, shapeFileStream);                
                default:
                    return sfRecordCol.IntersectRect(shapeIndex, ref rect, shapeFileStream);
            }
        }


        /// <summary>
        /// gets records intersecting a circle 
        /// </summary>
        /// <param name="indicies"></param>
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <param name="sourceCRS"></param>
        public void GetShapeIndiciesIntersectingCircle(List<int> indicies, PointD centre, double radius, ICRS sourceCRS = null)
        {

            if (indicies == null) return;

            if (sourceCRS != null && this.CoordinateReferenceSystem != null &&
                !this.CoordinateReferenceSystem.IsEquivalent(sourceCRS))
            {
                //transform pt to the shapefile's coordinates and calculate the equivalent radius                        
                using (ICoordinateTransformation transformation = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sourceCRS, this.CoordinateReferenceSystem))
                {
                    unsafe
                    {
                        PointD ptY = new PointD(centre.X, centre.Y + radius);
                        PointD ptX = new PointD(centre.X + radius, centre.Y);
                        //PointD* ptPtr = &pt;
                        transformation.Transform((double*)&centre, 1);
                        transformation.Transform((double*)&ptY, 1);
                        transformation.Transform((double*)&ptX, 1);
                        radius = Math.Max(Math.Sqrt((ptX.X - centre.X) * (ptX.X - centre.X) + (ptX.Y - centre.Y) * (ptX.Y - centre.Y)),
                                               Math.Sqrt((ptY.X - centre.X) * (ptY.X - centre.X) + (ptY.Y - centre.Y) * (ptY.Y - centre.Y)));
                    }
                }
            }

            GetShapeIndiciesIntersectingCircle(indicies, centre, radius, sfRecordCol);
            
        }

        private void GetShapeIndiciesIntersectingCircle(List<int> indices, PointD centre, double radius, SFRecordCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> shapeIndicies = shapeQuadTree.GetIndices(centre, radius);
            if (shapeIndicies != null)
            {
                for (int n = shapeIndicies.Count - 1; n >= 0; --n)
                {
                    if (col.IsRecordVisible(shapeIndicies[n]) && col.IntersectCircle(shapeIndicies[n], centre, radius, shapeFileStream))
                    {
                        indices.Add(shapeIndicies[n]);
                    }
                }
            }
        }


        public int GetClosestShape(PointD centre, double radius)
        {
            return GetClosestShape(centre, radius, sfRecordCol);            
        }

        private int GetClosestShape(PointD centre, double radius, SFRecordCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> shapeIndicies = shapeQuadTree.GetIndices(centre, radius);
            int closestIndex = -1;
            double closestDistance = radius + double.Epsilon;
            if (shapeIndicies != null)
            {
                for (int n = shapeIndicies.Count - 1; n >= 0; --n)
                {
                    if (!col.IsRecordVisible(shapeIndicies[n])) continue;

                    double d = col.GetDistanceToShape(shapeIndicies[n], centre, closestDistance, shapeFileStream);
                    if (d < closestDistance)
                    {
                        closestIndex = shapeIndicies[n];
                        closestDistance = d;                       
                    }
                }
            }
            return closestIndex;
        }

        public int GetClosestShape(PointD centre, double radius, out PolylineDistanceInfo polylineDistanceInfo)
        {
            return GetClosestShape(centre, radius, sfRecordCol, out polylineDistanceInfo);
        }

        private int GetClosestShape(PointD centre, double radius, SFRecordCol col, out PolylineDistanceInfo polylineDistanceInfo)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> shapeIndicies = shapeQuadTree.GetIndices(centre, radius);
            int closestIndex = -1;
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            double closestDistance = radius + double.Epsilon;
            if (shapeIndicies != null)
            {
                for (int n = shapeIndicies.Count - 1; n >= 0; --n)
                {
                    if (!col.IsRecordVisible(shapeIndicies[n])) continue;

                    PolylineDistanceInfo pdi;
                    double d = col.GetDistanceToShape(shapeIndicies[n], centre, closestDistance, shapeFileStream, out pdi);
                    if (d < closestDistance)
                    {
                        closestIndex = shapeIndicies[n];
                        polylineDistanceInfo = pdi;
                        closestDistance = d;
                    }
                }
            }
            return closestIndex;
        }


        /// <summary>
        /// returns the index of the closest shape in the given collection of records
        /// </summary>
        /// <param name="centre"></param>
        /// <param name="radius"></param>
        /// <param name="recordIndicies">list of zero-based record indices</param>
        /// <param name="polylineDistanceInfo"></param>
        /// <returns></returns>
        public int GetClosestShape(PointD centre, double radius, List<int> recordIndices, out PolylineDistanceInfo polylineDistanceInfo)
        {
            return GetClosestShape(centre, radius, sfRecordCol, recordIndices, out polylineDistanceInfo);
        }

        public int GetClosestShape(PointD centre, double radius, System.Collections.ObjectModel.ReadOnlyCollection<int> recordIndices, out PolylineDistanceInfo polylineDistanceInfo)
        {
            
            return GetClosestShape(centre, radius, sfRecordCol, recordIndices, out polylineDistanceInfo);
        }

        private int GetClosestShape(PointD centre, double radius, SFRecordCol col, System.Collections.ObjectModel.ReadOnlyCollection<int> recordIndices, out PolylineDistanceInfo polylineDistanceInfo)
        {
            int closestIndex = -1;
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            double closestDistance = radius + double.Epsilon;
            if (recordIndices != null)
            {
                foreach(int recordIndex in recordIndices)
                {
                    if (!col.IsRecordVisible(recordIndex)) continue;

                    PolylineDistanceInfo pdi;
                    double d = col.GetDistanceToShape(recordIndex, centre, closestDistance, shapeFileStream, out pdi);
                    if (d < closestDistance)
                    {
                        closestIndex = recordIndex;
                        polylineDistanceInfo = pdi;
                        closestDistance = d;
                    }
                }
            }
            return closestIndex;
        }

        private int GetClosestShape(PointD centre, double radius, SFRecordCol col, List<int> recordIndices, out PolylineDistanceInfo polylineDistanceInfo)
        {
            List<int> shapeIndices = recordIndices;
            int closestIndex = -1;
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            double closestDistance = radius + double.Epsilon;
            if (shapeIndices != null)
            {
                for (int n = shapeIndices.Count - 1; n >= 0; --n)
                {
                    if (!col.IsRecordVisible(shapeIndices[n])) continue;

                    PolylineDistanceInfo pdi;
                    double d = col.GetDistanceToShape(shapeIndices[n], centre, closestDistance, shapeFileStream, out pdi);
                    if (d < closestDistance)
                    {
                        closestIndex = shapeIndices[n];
                        polylineDistanceInfo = pdi;
                        closestDistance = d;
                    }
                }
            }
            return closestIndex;
        }



        #endregion

       
        #region "IEnumerator members"

        /// <summary>
        /// Gets a ShapeFileEnumerator, which can be used to enumerate over all of the records in the ShapeFile
        /// </summary>        
        /// <returns></returns>
        public ShapeFileEnumerator GetShapeFileEnumerator()
        {
            return new ShapeFileEnumerator(this);
        }

        /// <summary>
        /// Gets a ShapeFileEnumerator, which can be used to enumerate over all of the records in the ShapeFile, intersecting with a given bounds
        /// </summary>
        /// <returns></returns>        
        /// <param name="extents">extents defines the area that returned shapes must intersect with.If this is RectangleF.Empty then no recrods will be returned</param>        
        public ShapeFileEnumerator GetShapeFileEnumerator(RectangleD extents)
        {
            return new ShapeFileEnumerator(this, extents);
        }

        /// <summary>
        /// Gets a ShapeFileEnumerator, which can be used to enumerate over all of the records in the ShapeFile, intersecting or contained within a given bounds
        /// </summary>
        /// <returns></returns>        
        /// <param name="extent">Defines the area that returned shapes must intersect with or be contained by. If this is RectangleF.Empty then no recrods will be returned</param>        
        /// <param name="intersectionType">Indicates whether shapes must intersect with or be contained by extent</param>        
        public ShapeFileEnumerator GetShapeFileEnumerator(RectangleF extent, ShapeFileEnumerator.IntersectionType intersectionType)
        {
            return new ShapeFileEnumerator(this, extent, intersectionType);
        }

        #endregion

             
        private const double MaxLLMercProjD = 85.0511287798066;
        /// <summary>
        /// Applys the Mercator projection to a lat long pt and returns
        /// the projected point
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static PointD LLToMercator(PointD pt)
        {

            if (pt.Y > MaxLLMercProjD)
            {
                pt.Y = MaxLLMercProjD;
            }
            else if (pt.Y < -MaxLLMercProjD)
            {
                pt.Y = -MaxLLMercProjD;
            }
            
            double d = (Math.PI / 180) * pt.Y;
            double sd = Math.Sin(d);
            d = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
            return new PointD(pt.X, d);
        }
        
        /// <summary>
        /// Utillity method to translate a Point using a Mercator Projection t its Lat/Long representation.
        /// </summary>
        public static PointD MercatorToLL(PointD pt)
        {
            double d = (Math.PI / 180) * pt.Y;
            d = Math.Atan(Math.Sinh(d));
            d = d * (180 / Math.PI);
            return new PointD(pt.X, d);
        }

    }

    #region "Shapefile structs"

    /// <summary>
    /// Enumeration representing a ShapeType. Currently supported shape types are Point, PolyLine, Polygon and PolyLineM
    /// </summary>
    public enum ShapeType { NullShape = 0, Point = 1, PolyLine = 3, Polygon = 5, MultiPoint = 8, PointZ = 11, PolyLineZ = 13, PolygonZ = 15, MultiPointZ = 18, PointM = 21, PolyLineM = 23}
    
    	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct ShapeFileMainHeader
	{
        internal const int MAIN_HEADER_LENGTH = 100;
		public int FileCode;
		public int UnusedByte1;
		public int UnusedByte2;
		public int UnusedByte3;
		public int UnusedByte4;
		public int UnusedByte5;
		public int FileLength;
		public int Version;
		public ShapeType ShapeType;
		public double Xmin;
		public double Ymin;
		public double Xmax;
		public double Ymax;
		public double Zmin;
		public double Zmax;
		public double Mmin;
		public double Mmax;	

		public ShapeFileMainHeader(byte[] data)
		{
            //first convert any BE ints in the data to LE
			//swap FileCode
			EndianUtils.SwapIntBytes(data,0);
			//no need to swap unused bytes
			//swap File Length
			EndianUtils.SwapIntBytes(data,24);
				
			fixed(byte* bPtr = data)
			{
				//now cast and dereference the pointer
				this = *(ShapeFileMainHeader*)bPtr;
			}
			//adjust FileLength to be number of bytes (not num words)
			FileLength*=2;

            //double d = BitConverter.ToDouble(data, 9 * 4 + 8);
            //Console.Out.WriteLine("d=" + d);

            //d = BitConverter.ToDouble(data, 9 * 4 + 16);
            //Console.Out.WriteLine("d=" + d);

            //byte[] dbytes = BitConverter.GetBytes(d);
            

		}

		public override string ToString()
		{
			string str = "Filecode = " + FileCode + ", FileLength = " + FileLength + ", Version = " + Version + ", ShapeType = " + ShapeType;
			str += ", XMin = " + Xmin + ", Ymin = " + Ymin + ", Xmax = " + Xmax + ", Ymax = " + Ymax + ", MMin = " + Mmin + ", Mmax = " + Mmax;
			return str;
		}

	}
		
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct RecordHeader
	{
		public int RecordNumber;
		public int Offset;
        public int ContentLength;
		
		public RecordHeader(int recNum)
		{
			RecordNumber = recNum;
			ContentLength = 0;
			Offset= 0;
		}

		public void readFromIndexFile(byte[] data, int dataOffset)
		{
			Offset = EndianUtils.ReadIntBE(data, dataOffset) <<1; //offset in bytes
			ContentLength = EndianUtils.ReadIntBE(data, dataOffset+4) <<1; //*2 because length is in words not bytes
		}	

			
	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]	
	internal struct Box
	{        
		internal double xmin;
        internal double ymin;
        internal double xmax;
        internal double ymax;		

		public unsafe Box(byte[] data, int dataOffset)
		{
			fixed(byte* bPtr = data)
			{
				//now cast and de-reference the pointer
				this = *(Box*)(bPtr+dataOffset);
			}			
		}

		public Box(double xMin, double yMin, double xMax, double yMax)
		{
			xmin = xMin;
			ymin = yMin;
			xmax = xMax;
			ymax = yMax;
		}

		public override string ToString()
		{
			return "{" + xmin + "," + ymin + "," + xmax + "," + ymax + "}";
		}

        public RectangleD ToRectangleD()
        {
            return RectangleD.FromLTRB(xmin, ymin, xmax, ymax);
        }

        public RectangleF ToRectangleF()
        {
            return RectangleF.FromLTRB((float)xmin, (float)ymin, (float)xmax, (float)ymax);
        }

        public double Width
        {
            get
            {
                return xmax - xmin;
            }
        }

        public double Height
        {
            get
            {
                return ymax - ymin;
            }
        }

	}
    
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct MultiPointRecord
	{
		public RecordHeader recordHeader;
		public Box bounds;
		public int NumPoints;
		
		///To Do : add measures

        //public MultiPointRecord(RecordHeader recHeader)
        //{
        //    recordHeader = recHeader;
        //    NumPoints=0;			
        //    bounds = new Box(0,0,0,0);
        //}

		public void read(byte[] data, int dataOffset)
		{
			bounds = new Box(data, dataOffset+4);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+40);
		}
	}
	    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolyLineRecordP
    {
        //public RecordHeader recordHeader;
        public ShapeType ShapeType;
        public Box bounds;
        public int NumParts;
        public int NumPoints;
        public fixed int PartOffsets[1];
        
        public int DataOffset
        {
            get
            {
                return 44 + (NumParts << 2);
            }
        }
    }
	    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolyLineMRecordP
    {
        public ShapeType ShapeType;
        public Box bounds;
        public int NumParts;
        public int NumPoints;
        public fixed int PartOffsets[1];
        //points follow

        public int PointDataOffset
        {
            get
            {
                return 44 + (NumParts << 2);
            }
        }

        public int MeasureDataOffset
        {
            get
            {
                return 44 + (NumParts << 2) + (NumPoints<<4);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct MeasureP
    {
        public double MinM;
        public double MaxM;
        //measures follow
        public fixed double Measure[1];        
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct ZDataP
    {
        public double MinZ;
        public double MaxZ;
        //measures follow
        public fixed double ZData[1];
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolygonRecordP
    {
        public ShapeType ShapeType;        
        public Box bounds;
        public int NumParts;
        public int NumPoints;
        public fixed int PartOffsets[1];

        public int DataOffset
        {
            get
            {
                return 44 + (NumParts << 2);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PointRecordP
    {
        public ShapeType ShapeType;
        public double X;
        public double Y;       
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PointZRecordP
    {
        public ShapeType ShapeType;
        public double X;
        public double Y;
        public double Z;
        public double Measure;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct MultiPointRecordP
    {
        public ShapeType ShapeType;
        public Box bounds;
        public int NumPoints;
        public fixed double Points[2];        
    }
	    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolygonZRecordP
    {
        public ShapeType ShapeType;
        public Box bounds;
        public int NumParts;
        public int NumPoints;
        public fixed int PartOffsets[1];
        //points
        //bounding z range
        //z values
        //m range (optional)
        //measures (optional)

        public int PointDataOffset
        {
            get
            {
                return 44 + (NumParts << 2);
            }
        }

        public int ZDataOffset
        {
            get
            {
                //16 =>bounding z range
                return PointDataOffset + 16 + (NumPoints << 4);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolyLineZRecordP
    {
        public ShapeType ShapeType;
        public Box bounds;
        public int NumParts;
        public int NumPoints;
        public fixed int PartOffsets[1];
        //points
        //bounding z range
        //z values
        //m range (optional)
        //measures (optional)

        public int PointDataOffset
        {
            get
            {
                return 44 + (NumParts << 2);
            }
        }

        public int ZDataOffset
        {
            get
            {
                //16 =>bounding z range
                return PointDataOffset + (NumPoints << 4);
            }
        }

        
        public int ZMeasuresDataOffset
        {
            get
            {
                //16 => bounding m range (min/max measure)
                return ZDataOffset + 16 +  (NumPoints << 3);
            }
        }
    }


    #endregion


    #region "ShapeFileEx structs and classes"

   
	internal sealed class ShapeFileExConstants
	{
		public const int SHAPE_FILE_EX_MAIN_HEADER_LENGTH = 76;

		//public const int MAX_REC_LENGTH = 1<<23;//1<<20;
        public const int MAX_REC_LENGTH = 1 << 20;//1<<20;
        //public const int MAX_REC_LENGTH = 1 << 20;

        private ShapeFileExConstants()
        {
        }
	}  
   

#endregion

    #region base SFRecordCol class


    abstract class SFRecordCol
    {
        #region shared buffers
        private static byte[] sharedBuffer;// = new byte[ShapeFileExConstants.MAX_REC_LENGTH];
        private static Point[] sharedPointBuffer;// = new Point[ShapeFileExConstants.MAX_REC_LENGTH/8];
        private static int sharedBufferLength = ShapeFileExConstants.MAX_REC_LENGTH;

        internal static byte[] SharedBuffer
        {
            get
            {
                if (SingleThreaded)
                {
                    if (sharedBuffer == null) sharedBuffer = new byte[sharedBufferLength];
                    return sharedBuffer;
                }
                else return new byte[sharedBufferLength];
            }
        }

        internal static Point[] SharedPointBuffer
        {
            get
            {
                if (SingleThreaded)
                {
                    if (sharedPointBuffer == null) sharedPointBuffer = new Point[sharedBufferLength / 8];
                    return sharedPointBuffer;
                }
                else return new Point[sharedBufferLength / 8];                
            }
        }

        internal static void EnsureBufferSize(int requiredSize)
        {
            if (sharedBufferLength < requiredSize)
            {
                sharedBufferLength = requiredSize + 256;
                if (SingleThreaded)
                {
                    sharedBuffer = new byte[sharedBufferLength];
                    sharedPointBuffer = new Point[sharedBuffer.Length / 8];
                    System.GC.Collect();
                }
            }            
        }

        private int maxRecordLength = ShapeFileExConstants.MAX_REC_LENGTH;

        protected void SetMaxRecordLength(int length)
        {
            maxRecordLength = length + 256;
        }

        protected byte[] DataBuffer
        {
            get
            {
                if (SingleThreaded) return sharedBuffer;
                else return new byte[maxRecordLength];
            }
        }

        protected Point[] PointBuffer
        {
            get
            {
                if (SingleThreaded) return sharedPointBuffer;
                else return new Point[maxRecordLength/8];
            }
        }        

        #endregion
        
        internal const bool SingleThreaded = true;

        internal static bool MapFilesInMemory = true;
		
        public ShapeFileMainHeader MainHeader;
        public RecordHeader[] RecordHeaders;


        #region Selected Record stuff
        
        protected bool[] recordSelected;

        internal bool IsRecordSelected(int index)
        {
            if (recordSelected == null || index < 0 || index >= recordSelected.Length) return false;
            return recordSelected[index];
        }

        internal void SelectRecord(int index, bool selected)
        {
            if (recordSelected == null || index < 0 || index >= recordSelected.Length) return;
            recordSelected[index] = selected;
        }

        internal void ClearSelectedRecords()
        {
            Array.Clear(recordSelected, 0, recordSelected.Length);
        }


        #endregion


        #region Record Visibility

        protected bool[] recordVisible;

        internal bool IsRecordVisible(int index)
        {
            if (recordVisible == null || index < 0 || index >= recordVisible.Length) return false;
            return recordVisible[index];
        }

        internal void SetRecordVisible(int index, bool visible)
        {
            if (recordVisible == null || index < 0 || index >= recordVisible.Length) return;
            recordVisible[index] = visible;
        }

        internal void ClearRecordsVisibility()
        {
            for(int i= recordVisible.Length-1;i>=0;--i)
            {
                recordVisible[i] = true;
            }
        }


        #endregion

        
        public SFRecordCol(ref ShapeFileMainHeader head, RecordHeader[] recordHeaders)
        {
            if (recordHeaders == null) throw new ArgumentNullException("recordHeaders can not be null");
            this.MainHeader = head;
            this.RecordHeaders = recordHeaders;
            recordSelected = new bool[recordHeaders.Length];
            Array.Clear(recordSelected, 0, recordSelected.Length);
            recordVisible = new bool[recordHeaders.Length];
            ClearRecordsVisibility();
        }

		public abstract void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType);

		public abstract void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent);

        public void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, null, RectangleD.Empty);
        }


        public abstract List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream);
        public abstract List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer);

        public List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeDataD(recordIndex, shapeFileStream, SharedBuffer);
        }
        public abstract List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer);


        public abstract List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream);
        public abstract List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer);

        public List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeHeightDataD(recordIndex, shapeFileStream, SharedBuffer);
        }
        public abstract List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer);

        public List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeMDataD(recordIndex, shapeFileStream, SharedBuffer);
        }
        public abstract List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer);


        public abstract RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream);

        public abstract RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream);
        //{
        //    return GetRecordBounds(recordIndex, shapeFileStream);
        //}

        public abstract bool IntersectRect(int recordIndex, ref RectangleD rect, System.IO.Stream shapeFileStream);
        
        public abstract unsafe bool IntersectCircle(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream);

        public abstract double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream);

        public abstract double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo);


        #region static utility methods

        public static int GetMaxRecordLength(RecordHeader[] records)
        {
            int max = 0;
            for (int n = records.Length - 1; n >= 0; --n)
            {
                if (records[n].ContentLength > max) max = records[n].ContentLength;
            }
            return max;
        }

        //protected static bool RenderAllRecordsAtZoomLevel(float zoom, RenderSettings renderSettings)
        //{
        //    if (renderSettings == null) return true;
        //    return !(zoom < renderSettings.MinZoomLevel || (renderSettings.MaxZoomLevel > 0 && zoom > renderSettings.MaxZoomLevel));                        
        //}
        		

        protected static unsafe float GetPointsDAngle(byte* buf, int offset, int numPoints, ref int pointIndex, RectangleD extent, bool mercProj)
        {
            PointD[] pts = new PointD[numPoints];

            int maxIndex = -1;
            
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buf + offset);
            double maxDistSquared = 0f;

            while (ptIndex < numPoints)
            {
                pts[ptIndex] = mercProj ? LLToProjection(*(pPtr++)) : *(pPtr++); ;// LLToProjection(*(pPtr++));
                if (ptIndex > 0 && (extent.Contains(pts[ptIndex]) || extent.Contains(pts[ptIndex - 1])))
                {
                    double xdif = pts[ptIndex].X - pts[ptIndex - 1].X;
                    double ydif = pts[ptIndex].Y - pts[ptIndex - 1].Y;
                    double temp = xdif * xdif + ydif * ydif;
                    if (temp > maxDistSquared)
                    {
                        maxIndex = ptIndex - 1;
                        maxDistSquared = temp;
                    }
                }
                ptIndex++;
            }
            pointIndex = maxIndex;
            if (maxIndex >= 0)
            {
                return (float)(Math.Atan2((pts[maxIndex + 1].Y - pts[maxIndex].Y), (pts[maxIndex + 1].X - pts[maxIndex].X)) * 180f / Math.PI);
            }
            else
            {
                return float.NaN;
            }
        }
        

        protected static unsafe List<IndexAnglePair> GetPointsDAngle(byte* buf, int offset, int numPoints, float minDist, bool mercProj)
        {
            List<IndexAnglePair> indexAngleList = new List<IndexAnglePair>();
            PointD[] pts = new PointD[numPoints];
            
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buf + offset);
            float minDistSquared = minDist * minDist;

            while (ptIndex < numPoints)
            {
                pts[ptIndex] = mercProj ? LLToProjection(*(pPtr++)) : *(pPtr++);
                if (ptIndex > 0)
                {
                    double xdif = pts[ptIndex].X - pts[ptIndex - 1].X;
                    double ydif = pts[ptIndex].Y - pts[ptIndex - 1].Y;
                    double temp = xdif * xdif + ydif * ydif;
                    if (temp >= minDistSquared)
                    {
                        float angle = (float)(Math.Atan2((pts[ptIndex].Y - pts[ptIndex - 1].Y), (pts[ptIndex].X - pts[ptIndex - 1].X)) * 180f / Math.PI);
                        indexAngleList.Add(new IndexAnglePair(angle, ptIndex - 1));
                    }
                }
                ptIndex++;
            }
            return indexAngleList;
        }        

        private const double MaxLLMercProjD = 85.0511287798066;
        private const float MaxLLMercProjF = 85.0511287798066f;

        internal static PointF LLToProjection(PointF pt)
        {
            //if (MercProj)
            {
                if (pt.Y > MaxLLMercProjF)
                {
                    pt.Y = MaxLLMercProjF;
                }
                else if (pt.Y < -MaxLLMercProjF)
                {
                    pt.Y = -MaxLLMercProjF;
                }
                double d = (Math.PI/180) * pt.Y;
                double sd = Math.Sin(d);
        
                d = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
                return new PointF(pt.X, (float)d);
            }
            //return pt;
        }

        internal static PointD LLToProjection(PointD pt)
        {
            //if (MercProj)
            //{
                if (pt.Y > MaxLLMercProjD)
                {
                    pt.Y = MaxLLMercProjD;
                }
                else if (pt.Y < -MaxLLMercProjD)
                {
                    pt.Y = -MaxLLMercProjD;
                }
                double d = (Math.PI / 180) * pt.Y;
                double sd = Math.Sin(d);

                d = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
                return new PointD(pt.X, d);
            //}
            //return pt;
        }

        internal static void LLToProjection(ref double ptx, ref double pty, out double px, out double py)
        {
            px = ptx;
            //if (MercProj)
            {
                double d;
                if (pty > MaxLLMercProjD)
                {
                    d = (Math.PI / 180) * MaxLLMercProjD;
                }
                else if (pty < -MaxLLMercProjD)
                {
                    d = (Math.PI / 180) * (-MaxLLMercProjD);
                }
                else
                {
                    d = (Math.PI / 180) * pty;
                }
                double sd = Math.Sin(d);

                py = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
                return;
            }
            //py = pty;            
        }
        
        internal static PointD ProjectionToLL(PointD pt)
        {
            //if (MercProj)
            {
                double d = (Math.PI / 180) * pt.Y;
                d = Math.Atan(Math.Sinh(d));
                d = d * (180 / Math.PI);
                return new PointD(pt.X, d);
            }
            //return pt;
        }
        
        protected static unsafe Point[] GetPointsD(byte* buf, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY, bool mercProj)
        {
            Point[] pts = new Point[numPoints];
            
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buf + offset);
            while (ptIndex < numPoints)
            {
                PointD ptd = mercProj ? LLToProjection(*(pPtr++)) : *(pPtr++);
                if(ptIndex>0 &&(double.IsInfinity(ptd.X) || double.IsInfinity(ptd.Y)))
                {
                    pts[ptIndex] = pts[ptIndex-1];
                }
                else
                {
                    pts[ptIndex].X = (int)Math.Round((ptd.X + offX) * scaleX);
                    pts[ptIndex].Y = (int)Math.Round((ptd.Y + offY) * scaleY);
                }
                ++ptIndex;
            }
            return pts;
        }
               
        protected static unsafe Point[] GetPointsD(byte* buf, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY, ref Rectangle pixBounds, bool mercProj)
        {
            Point[] pts = new Point[numPoints];
            int left = int.MaxValue;
            int right = int.MinValue;
            int top = int.MaxValue;
            int bottom = int.MinValue;
            
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buf + offset);
            while (ptIndex < numPoints)
            {
                PointD ptf = mercProj ? LLToProjection(*(pPtr++)) : *(pPtr++); ;// LLToProjection(*(pPtr++));
                if (ptIndex>0 && (double.IsInfinity(ptf.X) || double.IsInfinity(ptf.Y)))
                {
                    pts[ptIndex] = pts[ptIndex - 1];
                }
                else
                {
                    pts[ptIndex].X = (int)Math.Round((ptf.X + offX) * scaleX);
                    pts[ptIndex].Y = (int)Math.Round((ptf.Y + offY) * scaleY);
                    left = Math.Min(pts[ptIndex].X, left);
                    right = Math.Max(pts[ptIndex].X, right);
                    top = Math.Min(pts[ptIndex].Y, top);
                    bottom = Math.Max(pts[ptIndex].Y, bottom);
                }
                ++ptIndex;
            }
            pixBounds = Rectangle.FromLTRB(left, top, right, bottom);
            return pts;
        }

        protected static unsafe void GetPointsRemoveDuplicatesD(byte* buf, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY, ref Rectangle pixBounds, Point[] pts, ref int usedPoints, bool mercProj)
        {
            //Point[] pts = new Point[numPoints];
            int left = int.MaxValue;
            int right = int.MinValue;
            int top = int.MaxValue;
            int bottom = int.MinValue;
            usedPoints = 1;
        
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buf + offset);

            PointD ptda = mercProj ? LLToProjection(*(pPtr++)) : *(pPtr++); //LLToProjection((*(pPtr++)));
            pts[ptIndex].X = (int)Math.Round((ptda.X + offX) * scaleX);
            pts[ptIndex].Y = (int)Math.Round((ptda.Y + offY) * scaleY);
            ++ptIndex;

            while (ptIndex < numPoints)
            {
                PointD ptf = mercProj ? LLToProjection(*(pPtr++)) : *(pPtr++); //LLToProjection((*(pPtr++)));
                if (!(double.IsInfinity(ptf.X) || double.IsInfinity(ptf.Y)))
                {
                    pts[usedPoints].X = (int)Math.Round((ptf.X + offX) * scaleX);
                    pts[usedPoints].Y = (int)Math.Round((ptf.Y + offY) * scaleY);
                    if ((pts[usedPoints].X != pts[usedPoints - 1].X) || (pts[usedPoints].Y != pts[usedPoints - 1].Y))
                    {
                        left = Math.Min(pts[usedPoints].X, left);
                        right = Math.Max(pts[usedPoints].X, right);
                        top = Math.Min(pts[usedPoints].Y, top);
                        bottom = Math.Max(pts[usedPoints].Y, bottom);
                        ++usedPoints;
                    }
                }
                ++ptIndex;
            }
            pixBounds = Rectangle.FromLTRB(left, top, right, bottom);
            if (usedPoints == 1)
            {
                pts[1] = pts[0];
            }
            // return pts;
        }
        
        protected static unsafe void GetPointsRemoveDuplicatesD(byte* dataPtr, int offset, int numPoints, ref double offX, ref double offY, ref double scale, Point[] pts, ref int usedPoints, bool MercProj)
        {
            int ptIndex = 0;
            usedPoints = 1;
            
            PointD* pPtr = (PointD*)(dataPtr + offset);
            PointD ptda = MercProj ? LLToProjection(*(pPtr++)) : *(pPtr++);
            pts[ptIndex].X = (int)Math.Round((ptda.X + offX) * scale);
            pts[ptIndex].Y = (int)Math.Round((ptda.Y + offY) * -scale);
            ++ptIndex;

            while (ptIndex < numPoints)
            {
                PointD ptd = MercProj ? LLToProjection(*(pPtr++)) : *(pPtr++);
                if (!(double.IsInfinity(ptd.X) || double.IsInfinity(ptd.Y)))
                {

                    pts[usedPoints].X = (int)Math.Round((ptd.X + offX) * scale);
                    pts[usedPoints].Y = (int)Math.Round((ptd.Y + offY) * -scale);

                    //if (pts[usedPoints].Y < -214648 && pts[usedPoints].X < -2147483)
                    //{
                    //    Console.Out.WriteLine("sdfdsf");
                    //}

                    if ((pts[usedPoints].X != pts[usedPoints - 1].X) || (pts[usedPoints].Y != pts[usedPoints - 1].Y))
                    {
                        ++usedPoints;
                    }
                }
                
                ++ptIndex;
            }
            if (usedPoints == 1)
            {
                pts[1] = pts[0];
            }
        }

        protected struct GPRDParams
        {
            public double offX;
            public double offY;
            public double scale;
            public int usedPoints;
            public bool MercProj;

            public GPRDParams(double offX, double offY, double scale, int usedPoints, bool MercProj)
            {
                this.offX = offX;
                this.offY = offY;
                this.scale = scale;
                this.usedPoints = usedPoints;
                this.MercProj = MercProj;
            }

        }

        protected static unsafe void GetPointsRemoveDuplicatesD(byte* dataPtr, int offset, int numPoints, Point[] pts, ref GPRDParams param)
        {
            int ptIndex = 0;
            param.usedPoints = 1;

            PointD* pPtr = (PointD*)(dataPtr + offset);
            PointD ptda = param.MercProj ? LLToProjection(*(pPtr++)) : *(pPtr++);
            pts[ptIndex].X = (int)Math.Round((ptda.X + param.offX) * param.scale);
            pts[ptIndex].Y = (int)Math.Round((ptda.Y + param.offY) * -param.scale);
            ++ptIndex;

            while (ptIndex < numPoints)
            {
                PointD ptd = param.MercProj ? LLToProjection(*(pPtr++)) : *(pPtr++);
                pts[param.usedPoints].X = (int)Math.Round((ptd.X + param.offX) * param.scale);
                pts[param.usedPoints].Y = (int)Math.Round((ptd.Y + param.offY) * -param.scale);
                if ((pts[param.usedPoints].X != pts[param.usedPoints - 1].X) || (pts[param.usedPoints].Y != pts[param.usedPoints - 1].Y))
                {
                    ++param.usedPoints;
                }
                ++ptIndex;
            }
            if (param.usedPoints == 1)
            {
                pts[1] = pts[0];
            }
        }

        protected static unsafe int ReducePoints(double* pts, int pointCount, double* outputPoints, double minDistance)
        {
            if (pointCount == 0) return 0;

            outputPoints[0] = pts[0];
            outputPoints[1] = pts[1];
            int inputIndex = 2;
            int outputIndex = 0;
            for (int n = 1; n < pointCount - 1; ++n)
            {                
                if (Math.Abs(pts[inputIndex] - outputPoints[outputIndex]) > minDistance ||
                    Math.Abs(pts[inputIndex + 1] - outputPoints[outputIndex + 1]) > minDistance)
                {
                    outputIndex += 2;
                    outputPoints[outputIndex] = pts[inputIndex];
                    outputPoints[outputIndex + 1] = pts[inputIndex + 1];
                }
                inputIndex += 2;
            }
            if (pointCount > 1)
            {
                outputIndex += 2;
                outputPoints[outputIndex] = pts[inputIndex];
                outputPoints[outputIndex + 1] = pts[inputIndex + 1];
            }

            return 1 + (outputIndex >> 1);
        }       

        protected static unsafe PointD[] GetPointsD(byte* buffer, int offset, int numPoints)
        {
            PointD[] pts = new PointD[numPoints];
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buffer + offset);
            while (ptIndex < numPoints)
            {
                pts[ptIndex++] = *(pPtr++);
            }
            
            return pts;
        }

        protected static unsafe PointF[] GetPointsDAsPointF(byte* buffer, int offset, int numPoints)
        {
            PointF[] pts = new PointF[numPoints];
            int ptIndex = 0;
            PointD* pPtr = (PointD*)(buffer + offset);
            while (ptIndex < numPoints)
            {
                pts[ptIndex].X = (float)(*(pPtr)).X;
                pts[ptIndex++].Y = (float)(*(pPtr++)).Y;
            }

            return pts;
        }
        

        protected static unsafe double[] GetDoubleData(byte* data, int offset, int numDoubles)
        {
            double[] doubleData = new double[numDoubles];            
            int ptIndex = 0;
            double* dPtr = (double*)(data + offset);
            while (ptIndex < numDoubles)
            {
                doubleData[ptIndex++] = *(dPtr++);
            }
            return doubleData;
        }

        protected static unsafe float[] GetFloatDataD(byte* data, int offset, int numDoubles)
        {
            float[] floatData = new float[numDoubles];
            int ptIndex = 0;
            double* dPtr = (double*)(data + offset);
            while (ptIndex < numDoubles)
            {
                floatData[ptIndex++] = (float)(*(dPtr++));
            }
            return floatData;
        }

        internal static int AutoHighThreshold = 1024 * 1024 * 1;
        internal static int AutoHighThresholdPoint = 1024 * 70;//150;
        
        protected bool UseGDI(RectangleF visibleExtent, RenderSettings renderSettings )
        {
            // Thanks to M Gerginski for suggestion
            if (renderSettings != null)
            {
                if (renderSettings.RenderQuality == RenderQuality.Low) return true;
                if (renderSettings.RenderQuality == RenderQuality.High) return false;
            }
            if (ShapeFile.RenderQuality == RenderQuality.Low) return true;
            else if (ShapeFile.RenderQuality == RenderQuality.High) return false;
            else
            {
                if (this.MainHeader.ShapeType != ShapeType.NullShape)
                {
                    int th;
                    if (this.MainHeader.ShapeType == ShapeType.Point)
                    {
                        th = AutoHighThresholdPoint;
                    }
                    else
                    {
                        th = AutoHighThreshold;
                    }
                    if (MainHeader.FileLength < th)
                    {
                        return false;
                    }
                    else
                    {
                        double t = (MainHeader.FileLength * (visibleExtent.Width * visibleExtent.Height) / ((this.MainHeader.Xmax - MainHeader.Xmin) * (MainHeader.Ymax - MainHeader.Ymin)));
                        bool b = 1.5*t > th;
                        //Console.Out.WriteLine("use gdi : " + b);
                        return b;
                    }
                }
                return false;                                
            }
        }

        protected static int ColorToGDIColor(Color c)
        {
            int color = 0;
            color = c.B & 0xff;
            color = (color << 8) | (c.G & 0xff);
            color = (color << 8) | (c.R & 0xff);
            return color;
        }


        protected enum BoundsTestResult
        {
            Undetermined,
            NoIntersection,
            Intersects
        }

        protected static BoundsTestResult TestBoundsIntersect(RectangleD subjectBounds, RectangleD testBounds, ICoordinateTransformation transformation)
        {
            if (transformation == null)
            {
                if (subjectBounds.IntersectsWith(testBounds)) return BoundsTestResult.Intersects;
                return BoundsTestResult.NoIntersection;
            }
            if (double.IsInfinity(subjectBounds.Width) || double.IsInfinity(subjectBounds.Height) ||
               double.IsInfinity(testBounds.Width) || double.IsInfinity(testBounds.Height)) return BoundsTestResult.Undetermined;

            RectangleD subjectTransformedBounds = subjectBounds.Transform(transformation);
            if (double.IsInfinity(subjectTransformedBounds.Width) || double.IsInfinity(subjectTransformedBounds.Height) || subjectTransformedBounds.Width < 0) return BoundsTestResult.Undetermined;

            if (subjectTransformedBounds.IntersectsWith(testBounds)) return BoundsTestResult.Intersects;
            return BoundsTestResult.NoIntersection;


        }

        protected static BoundsTestResult TestBoundsIntersect(RectangleD subjectBounds, RectangleD testBounds)
        {

            if (testBounds.Width < 0 || double.IsInfinity(subjectBounds.Width) || double.IsInfinity(subjectBounds.Height) ||
               double.IsInfinity(testBounds.Width) || double.IsInfinity(testBounds.Height)) return BoundsTestResult.Undetermined;

            if (subjectBounds.IntersectsWith(testBounds)) return BoundsTestResult.Intersects;
            return BoundsTestResult.NoIntersection;

        }

        protected static double CalculateSimplificationDistance(RectangleD recordBounds, double scaling, ICoordinateTransformation coordinateTransformation)
        {
            if (coordinateTransformation == null)
            {
                // simplificatoin distance = extent.Width /pixels
                //                         = width/(width*scaling)
                return scaling > double.Epsilon ? (1 / scaling) : 0;
            }

            if (recordBounds.Width < double.Epsilon || double.IsInfinity(recordBounds.Width) || double.IsInfinity(recordBounds.Height)) return 0;

            //RectangleD transformedExtent = ShapeFile.ConvertExtent(recordBounds, coordinateTransformation);
            RectangleD transformedExtent = recordBounds.Transform(coordinateTransformation);
            if (double.IsInfinity(transformedExtent.Width) || scaling <= double.Epsilon) return 0;
            return recordBounds.Width / (transformedExtent.Width * scaling);
        }


		protected static void DrawDirectionArrows(List<Point[]> pointList, int arrowLength, Pen arrowPen, Graphics g)
		{

			//use 10 pixel buffer either side of the arrow
			int minSegmentLength = (arrowLength + 20) * (arrowLength + 20);
			foreach (Point[] pts in pointList)
			{
				for (int n = 0; n < pts.Length - 1; ++n)
				{
					int length = (pts[n + 1].X - pts[n].X) * (pts[n + 1].X - pts[n].X) + (pts[n + 1].Y - pts[n].Y) * (pts[n + 1].Y - pts[n].Y);
					if (length >= minSegmentLength)
					{

						Point centre = new Point((pts[n].X + pts[n + 1].X) >> 1, (pts[n].Y + pts[n + 1].Y) >> 1);
						double lengthD = Math.Sqrt(length);
						double dx = (pts[n + 1].X - pts[n].X) / lengthD;
						double dy = (pts[n + 1].Y - pts[n].Y) / lengthD;

						PointF p0 = new PointF(centre.X - (float)(dx * (arrowLength >> 1)), centre.Y - (float)(dy * (arrowLength >> 1)));
						PointF p1 = new PointF(p0.X + (float)(dx * arrowLength), p0.Y + (float)(dy * arrowLength));
						g.DrawLine(arrowPen, p0, p1);
					}
				}

			}

		}


        /// <summary>
        /// removes any points that have infinite x or y coordinates
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        protected static unsafe int RemoveInfinitePoints(byte[] data, int offset, int numPoints)
        {                     
            byte[] tempData = new byte[numPoints * 16];
            int usedPoints = 0;
            fixed (byte* ptr = data)
            fixed (byte* tempPtr = tempData)
            {
                PointD* srcPoints = (PointD*)(ptr + offset);
                PointD* destPoints = (PointD*)tempPtr;
                for (int n = 0; n < numPoints; ++n)
                {
                    if (double.IsInfinity(srcPoints[n].X) || double.IsInfinity(srcPoints[n].Y)) continue;
                    destPoints[usedPoints++] = srcPoints[n];
                }
            }
            Array.Copy(tempData, 0, data, offset, usedPoints * 16);
            return usedPoints;                    
        }



        #endregion

    }

    internal struct IndexAnglePair
    {
        public float Angle;
        public int PointIndex;
        public IndexAnglePair(float angle, int pointIndex)
        {
            this.Angle = angle;
            this.PointIndex = pointIndex;
        }
    }
    
    

    #endregion


    #region Derived SFRecordCol classes

    class SFPolygonCol : SFRecordCol, QTNodeHelper
    {
        
        public SFPolygonCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            //this.RecordHeaders = recs;
            //SFRecordCol.EnsureBufferSize(SFRecordCol.GetMaxRecordLength(recs) + 100);
            int length = SFRecordCol.GetMaxRecordLength(recs) + 100;
            SFRecordCol.EnsureBufferSize(length);
            base.SetMaxRecordLength(length);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);            
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->DataOffset;
                    int numParts = nextRec->NumParts;
                    PointF[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsDAsPointF(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->DataOffset;
                    int numParts = nextRec->NumParts;
                    PointD[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }


        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            DateTime tick = DateTime.Now;
            if (this.UseGDI(extent, renderSettings))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

               //paintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
                paintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);

            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              

                paintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            System.Diagnostics.Debug.WriteLine("Render time: " + DateTime.Now.Subtract(tick).TotalSeconds + "s");
        }

        private unsafe void paintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            Pen gdiplusPen = null;
            Brush gdiplusBrush = null;
            bool renderInterior = true;

            Pen selectPen = null;
            Brush selectBrush = null;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);
            
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            if (double.IsNaN(scaleX) || Math.Abs(scaleX) < double.Epsilon)
            {
                simplificationDistance = 0;
            }
            else
            {
                simplificationDistance = 1 / scaleX;
            }

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;

            RectangleD testExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                testExtent = extent;
                //set simplificationDistance to zero if we're using a trnasformation - we'll calculate it 
                //when we render the first record
                simplificationDistance = 0;
            }


            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
            bool MercProj = projectionType == ProjectionType.Mercator;

            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(testExtent.Left, testExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(testExtent.Right, testExtent.Bottom));
                testExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }

            bool labelfields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            if (renderSettings != null) renderInterior = renderSettings.FillInterior;
            List<PartBoundsIndex> partBoundsIndexList = new List<PartBoundsIndex>(256);

           // g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
           // g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              

            try
            {
                // setup our pen and brush                
                gdiplusBrush = new SolidBrush(renderSettings.FillColor);
                gdiplusPen = new Pen(renderSettings.OutlineColor, 1);
                selectPen = new Pen(renderSettings.SelectOutlineColor, 4f);                
                gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                selectPen.DashStyle = renderSettings.LineDashStyle;
                selectBrush = new SolidBrush(renderSettings.SelectFillColor);
                byte* dataPtrZero=(byte*)IntPtr.Zero.ToPointer();
                //shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                if (fileMapped)
                {
                    dataPtrZero = (byte*)mapView.ToPointer();
                    dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                }
                else
                {
                    shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                }

                PointF pt = new PointF(0, 0);
                RecordHeader[] pgRecs = this.RecordHeaders;
                
                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                fixed (byte* dataBufPtr = data)
                {
                    if (!fileMapped) dataPtr = dataBufPtr;
                    
                    while (index < pgRecs.Length)
                    {

                        Rectangle partBounds = Rectangle.Empty;
                        if (fileMapped)
                        {
                            dataPtr = dataPtrZero + pgRecs[index].Offset;
                        }
                        else
                        {
                            if (shapeFileStream.Position != pgRecs[index].Offset)
                            {
                                Console.Out.WriteLine("offset wrong");
                                shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                            }
                            shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                        }
                        PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr + 8);// new PolygonRecordP(pgRecs[index]);
                        int dataOffset = nextRec->DataOffset;// nextRec.read(data, 8);
                        bool renderShape = recordVisible[index];
                        //overwrite if using CustomRenderSettings
                        if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                        RectangleD recordBounds = nextRec->bounds.ToRectangleD();
                        BoundsTestResult boundsTestResult = BoundsTestResult.Undetermined;// TestBoundsIntersect(recordBounds, testExtent);//, coordinateTransformation);
                        if (!recordBounds.IntersectsWith(testExtent)) boundsTestResult = BoundsTestResult.NoIntersection;
                        if (nextRec->ShapeType != ShapeType.NullShape &&
                            boundsTestResult != BoundsTestResult.NoIntersection && renderShape)
                        {
                            //check if the simplifiedDataBuffer sized needs to be increased
                            if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                            {
                                simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                            }

                            if (simplificationDistance <= double.Epsilon)
                            {
                                simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                            }

                            fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiplusPen = new Pen(customColor, 1);
                                        currentPenColor = customColor;
                                        gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiplusBrush = new SolidBrush(customColor);
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }
                                int numParts = nextRec->NumParts;
                                Point[] pts;
                                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                                gp.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

                                for (int partNum = 0; partNum < numParts; ++partNum)
                                {
                                    int numPoints;
                                    if ((numParts - partNum) > 1)
                                    {
                                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                    }
                                    else
                                    {
                                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                    }
                                    if (numPoints <= 1)
                                    {
                                        //Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                        continue;
                                    }

                                    //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                    int usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                    }

                                    if (usedPoints == 2)
                                    {
                                        simplifiedDataPtr[usedPoints << 1] = simplifiedDataPtr[0];
                                        simplifiedDataPtr[(usedPoints << 1) + 1] = simplifiedDataPtr[1];
                                        ++usedPoints;
                                    }

                                    pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, ref partBounds, MercProj);

                                    if (pts.Length == 3 && pts[0].X == pts[1].X && pts[0].Y == pts[1].Y && pts[0].X == pts[2].X && pts[0].Y == pts[2].Y)
                                    {
                                        //ensure the polygon ponts are not all identical or GDI+ throws an OutOfMemory exception
                                        //if the pen dashstyle is not solid. Also makes tiny polygons visible when zoomed out
                                        pts[1].Y = pts[1].Y + 1;                                        
                                    }
                                    


                                    if (partBounds.IntersectsWith(new Rectangle(0, 0, clientArea.Width, clientArea.Height)))
                                    {                                       
                                        if (partBounds.Left < -1000 || partBounds.Top < -1000 || partBounds.Right > clientArea.Width + 1000 || partBounds.Bottom > clientArea.Height + 1000)
                                        {
                                            //clip the polygon to avoid overflow
                                            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
                                            {
                                                XMin = 0,
                                                YMin = 0,
                                                XMax = clientArea.Width,
                                                YMax = clientArea.Height
                                            };
                                            List<Point> clippedPoints = new List<Point>();
                                            GeometryAlgorithms.PolygonClip(pts, pts.Length, clipBounds, clippedPoints);                                            
                                            pts = clippedPoints.ToArray();
                                        }

                                        if (pts.Length > 0)
                                        {
                                            gp.AddPolygon(pts);

                                            if (labelfields)
                                            {
                                                if (partBounds.Width > 5 && partBounds.Height > 5)
                                                {
                                                    partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                                }
                                            }
                                        }

                                    }
                                }
                                if (recordSelected[index])
                                {
                                    if (renderInterior)
                                    {
                                        g.FillPath(selectBrush, gp);
                                    }
                                    g.DrawPath(selectPen, gp);
                                }
                                else
                                {
                                    if (renderInterior)
                                    {
                                        g.FillPath(gdiplusBrush, gp);
                                    }
                                    g.DrawPath(gdiplusPen, gp);
                                }
                                gp.Dispose();
                            }
                        }
                        //else
                        //{
                        //    Console.Out.WriteLine("hq skipping record " + index);
                        //}
                        ++index;
                    }
                    //Console.Out.WriteLine("partPaintCount = " + partPaintCount);
                }
            }
            finally
            {
                if (gdiplusPen != null) gdiplusPen.Dispose();
                if (gdiplusBrush != null) gdiplusBrush.Dispose();
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
            if (labelfields)
            {

                PointF pt = new PointF(0, 0);
                int count = partBoundsIndexList.Count;
                Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                Color currentFontColor = renderSettings.FontColor;
                bool useCustomFontColor = customRenderSettings != null;
                for (int n = 0; n < count; n++)
                {
                    int index = partBoundsIndexList[n].RecIndex;
                    string strLabel = renderSettings.DbfReader.GetField(index, renderSettings.FieldIndex);
                    if (!string.IsNullOrEmpty(strLabel) && g.MeasureString(strLabel, renderSettings.Font).Width <= partBoundsIndexList[n].Bounds.Width)
                    {
                        pt.X = partBoundsIndexList[n].Bounds.Left + (partBoundsIndexList[n].Bounds.Width >> 1);
                        pt.Y = partBoundsIndexList[n].Bounds.Top + (partBoundsIndexList[n].Bounds.Height >> 1);
                        pt.X -= (g.MeasureString(strLabel, renderSettings.Font).Width / 2f);
                        if (useCustomFontColor)
                        {
                            Color customColor = customRenderSettings.GetRecordFontColor(index);
                            if (customColor.ToArgb() != currentFontColor.ToArgb())
                            {
                                fontBrush.Dispose();
                                fontBrush = new SolidBrush(customColor);
                                currentFontColor = customColor;
                            }
                        }
                        if (shadowText)
                        {
                            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                            gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, pt, StringFormat.GenericDefault);//new StringFormat());
                            g.DrawPath(pen, gp);
                            g.FillPath(fontBrush, gp);
                        }
                        else
                        {
                            g.DrawString(strLabel, renderSettings.Font, fontBrush, pt);
                            //g.DrawString(strLabel, renderer.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);                            
                        }
                    }
                }
                fontBrush.Dispose();
            }
        }


        private unsafe void paintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            IntPtr dc = IntPtr.Zero;
            IntPtr gdiBrush = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiBrush = IntPtr.Zero;
            IntPtr oldGdiPen = IntPtr.Zero;
            IntPtr selectPen = IntPtr.Zero;
            IntPtr selectBrush = IntPtr.Zero;  

            IntPtr fileMappingPtr = IntPtr.Zero;
            long fileSize = shapeFileStream.Length;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);
            bool renderInterior = true;                        

            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;

            if (double.IsNaN(scaleX) || Math.Abs(scaleX) < double.Epsilon)
            {
                simplificationDistance = 0;
            }
            else
            {
                simplificationDistance = 1 / scaleX;
            }
            

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);

            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            
            RectangleD testExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                testExtent = targetExtent;
            }

            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
            bool MercProj=projectionType == ProjectionType.Mercator;

            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(testExtent.Left, testExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(testExtent.Right, testExtent.Bottom));
                testExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }

            bool labelfields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            if (renderSettings != null) renderInterior = renderSettings.FillInterior;
            List<PartBoundsIndex> partBoundsIndexList = new List<PartBoundsIndex>(256);

            // obtain a handle to the Device Context so we can use GDI to perform the painting.            
            dc = g.GetHdc();

            try
            {
                // setup our pen and brush
                int color = ColorToGDIColor(renderSettings.FillColor);
                
                if (renderInterior)
                {
                    gdiBrush = NativeMethods.CreateSolidBrush(color);
                    oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                }
                else
                {
                    oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                }

                color = ColorToGDIColor(renderSettings.OutlineColor);                
                gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, color);
                oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 4, ColorToGDIColor(renderSettings.SelectOutlineColor));
                selectBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(renderSettings.SelectFillColor));

                NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                if (fileMapped)
                {
                    dataPtrZero = (byte*)mapView.ToPointer();
                    dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                }
                else
                {
                    shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                }
                
                PointF pt = new PointF(0, 0);
                RecordHeader[] pgRecs = this.RecordHeaders;
                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                fixed (byte* dataBufPtr = data)
                {
                    if (!fileMapped) dataPtr = dataBufPtr;
                        
                    while (index < pgRecs.Length)
                    {
                        Rectangle partBounds = Rectangle.Empty;
                            
                        if (fileMapped)
                        {
                            dataPtr = dataPtrZero + pgRecs[index].Offset;                            
                        }
                        else
                        {
                            if (shapeFileStream.Position != pgRecs[index].Offset)
                            {
                                Console.Out.WriteLine("offset wrong");
                                shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                            }
                            shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                        }
                        PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr+8);
                        
                        int dataOffset = nextRec->DataOffset;
                        bool renderShape = recordVisible[index];
                        //overwrite if using CustomRenderSettings
                        if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                        RectangleD recordBounds = nextRec->bounds.ToRectangleD();
                        BoundsTestResult boundsTestResult = TestBoundsIntersect(recordBounds, testExtent, coordinateTransformation);
                        if (nextRec->ShapeType != ShapeType.NullShape &&
                            boundsTestResult != BoundsTestResult.NoIntersection && renderShape)
                        {
                            //check if the simplifiedDataBuffer sized needs to be increased
                            if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                            {
                                simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                            }

                            if (simplificationDistance <= double.Epsilon)
                            {
                                simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                            }

                            fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, ColorToGDIColor(customColor));
                                        NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiBrush));
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }
                                int numParts = nextRec->NumParts;
                                for (int partNum = 0; partNum < numParts; ++partNum)
                                {
                                    int numPoints;
                                    if ((numParts - partNum) > 1)
                                    {
                                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                    }
                                    else
                                    {
                                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                    }
                                    if (numPoints <= 1)
                                    {
                                        //Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                        continue;
                                    }

                                    //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                    int usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                    if (coordinateTransformation != null)
                                    {
                                        int transformCount = coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                        //if (transformCount != usedPoints)
                                        //{
                                        //    Console.Out.WriteLine("transformcount={0}, usedpoints={1}", transformCount, usedPoints);
                                        //}
                                    }

                                    if (usedPoints == 2)
                                    {
                                        simplifiedDataPtr[usedPoints << 1] = simplifiedDataPtr[0];
                                        simplifiedDataPtr[(usedPoints << 1) + 1] = simplifiedDataPtr[1];
                                        ++usedPoints;
                                    }

                                    if (labelfields)
                                    {
                                        //GetPointsRemoveDuplicatesD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, offX, offY, scaleX, scaleY, ref partBounds, sharedPointBuffer, ref usedPoints, MercProj);
                                        GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, ref partBounds, sharedPointBuffer, ref usedPoints, MercProj);

                                        if (partBounds.Width > 5 && partBounds.Height > 5)
                                        {
                                            partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                        }
                                    }
                                    else
                                    {
                                        //GetPointsRemoveDuplicatesD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref offX, ref offY, ref scaleX, sharedPointBuffer, ref usedPoints, MercProj);
                                        GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr, 0, usedPoints, ref offX, ref offY, ref scaleX, sharedPointBuffer, ref usedPoints, MercProj);
                                    }


                                    if (recordSelected[index])
                                    {
                                        IntPtr tempPen = IntPtr.Zero;
                                        IntPtr tempBrush = IntPtr.Zero;
                                        try
                                        {
                                            tempPen = NativeMethods.SelectObject(dc, selectPen);
                                            if (renderInterior)
                                            {
                                                tempBrush = NativeMethods.SelectObject(dc, selectBrush);
                                            }
                                            NativeMethods.DrawPolygon(dc, sharedPointBuffer, usedPoints);
                                        }
                                        finally
                                        {
                                            if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                            if (tempBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, tempBrush);
                                        }
                                    }
                                    else
                                    {
                                        NativeMethods.DrawPolygon(dc, sharedPointBuffer, usedPoints);
                                    }

                                }
                            }
                        }
                        //else
                        //{
                        //    Console.Out.WriteLine("skipping record " + index);
                        //}
                        
                        ++index;
                    }
                }
            }
            finally
            {
                if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                if (selectBrush != IntPtr.Zero) NativeMethods.DeleteObject(selectBrush);
                if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);                    
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);

                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }

            if (labelfields)
            {

                PointF pt = new PointF(0, 0);
                int count = partBoundsIndexList.Count;
                Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                Color currentFontColor = renderSettings.FontColor;
                bool useCustomFontColor = customRenderSettings != null;
                for (int n = 0; n < count; n++)
                {
                    int index = partBoundsIndexList[n].RecIndex;
                    string strLabel = renderSettings.DbfReader.GetField(index, renderSettings.FieldIndex);
                    if (!string.IsNullOrEmpty(strLabel) && g.MeasureString(strLabel, renderSettings.Font).Width <= partBoundsIndexList[n].Bounds.Width)
                    {
                        pt.X = partBoundsIndexList[n].Bounds.Left + (partBoundsIndexList[n].Bounds.Width >> 1);
                        pt.Y = partBoundsIndexList[n].Bounds.Top + (partBoundsIndexList[n].Bounds.Height >> 1);
                        pt.X -= (g.MeasureString(strLabel, renderSettings.Font).Width / 2f);
                        if (useCustomFontColor)
                        {
                            Color customColor = customRenderSettings.GetRecordFontColor(index);
                            if (customColor.ToArgb() != currentFontColor.ToArgb())
                            {
                                fontBrush.Dispose();
                                fontBrush = new SolidBrush(customColor);
                                currentFontColor = customColor;
                            }
                        }
                        if (shadowText)
                        {
                            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                            gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, pt, StringFormat.GenericDefault);//new StringFormat());
                            g.DrawPath(pen, gp);
                            g.FillPath(fontBrush, gp);
                        }
                        else
                        {
                            g.DrawString(strLabel, renderSettings.Font, fontBrush, pt);
                            //g.DrawString(strLabel, renderer.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);                            
                        }
                    }
                }
                fontBrush.Dispose();
            }
        }

        //enum BoundsTestResult
        //{
        //    Undetermined,
        //    NoIntersection,
        //    Intersects            
        //}

        //private BoundsTestResult TestBoundsIntersect(RectangleD subjectBounds, RectangleD testBounds, ICoordinateTransformation transformation)
        //{
        //    if (transformation == null)
        //    {
        //        if(subjectBounds.IntersectsWith(testBounds)) return BoundsTestResult.Intersects;
        //        return BoundsTestResult.NoIntersection;
        //    }
        //    if(double.IsInfinity(subjectBounds.Width) || double.IsInfinity(subjectBounds.Height) ||
        //       double.IsInfinity(testBounds.Width) || double.IsInfinity(testBounds.Height) ) return BoundsTestResult.Undetermined;
           
        //    RectangleD subjectTransformedBounds = subjectBounds.Transform(transformation);
        //    if (double.IsInfinity(subjectTransformedBounds.Width) || double.IsInfinity(subjectTransformedBounds.Height) || subjectTransformedBounds.Width < 0) return BoundsTestResult.Undetermined;

        //    if (subjectTransformedBounds.IntersectsWith(testBounds)) return BoundsTestResult.Intersects;
        //    return BoundsTestResult.NoIntersection;


        //}

        //private BoundsTestResult TestBoundsIntersect(RectangleD subjectBounds, RectangleD testBounds)
        //{
            
        //    if (testBounds.Width < 0  || double.IsInfinity(subjectBounds.Width) || double.IsInfinity(subjectBounds.Height) ||
        //       double.IsInfinity(testBounds.Width) || double.IsInfinity(testBounds.Height)) return BoundsTestResult.Undetermined;
            
        //    if (subjectBounds.IntersectsWith(testBounds)) return BoundsTestResult.Intersects;
        //    return BoundsTestResult.NoIntersection;

        //}

       
        internal unsafe bool ContainsPoint(int shapeIndex, PointD pt, System.IO.Stream shapeFileStream)
        {
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength+8);           

            bool inPolygon = false;
            fixed (byte* dataPtr = data)
            {
                PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr+8);
                int dataOffset = nextRec->DataOffset;//nextRec.read(data, 8);
                int numParts = nextRec->NumParts;

                bool infiniteBounds = double.IsInfinity(nextRec->bounds.xmin) || double.IsInfinity(nextRec->bounds.ymin) |
                    double.IsInfinity(nextRec->bounds.xmax) || double.IsInfinity(nextRec->bounds.ymax);                

                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }                    
                    bool isHole = false;
                    if (infiniteBounds)
                    {
                        numPoints = RemoveInfinitePoints(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                    }
                    bool partInPolygon = GeometryAlgorithms.PointInPolygon(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, pt.X, pt.Y, numParts == 1, ref isHole); //ignore holes if only 1 part
                    inPolygon |= partInPolygon;
                    if (isHole && partInPolygon)
                    {
                        return false;
                    }
                }
            }
            return inPolygon;            
        }
        
        public override unsafe bool IntersectRect(int shapeIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with rect
            if (!GetRecordBoundsD(shapeIndex, shapeFileStream).IntersectsWith(rect)) return false;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                bool intersectsNonHolePolygon = false;
                PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;

                bool infiniteBounds = double.IsInfinity(nextRec->bounds.xmin) || double.IsInfinity(nextRec->bounds.ymin) |
                   double.IsInfinity(nextRec->bounds.xmax) || double.IsInfinity(nextRec->bounds.ymax);

                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    bool withinHole, isHole;
                    if (infiniteBounds)
                    {
                        numPoints = RemoveInfinitePoints(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                    }
                    if (IntersectsRect(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref rect, out withinHole, out isHole ))
                    {
                        intersectsNonHolePolygon = true;
                        if (isHole) return true; //if the polygon is a hole and the rect is not within the hole then it intersects
                    }
                    if (withinHole) return false; //if the rect is within a hole then return false
                }
                return intersectsNonHolePolygon;
            }
            //return false;
        }

        
        private static unsafe bool IntersectsRect(byte[] data, int offset, int numPoints, ref RectangleD rect, out bool withinHole, out bool isHole)
        {
            withinHole = isHole = false;
            if (GeometryAlgorithms.IsPolygonHole(data, offset, numPoints))
            {
                isHole = true;
                //check if the 
                fixed (byte* ptr = data)
                {
                    withinHole =  NativeMethods.RectWithinPolygon(rect.Left, rect.Top, rect.Right, rect.Bottom, ptr + offset, numPoints);
                }                
            }
            //the rect is inside the hole - return false
            if (withinHole) return false;

            fixed (byte* ptr = data)
            {
                return NativeMethods.PolygonRectIntersect(ptr + offset, numPoints, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }


        public override unsafe bool IntersectCircle(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                bool intersectsNonHolePolygon = false;                
                PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;

                bool infiniteBounds = double.IsInfinity(nextRec->bounds.xmin) || double.IsInfinity(nextRec->bounds.ymin) |
                  double.IsInfinity(nextRec->bounds.xmax) || double.IsInfinity(nextRec->bounds.ymax);

                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    bool withinHole;

                    if (infiniteBounds)
                    {
                        numPoints = RemoveInfinitePoints(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                    }

                    if (GeometryAlgorithms.PolygonCircleIntersects(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, radius, numParts==1, out withinHole))
                    {
                        intersectsNonHolePolygon = true;
                        if (GeometryAlgorithms.IsPolygonHole(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints))
                        {
                            return true; //if the polygon is a hole and the circle intersects the hole then the record intersects
                        }                                    
                    }
                    if (withinHole) return false; //if the circle is within a hole then return false                    
                }
                return intersectsNonHolePolygon;
            }
            //return false;
        }

        public unsafe override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            //RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            double minDistance = double.PositiveInfinity;
            fixed (byte* dataPtr = data)
            {
                PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    double partDistance = GeometryAlgorithms.DistanceToPolygon(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, numParts == 1);
                    if(partDistance < minDistance)
                    {
                        minDistance = partDistance;
                    }
                }
            }
            return minDistance;
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            return GetDistanceToShape(shapeIndex, centre, radius, shapeFileStream);
        }

        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset,SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);
                    
            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleD();
        }

        #endregion

        private struct PartBoundsIndex
        {
            public int RecIndex;
            public Rectangle Bounds;

            public PartBoundsIndex(int index, Rectangle r)
            {
                RecIndex = index;
                Bounds = r;
            }
        }
    }

    class SFPointCol : SFRecordCol, QTNodeHelper
    {
        //internal RecordHeader[] recordHeaders;

        public SFPointCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            //this.RecordHeaders = recs;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointRecordP* nextRec = (PointRecordP*)(dataPtr + 8);
                    dataList.Add(new PointF[] { new PointF((float)nextRec->X, (float)nextRec->Y) });
                }
            }
            return dataList;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return GetShapeData(recordIndex, shapeFileStream);
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointRecordP* nextRec = (PointRecordP*)(dataPtr + 8);
                    dataList.Add(new PointD[] { new PointD(nextRec->X, nextRec->Y) });
                }
            }
            return dataList;
        }

        public PointD GetPointD(int recordIndex, Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            unsafe
            {
                byte[] data = SFRecordCol.SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointRecordP* nextRec = (PointRecordP*)(dataPtr + 8);
                    return new PointD(nextRec->X, nextRec->Y);
                }
            }
            
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            bool useGDI = (this.UseGDI(extent, renderSettings) && renderSettings.GetImageSymbol()==null);

            if (useGDI)
            {
                PaintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
                
                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                //g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                //PaintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);                  
            }
            else
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                PaintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
        }

        public unsafe void PaintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
     
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }

            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            
            try
            {
                Image symbol = null;
                Size symbolSize = Size.Empty;

                float pointSize = 6f;

                if (renderSettings != null)
                {
                    renderInterior = renderSettings.FillInterior;
                    pointSize = renderSettings.PointSize;
                    symbol = renderSettings.GetImageSymbol();
                    if (symbol != null)
                    {
                        symbolSize = symbol.Size;                       
                    }
                    else
                    {
                        symbolSize = new Size((int)pointSize, (int)pointSize);
                    }
                }


                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;

                bool drawPoint = (symbol == null && !useCustomImageSymbols);
                bool MercProj=projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }
                                
                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Brush fillBrush = null;
                Pen outlinePen = null;
                Pen selectPen = null;
                Brush selectBrush = null;
                
                fillBrush = new SolidBrush(renderSettings.FillColor);
                outlinePen = new Pen(renderSettings.OutlineColor, 1f);
                selectPen = new Pen(renderSettings.SelectOutlineColor, 2f);
                selectBrush = new SolidBrush(renderSettings.SelectFillColor);
                outlinePen.DashStyle = renderSettings.LineDashStyle;
                selectPen.DashStyle = renderSettings.LineDashStyle;
                                
                try
                {
                    int index = 0;
                    float halfPointSize = pointSize * 0.5f;
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    double[] recPt= new double[2];
                    byte[] buffer = SharedBuffer;
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;

                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }
                            PointRecordP* nextRec = (PointRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.Contains(nextRec->X, nextRec->Y) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        outlinePen = new Pen(customColor, 1);
                                        currentPenColor = customColor;
                                        outlinePen.DashStyle = renderSettings.LineDashStyle;                                        
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            fillBrush = new SolidBrush(customColor);
                                            currentBrushColor = customColor;
                                        }
                                    }
                                    if (useCustomImageSymbols)
                                    {
                                        symbol = customRenderSettings.GetRecordImageSymbol(index);
                                        if (symbol != null)
                                        {
                                            symbolSize = symbol.Size;
                                            drawPoint = false;
                                        }
                                        else
                                        {
                                            drawPoint = true;
                                        }
                                    }
                                }
                                
                                recPt[0] = nextRec->X;
                                recPt[1] = nextRec->Y;
                                if (coordinateTransformation != null)
                                {
                                    coordinateTransformation.Transform(recPt, 1);
                                }

                                if (MercProj)
                                {
                                    LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                }
                                                               
                                PointF pt = new PointF((float)((recPt[0] + offX) * scaleX), (float)((recPt[1] + offY) * scaleY));
                                if (drawPoint)
                                {
                                    if (pointSize > 0)
                                    {
                                        if (renderInterior)
                                        {
                                            if (recordSelected[index])
                                            {
                                                g.FillEllipse(selectBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                            else
                                            {
                                                g.FillEllipse(fillBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                        }
                                        if (recordSelected[index])
                                        {
                                            g.DrawEllipse(selectPen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                        }
                                        else
                                        {
                                            g.DrawEllipse(outlinePen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                        }
                                    }
                                    if (labelFields)
                                    {
                                        renderPtObjList.Add(new RenderPtObj(pt, index, (int)halfPointSize + 5, -((int)halfPointSize + 5)));
                                    }
                                }
                                else
                                {
									//Console.Out.WriteLine()
									g.DrawImage(symbol, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1), symbolSize.Width, symbolSize.Height);
									//g.DrawImage(symbol, pt.X, pt.Y);
									if (recordSelected[index])
                                    {
                                        g.DrawRectangle(selectPen, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1), symbolSize.Width, symbolSize.Height);
                                    }
                                    if (labelFields)
                                    {
                                        renderPtObjList.Add(new RenderPtObj(pt, index, ((symbolSize.Width + 1) >> 1) + 5, -(((symbolSize.Height + 1) >> 1) + 5)));
                                    }
                                }


                            }
                            //if (fileMapped)
                            //{
                            //    dataPtr += this.RecordHeaders[index].ContentLength + 8;
                            //}
                            index++;
                        }
                    }
                }
                finally
                {
                    if (fillBrush != null) fillBrush.Dispose();
                    if (outlinePen != null) outlinePen.Dispose();
                    if (selectPen != null) selectPen.Dispose();
                    if (selectBrush != null) selectBrush.Dispose();
                }
                
                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }

        public unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
           
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;            
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }
            
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {
                
                float pointSize = 6f;                
                renderInterior = renderSettings.FillInterior;
                pointSize = renderSettings.PointSize;                    
                
                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }
                
                IntPtr dc = IntPtr.Zero;
                IntPtr gdiPen = IntPtr.Zero;
                IntPtr gdiBrush = IntPtr.Zero;
                IntPtr oldGdiPen = IntPtr.Zero;
                IntPtr oldGdiBrush = IntPtr.Zero;

                IntPtr selectPen = IntPtr.Zero;
                IntPtr selectBrush = IntPtr.Zero;
                
                // obtain a handle to the Device Context so we can use GDI to perform the painting.
                dc = g.GetHdc();

                try
                {
                    int color = 0x70c09b;
                    
                    color = ColorToGDIColor(renderSettings.FillColor);
                    if (renderInterior)
                    {
                        gdiBrush = NativeMethods.CreateSolidBrush(color);
                        oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                    }
                    else
                    {
                        oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                    }

                    color = ColorToGDIColor(renderSettings.OutlineColor);
                    gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, color);
                    oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                    selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 2, ColorToGDIColor(renderSettings.SelectOutlineColor));
                    selectBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(renderSettings.SelectFillColor));

                    NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                    int index = 0;
                    Point pt = new Point();
                    int halfPointSize = (int)Math.Round(pointSize * 0.5);
                    int pointSizeInt = (int)Math.Round(pointSize);
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    byte[] buffer = SharedBuffer;
                    double[] recPt = { 0, 0 };
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;
                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    //Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }
                            
                            PointRecordP* nextRec = (PointRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.Contains(nextRec->X, nextRec->Y) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, ColorToGDIColor(customColor));
                                        NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiBrush));
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }

                                recPt[0] = nextRec->X;
                                recPt[1] = nextRec->Y;

                                if (coordinateTransformation != null)
                                {
                                    coordinateTransformation.Transform(recPt,1);                                    
                                }

                                //PointD ptf = LLToProjection(new PointD(nextRec.X,nextRec.Y));
                                if (MercProj)
                                {
                                    LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                }                                

                                pt.X = (int)Math.Round((recPt[0] + offX) * scaleX);
                                pt.Y = (int)Math.Round((recPt[1] + offY) * scaleY);

                                if (pointSizeInt > 0)
                                {
                                    if (recordSelected[index])
                                    {
                                        IntPtr tempPen = IntPtr.Zero;
                                        IntPtr tempBrush = IntPtr.Zero;
                                        try
                                        {
                                            tempPen = NativeMethods.SelectObject(dc, selectPen);
                                            if (renderInterior)
                                            {
                                                tempBrush = NativeMethods.SelectObject(dc, selectBrush);
                                            }
                                            NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                        }
                                        finally
                                        {
                                            if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                            if (tempBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, tempBrush);
                                        }                                        
                                    }
                                    else
                                    {
                                        NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                    }
                                }
                                if (labelFields)
                                {
                                    renderPtObjList.Add(new RenderPtObj(pt, index, halfPointSize + 5, -(halfPointSize + 5)));
                                }
                            }
                            ++index;
                        }
                    }
                }
                finally
                {
                    if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                    if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                    if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                    if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                    if (selectBrush != IntPtr.Zero) NativeMethods.DeleteObject(selectBrush);
                    if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                    g.ReleaseHdc(dc);
                }
                
                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }

        

        private struct RenderPtObj
        {
            public PointF Pt;
            public int RecordIndex;

            public int offX, offY;

            public RenderPtObj(PointF p, int recordIndexParam, int x0, int y0)
            {
                Pt = p;
                RecordIndex = recordIndexParam;
                offX = x0;
                offY = y0;
            }

        }


        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return true;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            //PointD pt = this.GetPointD(recordIndex, shapeFileStream);
            //return new PointF((float)pt.X, (float)pt.Y);
            return this.GetPointD(recordIndex, shapeFileStream);
        }

        public override RectangleF GetRecordBounds(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            PointD pt = this.GetPointD(recordIndex, shapeFileStream);
            return new RectangleF((float)pt.X, (float)pt.Y, float.Epsilon, float.Epsilon);
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            PointD pt = this.GetPointD(recordIndex, shapeFileStream);
            return new RectangleD(pt.X, pt.Y, double.Epsilon, double.Epsilon);
        }

        public override bool IntersectRect(int recordIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            return rect.Contains(GetPointD(recordIndex, shapeFileStream));
        }

        public override bool IntersectCircle(int recordIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            PointD pt = GetPointD(recordIndex, shapeFileStream);

            return (pt.X - centre.X) * (pt.X - centre.X) + (pt.Y - centre.Y) * (pt.Y - centre.Y) < (radius * radius);
        }
        
       

        #endregion

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            PointD pt = GetPointD(shapeIndex, shapeFileStream);
            double dx = (pt.X - centre.X);
            double dy = (pt.Y - centre.Y);
            return Math.Sqrt(dx * dx + dy * dy);                
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            PointD pt = GetPointD(shapeIndex, shapeFileStream);
            double dx = (pt.X - centre.X);
            double dy = (pt.Y - centre.Y);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }
    }

    class SFPointZCol : SFRecordCol, QTNodeHelper
    {
     
        public SFPointZCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                    dataList.Add(new PointF[] { new PointF((float)nextRec->X, (float)nextRec->Y) });
                }
            }
            return dataList;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return GetShapeData(recordIndex, shapeFileStream);
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                    dataList.Add(new PointD[] { new PointD(nextRec->X, nextRec->Y) });
                }
            }
            return dataList;
        }

        public PointD GetPointD(int recordIndex, Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            unsafe
            {
                byte[] data = SFRecordCol.SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                    return new PointD(nextRec->X, nextRec->Y);
                }
            }

        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeHeightData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<float[]> dataList = new List<float[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                    dataList.Add(new float[] { (float)nextRec->Z});
                }
            }
            return dataList;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<double[]> dataList = new List<double[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                    dataList.Add(new double[] { nextRec->Z });
                }
            }
            return dataList;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            throw new NotImplementedException("GetShapeMDataD has not been implemented for PointZ shape type");
        }

        public override bool IntersectRect(int shapeIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            return rect.Contains(GetPointD(shapeIndex, shapeFileStream));
        }

        public override bool IntersectCircle(int recordIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            PointD pt = GetPointD(recordIndex, shapeFileStream);
            return (pt.X - centre.X) * (pt.X - centre.X) + (pt.Y - centre.Y) * (pt.Y - centre.Y) < (radius * radius);
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            PointD pt = GetPointD(shapeIndex, shapeFileStream);
            double dx = (pt.X - centre.X);
            double dy = (pt.Y - centre.Y);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            PointD pt = GetPointD(shapeIndex, shapeFileStream);
            double dx = (pt.X - centre.X);
            double dy = (pt.Y - centre.Y);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #region paint methods
        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            bool useGDI = (this.UseGDI(extent, renderSettings) && renderSettings.GetImageSymbol() == null);

            if (useGDI)
            {
                PaintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
                PaintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
        }


        public unsafe void PaintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
            
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }
            
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {
                Image symbol = null;
                Size symbolSize = Size.Empty;

                float pointSize = 6f;

                if (renderSettings != null)
                {
                    renderInterior = renderSettings.FillInterior;
                    pointSize = renderSettings.PointSize;
                    symbol = renderSettings.GetImageSymbol();
                    if (symbol != null)
                    {
                        symbolSize = symbol.Size;
                    }
                    else
                    {
                        symbolSize = new Size((int)pointSize, (int)pointSize);
                    }
                }

                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;

                bool drawPoint = (symbol == null && !useCustomImageSymbols);
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Brush fillBrush = null;
                Pen outlinePen = null;
                Pen selectPen = null;
                Brush selectBrush = null;

                fillBrush = new SolidBrush(renderSettings.FillColor);
                outlinePen = new Pen(renderSettings.OutlineColor, 1f);
                selectPen = new Pen(renderSettings.SelectOutlineColor, 2f);
                selectBrush = new SolidBrush(renderSettings.SelectFillColor);
                outlinePen.DashStyle = renderSettings.LineDashStyle;
                selectPen.DashStyle = renderSettings.LineDashStyle;                

                try
                {
                    int index = 0;
                    float halfPointSize = pointSize * 0.5f;
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    //shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    byte[] buffer = SharedBuffer;
                    double[] recPt = new double[2];
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;

                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }
                            PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.Contains(nextRec->X, nextRec->Y) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        outlinePen = new Pen(customColor, 1);
                                        currentPenColor = customColor;
                                        outlinePen.DashStyle = renderSettings.LineDashStyle;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            fillBrush = new SolidBrush(customColor);
                                            currentBrushColor = customColor;
                                        }
                                    }
                                    if (useCustomImageSymbols)
                                    {
                                        symbol = customRenderSettings.GetRecordImageSymbol(index);
                                        if (symbol != null)
                                        {
                                            symbolSize = symbol.Size;
                                            drawPoint = false;
                                        }
                                        else
                                        {
                                            drawPoint = true;
                                        }
                                    }
                                }
                                recPt[0] = nextRec->X;
                                recPt[1] = nextRec->Y;
                                if (coordinateTransformation != null)
                                {
                                    coordinateTransformation.Transform(recPt, 1);
                                }

                                if (MercProj)
                                {
                                    LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                }

                                PointF pt = new PointF((float)((recPt[0] + offX) * scaleX), (float)((recPt[1] + offY) * scaleY));
                                
                                if (drawPoint)
                                {
                                    if (pointSize > 0)
                                    {
                                        if (renderInterior)
                                        {
                                            if (recordSelected[index])
                                            {
                                                g.FillEllipse(selectBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                            else
                                            {
                                                g.FillEllipse(fillBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                        }
                                        if (recordSelected[index])
                                        {
                                            g.DrawEllipse(selectPen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                        }
                                        else
                                        {
                                            g.DrawEllipse(outlinePen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                        }
                                    }
                                    if (labelFields)
                                    {
                                        renderPtObjList.Add(new RenderPtObj(pt, index, (int)halfPointSize + 5, -((int)halfPointSize + 5)));
                                    }
                                }
                                else
                                {
                                    g.DrawImage(symbol, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1));
                                    if (recordSelected[index])
                                    {
                                        g.DrawRectangle(selectPen, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1), symbolSize.Width, symbolSize.Height);
                                    }
                                    if (labelFields)
                                    {
                                        renderPtObjList.Add(new RenderPtObj(pt, index, ((symbolSize.Width + 1) >> 1) + 5, -(((symbolSize.Height + 1) >> 1) + 5)));
                                    }
                                }


                            }
                            //if (fileMapped)
                            //{
                            //    dataPtr += this.RecordHeaders[index].ContentLength + 8;
                            //}
                            index++;
                        }
                    }
                }
                finally
                {
                    if (fillBrush != null) fillBrush.Dispose();
                    if (outlinePen != null) outlinePen.Dispose();
                    if (selectPen != null) selectPen.Dispose();
                    if (selectBrush != null) selectBrush.Dispose();
                }

                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }

        public unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
         
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }

            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {

                float pointSize = 6f;
                renderInterior = renderSettings.FillInterior;
                pointSize = renderSettings.PointSize;

                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }

                IntPtr dc = IntPtr.Zero;
                IntPtr gdiPen = IntPtr.Zero;
                IntPtr gdiBrush = IntPtr.Zero;
                IntPtr oldGdiPen = IntPtr.Zero;
                IntPtr oldGdiBrush = IntPtr.Zero;

                IntPtr selectPen = IntPtr.Zero;
                IntPtr selectBrush = IntPtr.Zero;

                // obtain a handle to the Device Context so we can use GDI to perform the painting.
                dc = g.GetHdc();

                try
                {
                    int color = 0x70c09b;

                    color = ColorToGDIColor(renderSettings.FillColor);
                    if (renderInterior)
                    {
                        gdiBrush = NativeMethods.CreateSolidBrush(color);
                        oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                    }
                    else
                    {
                        oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                    }

                    color = ColorToGDIColor(renderSettings.OutlineColor);
                    gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, color);
                    oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                    selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 2, ColorToGDIColor(renderSettings.SelectOutlineColor));
                    selectBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(renderSettings.SelectFillColor));

                    NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                    int index = 0;
                    Point pt = new Point();
                    int halfPointSize = (int)Math.Round(pointSize * 0.5);
                    int pointSizeInt = (int)Math.Round(pointSize);
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    byte[] buffer = SharedBuffer;
                    double[] recPt = { 0, 0 };
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;
                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }
                            PointZRecordP* nextRec = (PointZRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.Contains(nextRec->X, nextRec->Y) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, ColorToGDIColor(customColor));
                                        NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiBrush));
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }

                                recPt[0] = nextRec->X;
                                recPt[1] = nextRec->Y;

                                if (coordinateTransformation != null)
                                {
                                    coordinateTransformation.Transform(recPt, 1);
                                }

                                if (MercProj)
                                {
                                    LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                }

                                pt.X = (int)Math.Round((recPt[0] + offX) * scaleX);
                                pt.Y = (int)Math.Round((recPt[1] + offY) * scaleY);


                                //double px, py;
                                //if (MercProj)
                                //{
                                //    LLToProjection(ref nextRec->X, ref nextRec->Y, out px, out py);
                                //}
                                //else
                                //{
                                //    px = nextRec->X;
                                //    py = nextRec->Y;
                                //}
                                //pt.X = (int)Math.Round((px + offX) * scaleX);
                                //pt.Y = (int)Math.Round((py + offY) * scaleY);

                                if (pointSizeInt > 0)
                                {
                                    if (recordSelected[index])
                                    {
                                        IntPtr tempPen = IntPtr.Zero;
                                        IntPtr tempBrush = IntPtr.Zero;
                                        try
                                        {
                                            tempPen = NativeMethods.SelectObject(dc, selectPen);
                                            if (renderInterior)
                                            {
                                                tempBrush = NativeMethods.SelectObject(dc, selectBrush);
                                            }
                                            NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                        }
                                        finally
                                        {
                                            if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                            if (tempBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, tempBrush);
                                        }
                                    }
                                    else
                                    {
                                        NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                    }
                                }
                                if (labelFields)
                                {
                                    renderPtObjList.Add(new RenderPtObj(pt, index, halfPointSize + 5, -(halfPointSize + 5)));
                                }
                            }
                            //if (fileMapped)
                            //{
                            //    dataPtr += this.RecordHeaders[index].ContentLength + 8;
                            //}
                            ++index;
                        }
                    }
                }
                finally
                {
                    if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                    if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                    if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                    if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                    if (selectBrush != IntPtr.Zero) NativeMethods.DeleteObject(selectBrush);
                    if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                    g.ReleaseHdc(dc);
                }

                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }
        
        private struct RenderPtObj
        {
            public PointF Pt;
            public int RecordIndex;

            public int offX, offY;

            public RenderPtObj(PointF p, int recordIndexParam, int x0, int y0)
            {
                Pt = p;
                RecordIndex = recordIndexParam;
                offX = x0;
                offY = y0;
            }

        }
        
        #endregion

        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return true;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            //PointD pt = this.GetPointD(recordIndex, shapeFileStream);
            //return new PointF((float)pt.X, (float)pt.Y);
            return this.GetPointD(recordIndex, shapeFileStream);
        }

        public override RectangleF GetRecordBounds(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            PointD pt = this.GetPointD(recordIndex, shapeFileStream);
            return new RectangleF((float)pt.X, (float)pt.Y, float.Epsilon, float.Epsilon);
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            PointD pt = this.GetPointD(recordIndex, shapeFileStream);
            return new RectangleD(pt.X, pt.Y, double.Epsilon, double.Epsilon);
        }

        #endregion
    }

    internal struct RenderPtObj
    {
        public Point Point0;
        public float Point0Dist;
        public float Angle0;
        
        public int RecordIndex;

        public RenderPtObj(Point p0, float poDist, float p0Angle, /*Point p1, float p1Dist, float p1Angle,*/ int recordIndex)
        {
            Point0 = p0;
            Point0Dist = poDist;
            Angle0 = p0Angle;
            RecordIndex = recordIndex;
        }


        public static int AddRenderPtObjects(Point[] pts, int numPoints, List<RenderPtObj> renderPoints, int recordIndex, float minDist)
        {

            int count = renderPoints.Count;
            int ptIndex = 0;
            float minDistSquared = minDist * minDist;

            while (ptIndex < numPoints)
            {
                if (ptIndex > 0)
                {
                    double xdif = pts[ptIndex].X - pts[ptIndex - 1].X;
                    double ydif = pts[ptIndex].Y - pts[ptIndex - 1].Y;
                    double temp = (xdif * xdif) + (ydif * ydif);
                    if (temp >= minDistSquared)
                    {
                        float angle = (float)(Math.Atan2(-ydif, xdif) * 180f / Math.PI);

                        if (Math.Abs(angle) > 90f && Math.Abs(angle) <= 270f)
                        {
                            renderPoints.Add(new RenderPtObj(pts[ptIndex], (float)Math.Sqrt(temp), angle - 180f, recordIndex));
                        }
                        else
                        {
                            renderPoints.Add(new RenderPtObj(pts[ptIndex - 1], (float)Math.Sqrt(temp), angle, recordIndex));
                        }

                    }
                }
                ptIndex++;
            }
            return renderPoints.Count - count; ;
        }
    }


    class SFMultiPointCol : SFRecordCol, QTNodeHelper
    {
    
        public SFMultiPointCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                    PointF[] pts = new PointF[nextRec->NumPoints];
                    for (int p = 0; p < nextRec->NumPoints; ++p)
                    {
                        pts[p] = new PointF((float)nextRec->Points[p<<1], (float)nextRec->Points[(p<<1)+1]);
                    }
                    dataList.Add(pts);
                }
            }
            return dataList;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return GetShapeData(recordIndex, shapeFileStream);
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");

            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                    PointD[] pts = new PointD[nextRec->NumPoints];
                    for (int p = 0; p < nextRec->NumPoints; ++p)
                    {
                        pts[p] = new PointD(nextRec->Points[p<<1], nextRec->Points[(p<<1)+1]);
                    }
                    dataList.Add(pts);
                }
            }
            return dataList;
        }

        
        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        #region paint methods

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            bool useGDI = (this.UseGDI(extent, renderSettings) && renderSettings.GetImageSymbol() == null);

            if (useGDI)
            {
                PaintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
                PaintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation,targetExtent);
            }
        }

        public unsafe void PaintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }
           
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {
                Image symbol = null;
                Size symbolSize = Size.Empty;

                float pointSize = 6f;

                if (renderSettings != null)
                {
                    renderInterior = renderSettings.FillInterior;
                    pointSize = renderSettings.PointSize;
                    symbol = renderSettings.GetImageSymbol();
                    if (symbol != null)
                    {
                        symbolSize = symbol.Size;
                    }
                    else
                    {
                        symbolSize = new Size((int)pointSize, (int)pointSize);
                    }
                }


                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;

                bool drawPoint = (symbol == null && !useCustomImageSymbols);
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Brush fillBrush = null;
                Pen outlinePen = null;
                Pen selectPen = null;
                Brush selectBrush = null;

                fillBrush = new SolidBrush(renderSettings.FillColor);
                outlinePen = new Pen(renderSettings.OutlineColor, 1f);
                selectPen = new Pen(renderSettings.SelectOutlineColor, 2f);
                selectBrush = new SolidBrush(renderSettings.SelectFillColor);
                outlinePen.DashStyle = renderSettings.LineDashStyle;
                selectPen.DashStyle = renderSettings.LineDashStyle;

                try
                {
                    int index = 0;
                    float halfPointSize = pointSize * 0.5f;
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    byte[] buffer = SharedBuffer;
                    double[] recPt = { 0, 0 };
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;

                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }
                            MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        outlinePen = new Pen(customColor, 1);
                                        currentPenColor = customColor;
                                        outlinePen.DashStyle = renderSettings.LineDashStyle;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            fillBrush = new SolidBrush(customColor);
                                            currentBrushColor = customColor;
                                        }
                                    }
                                    if (useCustomImageSymbols)
                                    {
                                        symbol = customRenderSettings.GetRecordImageSymbol(index);
                                        if (symbol != null)
                                        {
                                            symbolSize = symbol.Size;
                                            drawPoint = false;
                                        }
                                        else
                                        {
                                            drawPoint = true;
                                        }
                                    }
                                }
                                for (int p = 0; p < nextRec->NumPoints; ++p)
                                {
                                    //double px, py;
                                    //if (MercProj)
                                    //{
                                    //    LLToProjection(ref nextRec->Points[p<<1], ref nextRec->Points[(p<<1)+1], out px, out py);
                                    //}
                                    //else
                                    //{
                                    //    px = nextRec->Points[p<<1];
                                    //    py = nextRec->Points[(p<<1)+1];
                                    //}

                                    recPt[0] = nextRec->Points[p << 1];
                                    recPt[1] = nextRec->Points[(p << 1) + 1];
                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform(recPt, 1);
                                    }

                                    if (MercProj)
                                    {
                                        LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                    }

                                    PointF pt = new PointF((float)((recPt[0] + offX) * scaleX), (float)((recPt[1] + offY) * scaleY));                                
                                    //PointF pt = new PointF((float)((px + offX) * scaleX), (float)((py + offY) * scaleY));
                                    if (drawPoint)
                                    {
                                        if (pointSize > 0)
                                        {
                                            if (renderInterior)
                                            {
                                                if (recordSelected[index])
                                                {
                                                    g.FillEllipse(selectBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                                }
                                                else
                                                {
                                                    g.FillEllipse(fillBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                                }
                                            }
                                            if (recordSelected[index])
                                            {
                                                g.DrawEllipse(selectPen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                            else
                                            {
                                                g.DrawEllipse(outlinePen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                        }
                                        if (labelFields)
                                        {
                                            renderPtObjList.Add(new RenderPtObj(pt, index, (int)halfPointSize + 5, -((int)halfPointSize + 5)));
                                        }
                                    }
                                    else
                                    {
                                        g.DrawImage(symbol, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1));
                                        if (recordSelected[index])
                                        {
                                            g.DrawRectangle(selectPen, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1), symbolSize.Width, symbolSize.Height);
                                        }
                                        if (labelFields)
                                        {
                                            renderPtObjList.Add(new RenderPtObj(pt, index, ((symbolSize.Width + 1) >> 1) + 5, -(((symbolSize.Height + 1) >> 1) + 5)));
                                        }
                                    }
                                }

                            }
                            //if (fileMapped)
                            //{
                            //    dataPtr += this.RecordHeaders[index].ContentLength + 8;
                            //}
                            index++;
                        }
                    }
                }
                finally
                {
                    if (fillBrush != null) fillBrush.Dispose();
                    if (outlinePen != null) outlinePen.Dispose();
                    if (selectPen != null) selectPen.Dispose();
                    if (selectBrush != null) selectBrush.Dispose();
                }

                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }

        public unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
            
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }

            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {

                float pointSize = 6f;
                renderInterior = renderSettings.FillInterior;
                pointSize = renderSettings.PointSize;

                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }

                IntPtr dc = IntPtr.Zero;
                IntPtr gdiPen = IntPtr.Zero;
                IntPtr gdiBrush = IntPtr.Zero;
                IntPtr oldGdiPen = IntPtr.Zero;
                IntPtr oldGdiBrush = IntPtr.Zero;

                IntPtr selectPen = IntPtr.Zero;
                IntPtr selectBrush = IntPtr.Zero;

                // obtain a handle to the Device Context so we can use GDI to perform the painting.
                dc = g.GetHdc();

                try
                {
                    int color = 0x70c09b;

                    color = ColorToGDIColor(renderSettings.FillColor);
                    if (renderInterior)
                    {
                        gdiBrush = NativeMethods.CreateSolidBrush(color);
                        oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                    }
                    else
                    {
                        oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                    }

                    color = ColorToGDIColor(renderSettings.OutlineColor);
                    gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, color);
                    oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                    selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 2, ColorToGDIColor(renderSettings.SelectOutlineColor));
                    selectBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(renderSettings.SelectFillColor));

                    NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                    int index = 0;
                    Point pt = new Point();
                    int halfPointSize = (int)Math.Round(pointSize * 0.5);
                    int pointSizeInt = (int)Math.Round(pointSize);
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    byte[] buffer = SharedBuffer;
                    double[] recPt = { 0, 0 };
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;
                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }

                            MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)                            
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, ColorToGDIColor(customColor));
                                        NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiBrush));
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }
                                for (int p = 0; p < nextRec->NumPoints; ++p)
                                {
                                    //double px, py;
                                    //if (MercProj)
                                    //{
                                    //    LLToProjection(ref nextRec->Points[p<<1], ref nextRec->Points[(p<<1)+1], out px, out py);
                                    //}
                                    //else
                                    //{
                                    //    px = nextRec->Points[p << 1];
                                    //    py = nextRec->Points[(p << 1) + 1];
                                    //}
                                    //pt.X = (int)Math.Round((px + offX) * scaleX);
                                    //pt.Y = (int)Math.Round((py + offY) * scaleY);

                                    recPt[0] = nextRec->Points[p << 1];
                                    recPt[1] = nextRec->Points[(p << 1) + 1];
                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform(recPt, 1);
                                    }
                                    if (MercProj)
                                    {
                                        LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                    }
                                    pt.X = (int)Math.Round((recPt[0] + offX) * scaleX);
                                    pt.Y = (int)Math.Round((recPt[1] + offY) * scaleY);

                                    if (pointSizeInt > 0)
                                    {
                                        if (recordSelected[index])
                                        {
                                            IntPtr tempPen = IntPtr.Zero;
                                            IntPtr tempBrush = IntPtr.Zero;
                                            try
                                            {
                                                tempPen = NativeMethods.SelectObject(dc, selectPen);
                                                if (renderInterior)
                                                {
                                                    tempBrush = NativeMethods.SelectObject(dc, selectBrush);
                                                }
                                                NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                            }
                                            finally
                                            {
                                                if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                                if (tempBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, tempBrush);
                                            }
                                        }
                                        else
                                        {
                                            NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                        }
                                    }
                                    if (labelFields)
                                    {
                                        renderPtObjList.Add(new RenderPtObj(pt, index, halfPointSize + 5, -(halfPointSize + 5)));
                                    }
                                }
                            }
                            ++index;
                        }
                    }
                }
                finally
                {
                    if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                    if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                    if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                    if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                    if (selectBrush != IntPtr.Zero) NativeMethods.DeleteObject(selectBrush);
                    if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                    g.ReleaseHdc(dc);
                }

                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }

        private struct RenderPtObj
        {
            public PointF Pt;
            public int RecordIndex;

            public int offX, offY;

            public RenderPtObj(PointF p, int recordIndexParam, int x0, int y0)
            {
                Pt = p;
                RecordIndex = recordIndexParam;
                offX = x0;
                offY = y0;
            }

        }

        #endregion


        #region QTNodeHelper Members

        public bool IsPointData()
        {
            //yes it multi points are points but the record has a "bounds" used in the quad tree
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);
            return bounds.ToRectangleD();
        }

        public override bool IntersectRect(int recordIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            List<PointD[]> pts = GetShapeDataD(recordIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    if (rect.Contains(pts[0][n])) return true;
                }
            }
            return false;
        }

        public override bool IntersectCircle(int recordIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            List<PointD[]> pts = GetShapeDataD(recordIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    if ((pts[0][n].X - centre.X) * (pts[0][n].X - centre.X) + (pts[0][n].Y - centre.Y) * (pts[0][n].Y - centre.Y) < (radius * radius))
                    {
                        return true;
                    }
                    
                }
            }
            return false;
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            double distance = double.PositiveInfinity;
            List<PointD[]> pts = GetShapeDataD(shapeIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    double dx = (pts[0][n].X - centre.X);
                    double dy = (pts[0][n].Y - centre.Y);
                    distance = Math.Min(dx * dx + dy * dy, distance);                   
                }
            }
            return Math.Sqrt(distance);
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            double distance = double.PositiveInfinity;
            List<PointD[]> pts = GetShapeDataD(shapeIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    double dx = (pts[0][n].X - centre.X);
                    double dy = (pts[0][n].Y - centre.Y);
                    distance = Math.Min(dx * dx + dy * dy, distance);
                }
            }
            return Math.Sqrt(distance);
        }
        

        #endregion
    }

    class SFMultiPointZCol : SFRecordCol, QTNodeHelper
    {

        public SFMultiPointZCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {

        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                    PointF[] pts = new PointF[nextRec->NumPoints];
                    for (int p = 0; p < nextRec->NumPoints; ++p)
                    {
                        pts[p] = new PointF((float)nextRec->Points[p << 1], (float)nextRec->Points[(p << 1) + 1]);
                    }
                    dataList.Add(pts);
                }
            }
            return dataList;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return GetShapeData(recordIndex, shapeFileStream);
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");

            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                    PointD[] pts = new PointD[nextRec->NumPoints];
                    for (int p = 0; p < nextRec->NumPoints; ++p)
                    {
                        pts[p] = new PointD(nextRec->Points[p << 1], nextRec->Points[(p << 1) + 1]);
                    }
                    dataList.Add(pts);
                }
            }
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<float[]> dataList = new List<float[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                    float[] heights = new float[nextRec->NumPoints];
                    double* dPtr = (double*)(dataPtr + 8 + 40 + (16 * nextRec->NumPoints) + 16);
                    for (int p = 0; p < nextRec->NumPoints; ++p)
                    {
                        heights[p] = (float)dPtr[p];
                    }
                    dataList.Add(heights);
                }
            }
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeHeightData(recordIndex, shapeFileStream, SharedBuffer);
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            if (recordIndex < 0 || recordIndex >= RecordHeaders.Length) throw new ArgumentOutOfRangeException("recordIndex");
            List<double[]> dataList = new List<double[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                    double[] heights = new double[nextRec->NumPoints];
                    double* dPtr = (double*)(dataPtr + 8 + 40 + (16 * nextRec->NumPoints) + 16);
                    for (int p = 0; p < nextRec->NumPoints; ++p)
                    {
                        heights[p] = dPtr[p];
                    }
                    dataList.Add(heights);
                }
            }
            return dataList;
        }


        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            throw new NotImplementedException("GetShapeMDataD has not been implemented for MultiPointZ shape type");
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            bool useGDI = (this.UseGDI(extent, renderSettings) && renderSettings.GetImageSymbol() == null);

            if (useGDI)
            {
                PaintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
                PaintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
        }

        public unsafe void PaintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }
           
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {
                Image symbol = null;
                Size symbolSize = Size.Empty;

                float pointSize = 6f;

                if (renderSettings != null)
                {
                    renderInterior = renderSettings.FillInterior;
                    pointSize = renderSettings.PointSize;
                    symbol = renderSettings.GetImageSymbol();
                    if (symbol != null)
                    {
                        symbolSize = symbol.Size;
                    }
                    else
                    {
                        symbolSize = new Size((int)pointSize, (int)pointSize);
                    }
                }


                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;

                bool drawPoint = (symbol == null && !useCustomImageSymbols);
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Brush fillBrush = null;
                Pen outlinePen = null;
                Pen selectPen = null;
                Brush selectBrush = null;

                fillBrush = new SolidBrush(renderSettings.FillColor);
                outlinePen = new Pen(renderSettings.OutlineColor, 1f);
                selectPen = new Pen(renderSettings.SelectOutlineColor, 2f);
                selectBrush = new SolidBrush(renderSettings.SelectFillColor);
                outlinePen.DashStyle = renderSettings.LineDashStyle;
                selectPen.DashStyle = renderSettings.LineDashStyle;

                try
                {
                    int index = 0;
                    float halfPointSize = pointSize * 0.5f;
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    //shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    byte[] buffer = SharedBuffer;
                    double[] recPt = { 0, 0 };
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;

                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }
                            MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        outlinePen = new Pen(customColor, 1);
                                        currentPenColor = customColor;
                                        outlinePen.DashStyle = renderSettings.LineDashStyle;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            fillBrush = new SolidBrush(customColor);
                                            currentBrushColor = customColor;
                                        }
                                    }
                                    if (useCustomImageSymbols)
                                    {
                                        symbol = customRenderSettings.GetRecordImageSymbol(index);
                                        if (symbol != null)
                                        {
                                            symbolSize = symbol.Size;
                                            drawPoint = false;
                                        }
                                        else
                                        {
                                            drawPoint = true;
                                        }
                                    }
                                }
                                for (int p = 0; p < nextRec->NumPoints; ++p)
                                {
                                    //double px, py;
                                    //if (MercProj)
                                    //{
                                    //    LLToProjection(ref nextRec->Points[p << 1], ref nextRec->Points[(p << 1) + 1], out px, out py);
                                    //}
                                    //else
                                    //{
                                    //    px = nextRec->Points[p << 1];
                                    //    py = nextRec->Points[(p << 1) + 1];
                                    //}
                                    recPt[0] = nextRec->Points[p << 1];
                                    recPt[1] = nextRec->Points[(p << 1) + 1];
                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform(recPt, 1);
                                    }

                                    if (MercProj)
                                    {
                                        LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                    }

                                    //PointF pt = new PointF((float)((px + offX) * scaleX), (float)((py + offY) * scaleY));
                                    PointF pt = new PointF((float)((recPt[0] + offX) * scaleX), (float)((recPt[1] + offY) * scaleY));
                                    if (drawPoint)
                                    {
                                        if (pointSize > 0)
                                        {
                                            if (renderInterior)
                                            {
                                                if (recordSelected[index])
                                                {
                                                    g.FillEllipse(selectBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                                }
                                                else
                                                {
                                                    g.FillEllipse(fillBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                                }
                                            }
                                            if (recordSelected[index])
                                            {
                                                g.DrawEllipse(selectPen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                            else
                                            {
                                                g.DrawEllipse(outlinePen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                            }
                                        }
                                        if (labelFields)
                                        {
                                            renderPtObjList.Add(new RenderPtObj(pt, index, (int)halfPointSize + 5, -((int)halfPointSize + 5)));
                                        }
                                    }
                                    else
                                    {
                                        g.DrawImage(symbol, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1));
                                        if (recordSelected[index])
                                        {
                                            g.DrawRectangle(selectPen, pt.X - (symbolSize.Width >> 1), pt.Y - (symbolSize.Height >> 1), symbolSize.Width, symbolSize.Height);
                                        }
                                        if (labelFields)
                                        {
                                            renderPtObjList.Add(new RenderPtObj(pt, index, ((symbolSize.Width + 1) >> 1) + 5, -(((symbolSize.Height + 1) >> 1) + 5)));
                                        }
                                    }
                                }

                            }
                            //if (fileMapped)
                            //{
                            //    dataPtr += this.RecordHeaders[index].ContentLength + 8;
                            //}
                            index++;
                        }
                    }
                }
                finally
                {
                    if (fillBrush != null) fillBrush.Dispose();
                    if (outlinePen != null) outlinePen.Dispose();
                    if (selectPen != null) selectPen.Dispose();
                    if (selectBrush != null) selectBrush.Dispose();
                }

                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }

        public unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }
           
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);


            try
            {

                float pointSize = 6f;
                renderInterior = renderSettings.FillInterior;
                pointSize = renderSettings.PointSize;

                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (customRenderSettings != null);

                Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
                bool useCustomImageSymbols = useCustomRenderSettings && customRenderSettings.UseCustomImageSymbols;
                bool MercProj = projectionType == ProjectionType.Mercator;
                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }

                IntPtr dc = IntPtr.Zero;
                IntPtr gdiPen = IntPtr.Zero;
                IntPtr gdiBrush = IntPtr.Zero;
                IntPtr oldGdiPen = IntPtr.Zero;
                IntPtr oldGdiBrush = IntPtr.Zero;

                IntPtr selectPen = IntPtr.Zero;
                IntPtr selectBrush = IntPtr.Zero;

                // obtain a handle to the Device Context so we can use GDI to perform the painting.
                dc = g.GetHdc();

                try
                {
                    int color = 0x70c09b;

                    color = ColorToGDIColor(renderSettings.FillColor);
                    if (renderInterior)
                    {
                        gdiBrush = NativeMethods.CreateSolidBrush(color);
                        oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                    }
                    else
                    {
                        oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                    }

                    color = ColorToGDIColor(renderSettings.OutlineColor);
                    gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, color);
                    oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                    selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 2, ColorToGDIColor(renderSettings.SelectOutlineColor));
                    selectBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(renderSettings.SelectFillColor));

                    NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                    int index = 0;
                    Point pt = new Point();
                    int halfPointSize = (int)Math.Round(pointSize * 0.5);
                    int pointSizeInt = (int)Math.Round(pointSize);
                    byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                    if (fileMapped)
                    {
                        dataPtrZero = (byte*)mapView.ToPointer();
                        dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                    }
                    else
                    {
                        shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                    }
                    byte[] buffer = SharedBuffer;
                    double[] recPt = { 0, 0 };
                    fixed (byte* bufPtr = buffer)
                    {
                        if (!fileMapped) dataPtr = bufPtr;
                        while (index < RecordHeaders.Length)
                        {
                            if (fileMapped)
                            {
                                dataPtr = dataPtrZero + RecordHeaders[index].Offset;
                            }
                            else
                            {
                                if (shapeFileStream.Position != RecordHeaders[index].Offset)
                                {
                                    Console.Out.WriteLine("offset wrong");
                                    shapeFileStream.Seek(RecordHeaders[index].Offset, SeekOrigin.Begin);
                                }
                                shapeFileStream.Read(buffer, 0, RecordHeaders[index].ContentLength + 8);
                            }

                            MultiPointRecordP* nextRec = (MultiPointRecordP*)(dataPtr + 8);
                            bool renderShape = recordVisible[index];
                            //overwrite if using CustomRenderSettings
                            if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                            if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, ColorToGDIColor(customColor));
                                        NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiBrush));
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }
                                for (int p = 0; p < nextRec->NumPoints; ++p)
                                {
                                    //double px, py;
                                    //if (MercProj)
                                    //{
                                    //    LLToProjection(ref nextRec->Points[p << 1], ref nextRec->Points[(p << 1) + 1], out px, out py);
                                    //}
                                    //else
                                    //{
                                    //    px = nextRec->Points[p << 1];
                                    //    py = nextRec->Points[(p << 1) + 1];
                                    //}
                                    recPt[0] = nextRec->Points[p << 1];
                                    recPt[1] = nextRec->Points[(p << 1) + 1];
                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform(recPt, 1);
                                    }

                                    if (MercProj)
                                    {
                                        LLToProjection(ref recPt[0], ref recPt[1], out recPt[0], out recPt[1]);
                                    }


                                    pt.X = (int)Math.Round((recPt[0] + offX) * scaleX);
                                    pt.Y = (int)Math.Round((recPt[1] + offY) * scaleY);

                                    if (pointSizeInt > 0)
                                    {
                                        if (recordSelected[index])
                                        {
                                            IntPtr tempPen = IntPtr.Zero;
                                            IntPtr tempBrush = IntPtr.Zero;
                                            try
                                            {
                                                tempPen = NativeMethods.SelectObject(dc, selectPen);
                                                if (renderInterior)
                                                {
                                                    tempBrush = NativeMethods.SelectObject(dc, selectBrush);
                                                }
                                                NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                            }
                                            finally
                                            {
                                                if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                                if (tempBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, tempBrush);
                                            }
                                        }
                                        else
                                        {
                                            NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                                        }
                                    }
                                    if (labelFields)
                                    {
                                        renderPtObjList.Add(new RenderPtObj(pt, index, halfPointSize + 5, -(halfPointSize + 5)));
                                    }
                                }
                            }
                            ++index;
                        }
                    }
                }
                finally
                {
                    if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                    if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                    if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                    if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                    if (selectBrush != IntPtr.Zero) NativeMethods.DeleteObject(selectBrush);
                    if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                    g.ReleaseHdc(dc);
                }

                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    int count = renderPtObjList.Count;
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    LabelPlacementMap labelPlacementMap = new LabelPlacementMap(clientArea.Width, clientArea.Height);

                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    float ssm = shadowText ? 0.8f : 1f;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)
                        {
                            SizeF labelSize = g.MeasureString(strLabel, renderSettings.Font);
                            int x0 = renderPtObjList[n].offX;
                            int y0 = renderPtObjList[n].offY;
                            if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width * ssm), (int)Math.Round(labelSize.Height * ssm)))
                            {
                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0), new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    //g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                                }
                            }
                        }
                    }
                    pen.Dispose();
                    fontBrush.Dispose();
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
        }



        private struct RenderPtObj
        {
            public PointF Pt;
            public int RecordIndex;

            public int offX, offY;

            public RenderPtObj(PointF p, int recordIndexParam, int x0, int y0)
            {
                Pt = p;
                RecordIndex = recordIndexParam;
                offX = x0;
                offY = y0;
            }

        }


        #region QTNodeHelper Members

        public bool IsPointData()
        {
            //yes multi points are points but the record has a "bounds" used in the quad tree
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);
            return bounds.ToRectangleD();
        }

        public override bool IntersectRect(int recordIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            List<PointD[]> pts = GetShapeDataD(recordIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    if (rect.Contains(pts[0][n])) return true;
                }
            }
            return false;
        }

        public override bool IntersectCircle(int recordIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            List<PointD[]> pts = GetShapeDataD(recordIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    if ((pts[0][n].X - centre.X) * (pts[0][n].X - centre.X) + (pts[0][n].Y - centre.Y) * (pts[0][n].Y - centre.Y) < (radius * radius))
                    {
                        return true;
                    }

                }
            }
            return false;
        }


        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            double distance = double.PositiveInfinity;
            List<PointD[]> pts = GetShapeDataD(shapeIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    double dx = (pts[0][n].X - centre.X);
                    double dy = (pts[0][n].Y - centre.Y);
                    distance = Math.Min(dx * dx + dy * dy, distance);
                }
            }
            return Math.Sqrt(distance);
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            double distance = double.PositiveInfinity;
            List<PointD[]> pts = GetShapeDataD(shapeIndex, shapeFileStream);
            if (pts != null && pts.Count > 0)
            {
                for (int n = pts[0].Length - 1; n >= 0; --n)
                {
                    double dx = (pts[0][n].X - centre.X);
                    double dy = (pts[0][n].Y - centre.Y);
                    distance = Math.Min(dx * dx + dy * dy, distance);
                }
            }
            return Math.Sqrt(distance);
        }

        #endregion
    }


    class SFPolyLineCol : SFRecordCol, QTNodeHelper
    {

        public SFPolyLineCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            //SFRecordCol.EnsureBufferSize(SFRecordCol.GetMaxRecordLength(recs) + 100);
            int length = SFRecordCol.GetMaxRecordLength(recs) + 100;
            SFRecordCol.EnsureBufferSize(length);
            base.SetMaxRecordLength(length);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer/*new byte[ShapeFileExConstants.MAX_REC_LENGTH]*/);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->DataOffset;
                    int numParts = nextRec->NumParts;
                    PointF[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsDAsPointF(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointD[]> dataList = new List<PointD[]>();                
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->DataOffset;
                    int numParts = nextRec->NumParts;
                    PointD[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;            
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        #region Paint methods

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            DateTime tick = DateTime.Now;
            if (this.UseGDI(extent, renderSettings))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                PaintHighQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
                //PaintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                PaintHighQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            //Console.Out.WriteLine("Render time: " + DateTime.Now.Subtract(tick).TotalSeconds + "s");
        }

        private unsafe void PaintHighQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType,  ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            Pen gdiplusPen = null;
            Pen rwPen = null;
            bool renderRailway = false;

            Pen selectPen = null;

			Pen arrowPen = null;
			bool drawArrows = false;
			int arrowLength=50;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
               mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            bool MercProj = projectionType == ProjectionType.Mercator;
            Point[] tempPoints = new Point[SFRecordCol.SharedPointBuffer.Length];
            
            try
            {                
                double scaleX = (double)(clientArea.Width / extent.Width);
                double scaleY = -scaleX;

                simplificationDistance = 1 / scaleX;

                RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);

                double offX = -projectedExtent.Left;
                double offY = -projectedExtent.Bottom;
                RectangleD actualExtent = projectedExtent;

                if (coordinateTransformation != null)
                {
                    scaleX = (double)(clientArea.Width / targetExtent.Width);
                    scaleY = -scaleX;

                    projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                    offX = -projectedExtent.Left;
                    offY = -projectedExtent.Bottom;
                    //when the coordinateTransformation has been supplied the extent will already contain
                    //the actual intersecting extent in the shapefile CRS
                    actualExtent = extent;
                    //set simplificationDistance to zero if we're using a trnasformation - we'll calculate it 
                    //when we render the first record
                    simplificationDistance = 0;
                }


                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }
                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);

                bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));               
                bool renderDuplicateFields = (labelFields && renderSettings.RenderDuplicateFields);
                const bool usePolySimplificationLabeling = true;
                List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

                float renderPenWidth = (float)(renderSettings.PenWidthScale * scaleX);
                if (float.IsNaN(renderPenWidth) || float.IsInfinity(renderPenWidth))
                {
                    renderPenWidth = 1;
                }
                if (renderSettings.MaxPixelPenWidth > 0 && renderPenWidth > renderSettings.MaxPixelPenWidth)
                {
                    renderPenWidth = renderSettings.MaxPixelPenWidth;
                }
                if (renderSettings.MinPixelPenWidth > 0 && renderPenWidth < renderSettings.MinPixelPenWidth)
                {
                    renderPenWidth = renderSettings.MinPixelPenWidth;
                }

                if (renderSettings.LineType == LineType.Railway)
                {
                    renderPenWidth = Math.Min(renderPenWidth, 7f);
                }

				if (renderSettings.DrawDirectionArrows && (renderSettings.DirectionArrowMinZoomLevel < 0 || scaleX >= renderSettings.DirectionArrowMinZoomLevel) )
				{
					int arrowWidth = Math.Max(1,Math.Min(renderSettings.DirectionArrowWidth, 50));
					arrowPen = new Pen(renderSettings.DirectionArrowColor, arrowWidth);
					arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(3, 6);
					drawArrows = true;
					arrowLength = Math.Max(renderSettings.DirectionArrowLength, 10);
				}
                
                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                //g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }
                try
                {

                    for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                    {
                        try
                        {
                            Color currentPenColor = renderSettings.OutlineColor;
                            int penWidth = (int)Math.Round(renderPenWidth);

                            if (paintCount == 1)
                            {
                                currentPenColor = renderSettings.FillColor;
                                penWidth -= 2;
                            }

                            if (paintCount == 0)
                            {
                                Color c = renderSettings.OutlineColor;
                                gdiplusPen = new Pen(renderSettings.OutlineColor, penWidth);
                                selectPen = new Pen(renderSettings.SelectOutlineColor, penWidth+4);
                            }
                            else if (paintCount == 1)
                            {
                                gdiplusPen = new Pen(renderSettings.FillColor, penWidth);
                                selectPen = new Pen(renderSettings.SelectFillColor, penWidth);
                                if (penWidth > 1)
                                {
                                    renderRailway = (renderSettings.LineType == LineType.Railway);
                                    if (renderRailway)
                                    {
                                        rwPen = new Pen(renderSettings.OutlineColor, 1);
                                    }
                                }
                            }
                            gdiplusPen.EndCap = renderSettings.LineEndCap;//  System.Drawing.Drawing2D.LineCap.Round;
                            gdiplusPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
                            gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                            selectPen.EndCap = renderSettings.LineEndCap;// System.Drawing.Drawing2D.LineCap.Round;
                            selectPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
                            selectPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                            if (renderSettings.LineType == LineType.Solid)
                            {
                                gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                                selectPen.DashStyle = renderSettings.LineDashStyle;
                            }
                            byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                            if (fileMapped)
                            {
                                dataPtrZero = (byte*)mapView.ToPointer();
                                dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                            }
                            else
                            {
                                shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                            }

                            PointF pt = new PointF(0, 0);
                            RecordHeader[] pgRecs = this.RecordHeaders;

                            int index = 0;
                            byte[] data = SFRecordCol.SharedBuffer;
                            float minLabelLength = (float)(6 / scaleX);
                            fixed (byte* dataBufPtr = data)
                            {
                                if (!fileMapped) dataPtr = dataBufPtr;
                                while (index < pgRecs.Length)
                                {
                                    if (fileMapped)
                                    {
                                        dataPtr = dataPtrZero + pgRecs[index].Offset;
                                    }
                                    else
                                    {
                                        if (shapeFileStream.Position != pgRecs[index].Offset)
                                        {
                                            Console.Out.WriteLine("offset wrong");
                                            shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                                        }
                                        shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                                    }
                                    PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                                    int dataOffset = nextRec->DataOffset;//nextRec.Read(data, 8);
                                    bool renderShape = recordVisible[index];
                                    //overwrite if using CustomRenderSettings
                                    if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                                    RectangleD recordBounds = nextRec->bounds.ToRectangleD();
                                    if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(recordBounds) && renderShape)
                                    {
                                        //check if the simplifiedDataBuffer sized needs to be increased
                                        if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                                        {
                                            simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                                        }
                                        if (simplificationDistance <= double.Epsilon)
                                        {
                                            simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                                        }

                                        fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                                        {
                                            int numParts = nextRec->NumParts;
                                            Point[] pts;

                                            if (useCustomRenderSettings)
                                            {
                                                Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                                if (customColor.ToArgb() != currentPenColor.ToArgb())
                                                {
                                                    gdiplusPen = new Pen(customColor, penWidth);
                                                    currentPenColor = customColor;
                                                    gdiplusPen.EndCap = renderSettings.LineEndCap;// System.Drawing.Drawing2D.LineCap.Round;
                                                    gdiplusPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
                                                    gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                                                    if (renderSettings.LineType == LineType.Solid)
                                                    {
                                                        gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                                                        selectPen.DashStyle = renderSettings.LineDashStyle;
                                                    }
                                                }
                                            }

                                            for (int partNum = 0; partNum < numParts; partNum++)
                                            {
                                                int numPoints;
                                                if ((numParts - partNum) > 1)
                                                {
                                                    numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                                }
                                                else
                                                {
                                                    numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                                }
                                                if (numPoints <= 1)
                                                {
                                                    continue;
                                                }

                                                //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                                int usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                                if (coordinateTransformation != null)
                                                {
                                                    coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                                }

                                                Rectangle pixelBounds = new Rectangle();
                                                pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, ref pixelBounds, MercProj);

                                                //pts = GetPointsD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, offX, offY, scaleX, scaleY, MercProj);

                                                //add any labels to the poly-lines
                                                if (labelFields && paintCount == 0 && usePolySimplificationLabeling)
                                                {
                                                    //check what the scaled bounds of this part are
                                                    //if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
                                                    if ((nextRec->bounds.Width > 6 * simplificationDistance) || (nextRec->bounds.Height > 6 * simplificationDistance))
                                                    {
                                                        int usedTempPoints = 0;
                                                        NativeMethods.SimplifyDouglasPeucker(pts, pts.Length, 2, tempPoints, ref usedTempPoints);
                                                        if (usedTempPoints > 0)
                                                        {
                                                            RenderPtObj.AddRenderPtObjects(tempPoints, usedTempPoints, renderPtObjList, index, 6);
                                                        }
                                                    }
                                                }

                                                List<Point[]> pointList = new List<Point[]>();
                                                if (pixelBounds.Left < -1000 || pixelBounds.Top < -1000 || pixelBounds.Right > clientArea.Width + 1000 || pixelBounds.Bottom > clientArea.Height + 1000)
                                                {
                                                    //clip the polyline to avoid overflow
                                                    GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
                                                    {
                                                        XMin = 0,
                                                        YMin = 0,
                                                        XMax = clientArea.Width,
                                                        YMax = clientArea.Height
                                                    };
                                                    List<int> clippedPoints = new List<int>();
                                                    List<int> clippedParts = new List<int>();
                                                    GeometryAlgorithms.PolyLineClip(pts, pts.Length, clipBounds, clippedPoints, clippedParts);
                                                    for (int clipPartIndex = 0; clipPartIndex < clippedParts.Count; ++clipPartIndex)
                                                    {
                                                        int startIndex = clippedParts[clipPartIndex];
                                                        int endIndex = clipPartIndex < clippedParts.Count - 1 ? clippedParts[clipPartIndex + 1] : clippedPoints.Count;
                                                        Point[] partPoints = new Point[(endIndex - startIndex) >> 1];
                                                        for (int s = startIndex, p = 0; s < endIndex; s += 2, ++p)
                                                        {
                                                            partPoints[p].X = clippedPoints[s];
                                                            partPoints[p].Y = clippedPoints[s + 1];
                                                        }
                                                        pointList.Add(partPoints);
                                                    }
                                                }
                                                else
                                                {
                                                    pointList.Add(pts);
                                                }
                                                foreach (var partPoints in pointList)
                                                {
                                                    pts = partPoints;
                                                    if (recordSelected[index] && selectPen != null)
                                                    {
                                                        g.DrawLines(selectPen, pts);
                                                    }
                                                    else
                                                    {
                                                        g.DrawLines(gdiplusPen, pts);
                                                    }
                                                    if (renderRailway)
                                                    {
                                                        int th = (int)Math.Ceiling((renderPenWidth + 2) / 2);
                                                        int tw = (int)Math.Max(Math.Round((7f * th) / 7), 5);
                                                        int pIndx = 0;
                                                        int lx = 0;
                                                        System.Drawing.Drawing2D.Matrix savedTransform = g.Transform;
                                                        try
                                                        {
                                                            while (pIndx < pts.Length - 1)
                                                            {
                                                                //draw the next line segment

                                                                int dy = pts[pIndx + 1].Y - pts[pIndx].Y;
                                                                int dx = pts[pIndx + 1].X - pts[pIndx].X;
                                                                float a = (float)(Math.Atan2(dy, dx) * 180f / Math.PI);
                                                                int d = (int)Math.Round(Math.Sqrt(dy * dy + dx * dx));
                                                                if (d > 0)
                                                                {
                                                                    g.Transform = savedTransform;
                                                                    if (Math.Abs(a) > 90f && Math.Abs(a) <= 270f)
                                                                    {
                                                                        a -= 180f;
                                                                        g.TranslateTransform(pts[pIndx + 1].X, pts[pIndx + 1].Y);
                                                                        g.RotateTransform(a);
                                                                        while (lx < d)
                                                                        {
                                                                            g.DrawLine(rwPen, lx, -th, lx, th);
                                                                            lx += tw;
                                                                        }
                                                                        lx -= d;
                                                                    }
                                                                    else
                                                                    {
                                                                        g.TranslateTransform(pts[pIndx].X, pts[pIndx].Y);
                                                                        g.RotateTransform(a);
                                                                        while (lx < d)
                                                                        {
                                                                            g.DrawLine(rwPen, lx, -th, lx, th);
                                                                            lx += tw;
                                                                        }
                                                                        lx -= d;
                                                                    }
                                                                }

                                                                pIndx++;
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            g.Transform = savedTransform;
                                                        }
                                                    }
                                                }
												if (drawArrows && paintCount == (maxPaintCount-1))
												{
													DrawDirectionArrows(pointList, arrowLength, arrowPen, g);
												}
                                            }
                                        }
                                    }                                   
                                    ++index;
                                }
                            }
                        }
                        finally
                        {
                            if (gdiplusPen != null) gdiplusPen.Dispose();
                            if (rwPen != null) rwPen.Dispose();
                            if (selectPen != null) selectPen.Dispose();														
                        }
                    }
                }
                finally
                {
                    tempPoints = null;                   
                }
                System.Drawing.Drawing2D.Matrix savedMatrix = g.Transform;                                    
                try
                {
                    if (labelFields)
                    {
                        bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                        Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                        Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                        pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                        int count = renderPtObjList.Count;
                        Color currentFontColor = renderSettings.FontColor;
                        bool useCustomFontColor = customRenderSettings != null;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                        float ws = 1.2f;
                        if (renderDuplicateFields) ws = 1f;
                        float ssm = shadowText ? 0.8f : 1f;
                        for (int n = 0; n < count; n++)
                        {
                            string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                            if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                            {
                                SizeF strSize = g.MeasureString(strLabel, renderSettings.Font);
                                strSize = new SizeF(strSize.Width * ssm, strSize.Height * ssm);
                                if (strSize.Width <= (renderPtObjList[n].Point0Dist) * ws)
                                {                                    
                                    g.Transform = savedMatrix;
                                    g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                    g.RotateTransform(-renderPtObjList[n].Angle0);

                                    if (useCustomFontColor)
                                    {
                                        Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                        if (customColor.ToArgb() != currentFontColor.ToArgb())
                                        {
                                            fontBrush.Dispose();
                                            fontBrush = new SolidBrush(customColor);
                                            currentFontColor = customColor;
                                        }
                                    }

                                    if (shadowText)
                                    {
                                        System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                        gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF((renderPtObjList[n].Point0Dist - strSize.Width) / 2, -(strSize.Height) / 2), StringFormat.GenericDefault);//new StringFormat());
                                        g.DrawPath(pen, gp);
                                        g.FillPath(fontBrush, gp);
                                    }
                                    else
                                    {
                                        g.DrawString(strLabel, renderSettings.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);
                                    }                                    
                                }
                            }
                        }
                        fontBrush.Dispose();
                        pen.Dispose();
                    }
                }
                finally
                {
                    g.Transform = savedMatrix;
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
				if (arrowPen != null) arrowPen.Dispose();
			}
        }        

        private unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {            
            IntPtr dc = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiObj = IntPtr.Zero;

            IntPtr selectPen = IntPtr.Zero;                

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);
           
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;

            simplificationDistance = 1 / scaleX;

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                // RectangleD targetExtent = ShapeFile.ConvertExtent(extent, coordinateTransformation);
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }


            bool MercProj = projectionType == ProjectionType.Mercator;

            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }
            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);

            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderDuplicateFields = (labelFields && renderSettings.RenderDuplicateFields);
            bool usePolySimplificationLabeling = true;
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

            float renderPenWidth = (float)(renderSettings.PenWidthScale * scaleX);

            if (renderSettings.MaxPixelPenWidth > 0 && renderPenWidth > renderSettings.MaxPixelPenWidth)
            {
                renderPenWidth = renderSettings.MaxPixelPenWidth;
            }
            if (renderSettings.MinPixelPenWidth > 0 && renderPenWidth < renderSettings.MinPixelPenWidth)
            {
                renderPenWidth = renderSettings.MinPixelPenWidth;
            }

            if (renderSettings.LineType == LineType.Railway)
            {
                renderPenWidth = Math.Min(renderPenWidth, 7f);
            }

            // obtain a handle to the Device Context so we can use GDI to perform the painting.            
            dc = g.GetHdc();
            Point[] tempPoints = new Point[SFRecordCol.SharedPointBuffer.Length];
            try
            {
                int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }

                NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                {
                    try
                    {
                        Color currentPenColor = renderSettings.OutlineColor;
                        int penWidth = (int)Math.Round(renderPenWidth);
                        int color = 0x1d1030;                    
                        color = renderSettings.OutlineColor.B & 0xff;
                        color = (color << 8) | (renderSettings.OutlineColor.G & 0xff);
                        color = (color << 8) | (renderSettings.OutlineColor.R & 0xff);
                
                        if (paintCount == 1)
                        {
                            currentPenColor = renderSettings.FillColor;                            
                            color = renderSettings.FillColor.B & 0xff;
                            color = (color << 8) | (renderSettings.FillColor.G & 0xff);
                            color = (color << 8) | (renderSettings.FillColor.R & 0xff);
                            penWidth -= 2;
                        }

                        int penStyle = NativeMethods.PS_SOLID;
                        if (renderSettings.LineType == LineType.Solid)
                        {
                            penStyle= (int)renderSettings.LineDashStyle;                            
                        }

                        gdiPen = NativeMethods.CreatePen(penStyle, penWidth, color);
                        oldGdiObj = NativeMethods.SelectObject(dc, gdiPen);

                        if (paintCount == 0)
                        {
                            selectPen = NativeMethods.CreatePen(penStyle, penWidth+4, ColorToGDIColor(renderSettings.SelectOutlineColor));
                        }
                        else
                        {
                            selectPen = NativeMethods.CreatePen(penStyle, penWidth, ColorToGDIColor(renderSettings.SelectFillColor));                    
                        }
                        byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                        if (fileMapped)
                        {
                            dataPtrZero = (byte*)mapView.ToPointer();
                            dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                        }
                        else
                        {
                            shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                        }

                        RecordHeader[] pgRecs = this.RecordHeaders;

                        int index = 0;
                        byte[] data = SFRecordCol.SharedBuffer;
                        Point[] sharedPoints = SFRecordCol.SharedPointBuffer;
                        float minLabelLength = (float)(6 / scaleX);
                        GPRDParams gprdParam = new GPRDParams(offX, offY, scaleX, 0, MercProj);

                        //int ptCountAvg = 0, totalParts = 0;
                        fixed (byte* dataBufPtr = data)
                        {
                            if (!fileMapped) dataPtr = dataBufPtr;
                            while (index < pgRecs.Length)
                            {
                                if (fileMapped)
                                {
                                    dataPtr = dataPtrZero + pgRecs[index].Offset;
                                }
                                else
                                {
                                    if (shapeFileStream.Position != pgRecs[index].Offset)
                                    {
                                        Console.Out.WriteLine("offset wrong");
                                        shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                                    }
                                    shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                                }
                                PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                                int dataOffset = nextRec->DataOffset;
                                bool renderShape = recordVisible[index];
                                //overwrite if using CustomRenderSettings
                                if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                                if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)
                                {

                                    //check if the simplifiedDataBuffer sized needs to be increased
                                    if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                                    {
                                        simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                                    }

                                    fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                                    {

                                        int numParts = nextRec->NumParts;



                                        if (useCustomRenderSettings)
                                        {
                                            Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                            if (customColor.ToArgb() != currentPenColor.ToArgb())
                                            {
                                                penStyle = NativeMethods.PS_SOLID;
                                                if (renderSettings.LineType == LineType.Solid)
                                                {
                                                    penStyle = (int)renderSettings.LineDashStyle;
                                                }
                                                gdiPen = NativeMethods.CreatePen(penStyle, penWidth, ColorToGDIColor(customColor));
                                                NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                                currentPenColor = customColor;
                                            }
                                        }

                                        for (int partNum = 0; partNum < numParts; ++partNum)
                                        {
                                            int numPoints;
                                            if ((numParts - partNum) > 1)
                                            {
                                                numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                            }
                                            else
                                            {
                                                numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                            }
                                            if (numPoints <= 1)
                                            {
                                                continue;
                                            }

                                            int usedPoints = 0;

                                            //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                            usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                            if (coordinateTransformation != null)
                                            {
                                                coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                            }
                                                                                    
                                            gprdParam.usedPoints = 0;
                                            //GetPointsRemoveDuplicatesD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, sharedPoints, ref gprdParam);
                                            GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr, 0, usedPoints, sharedPoints, ref gprdParam);


                                            //add any labels to the poly-lines
                                            if (labelFields && paintCount == 0 && usePolySimplificationLabeling)
                                            {
                                                //check what the scaled bounds of this part are
                                                //if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
                                                if ((nextRec->bounds.Width > 6 * simplificationDistance) || (nextRec->bounds.Height > 6 * simplificationDistance))
                                                {
                                                    int usedTempPoints = 0;
                                                    NativeMethods.SimplifyDouglasPeucker(sharedPoints, gprdParam.usedPoints, 2, tempPoints, ref usedTempPoints);
                                                    if (usedTempPoints > 0)
                                                    {
                                                        RenderPtObj.AddRenderPtObjects(tempPoints, usedTempPoints, renderPtObjList, index, 6);
                                                    }
                                                }
                                            }
                                            //render the lines
                                            if (recordSelected[index])
                                            {
                                                IntPtr tempPen = IntPtr.Zero;
                                                try
                                                {
                                                    tempPen = NativeMethods.SelectObject(dc, selectPen);
                                                    NativeMethods.DrawPolyline(dc, sharedPoints, gprdParam.usedPoints);
                                                }
                                                finally
                                                {
                                                    if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                                }
                                            }
                                            else
                                            {
                                                NativeMethods.DrawPolyline(dc, sharedPoints, gprdParam.usedPoints);
                                            }
                                        }
                                    }
                                }
                                
                                ++index;
                      
                            }
                        }
                      //  Console.Out.WriteLine("Avg ptCount: " + ((double)ptCountAvg/totalParts));
                                        
                        //Console.Out.WriteLine("numPainted:" + numPainted);
                    }
                    finally
                    {
                        if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                        if (oldGdiObj != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiObj);
                        if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                        selectPen = IntPtr.Zero;                    
                    }
                }
            }

            finally
            {
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
                tempPoints = null;
            }
            System.Drawing.Drawing2D.Matrix savedMatrix = g.Transform;                                                                    
            try
            {
                if (labelFields)
                {
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    int count = renderPtObjList.Count;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    float ws = 1.2f;
                    if (renderDuplicateFields) ws = 1f;
                    float ssm = shadowText ? 0.8f : 1f;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                        {
                            SizeF strSize = g.MeasureString(strLabel, renderSettings.Font);
                            strSize = new SizeF(strSize.Width * ssm, strSize.Height * ssm);
                            if (strSize.Width <= (renderPtObjList[n].Point0Dist) * ws)
                            {
                                g.Transform = savedMatrix;                                                                                   
                                g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                g.RotateTransform(-renderPtObjList[n].Angle0);

                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF((renderPtObjList[n].Point0Dist - strSize.Width) / 2, -(strSize.Height) / 2), StringFormat.GenericDefault);//new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);
                                }                                
                            }
                        }
                    }
                    fontBrush.Dispose();
                    pen.Dispose();
                }
            }
            finally
            {
                g.Transform = savedMatrix;             
            }

        }

		#endregion


		public bool ContainsPoint(int shapeIndex, PointD pt, System.IO.Stream shapeFileStream, byte[] dataBuf, double minDist)
        {
            unsafe
            {
                byte[] data = SFRecordCol.SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->DataOffset;
                    int numParts = nextRec->NumParts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1)
                        {
                            numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        }
                        else
                        {
                            numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        }
                        if (GeometryAlgorithms.PointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, pt, minDist))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }        

        public override unsafe bool IntersectRect(int shapeIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with rect
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!recBounds.IntersectsWith(rect)) return false;

            //next check if rect entirely contains the polyline
            if (rect.Contains(recBounds)) return true;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                       
                    if (IntersectsRect(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref rect))
                    {
                        return true;
                    }
                }
            }
            return false;
        }



        private static unsafe bool IntersectsRect(byte[] data, int offset, int numPoints, ref RectangleD rect)
        {
           
            fixed (byte* ptr = data)
            {
                return NativeMethods.PolyLineRectIntersect(ptr + offset, numPoints, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }


        public override unsafe bool IntersectCircle(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            
            fixed (byte* dataPtr = data)
            {
                PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    if (GeometryAlgorithms.PolylineCircleIntersects(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, radius))
                    {
                        return true;
                    }
                    
                }
            }
            return false;
        }


        public override unsafe double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            // RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;
            double foundDistance = double.PositiveInfinity;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    double polylineDistance = GeometryAlgorithms.ClosestPointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre);
                    if (polylineDistance < foundDistance) foundDistance = polylineDistance;
                }
            }
            return foundDistance;
        }

        public override unsafe double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            //first check if the record's bounds intersects with the circle
            //RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return double.PositiveInfinity;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineRecordP* nextRec = (PolyLineRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    PolylineDistanceInfo pdi;
                    double polylineDistance = GeometryAlgorithms.ClosestPointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, out pdi);
                    if (polylineDistance < polylineDistanceInfo.Distance)
                    {
                        pdi.PointIndex += nextRec->PartOffsets[partNum];
                        polylineDistanceInfo = pdi;
                    }
                }
            }
            return polylineDistanceInfo.Distance;
        }
        

        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            //byte[] data = SFRecordCol.SharedBuffer;
            byte[] data = SingleThreaded ? SFRecordCol.SharedBuffer :  new byte[32 + 12];
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);
            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);
            return bounds.ToRectangleD();
        }



        #endregion

    }

    class SFPolyLineMCol : SFRecordCol, QTNodeHelper
    {        
        public SFPolyLineMCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            //this.RecordHeaders = recs;
            int length = SFRecordCol.GetMaxRecordLength(recs) + 100;
            SFRecordCol.EnsureBufferSize(length);
            base.SetMaxRecordLength(length);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer/*new byte[ShapeFileExConstants.MAX_REC_LENGTH]*/);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset;
                    int numParts = nextRec->NumParts;
                    PointF[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsDAsPointF(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset;
                    int numParts = nextRec->NumParts;
                    PointD[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<double[]> dataList = new List<double[]>();            
            unsafe
            {
                const int RecordHeaderLength = 8;
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + RecordHeaderLength);
                fixed (byte* dataPtr = data)
                {
                    PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + RecordHeaderLength);
                    int dataOffset = nextRec->MeasureDataOffset + 16 + RecordHeaderLength; //skip min max measure
                    int numParts = nextRec->NumParts;
                    double[] measures;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        measures = GetDoubleData(dataPtr, dataOffset, numPoints);
                        dataOffset += numPoints << 3;
                        dataList.Add(measures);
                    }
                }
            }
            return dataList;

        }


        #region Paint methods

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            DateTime tick = DateTime.Now;
            if (this.UseGDI(extent, renderSettings))
            {
               // PaintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                PaintHighQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                PaintHighQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            //Console.Out.WriteLine("render time:" + DateTime.Now.Subtract(tick).TotalSeconds + "s");

        }

        private unsafe void PaintHighQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            Pen gdiplusPen = null;
            Pen rwPen = null;
            bool renderRailway = false;

            Pen selectPen = null;

			Pen arrowPen = null;
			bool drawArrows = false;
			int arrowLength = 50;

			IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            bool MercProj = projectionType == ProjectionType.Mercator;
            Point[] tempPoints = new Point[SFRecordCol.SharedBuffer.Length];
            try
            {
                double scaleX = (double)(clientArea.Width / extent.Width);
                double scaleY = -scaleX;
                
                simplificationDistance = 1 / scaleX;

                RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width* (double)clientArea.Height /(double)clientArea.Width);

                double offX = -projectedExtent.Left;
                double offY = -projectedExtent.Bottom;
                RectangleD actualExtent = projectedExtent;

                if (coordinateTransformation != null)
                {
                    scaleX = (double)(clientArea.Width / targetExtent.Width);
                    scaleY = -scaleX;

                    projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                    offX = -projectedExtent.Left;
                    offY = -projectedExtent.Bottom;
                    //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                    //when the coordinateTransformation has been supplied the extent will already contain
                    //the actual intersecting extent in the shapefile CRS
                    actualExtent = extent;
                    //set simplificationDistance to zero if we're using a trnasformation - we'll calculate it 
                    //when we render the first record
                    simplificationDistance = 0;
                }


                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }
                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);

                bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
                bool renderDuplicateFields = (labelFields && renderSettings.RenderDuplicateFields);
                const bool usePolySimplificationLabeling = true;
                List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

                float renderPenWidth = (float)(renderSettings.PenWidthScale * scaleX);

                if (renderSettings.MaxPixelPenWidth > 0 && renderPenWidth > renderSettings.MaxPixelPenWidth)
                {
                    renderPenWidth = renderSettings.MaxPixelPenWidth;
                }
                if (renderSettings.MinPixelPenWidth > 0 && renderPenWidth < renderSettings.MinPixelPenWidth)
                {
                    renderPenWidth = renderSettings.MinPixelPenWidth;
                }

                if (renderSettings.LineType == LineType.Railway)
                {
                    renderPenWidth = Math.Min(renderPenWidth, 7f);
                }

				if (renderSettings.DrawDirectionArrows && (renderSettings.DirectionArrowMinZoomLevel < 0 || scaleX >= renderSettings.DirectionArrowMinZoomLevel))
				{
					int arrowWidth = Math.Max(1, Math.Min(renderSettings.DirectionArrowWidth, 50));
					arrowPen = new Pen(renderSettings.DirectionArrowColor, arrowWidth);
					arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(3, 6);
					drawArrows = true;
					arrowLength = Math.Max(renderSettings.DirectionArrowLength, 10);
				}
				//g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				//g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

				int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }
               
                for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                {
                    try
                    {
                        Color currentPenColor = renderSettings.OutlineColor;
                        int penWidth = (int)Math.Round(renderPenWidth);

                        if (paintCount == 1)
                        {
                            currentPenColor = renderSettings.FillColor;
                            penWidth -= 2;
                        }

                        if (paintCount == 0)
                        {
                            Color c = renderSettings.OutlineColor;
                            //gdiplusPenPtr  = NativeMethods.CreateGdiplusPen(c.ToArgb(), penWidth);
                            gdiplusPen = new Pen(renderSettings.OutlineColor, penWidth);
                            selectPen = new Pen(renderSettings.SelectOutlineColor, penWidth + 4);
                        }
                        else if (paintCount == 1)
                        {
                            gdiplusPen = new Pen(renderSettings.FillColor, penWidth);
                            selectPen = new Pen(renderSettings.SelectFillColor, penWidth);
                            if (penWidth > 1)
                            {
                                renderRailway = (renderSettings.LineType == LineType.Railway);
                                if (renderRailway)
                                {
                                    rwPen = new Pen(renderSettings.OutlineColor, 1);
                                }
                            }
                        }
						gdiplusPen.EndCap = renderSettings.LineEndCap;//  System.Drawing.Drawing2D.LineCap.Round;
						gdiplusPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
						gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
						selectPen.EndCap = renderSettings.LineEndCap;// System.Drawing.Drawing2D.LineCap.Round;
						selectPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
						selectPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
						if (renderSettings.LineType == LineType.Solid)
						{
							gdiplusPen.DashStyle = renderSettings.LineDashStyle;
							selectPen.DashStyle = renderSettings.LineDashStyle;
						}

						byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                        if (fileMapped)
                        {
                            dataPtrZero = (byte*)mapView.ToPointer();
                            dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                        }
                        else
                        {
                            shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                        }

                        PointF pt = new PointF(0, 0);
                        RecordHeader[] pgRecs = this.RecordHeaders;

                        int index = 0;
                        byte[] data = SFRecordCol.SharedBuffer;
                        float minLabelLength = (float)(6 / scaleX);
                        fixed (byte* dataBufPtr = data)
                        {
                            if (!fileMapped) dataPtr = dataBufPtr;
                            while (index < pgRecs.Length)
                            {
                                if (fileMapped)
                                {
                                    dataPtr = dataPtrZero + pgRecs[index].Offset;
                                }
                                else
                                {
                                    if (shapeFileStream.Position != pgRecs[index].Offset)
                                    {
                                        Console.Out.WriteLine("offset wrong");
                                        shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                                    }
                                    shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                                }
                                PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                                int dataOffset = nextRec->PointDataOffset;
                                bool renderShape = recordVisible[index];
                                //overwrite if using CustomRenderSettings
                                if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                                RectangleD recordBounds = nextRec->bounds.ToRectangleD();
                                if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(recordBounds) && renderShape)
                                {
                                    //check if the simplifiedDataBuffer sized needs to be increased
                                    if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                                    {
                                        simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                                    }
                                    if (simplificationDistance <= double.Epsilon)
                                    {
                                        simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                                    }

                                    fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                                    {

                                        int numParts = nextRec->NumParts;
                                        Point[] pts;

                                            
                                        if (useCustomRenderSettings)
                                        {
                                            Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                            if (customColor.ToArgb() != currentPenColor.ToArgb())
                                            {
                                                gdiplusPen = new Pen(customColor, penWidth);
                                                currentPenColor = customColor;
												gdiplusPen.EndCap = renderSettings.LineEndCap;// System.Drawing.Drawing2D.LineCap.Round;
												gdiplusPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
                                                gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                                                if (renderSettings.LineType == LineType.Solid)
                                                {
                                                    gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                                                }
                                            }
                                        }

										for (int partNum = 0; partNum < numParts; partNum++)
										{
											int numPoints;
											if ((numParts - partNum) > 1)
											{
												numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
											}
											else
											{
												numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
											}
											if (numPoints <= 1)
											{
												continue;
											}


											//reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
											int usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

											if (coordinateTransformation != null)
											{
												coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
											}

											Rectangle pixelBounds = new Rectangle();
											pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, ref pixelBounds, MercProj);

											//pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, MercProj);

											//add any labels to the poly-lines
											if (labelFields && paintCount == 0 && usePolySimplificationLabeling)
											{
												//check what the scaled bounds of this part are
												//if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
												if ((nextRec->bounds.Width > 6 * simplificationDistance) || (nextRec->bounds.Height > 6 * simplificationDistance))
												{
													int usedTempPoints = 0;
													NativeMethods.SimplifyDouglasPeucker(pts, pts.Length, 2, tempPoints, ref usedTempPoints);
													if (usedTempPoints > 0)
													{
														RenderPtObj.AddRenderPtObjects(tempPoints, usedTempPoints, renderPtObjList, index, 6);
													}
												}
											}

											List<Point[]> pointList = new List<Point[]>();
											if (pixelBounds.Left < -1000 || pixelBounds.Top < -1000 || pixelBounds.Right > clientArea.Width + 1000 || pixelBounds.Bottom > clientArea.Height + 1000)
											{
												//clip the polyline to avoid overflow
												GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
												{
													XMin = 0,
													YMin = 0,
													XMax = clientArea.Width,
													YMax = clientArea.Height
												};
												List<int> clippedPoints = new List<int>();
												List<int> clippedParts = new List<int>();
												GeometryAlgorithms.PolyLineClip(pts, pts.Length, clipBounds, clippedPoints, clippedParts);
												for (int clipPartIndex = 0; clipPartIndex < clippedParts.Count; ++clipPartIndex)
												{
													int startIndex = clippedParts[clipPartIndex];
													int endIndex = clipPartIndex < clippedParts.Count - 1 ? clippedParts[clipPartIndex + 1] : clippedPoints.Count;
													Point[] partPoints = new Point[(endIndex - startIndex) >> 1];
													for (int s = startIndex, p = 0; s < endIndex; s += 2, ++p)
													{
														partPoints[p].X = clippedPoints[s];
														partPoints[p].Y = clippedPoints[s + 1];
													}
													pointList.Add(partPoints);
												}
											}
											else
											{
												pointList.Add(pts);
											}
											foreach (var partPoints in pointList)
											{
												pts = partPoints;

												if (recordSelected[index] && selectPen != null)
												{
													g.DrawLines(selectPen, pts);
												}
												else
												{
													g.DrawLines(gdiplusPen, pts);
												}
												if (renderRailway)
												{
													int th = (int)Math.Ceiling((renderPenWidth + 2) / 2);
													int tw = (int)Math.Max(Math.Round((7f * th) / 7), 5);
													int pIndx = 0;
													int lx = 0;
													System.Drawing.Drawing2D.Matrix savedMatrix = g.Transform;
													try
													{
														while (pIndx < pts.Length - 1)
														{
															//draw the next line segment

															int dy = pts[pIndx + 1].Y - pts[pIndx].Y;
															int dx = pts[pIndx + 1].X - pts[pIndx].X;
															float a = (float)(Math.Atan2(dy, dx) * 180f / Math.PI);
															int d = (int)Math.Round(Math.Sqrt(dy * dy + dx * dx));
															if (d > 0)
															{
																g.Transform = savedMatrix;
																if (Math.Abs(a) > 90f && Math.Abs(a) <= 270f)
																{
																	a -= 180f;
																	g.TranslateTransform(pts[pIndx + 1].X, pts[pIndx + 1].Y);
																	g.RotateTransform(a);
																	while (lx < d)
																	{
																		g.DrawLine(rwPen, lx, -th, lx, th);
																		lx += tw;
																	}
																	lx -= d;
																}
																else
																{
																	g.TranslateTransform(pts[pIndx].X, pts[pIndx].Y);
																	g.RotateTransform(a);
																	while (lx < d)
																	{
																		g.DrawLine(rwPen, lx, -th, lx, th);
																		lx += tw;
																	}
																	lx -= d;
																}
															}

															pIndx++;
														}
													}
													finally
													{
														g.Transform = savedMatrix;
													}
												}

											}

											if (drawArrows && paintCount == (maxPaintCount - 1))
											{
												DrawDirectionArrows(pointList, arrowLength, arrowPen, g);
											}
										}
                                    }
                                }
                                    
                                ++index;
                            }
                        }
                    }
                    finally
                    {
                        if (gdiplusPen != null) gdiplusPen.Dispose();
                        if (rwPen != null) rwPen.Dispose();
                        if (selectPen != null) selectPen.Dispose();							
                    }
                }
               
                System.Drawing.Drawing2D.Matrix savedMatrix2 = g.Transform;                                                    
                try
                {
                    if (labelFields)
                    {
                        bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                        Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                        Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                        pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                        int count = renderPtObjList.Count;
                        Color currentFontColor = renderSettings.FontColor;
                        bool useCustomFontColor = customRenderSettings != null;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                        float ws = 1.2f;
                        if (renderDuplicateFields) ws = 1f;
                        float ssm = shadowText ? 0.8f : 1f;
                        for (int n = 0; n < count; n++)
                        {
                            string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                            if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                            {
                                SizeF strSize = g.MeasureString(strLabel, renderSettings.Font);
                                strSize = new SizeF(strSize.Width * ssm, strSize.Height * ssm);
                                if (strSize.Width <= (renderPtObjList[n].Point0Dist) * ws)
                                {
                                    g.Transform = savedMatrix2;                                    
                                    g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                    g.RotateTransform(-renderPtObjList[n].Angle0);

                                    if (useCustomFontColor)
                                    {
                                        Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                        if (customColor.ToArgb() != currentFontColor.ToArgb())
                                        {
                                            fontBrush.Dispose();
                                            fontBrush = new SolidBrush(customColor);
                                            currentFontColor = customColor;
                                        }
                                    }

                                    if (shadowText)
                                    {
                                        System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                        gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF((renderPtObjList[n].Point0Dist - strSize.Width) / 2, -(strSize.Height) / 2), StringFormat.GenericDefault);//new StringFormat());
                                        g.DrawPath(pen, gp);
                                        g.FillPath(fontBrush, gp);
                                    }
                                    else
                                    {
                                        g.DrawString(strLabel, renderSettings.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);
                                    }
                                    
                                }
                            }
                        }
                        fontBrush.Dispose();
                        pen.Dispose();
                    }
                }
                finally
                {
                    g.Transform = savedMatrix2;
                }
            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
                tempPoints = null;
				if (arrowPen != null) arrowPen.Dispose();
			}
        }
        

        private unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            IntPtr dc = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiObj = IntPtr.Zero;
            IntPtr selectPen = IntPtr.Zero;                

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
               // mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ , 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);
            
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;

            if (double.IsNaN(scaleX) || Math.Abs(scaleX) < double.Epsilon)
            {
                simplificationDistance = 0;
            }
            else
            {
                simplificationDistance = 1 / scaleX;
            }

            //Console.Out.WriteLine("simplificationDistance = " + simplificationDistance);

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD testExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                testExtent = targetExtent;
            }

            bool MercProj = projectionType == ProjectionType.Mercator;

            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(testExtent.Left, testExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(testExtent.Right, testExtent.Bottom));
                testExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }
            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);

            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderDuplicateFields = (labelFields && renderSettings.RenderDuplicateFields);
            const bool usePolySimplificationLabeling = true;
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

            float renderPenWidth = (float)(renderSettings.PenWidthScale * scaleX);

            if (renderSettings.MaxPixelPenWidth > 0 && renderPenWidth > renderSettings.MaxPixelPenWidth)
            {
                renderPenWidth = renderSettings.MaxPixelPenWidth;
            }
            if (renderSettings.MinPixelPenWidth > 0 && renderPenWidth < renderSettings.MinPixelPenWidth)
            {
                renderPenWidth = renderSettings.MinPixelPenWidth;
            }

            if (renderSettings.LineType == LineType.Railway)
            {
                renderPenWidth = Math.Min(renderPenWidth, 7f);
            }

            // obtain a handle to the Device Context so we can use GDI to perform the painting.
            dc = g.GetHdc();

            Point[] labelPoints = new Point[SFRecordCol.SharedPointBuffer.Length];
            try
            {
                int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }
                NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                {
                    try
                    {                       
                        Color currentPenColor = renderSettings.OutlineColor;
                        int penWidth = (int)Math.Round(renderPenWidth);
                        int color = ColorToGDIColor(renderSettings.OutlineColor);
                        
                        if (paintCount == 1)
                        {
                            currentPenColor = renderSettings.FillColor;
                            color = ColorToGDIColor(renderSettings.FillColor);                       
                            penWidth -= 2;
                        }

                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, penWidth, color);
                        oldGdiObj = NativeMethods.SelectObject(dc, gdiPen);

                        if (paintCount == 0)
                        {
                            selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, penWidth+4, ColorToGDIColor(renderSettings.SelectOutlineColor));
                        }
                        else
                        {
                            selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, penWidth, ColorToGDIColor(renderSettings.SelectFillColor));
                        }

                        byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                        if (fileMapped)
                        {
                            dataPtrZero = (byte*)mapView.ToPointer();
                            dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                        }
                        else
                        {
                            shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                        }

                        RecordHeader[] pgRecs = this.RecordHeaders;

                        int index = 0;
                        byte[] data = SFRecordCol.SharedBuffer;
                        Point[] sharedPoints = SFRecordCol.SharedPointBuffer;
                        float minLabelLength = (float)(6 / scaleX);
                        fixed (byte* dataBufPtr = data)
                        {
                            if (!fileMapped) dataPtr = dataBufPtr;
                            while (index < pgRecs.Length)
                            {
                                if (fileMapped)
                                {
                                    dataPtr = dataPtrZero + pgRecs[index].Offset;
                                }
                                else
                                {
                                    if (shapeFileStream.Position != pgRecs[index].Offset)
                                    {
                                        Console.Out.WriteLine("offset wrong");
                                        shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                                    }
                                    shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                                }
                                PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                                int dataOffset = nextRec->PointDataOffset;
                                bool renderShape = recordVisible[index];
                                //overwrite if using CustomRenderSettings
                                if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);

                                RectangleD recordBounds = nextRec->bounds.ToRectangleD();
                                BoundsTestResult boundsTestResult = TestBoundsIntersect(recordBounds, testExtent, coordinateTransformation);

                                if (nextRec->ShapeType != ShapeType.NullShape && boundsTestResult != BoundsTestResult.NoIntersection && renderShape)
                                {
                                    //check if the simplifiedDataBuffer sized needs to be increased
                                    if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                                    {
                                        simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                                    }

                                    if (simplificationDistance <= double.Epsilon)
                                    {
                                        simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                                    }

                                    fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                                    {
                                        int numParts = nextRec->NumParts;
                                        
                                        if (useCustomRenderSettings)
                                        {
                                            Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                            if (customColor.ToArgb() != currentPenColor.ToArgb())
                                            {
                                                gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, penWidth, ColorToGDIColor(customColor));
                                                NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                                currentPenColor = customColor;
                                            }
                                        }

                                        for (int partNum = 0; partNum < numParts; ++partNum)
                                        {
                                            int numPoints;
                                            if ((numParts - partNum) > 1)
                                            {
                                                numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                            }
                                            else
                                            {
                                                numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                            }
                                            if (numPoints <= 1)
                                            {
                                                //Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                                continue;
                                            }
                                            int usedPoints = 0;

                                            //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                            usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)),numPoints,simplifiedDataPtr,simplificationDistance);
                                            
                                            if (coordinateTransformation != null)
                                            {
                                                int transformCount = coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                                if (transformCount != usedPoints)
                                                {
                                                    //Console.Out.WriteLine("transformcount={0}, usedpoints={1}", transformCount, usedPoints);
                                                }
                                            }


                                            //GetPointsRemoveDuplicatesD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref offX, ref offY, ref scaleX, sharedPoints, ref usedPoints, MercProj);

                                            GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr,0, usedPoints, ref offX, ref offY, ref scaleX, sharedPoints, ref usedPoints, MercProj);

                                            //add any labels to the poly-lines
                                            if (labelFields && paintCount == 0 && usePolySimplificationLabeling)
                                            {
                                                //check what the scaled bounds of this part are
                                                //if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
                                                if ((nextRec->bounds.Width > 6 * simplificationDistance) || (nextRec->bounds.Height > 6 * simplificationDistance))
                                                {
                                                    int usedTempPoints = 0;
                                                    NativeMethods.SimplifyDouglasPeucker(sharedPoints, usedPoints, 2, labelPoints, ref usedTempPoints);
                                                    if (usedTempPoints > 0)
                                                    {
                                                        RenderPtObj.AddRenderPtObjects(labelPoints, usedTempPoints, renderPtObjList, index, 6);
                                                    }
                                                }
                                            }

                                            if (recordSelected[index])
                                            {
                                                IntPtr tempPen = IntPtr.Zero;
                                                try
                                                {
                                                    tempPen = NativeMethods.SelectObject(dc, selectPen);
                                                    NativeMethods.DrawPolyline(dc, sharedPoints, usedPoints);
                                                }
                                                finally
                                                {
                                                    if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                                }
                                            }
                                            else
                                            {
                                                NativeMethods.DrawPolyline(dc, sharedPoints, usedPoints);
                                            }
                                        }
                                    }
                                }
                                ++index;
                            }
                        }
                    }
                    finally
                    {
                        if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                        if (oldGdiObj != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiObj);
                        if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                        selectPen = IntPtr.Zero;                    
                    }
                }
            }

            finally
            {
                labelPoints = null;
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
            System.Drawing.Drawing2D.Matrix savedMatrix = g.Transform;                                                    
            try
            {
                if (labelFields)
                {
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    int count = renderPtObjList.Count;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    float ws = 1.2f;
                    if (renderDuplicateFields) ws = 1f;
                    float ssm = shadowText ? 0.8f : 1f;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                        {
                            SizeF strSize = g.MeasureString(strLabel, renderSettings.Font);
                            strSize = new SizeF(strSize.Width * ssm, strSize.Height * ssm);
                            if (strSize.Width <= (renderPtObjList[n].Point0Dist) * ws)
                            {
                                g.Transform = savedMatrix;                                                    
                                g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                g.RotateTransform(-renderPtObjList[n].Angle0);

                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF((renderPtObjList[n].Point0Dist - strSize.Width) / 2, -(strSize.Height) / 2), StringFormat.GenericDefault);//new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);
                                }
                            }
                        }
                    }
                    fontBrush.Dispose();
                    pen.Dispose();
                }
            }
            finally
            {
                g.Transform = savedMatrix;
            }

        }

        
        #endregion


        public bool ContainsPoint(int shapeIndex, PointD pt, System.IO.Stream shapeFileStream, byte[] dataBuf, double minDist)
        {
            unsafe
            {
                byte[] data = SFRecordCol.SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset;
                    int numParts = nextRec->NumParts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1)
                        {
                            numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        }
                        else
                        {
                            numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        }
                        if (GeometryAlgorithms.PointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, pt, minDist))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override unsafe bool IntersectRect(int shapeIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with rect
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!recBounds.IntersectsWith(rect)) return false;

            //next check if rect entirely contains the polyline
            if (rect.Contains(recBounds)) return true;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    if (IntersectsRect(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref rect))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static unsafe bool IntersectsRect(byte[] data, int offset, int numPoints, ref RectangleD rect)
        {
            fixed (byte* ptr = data)
            {
                return NativeMethods.PolyLineRectIntersect(ptr + offset, numPoints, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }

        public override unsafe bool IntersectCircle(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    if (GeometryAlgorithms.PolylineCircleIntersects(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, radius))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override unsafe double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            // RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;
            double foundDistance = double.PositiveInfinity;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            
            fixed (byte* dataPtr = data)
            {
                PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    double polylineDistance = GeometryAlgorithms.ClosestPointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre);
                    if (polylineDistance < foundDistance) foundDistance = polylineDistance;
                }
            }
            return foundDistance;
        }

        public override unsafe double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            //first check if the record's bounds intersects with the circle
            //RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return double.PositiveInfinity;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            
            fixed (byte* dataPtr = data)
            {                
                PolyLineMRecordP* nextRec = (PolyLineMRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    PolylineDistanceInfo pdi;
                    double polylineDistance = GeometryAlgorithms.ClosestPointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, out pdi);
                    if (polylineDistance < polylineDistanceInfo.Distance)
                    {
                        pdi.PointIndex += nextRec->PartOffsets[partNum];
                        polylineDistanceInfo = pdi;
                    } 
                }
            }
            return polylineDistanceInfo.Distance;
        }
        
        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleD();
        }


        #endregion

    }

    class SFPolygonZCol : SFRecordCol, QTNodeHelper
    {

        public SFPolygonZCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            int length = SFRecordCol.GetMaxRecordLength(recs) + 100;
            SFRecordCol.EnsureBufferSize(length);
            base.SetMaxRecordLength(length);
            //SFRecordCol.EnsureBufferSize(SFRecordCol.GetMaxRecordLength(recs) + 100);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset+8;
                    int numParts = nextRec->NumParts;
                    PointF[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsDAsPointF(dataPtr,  dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);                        
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset;
                    int numParts = nextRec->NumParts;
                    PointD[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }


        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeHeightData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {            
            List<float[]> dataList = new List<float[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->ZDataOffset + 8;
                    int numParts = nextRec->NumParts;
                    float[] heights;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        heights = GetFloatDataD(dataPtr, dataOffset, numPoints);
                        dataOffset += numPoints << 3;
                        dataList.Add(heights);
                    }
                }
            }
            return dataList;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<double[]> dataList = new List<double[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->ZDataOffset + 8;
                    int numParts = nextRec->NumParts;
                    double[] heights;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        heights = GetDoubleData(dataPtr, dataOffset, numPoints);
                        dataOffset += numPoints << 3;
                        dataList.Add(heights);
                    }
                }
            }
            return dataList;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            throw new NotImplementedException("GetShapeMDataD has not been implemented for PolygonZ shape type");
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null,RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            if (this.UseGDI(extent, renderSettings))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              

                paintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);

                //paintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              

                paintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
        }

        private unsafe void paintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            Pen gdiplusPen = null;
            Brush gdiplusBrush = null;
            bool renderInterior = true;
            Pen selectPen = null;
            Brush selectBrush = null;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            simplificationDistance = 1 / scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;

                // set simplificationDistance to zero if we're using a trnasformation - we'll calculate it
                // when we render the first record
                simplificationDistance = 0;
            }

            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;

            bool MercProj = projectionType == ProjectionType.Mercator;

            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }

            bool labelfields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            if (renderSettings != null) renderInterior = renderSettings.FillInterior;
            List<PartBoundsIndex> partBoundsIndexList = new List<PartBoundsIndex>(256);

           
            try
            {
                // setup our pen and brush                
                gdiplusBrush = new SolidBrush(renderSettings.FillColor);
                gdiplusPen = new Pen(renderSettings.OutlineColor, 1);                
                selectPen = new Pen(renderSettings.SelectOutlineColor, 4f);
                selectBrush = new SolidBrush(renderSettings.SelectFillColor);                
                gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                selectPen.DashStyle = renderSettings.LineDashStyle;
                
                byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                if (fileMapped)
                {
                    dataPtrZero = (byte*)mapView.ToPointer();
                    dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                }
                else
                {
                    shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                }


                PointF pt = new PointF(0, 0);
                RecordHeader[] pgRecs = this.RecordHeaders;

                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                fixed (byte* dataBufPtr = data)
                {
                    //dataPtr = dataBufPtr;
                    if (!fileMapped) dataPtr = dataBufPtr;

                    while (index < pgRecs.Length)
                    {
                        Rectangle partBounds = Rectangle.Empty;                        
                        if (fileMapped)
                        {
                            dataPtr = dataPtrZero + pgRecs[index].Offset;
                        }
                        else
                        {
                            if (shapeFileStream.Position != pgRecs[index].Offset)
                            {
                                Console.Out.WriteLine("offset wrong");
                                shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                            }
                            shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                        }
                        
                        PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);// new PolygonRecordP(pgRecs[index]);
                        int dataOffset = nextRec->PointDataOffset;
                        bool renderShape = recordVisible[index];
                        //overwrite if using CustomRenderSettings
                        if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                        RectangleD recordBounds = nextRec->bounds.ToRectangleD();

                        if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(recordBounds) && renderShape)
                        {
                            //check if the simplifiedDataBuffer sized needs to be increased
                            if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                            {
                                simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                            }
                            if (simplificationDistance <= double.Epsilon)
                            {
                                simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                            }
                            fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                            {

                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiplusPen = new Pen(customColor, 1);
                                        gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiplusBrush = new SolidBrush(customColor);
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }
                                int numParts = nextRec->NumParts;
                                Point[] pts;
                                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                                gp.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

                                for (int partNum = 0; partNum < numParts; ++partNum)
                                {
                                    int numPoints;
                                    if ((numParts - partNum) > 1)
                                    {
                                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                    }
                                    else
                                    {
                                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                    }
                                    if (numPoints <= 1)
                                    {
                                        //Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                        continue;
                                    }

                                    //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                    int usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                    }

                                    if (usedPoints == 2)
                                    {
                                        simplifiedDataPtr[usedPoints << 1] = simplifiedDataPtr[0];
                                        simplifiedDataPtr[(usedPoints << 1) + 1] = simplifiedDataPtr[1];
                                        ++usedPoints;
                                    }


                                    pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, ref partBounds, MercProj);

                                    if (pts.Length == 3 && pts[0].X == pts[1].X && pts[0].Y == pts[1].Y && pts[0].X == pts[2].X && pts[0].Y == pts[2].Y)
                                    {
                                        //ensure the polygon points are not all identical or GDI+ throws an OutOfMemoryException
                                        //if the pen dashstyle is not solid. Also makes tiny polygons visible when zoomed out
                                        pts[1].Y = pts[1].Y + 1;                                        
                                    }

                                    if (partBounds.IntersectsWith(new Rectangle(0, 0, clientArea.Width, clientArea.Height)))
                                    {
                                        if (partBounds.Left < -1000 || partBounds.Top < -1000 || partBounds.Right > clientArea.Width + 1000 || partBounds.Bottom > clientArea.Height + 1000)
                                        {
                                            //clip the polygon to avoid overflow
                                            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
                                            {
                                                XMin = 0,
                                                YMin = 0,
                                                XMax = clientArea.Width,
                                                YMax = clientArea.Height
                                            };
                                            List<Point> clippedPoints = new List<Point>();
                                            GeometryAlgorithms.PolygonClip(pts, pts.Length, clipBounds, clippedPoints);
                                            pts = clippedPoints.ToArray();
                                        }

                                        if (pts.Length > 0)
                                        {
                                            gp.AddPolygon(pts);

                                            if (labelfields)
                                            {
                                                if (partBounds.Width > 5 && partBounds.Height > 5)
                                                {
                                                    partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                                }
                                            }
                                        }

                                    }
                                   
                                }
                                if (recordSelected[index])
                                {
                                    if (renderInterior)
                                    {
                                        g.FillPath(selectBrush, gp);
                                    }
                                    g.DrawPath(selectPen, gp);
                                }
                                else
                                {
                                    if (renderInterior)
                                    {
                                        g.FillPath(gdiplusBrush, gp);
                                    }
                                    g.DrawPath(gdiplusPen, gp);
                                }
                                gp.Dispose();
                            }
                        }
                        ++index;
                    }
                }
            }
            finally
            {
                if (gdiplusPen != null) gdiplusPen.Dispose();
                if (gdiplusBrush != null) gdiplusBrush.Dispose();
                if (selectPen != null) selectPen.Dispose();
                if (selectBrush != null) selectBrush.Dispose();
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }
            if (labelfields)
            {

                PointF pt = new PointF(0, 0);
                int count = partBoundsIndexList.Count;
                Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                Color currentFontColor = renderSettings.FontColor;
                bool useCustomFontColor = customRenderSettings != null;
                for (int n = 0; n < count; n++)
                {
                    int index = partBoundsIndexList[n].RecIndex;
                    string strLabel = renderSettings.DbfReader.GetField(index, renderSettings.FieldIndex);
                    if (!string.IsNullOrEmpty(strLabel) && g.MeasureString(strLabel, renderSettings.Font).Width <= partBoundsIndexList[n].Bounds.Width)
                    {
                        pt.X = partBoundsIndexList[n].Bounds.Left + (partBoundsIndexList[n].Bounds.Width >> 1);
                        pt.Y = partBoundsIndexList[n].Bounds.Top + (partBoundsIndexList[n].Bounds.Height >> 1);
                        pt.X -= (g.MeasureString(strLabel, renderSettings.Font).Width / 2f);
                        if (useCustomFontColor)
                        {
                            Color customColor = customRenderSettings.GetRecordFontColor(index);
                            if (customColor.ToArgb() != currentFontColor.ToArgb())
                            {
                                fontBrush.Dispose();
                                fontBrush = new SolidBrush(customColor);
                                currentFontColor = customColor;
                            }
                        }
                        if (shadowText)
                        {
                            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                            gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, pt, StringFormat.GenericDefault);//new StringFormat());
                            g.DrawPath(pen, gp);
                            g.FillPath(fontBrush, gp);
                        }
                        else
                        {
                            g.DrawString(strLabel, renderSettings.Font, fontBrush, pt);
                            //g.DrawString(strLabel, renderer.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);                            
                        }
                    }
                }
                fontBrush.Dispose();
            }
        }

        private unsafe void paintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            IntPtr dc = IntPtr.Zero;
            IntPtr gdiBrush = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiBrush = IntPtr.Zero;
            IntPtr oldGdiPen = IntPtr.Zero;
            IntPtr selectPen = IntPtr.Zero;
            IntPtr selectBrush = IntPtr.Zero;  

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            bool renderInterior = true;
            
            //double scaleX = (clientArea.Width / extent.Width);
            //double scaleY = -scaleX;

            //RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            //double offX = -projectedExtent.Left;
            //double offY = -projectedExtent.Bottom;
            //RectangleD actualExtent = projectedExtent;
            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            simplificationDistance = 1 / scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;
                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }

            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;
            bool MercProj = projectionType == ProjectionType.Mercator;
            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }

            bool labelfields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            if (renderSettings != null) renderInterior = renderSettings.FillInterior;
            List<PartBoundsIndex> partBoundsIndexList = new List<PartBoundsIndex>(256);

            // obtain a handle to the Device Context so we can use GDI to perform the painting.            
            dc = g.GetHdc();

            try
            {
                // setup our pen and brush
                int color = ColorToGDIColor(renderSettings.FillColor);
                
                if (renderInterior)
                {
                    gdiBrush = NativeMethods.CreateSolidBrush(color);
                    oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                }
                else
                {
                    oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                }

                color = ColorToGDIColor(renderSettings.OutlineColor);                
                gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, color);
                oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 4, ColorToGDIColor(renderSettings.SelectOutlineColor));
                selectBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(renderSettings.SelectFillColor));

                NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);

                byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                if (fileMapped)
                {
                    dataPtrZero = (byte*)mapView.ToPointer();
                    dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                }
                else
                {
                    shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                }


                PointF pt = new PointF(0, 0);
                RecordHeader[] pgRecs = this.RecordHeaders;
                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                fixed (byte* dataBufPtr = data)
                {
                    if (!fileMapped) dataPtr = dataBufPtr;

                    while (index < pgRecs.Length)
                    {
                        Rectangle partBounds = Rectangle.Empty;
                        if (fileMapped)
                        {
                            dataPtr = dataPtrZero + pgRecs[index].Offset;
                        }
                        else
                        {
                            if (shapeFileStream.Position != pgRecs[index].Offset)
                            {
                                Console.Out.WriteLine("offset wrong");
                                shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                            }
                            shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                        }
                        PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);
                        int dataOffset = nextRec->PointDataOffset;
                        bool renderShape = recordVisible[index];
                        //overwrite if using CustomRenderSettings
                        if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                        if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)
                        {
                            //check if the simplifiedDataBuffer sized needs to be increased
                            if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                            {
                                simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                            }
                            fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                            {
                                if (useCustomRenderSettings)
                                {
                                    Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                    if (customColor.ToArgb() != currentPenColor.ToArgb())
                                    {
                                        gdiPen = NativeMethods.CreatePen((int)renderSettings.LineDashStyle, 1, ColorToGDIColor(customColor));
                                        NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                        currentPenColor = customColor;
                                    }
                                    if (renderInterior)
                                    {
                                        customColor = customRenderSettings.GetRecordFillColor(index);
                                        if (customColor.ToArgb() != currentBrushColor.ToArgb())
                                        {
                                            gdiBrush = NativeMethods.CreateSolidBrush(ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiBrush));
                                            currentBrushColor = customColor;
                                        }
                                    }
                                }
                                int numParts = nextRec->NumParts;
                                for (int partNum = 0; partNum < numParts; ++partNum)
                                {
                                    int numPoints;
                                    if ((numParts - partNum) > 1)
                                    {
                                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                    }
                                    else
                                    {
                                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                    }
                                    if (numPoints <= 1)
                                    {
                                        //Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                        continue;
                                    }

                                    //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                    int usedPoints = ReducePoints((double*)(dataPtr + 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                    if (coordinateTransformation != null)
                                    {
                                        coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                    }

                                    if (usedPoints == 2)
                                    {
                                        simplifiedDataPtr[usedPoints << 1] = simplifiedDataPtr[0];
                                        simplifiedDataPtr[(usedPoints << 1) + 1] = simplifiedDataPtr[1];
                                        ++usedPoints;
                                    }

                                    if (labelfields)
                                    {
                                        //GetPointsRemoveDuplicatesD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, offX, offY, scaleX, scaleY, ref partBounds, sharedPointBuffer, ref usedPoints, MercProj);
                                        GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr,0, usedPoints, offX, offY, scaleX, scaleY, ref partBounds, sharedPointBuffer, ref usedPoints, MercProj);
                                        if (partBounds.Width > 5 && partBounds.Height > 5)
                                        {
                                            partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                        }
                                    }
                                    else
                                    {
                                        //GetPointsRemoveDuplicatesD(dataPtr, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref offX, ref offY, ref scaleX, sharedPointBuffer, ref usedPoints, MercProj);
                                        GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr,0, usedPoints, ref offX, ref offY, ref scaleX, sharedPointBuffer, ref usedPoints, MercProj);
                                    }
                                    if (recordSelected[index])
                                    {
                                        IntPtr tempPen = IntPtr.Zero;
                                        IntPtr tempBrush = IntPtr.Zero;
                                        try
                                        {
                                            tempPen = NativeMethods.SelectObject(dc, selectPen);
                                            if (renderInterior)
                                            {
                                                tempBrush = NativeMethods.SelectObject(dc, selectBrush);
                                            }
                                            NativeMethods.DrawPolygon(dc, sharedPointBuffer, usedPoints);
                                        }
                                        finally
                                        {
                                            if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                            if (tempBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, tempBrush);
                                        }
                                    }
                                    else
                                    {
                                        NativeMethods.DrawPolygon(dc, sharedPointBuffer, usedPoints);
                                    }
                                }
                            }
                        }                       
                        ++index;
                    }
                }
            }
            finally
            {
                if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                if (selectBrush != IntPtr.Zero) NativeMethods.DeleteObject(selectBrush);
                if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);                    
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);

                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
            }

            if (labelfields)
            {

                PointF pt = new PointF(0, 0);
                int count = partBoundsIndexList.Count;
                Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                Color currentFontColor = renderSettings.FontColor;
                bool useCustomFontColor = customRenderSettings != null;
                for (int n = 0; n < count; n++)
                {
                    int index = partBoundsIndexList[n].RecIndex;
                    string strLabel = renderSettings.DbfReader.GetField(index, renderSettings.FieldIndex);
                    if (!string.IsNullOrEmpty(strLabel) && g.MeasureString(strLabel, renderSettings.Font).Width <= partBoundsIndexList[n].Bounds.Width)
                    {
                        pt.X = partBoundsIndexList[n].Bounds.Left + (partBoundsIndexList[n].Bounds.Width >> 1);
                        pt.Y = partBoundsIndexList[n].Bounds.Top + (partBoundsIndexList[n].Bounds.Height >> 1);
                        pt.X -= (g.MeasureString(strLabel, renderSettings.Font).Width / 2f);
                        if (useCustomFontColor)
                        {
                            Color customColor = customRenderSettings.GetRecordFontColor(index);
                            if (customColor.ToArgb() != currentFontColor.ToArgb())
                            {
                                fontBrush.Dispose();
                                fontBrush = new SolidBrush(customColor);
                                currentFontColor = customColor;
                            }
                        }
                        if (shadowText)
                        {
                            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                            gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, pt, StringFormat.GenericDefault);//new StringFormat());
                            g.DrawPath(pen, gp);
                            g.FillPath(fontBrush, gp);
                        }
                        else
                        {
                            g.DrawString(strLabel, renderSettings.Font, fontBrush, pt);
                            //g.DrawString(strLabel, renderer.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);                            
                        }
                    }
                }
                fontBrush.Dispose();
            }
        }


        internal unsafe bool ContainsPoint(int shapeIndex, PointD pt, System.IO.Stream shapeFileStream)
        {
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            bool inPolygon = false;
            fixed (byte* dataPtr = data)
            {
                PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;//nextRec.read(data, 8);
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    bool isHole = false;
                    bool partInPolygon = GeometryAlgorithms.PointInPolygon(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, pt.X, pt.Y, numParts == 1, ref isHole); //ignore holes if only 1 part
                    inPolygon |= partInPolygon;
                    if (isHole && partInPolygon)
                    {
                        return false;
                    }
                }
            }
            return inPolygon;
        }
        
        public override unsafe bool IntersectRect(int shapeIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with rect
            if (!GetRecordBoundsD(shapeIndex, shapeFileStream).IntersectsWith(rect)) return false;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                bool intersectsNonHolePolygon = false;
                PolygonRecordP* nextRec = (PolygonRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->DataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    bool withinHole, isHole;
                    if (IntersectsRect(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref rect, out withinHole, out isHole))
                    {
                        intersectsNonHolePolygon = true;
                        if (isHole) return true; //if the polygon is a hole and the rect is not within the hole then it intersects
                    }
                    if (withinHole) return false; //if the rect is within a hole then return false
                }
                return intersectsNonHolePolygon;
            }
            
        }       
                
        private static unsafe bool IntersectsRect(byte[] data, int offset, int numPoints, ref RectangleD rect, out bool withinHole, out bool isHole)
        {
            withinHole = isHole = false;
            if (GeometryAlgorithms.IsPolygonHole(data, offset, numPoints))
            {
                isHole = true;
                //check if the rect is within the hole
                fixed (byte* ptr = data)
                {
                    withinHole = NativeMethods.RectWithinPolygon(rect.Left, rect.Top, rect.Right, rect.Bottom, ptr + offset, numPoints);
                }
            }
            //the rect is inside the hole - return false
            if (withinHole) return false;

            fixed (byte* ptr = data)
            {
                return NativeMethods.PolygonRectIntersect(ptr + offset, numPoints, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }

        public override unsafe bool IntersectCircle(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                bool intersectsNonHolePolygon = false;
                PolygonZRecordP* nextRec = (PolygonZRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    
                    bool withinHole;
                    if (GeometryAlgorithms.PolygonCircleIntersects(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, radius, numParts == 1, out withinHole))
                    {
                        intersectsNonHolePolygon = true;
                        if (GeometryAlgorithms.IsPolygonHole(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints))
                        {
                            return true; //if the polygon is a hole and the circle intersects the hole then the record intersects
                        }
                    }
                    if (withinHole) return false; //if the circle is within a hole then return false                    
                }
                return intersectsNonHolePolygon;
            }
            //return false;
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            throw new NotImplementedException();
        }

        public override double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            throw new NotImplementedException();
        }

        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleD();
        }

        #endregion

        private struct PartBoundsIndex
        {
            public int RecIndex;
            public Rectangle Bounds;

            public PartBoundsIndex(int index, Rectangle r)
            {
                RecIndex = index;
                Bounds = r;
            }
        }
    }

    class SFPolyLineZCol : SFRecordCol, QTNodeHelper
    {

        public SFPolyLineZCol(RecordHeader[] recs, ref ShapeFileMainHeader head)
            : base(ref head, recs)
        {
            //this.RecordHeaders = recs;
            //SFRecordCol.EnsureBufferSize(SFRecordCol.GetMaxRecordLength(recs) + 100);
            int length = SFRecordCol.GetMaxRecordLength(recs) + 100;
            SFRecordCol.EnsureBufferSize(length);
            base.SetMaxRecordLength(length);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer/*new byte[ShapeFileExConstants.MAX_REC_LENGTH]*/);
        }

        public override List<PointF[]> GetShapeData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointF[]> dataList = new List<PointF[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);//8bytes for the rec num + length
                fixed (byte* dataPtr = data)
                {
                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset+8; //8bytes for the rec num + length
                    int numParts = nextRec->NumParts;
                    PointF[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsDAsPointF(dataPtr, dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<PointD[]> GetShapeDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<PointD[]> dataList = new List<PointD[]>();
            unsafe
            {
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset + 8;
                    int numParts = nextRec->NumParts;
                    PointD[] pts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        pts = GetPointsD(dataPtr, dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints);
                        dataList.Add(pts);
                    }
                }
            }
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream)
        {
            return GetShapeHeightData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<float[]> dataList = new List<float[]>();
            unsafe
            {
                const int RecordHeaderLength = 8;
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + RecordHeaderLength);
                fixed (byte* dataPtr = data)
                {
                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + RecordHeaderLength);
                    int dataOffset = nextRec->ZDataOffset + RecordHeaderLength + 16; //skip min/max range (16 bytes)
                    int numParts = nextRec->NumParts;
                    float[] heights;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        heights = GetFloatDataD(dataPtr, dataOffset, numPoints);
                        dataOffset += numPoints << 3;
                        dataList.Add(heights);
                    }
                }
            }
            return dataList;
        }

        public override List<double[]> GetShapeHeightDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            List<double[]> dataList = new List<double[]>();
            unsafe
            {
                const int RecordHeaderLength = 8;
                byte[] data = dataBuffer;                
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + RecordHeaderLength);
                fixed (byte* dataPtr = data)
                {
                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + RecordHeaderLength);
                    int dataOffset = nextRec->ZDataOffset + RecordHeaderLength + 16; //skip min/max range (16 bytes)
                    int numParts = nextRec->NumParts;
                    double[] heights;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        heights = GetDoubleData(dataPtr, dataOffset, numPoints);
                        dataOffset += numPoints << 3;
                        dataList.Add(heights);
                    }
                }
            }
            return dataList;
        }

        public override List<double[]> GetShapeMDataD(int recordIndex, Stream shapeFileStream, byte[] dataBuffer)
        {
            //Thanks to D Svolos for supplying code for this function
            List<double[]> dataList = new List<double[]>();
            unsafe
            {
                const int RecordHeaderLength = 8;                
                byte[] data = dataBuffer;
                shapeFileStream.Seek(this.RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[recordIndex].ContentLength + RecordHeaderLength);
                fixed (byte* dataPtr = data)
                {
                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + RecordHeaderLength);
                    int dataOffset = nextRec->ZMeasuresDataOffset + 16 + RecordHeaderLength; //skip min max measure (16 bytes)
                    int numParts = nextRec->NumParts;
                    double[] measures;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1) numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        else numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        measures = GetDoubleData(dataPtr, dataOffset, numPoints);
                        dataOffset += numPoints << 3;
                        dataList.Add(measures);
                    }
                }
            }
            return dataList;
        }

        #region Paint methods

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, ProjectionType projectionType)
        {
            paint(g, clientArea, extent, shapeFileStream, null, projectionType, null, RectangleD.Empty);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            DateTime tick = DateTime.Now;
            if (this.UseGDI(extent, renderSettings))
            {
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

				PaintHighQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            else
            {
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

				PaintHighQuality(g, clientArea, extent, shapeFileStream, renderSettings, projectionType, coordinateTransformation, targetExtent);
            }
            System.Diagnostics.Debug.WriteLine("PolylineZ render time:" + DateTime.Now.Subtract(tick).TotalSeconds + "s");
			
		}

        private unsafe void PaintHighQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            Pen gdiplusPen = null;
            Pen rwPen = null;
            bool renderRailway = false;

            Pen selectPen = null;

			Pen arrowPen = null;
			bool drawArrows = false;
			int arrowLength = 50;

			IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            try
            {
                //double scaleX = (double)(clientArea.Width / extent.Width);
                //double scaleY = -scaleX;

                //RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
                //double offX = -projectedExtent.Left;
                //double offY = -projectedExtent.Bottom;
                //RectangleD actualExtent = projectedExtent;

                double scaleX = (double)(clientArea.Width / extent.Width);
                double scaleY = -scaleX;

                simplificationDistance = 1 / scaleX;

                RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);

                double offX = -projectedExtent.Left;
                double offY = -projectedExtent.Bottom;
                RectangleD actualExtent = projectedExtent;

                if (coordinateTransformation != null)
                {
                    scaleX = (double)(clientArea.Width / targetExtent.Width);
                    scaleY = -scaleX;

                    projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                    offX = -projectedExtent.Left;
                    offY = -projectedExtent.Bottom;
                    //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                    //when the coordinateTransformation has been supplied the extent will already contain
                    //the actual intersecting extent in the shapefile CRS
                    actualExtent = extent;
                    //set simplificationDistance to zero if we're using a trnasformation - we'll calculate it 
                    //when we render the first record
                    simplificationDistance = 0;
                }

                bool MercProj = projectionType == ProjectionType.Mercator;
                Point[] tempPoints = new Point[SFRecordCol.SharedPointBuffer.Length];
            

                if (MercProj)
                {
                    //if we're using a Mercator Projection then convert the actual Extent to LL coords
                    PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                    PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                    actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                }
                ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
                bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);

                bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
                bool renderDuplicateFields = (labelFields && renderSettings.RenderDuplicateFields);
                List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

                float renderPenWidth = (float)(renderSettings.PenWidthScale * scaleX);

                if (renderSettings.MaxPixelPenWidth > 0 && renderPenWidth > renderSettings.MaxPixelPenWidth)
                {
                    renderPenWidth = renderSettings.MaxPixelPenWidth;
                }
                if (renderSettings.MinPixelPenWidth > 0 && renderPenWidth < renderSettings.MinPixelPenWidth)
                {
                    renderPenWidth = renderSettings.MinPixelPenWidth;
                }

                if (renderSettings.LineType == LineType.Railway)
                {
                    renderPenWidth = Math.Min(renderPenWidth, 7f);
                }

				if (renderSettings.DrawDirectionArrows && (renderSettings.DirectionArrowMinZoomLevel < 0 || scaleX >= renderSettings.DirectionArrowMinZoomLevel))
				{
					int arrowWidth = Math.Max(1, Math.Min(renderSettings.DirectionArrowWidth, 50));
					arrowPen = new Pen(renderSettings.DirectionArrowColor, arrowWidth);
					arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(3, 6);
					drawArrows = true;
					arrowLength = Math.Max(renderSettings.DirectionArrowLength, 10);
				}

				//g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				//g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

				int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }
                try
                {

                    for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                    {
                        try
                        {
                            Color currentPenColor = renderSettings.OutlineColor;
                            int penWidth = (int)Math.Round(renderPenWidth);

                            if (paintCount == 1)
                            {
                                currentPenColor = renderSettings.FillColor;
                                penWidth -= 2;
                            }

                            if (paintCount == 0)
                            {
                                Color c = renderSettings.OutlineColor;
                                gdiplusPen = new Pen(renderSettings.OutlineColor, penWidth);
                                selectPen = new Pen(renderSettings.SelectOutlineColor, penWidth + 4);
                            }
                            else if (paintCount == 1)
                            {
                                gdiplusPen = new Pen(renderSettings.FillColor, penWidth);
                                selectPen = new Pen(renderSettings.SelectFillColor, penWidth);
                                if (penWidth > 1)
                                {
                                    renderRailway = (renderSettings.LineType == LineType.Railway);
                                    if (renderRailway)
                                    {
                                        rwPen = new Pen(renderSettings.OutlineColor, 1);
                                    }
                                }
                            }                            
							gdiplusPen.EndCap = renderSettings.LineEndCap;//  System.Drawing.Drawing2D.LineCap.Round;
							gdiplusPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
							gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
							selectPen.EndCap = renderSettings.LineEndCap;// System.Drawing.Drawing2D.LineCap.Round;
							selectPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
							selectPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
							if (renderSettings.LineType == LineType.Solid)
							{
								gdiplusPen.DashStyle = renderSettings.LineDashStyle;
								selectPen.DashStyle = renderSettings.LineDashStyle;
							}

							byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                            if (fileMapped)
                            {
                                dataPtrZero = (byte*)mapView.ToPointer();
                                dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                            }
                            else
                            {
                                shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                            }

                            PointF pt = new PointF(0, 0);
                            RecordHeader[] pgRecs = this.RecordHeaders;

                            int index = 0;
                            byte[] data = SFRecordCol.SharedBuffer;
                            float minLabelLength = (float)(6 / scaleX);
                            fixed (byte* dataBufPtr = data)
                            {
                                if (!fileMapped) dataPtr = dataBufPtr;
                                while (index < pgRecs.Length)
                                {
                                    if (fileMapped)
                                    {
                                        dataPtr = dataPtrZero + pgRecs[index].Offset;
                                    }
                                    else
                                    {
                                        if (shapeFileStream.Position != pgRecs[index].Offset)
                                        {
                                            Console.Out.WriteLine("offset wrong");
                                            shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                                        }
                                        shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                                    }
                                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                                    int dataOffset = nextRec->PointDataOffset+8;
                                    bool renderShape = recordVisible[index];
                                    //overwrite if using CustomRenderSettings
                                    if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);
                                    RectangleD recordBounds = nextRec->bounds.ToRectangleD();
                                    if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(recordBounds) && renderShape)
                                    {
                                        //check if the simplifiedDataBuffer size needs to be increased
                                        if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                                        {
                                            simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                                        }
                                        if (simplificationDistance <= double.Epsilon)
                                        {
                                            simplificationDistance = CalculateSimplificationDistance(recordBounds, scaleX, coordinateTransformation);
                                        }


                                        fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                                        {

                                            int numParts = nextRec->NumParts;
                                            Point[] pts;

                                            
                                            if (useCustomRenderSettings)
                                            {
                                                Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                                if (customColor.ToArgb() != currentPenColor.ToArgb())
                                                {
                                                    gdiplusPen = new Pen(customColor, penWidth);
                                                    currentPenColor = customColor;
													gdiplusPen.EndCap = renderSettings.LineEndCap;// System.Drawing.Drawing2D.LineCap.Round;
													gdiplusPen.StartCap = renderSettings.LineStartCap;// System.Drawing.Drawing2D.LineCap.Round;
                                                    gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                                                    if (renderSettings.LineType == LineType.Solid)
                                                    {
                                                        gdiplusPen.DashStyle = renderSettings.LineDashStyle;
                                                        selectPen.DashStyle = renderSettings.LineDashStyle;
                                                    }
                                                }
                                            }

                                            for (int partNum = 0; partNum < numParts; partNum++)
                                            {
                                                int numPoints;
                                                if ((numParts - partNum) > 1)
                                                {
                                                    numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                                }
                                                else
                                                {
                                                    numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                                }
                                                if (numPoints <= 1)
                                                {
                                                    continue;
                                                }

                                                //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                                int usedPoints = ReducePoints((double*)(dataPtr + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                                if (coordinateTransformation != null)
                                                {
                                                    coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                                }

												Rectangle pixelBounds = new Rectangle();
												pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, ref pixelBounds, MercProj);

												//pts = GetPointsD((byte*)simplifiedDataPtr, 0, usedPoints, offX, offY, scaleX, scaleY, MercProj);
                                               // pts = GetPointsD(dataPtr, dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, offX, offY, scaleX, scaleY, MercProj);

                                                if (labelFields && paintCount == 0 )
                                                {
                                                    //check what the scaled bounds of this part are
                                                    //if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
                                                    if ((nextRec->bounds.Width > 6 * simplificationDistance) || (nextRec->bounds.Height > 6 * simplificationDistance))
                                                    {
                                                        int usedTempPoints = 0;
                                                        NativeMethods.SimplifyDouglasPeucker(pts, pts.Length, 2, tempPoints, ref usedTempPoints);
                                                        if (usedTempPoints > 0)
                                                        {
                                                            RenderPtObj.AddRenderPtObjects(tempPoints, usedTempPoints, renderPtObjList, index, 6);
                                                        }
                                                    }
                                                }

												List<Point[]> pointList = new List<Point[]>();
												if (pixelBounds.Left < -1000 || pixelBounds.Top < -1000 || pixelBounds.Right > clientArea.Width + 1000 || pixelBounds.Bottom > clientArea.Height + 1000)
												{
													//clip the polyline to avoid overflow
													GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
													{
														XMin = 0,
														YMin = 0,
														XMax = clientArea.Width,
														YMax = clientArea.Height
													};
													List<int> clippedPoints = new List<int>();
													List<int> clippedParts = new List<int>();
													GeometryAlgorithms.PolyLineClip(pts, pts.Length, clipBounds, clippedPoints, clippedParts);
													for (int clipPartIndex = 0; clipPartIndex < clippedParts.Count; ++clipPartIndex)
													{
														int startIndex = clippedParts[clipPartIndex];
														int endIndex = clipPartIndex < clippedParts.Count - 1 ? clippedParts[clipPartIndex + 1] : clippedPoints.Count;
														Point[] partPoints = new Point[(endIndex - startIndex) >> 1];
														for (int s = startIndex, p = 0; s < endIndex; s += 2, ++p)
														{
															partPoints[p].X = clippedPoints[s];
															partPoints[p].Y = clippedPoints[s + 1];
														}
														pointList.Add(partPoints);
													}
												}
												else
												{
													pointList.Add(pts);
												}

												foreach (var partPoints in pointList)
												{
													pts = partPoints;

													if (recordSelected[index] && selectPen != null)
													{
														g.DrawLines(selectPen, pts);
													}
													else
													{
														g.DrawLines(gdiplusPen, pts);
													}
													if (renderRailway)
													{
														System.Drawing.Drawing2D.Matrix savedTransform = g.Transform;
														try
														{
															int th = (int)Math.Ceiling((renderPenWidth + 2) / 2);
															int tw = (int)Math.Max(Math.Round((7f * th) / 7), 5);
															int pIndx = 0;
															int lx = 0;
															while (pIndx < pts.Length - 1)
															{
																//draw the next line segment

																int dy = pts[pIndx + 1].Y - pts[pIndx].Y;
																int dx = pts[pIndx + 1].X - pts[pIndx].X;
																float a = (float)(Math.Atan2(dy, dx) * 180f / Math.PI);
																int d = (int)Math.Round(Math.Sqrt(dy * dy + dx * dx));
																if (d > 0)
																{
																	g.Transform = savedTransform;
																	if (Math.Abs(a) > 90f && Math.Abs(a) <= 270f)
																	{
																		a -= 180f;
																		g.TranslateTransform(pts[pIndx + 1].X, pts[pIndx + 1].Y);
																		g.RotateTransform(a);
																		while (lx < d)
																		{
																			g.DrawLine(rwPen, lx, -th, lx, th);
																			lx += tw;
																		}
																		lx -= d;
																	}
																	else
																	{
																		g.TranslateTransform(pts[pIndx].X, pts[pIndx].Y);
																		g.RotateTransform(a);
																		while (lx < d)
																		{
																			g.DrawLine(rwPen, lx, -th, lx, th);
																			lx += tw;
																		}
																		lx -= d;
																	}
																}

																pIndx++;
															}
														}
														finally
														{
															g.Transform = savedTransform;
														}
													}
												}

												if (drawArrows && paintCount == (maxPaintCount - 1))
												{
													DrawDirectionArrows(pointList, arrowLength, arrowPen, g);
												}

											}
                                        }
                                    }                                    
                                    ++index;
                                }
                            }
                        }
                        finally
                        {
                            if (gdiplusPen != null) gdiplusPen.Dispose();
                            if (rwPen != null) rwPen.Dispose();
                            if (selectPen != null) selectPen.Dispose();
                        }
                    }
                }
                finally
                {
                    //if (gdiplusGraphics != IntPtr.Zero) NativeMethods.ReleaseGraphics(gdiplusGraphics);
                    //if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
                    // if(gdiplusPenPtr != IntPtr.Zero)NativeMethods.ReleaseGdiplusPen(gdiplusPenPtr);                                                
                }
                System.Drawing.Drawing2D.Matrix savedMatrix = g.Transform;                                    
                
                try
                {
                    if (labelFields)
                    {
                        bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                        Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                        Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                        pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                        int count = renderPtObjList.Count;
                        Color currentFontColor = renderSettings.FontColor;
                        bool useCustomFontColor = customRenderSettings != null;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                        float ws = 1.2f;
                        if (renderDuplicateFields) ws = 1f;
                        float ssm = shadowText ? 0.8f : 1f;
                        for (int n = 0; n < count; n++)
                        {
                            string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                            if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                            {
                                SizeF strSize = g.MeasureString(strLabel, renderSettings.Font);
                                strSize = new SizeF(strSize.Width * ssm, strSize.Height * ssm);
                                if (strSize.Width <= (renderPtObjList[n].Point0Dist) * ws)
                                {
                                    g.Transform = savedMatrix;
                                    g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                    g.RotateTransform(-renderPtObjList[n].Angle0);

                                    if (useCustomFontColor)
                                    {
                                        Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                        if (customColor.ToArgb() != currentFontColor.ToArgb())
                                        {
                                            fontBrush.Dispose();
                                            fontBrush = new SolidBrush(customColor);
                                            currentFontColor = customColor;
                                        }
                                    }

                                    if (shadowText)
                                    {
                                        System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                        gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF((renderPtObjList[n].Point0Dist - strSize.Width) / 2, -(strSize.Height) / 2), StringFormat.GenericDefault);//new StringFormat());
                                        g.DrawPath(pen, gp);
                                        g.FillPath(fontBrush, gp);
                                    }
                                    else
                                    {
                                        g.DrawString(strLabel, renderSettings.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);
                                    }
                                }
                            }
                        }
                        fontBrush.Dispose();
                        pen.Dispose();
                    }
                }
                finally
                {
                    g.Transform = savedMatrix;
                }

            }
            finally
            {
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
				if (arrowPen != null) arrowPen.Dispose();
            }
        }

        private unsafe void PaintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings, ProjectionType projectionType, ICoordinateTransformation coordinateTransformation, RectangleD targetExtent)
        {
            IntPtr dc = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiObj = IntPtr.Zero;

            IntPtr selectPen = IntPtr.Zero;

            IntPtr fileMappingPtr = IntPtr.Zero;
            if (MapFilesInMemory && (shapeFileStream is FileStream)) fileMappingPtr = NativeMethods.MapFile((FileStream)shapeFileStream);
            IntPtr mapView = IntPtr.Zero;
            byte* dataPtr = null;
            double[] simplifiedDataBuffer = new double[1024];
            double simplificationDistance = 1;

            if (fileMappingPtr != IntPtr.Zero)
            {
                mapView = NativeMethods.MapViewOfFile(fileMappingPtr, NativeMethods.FileMapAccess.FILE_MAP_READ, 0, 0, 0);
            }
            bool fileMapped = (mapView != IntPtr.Zero);

            //double scaleX = (double)(clientArea.Width / extent.Width);
            //double scaleY = -scaleX;

            //RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            //double offX = -projectedExtent.Left;
            //double offY = -projectedExtent.Bottom;
            //RectangleD actualExtent = projectedExtent;

            double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
            simplificationDistance = 1 / scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, extent.Width, extent.Width * (double)clientArea.Height / (double)clientArea.Width);
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (coordinateTransformation != null)
            {
                scaleX = (double)(clientArea.Width / targetExtent.Width);
                scaleY = -scaleX;

                projectedExtent = new RectangleD(targetExtent.Left, targetExtent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));

                offX = -projectedExtent.Left;
                offY = -projectedExtent.Bottom;
                //actualExtent = ShapeFile.ConvertExtent(projectedExtent, coordinateTransformation.TargetCRS, coordinateTransformation.SourceCRS);
                //when the coordinateTransformation has been supplied the extent will already contain
                //the actual intersecting extent in the shapefile CRS
                actualExtent = extent;
            }

            bool MercProj = projectionType == ProjectionType.Mercator;
            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }
            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);

            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderDuplicateFields = (labelFields && renderSettings.RenderDuplicateFields);
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

            float renderPenWidth = (float)(renderSettings.PenWidthScale * scaleX);

            if (renderSettings.MaxPixelPenWidth > 0 && renderPenWidth > renderSettings.MaxPixelPenWidth)
            {
                renderPenWidth = renderSettings.MaxPixelPenWidth;
            }
            if (renderSettings.MinPixelPenWidth > 0 && renderPenWidth < renderSettings.MinPixelPenWidth)
            {
                renderPenWidth = renderSettings.MinPixelPenWidth;
            }

            if (renderSettings.LineType == LineType.Railway)
            {
                renderPenWidth = Math.Min(renderPenWidth, 7f);
            }

            // obtain a handle to the Device Context so we can use GDI to perform the painting.            
            dc = g.GetHdc();

            try
            {
                Point[] tempPoints = new Point[SFRecordCol.SharedPointBuffer.Length];
            
                int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }

                for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                {

                    try
                    {
                        Color currentPenColor = renderSettings.OutlineColor;
                        int penWidth = (int)Math.Round(renderPenWidth);
                        int color = 0x1d1030;
                        color = renderSettings.OutlineColor.B & 0xff;
                        color = (color << 8) | (renderSettings.OutlineColor.G & 0xff);
                        color = (color << 8) | (renderSettings.OutlineColor.R & 0xff);

                        if (paintCount == 1)
                        {
                            currentPenColor = renderSettings.FillColor;
                            color = renderSettings.FillColor.B & 0xff;
                            color = (color << 8) | (renderSettings.FillColor.G & 0xff);
                            color = (color << 8) | (renderSettings.FillColor.R & 0xff);
                            penWidth -= 2;
                        }

                        gdiPen = NativeMethods.CreatePen(0, penWidth, color);
                        oldGdiObj = NativeMethods.SelectObject(dc, gdiPen);

                        if (paintCount == 0)
                        {
                            selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, penWidth + 4, ColorToGDIColor(renderSettings.SelectOutlineColor));
                        }
                        else
                        {
                            selectPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, penWidth, ColorToGDIColor(renderSettings.SelectFillColor));
                        }
                        byte* dataPtrZero = (byte*)IntPtr.Zero.ToPointer();
                        if (fileMapped)
                        {
                            dataPtrZero = (byte*)mapView.ToPointer();
                            dataPtr = (byte*)(((byte*)mapView.ToPointer()) + ShapeFileMainHeader.MAIN_HEADER_LENGTH);
                        }
                        else
                        {
                            shapeFileStream.Seek(ShapeFileMainHeader.MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                        }

                        RecordHeader[] pgRecs = this.RecordHeaders;

                        int index = 0;
                        byte[] data = SFRecordCol.SharedBuffer;
                        Point[] sharedPoints = SFRecordCol.SharedPointBuffer;
                        float minLabelLength = (float)(6 / scaleX);
                        fixed (byte* dataBufPtr = data)
                        {
                            if (!fileMapped) dataPtr = dataBufPtr;
                            while (index < pgRecs.Length)
                            {
                                if (fileMapped)
                                {
                                    dataPtr = dataPtrZero + pgRecs[index].Offset;
                                }
                                else
                                {
                                    if (shapeFileStream.Position != pgRecs[index].Offset)
                                    {
                                        Console.Out.WriteLine("offset wrong");
                                        shapeFileStream.Seek(pgRecs[index].Offset, SeekOrigin.Begin);
                                    }
                                    shapeFileStream.Read(data, 0, pgRecs[index].ContentLength + 8);
                                }
                                PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                                int dataOffset = nextRec->PointDataOffset + 8;
                                bool renderShape = recordVisible[index];
                                //overwrite if using CustomRenderSettings
                                if (useCustomRenderSettings) renderShape = customRenderSettings.RenderShape(index);

                                if (nextRec->ShapeType != ShapeType.NullShape && actualExtent.IntersectsWith(nextRec->bounds.ToRectangleD()) && renderShape)
                                {
                                    //check if the simplifiedDataBuffer size needs to be increased
                                    if ((nextRec->NumPoints << 1) > simplifiedDataBuffer.Length)
                                    {
                                        simplifiedDataBuffer = new double[nextRec->NumPoints << 1];
                                    }

                                    fixed (double* simplifiedDataPtr = simplifiedDataBuffer)
                                    {
                                        int numParts = nextRec->NumParts;
                                        Point[] pts;

                                        //if labelling fields then add a renderPtObj
                                        //if (labelFields && paintCount == 0)
                                        //{
                                        //    //check what the scaled bounds of this part are
                                        //    if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
                                        //    {
                                        //        if (renderDuplicateFields)
                                        //        {
                                        //            List<IndexAnglePair> iapList = GetPointsDAngle(dataPtr, dataOffset, nextRec->NumPoints, minLabelLength, MercProj);
                                        //            for (int li = iapList.Count - 1; li >= 0; li--)
                                        //            {
                                        //                pts = GetPointsD(dataPtr, dataOffset + (iapList[li].PointIndex << 4), 2, offX, offY, scaleX, -scaleX, MercProj);
                                        //                float d0 = (float)Math.Sqrt(((pts[1].X - pts[0].X) * (pts[1].X - pts[0].X)) + ((pts[1].Y - pts[0].Y) * (pts[1].Y - pts[0].Y)));
                                        //                if (Math.Abs(iapList[li].Angle) > 90f && Math.Abs(iapList[li].Angle) <= 270f)
                                        //                {
                                        //                    renderPtObjList.Add(new RenderPtObj(pts[1], d0, iapList[li].Angle - 180f, Point.Empty, 0, 0, index));
                                        //                }
                                        //                else
                                        //                {
                                        //                    renderPtObjList.Add(new RenderPtObj(pts[0], d0, iapList[li].Angle, Point.Empty, 0, 0, index));
                                        //                }
                                        //            }
                                        //        }
                                        //        else
                                        //        {
                                        //            int pointIndex = 0;
                                        //            float angle = GetPointsDAngle(dataPtr, dataOffset, nextRec->NumPoints, ref pointIndex, projectedExtent, MercProj);
                                        //            if (!angle.Equals(float.NaN))
                                        //            {
                                        //                pts = GetPointsD(dataPtr, dataOffset + (pointIndex << 4), 2, offX, offY, scaleX, -scaleX, MercProj);
                                        //                float d0 = (float)Math.Sqrt(((pts[1].X - pts[0].X) * (pts[1].X - pts[0].X)) + ((pts[1].Y - pts[0].Y) * (pts[1].Y - pts[0].Y)));
                                        //                if (d0 > 6)
                                        //                {
                                        //                    if (Math.Abs(angle) > 90f && Math.Abs(angle) <= 270f)
                                        //                    {
                                        //                        renderPtObjList.Add(new RenderPtObj(pts[1], d0, angle - 180f, Point.Empty, 0, 0, index));
                                        //                    }
                                        //                    else
                                        //                    {
                                        //                        renderPtObjList.Add(new RenderPtObj(pts[0], d0, angle, Point.Empty, 0, 0, index));
                                        //                    }
                                        //                }
                                        //            }
                                        //        }
                                        //    }
                                        //}

                                        if (useCustomRenderSettings)
                                        {
                                            Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                            if (customColor.ToArgb() != currentPenColor.ToArgb())
                                            {
                                                gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, penWidth, ColorToGDIColor(customColor));
                                                NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                                currentPenColor = customColor;
                                            }
                                        }

                                        for (int partNum = 0; partNum < numParts; ++partNum)
                                        {
                                            int numPoints;
                                            if ((numParts - partNum) > 1)
                                            {
                                                numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                                            }
                                            else
                                            {
                                                numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                                            }
                                            if (numPoints <= 1)
                                            {
                                                //Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                                continue;
                                            }

                                            //reduce the number of points before transforming. x10 performance increase at full zoom for some shapefiles 
                                            int usedPoints = ReducePoints((double*)(dataPtr + dataOffset + (nextRec->PartOffsets[partNum] << 4)), numPoints, simplifiedDataPtr, simplificationDistance);

                                            if (coordinateTransformation != null)
                                            {
                                                coordinateTransformation.Transform((double*)simplifiedDataPtr, usedPoints);
                                            }

                                            //GetPointsRemoveDuplicatesD(dataPtr, dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref offX, ref offY, ref scaleX, sharedPoints, ref usedPoints, MercProj);
                                            GetPointsRemoveDuplicatesD((byte*)simplifiedDataPtr, 0, usedPoints, ref offX, ref offY, ref scaleX, sharedPoints, ref usedPoints, MercProj);

                                            if (labelFields && paintCount == 0)
                                            {
                                                //check what the scaled bounds of this part are
                                                //if ((nextRec->bounds.Width * scaleX > 6) || (nextRec->bounds.Height * scaleX > 6))
                                                if ((nextRec->bounds.Width > 6 * simplificationDistance) || (nextRec->bounds.Height > 6 * simplificationDistance))
                                                {
                                                    int usedTempPoints = 0;
                                                    NativeMethods.SimplifyDouglasPeucker(sharedPoints, usedPoints, 2, tempPoints, ref usedTempPoints);
                                                    if (usedTempPoints > 0)
                                                    {
                                                        RenderPtObj.AddRenderPtObjects(tempPoints, usedTempPoints, renderPtObjList, index, 6);                                                       
                                                    }
                                                }
                                            }

                                            if (recordSelected[index])
                                            {
                                                IntPtr tempPen = IntPtr.Zero;
                                                try
                                                {
                                                    tempPen = NativeMethods.SelectObject(dc, selectPen);
                                                    NativeMethods.DrawPolyline(dc, sharedPoints, usedPoints);
                                                }
                                                finally
                                                {
                                                    if (tempPen != IntPtr.Zero) NativeMethods.SelectObject(dc, tempPen);
                                                }
                                            }
                                            else
                                            {
                                                NativeMethods.DrawPolyline(dc, sharedPoints, usedPoints);                                                
                                            }
                                        }
                                    }
                                }
                                
                                ++index;
                            }
                        }
                        //Console.Out.WriteLine("numPainted:" + numPainted);
                    }
                    finally
                    {
                        if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                        if (oldGdiObj != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiObj);
                        if (selectPen != IntPtr.Zero) NativeMethods.DeleteObject(selectPen);
                        selectPen = IntPtr.Zero;
                    }
                }
                tempPoints = null;
            }

            finally
            {
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
                if (mapView != IntPtr.Zero) NativeMethods.UnmapViewOfFile(mapView);
                if (fileMappingPtr != IntPtr.Zero) NativeMethods.CloseHandle(fileMappingPtr);
                
            }
            System.Drawing.Drawing2D.Matrix savedMatrix = g.Transform;                                                    
            try
            {
                if (labelFields)
                {
                    bool shadowText = (renderSettings != null && renderSettings.ShadowText);
                    Brush fontBrush = new SolidBrush(renderSettings.FontColor);
                    Pen pen = new Pen(Color.FromArgb(255, Color.White), 4f);
                    pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    int count = renderPtObjList.Count;
                    Color currentFontColor = renderSettings.FontColor;
                    bool useCustomFontColor = customRenderSettings != null;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    float ws = 1.2f;
                    if (renderDuplicateFields) ws = 1f;
                    float ssm = shadowText ? 0.8f : 1f;
                    for (int n = 0; n < count; n++)
                    {
                        string strLabel = renderSettings.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderSettings.FieldIndex).Trim();
                        if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                        {
                            SizeF strSize = g.MeasureString(strLabel, renderSettings.Font);
                            strSize = new SizeF(strSize.Width * ssm, strSize.Height * ssm);
                            if (strSize.Width <= (renderPtObjList[n].Point0Dist) * ws)
                            {
                                g.Transform = savedMatrix;
                                g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                g.RotateTransform(-renderPtObjList[n].Angle0);

                                if (useCustomFontColor)
                                {
                                    Color customColor = customRenderSettings.GetRecordFontColor(renderPtObjList[n].RecordIndex);
                                    if (customColor.ToArgb() != currentFontColor.ToArgb())
                                    {
                                        fontBrush.Dispose();
                                        fontBrush = new SolidBrush(customColor);
                                        currentFontColor = customColor;
                                    }
                                }

                                if (shadowText)
                                {
                                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding);
                                    gp.AddString(strLabel, renderSettings.Font.FontFamily, (int)renderSettings.Font.Style, renderSettings.Font.Size, new PointF((renderPtObjList[n].Point0Dist - strSize.Width) / 2, -(strSize.Height) / 2), StringFormat.GenericDefault);//new StringFormat());
                                    g.DrawPath(pen, gp);
                                    g.FillPath(fontBrush, gp);
                                }
                                else
                                {
                                    g.DrawString(strLabel, renderSettings.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2, -strSize.Height / 2);
                                }
                            }
                        }
                    }
                    fontBrush.Dispose();
                    pen.Dispose();
                }
            }
            finally
            {
                g.Transform = savedMatrix;                
            }

        }

        
        #endregion


        public bool ContainsPoint(int shapeIndex, PointD pt, System.IO.Stream shapeFileStream, byte[] dataBuf, double minDist)
        {
            unsafe
            {
                byte[] data = SFRecordCol.SharedBuffer;
                shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
                shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
                fixed (byte* dataPtr = data)
                {
                    PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                    int dataOffset = nextRec->PointDataOffset;
                    int numParts = nextRec->NumParts;
                    for (int partNum = 0; partNum < numParts; partNum++)
                    {
                        int numPoints;
                        if ((numParts - partNum) > 1)
                        {
                            numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                        }
                        else
                        {
                            numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                        }
                        if (GeometryAlgorithms.PointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, pt, minDist))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override unsafe bool IntersectRect(int shapeIndex, ref RectangleD rect, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with rect
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!recBounds.IntersectsWith(rect)) return false;

            //next check if rect entirely contains the polyline
            if (rect.Contains(recBounds)) return true;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    if (IntersectsRect(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, ref rect))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static unsafe bool IntersectsRect(byte[] data, int offset, int numPoints, ref RectangleD rect)
        {
            fixed (byte* ptr = data)
            {
                return NativeMethods.PolyLineRectIntersect(ptr + offset, numPoints, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }

        public override unsafe bool IntersectCircle(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;

            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    if (GeometryAlgorithms.PolylineCircleIntersects(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, radius))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override unsafe double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream)
        {
            //first check if the record's bounds intersects with the circle
            // RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return false;
            double foundDistance = double.PositiveInfinity;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }

                    double polylineDistance = GeometryAlgorithms.ClosestPointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre);
                    if (polylineDistance < foundDistance) foundDistance = polylineDistance;
                }
            }
            return foundDistance;
        }

        public override unsafe double GetDistanceToShape(int shapeIndex, PointD centre, double radius, System.IO.Stream shapeFileStream, out PolylineDistanceInfo polylineDistanceInfo)
        {
            polylineDistanceInfo = PolylineDistanceInfo.Empty;
            //first check if the record's bounds intersects with the circle
            //RectangleD recBounds = GetRecordBoundsD(shapeIndex, shapeFileStream);
            //if (!GeometryAlgorithms.RectangleCircleIntersects(ref recBounds, ref centre, radius)) return double.PositiveInfinity;
            //now do an accurate intersection check
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Seek(this.RecordHeaders[shapeIndex].Offset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, this.RecordHeaders[shapeIndex].ContentLength + 8);
            fixed (byte* dataPtr = data)
            {
                PolyLineZRecordP* nextRec = (PolyLineZRecordP*)(dataPtr + 8);
                int dataOffset = nextRec->PointDataOffset;
                int numParts = nextRec->NumParts;
                for (int partNum = 0; partNum < numParts; partNum++)
                {
                    int numPoints;
                    if ((numParts - partNum) > 1)
                    {
                        numPoints = nextRec->PartOffsets[partNum + 1] - nextRec->PartOffsets[partNum];
                    }
                    else
                    {
                        numPoints = nextRec->NumPoints - nextRec->PartOffsets[partNum];
                    }
                    PolylineDistanceInfo pdi;
                    double polylineDistance = GeometryAlgorithms.ClosestPointOnPolyline(data, 8 + dataOffset + (nextRec->PartOffsets[partNum] << 4), numPoints, centre, out pdi);
                    if (polylineDistance < polylineDistanceInfo.Distance)
                    {
                        pdi.PointIndex += nextRec->PartOffsets[partNum];
                        polylineDistanceInfo = pdi;
                    }
                }
            }
            return polylineDistanceInfo.Distance;
        }
        

        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointD GetRecordPoint(int recordIndex, Stream shapeFileStream)
        {
            //throw new Exception("The method or operation is not implemented.");
            return PointD.Empty;
        }

        public override RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);

            return bounds.ToRectangleF();
        }

        public override RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream)
        {
            if (recordIndex < 0 || recordIndex >= this.RecordHeaders.Length) return RectangleF.Empty;
            shapeFileStream.Seek(RecordHeaders[recordIndex].Offset, SeekOrigin.Begin);
            byte[] data = SFRecordCol.SharedBuffer;
            shapeFileStream.Read(data, 0, 32 + 12);
            Box bounds = new Box(data, 12);
            return bounds.ToRectangleD();
        }



        #endregion

    }


    internal class LabelPlacementMap
    {
        private LabelPlacementStrip[] strips;
        private int lineHeight;
        private int colWidth;
        private int numRows;

        internal LabelPlacementMap(int pixWidth, int pixHeight)
        {
            this.lineHeight = 1;
            colWidth = (int)Math.Ceiling(((float)pixWidth) / LabelPlacementStrip.NumCols);
            numRows = pixHeight;
            strips = new LabelPlacementStrip[numRows];
        }

        internal bool addLabelToMap(Point pos, int dx, int dy, int labelWidth, int labelHeight)
        {
            int sStart = (int)Math.Round((pos.Y - dy) / (float)lineHeight);
            if (sStart < 0) sStart = 0;
            int sEnd = (int)Math.Round((pos.Y - dy + labelHeight) / (float)lineHeight);           

            int xStart = (int)Math.Round((pos.X + dx) / (float)colWidth);
            if (xStart < 0) xStart = 0;
            int xEnd = (int)Math.Round((pos.X + dx + labelWidth) / (float)colWidth);

            if (sStart >= numRows || sEnd < 0) return false;
            if (xStart >= LabelPlacementStrip.NumCols || xEnd < 0) return false;
            if (sEnd >= numRows) sEnd = numRows - 1;
            if (xEnd >= LabelPlacementStrip.NumCols) xEnd = LabelPlacementStrip.NumCols-1;
            
            long labelMask1=0, labelMask2=0;
            LabelPlacementStrip.CreateMasks(out labelMask1, out labelMask2, xStart, xEnd);
            bool ok = true;
            for (int s = sStart;ok&&(s <= sEnd);s++ )
            {                
                ok = !strips[s].overlaps(labelMask1, labelMask2);
            }
            if (ok)
            {
                for (int s = sStart;(s <= sEnd); s++)
                {
                     strips[s].addLabel(labelMask1, labelMask2);
                }
            }
            return ok;
        }
    }

    internal struct LabelPlacementStrip
    {
        internal const int NumCols = 128;
        private long stripMask1;
        private long stripMask2;

        internal static long[] PosMaskLU;

        static LabelPlacementStrip()
        {
            
            PosMaskLU = new long[64];
            long bit = 1;
            for (int n = 0; n < 64; n++)
            {
                PosMaskLU[n] = bit;
                //Console.Out.WriteLine("" + n + " - " + bit);
                bit <<= 1;
                bit |= 1;                
            }                       
        }

        internal static void CreateMasks(out long mask1, out long mask2, int colStart, int colEnd)
        {
            mask1 = 0;
            mask2 = 0;
            if (colStart < 64)
            {
                if (colEnd < 64)
                {
                    mask1 = LabelPlacementStrip.PosMaskLU[(colEnd - colStart)] << colStart;
                }
                else
                {
                    mask1 = LabelPlacementStrip.PosMaskLU[(63 - colStart)] << colStart;
                    mask2 = LabelPlacementStrip.PosMaskLU[colEnd-64];
                }
            }
            else
            {
                mask2 = LabelPlacementStrip.PosMaskLU[(colEnd - colStart)] << (colStart-64);
            }
        }

        internal bool overlaps(long labelMask1, long labelMask2)
        {
            bool overlap =  ((labelMask1 & stripMask1) != 0) || ((labelMask2 & stripMask2) != 0);
            return overlap;
        }

        internal void addLabel(long labelMask1, long labelMask2)
        {
            stripMask1 |= labelMask1;
            stripMask2 |= labelMask2;
        }
    }

    #endregion


    #region "DBF types"

    
    
    /// <summary>
    /// A DBF Reader class, providing direct access to record data within a DBF file, with low memory useage.    
    /// </summary>
    public sealed class DbfReader : IDisposable
	{
		private DbfFileHeader dBFRecordHeader;
		private Stream dbfFileStream;
        private const int FileBufSize = 1024*8;//dont set buffer size too high as performance will suffer;

        private static System.Text.Encoding DefaultEncoding = System.Text.Encoding.UTF8;
        private System.Text.Encoding stringEncoding = DefaultEncoding;//System.Text.Encoding.UTF8;//System.Text.Encoding.Default;

        /// <summary>
        /// DbfReader constructor
        /// </summary>
        /// <param name="filePath"> The path to the DBF file. If the file extension contained in fileName is not included then ".dbf" will be appended to the file path
        /// before it is opened</param>
		public DbfReader(string filePath)
		{
            //Console.Out.WriteLine("string encoding: " + stringEncoding);
            //Console.Out.WriteLine(string.Format("string encoding: {0}[{1}]", stringEncoding.EncodingName, stringEncoding.CodePage));
            

			try
			{
                if (filePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = System.IO.Path.ChangeExtension(filePath, "dbf");
                }
                else if (!filePath.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".dbf";
                }
                dbfFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileBufSize);				
				dBFRecordHeader = new DbfFileHeader();
				dBFRecordHeader.Read(dbfFileStream);
                try
                {
                    int codePage = LD_CP_LU[dBFRecordHeader.LDID].codepage;
                    if (codePage > 0)
                    {
                        System.Text.Encoding enc = System.Text.Encoding.GetEncoding(codePage);
                        if (enc != null) this.StringEncoding = enc;
                    }
                }
                catch
                {
                    //Console.Out.WriteLine("Error reading Language Driver Id - using default");
                    this.StringEncoding = DefaultEncoding;
                }
			}			
			catch
			{
				if(dbfFileStream != null)
				{
					dbfFileStream.Close();
					dbfFileStream = null;
				}
				throw;
			}
		}

        /// <summary>
        /// Constructs a DbfReader from a Stream
        /// </summary>
        /// <param name="inputStream">input stream that supports seeking</param>
        public DbfReader(System.IO.Stream inputStream)
        {          
            try
            {
                dbfFileStream = inputStream;
				dbfFileStream.Seek(0, SeekOrigin.Begin);
                dBFRecordHeader = new DbfFileHeader();
                dBFRecordHeader.Read(dbfFileStream);
                try
                {
                    int codePage = LD_CP_LU[dBFRecordHeader.LDID].codepage;
                    if (codePage > 0)
                    {
                        System.Text.Encoding enc = System.Text.Encoding.GetEncoding(codePage);
                        if (enc != null) this.StringEncoding = enc;
                    }
                }
                catch
                {
                    this.StringEncoding = DefaultEncoding;
                }
            }
            catch
            {
                if (dbfFileStream != null)
                {
                    dbfFileStream.Close();
                    dbfFileStream = null;
                }
                throw;
            }
        }


        /// <summary>
        /// Returns the DbfFileHeader representing the contents of the DBF file's main header
        /// </summary>
		public DbfFileHeader DbfRecordHeader
		{
			get
			{
				return this.dBFRecordHeader;
			}
		}
		
		/// <summary>
		/// returns a string representing the field[fieldIndex] of the Record[recordNumber] in the dbf file
		/// Throws ArgumentException if recordNumber ">=" NumberOfRecords or negative, fieldIndex >= FieldCount or negative, 
		/// </summary>
		/// <param name="recordNumber">Zero based record number in the dbf file</param>
		/// <param name="fieldIndex">Zero based field index in a record</param>
		/// <returns>the string contents of the required field. Note that the returned string is not trimmed so that if a field length is longer than the stored string
        /// then the string is padded with spaces. I.E if a field of length 10 is storing "abc" then the returned string will be padded with 7 space characters</returns>
		public unsafe string GetField(int recordNumber, int fieldIndex)
		{
            if (recordNumber >= this.dBFRecordHeader.RecordCount || recordNumber < 0)
            {
                throw new ArgumentException("recNum must be <= DBFRecordHeader.NumRecords and >=0");
            }

            if (fieldIndex >= this.dBFRecordHeader.FieldCount || fieldIndex < 0)
            {
                throw new ArgumentException("fieldIndex must be <= DBFRecordHeader.NumFields and >=0");
            }
			
			long FieldOffset = this.dBFRecordHeader.HeaderLength + this.dBFRecordHeader.RecordLength*(long)recordNumber + this.dBFRecordHeader.GetFieldDescriptions()[fieldIndex].RecordOffset;
			//string strField;
			dbfFileStream.Seek(FieldOffset,SeekOrigin.Begin);
			byte[] data = new byte[this.dBFRecordHeader.GetFieldDescriptions()[fieldIndex].FieldLength];
			dbfFileStream.Read(data,0,data.Length);
            //foreach (System.Text.EncodingInfo info in System.Text.Encoding.GetEncodings())
            //{
            //    Console.Out.WriteLine(info.DisplayName + "["+info.CodePage+"]");

            //}
            //fixed (byte* bPtr = data)
            //{
            //    strField = new String((sbyte*)(bPtr));
                
            //}
           
            ////return strField;
            ////int codePage = 126;// 28596;
            //return System.Text.Encoding.UTF8.GetString(data);
            return this.stringEncoding.GetString(data);
		}

        /// <summary>
        /// Get/Sets the Encoding used to read string fields
        /// </summary>
        public System.Text.Encoding StringEncoding
        {
            get
            {
                return this.stringEncoding;
            }
            set
            {
                this.stringEncoding = value;
            }
        }


		/// <summary>
		/// Gets an array of strings representing the fields of Record[recordNumber] in the DBF file
		/// Throws ArgumentException if recordNumber ">=" NumberOfRecords or negative
		/// </summary>
		/// <param name="recordNumber">Zero based record number in the dbfFile</param>
		/// <returns></returns>
		public unsafe string[] GetFields(int recordNumber)
		{
			if (recordNumber >= this.dBFRecordHeader.RecordCount || recordNumber < 0)
			{
				throw new ArgumentException("recordNumber must be <= DBFRecordHeader.NumRecords and >=0");
			}
			
			long RecordOffset = this.dBFRecordHeader.HeaderLength + this.dBFRecordHeader.RecordLength* (long)recordNumber;
			
			int numFields = this.dBFRecordHeader.FieldCount;
			string[] strFields = new string[numFields];
            //if (RecordOffset > dbfFileStream.Length)
            //{
            //    Console.Out.WriteLine("record Number:" + recordNumber);
            //}
            dbfFileStream.Seek(RecordOffset,SeekOrigin.Begin);
			byte[] data = new byte[this.dBFRecordHeader.RecordLength];
            
			dbfFileStream.Read(data,0,data.Length);
            //fixed(byte* bPtr = data)
            //{				
            //    sbyte* sbPtr = (sbyte*)bPtr;
            //    for(int n=0;n<numFields;n++)
            //    {
            //        strFields[n] = new String(sbPtr,dBFRecordHeader.GetFieldDescriptions()[n].RecordOffset,dBFRecordHeader.GetFieldDescriptions()[n].FieldLength);
            //    }
            //}
            for(int n=0;n<numFields;n++)
			{
                strFields[n] = this.stringEncoding.GetString(data, dBFRecordHeader.GetFieldDescriptions()[n].RecordOffset, dBFRecordHeader.GetFieldDescriptions()[n].FieldLength);
			}            
			return strFields;
		}

        /// <summary>
        /// Gets an array of strings representing all of the record contents for a specified field.
        /// Each string in the returned array is trimmed to remove trailing spaces
        /// </summary>
        /// <param name="fieldIndex">Zero based index of the field in the DBF file</param>
        /// <returns></returns>
        public string[] GetRecords(int fieldIndex)
        {
            string[] records = new string[this.dBFRecordHeader.RecordCount];
            for (int n = 0; n < records.Length; n++)
            {
                records[n] = this.GetField(n, fieldIndex).Trim(new char[] { ' ', (char)0 });
                
            }
            return records;
        }

        /// <summary>
        /// Gets an array of strings representing the distinct record contents for a specified field.
        /// Each string in the returned array is trimmed to remove trailing spaces
        /// </summary>
        /// <param name="fieldIndex">Zero based index of the field in the DBF file</param>
        /// <returns></returns>
        public string[] GetDistinctRecords(int fieldIndex)
        {
            return this.GetDistinctRecordsImpl(fieldIndex);
        }

        private unsafe string[] GetDistinctRecordsImpl(int fieldIndex)
        {
            int numRecs = this.dBFRecordHeader.RecordCount;
            List<string> records = new List<string>();
            Dictionary<string, int> d = new Dictionary<string, int>();

            long FieldOffset = this.dBFRecordHeader.HeaderLength + this.dBFRecordHeader.GetFieldDescriptions()[fieldIndex].RecordOffset;
            
            int fieldLength = this.dBFRecordHeader.GetFieldDescriptions()[fieldIndex].FieldLength;
            dbfFileStream.Seek(FieldOffset, SeekOrigin.Begin);
            byte[] data = new byte[this.dBFRecordHeader.RecordLength];
                        
            for (int n = 0; n < numRecs; n++)
            {
                string s;
                dbfFileStream.Read(data, 0, data.Length);
                //fixed (byte* bPtr = data)
                //{
                //    s = new String((sbyte*)(bPtr),0,fieldLength);
                //}
                s = this.stringEncoding.GetString(data, 0, fieldLength);
                s = s.Trim();
                if (!d.ContainsKey(s))
                {
                    d.Add(s, 0);
                    records.Add(s);
                }
            }
            return records.ToArray();
        }
        

        /// <summary>
        /// returns the field index of fieldName.
        /// If a field with name fieldName does not exist the method will return -1
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public int IndexOfFieldName(string fieldName)
        {
            return Array.IndexOf(GetFieldNames(), fieldName);
        }

        [Obsolete("this method may be removed in future versions")]
        public int IndexOf(string value, int fieldIndex, bool ignoreCase)
        {
            if (fieldIndex != indexedFieldIndex)
            {
                CreateFieldIndex(fieldIndex);
            }
            int listIndex = indexFieldList.BinarySearch(new IndexKey(value, -1));
            if (listIndex < 0) return -1;
            return indexFieldList[listIndex].Index;

            //if(string.IsNullOrEmpty(value)) return -1;
            //string trimmedValue= value.Trim();
            //for (int n = 0; n < this.DBFRecordHeader.RecordCount; n++)
            //{
            //    if (string.Compare(getField(n, fieldIndex).Trim(), trimmedValue, ignoreCase) == 0) return n;
            //}
            //return -1;
        }

        /// <summary>
        /// Closes the DbfReader. This method will close internal resources and should be called when you have finshed using the DbfReader.
        /// After this method has been called any attempt to read a field or record data from the DbfReader will throw an Exception
        /// </summary>
        public void Close()
		{
			if (dbfFileStream != null)
			{
				dbfFileStream.Close();
			}
		}

        /// <summary>
        /// Utility method to return the names of the fields in the DBF file
        /// Internally extracts the field names from the DBFRecordHeader
        /// </summary>
        /// <returns></returns>
        public string[] GetFieldNames()
        {
            string[] names = new string[DbfRecordHeader.FieldCount];
            for(int n=names.Length-1; n>=0;n--)
            {
                names[n] = DbfRecordHeader.GetFieldDescriptions()[n].FieldName;
            }
            return names;
        }

        private void CreateFieldIndex(int fieldIndex)
        {
            indexedFieldIndex = fieldIndex;
            int numRecords = this.DbfRecordHeader.RecordCount;
            indexFieldList = new List<IndexKey>(numRecords);
            for (int n = 0; n < numRecords; n++)
            {
                indexFieldList.Add(new IndexKey(GetField(n, fieldIndex).Trim(), n));
            }
            indexFieldList.Sort();
            //indexFieldList.BinarySearch(

        }

        private int indexedFieldIndex = -1;

        private List<IndexKey> indexFieldList;

        private class IndexKey : IComparable
        {
            public string Key;
            public int Index;

            public IndexKey(string key, int index)
            {
                if (key == null) throw new ArgumentException("key can not be null");
                this.Key = key;
                this.Index = index;
            }

            #region IComparable Members

            public int CompareTo(object obj)
            {
                //throw new Exception("The method or operation is not implemented.");
                IndexKey other = obj as IndexKey;
                //return this.Key.CompareTo(other.Key);
                return string.Compare(Key, other.Key, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return (this.CompareTo(obj) == 0);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }


            #endregion
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing) //dispose managed resources
            {
                if (this.dbfFileStream != null) dbfFileStream.Close();
				dbfFileStream = null;
            }
        }

        #endregion

        #region Language Driver To Code Page Utils

        private struct LD_CP
        {
            public int ldid;
            public int codepage;
            public LD_CP(int ld,int cp){ldid=ld;codepage=cp;}
        }
        private static LD_CP[] LD_CP_LU = new LD_CP[]{
            new LD_CP(0x00,-1),     new LD_CP(0x01,437),
            new LD_CP(0x02,850),    new LD_CP(0x03,1252),
            new LD_CP(0x04,-1),     new LD_CP(0x05,-1),
            new LD_CP(0x06,-1),     new LD_CP(0x07,-1),
            new LD_CP(0x08,865),    new LD_CP(0x09,437),
            new LD_CP(0x0A,850),    new LD_CP(0x0B,437),
            new LD_CP(0x0C,-1),     new LD_CP(0x0D,437),
            new LD_CP(0x0E,850),    new LD_CP(0x0F,437),
            
            new LD_CP(0x10,850),    new LD_CP(0x11,437),
            new LD_CP(0x12,850),    new LD_CP(0x13,932),
            new LD_CP(0x14,850),    new LD_CP(0x15,437),
            new LD_CP(0x16,850),    new LD_CP(0x17,865),
            new LD_CP(0x18,437),    new LD_CP(0x19,437),
            new LD_CP(0x1A,850),    new LD_CP(0x1B,437),
            new LD_CP(0x1C,863),    new LD_CP(0x1D,850),
            new LD_CP(0x1E,-1),     new LD_CP(0x1F,852),
            
            new LD_CP(0x20,-1),     new LD_CP(0x21,-1),
            new LD_CP(0x22,852),    new LD_CP(0x23,852),
            new LD_CP(0x24,860),    new LD_CP(0x25,850),
            new LD_CP(0x26,866),    new LD_CP(0x27,-1),
            new LD_CP(0x28,-1),     new LD_CP(0x29,-1),
            new LD_CP(0x2A,-1),     new LD_CP(0x2B,-1),
            new LD_CP(0x2C,-1),     new LD_CP(0x2D,-1),
            new LD_CP(0x2E,-1),     new LD_CP(0x2F,-1),

            new LD_CP(0x30,-1),     new LD_CP(0x31,-1),
            new LD_CP(0x32,-1),     new LD_CP(0x33,-1),
            new LD_CP(0x34,-1),     new LD_CP(0x35,-1),
            new LD_CP(0x36,-1),     new LD_CP(0x37,850),
            new LD_CP(0x38,-1),     new LD_CP(0x39,-1),
            new LD_CP(0x3A,-1),     new LD_CP(0x3B,-1),
            new LD_CP(0x3C,-1),     new LD_CP(0x3D,-1),
            new LD_CP(0x3E,-1),     new LD_CP(0x3F,-1),

            new LD_CP(0x40,852),    new LD_CP(0x41,-1),
            new LD_CP(0x42,-1),     new LD_CP(0x43,-1),
            new LD_CP(0x44,-1),     new LD_CP(0x45,-1),
            new LD_CP(0x46,-1),     new LD_CP(0x47,-1),
            new LD_CP(0x48,-1),     new LD_CP(0x49,-1),
            new LD_CP(0x4A,-1),     new LD_CP(0x4B,-1),
            new LD_CP(0x4C,-1),     new LD_CP(0x4D,936),
            new LD_CP(0x4E,949),    new LD_CP(0x4F,950),

            new LD_CP(0x50,874),    new LD_CP(0x51,-1),
            new LD_CP(0x52,-1),     new LD_CP(0x53,-1),
            new LD_CP(0x54,-1),     new LD_CP(0x55,-1),
            new LD_CP(0x56,-1),     new LD_CP(0x57,1252),
            new LD_CP(0x58,1252),   new LD_CP(0x59,1252),
            new LD_CP(0x5A,-1),     new LD_CP(0x5B,-1),
            new LD_CP(0x5C,-1),     new LD_CP(0x5D,936),
            new LD_CP(0x5E,949),    new LD_CP(0x5F,950),

            new LD_CP(0x60,-1),     new LD_CP(0x61,-1),
            new LD_CP(0x62,-1),     new LD_CP(0x63,-1),
            new LD_CP(0x64,852),    new LD_CP(0x65,866),
            new LD_CP(0x66,865),    new LD_CP(0x67,861),
            //new LD_CP(0x68,895),    new LD_CP(0x69,620),    /*??*/
            new LD_CP(0x68,-1),    new LD_CP(0x69,-1),
            new LD_CP(0x6A,737),    new LD_CP(0x6B,857),
            new LD_CP(0x6C,863),    new LD_CP(0x6D,-1),
            new LD_CP(0x6E,-1),     new LD_CP(0x6F,-1),

            new LD_CP(0x70,-1),     new LD_CP(0x71,-1),
            new LD_CP(0x72,-1),     new LD_CP(0x73,-1),
            new LD_CP(0x74,-1),     new LD_CP(0x75,-1),
            new LD_CP(0x76,-1),     new LD_CP(0x77,-1),
            new LD_CP(0x78,950),    new LD_CP(0x79,949),
            new LD_CP(0x7A,936),    new LD_CP(0x7B,932),
            new LD_CP(0x7C,874),    new LD_CP(0x7D,1255),   /*??*/
            new LD_CP(0x7E,1256),   new LD_CP(0x7F,-1),     /*??*/

            new LD_CP(0x80,-1),     new LD_CP(0x81,-1),
            new LD_CP(0x82,-1),     new LD_CP(0x83,-1),
            new LD_CP(0x84,-1),     new LD_CP(0x85,-1),
            new LD_CP(0x86,737),    new LD_CP(0x87,852),
            new LD_CP(0x88,857),    new LD_CP(0x89,-1),
            new LD_CP(0x8A,-1),     new LD_CP(0x8B,-1),
            new LD_CP(0x8C,-1),     new LD_CP(0x8D,-1),
            new LD_CP(0x8E,-1),     new LD_CP(0x8F,-1),

            new LD_CP(0x90,-1),     new LD_CP(0x91,-1),
            new LD_CP(0x92,-1),     new LD_CP(0x93,-1),
            new LD_CP(0x94,-1),     new LD_CP(0x95,-1),
            new LD_CP(0x96,-1),     new LD_CP(0x97,10029),  /*??*/
            new LD_CP(0x98,-1),     new LD_CP(0x99,-1),
            new LD_CP(0x9A,-1),     new LD_CP(0x9B,-1),
            new LD_CP(0x9C,-1),     new LD_CP(0x9D,-1),
            new LD_CP(0x9E,-1),     new LD_CP(0x9F,-1),

            new LD_CP(0xA0,-1),     new LD_CP(0xA1,-1),
            new LD_CP(0xA2,-1),     new LD_CP(0xA3,-1),
            new LD_CP(0xA4,-1),     new LD_CP(0xA5,-1),
            new LD_CP(0xA6,-1),     new LD_CP(0xA7,-1),
            new LD_CP(0xA8,-1),     new LD_CP(0xA9,-1),
            new LD_CP(0xAA,-1),     new LD_CP(0xAB,-1),
            new LD_CP(0xAC,-1),     new LD_CP(0xAD,-1),
            new LD_CP(0xAE,-1),     new LD_CP(0xAF,-1),

            new LD_CP(0xB0,-1),     new LD_CP(0xB1,-1),
            new LD_CP(0xB2,-1),     new LD_CP(0xB3,-1),
            new LD_CP(0xB4,-1),     new LD_CP(0xB5,-1),
            new LD_CP(0xB6,-1),     new LD_CP(0xB7,-1),
            new LD_CP(0xB8,-1),     new LD_CP(0xB9,-1),
            new LD_CP(0xBA,-1),     new LD_CP(0xBB,-1),
            new LD_CP(0xBC,-1),     new LD_CP(0xBD,-1),
            new LD_CP(0xBE,-1),     new LD_CP(0xBF,-1),

            new LD_CP(0xC0,-1),     new LD_CP(0xC1,-1),
            new LD_CP(0xC2,-1),     new LD_CP(0xC3,-1),
            new LD_CP(0xC4,-1),     new LD_CP(0xC5,-1),
            new LD_CP(0xC6,-1),     new LD_CP(0xC7,-1),
            new LD_CP(0xC8,1250),   new LD_CP(0xC9,1251),
            new LD_CP(0xCA,1254),   new LD_CP(0xCB,1253),
            new LD_CP(0xCC,1257),   new LD_CP(0xCD,-1),
            new LD_CP(0xCE,-1),     new LD_CP(0xCF,-1),
            
            new LD_CP(0xD0,-1),     new LD_CP(0xD1,-1),
            new LD_CP(0xD2,-1),     new LD_CP(0xD3,-1),
            new LD_CP(0xD4,-1),     new LD_CP(0xD5,-1),
            new LD_CP(0xD6,-1),     new LD_CP(0xD7,-1),
            new LD_CP(0xD8,-1),     new LD_CP(0xD9,-1),
            new LD_CP(0xDA,-1),     new LD_CP(0xDB,-1),
            new LD_CP(0xDC,-1),     new LD_CP(0xDD,-1),
            new LD_CP(0xDE,-1),     new LD_CP(0xDF,-1),

            new LD_CP(0xE0,-1),     new LD_CP(0xE1,-1),
            new LD_CP(0xE2,-1),     new LD_CP(0xE3,-1),
            new LD_CP(0xE4,-1),     new LD_CP(0xE5,-1),
            new LD_CP(0xE6,-1),     new LD_CP(0xE7,-1),
            new LD_CP(0xE8,-1),     new LD_CP(0xE9,-1),
            new LD_CP(0xEA,-1),     new LD_CP(0xEB,-1),
            new LD_CP(0xEC,-1),     new LD_CP(0xED,-1),
            new LD_CP(0xEE,-1),     new LD_CP(0xEF,-1),

            new LD_CP(0xF0,-1),     new LD_CP(0xF1,-1),
            new LD_CP(0xF2,-1),     new LD_CP(0xF3,-1),
            new LD_CP(0xF4,-1),     new LD_CP(0xF5,-1),
            new LD_CP(0xF6,-1),     new LD_CP(0xF7,-1),
            new LD_CP(0xF8,-1),     new LD_CP(0xF9,-1),
            new LD_CP(0xFA,-1),     new LD_CP(0xFB,-1),
            new LD_CP(0xFC,-1),     new LD_CP(0xFD,-1),
            new LD_CP(0xFE,-1),     new LD_CP(0xFF,-1)
        };

        #endregion
    }

    /// <summary>
    /// Struct representing the contents of the main header of a DBF file.
    /// The main header stores version number, modification date, the number of fields in each record
    /// and a description of each field in a record.
    /// For a more detailed description of the main header of a DBF file refer to the DBF File Format techincal description
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]    
    public unsafe struct DbfFileHeader
	{
		private byte _versionNumber;
        private byte _year;
        private byte _month;
        private byte _day;
        private int _numRecords;
        private short _headerLength;
        private short _recordLength;
        private int _numFields;
        private int _ldId;
		private DbfFieldDesc[] _fieldDescs;
		
        /// <summary>
        /// Reads the header contents from a stream
        /// </summary>
        /// <param name="stream"></param>
		public void Read(Stream stream)
		{
			//read first 12 bytes to determine version, etc. and header length
			byte[] data = new byte[12];
			stream.Read(data,0,12);
			_versionNumber = data[0];
			_year = data[1];
			_month = data[2];
			_day = data[3];
			_numRecords = EndianUtils.ReadIntLE(data,4);
			_headerLength = BitConverter.ToInt16(data,8);
			_recordLength = BitConverter.ToInt16(data, 10);
			
			_numFields = (HeaderLength-33)/32;
			_fieldDescs = new DbfFieldDesc[_numFields];
			
			//now read the field descriptors
			data = new byte[HeaderLength-12];
			stream.Read(data,0,data.Length);

            _ldId = data[29 - 12];

			int fieldOffset = 20;
			int recOffset = 1; //set to 1 as first byte in a record indicates whether valid/deleted
			
			for(int n=0;n<FieldCount;n++)
			{
                _fieldDescs[n] = new DbfFieldDesc(data, fieldOffset, recOffset);
                recOffset += _fieldDescs[n].FieldLength;				
				fieldOffset+=32;
			}
		}

        /// <summary>
        /// Reads the header contents from raw byte data
        /// </summary>
        /// <param name="data">byte array containing the header data</param>        
        /// <param name="dataOffset">offset in the data byte array containing the first byte of the header</param>        
		public void Read(byte[] data, int dataOffset)
		{
			_versionNumber = data[dataOffset];
			_year = data[dataOffset+1];
			_month = data[dataOffset+2];
			_day = data[dataOffset+3];
			_numRecords = EndianUtils.ReadIntLE(data,dataOffset+4);
			_headerLength = BitConverter.ToInt16(data, dataOffset+8);
			_recordLength = BitConverter.ToInt16(data, dataOffset+10);
			_numFields = (HeaderLength-33)/32;
			_fieldDescs = new DbfFieldDesc[FieldCount];

            _ldId = data[29 + dataOffset];

			//now read each Field Descriptor
			int fieldOffset = dataOffset+32;
			int recOffset = 1;//set to 1 as first byte in a record indicates whether valid/deleted
			for(int n=0;n<FieldCount;n++)
			{
                _fieldDescs[n] = new DbfFieldDesc(data, dataOffset + fieldOffset, recOffset);
                recOffset += _fieldDescs[n].FieldLength;
				fieldOffset+=32;
			}

		}


        /// <summary>
        /// The DBF version number
        /// </summary>
        public byte VersionNumber
        {
            get
            {
                return _versionNumber;
            }
            //set
            //{
            //    _versionNumber = value;
            //}
        }

        public byte Year
        {
            get
            {
                return _year;
            }
            //set
            //{
            //    _year = value;
            //}
        }

        public byte Month
        {
            get
            {
                return _month;
            }
            //set
            //{
            //    _month = value;
            //}
        }

        public byte Day
        {
            get
            {
                return _day;
            }
            //set
            //{
            //    _day = value;
            //}
        }

        /// <summary>
        /// The DBF Language Driver Id        
        /// </summary>
        /// <remarks>
        /// See http://downloads.esri.com/support/documentation/pad_/ArcPad_RefGuide_1105.pdf
        /// </remarks>
        public int LDID
        {
            get
            {
                return _ldId;
            }
        }

        /// <summary>
        /// the number of records contained in the DBF file
        /// </summary>
        public int RecordCount
        {
            get
            {
                return _numRecords;
            }
            //set
            //{
            //    _numRecords = value;
            //}
        }

        /// <summary>
        /// The length of the main header
        /// </summary>
        public short HeaderLength
        {
            get
            {
                return _headerLength;
            }
            //set
            //{
            //    _headerLength = value;
            //}
        }

        /// <summary>
        /// The length of each record (in bytes) in the DBF file. Note that each record will have the same length.
        /// </summary>
        public short RecordLength
        {
            get
            {
                return _recordLength;
            }
            //set
            //{
            //    _recordLength = value;
            //}
        }

        /// <summary>
        /// The number of fields in each record
        /// </summary>
        public int FieldCount
        {
            get
            {
                return _numFields;
            }
            //set
            //{
            //    _numFields = value;
            //}
        }

        /// <summary>
        /// Method to return an array of DbfFieldDesc structs, describing each field in a record
        /// </summary>
        /// <returns></returns>
        public DbfFieldDesc[] GetFieldDescriptions()
        {            
            return _fieldDescs;            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return "VersionNumber=" + VersionNumber + ", Year=" + Year + ", Month=" + Month + ", Day=" + Day +", NumRecords=" + RecordCount + ", HeaderLength=" + HeaderLength + ", RecordLength=" + RecordLength; 
		}
	}

    /// <summary>
    /// Specifies the type of a field in a DBF file 
    /// </summary>
    public enum DbfFieldType { Character = (int)('C'), Number = (int)('N'), Logical = (int)('L'), Date = (int)('D'), FloatingPoint = (int)('F'), Binary = (int)('B'), General = (int)('G'), None = 0 };

    /// <summary>
    /// Describes a single field in a DBF file, including the name of the field, the DbfFieldType and the length of the field
    /// </summary>
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public unsafe struct DbfFieldDesc
	{
		private string _fieldName;
		private DbfFieldType _fieldType;
		private int _fieldLength;

        private int _decimalCount;

		/// <summary>
		/// RecordOffset = the offset of the field in a record.
		/// For Field 0, RecordOffset = 0,
		/// For Field 1, RecordOffset = Length of Field 0,
		/// For Field N, RecordOffset = Length of Field0 to FieldN-1
		/// </summary>
		private int _recordOffset;
		
		/// <summary>
		/// Creates a DBFFieldDesc struct by reading from raw byte data. The contents of the raw byte data are as they would appear in the 
        /// header of a DBF file
		/// </summary>
		/// <param name="data">a byte array containing the raw header data</param>
		/// <param name="dataOffset">the zero based offset in the raw data from where to start reading</param>
		/// <param name="recordOffset">the zero based offset of the Field within a record</param>
		public DbfFieldDesc(byte[] data, int dataOffset, int recordOffset)
		{
			fixed (byte* bPtr = data)
			{
				//sbyte* sbptr = (sbyte*)(bPtr+dataOffset);
				_fieldName = new String((sbyte*)(bPtr+dataOffset)); 
			}
			_fieldType = (DbfFieldType)data[dataOffset+11];
            _fieldLength = (int)data[dataOffset+16];
            _decimalCount = (int)data[dataOffset + 17];
            _recordOffset = recordOffset;
             
		}

        /// <summary>
        /// The name of the field. The maximum length of a field name is 10 characters
        /// </summary>
        public string FieldName
        {
            get
            {
                return _fieldName;
            }
            
            set
            {
                if (string.IsNullOrEmpty(value)) throw new System.ArgumentNullException("fieldName can not be null");
                if (value.Length > 10) throw new System.ArgumentException("FielName length must be <=10");
                _fieldName = value;
            }
        }

        /// <summary>
        /// The field type
        /// </summary>
        public DbfFieldType FieldType
        {
            get
            {
                return _fieldType;
            }
            set
            {
                _fieldType = value;
            }
        }

        /// <summary>
        /// The length of the field. Note that in a DBF file each record is a constant length (even if a record is an empty string)
        /// </summary>
        public int FieldLength
        {
            get
            {
                return _fieldLength;
            }
            set
            {
                _fieldLength = value;
            }
        }

        /// <summary>
        /// Gets/Sets the decimal count for numeric data types.
        /// </summary>
        /// <remarks>
        /// <para>DecimalCount must be &gt;=0 and &lt;16</para>
        /// </remarks>
        public int DecimalCount
        {
            get
            {
                return _decimalCount;
            }
            set
            {
                if (value < 0 || value > 15) throw new ArgumentOutOfRangeException("DecimalCount must be >=0 and <=15");
                _decimalCount = value;
            }
        }

        /// <summary>
        /// RecordOffset = the offset of the field in a record.
        /// For Field 0, RecordOffset = 0,
        /// For Field 1, RecordOffset = Length of Field 0,
        /// For Field N, RecordOffset = Length of Field0 to FieldN-1
        /// </summary>
        public int RecordOffset
        {
            get
            {
                return _recordOffset;
            }
            set
            {
                _recordOffset = value;
            }
        }
		
        /// <summary>
        /// Returns a string representation of the DbfFieldDesc
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return "FieldName=" + FieldName + ", FieldType = " + (char)FieldType + ", FieldLength = " + FieldLength;
		}

	}


    #endregion
        

    
    #region EndianUtils

    internal class EndianUtils
	{

        private EndianUtils()
        {
        }

        public static byte[] GetBytesBE(int x)
        {
            byte[] b = new byte[4];
            b[3] = (byte)(x&0xff);
            b[2] = (byte)((x>>8)&0xff);
            b[1] = (byte)((x>>16)&0xff);
            b[0] = (byte)((x>>24)&0xff);
            return b;           
        }

        //public static byte[] GetBytesBE(short x)
        //{
        //    byte[] b = new byte[2];
        //    b[1] = (byte)(x & 0xff);
        //    b[0] = (byte)((x >> 8) & 0xff);
        //    return b;
        //}



		public static int ReadIntBE(byte[] data, int offset)
		{
			int result = data[offset];
			result= (result<<8)|data[offset+1];
			result= (result<<8)|data[offset+2];
			result= (result<<8)|data[offset+3];            
            return result;
		}

		public static unsafe int ReadIntLE(byte[] data, int offset)
		{			
			int result;
			fixed(byte* bPtr = data)
			{			
				//convert the byte array to a double Ptr and then store the dereferenced pointer in result
				result = *(int*)(bPtr+offset);
			}
            return result;
		}

        //public static unsafe double ReadDoubleBE(byte[] data, int offset)
        //{
        //    long result = data[offset];
        //    result= (result<<8)|data[offset+1];
        //    result= (result<<8)|data[offset+2];
        //    result= (result<<8)|data[offset+3];
        //    result= (result<<8)|data[offset+4];
        //    result= (result<<8)|data[offset+5];
        //    result= (result<<8)|data[offset+6];
        //    result= (result<<8)|data[offset+7];
		
        //    //convert the address of result to a long ptr and return the de-referenced pointer
        //    return *(double*)(&result);
        //}

		public static unsafe double ReadDoubleLE(byte[] data, int offset)
		{			
			double result;
			fixed(byte* bPtr = data)
			{			
				//convert the byte array to a double Ptr and then store the dereferenced pointer in result
				result = *(double*)(bPtr+offset);
			}
			return result;	
		}

		public static unsafe float ReadFloatLE(byte[] data, int offset)
		{			
			float result;
			fixed(byte* bPtr = data)
			{			
				//convert the byte array to a float Ptr and then store the dereferenced pointer in result
				result = *(float*)(bPtr+offset);
			}
			return result;	
		}

        //public static unsafe void WriteFloatLE(float f, byte[] data, int offset)
        //{		
        //    int n = *(int*)(&f);
        //    data[offset+1] = (byte)((n&0xff00)>>8);
        //    data[offset+2] = (byte)((n&0xff0000)>>16);
        //    data[offset+3] = (byte)((n&0xff000000)>>24);			
        //}

		/// <summary>
		/// swaps the bytes ordering of the data at offset
		/// ie. bytes offset, ofset+1, offset+2, offset+3 become offset+3, offset+2, offset+1, offset
		/// This can be used to convert a BE int to a LE int and vice-versa
		/// Note that no bounds checks are performed so offset must be &lt;= data.length-4
		/// </summary>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		public static void SwapIntBytes(byte[] data, int offset)
		{
			byte temp = data[offset];
			data[offset] = data[offset+3];
			data[offset+3]=temp;

			temp = data[offset+1];
			data[offset+1] = data[offset+2];
			data[offset+2] = temp;
		}

    }

    #endregion


    #region "IEnumerator classes"



    /// <summary>
    /// ShapeFile enumerator used to enumerate the raw data of each shape in a shapeFile.
    /// The ShapeFileEnumerator provides a fast, low memory, forward only means of iterating over all of the 
    /// records in a shapefile.
    /// </summary>
    public sealed class ShapeFileEnumerator : IEnumerator<System.Collections.ObjectModel.ReadOnlyCollection<PointD[]>>
    {
        /// <summary>
        /// Defines how shapes will be evaluated when moving to the next record when enumerating through a shapefile.
        /// Intersects (the default) evaluates shapes that intersect with the Extent of the ShapeFileEnumerator. Contains
        /// evaluates shapes that fit entirely within the Extent of the ShapeFileEnumerator.
        /// </summary>
        public enum IntersectionType { Intersects, Contains };

        int currentIndex = -1;

        private ShapeFile myShapeFile;

        private RectangleD[] recordExtents;
        private int totalRecords;

        private RectangleD extent;

        private byte[] dataBuffer = new byte[ShapeFileExConstants.MAX_REC_LENGTH];

        private IntersectionType intersectType = IntersectionType.Intersects;

        internal ShapeFileEnumerator(ShapeFile shapeFile, RectangleD extent):this(shapeFile, extent, IntersectionType.Intersects)
        {
        }

        internal ShapeFileEnumerator(ShapeFile shapeFile, RectangleD extent, IntersectionType intersectionType)
        {
            if (shapeFile == null) throw new ArgumentException("null shapefile");
            if(dataBuffer.Length < SFRecordCol.SharedBuffer.Length)
            {
                dataBuffer = new byte[SFRecordCol.SharedBuffer.Length];
                
            }
            this.intersectType = intersectionType;
            this.extent = extent;
            myShapeFile = shapeFile;
            if (shapeFile.ShapeType != ShapeType.Point)
            {
                recordExtents = shapeFile.GetShapeExtentsD();
            }
            totalRecords = shapeFile.RecordCount;
        }

        internal ShapeFileEnumerator(ShapeFile shapeFile)
        {
            if (shapeFile == null) throw new ArgumentException("null shapefile");
            if (dataBuffer.Length < SFRecordCol.SharedBuffer.Length)
            {
                dataBuffer = new byte[SFRecordCol.SharedBuffer.Length];
            }
            this.extent = shapeFile.Extent;
            myShapeFile = shapeFile;
            if (shapeFile.ShapeType != ShapeType.Point)
            {
                recordExtents = shapeFile.GetShapeExtentsD();
            }
            totalRecords = shapeFile.RecordCount;            
        }

        /// <summary>
        /// Gets the Extent of the enumerator. In most cases this will be the extent of the shapefile, but
        /// this may be smaller than the shape file's exent if the enumerator has been created from a sub region
        /// of the shapefile's extent
        /// </summary>
        public RectangleD Extent
        {
            get
            {
                return this.extent;
            }
        }

        #region IEnumerator<List<PointD[]>> Members

        /// <summary>
        /// Gets the raw PointD data of the current shape
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> Current
        {
            get 
            {
                return myShapeFile.GetShapeDataD(currentIndex, dataBuffer);
            }
        }
        

        /// <summary>
        /// Gets the index (zero based) of the current shape.        
        /// </summary>
        public int CurrentShapeIndex
        {
            get
            {
                return currentIndex;
            }
        }

        /// <summary>
        /// Gets the raw Z data of the current shape (3D shapefiles only)
        /// </summary>        
        public System.Collections.ObjectModel.ReadOnlyCollection<double[]> GetCurrentZValues()
        {
            //return myShapeFile.GetShapeData
            return myShapeFile.GetShapeZDataD(currentIndex, dataBuffer);
        }

        /// <summary>
        /// Gets the Measure data of the current shape.
        /// This method is currently not implemented
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<double[]> GetCurrentMValues()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            
        }

        #endregion

        #region IEnumerator Members
        
        object IEnumerator.Current
        {
            get { return this.Current; }
        }
        
        /// <summary>
        /// Advances the enumerator to the next shape in the shape file. The next shape may not be the next shape in the shape file if
        /// the enumerator has a smaller Extent than the shape file's Extent.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            currentIndex++;
            if (myShapeFile.ShapeType == ShapeType.Point || myShapeFile.ShapeType == ShapeType.PointZ)
            {
                while (currentIndex < totalRecords && !this.extent.Contains(myShapeFile.GetShapeBoundsD(currentIndex)))
                {
                    currentIndex++;
                }
            }            
            else
            {
                if (this.intersectType == IntersectionType.Intersects)
                {
                    
                    while (currentIndex < totalRecords && !this.myShapeFile.ShapeIntersectsRect(currentIndex, ref this.extent))
                    {
                        currentIndex++;
                    }
                    
                }
                else if (this.intersectType == IntersectionType.Contains)
                {
                    while (currentIndex < totalRecords && !this.extent.Contains(recordExtents[currentIndex]))
                    {
                        currentIndex++;
                    }
                }
            }
            return (currentIndex < totalRecords);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first shape in the shapefile
        /// </summary>
        public void Reset()
        {
            currentIndex = -1;
        }

        /// <summary>
        /// Convenience method which returns he total records contained in the shapefile. TotalRecords is the same as the shape File's RecordCount property         
        /// </summary>
        public int TotalRecords
        {
            get
            {
                return totalRecords;
            }
        }

        #endregion
    }

    #endregion


}
