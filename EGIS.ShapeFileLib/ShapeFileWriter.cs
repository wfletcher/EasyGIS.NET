#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2020 Winston Fletcher.
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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Reflection;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// .NET ShapeFile Writer utility class which provides the ability to create or edit shapefiles from .NET applications.
    /// </summary>
    /// <remarks>        
    /// ShapeFileWriter is an abstract class. To create an instance of ShapeFileWriter call the CreateWriter
    /// method or derive a new class by extending ShapeFileWriter
    /// <para>To use the ShapeFileWriter create a new instance by calling the CreateWriter method, passing in 
    /// the shapefile path information, shape type and DBF field desricriptions. Progressively call the AddRecord method to 
    /// add new records to the shapefile. Once all records have been added, close the shapefile by calling the Close method</para>
    /// <para>Deriving classes must overload AddRecord.</para>
    /// <para>IMPORTANT- When you have finished writing all records you must call Close.</para>            
    /// <para>All characters stored in DBF records use UTF-8 character encoding to support Unicode character sets</para>
    /// </remarks>
    /// <example> Example code to create a new shapefile using an existing shapefile and a ShapeFileWriter. 
    /// <code>
    /// public void CreateShapeFile()
    /// {
    ///     ShapeFile sf = new ShapeFile("allroads.shp");
    ///     DbfReader dbfReader = new DbfReader("allroads.dbf");
    /// 
    ///     //create a new ShapeFileWriter
    ///     ShapeFileWriter sfw;
    ///     sfw = ShapeFileWriter.CreateWriter(".", "highways", sf.ShapeType, 
    ///         dbfReader.DbfRecordHeader.GetFieldDescriptions());
    ///     try
    ///     {
    ///         // Get a ShapeFileEnumerator from the shapefile
    ///         // and read each record
    ///         ShapeFileEnumerator sfEnum = sf.GetShapeFileEnumerator();
    ///         while (sfEnum.MoveNext())
    ///         {
    ///             // get the raw point data
    ///             PointD[] points = sfEnum.Current[0];
    ///             //get the DBF record
    ///             string[] fields = dbfReader.GetFields(sfEnum.CurrentShapeIndex);
    ///             //check whether to add the record to the new shapefile.
    ///             //(in this example, field zero contains the road type)
    ///             if(string.Compare(fields[0].Trim(), "highway", true) == 0)
    ///             {
    ///                 sfw.AddRecord(points, points.Length, fields);
    ///             }
    ///         }
    ///     }
    ///     finally
    ///     {
    ///         //close the shapefile, shapefilewriter and dbfreader
    ///         sfw.Close(); 
    ///         sf.Close();
    ///         dbfReader.Close();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AddRecord"/>
    /// <seealso cref="Close"/>
    [ObfuscationAttribute(Exclude = true, ApplyToMembers = true)]
    public abstract class ShapeFileWriter : IDisposable
    {

        private string baseDirectory;
        private string fileName;    
        private DbfFieldDesc[] dataFields;
        private ShapeType shapeType = ShapeType.NullShape;
        private int recordCount;
        
        private Stream shapeStream, indexStream, dbfStream, prjStream;
		private bool usingMemoryStreams = false;
        
        private RectangleD shapeBounds = RectangleD.Empty;

        static byte[] ShapeFileHeaderBytes =
        {0x00,0x00,0x27,0x0a, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, (byte)0xe8,0x03,0x00,0x00,
         0x01,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
         0x00,0x00,0x00,0x00};
        
        private const int ShapeFileHeaderShapeTypeOffset = 32;
        private const int ShapeFileHeaderFileLengthOffset = 24;
        private const int ShapeFileHeaderBoundingBoxOffset = 36;
        private const int DbfFileHeaderRecordCountOffset = 4;
         
        /// <summary>
        /// ShapeFileWriter Constructor
        /// </summary>
        /// <remarks>Derived classes must call this constructor</remarks>
        /// <param name="baseDir">The base directory where the 3 shapefile files will be created</param>
        /// <param name="shapeFileName">The name of the shapefile. The shapefile will be generated at baseDir + shapeFileName + ".shx|.shp|.dbf|.cpg</param>
        /// <param name="dataFields">DbfFieldDesc array describing the dbf fields of the shapefile</param>
        /// <param name="append"></param>
        protected ShapeFileWriter(string baseDir, string shapeFileName, DbfFieldDesc[] dataFields, bool append)
        {
            if(dataFields==null || dataFields.Length==0) throw new ArgumentException("dataFields can not be null or zero length");
            
            this.BaseDirectory = baseDir;        
            this.FileName = shapeFileName;
            this.dataFields = dataFields;            
            SetupFiles(append);
        }

		protected ShapeFileWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, DbfFieldDesc[] dataFields, bool append)
		{
			if (dataFields == null || dataFields.Length == 0) throw new ArgumentException("dataFields can not be null or zero length");
			this.dataFields = dataFields;
			this.usingMemoryStreams = true;
			SetupStreams(shxStream, shpStream, dbfStream, prjStream, append);
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~ShapeFileWriter()
        {
            try
            {
                Dispose(false);
            }
            catch { }
            
        }


        #region protected properties and methods

        /// <summary>
        /// The base directory where the new shape file will be written
        /// </summary>
        /// <remarks>
        /// The location of the 3 shapefiles will be written to BaseDirectory + FileName + .shp|.shx|.dbf
        /// </remarks>                
        /// <seealso cref="FileName"/>
        protected string BaseDirectory
        {
            get
            {
                return baseDirectory;
            }
            set
            {
                baseDirectory = value;
            }
        }

        /// <summary>
        /// The file name of the 3 shpaefile files, exluding their file extensions
        /// </summary>
        /// <seealso cref="BaseDirectory"/>
        protected string FileName 
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }


        /// <summary>
        /// The Stream used to write the shape files index (.shx) file
        /// </summary>
        protected Stream IndexStream
        {
            get
            {
                return this.indexStream;
            }
        }

        /// <summary>
        /// The Stream used to write the shape files .shp file
        /// </summary>
        protected Stream ShapeStream
        {
            get
            {
                return this.shapeStream;
            }
        }

        /// <summary>
        /// The Stream used to write the shape files .dbf file
        /// </summary>
        protected Stream DbfStream
        {
            get
            {
                return this.dbfStream;
            }
        }

		/// <summary>
		/// The Stream used to write the shape files .prj projection file
		/// </summary>
		protected Stream PrjStream
		{
			get
			{
				return this.prjStream;
			}
		}



		/// <summary>
		/// The shape type of the shapefile        
		/// </summary>
		/// <remarks>Derived classes should set this when creating the ShapeFileWriter</remarks>
		protected ShapeType ShapeType
        {
            get
            {
                return shapeType;
            }
            set
            {
                shapeType = value;
            }
        }


        /// <summary>
        /// The number of records written in the shapefile
        /// </summary>
        protected int RecordCount
        {
            get
            {
                return recordCount;
            }
            set
            {
                recordCount = value;
            }
        }


        /// <summary>
        /// The extent bounds of the shapefile being written
        /// </summary>
        /// <remarks>
        /// Derived classes must update the ShapeBounds as each record is written, as thie shapefile's main
        /// header will be updated with the ShapeBounds when the Close method is called.
        /// </remarks>
        protected RectangleD ShapeBounds
        {
            get
            {
                return shapeBounds;
            }
            set
            {
                shapeBounds = value;
            }
        }

        System.Text.UTF8Encoding utf8 = new UTF8Encoding();
        /// <summary>
        /// Writes a record to the Shape Files's DBF file
        /// </summary>
        /// <param name="fieldData">The data for each field in the record</param>
        protected void WriteDbfRecord(String[] fieldData)
        {
            if (fieldData == null || fieldData.Length != this.dataFields.Length)
            {
                throw new ArgumentException("fieldData length does not match dataFields length");
            }
            //write the deleted flag
            dbfStream.WriteByte(0x20); //0x20 => record is valid
            for (int n = 0; n < fieldData.Length; n++)
            {
                byte[] data = Encoding.UTF8.GetBytes(fieldData[n]);
                if (data.Length >= dataFields[n].FieldLength)
                {
                    dbfStream.Write(data, 0, dataFields[n].FieldLength);
                }
                else
                {
                    dbfStream.Write(data, 0, data.Length);
                    dbfStream.Write(PaddingString, 0, dataFields[n].FieldLength - data.Length);
                }
            }
        }


        #endregion

        #region private methods

        private DbfFieldDesc[] GetFieldDescriptions()
        {
            return this.dataFields;
        }

        private void SetupFiles(bool append)
        {
            if (append)
            {
                shapeStream = new FileStream(BaseDirectory + "\\" + FileName + ".shp", FileMode.Open, FileAccess.ReadWrite);
                indexStream = new FileStream(BaseDirectory + "\\" + FileName + ".shx", FileMode.Open, FileAccess.ReadWrite);
                dbfStream = new FileStream(BaseDirectory + "\\" + FileName + ".dbf", FileMode.Open, FileAccess.ReadWrite);
				//if we're appending we don't change the prj file
				prjStream = null;

				//read the bounding box                
				indexStream.Seek(ShapeFileHeaderBoundingBoxOffset, SeekOrigin.Begin);
                byte[] doubleBytes = new byte[8];
                double left, top, right, bottom;
                indexStream.Read(doubleBytes, 0, 8);
                left = BitConverter.ToDouble(doubleBytes, 0);
                indexStream.Read(doubleBytes, 0, 8);
                top = BitConverter.ToDouble(doubleBytes, 0);
                indexStream.Read(doubleBytes, 0, 8);
                right = BitConverter.ToDouble(doubleBytes, 0);
                indexStream.Read(doubleBytes, 0, 8);
                bottom = BitConverter.ToDouble(doubleBytes, 0);

                this.shapeBounds = RectangleD.FromLTRB(left, top, right, bottom);

                //adjust the file positions
                shapeStream.Seek(0, SeekOrigin.End);
                indexStream.Seek(0, SeekOrigin.End);
                dbfStream.Seek(-1, SeekOrigin.End); //seek to end -1 (end of file byte)

                //calculate the number of records
                //each record in the index file is 8 bytes
                this.recordCount = (int)((indexStream.Position - ShapeFileHeaderBytes.Length) >> 3);
            }
            else
            {
                shapeStream = new FileStream(BaseDirectory + "\\" + FileName + ".shp", FileMode.Create);
                indexStream = new FileStream(BaseDirectory + "\\" + FileName + ".shx", FileMode.Create);
                dbfStream = new FileStream(BaseDirectory + "\\" + FileName + ".dbf", FileMode.Create);
				prjStream = new FileStream(BaseDirectory + "\\" + FileName + ".prj", FileMode.Create);

				//write the code page file
				string cpgFilePath = BaseDirectory + "\\" + FileName + ".cpg";
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(cpgFilePath, false, System.Text.Encoding.ASCII))
                {
                    sw.Write("utf-8");
                }
            }
            
        }

		private void SetupStreams(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, bool append)
		{
			this.indexStream = shxStream;
			this.indexStream.Seek(0, SeekOrigin.Begin);
			this.dbfStream = dbfStream;
			this.dbfStream.Seek(0, SeekOrigin.Begin);
			this.shapeStream = shpStream;
			this.shapeStream.Seek(0, SeekOrigin.Begin);
			this.prjStream = prjStream;
			this.prjStream.Seek(0, SeekOrigin.Begin);

			if (append)
			{
				
				//read the bounding box                
				indexStream.Seek(ShapeFileHeaderBoundingBoxOffset, SeekOrigin.Begin);
				byte[] doubleBytes = new byte[8];
				double left, top, right, bottom;
				indexStream.Read(doubleBytes, 0, 8);
				left = BitConverter.ToDouble(doubleBytes, 0);
				indexStream.Read(doubleBytes, 0, 8);
				top = BitConverter.ToDouble(doubleBytes, 0);
				indexStream.Read(doubleBytes, 0, 8);
				right = BitConverter.ToDouble(doubleBytes, 0);
				indexStream.Read(doubleBytes, 0, 8);
				bottom = BitConverter.ToDouble(doubleBytes, 0);

				this.shapeBounds = RectangleD.FromLTRB(left, top, right, bottom);

				//adjust the file positions
				shapeStream.Seek(0, SeekOrigin.End);
				indexStream.Seek(0, SeekOrigin.End);
				dbfStream.Seek(-1, SeekOrigin.End); //seek to end -1 (end of file byte)

				//calculate the number of records
				//each record in the index file is 8 bytes
				this.recordCount = (int)((indexStream.Position - ShapeFileHeaderBytes.Length) >> 3);
			}
		
		}

		private void WriteMainHeader()
        {
            ShapeFileHeaderBytes[ShapeFileHeaderShapeTypeOffset] = (byte)ShapeType;
            shapeStream.Write(ShapeFileHeaderBytes, 0, ShapeFileHeaderBytes.Length);
        }

        private void WriteIndexHeader()
        {
            ShapeFileHeaderBytes[ShapeFileHeaderShapeTypeOffset] = (byte)ShapeType;
            indexStream.Write(ShapeFileHeaderBytes, 0, ShapeFileHeaderBytes.Length);
        }

        private void WriteDbfHeader()
        {
            int numFields = dataFields.Length;
            int recordLength = 1; //first byte used for deletion flag
            for (int n = numFields - 1; n >= 0; n--)
            {
                recordLength += dataFields[n].FieldLength;
            }

            dbfStream.WriteByte(0x03);
            DateTime cal = new DateTime();
            //write todays date
            dbfStream.WriteByte((byte)(cal.Year - 1900));
            dbfStream.WriteByte((byte)cal.Month);
            dbfStream.WriteByte((byte)cal.Day);

            //Num records in file - just write zero records when we start
            dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);
            //write length of header structure
            dbfStream.Write(BitConverter.GetBytes((short)(33 + (numFields * 32))), 0, 2);
            //write length of each record
            dbfStream.Write(BitConverter.GetBytes((short)recordLength), 0, 2);

            //write 2 reserved bytes + incomplete transaction(=0) + not encrypted(0)
            dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);

            //free record thread not used - just write 4 zero bytes
            dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);
            //multi user stuff not used - just write 8 bytes
            dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);
            dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);

            //MDX flag
            dbfStream.WriteByte(0x0);
            //language driver
            //dbfStream.WriteByte(0x57); //ANSI
            dbfStream.WriteByte(0x0); //empty - use .cpg file to specify utf-8
            //2 reserved bytes
            dbfStream.WriteByte(0x0);
            dbfStream.WriteByte(0x0);

            //now write the field descriptors
            for (int n = 0; n < numFields; n++)
            {
                //write the field name
                dbfStream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(dataFields[n].FieldName), 0, dataFields[n].FieldName.Length);
                //write string terminator (0x00) and fill with zeros
                //for any names < 10 characters long
                for (int x = 11 - dataFields[n].FieldName.Length; x > 0; x--)
                {
                    dbfStream.WriteByte(0x00);
                }
                //write the field type
                dbfStream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(dataFields[n].FieldType.ToString()), 0, 1);
                //write the field data address(just use zero)
                dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);
                //write the field length
                dbfStream.WriteByte((byte)dataFields[n].FieldLength);
                //write the decimal count(just use zero)
                dbfStream.WriteByte((byte)dataFields[n].DecimalCount);                
                //write the multi-user stuff (just use zeros)
                dbfStream.WriteByte(0x0);
                dbfStream.WriteByte(0x0);

                //write the work area id
                dbfStream.WriteByte(0x01);
                //write zeros for multi-user, SET FIELDS, reserved bytes and index field flag
                dbfStream.WriteByte(0x0);
                dbfStream.WriteByte(0x0);

                dbfStream.WriteByte(0x0);
                dbfStream.Write(BitConverter.GetBytes((long)0), 0, 8);
            }

            //write the header terminator
            dbfStream.WriteByte(0x0d);

        }

		private void WritePrjFile(string wkt)
		{
			if (this.prjStream != null)
			{
				byte[] data = System.Text.Encoding.UTF8.GetBytes(wkt);
				this.prjStream.Write(data, 0, data.Length);
			}
		}

        private void CloseShapeFile()
        {
            // update the file length (in words) and the bounding box of the entire shapefile
            // then close the stream
            try
            {
                int fileLength = (int)shapeStream.Position / 2;
                shapeStream.Seek(ShapeFileHeaderFileLengthOffset, SeekOrigin.Begin);

                int len = EndianUtils.ReadIntBE(EndianUtils.GetBytesBE(fileLength), 0);
                //System.Diagnostics.Debug.WriteLine("len = " + len);
                shapeStream.Write(EndianUtils.GetBytesBE(fileLength), 0, 4);//be
                shapeStream.Seek(ShapeFileHeaderBoundingBoxOffset, SeekOrigin.Begin);
                shapeStream.Write(BitConverter.GetBytes(ShapeBounds.Left), 0, 8);
                shapeStream.Write(BitConverter.GetBytes(ShapeBounds.Top), 0, 8);
                shapeStream.Write(BitConverter.GetBytes(ShapeBounds.Right), 0, 8);
                shapeStream.Write(BitConverter.GetBytes(ShapeBounds.Bottom), 0, 8);
            }
            finally
            {
				shapeStream.Flush();
                if(!usingMemoryStreams) shapeStream.Close();
            }
        }

        /// <summary>
        /// Closes the index file.  
        /// </summary>
        private void CloseIndexFile()
        {
            // update the file length (in words) and the bounding box of the entire shapefile
            // then close the stream
            try
            {
                int fileLength = (int)indexStream.Position / 2;
                indexStream.Seek(ShapeFileHeaderFileLengthOffset, SeekOrigin.Begin);

                indexStream.Write(EndianUtils.GetBytesBE(fileLength), 0, 4);//be
                indexStream.Seek(ShapeFileHeaderBoundingBoxOffset, SeekOrigin.Begin);
                indexStream.Write(BitConverter.GetBytes(ShapeBounds.Left), 0, 8);
                indexStream.Write(BitConverter.GetBytes(ShapeBounds.Top), 0, 8);
                indexStream.Write(BitConverter.GetBytes(ShapeBounds.Right), 0, 8);
                indexStream.Write(BitConverter.GetBytes(ShapeBounds.Bottom), 0, 8);
            }
            finally
            {
				indexStream.Flush();
				if (!usingMemoryStreams) indexStream.Close();                
            }
        }

        /// <summary>
        /// Closes the shape file's DBF file
        /// </summary>
        private void CloseDbfFile()
        {
            try
            {
                //write the end of file byte
                dbfStream.WriteByte(0x1a);
                //now update the number of records in the header                  
                dbfStream.Seek(DbfFileHeaderRecordCountOffset, SeekOrigin.Begin);
                dbfStream.Write(BitConverter.GetBytes(RecordCount), 0, 4);
            }
            finally
            {
				dbfStream.Flush();
				if (!usingMemoryStreams) dbfStream.Close();                
            }

        }

		private void ClosePrjFile()
		{
			if (prjStream != null)
			{
				prjStream.Flush();
				if (!usingMemoryStreams) prjStream.Close();
			}
		}



		#endregion


		/// <summary>
		/// writes the shapefile's 3 file headers
		/// </summary>
		/// <remarks>
		/// Derived classes must call this method after setting the ShapeType when the ShapeFileWriter is created
		/// before the first record is written
		/// </remarks>        
		protected void WriteFileHeaders()
        {
            WriteMainHeader();
            WriteIndexHeader();
            WriteDbfHeader();			
        }                    
    
    
    

        /// <summary>
        /// abstract method used to add a new record to the shapefile    
        /// </summary>
        /// <param name="points"> an array containing the individual points of the shape record, where the 
        ///  first element of the array is the x value of point 0, second element is y value of point 0 etc.
        /// </param>
        /// <param name="pointCount"> The number of points in the pts array (which may be less than points.length*2).
        ///  Note that if all elements of points are used pointCount will equal pts.length/2
        /// </param>
        /// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
        /// in the dbf file)
        ///</param>
        /// <remarks>
        /// Derived subclasses override this method to write the appropriate data
        /// depending on the ShapeType being used.
        /// </remarks>
        public abstract void AddRecord(double[] points, int pointCount, string[] fieldData);

        /// <summary>
        /// abstract method used to add a new record to the shapefile    
        /// </summary>
        /// <param name="points"> an array containing the individual points of the shape record.    
        /// </param>
        /// <param name="pointCount"> The number of points in the pts array (which may be less than points.length).
        /// </param>
        /// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
        /// in the dbf file)
        ///</param>
        ///<remarks>
        /// Implementing subclasses override this method to write the appropriate data
        /// depending on the ShapeType being used.
        /// </remarks>
        public abstract void AddRecord(PointF[] points, int pointCount, string[] fieldData);

        /// <summary>
        /// abstract method used to add a new record to the shapefile    
        /// </summary>
        /// <param name="points"> an array containing the individual points of the shape record.    
        /// </param>
        /// <param name="pointCount"> The number of points in the pts array (which may be less than points.length).
        /// </param>
        /// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
        /// in the dbf file)
        ///</param>
        ///<remarks>
        /// Implementing subclasses override this method to write the appropriate data
        /// depending on the ShapeType being used.
        /// </remarks>
        public abstract void AddRecord(PointD[] points, int pointCount, string[] fieldData);


        /// <summary>
        /// abstract method used to add a new record to the shapefile    
        /// </summary>
        /// <param name="parts"> collection of double arrays containing the individual points of each part in the shape record.    
        /// </param>
        /// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
        /// in the dbf file)
        ///</param>
        ///<remarks>
        /// Implementing subclasses override this method to write the appropriate data
        /// depending on the ShapeType being used.
        /// </remarks>
        public abstract void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<double[]> parts, string[] fieldData);

        /// <summary>
        /// abstract method used to add a new record to the shapefile    
        /// </summary>
        /// <param name="parts"> collection of float arrays containing the individual points of each part in the shape record.    
        /// </param>
        /// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
        /// in the dbf file)
        ///</param>
        ///<remarks>
        /// Implementing subclasses override this method to write the appropriate data
        /// depending on the ShapeType being used.
        /// </remarks>
        public abstract void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> parts, string[] fieldData);


        /// <summary>
        /// abstract method used to add a new record to the shapefile    
        /// </summary>
        /// <param name="parts"> collection of PointD arrays containing the individual points of each part in the shape record.    
        /// </param>
        /// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
        /// in the dbf file)
        ///</param>
        ///<remarks>
        /// Implementing subclasses override this method to write the appropriate data
        /// depending on the ShapeType being used.
        /// </remarks>
        [ObfuscationAttribute(Exclude=true)]
        public abstract void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, string[] fieldData);



		/// <summary>
		/// abstract method used to add a new record to the shapefile    
		/// </summary>
		/// <param name="points"> an array containing the individual points of the shape record.    
		/// </param>
		/// <param name="pointCount"> The number of points in the pts array (which may be less than points.length).
		/// </param>
		/// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
		/// in the dbf file)
		///</param>
		///<param name="measures">Array of measures for each point in the record</param>
		///<remarks>
		///<para>
		/// Implementing subclasses override this method to write the appropriate data
		/// depending on the ShapeType being used.
		/// </para>
		/// <para>This method is only used for a PolyLineM ShapeType</para>
		/// </remarks>

		public void AddRecord(PointD[] points, int pointCount, double[] measures, string[] fieldData)
		{
			AddRecord(points, pointCount, measures, null, fieldData);
		}

		public abstract void AddRecord(PointD[] points, int pointCount, double[] measures, double[] zValues, string[] fieldData);

		/// <summary>
		/// abstract method used to add a new record to the shapefile    
		/// </summary>
		/// <param name="parts"> collection of double arrays containing the individual points of each part in the shape record.    
		/// </param>
		/// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
		/// in the dbf file)
		///</param>
		///<remarks>
		/// Implementing subclasses override this method to write the appropriate data
		/// depending on the ShapeType being used.
		/// </remarks>
		public void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures, string[] fieldData)
		{
			AddRecord(parts, measures, null, fieldData);
		}

		/// <summary>
		/// abstract method used to add a new record to the shapefile    
		/// </summary>
		/// <param name="parts"> collection of double arrays containing the individual points of each part in the shape record.    
		/// </param>
		/// <param name="fieldData">fieldData string values of the data associated with the shape record (which is stored
		/// in the dbf file)
		///</param>
		///<param name="measures">the measures for each point in parts. Only applicable for ShapeTypes using measures</param>
		///<param name="zValues">the z values for each point in parts. Only used for PolyLineZ </param>
		///<remarks>
		/// Implementing subclasses override this method to write the appropriate data
		/// depending on the ShapeType being used.
		/// </remarks>
		public abstract void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures, System.Collections.ObjectModel.ReadOnlyCollection<double[]> zValues, string[] fieldData);


		/// <summary>
		/// Creates a ShapeFileWriter class
		/// </summary>
		/// <remarks>
		/// <para>
		/// To create a ShapeFileWriter to create a polygon shapefile pass in ShapeType.Polygon
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a point shapefile pass in ShapeType.Point
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a polyline shapefile pass in ShapeType.Polyline
		/// </para>
		/// </remarks>
		/// <param name="baseDir">The path to the base directory where the shape file should be created</param>
		/// <param name="shapeFileName">The name of the shapefile to create</param>
		/// <param name="shapeFileType">The ShapeType of the shapefile</param>
		/// <param name="dataFields">array of DbfFieldDesc objects describing the fields to be created in the shape file's DBF file</param>
		/// <returns></returns>
		public static ShapeFileWriter CreateWriter(string baseDir, string shapeFileName, ShapeType shapeFileType, DbfFieldDesc[] dataFields)
        {
            switch (shapeFileType)
            {
                case ShapeType.Polygon:
                    return new PolygonShapeFileWriter(baseDir, shapeFileName, dataFields); 
                case ShapeType.NullShape:
                    throw new ArgumentException("Can not create shapefile using NullShape ShapeType");
                case ShapeType.Point:
                    return new PointShapeFileWriter(baseDir, shapeFileName, dataFields); 
                case ShapeType.PolyLine:
                    return new PolyLineShapeFileWriter(baseDir, shapeFileName, dataFields);
                case ShapeType.PolyLineM:
                    return new PolyLineShapeFileWriter(baseDir, shapeFileName, dataFields, true, false, false);
				case ShapeType.PolyLineZ:
					return new PolyLineShapeFileWriter(baseDir, shapeFileName, dataFields, true, true, false);
                default:
                    throw new ArgumentException("Unsupported ShapeType");
            }        
        }


		/// <summary>
		/// Creates a ShapeFileWriter class
		/// </summary>
		/// <remarks>
		/// <para>
		/// To create a ShapeFileWriter to create a polygon shapefile pass in ShapeType.Polygon
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a point shapefile pass in ShapeType.Point
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a polyline shapefile pass in ShapeType.Polyline
		/// </para>
		/// </remarks>
		/// <param name="baseDir">The path to the base directory where the shape file should be created</param>
		/// <param name="shapeFileName">The name of the shapefile to create</param>
		/// <param name="shapeFileType">The ShapeType of the shapefile</param>
		/// <param name="dataFields">array of DbfFieldDesc objects describing the fields to be created in the shape file's DBF file</param>
		/// <returns></returns>
		public static ShapeFileWriter CreateWriter(string baseDir, string shapeFileName, ShapeType shapeFileType, DbfFieldDesc[] dataFields, string projectionWkt)
		{
			ShapeFileWriter writer = CreateWriter(baseDir, shapeFileName, shapeFileType, dataFields);
			if (projectionWkt != null)
			{
				writer.WritePrjFile(projectionWkt);
			}
			return writer;
		}


		/// <summary>
		/// Creates a ShapeFileWriter class
		/// </summary>
		/// <remarks>
		/// <para>
		/// To create a ShapeFileWriter to create a polygon shapefile pass in ShapeType.Polygon
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a point shapefile pass in ShapeType.Point
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a polyline shapefile pass in ShapeType.Polyline
		/// </para>
		/// </remarks>
		/// <param name="shapeFileType">The ShapeType of the shapefile</param>
		/// <param name="dataFields">array of DbfFieldDesc objects describing the fields to be created in the shape file's DBF file</param>
		/// <param name="shxStream">open stream </param>
		/// <param name="shpStream"></param>
		/// <param name="dbfStream"></param>
		/// <param name="prjStream"></param>
		/// <returns></returns>
		public static ShapeFileWriter CreateWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, ShapeType shapeFileType, DbfFieldDesc[] dataFields)
		{
			switch (shapeFileType)
			{
				case ShapeType.Polygon:
					return new PolygonShapeFileWriter(shxStream, shpStream, dbfStream, prjStream, dataFields);
				case ShapeType.NullShape:
					throw new ArgumentException("Can not create shapefile using NullShape ShapeType");
				case ShapeType.Point:
					return new PointShapeFileWriter(shxStream, shpStream, dbfStream, prjStream, dataFields, false);
				case ShapeType.PolyLine:
					return new PolyLineShapeFileWriter(shxStream, shpStream, dbfStream, prjStream, dataFields, false, false,false);
				case ShapeType.PolyLineM:
					return new PolyLineShapeFileWriter(shxStream, shpStream, dbfStream, prjStream, dataFields, true, false, false);
				case ShapeType.PolyLineZ:
					return new PolyLineShapeFileWriter(shxStream, shpStream, dbfStream, prjStream, dataFields, true, true, false);
				default:
					throw new ArgumentException("Unsupported ShapeType");
			}
		}

		/// <summary>
		/// Creates a ShapeFileWriter class
		/// </summary>
		/// <remarks>
		/// <para>
		/// To create a ShapeFileWriter to create a polygon shapefile pass in ShapeType.Polygon
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a point shapefile pass in ShapeType.Point
		/// </para>
		/// <para>
		/// To create a ShapeFileWriter to create a polyline shapefile pass in ShapeType.Polyline
		/// </para>
		/// </remarks>
		/// <param name="shapeFileType">The ShapeType of the shapefile</param>
		/// <param name="dataFields">array of DbfFieldDesc objects describing the fields to be created in the shape file's DBF file</param>
		/// <returns></returns>
		public static ShapeFileWriter CreateWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, ShapeType shapeFileType, DbfFieldDesc[] dataFields, string projectionWkt)
		{
			ShapeFileWriter writer = CreateWriter(shxStream, shpStream, dbfStream, prjStream, shapeFileType, dataFields);
			if (projectionWkt != null)
			{
				writer.WritePrjFile(projectionWkt);
			}
			return writer;
		}



		/// <summary>
		/// Creates a ShapeFileWriter class by opening an existing shapefile
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method can be used to open an existing shapefile in order to append records
		/// </para>
		/// <para>
		/// Query the returned ShapeFileWriter to obtain the ShapeType and Dbf Field Descriptions
		/// </para>
		/// </remarks>
		/// <see cref="GetFieldDescriptions()"/>
		/// <see cref="ShapeType"/>

		/// <param name="baseDir">The path to the base directory of the shape filed</param>
		/// <param name="shapeFileName">The name of the shapefile to open for appending</param>
		/// <returns></returns>
		public static ShapeFileWriter OpenWriter(string baseDir, string shapeFileName)
        {
            ShapeType shapeType = ShapeType.NullShape;
            DbfFieldDesc[] fieldDescriptions = null;
            //step 1 create and open a ShapeFile to read the shape type and field descriptions
            string fullPath = System.IO.Path.ChangeExtension(System.IO.Path.Combine(baseDir, shapeFileName), ".shp");
            using (ShapeFile sf = new ShapeFile(fullPath))
            using(DbfReader dbfReader = new DbfReader(System.IO.Path.ChangeExtension(fullPath, ".dbf")))
            {
                shapeType = sf.ShapeType;
                fieldDescriptions = dbfReader.DbfRecordHeader.GetFieldDescriptions();
            }

            switch (shapeType)
            {
                case ShapeType.Polygon:
                    return new PolygonShapeFileWriter(baseDir, shapeFileName, fieldDescriptions, true);
                case ShapeType.NullShape:
                    throw new ArgumentException("Can not create shapefile using NullShape ShapeType");
                case ShapeType.Point:
                    return new PointShapeFileWriter(baseDir, shapeFileName, fieldDescriptions, true);
                case ShapeType.PolyLine:
                    return new PolyLineShapeFileWriter(baseDir, shapeFileName, fieldDescriptions, true);
                case ShapeType.PolyLineM:
                    return new PolyLineShapeFileWriter(baseDir, shapeFileName, fieldDescriptions, true, false, true);
				case ShapeType.PolyLineZ:
					return new PolyLineShapeFileWriter(baseDir, shapeFileName, fieldDescriptions, true, true, true);
				default:
                    throw new NotSupportedException("Unsupported ShapeType");
            }
        }


        private bool closed = false;
     
        /// <summary>
        /// Updates the headers inside the individual shapefile files and closes any used streams.    
        /// </summary>
        /// <remarks>
        /// This method MUST be called after all records have been added to the shapefile.
        /// Failure to call this method will result in a shapefile being generated with incorrect
        /// headers
        /// </remarks>
        public void Close()
        {
            if (!closed)
            {
                try
                {
                    CloseIndexFile();                    
                }
                finally
                {                    
                    try
                    {
                        CloseShapeFile();
                    }
                    finally
                    {
                        try
                        {
                            CloseDbfFile();
                        }
                        finally
                        {
							try
							{
								ClosePrjFile();
							}
							finally
							{
								closed = true;
							}
                        }
                    }                                        
                }
            }
        }


        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by the DbfWriter
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed) //dispose managed resources
            {                                    
                if(disposing) Close();
                disposed = true;
            }
        }

        #endregion
        
        
        //Used for padding field data with spaces if neccessary
        static byte[] PaddingString = {0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
                0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20
        };
    
        
    
}

    [ObfuscationAttribute(Exclude = true, ApplyToMembers = true)]
    class PolygonShapeFileWriter : ShapeFileWriter
{

