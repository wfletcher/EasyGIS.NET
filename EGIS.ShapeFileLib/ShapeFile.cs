using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Xml;
using System.Security.Permissions;

[assembly: CLSCompliant(true)]
//give the EGIS.Controls access to the internal methods
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EGIS.Controls,     PublicKey=0024000004800000940000000602000000240000525341310004000001000100ad413f7f4a7f27fbb045d205cfc65fe64665694533fc72b0d82433368f98f7bd82c18b98ee2f5fe417ed1427a9e6ff84e5dce034638bb7761ea22c9881b8fa09ac621ad78ebb3002b3dbb876f479fa0b2bccd95fc1d54c7fc87b5dc084d575fb304387c9bbd4ce6a5bf91328ae3ecc3f5472a14ce8e572d7d01d01483fe1f2d0")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EGIS.Web.Controls, PublicKey=0024000004800000940000000602000000240000525341310004000001000100ad413f7f4a7f27fbb045d205cfc65fe64665694533fc72b0d82433368f98f7bd82c18b98ee2f5fe417ed1427a9e6ff84e5dce034638bb7761ea22c9881b8fa09ac621ad78ebb3002b3dbb876f479fa0b2bccd95fc1d54c7fc87b5dc084d575fb304387c9bbd4ce6a5bf91328ae3ecc3f5472a14ce8e572d7d01d01483fe1f2d0")]
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
    /// The class is designed to open very large shapefiles, using a low memory implementation which WILL NOT LOAD the contents of the
    /// shapefile in memory.    
    /// <para>
    /// NOTE THAT MANY OF THE METHODS IN THIS CLASS ARE NOT THREAD SAFE (due to internal shared buffers). If multiple threads require access to ShapeFile objects
    /// then the static (Shared in VB) Sync object should be used to provide mutual access to the shapefiles
    /// </para>
    /// <para>
    /// An ESRI Shape File consists of 3 files - the ShapeFile index file (.shx), the shape file (.shp) and a DBF file (.dbf). For a more detailed
    /// desription of the ShapeFile format consult the ESRI ShapeFile file specification.</para>
    /// <para>
    /// Currently supported Shape Types are Point, PolyLine, Polygon and PolyLineM</para>   
    /// </remarks>        
    public sealed class ShapeFile : IDisposable
	{
        internal static bool TestSEcurity()
        {
            return true;
        }

		const int MAIN_HEADER_LENGTH = 100;

		private ShapeFileMainHeaderEx mainHeader;
		private RecordHeader[] recordHeaders;
		private SFRecordCol mySFRecordCol;
		private BinaryReader shapeFileStream;		
		private string filePath;				
		private RenderSettings myRenderer;

        private static RenderQuality renderQuality = RenderQuality.Auto;
        
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
        /// Gets the ShapeType of the ShapeFile
        /// 
        /// </summary>
        public ShapeType ShapeType
        {
            get
            {
                return mainHeader.ShapeType;
            }
        }

        private static object syncObj = new object();

        /// <summary>
        /// Gets an object that can be used to synchronize access to the ShapeFile
        /// </summary>
        /// <remarks>
        /// NOTE: Many of the methods in theis class are not Thread Safe (due to internal shared buffers). If multiple threads require access to ShapeFile objects
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

        /// <summary>
        /// Gets the Rectangular extent of the shapefile
        /// </summary>
        /// <remarks>If the UseMercatorProjection is true then the returned Extent will be in the projected Mercator coordinates </remarks>
		public RectangleF Extent
		{
			get
			{
				if(mySFRecordCol != null)
				{
                    if (SFRecordCol.MercProj)
                    {
                        PointF tl = SFRecordCol.LLToProjection(new PointF(mySFRecordCol.MainHeader.Xmin, mySFRecordCol.MainHeader.Ymin));
                        PointF br = SFRecordCol.LLToProjection(new PointF(mySFRecordCol.MainHeader.Xmax, mySFRecordCol.MainHeader.Ymax));
                        RectangleF r = RectangleF.FromLTRB(tl.X, tl.Y, br.X, br.Y);
                        return r;
                        
                    }
                    else
                    {
                        RectangleF r = RectangleF.FromLTRB(mySFRecordCol.MainHeader.Xmin, mySFRecordCol.MainHeader.Ymin, mySFRecordCol.MainHeader.Xmax, mySFRecordCol.MainHeader.Ymax);
                        return r;
                    }
				}
				else
				{
					return RectangleF.Empty;
				}
			}
		}

        /// <summary>
        /// Returns the actual ShapeFile Extent in unprojected coordinates
        /// </summary>
        /// <remarks>The returned rectanlge is the extent contained in the shapefile's header</remarks>
        /// <returns></returns>
        public RectangleF GetActualExtent()
        {
            if (mySFRecordCol != null)
            {                
                return RectangleF.FromLTRB(mySFRecordCol.MainHeader.Xmin, mySFRecordCol.MainHeader.Ymin, mySFRecordCol.MainHeader.Xmax, mySFRecordCol.MainHeader.Ymax);                
            }
            else
            {
                return RectangleF.Empty;
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



        /// <summary>
        /// Gets the rectangular extent of each shape in the shapefile
        /// </summary>
        /// <returns>RectangleF[] representing the extent of each shape in the shapefile</returns>
        public RectangleF[] GetShapeExtents()
        {
            if (mySFRecordCol is SFPolygonExCol)
            {
                SFPolygonExCol polyCol = mySFRecordCol as SFPolygonExCol;
                RectangleF[] extents = new RectangleF[polyCol.Recs.Length];
                int index = extents.Length - 1;
                while (index >= 0)
                {
                    extents[index] = polyCol.Recs[index].Bounds;
                    index--;
                }
                return extents;
            }
            else if (mySFRecordCol is SFPolyLineExCol)
            {
                SFPolyLineExCol polyCol = mySFRecordCol as SFPolyLineExCol;
                RectangleF[] extents = new RectangleF[polyCol.Recs.Length];
                extents = new RectangleF[polyCol.Recs.Length];
                int index = extents.Length - 1;
                while (index >= 0)
                {
                    extents[index] = polyCol.Recs[index].Bounds;
                    index--;
                }
                return extents;
            }
            else if (mySFRecordCol is SFPolygonZExCol)
            {
                SFPolygonZExCol polyCol = mySFRecordCol as SFPolygonZExCol;
                RectangleF[] extents = new RectangleF[polyCol.Recs.Length];
                int index = extents.Length - 1;
                while (index >= 0)
                {
                    extents[index] = polyCol.Recs[index].Bounds;
                    index--;
                }
                return extents;
            }
            throw new NotImplementedException("Not yet implemented");
            
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
        /// Gets or sets the index (zero based) of the current selected shape/record. If no shape is selected then SelectedRecordIndex is -1
        /// </summary>
        public int SelectedRecordIndex
        {
            set
            {
                if (mySFRecordCol != null) mySFRecordCol.SetSelectedRecordIndex(value);
            }
            get
            {
                if (mySFRecordCol == null)
                {
                    return -1;
                }
                else
                {
                    return mySFRecordCol.GetSelectedRecordIndex();
                }
            }
        }

        /// <summary>
        /// Gets the number of records(shapes) in the ShapeFile
        /// </summary>
        public int RecordCount
        {
            get
            {
                return mySFRecordCol.MainHeader.NumRecords;
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


        #region Render Methods

        void Render(Graphics g, Size clientArea, RectangleF extent)
        {
            Render(g, clientArea, extent, RenderSettings);
        }

		void Render(Graphics g, Size clientArea, RectangleF extent, RenderSettings renderSettings)
		{
            //DateTime dts = DateTime.Now;
            if (!Extent.IntersectsWith(extent)) return;
           	if(mySFRecordCol != null)
			{
                mySFRecordCol.paint(g, clientArea, extent, shapeFileStream.BaseStream, RenderSettings);
			}
            //Console.Out.WriteLine("render time: " + ((TimeSpan)DateTime.Now.Subtract(dts)));
		}

        void Render(Graphics g, Size clientArea, RectangleD extent, RenderSettings renderSettings)
        {
            DateTime dts = DateTime.Now;
            //if (!Extent.IntersectsWith(extent)) return;
            if (!extent.IntersectsWith(Extent)) return;
            if (mySFRecordCol != null)
            {
                mySFRecordCol.paint(g, clientArea, extent, shapeFileStream.BaseStream, RenderSettings);
            }
            Console.Out.WriteLine("render time: " + ((TimeSpan)DateTime.Now.Subtract(dts)));
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
        public void Render(Graphics graphics, Size clientArea, PointF centre, float zoom, RenderSettings renderSetings)
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
            Render(graphics, clientArea, r, renderSetings);

            Font f = new Font("Arial", 8);
            StringFormat sf = new StringFormat();
            Brush brush = new SolidBrush(Color.FromArgb(100, Color.Black));
            try
            {                
                sf.Alignment = StringAlignment.Far;
                sf.LineAlignment = StringAlignment.Far;
                graphics.DrawString("Map Generated by Easy GIS .NET", f, brush, new PointF(clientArea.Width - 10, clientArea.Height - 10), sf);                
            }
            finally
            {
                f.Dispose();
                sf.Dispose();
                brush.Dispose();
            }

        }

        /// <summary>
        /// Renders the shapefile centered at given point and zoom
        /// </summary>
        /// <param name="graphics">The Graphics device to render to</param>
        /// <param name="clientArea">The client area in pixels to draw </param>
        /// <param name="centre">The centre point in the ShapeFiles coordinates</param>
        /// <param name="zoom">The scaling to apply</param>
        /// <remarks>
        /// If zoom is 1 and the width of the ShapeFile's extent is N units wide, then the
        /// ShapeFile wil be rendered N pixels wide. If zoom is 2 then shapefile will be rendered 2N pixels wide
        /// </remarks>
        public void Render(Graphics graphics, Size clientArea, PointF centre, float zoom)
        {
            this.Render(graphics, clientArea, centre, zoom, this.RenderSettings);
        }        

        internal void RenderInternal(Graphics graphics, Size clientArea, PointF centre, float zoom)
        {
            this.RenderInternal(graphics, clientArea, centre, zoom, this.RenderSettings);
        }

        internal void RenderInternal(Graphics graphics, Size clientArea, PointD centre, double zoom)
        {
            this.RenderInternal(graphics, clientArea, centre, zoom, this.RenderSettings);
        }


        internal void RenderInternal(Graphics graphics, Size clientArea, PointF centre, float zoom, RenderSettings renderSetings)
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
            Render(graphics, clientArea, r, renderSetings);
        }

        internal void RenderInternal(Graphics graphics, Size clientArea, PointD centre, double zoom, RenderSettings renderSetings)
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
            Render(graphics, clientArea, r, renderSetings);
        }


        #endregion

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
        {
            LoadFromFile(shapeFilePath);
        }

        /// <summary>
        /// Loads a ShapeFile using a path to a .shp shape file
        /// </summary>
        /// <param name="shapeFilePath">The path to the ".shp" shape file</param>
        public void LoadFromFile(string shapeFilePath)
        {
            if (shapeFilePath.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
            {
                shapeFilePath = shapeFilePath.Substring(0, shapeFilePath.Length - 4);
            }
            
            DateTime start = DateTime.Now;

            if (!File.Exists(shapeFilePath + ".shpx"))
            {
                try
                {
                    //read record headers from the index file
                    System.IO.BinaryReader bReader = new BinaryReader(new FileStream(shapeFilePath + ".shx", FileMode.Open));
                    try
                    {
                        ShapeFileMainHeader mainHeader = new ShapeFileMainHeader(bReader.ReadBytes(100));
                        //this.mainHeader = mainHeader;
                        //create the recordHeaders array - length is the fileLength - mainheader / 8 bytes per rec header
                        int totalRecords = (mainHeader.FileLength - 100) / 8;
                        recordHeaders = new RecordHeader[totalRecords];
                        int numRecs = 0;
                        //now read the record headers
                        byte[] data = new byte[mainHeader.FileLength - 100];
                        bReader.Read(data, 0, data.Length);
                        int maxRecLength = 0;
                        while (numRecs < totalRecords)
                        {
                            RecordHeader recHead = new RecordHeader(numRecs + 1);
                            recHead.readFromIndexFile(data, numRecs << 3);
                            if (numRecs > 0)
                            {
                                maxRecLength = Math.Max(maxRecLength, recHead.Offset - recordHeaders[numRecs - 1].Offset);

                            }
                            recordHeaders[numRecs++] = recHead;                                                        
                        }
                        
                        FileStream fs = new FileStream(shapeFilePath + ".shp", FileMode.Open, FileAccess.Read, FileShare.Read);
                        try
                        {
                            if (numRecs == 1)
                            {
                                maxRecLength = (int)fs.Length;
                            }
                            else
                            {
                                maxRecLength = Math.Max(maxRecLength, (int)fs.Length - recordHeaders[numRecs - 1].Offset);
                            }
                        }
                        finally
                        {
                            fs.Close();
                        }
                        //ensure the shared buffer is large enough
                        SFRecordCol.EnsureBufferSize(maxRecLength);


                        if (mainHeader.ShapeType == ShapeType.PolyLineM)
                        {
                            //now read the shape record headers from the main shapefile
                            PolyLineMRecord[] plmRecs = readPLMShapeHeaders(shapeFilePath, mainHeader, recordHeaders);

                            if (plmRecs != null)
                            {
                                createShapeFileEx(shapeFilePath, mainHeader, plmRecs);
                            }
                        }
                        else if (mainHeader.ShapeType == ShapeType.PolyLine)
                        {
                            //now read the shape record headers from the main shapefile
                            PolyLineRecord[] plRecs = readPLShapeHeaders(shapeFilePath, mainHeader, recordHeaders);

                            if (plRecs != null)
                            {
                                createShapeFileEx(shapeFilePath, mainHeader, plRecs);
                            }
                        }
                        else if (mainHeader.ShapeType == ShapeType.Polygon)
                        {
                            //now read the shape record headers from the main shapefile
                            PolygonRecord[] pgRecs = readPGShapeHeaders(shapeFilePath, mainHeader, recordHeaders);

                            if (pgRecs != null)
                            {
                                createShapeFileEx(shapeFilePath, mainHeader, pgRecs);
                            }
                        }
                        else if (mainHeader.ShapeType == ShapeType.PolygonZ)
                        {
                            //now read the shape record headers from the main shapefile
                            PolygonZRecord[] pgRecs = readPGZShapeHeaders(shapeFilePath, mainHeader, recordHeaders);

                            if (pgRecs != null)
                            {
                                createShapeFileEx(shapeFilePath, mainHeader, pgRecs);
                            }
                        }
                        else if (mainHeader.ShapeType == ShapeType.Point)
                        {
                            createShapeFileEx(shapeFilePath, mainHeader);

                        }
                        else throw new NotSupportedException("Shape Type " + mainHeader.ShapeType + " not supported");

                    }
                    finally
                    {
                        bReader.Close();
                        bReader = null;
                    }                    
                }
                finally
                {
                    if (this.shapeFileStream != null)
                    {
                        shapeFileStream.Close();
                    }
                }
                System.GC.Collect();
            }

            mySFRecordCol = readShapeHeadersEx(shapeFilePath);

            this.mainHeader = mySFRecordCol.MainHeader;
            this.filePath = shapeFilePath;

            this.recordHeaders = null;

            //MapFile(shapeFilePath + ".shpx");

            DateTime end = DateTime.Now;
            System.Diagnostics.Debug.WriteLine("Shape Type : " + this.mainHeader.ShapeType);
            System.Diagnostics.Debug.WriteLine("total number of records = " + mySFRecordCol.MainHeader.NumRecords);
            System.Diagnostics.Debug.WriteLine("Total time to read shapefile is " + end.Subtract(start).ToString());
            //data = null;
        }

       
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
            if (shapeFileStream != null)
            {
                shapeFileStream.Close();
            }
            if (this.RenderSettings != null)
            {
                RenderSettings.Dispose();
                this.RenderSettings = null;
            }
            shapeQuadTree = null;
            recordHeaders = null;
            mySFRecordCol = null;
            filePath = null;
        }

        /// <summary>
        /// overridden ToString method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (filePath == null) return "Empty";
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} [{1}]", new object[] { myName, mainHeader.ShapeType.ToString() });
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
        public void ReadXml(XmlElement element)
        {            
            string path = element.GetElementsByTagName("path")[0].InnerText;
            //check if path is relative to project
            if (!Path.IsPathRooted(path))
            {
                string rootDir = "";
                Uri uri = new Uri(element.OwnerDocument.BaseURI);
                if (uri.IsFile)
                {
                    rootDir = System.IO.Path.GetDirectoryName(uri.AbsolutePath);
                }
                path = rootDir + "\\" + path;
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


        #region "Find shapes by position methods"

        /// <summary>
        /// Gets the raw shape data at given record index.        
        /// </summary>
        /// <param name="recordIndex"> The zero based index of the shape data to return</param>
        /// <returns></returns>
        /// <remarks>If you are reading every record in a large shape file then the preferred method is to call GetShapeFileEnumerator</remarks>
        /// <seealso cref="GetShapeFileEnumerator()"/>
        public System.Collections.ObjectModel.Collection<PointF[]> GetShapeData(int recordIndex)
        {
            return new System.Collections.ObjectModel.Collection<PointF[]>(mySFRecordCol.GetShapeData(recordIndex, this.shapeFileStream));
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
        public System.Collections.ObjectModel.Collection<PointF[]> GetShapeData(int recordIndex, byte[] dataBuffer)
        {
            return new System.Collections.ObjectModel.Collection<PointF[]>(mySFRecordCol.GetShapeData(recordIndex, this.shapeFileStream, dataBuffer));
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
        public System.Collections.ObjectModel.Collection<float[]> GetShapeZData(int recordIndex)
        {
            return new System.Collections.ObjectModel.Collection<float[]>(mySFRecordCol.GetShapeHeightData(recordIndex, this.shapeFileStream));
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
        public System.Collections.ObjectModel.Collection<float[]> GetShapeZData(int recordIndex, byte[] dataBuffer)
        {
            return new System.Collections.ObjectModel.Collection<float[]>(mySFRecordCol.GetShapeHeightData(recordIndex, this.shapeFileStream, dataBuffer));
        }


        /// <summary>
        /// Gets the rectangular bounds of an individual shape in the ShapeFile
        /// </summary>
        /// <param name="recordIndex">Zero based index of shape bounds to return</param>
        /// <returns></returns>
        public RectangleF GetShapeBounds(int recordIndex)
        {
            if (recordIndex < 0 || recordIndex >= mainHeader.NumRecords) throw new ArgumentException("recordIndex must be >=0 and < " + mainHeader.NumRecords);
            if (mySFRecordCol is SFPolygonExCol)
            {                
                SFPolygonExCol polyCol = mySFRecordCol as SFPolygonExCol;
                return polyCol.Recs[recordIndex].Bounds;                
            }
            else if (mySFRecordCol is SFPolyLineExCol)
            {
                SFPolyLineExCol polyCol = mySFRecordCol as SFPolyLineExCol;
                return polyCol.Recs[recordIndex].Bounds;                
            }
            else if (mySFRecordCol is SFPolyLineMExCol)
            {
                SFPolyLineMExCol polyCol = mySFRecordCol as SFPolyLineMExCol;
                return polyCol.Recs[recordIndex].Bounds;
            }
            else if (mySFRecordCol is SFPointExCol)
            {
                SFPointExCol pointCol = mySFRecordCol as SFPointExCol;
                return new RectangleF(pointCol.Recs[recordIndex].pt, new SizeF(0, 0));
            }
            return RectangleF.Empty;
        }

        /// <summary>
        /// returns the index of the shape containing Point pt
        /// </summary>
        /// <param name="pt">The location of the point (in the shapefile's coordinates)</param>
        /// <param name="minDistance">the min distance (in ShapeFile coordinates) between pt and a shape when searching point or line shapes
        /// that will return a "hit". </param>
        /// <returns>zero based index of the shape containing pt or -1 if no shape contains the point</returns>
        /// <remarks>When searching in a Point ShapeFile a point will be defined as contining pt if the distance between the found point and pt is less than or equal to minDistance
        /// <para>When searching in a PolyLine ShapeFile a Line will be defined as containing the point if the distance between a line segment in the found shape and pt is less than or equal to minDistance</para>
        /// </remarks>
        public int GetShapeIndexContainingPoint(PointF pt, float minDistance)
        {
            //if (UseMercatorProjection)
            //{
            //    //cnvert to actual LL
            //    pt = MercatorToLLF(pt);
            //}
            //first check the entire shapefile's Extent
            RectangleF extent = GetActualExtent();
            if (extent.Contains(pt))
            {
                switch (mySFRecordCol.MainHeader.ShapeType)
                {
                    case ShapeType.Point:
                        return GetShapeIndexContainingPoint(pt, minDistance, mySFRecordCol as SFPointExCol);                        
                    case ShapeType.PointM:
                        break;
                    case ShapeType.Polygon:
                        return GetShapeIndexContainingPoint(pt, mySFRecordCol as SFPolygonExCol);
                    case ShapeType.PolygonZ:
                        return GetShapeIndexContainingPoint(pt, mySFRecordCol as SFPolygonZExCol);                        
                    case ShapeType.PolyLine:
                        return GetShapeIndexContainingPoint(pt, minDistance, mySFRecordCol as SFPolyLineExCol);                        
                    default:
                        return -1;
                }
                
            }
            return -1;
        }


        private QuadTree shapeQuadTree;

        private void CreateQuadTree(SFPolygonExCol col)
        {
            shapeQuadTree = new QuadTree(GetActualExtent());
            for (int n = 0; n < col.Recs.Length; n++)
            {
                shapeQuadTree.Insert(n, col);
            }

        }

        private void CreateQuadTree(SFPolygonZExCol col)
        {
            shapeQuadTree = new QuadTree(GetActualExtent());
            for (int n = 0; n < col.Recs.Length; n++)
            {
                shapeQuadTree.Insert(n, col);
            }

        }

        private void CreateQuadTree(SFPolyLineExCol col)
        {
            shapeQuadTree = new QuadTree(GetActualExtent());
            for (int n = 0; n < col.Recs.Length; n++)
            {
                shapeQuadTree.Insert(n, col);
            }

        }

        private int GetShapeIndexContainingPoint(PointF pt, SFPolygonExCol col)
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
                    if (col.Recs[indices[n]].Bounds.Contains(pt))
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

        private int GetShapeIndexContainingPoint(PointF pt, SFPolygonZExCol col)
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
                    if (col.Recs[indices[n]].Bounds.Contains(pt))
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


        private static int GetShapeIndexContainingPoint(PointF pt, float minDistance, SFPointExCol col)
        {
            float distSqr = minDistance * minDistance;
            int numRecs = col.MainHeader.NumRecords;
            for (int n = 0; n < numRecs; n++)
            {
                float x = (pt.X - col.Recs[n].pt.X);
                float y = (pt.Y - col.Recs[n].pt.Y);
                float d = x * x + y * y;
                if (d <= distSqr)
                {
                    return n;
                }
            }
            return -1;
        }

        private int GetShapeIndexContainingPoint(PointF pt, float minDistance, SFPolyLineExCol col)
        {
            if (shapeQuadTree == null)
            {
                CreateQuadTree(col);
            }

            List<int> indices = shapeQuadTree.GetIndices(pt);
            if (indices != null)
            {
                byte[] buffer = SFRecordCol.SharedBuffer;                                
                RectangleF r = new RectangleF(pt.X - minDistance, pt.Y - minDistance, minDistance * 2f, minDistance * 2f);
                for (int n = 0; n < indices.Count; n++)
                {
                    if (col.Recs[indices[n]].Bounds.IntersectsWith(r))
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

        #region "private methods"
        
		
		private void createShapeFileEx(string shapeFile, ShapeFileMainHeader mainHeader, PolyLineMRecord[] plmRecs )
		{			
			BinaryWriter bWriter = new BinaryWriter(new FileStream(shapeFile+".shpx",FileMode.Create));
			try
			{
				ShapeFileMainHeaderEx headEx = new ShapeFileMainHeaderEx(mainHeader);
				headEx.ShapeType = ShapeType.PolyLineM;
				headEx.NumRecords = plmRecs.Length;

				headEx.write(bWriter);

				//write the record headers
				for(int n=0;n<plmRecs.Length;n++)
				{
					PolyLineMRecordEx recEx = new PolyLineMRecordEx(plmRecs[n]);					
					recEx.write(bWriter);					
				}
				
				//store the offset of the first byte of data in the file
				headEx.DataOffset = (int)bWriter.BaseStream.Position;
				
				//now write the data..
                byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
				this.shapeFileStream.BaseStream.Seek(MAIN_HEADER_LENGTH,SeekOrigin.Begin);
                int inFileOffset = MAIN_HEADER_LENGTH;
				for(int n=0;n<plmRecs.Length;n++)
				{
                    if (n > 0 && recordHeaders[n].Offset != inFileOffset)
                    {
                        //System.Diagnostics.Debug.WriteLine("removing unused data");
                        this.shapeFileStream.Read(data, 0, recordHeaders[n].Offset - inFileOffset);
                        inFileOffset = recordHeaders[n].Offset;
                    }
					int offset = 8 + 44 + (plmRecs[n].NumParts<<2);
					int dataLength = plmRecs[n].NumPoints<<4;
					//this.shapeFileStream.Read(data,0,dataLength+offset);
					this.shapeFileStream.Read(data,0,8 + plmRecs[n].recordHeader.ContentLength);
					int indexD = 0;
					while(indexD < dataLength)
					{
						double dd = EndianUtils.ReadDoubleLE(data,indexD+offset);
						bWriter.Write((float)dd);						
						indexD+=8;
					}
					indexD+=16; //skip the M range
					dataLength = (dataLength>>1) + indexD;
					while(indexD < dataLength)
					{
						double dd = EndianUtils.ReadDoubleLE(data,indexD+offset);
						bWriter.Write((float)dd);
						indexD+=8;
					}
                    inFileOffset += (8 + plmRecs[n].recordHeader.ContentLength);
				}

				//update the mainheader and re-write

				headEx.FileLength = (int)bWriter.BaseStream.Position;								
				bWriter.Seek(0,SeekOrigin.Begin);
				headEx.write(bWriter);				
			}
			finally
			{
				bWriter.Close();
			}
		}
		
		private void createShapeFileEx(string shapeFile, ShapeFileMainHeader mainHeader, PolyLineRecord[] plRecs )
		{			
			BinaryWriter bWriter = new BinaryWriter(new FileStream(shapeFile+".shpx",FileMode.Create));
			try
			{
				ShapeFileMainHeaderEx headEx = new ShapeFileMainHeaderEx(mainHeader);
				headEx.ShapeType = ShapeType.PolyLine;
				headEx.NumRecords = plRecs.Length;

				headEx.write(bWriter);
				
				//write the record headers
				for(int n=0;n<plRecs.Length;n++)
				{
					PolyLineRecordEx recEx;
					recEx.Bounds = RectangleF.FromLTRB((float)plRecs[n].bounds.xmin,(float)plRecs[n].bounds.ymin,(float)plRecs[n].bounds.xmax,(float)plRecs[n].bounds.ymax);
					recEx.RecordNumber = plRecs[n].recordHeader.RecordNumber;
					recEx.NumParts = plRecs[n].NumParts;
					recEx.NumPoints = plRecs[n].NumPoints;
					recEx.PartOffsets = new int[recEx.NumParts];
                    recEx.DataOffset = -1;
					for(int p=0;p<recEx.NumParts;p++)
					{
						recEx.PartOffsets[p] = plRecs[n].PartOffsets[p];						
					}
					recEx.write(bWriter);
				}
				
				//store the offset of the first byte of data in the file
				headEx.DataOffset = (int)bWriter.BaseStream.Position;

				//now write the data..
                byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
				this.shapeFileStream.BaseStream.Seek(MAIN_HEADER_LENGTH,SeekOrigin.Begin);
                int inFileOffset = MAIN_HEADER_LENGTH;
				for(int n=0;n<plRecs.Length;n++)
				{
                    if (n > 0 && recordHeaders[n].Offset != inFileOffset)
                    {
                        //System.Diagnostics.Debug.WriteLine("removing unused data");
                        this.shapeFileStream.Read(data, 0, recordHeaders[n].Offset - inFileOffset);
                        inFileOffset = recordHeaders[n].Offset;
                    }
					int offset = 8 + 44 + (plRecs[n].NumParts<<2);
					int dataLength = plRecs[n].NumPoints<<4;
					this.shapeFileStream.Read(data,0,8 + plRecs[n].recordHeader.ContentLength);
					int indexD = 0;
					while(indexD < dataLength)
					{
						double dd = EndianUtils.ReadDoubleLE(data,indexD+offset);
						bWriter.Write((float)dd);						
						indexD+=8;
					}
                    inFileOffset += (8 + plRecs[n].recordHeader.ContentLength);
				}

				//update the mainheader and re-write
				headEx.FileLength = (int)bWriter.BaseStream.Position;								
				bWriter.Seek(0,SeekOrigin.Begin);
				headEx.write(bWriter);				

			}
			finally
			{
				bWriter.Close();
			}
		}
		
		private void createShapeFileEx(string shapeFile, ShapeFileMainHeader mainHeader, PolygonRecord[] pgRecs )
		{			
			BinaryWriter bWriter = new BinaryWriter(new FileStream(shapeFile+".shpx",FileMode.Create));
			try
			{
				ShapeFileMainHeaderEx headEx = new ShapeFileMainHeaderEx(mainHeader);
				headEx.ShapeType = ShapeType.Polygon;
				headEx.NumRecords = pgRecs.Length;

				headEx.write(bWriter);
				
                RectangleF sfExtent = RectangleF.FromLTRB(headEx.Xmin-float.Epsilon, headEx.Ymin-float.Epsilon, headEx.Xmax+float.Epsilon, headEx.Ymax+float.Epsilon);
                
                //write the record headers
				for(int n=0;n<pgRecs.Length;n++)
				{
					PolygonRecordEx recEx;
					recEx.Bounds = RectangleF.FromLTRB((float)pgRecs[n].bounds.xmin,(float)pgRecs[n].bounds.ymin,(float)pgRecs[n].bounds.xmax,(float)pgRecs[n].bounds.ymax);
                    
					recEx.RecordNumber = pgRecs[n].recordHeader.RecordNumber;
					recEx.NumParts = pgRecs[n].NumParts;
					recEx.NumPoints = pgRecs[n].NumPoints;
					recEx.PartOffsets = new int[recEx.NumParts];
                    recEx.DataOffset = -1;
					for(int p=0;p<recEx.NumParts;p++)
					{
						recEx.PartOffsets[p] = pgRecs[n].PartOffsets[p];						
					}
					recEx.write(bWriter);                
				}
				
				//store the offset of the first byte of data in the file
				headEx.DataOffset = (int)bWriter.BaseStream.Position;

				//now write the data..
                byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
				this.shapeFileStream.BaseStream.Seek(MAIN_HEADER_LENGTH,SeekOrigin.Begin);
                int inFileOffset = MAIN_HEADER_LENGTH;
				for(int n=0;n<pgRecs.Length;n++)
				{
                    if (n > 0 && recordHeaders[n].Offset != inFileOffset)
                    {
                        this.shapeFileStream.Read(data, 0, recordHeaders[n].Offset-inFileOffset);
                        inFileOffset = recordHeaders[n].Offset;
                    }
					int offset = 8 + 44 + (pgRecs[n].NumParts<<2);
					int dataLength = pgRecs[n].NumPoints<<4;
                    if (data.Length < (pgRecs[n].recordHeader.ContentLength+8)) data = new byte[pgRecs[n].recordHeader.ContentLength+8];
					this.shapeFileStream.Read(data,0,8 + pgRecs[n].recordHeader.ContentLength);
					int indexD = 0;
					while(indexD < dataLength)
					{
						double dd = EndianUtils.ReadDoubleLE(data,indexD+offset);
						bWriter.Write((float)dd);						
						indexD+=8;                                                
					}
                    inFileOffset += (8 + pgRecs[n].recordHeader.ContentLength);
				}

				//update the mainheader and re-write
				headEx.FileLength = (int)bWriter.BaseStream.Position;								
				bWriter.Seek(0,SeekOrigin.Begin);
				headEx.write(bWriter);				

			}
			finally
			{
				bWriter.Close();
			}
		}

        private void createShapeFileEx(string shapeFile, ShapeFileMainHeader mainHeader, PolygonZRecord[] pgRecs)
        {
            BinaryWriter bWriter = new BinaryWriter(new FileStream(shapeFile + ".shpx", FileMode.Create));
            try
            {
                ShapeFileMainHeaderEx headEx = new ShapeFileMainHeaderEx(mainHeader);
                headEx.ShapeType = ShapeType.PolygonZ;
                headEx.NumRecords = pgRecs.Length;

                headEx.write(bWriter);

                RectangleF sfExtent = RectangleF.FromLTRB(headEx.Xmin - float.Epsilon, headEx.Ymin - float.Epsilon, headEx.Xmax + float.Epsilon, headEx.Ymax + float.Epsilon);

                //write the record headers
                for (int n = 0; n < pgRecs.Length; n++)
                {
                    PolygonZRecordEx recEx;
                    recEx.Bounds = RectangleF.FromLTRB((float)pgRecs[n].bounds.xmin, (float)pgRecs[n].bounds.ymin, (float)pgRecs[n].bounds.xmax, (float)pgRecs[n].bounds.ymax);

                    recEx.RecordNumber = pgRecs[n].recordHeader.RecordNumber;
                    recEx.NumParts = pgRecs[n].NumParts;
                    recEx.NumPoints = pgRecs[n].NumPoints;
                    recEx.PartOffsets = new int[recEx.NumParts];                   
                    recEx.DataOffset = -1;
                    for (int p = 0; p < recEx.NumParts; p++)
                    {
                        recEx.PartOffsets[p] = pgRecs[n].PartOffsets[p];
                    }
                    recEx.MinZ = (float)pgRecs[n].MinZ;
                    recEx.MaxZ = (float)pgRecs[n].MaxZ;
                    recEx.MinMeasure = (float)pgRecs[n].MinM;
                    recEx.MaxMeasure = (float)pgRecs[n].MaxM;

                    recEx.write(bWriter);
                }

                //store the offset of the first byte of data in the file
                headEx.DataOffset = (int)bWriter.BaseStream.Position;

                //now write the data..
                byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
                this.shapeFileStream.BaseStream.Seek(MAIN_HEADER_LENGTH, SeekOrigin.Begin);
                int inFileOffset = MAIN_HEADER_LENGTH;
                for (int n = 0; n < pgRecs.Length; n++)
                {
                    if (n > 0 && recordHeaders[n].Offset != inFileOffset)
                    {
                        this.shapeFileStream.Read(data, 0, recordHeaders[n].Offset - inFileOffset);
                        inFileOffset = recordHeaders[n].Offset;
                    }
                    int offset = 8 + 44 + (pgRecs[n].NumParts << 2);
                    int dataLength = pgRecs[n].NumPoints << 4;
                    if (data.Length < (pgRecs[n].recordHeader.ContentLength + 8)) data = new byte[pgRecs[n].recordHeader.ContentLength + 8];
                    this.shapeFileStream.Read(data, 0, 8 + pgRecs[n].recordHeader.ContentLength);
                    int indexD = 0;
                    //write the point data
                    //while (indexD < dataLength)
                    int numPoints = pgRecs[n].NumPoints;
                    for(int p=0;p<numPoints;p++)
                    {
                        double dd = EndianUtils.ReadDoubleLE(data, indexD + offset);
                        bWriter.Write((float)dd);
                        indexD += 8;
                        dd = EndianUtils.ReadDoubleLE(data, indexD + offset);
                        bWriter.Write((float)dd);
                        indexD += 8;
                    }
                    //write the z data
                    indexD += 16; //skip the minz maxz
                    for (int p = 0; p < numPoints; p++)
                    {
                        double dd = EndianUtils.ReadDoubleLE(data, indexD + offset);
                        bWriter.Write((float)dd);
                        indexD += 8;
                    }
                    //write the M data
                    indexD += 16; //skip the minM maxM
                    for (int p = 0; p < numPoints; p++)
                    {
                        double dd = EndianUtils.ReadDoubleLE(data, indexD + offset);
                        bWriter.Write((float)dd);
                        indexD += 8;
                    }

                    inFileOffset += (8 + pgRecs[n].recordHeader.ContentLength);
                }

                //update the mainheader and re-write
                headEx.FileLength = (int)bWriter.BaseStream.Position;
                bWriter.Seek(0, SeekOrigin.Begin);
                headEx.write(bWriter);

            }
            finally
            {
                bWriter.Close();
            }
        }
				
		
		private static void createShapeFileEx(string shapeFile, ShapeFileMainHeader mainHeader)
		{			
			if (mainHeader.ShapeType != ShapeType.Point) throw new InvalidOperationException("Unexpected ShapeType (must be Point)");

			BinaryWriter bWriter = new BinaryWriter(new FileStream(shapeFile+".shpx",FileMode.Create));
			BinaryReader shapeFileStream = new BinaryReader(new FileStream(shapeFile +".shp",FileMode.Open));
			try
			{
                mainHeader.FileLength = (int)shapeFileStream.BaseStream.Length; //adjust because main header stores length of index file
				//setup the mainheader
				ShapeFileMainHeaderEx headEx = new ShapeFileMainHeaderEx(mainHeader);
				headEx.ShapeType = ShapeType.Point;
				
				headEx.NumRecords = (mainHeader.FileLength - MAIN_HEADER_LENGTH) / 28; //each record fixed length of 28 bytes
				headEx.FileLength = ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH + (headEx.NumRecords*12);
				headEx.DataOffset = ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH;
				headEx.write(bWriter);
								
				//now write the data..
				
				byte[] data = new byte[28]; //length of Point Record + record header
				shapeFileStream.BaseStream.Seek(MAIN_HEADER_LENGTH,SeekOrigin.Begin);
				
				for(int n=0;n<headEx.NumRecords;n++)
				{
					shapeFileStream.Read(data,0,28);
					PointRecordEx prEx;
					prEx.RecordNumber = EndianUtils.ReadIntLE(data,0);
					prEx.pt = new PointF((float)EndianUtils.ReadDoubleLE(data,12),(float)EndianUtils.ReadDoubleLE(data,20));
					prEx.write(bWriter);					
				}
				
			}
			finally
			{
				bWriter.Close();
				shapeFileStream.Close();
			}
		}
		

        /*
         *          THIS METHOD NEEDS TO BE RE-WRITTEN
         *          READING ENTIRE HEADER OF SHAPEFILE IS USING EXCESSIVE MEMEORY
         * */
        
		private SFRecordCol readShapeHeadersEx(string shapeFile)
		{
			SFRecordCol recordCol = null;				
			shapeFileStream = new BinaryReader(new FileStream(shapeFile +".shpx",FileMode.Open, FileAccess.Read));
			//read the main header

			ShapeFileMainHeaderEx mainHeadEx = new ShapeFileMainHeaderEx(shapeFileStream.ReadBytes(ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH));

			if (mainHeadEx.ShapeType == ShapeType.PolyLine)				
			{
				PolyLineRecordEx[] plRecs = new PolyLineRecordEx[mainHeadEx.NumRecords];				
				byte[] data = new byte[mainHeadEx.DataOffset - ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH];
                shapeFileStream.Read(data,0,data.Length);
				int numRecs = plRecs.Length;					
				int headerOff = 0;
                int dataOffset = mainHeadEx.DataOffset;
                int totalPoints = 0;
                int maxRecordLength = 0;
				for(int n=0;n<numRecs;)				
				{
					plRecs[n] = new PolyLineRecordEx();
					headerOff += plRecs[n].read(data,headerOff);
                    plRecs[n].DataOffset = dataOffset;
                    totalPoints += plRecs[n].NumPoints;
                    dataOffset += plRecs[n++].NumPoints << 3;

                    maxRecordLength = Math.Max(maxRecordLength, dataOffset - plRecs[n - 1].DataOffset);
                    
				}
                SFRecordCol.EnsureBufferSize(maxRecordLength);
				data = null;	
				System.GC.Collect();
				recordCol = new SFPolyLineExCol(plRecs,mainHeadEx);
                //System.Diagnostics.Debug.WriteLine("Total points in shapefile : " + totalPoints);
			}
			else if (mainHeadEx.ShapeType == ShapeType.PolyLineM)				
			{
				PolyLineMRecordEx[] plmRecs = new PolyLineMRecordEx[mainHeadEx.NumRecords];				
							
				byte[] data = new byte[mainHeadEx.DataOffset - ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH];
				shapeFileStream.Read(data,0,data.Length);
				int numRecs = plmRecs.Length;					
				int headerOff = 0;
                int dataOffset = mainHeadEx.DataOffset;
                int maxRecordLength = 0;
				for(int n=0;n<numRecs;)				
				{
					plmRecs[n] = new PolyLineMRecordEx();
					headerOff += plmRecs[n++].read(data,headerOff);
                    maxRecordLength = Math.Max(maxRecordLength, plmRecs[n - 1].NumPoints << 3);
				}
                SFRecordCol.EnsureBufferSize(maxRecordLength);
				data = null;	
				System.GC.Collect();
				recordCol = new SFPolyLineMExCol(plmRecs,mainHeadEx);
			}
			else if (mainHeadEx.ShapeType == ShapeType.Point)				
			{
				PointRecordEx[] pRecs = new PointRecordEx[mainHeadEx.NumRecords];				
							
				byte[] data = new byte[mainHeadEx.FileLength - ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH];
				shapeFileStream.Read(data,0,data.Length);
				int numRecs = pRecs.Length;					
				int recOff = 0;
				for(int n=0;n<numRecs;)				
				{
					pRecs[n++] = new PointRecordEx(data,recOff);
					recOff += 12;						
				}		
				data = null;	
				System.GC.Collect();
				recordCol = new SFPointExCol(pRecs,mainHeadEx);
			}
			else if (mainHeadEx.ShapeType == ShapeType.Polygon)				
			{
				PolygonRecordEx[] pgRecs = new PolygonRecordEx[mainHeadEx.NumRecords];				
							
				byte[] data = new byte[mainHeadEx.DataOffset - ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH];
				shapeFileStream.Read(data,0,data.Length);
				int numRecs = pgRecs.Length;					
				int headerOff = 0;
                int dataOffset = mainHeadEx.DataOffset;
                int maxRecordLength=0;
				for(int n=0;n<numRecs;)				
				{
					pgRecs[n] = new PolygonRecordEx();
					headerOff += pgRecs[n].read(data,headerOff);
                    pgRecs[n].DataOffset = dataOffset;
                    dataOffset += pgRecs[n++].NumPoints <<3;
                    maxRecordLength = Math.Max(maxRecordLength, pgRecs[n - 1].NumPoints << 3);
				}
                SFRecordCol.EnsureBufferSize(maxRecordLength);
				data = null;	
				System.GC.Collect();
				recordCol = new SFPolygonExCol(pgRecs,mainHeadEx);
			}
            else if (mainHeadEx.ShapeType == ShapeType.PolygonZ)
            {
                PolygonZRecordEx[] pgRecs = new PolygonZRecordEx[mainHeadEx.NumRecords];

                byte[] data = new byte[mainHeadEx.DataOffset - ShapeFileExConstants.SHAPE_FILE_EX_MAIN_HEADER_LENGTH];
                shapeFileStream.Read(data, 0, data.Length);
                int numRecs = pgRecs.Length;
                int headerOff = 0;
                int dataOffset = mainHeadEx.DataOffset;
                int maxRecordLength = 0;
                for (int n = 0; n < numRecs; )
                {
                    pgRecs[n] = new PolygonZRecordEx();
                    headerOff += pgRecs[n].read(data, headerOff);
                    pgRecs[n].DataOffset = dataOffset;
                    dataOffset += pgRecs[n++].NumPoints << 4;
                    maxRecordLength = Math.Max(maxRecordLength, pgRecs[n - 1].NumPoints << 4);
                }
                SFRecordCol.EnsureBufferSize(maxRecordLength);
                data = null;
                System.GC.Collect();
                recordCol = new SFPolygonZExCol(pgRecs, mainHeadEx);
            }

			
			return recordCol;

		}
		
		private PolyLineMRecord[] readPLMShapeHeaders(string shapeFile, ShapeFileMainHeader mainHeader, RecordHeader[] recordHeaders)
		{
			PolyLineMRecord[] plmRecs = null;
            if (mainHeader.ShapeType != ShapeType.PolyLineM) throw new InvalidOperationException("ShapeType must be PolyLineM");
			
			plmRecs = new PolyLineMRecord[recordHeaders.Length];
			shapeFileStream = new BinaryReader(new FileStream(shapeFile +".shp",FileMode.Open, FileAccess.Read));

            byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
			int numRecs = plmRecs.Length;
            int readOffset = MAIN_HEADER_LENGTH;

			//skip the first 100 bytes (main header)
			shapeFileStream.Read(data,0,MAIN_HEADER_LENGTH);

			int n=0;
			while(n<numRecs)
			{
                //check if the current readOffset matchs the current record offset
                //we need to do this because a record length may not equal rec[n].offset-rec[n-1].offset
                if (n > 0 && readOffset != recordHeaders[n].Offset)
                {
                    shapeFileStream.Read(data, 0, recordHeaders[n].Offset - readOffset);
                    readOffset = recordHeaders[n].Offset;
                }	
				//read record header + content length(8 bytes)
                int len = recordHeaders[n].ContentLength + 8;
				shapeFileStream.Read(data,0,len);
				plmRecs[n] = new PolyLineMRecord(recordHeaders[n++], data, 8);
                readOffset += len;                                
			}

			return plmRecs;

		}

		private PolyLineRecord[] readPLShapeHeaders(string shapeFile, ShapeFileMainHeader mainHeader, RecordHeader[] recordHeaders)
		{
            if (mainHeader.ShapeType != ShapeType.PolyLine) throw new InvalidOperationException("ShapeType must be PolyLine");				
			PolyLineRecord[] plRecs = new PolyLineRecord[recordHeaders.Length];				
			shapeFileStream = new BinaryReader(new FileStream(shapeFile +".shp",FileMode.Open, FileAccess.Read));				
			//skip the first 100 bytes (main header)
			shapeFileStream.ReadBytes(MAIN_HEADER_LENGTH);
            byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
            int readOffset = MAIN_HEADER_LENGTH;
			int numRecs = plRecs.Length;					
			for(int n=0;n<numRecs;n++)
			{
                //check if the current readOffset matchs the current record offset
                //we need to do this because a record length may not equal rec[n].offset-rec[n-1].offset
                if (n > 0 && readOffset != recordHeaders[n].Offset)
                {
                    shapeFileStream.Read(data, 0, recordHeaders[n].Offset - readOffset);
                    readOffset = recordHeaders[n].Offset;
                }

                //skip the record header (avoid using seek as more efficient to just read bytes)
                int len = recordHeaders[n].ContentLength + 8;
                shapeFileStream.Read(data, 0, len);                				
				plRecs[n] = new PolyLineRecord(recordHeaders[n]);
				plRecs[n].read(data,8);
                readOffset += len;                                
			}
				
			return plRecs;
		}

		private PolygonRecord[] readPGShapeHeaders(string shapeFile, ShapeFileMainHeader mainHeader, RecordHeader[] recordHeaders)
		{
            if (mainHeader.ShapeType != ShapeType.Polygon) throw new System.InvalidOperationException("ShapeType must be Polygon");
				
			PolygonRecord[] pgRecs = new PolygonRecord[recordHeaders.Length];				
			shapeFileStream = new BinaryReader(new FileStream(shapeFile +".shp",FileMode.Open, FileAccess.Read));
				
			//skip the first 100 bytes (main header)
			shapeFileStream.ReadBytes(MAIN_HEADER_LENGTH);

            byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
            int readOffset = MAIN_HEADER_LENGTH;
			int numRecs = pgRecs.Length;					
			for(int n=0;n<numRecs;n++)
			{                
                //check if the current readOffset matchs the current record offset
                //we need to do this because a record length may not equal rec[n].offset-rec[n-1].offset
                if (n > 0 && readOffset != recordHeaders[n].Offset)
                {
                    shapeFileStream.Read(data,0,recordHeaders[n].Offset-readOffset);
                    readOffset = recordHeaders[n].Offset;
                }

				//skip the record header (avoid using seek as more efficient to just read bytes)
                int len = recordHeaders[n].ContentLength+8;
                if (len > data.Length) data = new byte[len];
                shapeFileStream.Read(data,0,len);                
				pgRecs[n] = new PolygonRecord(recordHeaders[n]);                
				pgRecs[n].read(data,8);
                readOffset += len;                                
			}
				
			return pgRecs;
        }

        private PolygonZRecord[] readPGZShapeHeaders(string shapeFile, ShapeFileMainHeader mainHeader, RecordHeader[] recordHeaders)
        {
            if (mainHeader.ShapeType != ShapeType.PolygonZ) throw new System.InvalidOperationException("ShapeType must be PolygonZ");

            PolygonZRecord[] pgRecs = new PolygonZRecord[recordHeaders.Length];
            shapeFileStream = new BinaryReader(new FileStream(shapeFile + ".shp", FileMode.Open, FileAccess.Read));

            //skip the first 100 bytes (main header)
            shapeFileStream.ReadBytes(MAIN_HEADER_LENGTH);

            byte[] data = SFRecordCol.SharedBuffer;// new byte[ShapeFileExConstants.MAX_REC_LENGTH];
            int readOffset = MAIN_HEADER_LENGTH;
            int numRecs = pgRecs.Length;
            for (int n = 0; n < numRecs; n++)
            {
                //check if the current readOffset matches the current record offset
                //we need to do this because a record length may not equal rec[n].offset-rec[n-1].offset
                if (n > 0 && readOffset != recordHeaders[n].Offset)
                {
                    shapeFileStream.Read(data, 0, recordHeaders[n].Offset - readOffset);
                    readOffset = recordHeaders[n].Offset;
                }

                //skip the record header (avoid using seek as more efficient to just read bytes)
                int len = recordHeaders[n].ContentLength + 8;
                if (len > data.Length) data = new byte[len];
                shapeFileStream.Read(data, 0, len);
                pgRecs[n] = new PolygonZRecord(recordHeaders[n]);
                pgRecs[n].read(data, 8);
                readOffset += len;
            }

            return pgRecs;
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
        public ShapeFileEnumerator GetShapeFileEnumerator(RectangleF extents)
        {
            return new ShapeFileEnumerator(this, extents);
        }

        /// <summary>
        /// Gets a ShapeFileEnumerator, which can be used to enumerate over all of the records in the ShapeFile, intersecting or contained within a given bounds
        /// </summary>
        /// <returns></returns>        
        /// <param name="extent">Defines the area that returned shapes must intersect with or be contained by. If this is RectangleF.Empty then no recrods will be returned</param>        
        /// /// <param name="intersectionType">Indicates whether shapes must intersect with or be contained by extent</param>        
        public ShapeFileEnumerator GetShapeFileEnumerator(RectangleF extent, ShapeFileEnumerator.IntersectionType intersectionType)
        {
            return new ShapeFileEnumerator(this, extent, intersectionType);
        }



        #endregion


        /// <summary>
        /// Gets/Sets whether ShapeFiles should be rendered using the Mercator Projection.
        /// </summary>
        public static bool UseMercatorProjection
        {
            get
            {
                return SFRecordCol.MercProj;
            }
            set
            {
                SFRecordCol.MercProj = value;
            }
        }

        private const float MaxLLMercProjF = 85.0511287798066f;
        /// <summary>
        /// Utillity method to translate a Lat/Long Point to its Mercator Projection representation.
        /// </summary>
        public static PointF LLToMercator(PointF pt)
        {
            if (pt.Y > MaxLLMercProjF)
            {
                pt.Y = MaxLLMercProjF;
            }
            else if (pt.Y < -MaxLLMercProjF)
            {
                pt.Y = -MaxLLMercProjF;
            }
            double d = (Math.PI / 180) * pt.Y;
            double sd = Math.Sin(d);
            d = (90 / Math.PI) * Math.Log((1 + sd) / (1 - sd));
            return new PointF(pt.X, (float)d);            
        }        

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
        public static PointF MercatorToLLF(PointF pt)
        {            
            double d = (Math.PI / 180) * pt.Y;
            d = Math.Atan(Math.Sinh(d));
            d = d * (180 / Math.PI);
            return new PointF(pt.X, (float)d);            
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
		public int ContentLength;
		public int Offset;

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

	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]
	struct PointRecord
	{
		public RecordHeader recordHeader;
	
		public PointRecord(RecordHeader recHeader)
		{
			recordHeader = recHeader;			
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

	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct PolyLineRecord
	{
		public RecordHeader recordHeader;
		public Box bounds;
		public int NumParts;
		public int NumPoints;
		public int[] PartOffsets;

		public PolyLineRecord(RecordHeader recHeader)
		{
			recordHeader = recHeader;
			NumParts = 0;
			NumPoints=0;
			PartOffsets = null;
			
			bounds = new Box(0,0,0,0);
		}

		public PolyLineRecord(RecordHeader recHeader, byte[] data, int dataOffset)
		{
			recordHeader = recHeader;
			bounds = new Box(data, dataOffset+4);
			NumParts = EndianUtils.ReadIntLE(data,dataOffset+36);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+40);

			PartOffsets = new int[NumParts];
			int partOff = 44;
			for(int n=0;n<NumParts;n++)
			{
				PartOffsets[n] = EndianUtils.ReadIntLE(data,dataOffset+partOff);				
				partOff+=4;
			}
		}

		public void read(byte[] data, int dataOffset)
		{			
			bounds = new Box(data, dataOffset+4);
			NumParts = EndianUtils.ReadIntLE(data,dataOffset+36);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+40);            
            PartOffsets = new int[NumParts];
            int partOff = 44;
            for (int n = 0; n < NumParts; n++)
            {
                PartOffsets[n] = EndianUtils.ReadIntLE(data, dataOffset + partOff);
                partOff += 4;
            }            
		}
	}

	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct PolyLineMRecord
	{
		public RecordHeader recordHeader;
		public Box bounds;
		public int NumParts;
		public int NumPoints;
		public int[] PartOffsets;
		
		//measures
		public double mMin;
		public double mMax;

        //public PolyLineMRecord(RecordHeader recHeader)
        //{
        //    recordHeader = recHeader;
        //    NumParts = 0;
        //    NumPoints=0;
        //    PartOffsets = null;

        //    mMin = -1.0E40;
        //    mMax = -1.0E40;
			
        //    bounds = new Box(0,0,0,0);
        //}

		public PolyLineMRecord(RecordHeader recHeader, byte[] data, int dataOffset)
		{
			recordHeader = recHeader;
			bounds = new Box(data, dataOffset+4);
			NumParts = EndianUtils.ReadIntLE(data,dataOffset+36);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+40);

			PartOffsets = new int[NumParts];
			int partOff = 44;
			for(int n=0;n<NumParts;n++)
			{
				PartOffsets[n] = EndianUtils.ReadIntLE(data,dataOffset + partOff);				
				partOff+=4;
			}
			partOff += (NumPoints<<4); //skip the Point data
			
			//read the measures min/max values
			mMin = EndianUtils.ReadDoubleLE(data, dataOffset + partOff);
			mMax = EndianUtils.ReadDoubleLE(data, dataOffset + partOff + 8);

		}

        //public void read(byte[] data, int dataOffset)
        //{
        //    bounds = new Box(data, dataOffset+4);
        //    NumParts = EndianUtils.ReadIntLE(data,dataOffset+36);
        //    NumPoints = EndianUtils.ReadIntLE(data,dataOffset+40);
	
        //    PartOffsets = new int[NumParts];
        //    int partOff=44;			
        //    for(int n=0;n<NumParts;n++)
        //    {
        //        PartOffsets[n] = EndianUtils.ReadIntLE(data,dataOffset+partOff);	
        //        partOff+=4;
        //    }
        //    partOff += (NumPoints<<4); //skip the Point data
			
        //    //read the measures min/max values
        //    mMin = EndianUtils.ReadDoubleLE(data, dataOffset + partOff);
        //    mMax = EndianUtils.ReadDoubleLE(data, dataOffset + partOff + 8);

        //}

		public override string ToString()
		{
			return "bounds = " + bounds.ToString();
		}

	}

	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct PolygonRecord
	{
		public RecordHeader recordHeader;
		public Box bounds;
		public int NumParts;
		public int NumPoints;
		public int[] PartOffsets;

		public PolygonRecord(RecordHeader recHeader)
		{
			recordHeader = recHeader;
			NumParts = 0;
			NumPoints=0;
			PartOffsets = null;
			
			bounds = new Box(0,0,0,0);
		}

		public void read(byte[] data, int dataOffset)
		{
            ShapeType st = (ShapeType)EndianUtils.ReadIntLE(data, dataOffset);            
            if (st == ShapeType.Polygon)
            {
                bounds = new Box(data, dataOffset + 4);
                NumParts = EndianUtils.ReadIntLE(data, dataOffset + 36);
                NumPoints = EndianUtils.ReadIntLE(data, dataOffset + 40);
                
                PartOffsets = new int[NumParts];
                int partOff = 44;
                for (int n = 0; n < NumParts; n++)
                {
                    PartOffsets[n] = EndianUtils.ReadIntLE(data, dataOffset + partOff);
                    partOff += 4;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("null or unknown shapetype");
            }
		}
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolygonZRecord
    {
        public RecordHeader recordHeader;
        public Box bounds;
        public int NumParts;
        public int NumPoints;
        public int[] PartOffsets;
        public double MinZ;
        public double MaxZ;
        public double MinM;
        public double MaxM;

        public PolygonZRecord(RecordHeader recHeader)
        {
            recordHeader = recHeader;
            NumParts = 0;
            NumPoints = 0;
            PartOffsets = null;
            MinZ = 0;
            MaxZ = 0;
            MinM = 0;
            MaxM = 0;
            bounds = new Box(0, 0, 0, 0);
        }

        public void read(byte[] data, int dataOffset)
        {
            ShapeType st = (ShapeType)EndianUtils.ReadIntLE(data, dataOffset);
            if (st == ShapeType.PolygonZ)
            {
                bounds = new Box(data, dataOffset + 4);
                NumParts = EndianUtils.ReadIntLE(data, dataOffset + 36);
                NumPoints = EndianUtils.ReadIntLE(data, dataOffset + 40);

                PartOffsets = new int[NumParts];
                int partOff = 44;
                for (int n = 0; n < NumParts; n++)
                {
                    PartOffsets[n] = EndianUtils.ReadIntLE(data, dataOffset + partOff);
                    partOff += 4;
                }
                partOff += (16 * NumPoints); //skip the point data
                MinZ = EndianUtils.ReadDoubleLE(data, dataOffset + partOff);
                partOff += 8;
                MaxZ = EndianUtils.ReadDoubleLE(data, dataOffset + partOff);
                partOff += 8;
                if (partOff >= this.recordHeader.ContentLength)
                {
                    Console.Out.WriteLine("warning: shape does not appear to be using measures");
                }
                partOff += (8 * NumPoints); //skip the z values
                MinM = EndianUtils.ReadDoubleLE(data, dataOffset + partOff);
                partOff += 8;
                MaxM = EndianUtils.ReadDoubleLE(data, dataOffset + partOff);
                partOff += 8;
                //Console.Out.WriteLine(string.Format("partOff : {0}, contentLength:{1}", partOff, recordHeader.ContentLength));
                
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("null or unknown shapetype");
            }
        }
    }


    #endregion


    #region "ShapeFileEx structs and classes"

   
	internal sealed class ShapeFileExConstants
	{
		public const int SHAPE_FILE_EX_MAIN_HEADER_LENGTH = 76;

		//public const int MAX_REC_LENGTH = 1<<23;//1<<20;
        public const int MAX_REC_LENGTH = 1 << 22;//1<<20;
        //public const int MAX_REC_LENGTH = 1 << 20;

        private ShapeFileExConstants()
        {
        }
	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct ShapeFileMainHeaderEx
	{
		public int FileCode;	//99994 (standard shapefiles use 9994)
		public int UnusedByte1;
		public int UnusedByte2;
		public int UnusedByte3;
		public int UnusedByte4;
		public int UnusedByte5;
		public int FileLength;	//length of the entire file in bytes (standard shapefile uses 16 bit words as length)
		public int DataOffset; //offset of first byte of data
		public int Version;		//1000 (same as a standard shapefile)
		public ShapeType ShapeType; 
		//all the bounding box values have been changed from double (8 bytes) to float (4 bytes)
		public int NumRecords;
		public float Xmin;
		public float Ymin;
		public float Xmax;
		public float Ymax;
		public float Zmin;
		public float Zmax;
		public float Mmin;
		public float Mmax;	

		public ShapeFileMainHeaderEx(ShapeFileMainHeader head)
		{
			FileCode = 99994;
			UnusedByte1 = head.UnusedByte1;
			UnusedByte2 = head.UnusedByte2;
			UnusedByte3 = head.UnusedByte3;
			UnusedByte4 = head.UnusedByte4;
			UnusedByte5 = head.UnusedByte5;
			FileLength = head.FileLength;
			DataOffset = 0;
			Version = head.Version;
			ShapeType = head.ShapeType;
			NumRecords = 0;
			Xmin = (float)head.Xmin;
			Ymin = (float)head.Ymin;
			Xmax = (float)head.Xmax;
			Ymax = (float)head.Ymax;
			Zmin = (float)head.Zmin;
			Zmax = (float)head.Zmax;
			Mmin = (float)head.Mmin;
			Mmax = (float)head.Mmax;
		}
		public ShapeFileMainHeaderEx(byte[] data)
		{							
			fixed(byte* bPtr = data)
			{
				//now cast and dereference the pointer
				this = *(ShapeFileMainHeaderEx*)bPtr;
			}
		}

		public override string ToString()
		{
			string str = "Filecode = " + FileCode + ", FileLength = " + FileLength + ", DataOffset = " + DataOffset + ", Version = " + Version + ", ShapeType = " + ShapeType;
			str += ", NumRecords = " + NumRecords + ", XMin = " + Xmin + ", Ymin = " + Ymin + ", Xmax = " + Xmax + ", Ymax = " + Ymax;
			return str;
		}

		public void write(System.IO.BinaryWriter bWriter)
		{
			bWriter.Write(FileCode);
			bWriter.Write(UnusedByte1);
			bWriter.Write(UnusedByte2);
			bWriter.Write(UnusedByte3);
			bWriter.Write(UnusedByte4);
			bWriter.Write(UnusedByte5);
			bWriter.Write(FileLength);
			bWriter.Write(DataOffset);			
			bWriter.Write(Version);
			bWriter.Write((int)ShapeType);
			bWriter.Write(NumRecords);
			bWriter.Write(Xmin);
			bWriter.Write(Ymin);
			bWriter.Write(Xmax);
			bWriter.Write(Ymax);
			bWriter.Write(Zmin);
			bWriter.Write(Zmax);
			bWriter.Write(Mmin);
			bWriter.Write(Mmax);
		}

		


	}

	
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	struct PointRecordEx
	{
		public int RecordNumber;
		public PointF pt;

		public unsafe PointRecordEx(byte[] data, int dataOffset)
		{
			fixed(byte* bPtr = data)
			{
				this = *(PointRecordEx*)(bPtr+dataOffset);
			}		
		}				

		public void write(BinaryWriter bWriter)
		{
			bWriter.Write(RecordNumber);
			bWriter.Write(pt.X);
			bWriter.Write(pt.Y);			
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct PolyLineRecordEx
	{
		public int RecordNumber;
		public RectangleF Bounds;
		public int NumParts;
		public int NumPoints;
		public int[] PartOffsets;
        
        public int DataOffset;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="dataOffset"></param>
		/// <returns>number of bytes read</returns>
		public int read(byte[] data, int dataOffset)
		{
			RecordNumber = EndianUtils.ReadIntLE(data,dataOffset);
			fixed(byte* bPtr = data)
			{				
				Bounds = *(RectangleF*)(bPtr+4 + dataOffset);								
			}

			NumParts = EndianUtils.ReadIntLE(data,dataOffset+20);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+24);

			PartOffsets = new int[NumParts];
			int partOff = 28;
			for(int n=0;n<NumParts;n++)
			{
				PartOffsets[n] = EndianUtils.ReadIntLE(data,dataOffset+partOff);				
				partOff+=4;
			}
            DataOffset = -1;
			return partOff;
		}

		public void write(BinaryWriter bWriter)
		{
			bWriter.Write(RecordNumber);

			bWriter.Write(Bounds.Left);
			bWriter.Write(Bounds.Top);
			bWriter.Write(Bounds.Width);
			bWriter.Write(Bounds.Height);
				
			bWriter.Write(NumParts);
			bWriter.Write(NumPoints);
			for(int n=0;n<NumParts;n++)
			{
				bWriter.Write(PartOffsets[n]);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct PolyLineMRecordEx
	{
		public int RecordNumber;
		public RectangleF Bounds;
		public int NumParts;
		public int NumPoints;
		public int[] PartOffsets;
		public float Mmin;
		public float Mmax;
		

		public PolyLineMRecordEx(PolyLineMRecord plmRec)
		{
			Bounds = RectangleF.FromLTRB((float)plmRec.bounds.xmin,(float)plmRec.bounds.ymin,(float)plmRec.bounds.xmax,(float)plmRec.bounds.ymax);
			RecordNumber = plmRec.recordHeader.RecordNumber;
			NumParts = plmRec.NumParts;
			NumPoints = plmRec.NumPoints;
			PartOffsets = new int[NumParts];
			for(int p=0;p<NumParts;p++)
			{
				PartOffsets[p] = plmRec.PartOffsets[p];
			}
			Mmin = (float)plmRec.mMin;
			Mmax = (float)plmRec.mMax;
				
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="dataOffset"></param>
		/// <returns>number of bytes read</returns>
		public int read(byte[] data, int dataOffset)
		{
			RecordNumber = EndianUtils.ReadIntLE(data,dataOffset);
			fixed(byte* bPtr = data)
			{				
				Bounds = *(RectangleF*)(bPtr+4 + dataOffset);								
			}

			NumParts = EndianUtils.ReadIntLE(data,dataOffset+20);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+24);

			PartOffsets = new int[NumParts];
			int partOff = 28;
			for(int n=0;n<NumParts;n++)
			{
				PartOffsets[n] = EndianUtils.ReadIntLE(data,dataOffset+partOff);				
				partOff+=4;
			}
			Mmin = EndianUtils.ReadFloatLE(data,dataOffset+partOff);
			partOff+=4;
			Mmax = EndianUtils.ReadFloatLE(data,dataOffset+partOff);
			partOff+=4;
			return partOff;
		}



		public void write(BinaryWriter bWriter)
		{
			bWriter.Write(RecordNumber);

			bWriter.Write(Bounds.Left);
			bWriter.Write(Bounds.Top);
			bWriter.Write(Bounds.Width);
			bWriter.Write(Bounds.Height);
				
			bWriter.Write(NumParts);
			bWriter.Write(NumPoints);
			for(int n=0;n<NumParts;n++)
			{
				bWriter.Write(PartOffsets[n]);
			}
			bWriter.Write(Mmin);
			bWriter.Write(Mmax);

		}
	}

	[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct PolygonRecordEx
	{
		public int RecordNumber;
		public RectangleF Bounds;
		public int NumParts;
		public int NumPoints;
		public int[] PartOffsets;

        public int DataOffset;
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="dataOffset"></param>
		/// <returns>number of bytes read</returns>
		public int read(byte[] data, int dataOffset)
		{
			RecordNumber = EndianUtils.ReadIntLE(data,dataOffset);
			fixed(byte* bPtr = data)
			{				
				Bounds = *(RectangleF*)(bPtr+4 + dataOffset);								
			}

			NumParts = EndianUtils.ReadIntLE(data,dataOffset+20);
			NumPoints = EndianUtils.ReadIntLE(data,dataOffset+24);

			PartOffsets = new int[NumParts];
			int partOff = 28;
			for(int n=0;n<NumParts;n++)
			{
				PartOffsets[n] = EndianUtils.ReadIntLE(data,dataOffset+partOff);				
				partOff+=4;
			}
            DataOffset = -1;
			return partOff;
		}

		public void write(BinaryWriter bWriter)
		{
			bWriter.Write(RecordNumber);

			bWriter.Write(Bounds.Left);
			bWriter.Write(Bounds.Top);
			bWriter.Write(Bounds.Width);
			bWriter.Write(Bounds.Height);
				
			bWriter.Write(NumParts);
			bWriter.Write(NumPoints);
			for(int n=0;n<NumParts;n++)
			{
				bWriter.Write(PartOffsets[n]);
			}
		}
	}


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct PolygonZRecordEx
    {
        public int RecordNumber;
        public RectangleF Bounds;
        public int NumParts;
        public int NumPoints;
        public int[] PartOffsets;
        public float MinZ;
        public float MaxZ;
        public float MinMeasure;
        public float MaxMeasure;

        public int DataOffset;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataOffset"></param>
        /// <returns>number of bytes read</returns>
        public int read(byte[] data, int dataOffset)
        {
            RecordNumber = EndianUtils.ReadIntLE(data, dataOffset);
            fixed (byte* bPtr = data)
            {
                Bounds = *(RectangleF*)(bPtr + 4 + dataOffset);
            }

            NumParts = EndianUtils.ReadIntLE(data, dataOffset + 20);
            NumPoints = EndianUtils.ReadIntLE(data, dataOffset + 24);

            PartOffsets = new int[NumParts];
            int partOff = 28;
            for (int n = 0; n < NumParts; n++)
            {
                PartOffsets[n] = EndianUtils.ReadIntLE(data, dataOffset + partOff);
                partOff += 4;
            }

            MinZ = EndianUtils.ReadFloatLE(data, dataOffset + partOff);
            partOff+=4;
            MaxZ = EndianUtils.ReadFloatLE(data, dataOffset + partOff);
            partOff+=4;
            MinMeasure = EndianUtils.ReadFloatLE(data, dataOffset + partOff);
            partOff+=4;
            MaxMeasure = EndianUtils.ReadFloatLE(data, dataOffset + partOff);
            partOff+=4;        

            DataOffset = -1;
            return partOff;
        }

        public void write(BinaryWriter bWriter)
        {
            bWriter.Write(RecordNumber);

            bWriter.Write(Bounds.Left);
            bWriter.Write(Bounds.Top);
            bWriter.Write(Bounds.Width);
            bWriter.Write(Bounds.Height);

            bWriter.Write(NumParts);
            bWriter.Write(NumPoints);
            for (int n = 0; n < NumParts; n++)
            {
                bWriter.Write(PartOffsets[n]);
            }
            bWriter.Write(MinZ);
            bWriter.Write(MaxZ);
            bWriter.Write(MinMeasure);
            bWriter.Write(MaxMeasure);

        }
    }


#endregion

    #region SFShape_Ex types


    abstract class SFRecordCol
	{
        private static byte[] sharedBuffer = new byte[ShapeFileExConstants.MAX_REC_LENGTH];

        private static Point[] sharedPointBuffer = new Point[ShapeFileExConstants.MAX_REC_LENGTH/8];

        internal static byte[] SharedBuffer
        {
            get
            {
                if (SingleThreaded) return sharedBuffer;
                else return new byte[ShapeFileExConstants.MAX_REC_LENGTH];
            }
        }

        internal static Point[] SharedPointBuffer
        {
            get
            {
                if (SingleThreaded) return sharedPointBuffer;
                else return new Point[ShapeFileExConstants.MAX_REC_LENGTH/8];                
            }
        }

        internal static void EnsureBufferSize(int requiredSize)
        {
            if (sharedBuffer.Length < requiredSize)
            {
                sharedBuffer = new byte[requiredSize + 256];
                sharedPointBuffer = new Point[sharedBuffer.Length / 8];
                //System.Diagnostics.Debug.WriteLine("shared buffer resized to : " + requiredSize);
                System.GC.Collect();
            }
        }

        internal static bool SingleThreaded = true;

		public ShapeFileMainHeaderEx MainHeader;

        protected int selectedRecordIndex = -1;

        internal void SetSelectedRecordIndex(int index)
        {
            selectedRecordIndex = index;
        }

        internal int GetSelectedRecordIndex()
        {
            return selectedRecordIndex;
        }

		public SFRecordCol(ShapeFileMainHeaderEx head)
		{
			MainHeader = head;
		}

		public abstract void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream);

		public abstract void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings);

        public abstract List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream);
        public abstract List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer);

        public abstract List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream);
        public abstract List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer);


        protected static bool RenderAllRecordsAtZoomLevel(float zoom, RenderSettings renderSettings)
        {
            if (renderSettings == null) return true;
            return !(zoom < renderSettings.MinZoomLevel || (renderSettings.MaxZoomLevel > 0 && zoom > renderSettings.MaxZoomLevel));                        
        }

        
		/// <summary>
		/// returns the angle of the longest line between consecutive points in the data
		/// </summary>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <param name="numPoints"></param>
		/// <param name="pointIndex"> reference to the index in data containing the first point of the segment</param>
		/// <param name="extent"></param>
		/// <returns></returns>
		protected static unsafe float GetPointsFAngle(byte[] data, int offset, int numPoints, ref int pointIndex, RectangleF extent)
		{
			PointF[] pts = new PointF[numPoints];
			int maxIndex = -1;
			fixed(byte* bPtr = data)
			{
				int ptIndex = 0;
				PointF* pPtr = (PointF*)(bPtr + offset);
				float maxDistSquared=0f;
				
				while(ptIndex < numPoints)
				{			
					pts[ptIndex] = LLToProjection(*(pPtr++));					
					if(ptIndex > 0  && (extent.Contains(pts[ptIndex]) || extent.Contains(pts[ptIndex-1]) ))
					{
						float xdif = pts[ptIndex].X - pts[ptIndex-1].X;
						float ydif = pts[ptIndex].Y - pts[ptIndex-1].Y;
						float temp = xdif*xdif + ydif*ydif;
						if(temp > maxDistSquared )
						{
							maxIndex = ptIndex-1;
							maxDistSquared = temp;
						}
					}
					ptIndex++;
				}
			}			
			pointIndex = maxIndex;
			if(maxIndex >= 0)
			{
				return (float)(Math.Atan2( (double)(pts[maxIndex+1].Y-pts[maxIndex].Y),(double)(pts[maxIndex+1].X - pts[maxIndex].X)) *180f/Math.PI);
			}
			else
			{
				return float.NaN;
			}			 
		}

        
        protected static unsafe List<IndexAnglePair> GetPointsFAngle(byte[] data, int offset, int numPoints, float minDist)
        {
            List<IndexAnglePair> indexAngleList = new List<IndexAnglePair>();
            PointF[] pts = new PointF[numPoints];
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);
                float minDistSquared = minDist*minDist;

                while (ptIndex < numPoints)
                {
                    pts[ptIndex] = LLToProjection(*(pPtr++));
                    if (ptIndex > 0)
                    {
                        float xdif = pts[ptIndex].X - pts[ptIndex - 1].X;
                        float ydif = pts[ptIndex].Y - pts[ptIndex - 1].Y;
                        float temp = xdif * xdif + ydif * ydif;
                        if (temp >= minDistSquared)
                        {
                            float angle = (float)(Math.Atan2((double)(pts[ptIndex].Y - pts[ptIndex-1].Y), (double)(pts[ptIndex].X - pts[ptIndex-1].X)) * 180f / Math.PI);
                            indexAngleList.Add(new IndexAnglePair(angle,ptIndex - 1));                            
                        }
                    }
                    ptIndex++;
                }
            }
            return indexAngleList;
        }
        

        internal static bool MercProj = false;

        private const double MaxLLMercProjD = 85.0511287798066;
        private const float MaxLLMercProjF = 85.0511287798066f;

        internal static PointF LLToProjection(PointF pt)
        {
            if (MercProj)
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
            return pt;
        }

        internal static PointD LLToProjection(PointD pt)
        {
            if (MercProj)
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
            return pt;
        }

        
        //internal static PointF ProjectionToLL(PointF pt)
        //{
        //    if (MercProj)
        //    {
        //        double d = (Math.PI / 180) * pt.Y;
        //        d = Math.Atan(Math.Sinh(d));
        //        d = d * (180 / Math.PI);
        //        return new PointF(pt.X, (float)d);
        //    }
        //    return pt;            
        //}

        internal static PointD ProjectionToLL(PointD pt)
        {
            if (MercProj)
            {
                double d = (Math.PI / 180) * pt.Y;
                d = Math.Atan(Math.Sinh(d));
                d = d * (180 / Math.PI);
                return new PointD(pt.X, d);
            }
            return pt;
        }

        protected static unsafe Point[] GetPoints(byte[] data, int offset, int numPoints, float offX, float offY, float scaleX, float scaleY)
        {
            Point[] pts = new Point[numPoints];
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);
                while (ptIndex < numPoints)
                {
                    PointF ptf = LLToProjection(*(pPtr++));
                    pts[ptIndex].X = (int)Math.Round((ptf.X + offX) * scaleX);
                    pts[ptIndex].Y = (int)Math.Round((ptf.Y + offY) * scaleY);
                    ++ptIndex;
                }
            }
            return pts;
        }
        
        protected static unsafe Point[] GetPoints(byte[] data, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY)
        {
            //if (numPoints > 10) return GetPointsRemoveDuplicates(data, offset, numPoints, offX, offY, scaleX, scaleY);
            Point[] pts = new Point[numPoints];
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);
                while (ptIndex < numPoints)
                {
                    PointD ptd = LLToProjection((PointD)(*(pPtr++)));
                    pts[ptIndex].X = (int)Math.Round((ptd.X + offX) * scaleX);
                    pts[ptIndex].Y = (int)Math.Round((ptd.Y + offY) * scaleY);
                    ++ptIndex;
                }
            }
            return pts;
        }

        protected static unsafe Point[] GetPoints(byte[] data, int offset, int numPoints, float offX, float offY, float scaleX, float scaleY, ref Rectangle pixBounds)
        {
            Point[] pts = new Point[numPoints];
            int left = int.MaxValue;
            int right = int.MinValue;
            int top = int.MaxValue;
            int bottom = int.MinValue;
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);
                while (ptIndex < numPoints)
                {
                    PointF ptf = LLToProjection(*(pPtr++));
                    pts[ptIndex].X = (int)Math.Round((ptf.X + offX) * scaleX);
                    pts[ptIndex].Y = (int)Math.Round((ptf.Y + offY) * scaleY);
                    left = Math.Min(pts[ptIndex].X, left);
                    right = Math.Max(pts[ptIndex].X, right);
                    top = Math.Min(pts[ptIndex].Y, top);
                    bottom = Math.Max(pts[ptIndex].Y, bottom);

                    ++ptIndex;
                }
            }
            pixBounds = Rectangle.FromLTRB(left, top, right, bottom);
            return pts;
        }

        protected static unsafe Point[] GetPoints(byte[] data, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY, ref Rectangle pixBounds)
        {
            Point[] pts = new Point[numPoints];
            int left = int.MaxValue;
            int right = int.MinValue;
            int top = int.MaxValue;
            int bottom = int.MinValue;
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);
                while (ptIndex < numPoints)
                {
                    PointD ptf = LLToProjection((PointD)(*(pPtr++)));
                    pts[ptIndex].X = (int)Math.Round((ptf.X + offX) * scaleX);
                    pts[ptIndex].Y = (int)Math.Round((ptf.Y + offY) * scaleY);
                    left = Math.Min(pts[ptIndex].X, left);
                    right = Math.Max(pts[ptIndex].X, right);
                    top = Math.Min(pts[ptIndex].Y, top);
                    bottom = Math.Max(pts[ptIndex].Y, bottom);

                    ++ptIndex;
                }
            }
            pixBounds = Rectangle.FromLTRB(left, top, right, bottom);
            return pts;
        }

        protected static unsafe void GetPointsRemoveDuplicates(byte[] data, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY, ref Rectangle pixBounds, Point[] pts, ref int usedPoints)
        {
            //Point[] pts = new Point[numPoints];
            int left = int.MaxValue;
            int right = int.MinValue;
            int top = int.MaxValue;
            int bottom = int.MinValue;
            usedPoints = 1;
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);

                PointD ptda = LLToProjection((PointD)(*(pPtr++)));
                pts[ptIndex].X = (int)Math.Round((ptda.X + offX) * scaleX);
                pts[ptIndex].Y = (int)Math.Round((ptda.Y + offY) * scaleY);
                ++ptIndex;
                
                while (ptIndex < numPoints)
                {
                    PointD ptf = LLToProjection((PointD)(*(pPtr++)));
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
                    ++ptIndex;
                }
            }
            pixBounds = Rectangle.FromLTRB(left, top, right, bottom);
            if (usedPoints == 1)
            {
                pts[1] = pts[0];
            }
           // return pts;
        }

        protected static unsafe void GetPointsRemoveDuplicates(byte[] data, int offset, int numPoints, double offX, double offY, double scaleX, double scaleY, Point[] pts, ref int usedPoints)
        {
            //if (numPoints == 2)
            //{
            //    GetPointsRemoveDuplicates2p(data, offset, numPoints, offX, offY, scaleX, scaleY, pts, ref usedPoints);
            //    return;
            //}
            int ptIndex = 0;
            usedPoints = 1;
            fixed (byte* bPtr = data)
            {                
                PointF* pPtr = (PointF*)(bPtr + offset);
                PointD ptda = LLToProjection((PointD)(*(pPtr++)));
                pts[ptIndex].X = (int)Math.Round((ptda.X + offX) * scaleX);
                pts[ptIndex].Y = (int)Math.Round((ptda.Y + offY) * scaleY);
                ++ptIndex;

                while (ptIndex < numPoints)
                {
                    PointD ptd = LLToProjection((PointD)(*(pPtr++)));
                    pts[usedPoints].X = (int)Math.Round((ptd.X + offX) * scaleX);
                    pts[usedPoints].Y = (int)Math.Round((ptd.Y + offY) * scaleY);
                    if ((pts[usedPoints].X != pts[usedPoints - 1].X) || (pts[usedPoints].Y != pts[usedPoints - 1].Y))
                    {
                        ++usedPoints;
                    }
                    ++ptIndex;
                }
            }
            if (usedPoints == 1)
            {
              pts[1] = pts[0];
            }
            //return pts;
        }
                
        protected static unsafe PointF[] GetPointsF(byte[] data, int offset, int numPoints)
        {
            PointF[] pts = new PointF[numPoints];
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                PointF* pPtr = (PointF*)(bPtr + offset);
                while (ptIndex < numPoints)
                {
                    pts[ptIndex++] = *(pPtr++);                    
                }
            }
            return pts;
        }

        protected static unsafe float[] GetFloatData(byte[] data, int offset, int numFloats)
        {
            float[] floatData = new float[numFloats];
            fixed (byte* bPtr = data)
            {
                int ptIndex = 0;
                float* fPtr = (float*)(bPtr + offset);
                while (ptIndex < numFloats)
                {
                    floatData[ptIndex++] = *(fPtr++);
                }
            }
            return floatData;
        }

        internal static int AutoHighThreshold = 1024 * 1024 * 1;
        internal static int AutoHighThresholdPoint = 1024 * 150;

        
        protected bool UseGDI(RectangleF visibleExtent)
        {
            if (ShapeFile.RenderQuality == RenderQuality.Low) return true;
            else if (ShapeFile.RenderQuality == RenderQuality.High) return false;
            else
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
                    return t > th;
                }
                
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

    class SFPolyLineExCol : SFRecordCol, QTNodeHelper
	{        
		public PolyLineRecordEx[] Recs;

        
		public SFPolyLineExCol(PolyLineRecordEx[] recs,ShapeFileMainHeaderEx head): base(head)
		{
			this.Recs = recs;
		}

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer/*new byte[ShapeFileExConstants.MAX_REC_LENGTH]*/);            
        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            byte[] data = dataBuffer;
            PolyLineRecordEx nextRec = Recs[recordIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
            int numParts = nextRec.NumParts;
            List<PointF[]> dataList = new List<PointF[]>();
            PointF[] pts;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                pts = GetPointsF(data, nextRec.PartOffsets[partNum] << 3, numPoints);
                dataList.Add(pts);
            }
            data = null;
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }


		public override void paint(Graphics g, Size clientArea, RectangleD extent,Stream shapeFileStream)
		{
			paint(g, clientArea, extent, shapeFileStream, null);
		}

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderer)
        {
            float zoom = (float)(clientArea.Width / extent.Width);

            if (SFRecordCol.RenderAllRecordsAtZoomLevel(zoom, renderer))
            {
                paintAllRecords(g, clientArea, extent, shapeFileStream, renderer);
            }
            
        }

        private void paintAllRecords(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings)
		{
            bool UseGDI = this.UseGDI(extent);
            
			IntPtr dc = IntPtr.Zero;
			IntPtr gdiPen = IntPtr.Zero;
			IntPtr oldGdiObj = IntPtr.Zero;
            Pen gdiplusPen = null;
            Pen rwPen = null;
            bool renderRailway = false;

            Pen selectPen = null;

			double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
			            
			RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;
            
            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }
            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null && customRenderSettings != null);
            
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >=0 && (renderSettings.MinRenderLabelZoom <0 || scaleX > renderSettings.MinRenderLabelZoom));
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
            if (UseGDI)
            {
                dc = g.GetHdc();
            }
            else
            {                
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;                              
            }
            try
            {
                int maxPaintCount = 2;
                if ((renderSettings.LineType == LineType.Solid )|| (Math.Round(renderPenWidth) < 3))
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

                        if (renderSettings != null)
                        {
                            color = renderSettings.OutlineColor.B & 0xff;
                            color = (color << 8) | (renderSettings.OutlineColor.G & 0xff);
                            color = (color << 8) | (renderSettings.OutlineColor.R & 0xff);
                        }

                        if (paintCount ==1)
                        {
                            currentPenColor = renderSettings.FillColor;
                            color = 0x202020;
                            if (renderSettings != null)
                            {
                                color = renderSettings.FillColor.B & 0xff;
                                color = (color << 8) | (renderSettings.FillColor.G & 0xff);
                                color = (color << 8) | (renderSettings.FillColor.R & 0xff);
                            }
                            penWidth -=2;
                        }                        
                        
                        if (UseGDI)
                        {
                            gdiPen = NativeMethods.CreatePen(0, penWidth, color);                            
                            oldGdiObj = NativeMethods.SelectObject(dc, gdiPen);
                        }
                        else
                        {
                            if (paintCount == 0)
                            {
                                gdiplusPen = new Pen(renderSettings.OutlineColor, penWidth);
                                //System.Drawing.Drawing2D.HatchBrush hb = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.Vertical, renderer.OutlineColor, Color.FromArgb(10, Color.White));                                
                                //gdiplusPen = new Pen(hb, penWidth);

                            }
                            else if (paintCount == 1)
                            {
                                gdiplusPen = new Pen(renderSettings.FillColor, penWidth);
                                if (paintCount == 1)
                                {
                                    selectPen = new Pen(Color.LightPink, penWidth);
                                }
                                if (penWidth > 1)
                                {
                                    //hatchBrush = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.Vertical, renderer.OutlineColor, Color.FromArgb(10, Color.White));
                                    //hatchPen = new Pen(hatchBrush, penWidth + 4);
                                    renderRailway = (renderSettings.LineType == LineType.Railway);
                                    if (renderRailway)
                                    {
                                        rwPen = new Pen(renderSettings.OutlineColor, 1);
                                    }
                                }
                            }                            
                            gdiplusPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            gdiplusPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;                            
                        }

                        //shapeFileStream.BaseStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);
                        shapeFileStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);                        
                        PolyLineRecordEx[] plmRecs = Recs;
                        int index = 0;
                        byte[] data = SFRecordCol.SharedBuffer;;
                        Point[] sharedPoints = SFRecordCol.SharedPointBuffer; 
                        
                        float minLabelLength = (float)(6/scaleX);
                        while (index < plmRecs.Length)
                        {
                            PolyLineRecordEx nextRec = plmRecs[index];
                            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
                            if (actualExtent.IntersectsWith(nextRec.Bounds) && (!useCustomRenderSettings || customRenderSettings.RenderShape(index)))
                            {
                                int numParts = nextRec.NumParts;
                                Point[] pts;
                                
                                //if labelling fields then add a renderPtObj
                                if (labelFields && paintCount == 0)
                                {                                                                        
                                    //check what the scaled bounds of this part are
                                    if ((nextRec.Bounds.Width * scaleX > 6) || (nextRec.Bounds.Height * scaleX > 6))
                                    {
                                        if (renderDuplicateFields)
                                        {
                                            List<IndexAnglePair> iapList = GetPointsFAngle(data, 0, nextRec.NumPoints, minLabelLength);
                                            for (int li = iapList.Count - 1; li >= 0; li--)
                                            {
                                                pts = GetPoints(data, iapList[li].PointIndex << 3, 2, offX, offY, scaleX, -scaleX);
                                                float d0 = (float)Math.Sqrt(((pts[1].X - pts[0].X) * (pts[1].X - pts[0].X)) + ((pts[1].Y - pts[0].Y) * (pts[1].Y - pts[0].Y)));
                                                if (Math.Abs(iapList[li].Angle) > 90f && Math.Abs(iapList[li].Angle) <= 270f)
                                                {
                                                    renderPtObjList.Add(new RenderPtObj(pts[1], d0, iapList[li].Angle - 180f, Point.Empty, 0, 0, index));
                                                }
                                                else
                                                {
                                                    renderPtObjList.Add(new RenderPtObj(pts[0], d0, iapList[li].Angle, Point.Empty, 0, 0, index));
                                                }                                                
                                            }
                                        }
                                        else
                                        {
                                            int pointIndex = 0;
                                            float angle = GetPointsFAngle(data, 0, nextRec.NumPoints, ref pointIndex, projectedExtent);
                                            if (!angle.Equals(float.NaN))
                                            {
                                                pts = GetPoints(data, pointIndex << 3, 2, offX, offY, scaleX, -scaleX);
                                                float d0 = (float)Math.Sqrt(((pts[1].X - pts[0].X) * (pts[1].X - pts[0].X)) + ((pts[1].Y - pts[0].Y) * (pts[1].Y - pts[0].Y)));
                                                if (d0 > 6)
                                                {
                                                    if (Math.Abs(angle) > 90f && Math.Abs(angle) <= 270f)
                                                    {
                                                        renderPtObjList.Add(new RenderPtObj(pts[1], d0, angle - 180f, Point.Empty, 0, 0, index));
                                                    }
                                                    else
                                                    {
                                                        renderPtObjList.Add(new RenderPtObj(pts[0], d0, angle, Point.Empty, 0, 0, index));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (useCustomRenderSettings)
                                {
                                    Color customColor = (paintCount == 0) ? customRenderSettings.GetRecordOutlineColor(index) : customRenderSettings.GetRecordFillColor(index);
                                        
                                    if (UseGDI)
                                    {
                                        if (customColor.ToArgb() != currentPenColor.ToArgb())
                                        {
                                            gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, penWidth, ColorToGDIColor(customColor));
                                            NativeMethods.DeleteObject(NativeMethods.SelectObject(dc, gdiPen));
                                            currentPenColor = customColor;
                                        }                                        
                                    }
                                    else
                                    {
                                        if (customColor.ToArgb() != currentPenColor.ToArgb())
                                        {
                                            gdiplusPen = new Pen(customColor, penWidth);
                                            currentPenColor = customColor;
                                            gdiplusPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                                            gdiplusPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                                            gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;                            
                                        }
                                        

                                    }
                                }

                                for (int partNum = 0; partNum < numParts; partNum++)
                                {
                                    int numPoints;
                                    if ((numParts - partNum) > 1)
                                    {
                                        numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                                    }
                                    else
                                    {
                                        numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                                    }
                                    if (numPoints <= 1)
                                    {
                                        Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                        continue;
                                    }
                                    if(UseGDI)
                                    {                                        
                                        int usedPoints = 0;
                                        GetPointsRemoveDuplicates(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, -scaleX, sharedPoints, ref usedPoints);
                                        NativeMethods.DrawPolyline(dc,sharedPoints, usedPoints);
                                    }
                                    else
                                    {                                        
                                        pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY);                                        
                                        if (index == selectedRecordIndex && selectPen != null)
                                        {
                                            g.DrawLines(selectPen, pts);
                                        }
                                        else
                                        {
                                            g.DrawLines(gdiplusPen, pts);
                                        }

                                        if (renderRailway)
                                        {
                                            int th = (int)Math.Ceiling((renderPenWidth + 2)/2);
                                            int tw = (int)Math.Max(Math.Round((7f *th)/7), 5);
                                            int pIndx = 0;
                                            int lx = 0;
                                            while (pIndx < pts.Length - 1)
                                            {
                                                //draw the next line segment
                                                
                                                int dy= pts[pIndx + 1].Y - pts[pIndx].Y;
                                                int dx = pts[pIndx + 1].X - pts[pIndx].X;
                                                float a = (float)(Math.Atan2(dy, dx)*180f/Math.PI);
                                                int d = (int)Math.Round(Math.Sqrt(dy * dy + dx * dx));
                                                if (d > 0)
                                                {
                                                    g.ResetTransform();
                                                    if (Math.Abs(a) > 90f && Math.Abs(a) <= 270f)
                                                    {
                                                        a -= 180f;
                                                        g.TranslateTransform(pts[pIndx + 1].X, pts[pIndx + 1].Y);
                                                        g.RotateTransform(a);
                                                        while(lx < d)
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
                                                        while(lx < d)
                                                        {
                                                            g.DrawLine(rwPen, lx, -th, lx, th);
                                                            lx += tw;
                                                        }
                                                        lx -= d;
                                                    }
                                                }
                                                                                                
                                                pIndx++;
                                            }
                                            g.ResetTransform();
                                        }
                                        
                                    }
                                }

                            }
                            index++;
                        }
                    }
                    finally
                    {                        
                        if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                        if (oldGdiObj != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiObj);
                        if (gdiplusPen != null) gdiplusPen.Dispose();
                        if (rwPen != null) rwPen.Dispose();                                                
                    }
                }
            }
            
            finally
            {
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
            }

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
                            if (strSize.Width <= (renderPtObjList[n].Point0Dist)*ws)
                            {
                                g.ResetTransform();
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
                g.ResetTransform();
                //g.DrawString("Test", renderer.Font, Brushes.Black, 0, 0);
            }
            
		}

        
        private struct RenderPtObj
        {
            public Point Point0;
            public float Point0Dist;
            public float Angle0;
            public Point Point1;
            public float Point1Dist;
            public float Angle1;

            public int RecordIndex;

            public RenderPtObj(Point p0, float poDist, float p0Angle, Point p1, float p1Dist,float p1Angle, int recordIndexParam)
            {
                Point0 = p0;
                Point0Dist = poDist;
                Angle0 = p0Angle;
                RecordIndex = recordIndexParam;
                Point1 = p1;
                Point1Dist = p1Dist;
                Angle1 = p1Angle;
            }

        }

        public bool ContainsPoint(int shapeIndex, PointF pt, BinaryReader shapeFileStream, byte[] dataBuf, float minDist)
        {
            byte[] data = dataBuf;
            PolyLineRecordEx nextRec = Recs[shapeIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
            int numParts = nextRec.NumParts;
            PointF[] pts;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                pts = GetPointsF(data, nextRec.PartOffsets[partNum] << 3, numPoints);
                if (PointOnPolyLine(pts, pt, minDist))
                {
                    return true;
                }
            }
            data = null;
            return false;
        }

        private static bool PointOnPolyLine(PointF[] points, PointF pt, float minDist)
        {
            
            for (int i = 0; i < points.Length-1; i++)
            {
                if (lineSegPointDist(ref points[i], ref points[i + 1], ref pt) <= minDist)
                {
                    return true;
                }
                
            }
            
            return false;
        }

         //compute the dot product AB*BC         
        static float dot(ref PointF a, ref PointF b, ref PointF c)
        {
            PointF ab = new PointF(b.X-a.X, b.Y-a.Y);
            PointF bc = new PointF(c.X - b.X, c.Y - b.Y);
            return (ab.X * bc.X) + (ab.Y * bc.Y);        
        }

        //Compute the cross product AB x AC
        static float cross(ref PointF a, ref PointF b, ref PointF c)
        {
            PointF ab = new PointF(b.X - a.X, b.Y - a.Y);
            PointF ac = new PointF(c.X - a.X, c.Y - a.Y);
            return (ab.X * ac.Y) - (ab.Y * ac.X);                
        }
   
        private static double distance(ref PointF a,  ref PointF b)
        {
            float d1 = a.X - b.X;
            float d2 = a.Y - b.Y;
            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }


    //Compute the distance from AB to C
    //if isSegment is true, AB is a segment, not a line.
    private static double lineSegPointDist( ref PointF a,  ref PointF b, ref PointF c)
    {
        //float dist = cross(a,b,c) / distance(a,b);

        if (dot(ref a, ref b, ref c) > 0)
        {
            return distance(ref b,ref c);
        }
        if (dot(ref b, ref a, ref c) > 0)
        {
            return distance(ref a, ref c);
        } 
        return Math.Abs(cross(ref a, ref b, ref c) / distance(ref a, ref b));
    }



    #region QTNodeHelper Members

    public bool IsPointData()
    {
        return false;
    }

    public PointF GetRecordPoint(int recordIndex)
    {
        //throw new Exception("The method or operation is not implemented.");
        return PointF.Empty;
    }

    public RectangleF GetRecordBounds(int recordIndex)
    {
        //throw new Exception("The method or operation is not implemented.");
        return Recs[recordIndex].Bounds;
    }

    #endregion

    
}

    struct LabelRenderLineObj
    {
        public Point Point1, Point2, Point3;
        public float Angle1, Angle2;
        public int RecordIndex;

        public LabelRenderLineObj(Point pt1, Point pt2, Point pt3, int recordIndexParam)
        {
            Point1 = pt1;
            Point2 = pt2;
            Point3 = pt3;
            Angle1 = Angle2 = 0f;
            RecordIndex = recordIndexParam;
        }

    }
    
	class SFPolyLineMExCol : SFRecordCol
	{
		public PolyLineMRecordEx[] Recs;

		public SFPolyLineMExCol(PolyLineMRecordEx[] recs,ShapeFileMainHeaderEx head): base(head)
		{
			this.Recs = recs;
		}

		public override void paint(Graphics g, Size clientArea, RectangleD extent,Stream shapeFileStream)
		{
			paint(g, clientArea, extent, shapeFileStream, null);
		}

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream)
        {
            throw new System.NotImplementedException("The method or operation is not implemented.");
        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderer)
        {
            bool UseGDI = this.UseGDI(extent);
            //DateTime start = DateTime.Now;

            IntPtr dc = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiObj = IntPtr.Zero;
            Pen gdiplusPen = null;
            Pen rwPen = null;
            bool renderRailway = false;


            float scaleX = (float)(clientArea.Width / extent.Width);
            float scaleY = -scaleX;

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }
            
            bool labelFields = (renderer != null && renderer.FieldIndex >= 0 && (renderer.MinRenderLabelZoom < 0 || scaleX > renderer.MinRenderLabelZoom));
            bool renderDuplicateFields = (labelFields && renderer.RenderDuplicateFields); 
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);

            float renderPenWidth = renderer.PenWidthScale * scaleX;

            if (renderer.MaxPixelPenWidth > 0 && renderPenWidth > renderer.MaxPixelPenWidth)
            {
                renderPenWidth = renderer.MaxPixelPenWidth;
            }
            if (renderer.MinPixelPenWidth > 0 && renderPenWidth < renderer.MinPixelPenWidth)
            {
                renderPenWidth = renderer.MinPixelPenWidth;
            }

            if (renderer.LineType == LineType.Railway)
            {
                renderPenWidth = Math.Min(renderPenWidth, 7f);
            }

            // obtain a handle to the Device Context so we can use GDI to perform the painting.
            if (UseGDI)
            {
                dc = g.GetHdc();
            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              
            }
            try
            {
                int maxPaintCount = 2;
                if ((renderer.LineType == LineType.Solid) || (Math.Round(renderPenWidth) < 3))
                {
                    maxPaintCount = 1;
                }

                for (int paintCount = 0; paintCount < maxPaintCount; paintCount++)
                {
                    try
                    {
                        int penWidth = (int)Math.Round(renderPenWidth);
                        int color = 0x1d1030;

                        if (renderer != null)
                        {
                            color = renderer.OutlineColor.B & 0xff;
                            color = (color << 8) | (renderer.OutlineColor.G & 0xff);
                            color = (color << 8) | (renderer.OutlineColor.R & 0xff);
                        }

                        if (paintCount == 1)
                        {
                            color = 0x202020;
                            if (renderer != null)
                            {
                                color = renderer.FillColor.B & 0xff;
                                color = (color << 8) | (renderer.FillColor.G & 0xff);
                                color = (color << 8) | (renderer.FillColor.R & 0xff);
                            }
                            penWidth -= 2;
                        }


                        if (UseGDI)
                        {
                            gdiPen = NativeMethods.CreatePen(0, penWidth, color);
                            oldGdiObj = NativeMethods.SelectObject(dc, gdiPen);
                        }
                        else
                        {
                            if (paintCount == 0)
                            {
                                gdiplusPen = new Pen(renderer.OutlineColor, penWidth);
                            }
                            else if (paintCount == 1)
                            {
                                gdiplusPen = new Pen(renderer.FillColor, penWidth);

                                if (penWidth > 1)
                                {
                                    renderRailway = (renderer.LineType == LineType.Railway);
                                    if (renderRailway)
                                    {
                                        rwPen = new Pen(renderer.OutlineColor, 1);
                                    }
                                }

                            }
                            gdiplusPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            gdiplusPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            gdiplusPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                        }

                        //shapeFileStream.BaseStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);
                        shapeFileStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);

                        PolyLineMRecordEx[] plmRecs = Recs;
                        int index = 0;
                        byte[] data = SFRecordCol.SharedBuffer;                        
                        float minLabelLength = (float)(6 / scaleX);
                        while (index < plmRecs.Length)
                        {
                            PolyLineMRecordEx nextRec = plmRecs[index];
                            //shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
                            shapeFileStream.Read(data, 0, (nextRec.NumPoints << 3) + (nextRec.NumPoints << 2));
                            if (actualExtent.IntersectsWith(nextRec.Bounds))
                            {
                                int numParts = nextRec.NumParts;
                                Point[] pts;

                                //if using a renderer then add a renderPtObj
                                if (labelFields && paintCount == 0)
                                {
                                    if ((nextRec.Bounds.Width * scaleX > 6) || (nextRec.Bounds.Height * scaleX > 6))
                                    {
                                        if (renderDuplicateFields)
                                        {
                                            List<IndexAnglePair> iapList = GetPointsFAngle(data, 0, nextRec.NumPoints, minLabelLength);
                                            for (int li = iapList.Count - 1; li >= 0; li--)
                                            {
                                                pts = GetPoints(data, iapList[li].PointIndex << 3, 2, offX, offY, scaleX, -scaleX);
                                                float d0 = (float)Math.Sqrt(((pts[1].X - pts[0].X) * (pts[1].X - pts[0].X)) + ((pts[1].Y - pts[0].Y) * (pts[1].Y - pts[0].Y)));
                                                if (Math.Abs(iapList[li].Angle) > 90f && Math.Abs(iapList[li].Angle) <= 270f)
                                                {
                                                    renderPtObjList.Add(new RenderPtObj(pts[1], d0, iapList[li].Angle - 180f, Point.Empty, 0, 0, index));
                                                }
                                                else
                                                {
                                                    renderPtObjList.Add(new RenderPtObj(pts[0], d0, iapList[li].Angle, Point.Empty, 0, 0, index));
                                                }
                                            }
                                        }
                                        else
                                        {

                                            int pointIndex = 0;
                                            float angle = GetPointsFAngle(data, 0, nextRec.NumPoints, ref pointIndex, actualExtent);
                                            if (!angle.Equals(float.NaN))
                                            {                                               
                                                pts = GetPoints(data, pointIndex << 3, 2, offX, offY, scaleX, -scaleX);
                                                float d0 = (float)Math.Sqrt(((pts[1].X - pts[0].X) * (pts[1].X - pts[0].X)) + ((pts[1].Y - pts[0].Y) * (pts[1].Y - pts[0].Y)));
                                                if (Math.Abs(angle) > 90f && Math.Abs(angle) <= 270f)
                                                {
                                                    renderPtObjList.Add(new RenderPtObj(pts[1], d0, angle - 180f, Point.Empty, 0, 0, index));
                                                }
                                                else
                                                {
                                                    renderPtObjList.Add(new RenderPtObj(pts[0], d0, angle, Point.Empty, 0, 0, index));
                                                }                                                
                                            }
                                        }
                                    }
                                }

                                for (int partNum = 0; partNum < numParts; partNum++)
                                {
                                    int numPoints;
                                    if ((numParts - partNum) > 1)
                                    {
                                        numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                                    }
                                    else
                                    {
                                        numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                                    }
                                    if (numPoints <= 1)
                                    {
                                        Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                        continue;
                                    }
                                    if (UseGDI)
                                    {
                                        pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, -scaleX);
                                        NativeMethods.DrawPolyline(dc, pts, numPoints);
                                    }
                                    else
                                    {
                                        pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY);
                                        g.DrawLines(gdiplusPen, pts);

                                        if (renderRailway)
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
                                                    g.ResetTransform();
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
                                            g.ResetTransform();
                                        }
                                    }
                                }

                            }
                            index++;
                        }
                    }
                    finally
                    {                        
                        if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                        if (oldGdiObj != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiObj);
                        if (gdiplusPen != null) gdiplusPen.Dispose();
                        if (rwPen != null) rwPen.Dispose();                    
                    }
                }
            }

            finally
            {
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
            }

            try
            {
                if (labelFields)
                {
                    Brush fontBrush = new SolidBrush(renderer.FontColor);
                    int count = renderPtObjList.Count;
                    //string lastLabel = null;
                    for (int n = 0; n < count; n++)
                    {
                        //RenderPtObj rpObj = renderPtObjList[n];
                        string strLabel = renderer.DbfReader.GetField(renderPtObjList[n].RecordIndex, renderer.FieldIndex).Trim();
                        if (strLabel.Length > 0)// && string.Compare(strLabel, lastLabel) != 0)
                        {
                            //lastLabel = strLabel;
                            SizeF strSize = g.MeasureString(strLabel, renderer.Font);
                            if (strSize.Width*.5f <= (renderPtObjList[n].Point0Dist))
                            {
                                g.ResetTransform();
                                g.TranslateTransform(renderPtObjList[n].Point0.X, renderPtObjList[n].Point0.Y);
                                g.RotateTransform(-renderPtObjList[n].Angle0);

                                g.DrawString(strLabel, renderer.Font, fontBrush, (renderPtObjList[n].Point0Dist - strSize.Width) / 2/*-strSize.Width / 2*/, -strSize.Height / 2);
                                //g.DrawRectangle(Pens.Red, 0, -strSize.Height, rpObj.Point0Dist, strSize.Height);
                            }
                        }
                    }
                }
            }
            finally
            {
                g.ResetTransform();
            }            
        }

        private struct RenderPtObj
        {
            public Point Point0;
            public float Point0Dist;
            public float Angle0;
            public Point Point1;
            public float Point1Dist;
            public float Angle1;

            public int RecordIndex;

            public RenderPtObj(Point p0, float poDist, float p0Angle, Point p1, float p1Dist, float p1Angle, int recordIndexParam)
            {
                Point0 = p0;
                Point0Dist = poDist;
                Angle0 = p0Angle;
                RecordIndex = recordIndexParam;
                Point1 = p1;
                Point1Dist = p1Dist;
                Angle1 = p1Angle;
            }

        }
        
	}

    class SFPolygonExCol : SFRecordCol, QTNodeHelper
	{        
		public PolygonRecordEx[] Recs;

		public SFPolygonExCol(PolygonRecordEx[] recs, ShapeFileMainHeaderEx head): base(head)
		{
			this.Recs = recs;
		}

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer/*new byte[ShapeFileExConstants.MAX_REC_LENGTH]*/);

        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            byte[] data = dataBuffer;
            PolygonRecordEx nextRec = Recs[recordIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
            int numParts = nextRec.NumParts;
            List<PointF[]> dataList = new List<PointF[]>();
            PointF[] pts;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                pts = GetPointsF(data, nextRec.PartOffsets[partNum] << 3, numPoints);
                dataList.Add(pts);
            }
            data = null;
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream)
		{
			paint(g, clientArea, extent, shapeFileStream, null);
		}
        
        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings)
        {
            if (this.UseGDI(extent))
            {
                paintLowQuality(g, clientArea, extent, shapeFileStream, renderSettings);
            }
            else
            {
                paintHiQuality(g, clientArea, extent, shapeFileStream, renderSettings);
            }
        }

        private void paintHiQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings)
        {                      
            Pen gdiplusPen = null;
            Brush gdiplusBrush = null;
            bool renderInterior = true;

            float scaleX = (float)(clientArea.Width / extent.Width);
            float scaleY = -scaleX;
            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;

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
           
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              
           
            try
            {
                // setup our pen and brush                
                if (renderSettings != null)
                {
                    gdiplusBrush = new SolidBrush(renderSettings.FillColor);
                    gdiplusPen = new Pen(renderSettings.OutlineColor, 1);
                }
                else
                {
                    gdiplusBrush = new SolidBrush(Color.White);
                    gdiplusPen = new Pen(Color.Black, 1);
                }            
                shapeFileStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);

                PointF pt = new PointF(0, 0);
                PolygonRecordEx[] pgRecs = Recs;
                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                while (index < pgRecs.Length)
                {
                    Rectangle partBounds = Rectangle.Empty;
                    PolygonRecordEx nextRec = pgRecs[index];
                    shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
                    if (actualExtent.IntersectsWith(nextRec.Bounds) && (!useCustomRenderSettings || customRenderSettings.RenderShape(index)))
                    {
                        if (useCustomRenderSettings)
                        {                                                        
                            Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                            if (customColor.ToArgb() != currentPenColor.ToArgb())
                            {
                                gdiplusPen = new Pen(customColor, 1);
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
                        int numParts = nextRec.NumParts;
                        Point[] pts;
                        System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                        gp.FillMode = System.Drawing.Drawing2D.FillMode.Winding;
                    
                        for (int partNum = 0; partNum < numParts; ++partNum)
                        {
                            int numPoints;
                            if ((numParts - partNum) > 1)
                            {
                                numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                            }
                            else
                            {
                                numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                            }
                            if (numPoints <= 1)
                            {
                                Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                continue;
                            }                           
                            if (labelfields)
                            {
                                pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY, ref partBounds);
                                if (partBounds.Width > 5 && partBounds.Height > 5)
                                {
                                    partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                }
                            }
                            else
                            {
                                pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY);
                            }
                            //if (renderInterior)
                            //{
                            //    g.FillPolygon(gdiplusBrush, pts);
                            //}
                            //g.DrawPolygon(gdiplusPen, pts);
                            gp.AddPolygon(pts);                    
                        }
                        if (renderInterior)
                        {
                            //g.FillPolygon(gdiplusBrush, pts);
                            g.FillPath(gdiplusBrush, gp);
                        }
                        //g.DrawPolygon(gdiplusPen, pts);
                        g.DrawPath(gdiplusPen, gp);
                        gp.Dispose();
                    }
                    ++index;
                }
            }
            finally
            {
                if (gdiplusPen != null) gdiplusPen.Dispose();
                if (gdiplusBrush != null) gdiplusBrush.Dispose();
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


        private void paintLowQuality(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings)
        {           
            IntPtr dc = IntPtr.Zero;
            IntPtr gdiBrush = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiBrush = IntPtr.Zero;
            IntPtr oldGdiPen = IntPtr.Zero;
            bool renderInterior = true;
            float scaleX = (float)(clientArea.Width / extent.Width);
            float scaleY = -scaleX;

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;

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
                int color = 0x70c09b;                
                if (renderSettings != null)
                {
                    color = ColorToGDIColor(renderSettings.FillColor);
                }

                if (renderInterior)
                {
                    gdiBrush = NativeMethods.CreateSolidBrush(color);
                    oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                }
                else
                {
                    oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                }

                color = 0x70c09b;
                if (renderSettings != null)
                {
                    color = ColorToGDIColor(renderSettings.OutlineColor);
                }
                gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 1, color);
                oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);
            
                shapeFileStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);

                PointF pt = new PointF(0, 0);
                PolygonRecordEx[] pgRecs = Recs;
                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                while (index < pgRecs.Length)
                {
                    Rectangle partBounds = Rectangle.Empty;
                    PolygonRecordEx nextRec = pgRecs[index];
                    shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
                    if (actualExtent.IntersectsWith(nextRec.Bounds) && (!useCustomRenderSettings || customRenderSettings.RenderShape(index)))
                    {
                        if (useCustomRenderSettings)
                        {                            
                            Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                            if (customColor.ToArgb() != currentPenColor.ToArgb())
                            {
                                gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 1, ColorToGDIColor(customColor));
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
                        int numParts = nextRec.NumParts;
                        for (int partNum = 0; partNum < numParts; ++partNum)
                        {
                            int numPoints;
                            if ((numParts - partNum) > 1)
                            {
                                numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                            }
                            else
                            {
                                numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                            }
                            if (numPoints <= 1)
                            {
                                Console.Out.WriteLine("Skipping Record {0}, Part {1} - {2} point(s)", index, partNum, numPoints);
                                continue;
                            }
                            
                            int usedPoints = 0;
                            if (labelfields)
                            {
                                GetPointsRemoveDuplicates(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY, ref partBounds, sharedPointBuffer, ref usedPoints);
                                if (partBounds.Width > 5 && partBounds.Height > 5)
                                {
                                    partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                }
                            }
                            else
                            {
                                GetPointsRemoveDuplicates(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY, sharedPointBuffer, ref usedPoints);
                            }
                            NativeMethods.DrawPolygon(dc, sharedPointBuffer, usedPoints);
                                                        
                        }
                    }
                    ++index;
                }
            }
            finally
            {
                if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);                
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


        internal bool ContainsPoint(int shapeIndex, PointF pt, BinaryReader shapeFileStream)
        {
            byte[] data = SFRecordCol.SharedBuffer;            
            PolygonRecordEx nextRec = Recs[shapeIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
            int numParts = nextRec.NumParts;
            PointF[] pts;
            //Console.Out.WriteLine("num parts:" + numParts);
            //check for holes
            bool inPolygon = false;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                pts = GetPointsF(data, nextRec.PartOffsets[partNum] << 3, numPoints);
                
                bool isHole = false;
                bool partInPolygon = PointInPolygon(pts, pt.X, pt.Y, numParts == 1, ref isHole); //ignore holes if only 1 part
                inPolygon |= partInPolygon;
                if(isHole && partInPolygon)
                {
                    //Console.Out.WriteLine("is hole!");
                    return false;
                }
                
            }

            return inPolygon;
        }
		
        /// <summary>
        /// tests whether point is inside a polygon
        /// </summary>
        /// <param name="points"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static bool PointInPolygon(PointF[] points,float x, float y, bool ignoreHoles, ref bool isHole) 
        {
            if (ignoreHoles) return PointInPolygon(points, x, y);

            //if we are detecting holes then we need to calculate the area
            float area = 0;
            //latitude = y
            int j=points.Length-1;
            bool inPoly=false;

            for (int i=0; i<points.Length; ++i) 
            {
                if (points[i].X<x && points[j].X>=x ||  points[j].X<x && points[i].X>=x) 
                {
                    if (points[i].Y+(x-points[i].X)/(points[j].X-points[i].X)*(points[j].Y-points[i].Y)<y) 
                    {
                        inPoly=!inPoly; 
                    }
                }
                
                area += (points[j].X*points[i].Y - points[i].X*points[j].Y);
                
                j=i;
            }
            area *=0.5f;
            //Console.Out.WriteLine("area = " + area);
            isHole = area > 0;
            return inPoly;// && isHole; 
        }

        private static bool PointInPolygon(PointF[] points, float x, float y)
        {
            //latitude = y
            int j = points.Length - 1;
            bool inPoly = false;

            for (int i = 0; i < points.Length; ++i)
            {
                if (points[i].X < x && points[j].X >= x || points[j].X < x && points[i].X >= x)
                {
                    if (points[i].Y + (x - points[i].X) / (points[j].X - points[i].X) * (points[j].Y - points[i].Y) < y)
                    {
                        inPoly = !inPoly;
                    }
                }
                j = i;
            }

            return inPoly;
        }


        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointF GetRecordPoint(int recordIndex)
        {
            return PointF.Empty;
        }

        public RectangleF GetRecordBounds(int recordIndex)
        {
            return Recs[recordIndex].Bounds;
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

    class SFPolygonZExCol : SFRecordCol, QTNodeHelper
    {
        public PolygonZRecordEx[] Recs;

        public SFPolygonZExCol(PolygonZRecordEx[] recs, ShapeFileMainHeaderEx head)
            : base(head)
        {
            this.Recs = recs;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream)
        {
            return GetShapeData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);

        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            byte[] data = dataBuffer;
            PolygonZRecordEx nextRec = Recs[recordIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3);
            int numParts = nextRec.NumParts;
            List<PointF[]> dataList = new List<PointF[]>();
            PointF[] pts;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                pts = GetPointsF(data, nextRec.PartOffsets[partNum] << 3, numPoints);
                dataList.Add(pts);
            }
            data = null;
            return dataList;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream)
        {
            return GetShapeHeightData(recordIndex, shapeFileStream, SFRecordCol.SharedBuffer);
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            byte[] data = dataBuffer;
            PolygonZRecordEx nextRec = Recs[recordIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset + (nextRec.NumPoints << 3), SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints <<2); //4bytes per point
            int numParts = nextRec.NumParts;
            List<float[]> dataList = new List<float[]>();
            float[] heights;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                heights = GetFloatData(data, nextRec.PartOffsets[partNum] << 2, numPoints);
                dataList.Add(heights);
            }
            data = null;
            return dataList;
        }


        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream)
        {
            paint(g, clientArea, extent, shapeFileStream, null);
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings)
        {
            bool UseGDI = this.UseGDI(extent);

            IntPtr dc = IntPtr.Zero;
            IntPtr gdiBrush = IntPtr.Zero;
            IntPtr gdiPen = IntPtr.Zero;
            IntPtr oldGdiBrush = IntPtr.Zero;
            IntPtr oldGdiPen = IntPtr.Zero;
            Pen gdiplusPen = null;
            Brush gdiplusBrush = null;
            bool renderInterior = true;

            float scaleX = (float)(clientArea.Width / extent.Width);
            float scaleY = -scaleX;

            RectangleD projectedExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width / scaleX, clientArea.Height / (-scaleY));
            double offX = -projectedExtent.Left;
            double offY = -projectedExtent.Bottom;
            RectangleD actualExtent = projectedExtent;

            ICustomRenderSettings customRenderSettings = renderSettings.CustomRenderSettings;
            bool useCustomRenderSettings = (renderSettings != null) && (customRenderSettings != null);

            Color currentBrushColor = renderSettings.FillColor, currentPenColor = renderSettings.OutlineColor;

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
            if (UseGDI)
            {
                dc = g.GetHdc();
            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;// ClearTypeGridFit;              
            }

            try
            {
                // setup our pen and brush
                int color = 0x70c09b;

                if (UseGDI)
                {
                    if (renderSettings != null)
                    {
                        color = ColorToGDIColor(renderSettings.FillColor);
                    }

                    if (renderInterior)
                    {
                        gdiBrush = NativeMethods.CreateSolidBrush(color);
                        oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                    }
                    else
                    {
                        oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                    }

                    color = 0x70c09b;
                    if (renderSettings != null)
                    {
                        color = ColorToGDIColor(renderSettings.OutlineColor);
                    }
                    gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 1, color);
                    oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);

                    NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);
                }
                else
                {
                    if (renderSettings != null)
                    {
                        gdiplusBrush = new SolidBrush(renderSettings.FillColor);
                        gdiplusPen = new Pen(renderSettings.OutlineColor, 1);
                    }
                    else
                    {
                        gdiplusBrush = new SolidBrush(Color.White);
                        gdiplusPen = new Pen(Color.Black, 1);
                    }
                }

                shapeFileStream.Seek(MainHeader.DataOffset, SeekOrigin.Begin);

                PointF pt = new PointF(0, 0);
                PolygonZRecordEx[] pgRecs = Recs;
                int index = 0;
                byte[] data = SFRecordCol.SharedBuffer;
                Point[] sharedPointBuffer = SFRecordCol.SharedPointBuffer;
                while (index < pgRecs.Length)
                {
                    Rectangle partBounds = Rectangle.Empty;
                    PolygonZRecordEx nextRec = pgRecs[index];
                    //we read all of the record data 8 bytes for each point + (4+4 for the Z and Measure)
                    //but we wont actually use the Z or Measure
                    shapeFileStream.Read(data, 0, nextRec.NumPoints << 4);
                    if (actualExtent.IntersectsWith(nextRec.Bounds) && (!useCustomRenderSettings || customRenderSettings.RenderShape(index)))
                    {
                        if (useCustomRenderSettings)
                        {
                            if (UseGDI)
                            {
                                Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                if (customColor.ToArgb() != currentPenColor.ToArgb())
                                {
                                    gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 1, ColorToGDIColor(customColor));
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
                            else
                            {
                                Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                if (customColor.ToArgb() != currentPenColor.ToArgb())
                                {
                                    gdiplusPen = new Pen(customColor, 1);
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
                        }
                        int numParts = nextRec.NumParts;
                        Point[] pts;
                        for (int partNum = 0; partNum < numParts; ++partNum)
                        {
                            int numPoints;
                            if ((numParts - partNum) > 1)
                            {
                                numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                            }
                            else
                            {
                                numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                            }
                            if (UseGDI)
                            {
                                int usedPoints = 0;
                                if (labelfields)
                                {
                                    GetPointsRemoveDuplicates(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY, ref partBounds, sharedPointBuffer, ref usedPoints);
                                    if (partBounds.Width > 5 && partBounds.Height > 5)
                                    {
                                        partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                    }
                                }
                                else
                                {
                                    GetPointsRemoveDuplicates(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY, sharedPointBuffer, ref usedPoints);
                                }
                                NativeMethods.DrawPolygon(dc, sharedPointBuffer, usedPoints);
                            }
                            else
                            {
                                if (labelfields)
                                {
                                    pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY, ref partBounds);
                                    if (partBounds.Width > 5 && partBounds.Height > 5)
                                    {
                                        partBoundsIndexList.Add(new PartBoundsIndex(index, partBounds));
                                    }
                                }
                                else
                                {
                                    pts = GetPoints(data, nextRec.PartOffsets[partNum] << 3, numPoints, offX, offY, scaleX, scaleY);
                                }
                                if (renderInterior)
                                {
                                    g.FillPolygon(gdiplusBrush, pts);
                                }
                                g.DrawPolygon(gdiplusPen, pts);
                            }
                        }
                    }
                    ++index;
                }
            }
            finally
            {
                if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);
                if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                if (dc != IntPtr.Zero) g.ReleaseHdc(dc);
                if (gdiplusPen != null) gdiplusPen.Dispose();
                if (gdiplusBrush != null) gdiplusBrush.Dispose();

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

        internal bool ContainsPoint(int shapeIndex, PointF pt, BinaryReader shapeFileStream)
        {
            byte[] data = SFRecordCol.SharedBuffer;
            PolygonZRecordEx nextRec = Recs[shapeIndex];
            shapeFileStream.BaseStream.Seek(nextRec.DataOffset, SeekOrigin.Begin);
            shapeFileStream.Read(data, 0, nextRec.NumPoints << 3); //just read the points (don't care about the Z or M values)
            int numParts = nextRec.NumParts;
            PointF[] pts;
            for (int partNum = 0; partNum < numParts; partNum++)
            {
                int numPoints;
                if ((numParts - partNum) > 1)
                {
                    numPoints = nextRec.PartOffsets[partNum + 1] - nextRec.PartOffsets[partNum];
                }
                else
                {
                    numPoints = nextRec.NumPoints - nextRec.PartOffsets[partNum];
                }
                pts = GetPointsF(data, nextRec.PartOffsets[partNum] << 3, numPoints);
                if (PointInPolygon(pts, pt.X, pt.Y))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PointInPolygon(PointF[] points, float x, float y)
        {
            //latitude = y
            int j = points.Length - 1;
            bool inPoly = false;

            for (int i = 0; i < points.Length; ++i)
            {
                if (points[i].X < x && points[j].X >= x || points[j].X < x && points[i].X >= x)
                {
                    if (points[i].Y + (x - points[i].X) / (points[j].X - points[i].X) * (points[j].Y - points[i].Y) < y)
                    {
                        inPoly = !inPoly;
                    }
                }
                j = i;
            }

            return inPoly;
        }


        #region QTNodeHelper Members

        public bool IsPointData()
        {
            return false;
        }

        public PointF GetRecordPoint(int recordIndex)
        {
            return PointF.Empty;
        }

        public RectangleF GetRecordBounds(int recordIndex)
        {
            return Recs[recordIndex].Bounds;
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


	class SFPointExCol : SFRecordCol
	{
        public PointRecordEx[] Recs;

		public SFPointExCol(PointRecordEx[] recs,ShapeFileMainHeaderEx head): base(head)
		{
			this.Recs = recs;
        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream)
        {
            if(recordIndex <0 || recordIndex >= Recs.Length) throw new ArgumentOutOfRangeException("recordIndex");

            PointRecordEx nextRec = Recs[recordIndex];
            List<PointF[]> data = new List<PointF[]>();
            data.Add( new PointF[] { nextRec.pt });
            return data;

        }

        public override List<PointF[]> GetShapeData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            return GetShapeData(recordIndex, shapeFileStream);
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream)
        {
            return null;
        }

        public override List<float[]> GetShapeHeightData(int recordIndex, BinaryReader shapeFileStream, byte[] dataBuffer)
        {
            return null;
        }

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream)
		{
			paint(g, clientArea, extent, shapeFileStream, null);
		}

        public override void paint(Graphics g, Size clientArea, RectangleD extent, Stream shapeFileStream, RenderSettings renderSettings)
		{
            bool useGDI = this.UseGDI(extent);
            List<RenderPtObj> renderPtObjList = new List<RenderPtObj>(64);			
			double scaleX = (double)(clientArea.Width / extent.Width);
            double scaleY = -scaleX;
			RectangleD actualExtent = new RectangleD(extent.Left, extent.Top, clientArea.Width/scaleX, clientArea.Height/(-scaleY));
			double offX = -actualExtent.Left;
			double offY = -actualExtent.Bottom;
            bool labelFields = (renderSettings != null && renderSettings.FieldIndex >= 0 && (renderSettings.MinRenderLabelZoom < 0 || scaleX > renderSettings.MinRenderLabelZoom));
            bool renderInterior = true;
            
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
                    useGDI = false; //use hi quality if we're going to render an image
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
            
            if (MercProj)
            {
                //if we're using a Mercator Projection then convert the actual Extent to LL coords
                PointD tl = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Left, actualExtent.Top));
                PointD br = SFRecordCol.ProjectionToLL(new PointD(actualExtent.Right, actualExtent.Bottom));
                actualExtent = RectangleD.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            }

            if (useGDI)
            {
                IntPtr dc = IntPtr.Zero;
                IntPtr gdiPen = IntPtr.Zero;
                IntPtr gdiBrush = IntPtr.Zero;
                IntPtr oldGdiPen = IntPtr.Zero;
                IntPtr oldGdiBrush = IntPtr.Zero;


                // obtain a handle to the Device Context so we can use GDI to perform the painting.
                dc = g.GetHdc();

                try
                {
                    int color = 0x70c09b;                    
                    if (renderSettings != null)
                    {
                        color = ColorToGDIColor(renderSettings.FillColor);
                    }
                    if (renderInterior)
                    {
                        gdiBrush = NativeMethods.CreateSolidBrush(color);
                        oldGdiBrush = NativeMethods.SelectObject(dc, gdiBrush);
                    }
                    else
                    {
                        oldGdiBrush = NativeMethods.SelectObject(dc, NativeMethods.GetStockObject(NativeMethods.NULL_BRUSH));
                    }
                    
                    color = 0x70c09b;
                    if (renderSettings != null)
                    {
                        color = ColorToGDIColor(renderSettings.OutlineColor);
                    }
                    gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 1, color);
                    oldGdiPen = NativeMethods.SelectObject(dc, gdiPen);
                    
                    NativeMethods.SetBkMode(dc, NativeMethods.TRANSPARENT);
                     
                    int index = 0;
                    Point pt = new Point();
                    int halfPointSize = (int)Math.Round(pointSize*0.5);
                    int pointSizeInt = (int)Math.Round(pointSize);
                    while (index < Recs.Length)
                    {
                        PointRecordEx nextRec = Recs[index];
                        if (actualExtent.Contains(nextRec.pt) && (!useCustomRenderSettings || customRenderSettings.RenderShape(index)))
                        {
                            if (useCustomRenderSettings)
                            {                                
                                Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                if (customColor.ToArgb() != currentPenColor.ToArgb())
                                {
                                    gdiPen = NativeMethods.CreatePen(NativeMethods.PS_SOLID, 1, ColorToGDIColor(customColor));
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

                            PointD ptf = LLToProjection((PointD)nextRec.pt);
                            pt.X = (int)Math.Round((ptf.X + offX) * scaleX);
                            pt.Y = (int)Math.Round((ptf.Y + offY) * scaleY);

                            if (pointSizeInt > 0)
                            {
                                NativeMethods.Ellipse(dc, pt.X - halfPointSize, pt.Y - halfPointSize, pt.X + halfPointSize, pt.Y + halfPointSize);
                            }
                            if (labelFields)
                            {
                                renderPtObjList.Add(new RenderPtObj(pt, index, halfPointSize+5, -(halfPointSize+5)));                                
                            }                            
                        }
                        ++index;
                    }
                }
                finally
                {                    
                    if (oldGdiPen != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiPen);
                    if (gdiPen != IntPtr.Zero) NativeMethods.DeleteObject(gdiPen);
                    if (oldGdiBrush != IntPtr.Zero) NativeMethods.SelectObject(dc, oldGdiBrush);
                    if (gdiBrush != IntPtr.Zero) NativeMethods.DeleteObject(gdiBrush);                
                    g.ReleaseHdc(dc);
                }
            }
            else
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Brush fillBrush = null;
                Pen outlinePen = null;
                if (renderSettings != null)
                {
                    fillBrush = new SolidBrush(renderSettings.FillColor);
                    outlinePen = new Pen(renderSettings.OutlineColor,1f);
                }
                else
                {
                    fillBrush = new SolidBrush(Color.Black);
                    outlinePen = new Pen(Color.White,1f);
                }
                try
                {                   
                    int index = 0;
                    float halfPointSize = pointSize * 0.5f;
                    while (index < Recs.Length)
                    {
                        PointRecordEx nextRec = Recs[index];
                        if (actualExtent.Contains(nextRec.pt) && (!useCustomRenderSettings || customRenderSettings.RenderShape(index)))
                        {
                            if(useCustomRenderSettings)
                            {
                                Color customColor = customRenderSettings.GetRecordOutlineColor(index);
                                if (customColor.ToArgb() != currentPenColor.ToArgb())
                                {
                                    outlinePen = new Pen(customColor, 1);
                                    currentPenColor = customColor;
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

                            PointD ptD = LLToProjection((PointD)nextRec.pt);
                        
                            PointF pt = new PointF((float)((ptD.X + offX) * scaleX), (float)((ptD.Y + offY) * scaleY));
                            if (drawPoint)
                            {
                                if (pointSize > 0)
                                {
                                    if (renderInterior)
                                    {
                                        g.FillEllipse(fillBrush, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                    }
                                    g.DrawEllipse(outlinePen, pt.X - halfPointSize, pt.Y - halfPointSize, pointSize, pointSize);
                                }
                                if (labelFields)
                                {
                                    renderPtObjList.Add(new RenderPtObj(pt, index, (int)halfPointSize + 5, -((int)halfPointSize + 5)));
                                }
                            }
                            else
                            {                        
                                g.DrawImage(symbol, pt.X - (symbolSize.Width >> 1), pt.Y -(symbolSize.Height>>1));
                                if (labelFields)
                                {
                                    renderPtObjList.Add(new RenderPtObj(pt, index, ((symbolSize.Width + 1) >> 1)+5, -(((symbolSize.Height + 1) >> 1) + 5) ));
                                }
                            }
                            
                            
                        }                        
                        index++;
                    }
                    
                }
                finally
                {                    
                    if(fillBrush!= null) fillBrush.Dispose();
                    if (outlinePen != null) outlinePen.Dispose();  
                }
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
                        if (labelPlacementMap.addLabelToMap(Point.Round(renderPtObjList[n].Pt), x0, y0, (int)Math.Round(labelSize.Width*ssm), (int)Math.Round(labelSize.Height*ssm)))
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
                                g.DrawRectangle(Pens.Red, renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0, labelSize.Width * ssm, labelSize.Height * ssm);
                                g.DrawString(strLabel, renderSettings.Font, fontBrush, new PointF(renderPtObjList[n].Pt.X + x0, renderPtObjList[n].Pt.Y + y0));
                            }
                        }
                    }
                }
                pen.Dispose();
                fontBrush.Dispose();
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
		private FileStream dbfFileStream;
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
            Console.Out.WriteLine("string encoding: " + stringEncoding);
            Console.Out.WriteLine(string.Format("string encoding: {0}[{1}]", stringEncoding.EncodingName, stringEncoding.CodePage));
            

			try
			{                
                dbfFileStream = new FileStream(System.IO.Path.ChangeExtension(filePath, ".dbf"), FileMode.Open, FileAccess.Read, FileShare.Read, FileBufSize);				
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
                    Console.Out.WriteLine("Error reading Language Driver Id - using default");
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
			
			int FieldOffset = this.dBFRecordHeader.HeaderLength + this.dBFRecordHeader.RecordLength*recordNumber + this.dBFRecordHeader.GetFieldDescriptions()[fieldIndex].RecordOffset;
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
			
			int RecordOffset = this.dBFRecordHeader.HeaderLength + this.dBFRecordHeader.RecordLength*recordNumber;
			
			int numFields = this.dBFRecordHeader.FieldCount;
			string[] strFields = new string[numFields];
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
                records[n] = this.GetField(n, fieldIndex).Trim();
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

            int FieldOffset = this.dBFRecordHeader.HeaderLength + this.dBFRecordHeader.GetFieldDescriptions()[fieldIndex].RecordOffset;
            
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

    #region GDIUtils

    internal sealed class NativeMethods
	{
        private NativeMethods() { }

		/// <summary>
		/// constant representing the OPAQUE Background Mode
		/// </summary>
		public const int OPAQUE = 2;
		
		/// <summary>
		/// constant representing the TRANSPARENT Background Mode
		/// </summary>
		public const int TRANSPARENT = 1;

		// Pen Style constants
		public const int PS_SOLID = 0;
		public const int PS_DASH = 1;
		public const int PS_DOT = 2;
		public const int PS_DASHDOT = 3;
        public const int NULL_BRUSH = 5;

		//BitBlt constants
		public const int SRCCOPY = 0xcc0020;

		[System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
		public static unsafe extern int Polyline(IntPtr hdc, Point* points, int count);

        public static unsafe void DrawPolyline(IntPtr hdc, Point[] points, int count)
        {
            fixed (Point* ptr = points)
            {
                Polyline(hdc, ptr, count);
            }
        }

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern unsafe int Polygon(IntPtr hdc, Point* points, int count);

        public static unsafe void DrawPolygon(IntPtr hdc, Point[] points, int count)
        {
            fixed (Point* ptr = points)
            {
                Polygon(hdc, ptr, count);
            }
        }

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern int Ellipse(IntPtr hdc, int left, int top, int right, int bottom);
       
		[System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr gdiobj);

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        public static extern IntPtr GetStockObject(int index);

		[System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]		
		public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int rgbColor);
		
		[System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]		
		public static extern IntPtr CreateSolidBrush(int rgbColor);

		[System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]		
		public static extern int DeleteObject(IntPtr gdiobj);				
       	
		[System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]		
		public static extern int SetBkMode(IntPtr hdc, int mode);				
               
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
    public sealed class ShapeFileEnumerator : IEnumerator<System.Collections.ObjectModel.Collection<PointF[]>>
    {
        /// <summary>
        /// Defines how shapes will be evaluated when moving to the next record when enumerating through a shapefile.
        /// Intersects (the default) evaluates shapes that intersect with the Extent of the ShapeFileEnumerator. Contains
        /// evaluates shapes that fit entirely within the Extent of the ShapeFileEnumerator.
        /// </summary>
        public enum IntersectionType { Intersects, Contains };

        int currentIndex = -1;

        private ShapeFile myShapeFile;

        private RectangleF[] recordExtents;
        private int totalRecords;

        private RectangleF extent;

        private byte[] dataBuffer = new byte[ShapeFileExConstants.MAX_REC_LENGTH];

        private IntersectionType intersectType = IntersectionType.Intersects;

        internal ShapeFileEnumerator(ShapeFile shapeFile, RectangleF extent):this(shapeFile, extent, IntersectionType.Intersects)
        {
        }

        internal ShapeFileEnumerator(ShapeFile shapeFile, RectangleF extent, IntersectionType intersectionType)
        {
            if (shapeFile == null) throw new ArgumentException("null shapefile");
            this.intersectType = intersectionType;
            this.extent = extent;
            myShapeFile = shapeFile;
            if (shapeFile.ShapeType != ShapeType.Point)
            {
                recordExtents = shapeFile.GetShapeExtents();
            }
            totalRecords = shapeFile.RecordCount;
        }

        internal ShapeFileEnumerator(ShapeFile shapeFile)
        {
            if (shapeFile == null) throw new ArgumentException("null shapefile");
            this.extent = shapeFile.Extent;
            myShapeFile = shapeFile;
            if (shapeFile.ShapeType != ShapeType.Point)
            {
                recordExtents = shapeFile.GetShapeExtents();
            }
            totalRecords = shapeFile.RecordCount;            
        }

        /// <summary>
        /// Gets the Extent of the enumerator. In most cases this will be the extent of the shapefile, but
        /// this may be smaller than the shape file's exent if the enumerator has been created from a sub region
        /// of the shapefile's extent
        /// </summary>
        public RectangleF Extent
        {
            get
            {
                return this.extent;
            }
        }

        #region IEnumerator<List<PointF[]>> Members

        /// <summary>
        /// Gets the raw PointF data of the current shape
        /// </summary>
        public System.Collections.ObjectModel.Collection<PointF[]> Current
        {
            get 
            {
                return myShapeFile.GetShapeData(currentIndex, dataBuffer);
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

        public System.Collections.ObjectModel.Collection<float[]> GetCurrentZValues()
        {
            //return myShapeFile.GetShapeData
            return myShapeFile.GetShapeZData(currentIndex, dataBuffer);
        }

        public System.Collections.ObjectModel.Collection<float[]> GetCurrentMValues()
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
            if (myShapeFile.ShapeType == ShapeType.Point)
            {

                while (currentIndex < totalRecords && !this.extent.Contains(myShapeFile.GetShapeBounds(currentIndex)))
                {
                    currentIndex++;
                }
            }
            else
            {
                if (this.intersectType == IntersectionType.Intersects)
                {
                    while (currentIndex < totalRecords && !recordExtents[currentIndex].IntersectsWith(this.extent))
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