    internal PolygonShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields, bool append)
        :base (baseDir, shapeFileName, datafields, append)
    {
        this.ShapeType = ShapeType.Polygon;       
        if(!append) WriteFileHeaders();
        
    }

    internal PolygonShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields)
        : this(baseDir,shapeFileName, datafields, false) 
    {        
    }

	internal PolygonShapeFileWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, DbfFieldDesc[] datafields, bool append)
			: base(shxStream, shpStream, dbfStream, prjStream, datafields, append)
	{
		this.ShapeType = ShapeType.Polygon;
		if (!append) WriteFileHeaders();
	}

	internal PolygonShapeFileWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, DbfFieldDesc[] datafields)
		: this(shxStream, shpStream, dbfStream, prjStream, datafields, false)
	{		
	}


	public override void AddRecord(double[] points, int pointCount, String[] fieldData)
    {
        RecordCount++;
        writeShapeRecord5(points, pointCount);
        WriteDbfRecord(fieldData);
    }

    public override void AddRecord(PointF[] pts, int numPoints, string[] fieldData)
    {
        RecordCount++;
        writeShapeRecord2(pts, numPoints);
        WriteDbfRecord(fieldData);
    }

    public override void AddRecord(PointD[] pts, int numPoints, string[] fieldData)
    {
        RecordCount++;
        writeShapeRecord4(pts, numPoints);
        WriteDbfRecord(fieldData);
    }

    public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> parts, string[] fieldData)
    {
        RecordCount++;
        writeShapeRecord1(parts);
        WriteDbfRecord(fieldData);
    }

    public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, string[] fieldData)
    {
        RecordCount++;
        writeShapeRecord3(parts);
        WriteDbfRecord(fieldData);
    }

    public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<double[]> parts, string[] fieldData)
    {
        throw new NotImplementedException();
    }

    public override void AddRecord(PointD[] points, int pointCount, double[] measures, double[] zValues, string[] fieldData)
    {
        throw new NotSupportedException("Not supported for polygon shapefiles");
    }


    public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures, System.Collections.ObjectModel.ReadOnlyCollection<double[]> zValues, string[] fieldData)
    {
        throw new NotSupportedException("Not supported for polygon shapefiles");
    }


    private const int ShapeTypeLE = ((0x05)<<24);


    private void writeShapeRecord1(System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> parts)
    {

        int numPoints = 0;
        for(int n=0;n<parts.Count;n++)
        {
            numPoints += (parts[n].Length);
        }

        int recordOffset = (int)ShapeStream.Position / 2;

        //output the record number
        ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
        //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + (4*numparts) + 16*numPoints]/2
        int contentLength = (4 + 32 + 4 + 4 + (4*parts.Count) + (16 * numPoints)) / 2;
        ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        //write the shapeType (LE)
        ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Polygon), 0, 4);

        //calculate and write the bounding box
        double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
        int index = 0;
            
        //for (int n = 0; n < numPoints; n++)
        for (int n = 0; n < parts.Count; n++)
        {
            index=0;
            PointF[] pts = parts[n];
            for (int i = 0; i < pts.Length; i++)
            {
                double x = pts[index].X;
                double y = pts[index++].Y;
                if (double.IsInfinity(x) || double.IsInfinity(y)) continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
        ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

        //update the entire shapefile bounds
        if (RecordCount == 1)
        {
            ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
        }
        else
        {
            ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
        }
        //write number of parts
        ShapeStream.Write(BitConverter.GetBytes((int)parts.Count), 0, 4);
        //write number of points
        ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
        //write part offsets
        int off = 0;
        for (int n = 0; n < parts.Count; n++)
        {
            ShapeStream.Write(BitConverter.GetBytes((int)off), 0, 4);
            off += (parts[n].Length);
        }
        //now write the point data
        for (int n = 0; n < parts.Count; n++)
        {
            index = 0;
            PointF[] pts = parts[n];
            for (int i = 0; i < pts.Length; i++)
            {
                ShapeStream.Write(BitConverter.GetBytes((double)pts[index].X), 0, 8);
                ShapeStream.Write(BitConverter.GetBytes((double)pts[index++].Y), 0, 8);
            }
        }

        //now write to the index file
        IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
        IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
    }

    private void writeShapeRecord2(PointF[] pts, int numPoints)
    {
        
        int recordOffset = (int)ShapeStream.Position/2;
        
        //output the record number
        ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
        //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
        int contentLength = (4 + 32 + 4 + 4 + 4 + (16*numPoints))/2;
        ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
        
        //write the shapeType (LE)
        ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Polygon),0,4);
        
        //calculate and write the bounding box
        double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;        
        int index=0;
        for(int n=0; n<numPoints;n++)
        {
            double x = pts[index].X;
            double y = pts[index++].Y;
            if (double.IsInfinity(x) || double.IsInfinity(y)) continue;
            if (x<minX) minX = x;
            if(x>maxX) maxX = x;
            if(y<minY) minY = y;
            if(y>maxY) maxY = y;            
        }
        ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);
        
        //update the entire shapefile bounds
        if(RecordCount == 1)
        {
            ShapeBounds = new RectangleD(minX, minY, (maxX-minX), (maxY-minY));
        }
        else
        {
            ShapeBounds = RectangleD.Union(ShapeBounds,new RectangleD(minX, minY, (maxX-minX), (maxY-minY)));
        }
        //write number of parts
        ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
        //write number of points
        ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
        //write part offsets
        ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);
        
        //now write the point data
        index=0;
        for(int n=0; n<numPoints;n++)
        {
            ShapeStream.Write(BitConverter.GetBytes((double)pts[index].X), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes((double)pts[index++].Y), 0, 8);            
        }
        
        //now write to the index file
        IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
        IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);        
    }

    private void writeShapeRecord3(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts)
    {
        int numPoints = 0;
        for (int n = 0; n < parts.Count; n++)
        {
            numPoints += (parts[n].Length);
        }

        int recordOffset = (int)ShapeStream.Position / 2;

        //output the record number
        ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
        //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + (4*numparts) + 16*numPoints]/2
        int contentLength = (4 + 32 + 4 + 4 + (4 * parts.Count) + (16 * numPoints)) / 2;
        ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        //write the shapeType (LE)
        ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Polygon), 0, 4);

        //calculate and write the bounding box
        double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
        int index = 0;

        //for (int n = 0; n < numPoints; n++)
        for (int n = 0; n < parts.Count; n++)
        {
            index = 0;
            PointD[] pts = parts[n];
            for (int i = 0; i < pts.Length; i++)
            {
                double x = pts[index].X;
                double y = pts[index++].Y;
                if (double.IsInfinity(x) || double.IsInfinity(y)) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
        //Console.Out.WriteLine("minX:{0},minY:{1}, maxX:{2},maxY:{3}", minX, minY, maxX, maxY);
        ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);
        //update the entire shapefile bounds
        if (RecordCount == 1)
        {
            ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
        }
        else
        {
            ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
        }
        //write number of parts
        ShapeStream.Write(BitConverter.GetBytes((int)parts.Count), 0, 4);
        //write number of points
        ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
        //write part offsets
        int off = 0;
        for (int n = 0; n < parts.Count; n++)
        {
            ShapeStream.Write(BitConverter.GetBytes((int)off), 0, 4);
            off += (parts[n].Length);
        }
        //now write the point data
        for (int n = 0; n < parts.Count; n++)
        {
            index = 0;
            PointD[] pts = parts[n];
            for (int i = 0; i < pts.Length; i++)
            {
                ShapeStream.Write(BitConverter.GetBytes(pts[index].X), 0, 8);
                ShapeStream.Write(BitConverter.GetBytes(pts[index++].Y), 0, 8);
            }
        }

        //now write to the index file
        IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
        IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
    }

    private void writeShapeRecord4(PointD[] pts, int numPoints)
    {

        int recordOffset = (int)ShapeStream.Position / 2;

        //output the record number
        ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
        //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
        int contentLength = (4 + 32 + 4 + 4 + 4 + (16 * numPoints)) / 2;
        ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        //write the shapeType (LE)
        ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Polygon), 0, 4);

        //calculate and write the bounding box
        double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
        int index = 0;
        for (int n = 0; n < numPoints; n++)
        {
            double x = pts[index].X;
            double y = pts[index++].Y;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

        //update the entire shapefile bounds
        if (RecordCount == 1)
        {
            ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
        }
        else
        {
            ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
        }
        //write number of parts
        ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
        //write number of points
        ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
        //write part offsets
        ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);

        //now write the point data
        index = 0;
        for (int n = 0; n < numPoints; n++)
        {
            ShapeStream.Write(BitConverter.GetBytes((double)pts[index].X), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes((double)pts[index++].Y), 0, 8);
        }

        //now write to the index file
        IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
        IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
    }


    private void writeShapeRecord5(double[] pts, int numPoints)
    {

        int recordOffset = (int)ShapeStream.Position / 2;

        //output the record number
        ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
        //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
        int contentLength = (4 + 32 + 4 + 4 + 4 + (16 * numPoints)) / 2;
        ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        //write the shapeType (LE)
        ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Polygon), 0, 4);

        //calculate and write the bounding box
        double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
        int index = 0;
        for (int n = 0; n < numPoints; n++)
        {
            double x = pts[index++];
            double y = pts[index++];
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

        //update the entire shapefile bounds
        if (RecordCount == 1)
        {
            ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
        }
        else
        {
            ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
        }
        //write number of parts
        ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
        //write number of points
        ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
        //write part offsets
        ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);

        //now write the point data
        index = 0;
        for (int n = 0; n < numPoints; n++)
        {
            ShapeStream.Write(BitConverter.GetBytes(pts[index++]), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(pts[index++]), 0, 8);

        }

        //now write to the index file
        IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
        IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
    }

    
}

    [ObfuscationAttribute(Exclude = true, ApplyToMembers = true)]
    class PolyLineShapeFileWriter : ShapeFileWriter
    {
        bool useMeasures = false;
		bool useZValues = false;

        internal PolyLineShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields, bool append)
            :base(baseDir, shapeFileName, datafields, append)
        {        
            this.ShapeType = ShapeType.PolyLine;       
            if(!append) WriteFileHeaders();        
        }

        internal PolyLineShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields)
            : this(baseDir, shapeFileName, datafields, false)
        {            
        }

        internal PolyLineShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields, bool useMeasures, bool useZValues, bool append)
            : base(baseDir, shapeFileName, datafields, append)
        {
            this.useMeasures = useMeasures;
			this.useZValues = useZValues;
            this.ShapeType = useZValues ? ShapeType.PolyLineZ : (useMeasures ? ShapeType.PolyLineM : ShapeType.PolyLine);
            if (!append) WriteFileHeaders();
        }

		internal PolyLineShapeFileWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, DbfFieldDesc[] datafields, bool useMeasures, bool useZValues, bool append)
			: base(shxStream, shpStream, dbfStream, prjStream, datafields, append)
		{
			this.useMeasures = useMeasures;
			this.useZValues = useZValues;
			this.ShapeType = useZValues ? ShapeType.PolyLineZ : (useMeasures ? ShapeType.PolyLineM : ShapeType.PolyLine);
			if (!append) WriteFileHeaders();
		}


		public override void AddRecord(double[] pts, int numPoints, String[] fieldData)
        {
            RecordCount++;
            writeShapeRecord3(pts, numPoints);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(PointF[] pts, int numPoints, string[] fieldData)
        {
            RecordCount++;
            writeShapeRecord1(pts, numPoints);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(PointD[] pts, int numPoints, string[] fieldData)
        {
            RecordCount++;
            writeShapeRecord2(pts, numPoints, null);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(PointD[] points, int pointCount, double[] measures, double[] zValues, string[] fieldData)
        {
            RecordCount++;
			if (this.useZValues)
			{
				writeShapeRecord2(points, pointCount, measures, zValues);
			}
			else
			{
				writeShapeRecord2(points, pointCount, measures);
			}
            WriteDbfRecord(fieldData);

        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> parts, string[] fieldData)
        {
            RecordCount++;
            writeShapeRecord4(parts);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, string[] fieldData)
        {
            RecordCount++;
            //obfuscation was casting parts to following line - renamed private methods
            //writeShapeRecord4((System.Collections.ObjectModel.ReadOnlyCollection<PointF[]>)parts);            
            writeShapeRecord5(parts, null);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<double[]> parts, string[] fieldData)
        {
            throw new NotImplementedException();
        }


        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures, System.Collections.ObjectModel.ReadOnlyCollection<double[]> zValues, string[] fieldData)
        {
            RecordCount++;
			if (this.useZValues)
			{
				writeShapeRecord5(parts, measures, zValues);
			}
			else
			{
				writeShapeRecord5(parts, measures);
			}
            WriteDbfRecord(fieldData);
        }


        private void writeShapeRecord1(PointF[] pts, int numPoints)
        {

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
            int contentLength = (4 + 32 + 4 + 4 + 4 + (16 * numPoints)) / 2;
            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.PolyLine), 0, 4);

            //calculate and write the bounding box
            double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
            int index = 0;
            for (int n = 0; n < numPoints; n++)
            {
                double x = pts[index].X;
                double y = pts[index++].Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
            }
            else
            {
                ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
            }
            //write number of parts
            ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
            //write number of points
            ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
            //write part offsets
            ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);

            //now write the point data
            index = 0;
            for (int n = 0; n < numPoints; n++)
            {
                ShapeStream.Write(BitConverter.GetBytes((double)pts[index].X), 0, 8);
                ShapeStream.Write(BitConverter.GetBytes((double)pts[index++].Y), 0, 8);

            }
            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        }

        private void writeShapeRecord2(PointD[] pts, int numPoints, double[] measures)
        {

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
            int contentLength = (4 + 32 + 4 + 4 + 4 + (16 * numPoints)) / 2;

            if (this.useMeasures && measures != null)
            {
                contentLength += (8 + 8 + (8 * numPoints)) / 2;
            }

            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.PolyLine), 0, 4);

            //calculate and write the bounding box
            double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
            int index = 0;
            for (int n = 0; n < numPoints; n++)
            {
                double x = pts[index].X;
                double y = pts[index++].Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
            }
            else
            {
                ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
            }
            //write number of parts
            ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
            //write number of points
            ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
            //write part offsets
            ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);

            //now write the point data
            index = 0;
            for (int n = 0; n < numPoints; n++)
            {
                ShapeStream.Write(BitConverter.GetBytes(pts[index].X), 0, 8);
                ShapeStream.Write(BitConverter.GetBytes(pts[index++].Y), 0, 8);

            }

            if (this.useMeasures && measures != null)
            {
                //write the measures;
                double minMeasure = double.MaxValue, maxMeasure = double.MinValue;
                for (int n = 0; n < numPoints; ++n)
                {
                    if (measures[n] < minMeasure) minMeasure = measures[n];
                    if (measures[n] > maxMeasure) maxMeasure = measures[n];
                }
                ShapeStream.Write(BitConverter.GetBytes(minMeasure), 0, 8);
                ShapeStream.Write(BitConverter.GetBytes(maxMeasure), 0, 8);
                for (int n = 0; n < numPoints; ++n)
                {
                    ShapeStream.Write(BitConverter.GetBytes(measures[n]), 0, 8);                   
                }
            }

            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        }

		private void writeShapeRecord2(PointD[] pts, int numPoints, double[] measures, double[] zValues)
		{

			int recordOffset = (int)ShapeStream.Position / 2;

			//output the record number
			ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
			//output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
			int contentLength = (4 + 32 + 4 + 4 + 4 + (16 * numPoints)) / 2;

			if (this.useZValues && zValues != null)
			{
				contentLength += (8 + 8 + (8 * numPoints)) / 2;
			}
			if (this.useMeasures && measures != null)
			{
				contentLength += (8 + 8 + (8 * numPoints)) / 2;
			}

			ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

			//write the shapeType (LE)
			ShapeStream.Write(BitConverter.GetBytes((int)this.ShapeType), 0, 4);

			//calculate and write the bounding box
			double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
			int index = 0;
			for (int n = 0; n < numPoints; n++)
			{
				double x = pts[index].X;
				double y = pts[index++].Y;
				if (x < minX) minX = x;
				if (x > maxX) maxX = x;
				if (y < minY) minY = y;
				if (y > maxY) maxY = y;
			}
			ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
			ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
			ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
			ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

			//update the entire shapefile bounds
			if (RecordCount == 1)
			{
				ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
			}
			else
			{
				ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
			}
			//write number of parts
			ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
			//write number of points
			ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
			//write part offsets
			ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);

			//now write the point data
			index = 0;
			for (int n = 0; n < numPoints; n++)
			{
				ShapeStream.Write(BitConverter.GetBytes(pts[index].X), 0, 8);
				ShapeStream.Write(BitConverter.GetBytes(pts[index++].Y), 0, 8);

			}

			if (this.useZValues && zValues != null)
			{
				//write the measures;
				double minZ = double.MaxValue, maxZ = double.MinValue;
				for (int n = 0; n < numPoints; ++n)
				{
					if (zValues[n] < minZ) minZ = zValues[n];
					if (zValues[n] > maxZ) maxZ = zValues[n];
				}
				ShapeStream.Write(BitConverter.GetBytes(minZ), 0, 8);
				ShapeStream.Write(BitConverter.GetBytes(maxZ), 0, 8);
				for (int n = 0; n < numPoints; ++n)
				{
					ShapeStream.Write(BitConverter.GetBytes(zValues[n]), 0, 8);
				}
			}

			if (this.useMeasures && measures != null)
			{
				//write the measures;
				double minMeasure = double.MaxValue, maxMeasure = double.MinValue;
				for (int n = 0; n < numPoints; ++n)
				{
					if ( measures[n] < minMeasure) minMeasure = measures[n];
					if ( measures[n] > maxMeasure) maxMeasure = measures[n];
				}
				ShapeStream.Write(BitConverter.GetBytes(minMeasure), 0, 8);
				ShapeStream.Write(BitConverter.GetBytes(maxMeasure), 0, 8);
				for (int n = 0; n < numPoints; ++n)
				{
					ShapeStream.Write(BitConverter.GetBytes(measures[n]), 0, 8);
				}
			}

			//now write to the index file
			IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
			IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

		}


		private void writeShapeRecord3(double[] pts, int numPoints) 
    {

        int recordOffset = (int)ShapeStream.Position / 2;

        //output the record number
        ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
        //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
        int contentLength = (4 + 32 + 4 + 4 + 4 + (16 * numPoints)) / 2;
        ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        //write the shapeType (LE)
        ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.PolyLine), 0, 4);

        //calculate and write the bounding box
        double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
        int index = 0;
        for (int n = 0; n < numPoints; n++)
        {
            double x = pts[index++];
            double y = pts[index++];
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
        ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

        //update the entire shapefile bounds
        if (RecordCount == 1)
        {
            ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
        }
        else
        {
            ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
        }
        //write number of parts
        ShapeStream.Write(BitConverter.GetBytes((int)1), 0, 4);
        //write number of points
        ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
        //write part offsets
        ShapeStream.Write(BitConverter.GetBytes((int)0), 0, 4);

        //now write the point data
        index = 0;
        for (int n = 0; n < numPoints; n++)
        {
            ShapeStream.Write(BitConverter.GetBytes(pts[index++]), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(pts[index++]), 0, 8);

        }
        //now write to the index file
        IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
        IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);                           
        
    }

        private void writeShapeRecord4(System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> parts)
        {

            int numPoints = 0;
            for (int n = 0; n < parts.Count; n++)
            {
                numPoints += (parts[n].Length);
            }

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + (4*numparts) + 16*numPoints]/2
            int contentLength = (4 + 32 + 4 + 4 + (4 * parts.Count) + (16 * numPoints)) / 2;
            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.PolyLine), 0, 4);

            //calculate and write the bounding box
            double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
            int index = 0;

            //for (int n = 0; n < numPoints; n++)
            for (int n = 0; n < parts.Count; n++)
            {
                index = 0;
                PointF[] pts = parts[n];
                for (int i = 0; i < pts.Length; i++)
                {
                    double x = pts[index].X;
                    double y = pts[index++].Y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
            }
            else
            {
                ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
            }
            //write number of parts
            ShapeStream.Write(BitConverter.GetBytes((int)parts.Count), 0, 4);
            //write number of points
            ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
            //write part offsets
            int off = 0;
            for (int n = 0; n < parts.Count; n++)
            {
                ShapeStream.Write(BitConverter.GetBytes((int)off), 0, 4);
                off += (parts[n].Length);
            }
            //now write the point data
            for (int n = 0; n < parts.Count; n++)
            {
                index = 0;
                PointF[] pts = parts[n];
                for (int i = 0; i < pts.Length; i++)
                {
                    ShapeStream.Write(BitConverter.GetBytes((double)pts[index].X), 0, 8);
                    ShapeStream.Write(BitConverter.GetBytes((double)pts[index++].Y), 0, 8);
                }
            }

            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
        }


        private void writeShapeRecord5(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures)
        {

            int numPoints = 0;
            for (int n = 0; n < parts.Count; n++)
            {
                numPoints += (parts[n].Length);
            }

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + (4*numparts) + 16*numPoints]/2
            int contentLength = (4 + 32 + 4 + 4 + (4 * parts.Count) + (16 * numPoints)) / 2;

            if (this.useMeasures && measures != null)
            {
                contentLength += (8 + 8 + (8 * numPoints)) / 2;
            }

            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.PolyLine), 0, 4);

            //calculate and write the bounding box
            double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
            int index = 0;

            //for (int n = 0; n < numPoints; n++)
            for (int n = 0; n < parts.Count; n++)
            {
                index = 0;
                PointD[] pts = parts[n];
                for (int i = 0; i < pts.Length; i++)
                {
                    double x = pts[i].X;
                    double y = pts[i].Y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
           
            ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
            }
            else
            {
                if (numPoints > 0)
                {
                    ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
                }
            }

            //Console.Out.WriteLine("Record : " + RecordCount + ", ShapeBounds: " + ShapeBounds);

            //write number of parts
            ShapeStream.Write(BitConverter.GetBytes((int)parts.Count), 0, 4);
            //write number of points
            ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
            //write part offsets
            int off = 0;
            for (int n = 0; n < parts.Count; n++)
            {
                ShapeStream.Write(BitConverter.GetBytes((int)off), 0, 4);
                off += (parts[n].Length);
            }
            //now write the point data
            for (int n = 0; n < parts.Count; n++)
            {
                PointD[] pts = parts[n];
                for (int i = 0; i < pts.Length; i++)
                {
                    ShapeStream.Write(BitConverter.GetBytes((double)pts[i].X), 0, 8);
                    ShapeStream.Write(BitConverter.GetBytes((double)pts[i].Y), 0, 8);
                }
            }

            if (this.useMeasures && measures != null)
            {
                //write the measures;
                double minMeasure = double.MaxValue, maxMeasure = double.MinValue;
                foreach (double[] measuresPart in measures)
                {
                    for (int n = 0; n < measuresPart.Length; ++n)
                    {
                        if (measuresPart[n] < minMeasure) minMeasure = measuresPart[n];
                        if (measuresPart[n] > maxMeasure) maxMeasure = measuresPart[n];
                    }
                }
                ShapeStream.Write(BitConverter.GetBytes(minMeasure), 0, 8);
                ShapeStream.Write(BitConverter.GetBytes(maxMeasure), 0, 8);

                foreach (double[] measuresPart in measures)
                {
                    for (int n = 0; n < measuresPart.Length; ++n)
                    {
                        ShapeStream.Write(BitConverter.GetBytes(measuresPart[n]), 0, 8);
                    }
                }
            }

            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
        }

		private void writeShapeRecord5(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures, System.Collections.ObjectModel.ReadOnlyCollection<double[]> zValues)
		{

			int numPoints = 0;
			for (int n = 0; n < parts.Count; n++)
			{
				numPoints += (parts[n].Length);
			}

			int recordOffset = (int)ShapeStream.Position / 2;

			//output the record number
			ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
			//output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + (4*numparts) + 16*numPoints]/2
			int contentLength = (4 + 32 + 4 + 4 + (4 * parts.Count) + (16 * numPoints)) / 2;

			if (this.useZValues && zValues != null)
			{
				contentLength += (8 + 8 + (8 * numPoints)) / 2;
			}

			if (this.useMeasures && measures != null && measures.Count > 0)
			{
				contentLength += (8 + 8 + (8 * numPoints)) / 2;
			}			

			ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

			//write the shapeType (LE)
			ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.PolyLine), 0, 4);

			//calculate and write the bounding box
			double minX = Double.PositiveInfinity, minY = Double.PositiveInfinity, maxX = Double.NegativeInfinity, maxY = Double.NegativeInfinity;
			
			for (int n = 0; n < parts.Count; n++)
			{
				PointD[] pts = parts[n];
				for (int i = 0; i < pts.Length; i++)
				{
					double x = pts[i].X;
					double y = pts[i].Y;
					if (x < minX) minX = x;
					if (x > maxX) maxX = x;
					if (y < minY) minY = y;
					if (y > maxY) maxY = y;
				}
			}

			ShapeStream.Write(BitConverter.GetBytes(minX), 0, 8);
			ShapeStream.Write(BitConverter.GetBytes(minY), 0, 8);
			ShapeStream.Write(BitConverter.GetBytes(maxX), 0, 8);
			ShapeStream.Write(BitConverter.GetBytes(maxY), 0, 8);

			//update the entire shapefile bounds
			if (RecordCount == 1)
			{
				ShapeBounds = new RectangleD(minX, minY, (maxX - minX), (maxY - minY));
			}
			else
			{
				if (numPoints > 0)
				{
					ShapeBounds = RectangleD.Union(ShapeBounds, new RectangleD(minX, minY, (maxX - minX), (maxY - minY)));
				}
			}

			//Console.Out.WriteLine("Record : " + RecordCount + ", ShapeBounds: " + ShapeBounds);

			//write number of parts
			ShapeStream.Write(BitConverter.GetBytes((int)parts.Count), 0, 4);
			//write number of points
			ShapeStream.Write(BitConverter.GetBytes(numPoints), 0, 4);
			//write part offsets
			int off = 0;
			for (int n = 0; n < parts.Count; n++)
			{
				ShapeStream.Write(BitConverter.GetBytes((int)off), 0, 4);
				off += (parts[n].Length);
			}
			//now write the point data
			for (int n = 0; n < parts.Count; n++)
			{
				PointD[] pts = parts[n];
				for (int i = 0; i < pts.Length; i++)
				{
					ShapeStream.Write(BitConverter.GetBytes((double)pts[i].X), 0, 8);
					ShapeStream.Write(BitConverter.GetBytes((double)pts[i].Y), 0, 8);
				}
			}

			if (this.useZValues && zValues != null)
			{
				//write the measures;
				double minZ = double.MaxValue, maxZ = double.MinValue;
				foreach (double[] zPart in zValues)
				{
					for (int n = 0; n < zPart.Length; ++n)
					{
						if (zPart[n] < minZ ) minZ = zPart[n];
						if (zPart[n] > maxZ) maxZ = zPart[n];
					}
				}
				ShapeStream.Write(BitConverter.GetBytes(minZ), 0, 8);
				ShapeStream.Write(BitConverter.GetBytes(maxZ), 0, 8);

				foreach (double[] zPart in zValues)
				{
					for (int n = 0; n < zPart.Length; ++n)
					{
						ShapeStream.Write(BitConverter.GetBytes(zPart[n]), 0, 8);
					}
				}
			}


			if (this.useMeasures && measures != null && measures.Count > 0)
			{
				//write the measures;
				double minMeasure = double.MaxValue, maxMeasure = double.MinValue;
				foreach (double[] measuresPart in measures)
				{
					for (int n = 0; n < measuresPart.Length; ++n)
					{
						if (measuresPart[n] < minMeasure) minMeasure = measuresPart[n];
						if ( measuresPart[n] > maxMeasure) maxMeasure = measuresPart[n];
					}
				}
				ShapeStream.Write(BitConverter.GetBytes(minMeasure), 0, 8);
				ShapeStream.Write(BitConverter.GetBytes(maxMeasure), 0, 8);

				foreach (double[] measuresPart in measures)
				{
					for (int n = 0; n < measuresPart.Length; ++n)
					{
						ShapeStream.Write(BitConverter.GetBytes(measuresPart[n]), 0, 8);
					}
				}
			}

			//now write to the index file
			IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
			IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);
		}


	}

	[ObfuscationAttribute(Exclude = true, ApplyToMembers = true)]
    class PointShapeFileWriter : ShapeFileWriter
{

    internal PointShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields, bool append)
        : base(baseDir, shapeFileName, datafields, append)
    {
        this.ShapeType = ShapeType.Point;
        if(!append) WriteFileHeaders();
    }

    internal PointShapeFileWriter(String baseDir, String shapeFileName, DbfFieldDesc[] datafields)
        : this(baseDir, shapeFileName, datafields, false)
    {        
    }

	internal PointShapeFileWriter(Stream shxStream, Stream shpStream, Stream dbfStream, Stream prjStream, DbfFieldDesc[] datafields, bool append)
		: base(shxStream, shpStream, dbfStream, prjStream, datafields, append)
	{
			this.ShapeType = ShapeType.Point;
			if (!append) WriteFileHeaders();
	}

	public override void AddRecord(double[] pts, int numPoints, String[] fieldData)
    {
        RecordCount++;
        WriteShapeRecord1(pts, numPoints);
        WriteDbfRecord(fieldData);
    }



        public override void AddRecord(PointF[] pts, int numPoints, string[] fieldData)
        {
            RecordCount++;
            WriteShapeRecord2(pts, numPoints);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(PointD[] pts, int numPoints, string[] fieldData)
        {
            RecordCount++;
            WriteShapeRecord3(pts, numPoints);
            WriteDbfRecord(fieldData);
        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointF[]> parts, string[] fieldData)
        {
            throw new NotSupportedException("Point Shapes do not support multi parts");
        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, string[] fieldData)
        {
            if(parts.Count > 0) AddRecord(parts[0], 1, fieldData);
        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<double[]> parts, string[] fieldData)
        {
            throw new NotSupportedException("Point Shapes do not support multi parts");
        }

        public override void AddRecord(PointD[] points, int pointCount, double[] measures, double[] zValues, string[] fieldData)
        {
            throw new NotSupportedException("Point Shapes do not support measures");
        }

        public override void AddRecord(System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> parts, System.Collections.ObjectModel.ReadOnlyCollection<double[]> measures, System.Collections.ObjectModel.ReadOnlyCollection<double[]> zValues, string[] fieldData)
        {
            throw new NotSupportedException("Point Shapes do not support measures");
        }


        private void WriteShapeRecord1(double[] pts, int numPoints)
        {

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
            int contentLength = (4 + 16 ) / 2;
            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Point), 0, 4);

        
        
            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(pts[0], pts[1], double.Epsilon, double.Epsilon);
            }
            else
            {
                double minX = Math.Min(ShapeBounds.Left, pts[0]);
                double maxX = Math.Max(ShapeBounds.Right, pts[0]);
                double minY = Math.Min(ShapeBounds.Top, pts[1]);
                double maxY = Math.Max(ShapeBounds.Bottom, pts[1]);

                ShapeBounds = RectangleD.FromLTRB(minX, minY, maxX, maxY);
            }
        
        
            ShapeStream.Write(BitConverter.GetBytes(pts[0]), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes(pts[1]), 0, 8);

            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        }

        private void WriteShapeRecord2(PointF[] pts, int numPoints)
        {

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
            int contentLength = (4 + 16) / 2;
            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Point), 0, 4);



            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(pts[0].X, pts[0].Y, double.Epsilon, double.Epsilon);
            }
            else
            {
                double minX = Math.Min(ShapeBounds.Left, pts[0].X);
                double maxX = Math.Max(ShapeBounds.Right, pts[0].X);
                double minY = Math.Min(ShapeBounds.Top, pts[0].Y);
                double maxY = Math.Max(ShapeBounds.Bottom, pts[0].Y);
                ShapeBounds = RectangleD.FromLTRB(minX, minY, maxX, maxY);
            }


            ShapeStream.Write(BitConverter.GetBytes((double)pts[0].X), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes((double)pts[0].Y), 0, 8);

            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        }

        private void WriteShapeRecord3(PointD[] pts, int numPoints)
        {

            int recordOffset = (int)ShapeStream.Position / 2;

            //output the record number
            ShapeStream.Write(EndianUtils.GetBytesBE(RecordCount), 0, 4);
            //output the content length in words = [ 4 (shapetype) + 32 (box) + 4 + 4 + 4 + 16*numPoints]/2
            int contentLength = (4 + 16) / 2;
            ShapeStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

            //write the shapeType (LE)
            ShapeStream.Write(BitConverter.GetBytes((int)EGIS.ShapeFileLib.ShapeType.Point), 0, 4);



            //update the entire shapefile bounds
            if (RecordCount == 1)
            {
                ShapeBounds = new RectangleD(pts[0].X, pts[0].Y, double.Epsilon, double.Epsilon);
            }
            else
            {
                double minX = Math.Min(ShapeBounds.Left, pts[0].X);
                double maxX = Math.Max(ShapeBounds.Right, pts[0].X);
                double minY = Math.Min(ShapeBounds.Top, pts[0].Y);
                double maxY = Math.Max(ShapeBounds.Bottom, pts[0].Y);

                ShapeBounds = RectangleD.FromLTRB(minX, minY, maxX, maxY);
            }


            ShapeStream.Write(BitConverter.GetBytes((double)pts[0].X), 0, 8);
            ShapeStream.Write(BitConverter.GetBytes((double)pts[0].Y), 0, 8);

            //now write to the index file
            IndexStream.Write(EndianUtils.GetBytesBE(recordOffset), 0, 4);
            IndexStream.Write(EndianUtils.GetBytesBE(contentLength), 0, 4);

        }

    
}



}
