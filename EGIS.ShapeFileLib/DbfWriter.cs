#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
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

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// a DBF File Writer class
    /// </summary>
    /// <remarks>
    /// The DbfWriter class can be used to create or edit a DBF file
    /// <para>
    /// The class is used by the ShapeFileWriter class to create new shapefiles. The class can also be used in conjunction with 
    /// a DbfReader to edit a DBF file by filtering required records or adding/removing new columns
    /// </para>
    /// </remarks>
    /// <seealso cref="EGIS.ShapeFileLib.DbfReader"/>
    /// <seealso cref="EGIS.ShapeFileLib.ShapeFileWriter"/>
    public sealed class DbfWriter : IDisposable
    {
        private String fileName;    
        private DbfFieldDesc[] dataFields;
        private int numRecords;
    
        private System.IO.FileStream dbfStream;

        /// <summary>
        /// Creates a new DbfWriter
        /// </summary>
        /// <param name="filePath">The path to the new DBF file to be created. If filePath does not include the ".dbf" extension it will be added</param>
        /// <param name="dataFields">A DbfFieldDesc aray describing the field of the DBF file</param>
        public DbfWriter(String filePath, DbfFieldDesc[] dataFields)
        {
            if(dataFields==null || dataFields.Length==0) throw new ArgumentException("datafields can not be null or zero length");

            this.fileName = filePath;
            this.dataFields = dataFields;
            
            setupStream();
            writeHeader();
        }
    
        private void setupStream()
        {            
            string dbfPath = System.IO.Path.ChangeExtension(fileName, "dbf");            
            dbfStream = new FileStream(dbfPath, FileMode.Create, FileAccess.ReadWrite);            
        }

        private void writeHeader()
        {
            int numFields = dataFields.Length;
            int recordLength = 1; //first byte used for deletion flag
            for (int n = numFields-1;n>=0;n--)
            {
                recordLength+= dataFields[n].FieldLength;
            }
            
            dbfStream.WriteByte(0x03);
            DateTime cal = new DateTime();
            //write todays date
            dbfStream.WriteByte((byte)(cal.Year-1900));
            dbfStream.WriteByte((byte)cal.Month);
            dbfStream.WriteByte((byte)cal.Day);
            
            //Num records in file - just write zero records when we start
            dbfStream.Write(BitConverter.GetBytes((int)0), 0, 4);
            //write length of header structure
            dbfStream.Write(BitConverter.GetBytes((short)(33 + (numFields*32) ) ),0,2);
            //write length of each record
            dbfStream.Write(BitConverter.GetBytes((short)recordLength ),0,2);
            
            //write 2 reserved bytes + incomplete transaction(=0) + not encrypted(0)
            dbfStream.Write(BitConverter.GetBytes((int)0),0,4);
            
            //free record thread not used - just write 4 zero bytes
            dbfStream.Write(BitConverter.GetBytes((int)0),0,4);
            //multi user stuff not used - just write 8 bytes
            dbfStream.Write(BitConverter.GetBytes((int)0),0,4);
            dbfStream.Write(BitConverter.GetBytes((int)0),0,4);
            
            //MDX flag
            dbfStream.WriteByte(0x0);
            //language driver
            //dbfStream.WriteByte(0x57); //ANSI
            dbfStream.WriteByte(0x0); //empty - use .cpg file to specify utf-8            
            //2 reserved bytes
            dbfStream.WriteByte(0x0);
            dbfStream.WriteByte(0x0);
            
            //now write the field descriptors
            for(int n=0;n<numFields;n++)
            {
                //write the field name
                dbfStream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(dataFields[n].FieldName), 0,dataFields[n].FieldName.Length);
                //write string terminator (0x00) and fill with zeros
                //for any names < 10 characters long
                for(int x=11-dataFields[n].FieldName.Length;x>0;x--)
                {
                    dbfStream.WriteByte(0x00);
                }
                //write the field type
                dbfStream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(dataFields[n].FieldType.ToString()),0,1);
                //write the field data address(just use zero)
                dbfStream.Write(BitConverter.GetBytes((int)0),0,4);        
                //write the field length
                dbfStream.WriteByte((byte)dataFields[n].FieldLength);
                //write the decimal count(just use zero)
                dbfStream.WriteByte((byte)dataFields[n].DecimalCount);
                //write the multi-user stuff (just use zeros)
                dbfStream.WriteByte(0x0);
                dbfStream.WriteByte(0x0);

                //write the work area id
                dbfStream.WriteByte(0x01);
                //write zeros for multi-user, SET FIELDS, reserved bytes and 
                // index field flag
                dbfStream.WriteByte(0x0);
                dbfStream.WriteByte(0x0);

                dbfStream.WriteByte(0x0);
                dbfStream.Write(BitConverter.GetBytes((long)0),0,8);                                    
            }
            
            //write the header terminator
            dbfStream.WriteByte(0x0d);
            
        }

        private const int DbfFileHeaderRecordCountOffset = 4;
    
        private void CloseDbfFile()
        {
            try
            {
                //write the end of file byte
                dbfStream.WriteByte(0x1a);
                //now update the number of records in the header                  
                dbfStream.Seek(DbfFileHeaderRecordCountOffset,SeekOrigin.Begin);
                dbfStream.Write(BitConverter.GetBytes(numRecords),0,4);            
            }
            finally
            {
                dbfStream.Close();
                dbfStream = null;
            }
            
        }

        private static byte[] PaddingString = {0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,0x20,
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

        //System.Text.UTF8Encoding utf8 = new UTF8Encoding();

        /// <summary>
        /// Adds a new reocrd to the DBF file
        /// </summary>
        /// <param name="fieldData">strign array containing the data for each field in the record</param>
        public void WriteRecord(String[] fieldData)
        {
            if(fieldData == null || fieldData.Length != this.dataFields.Length)
            {
                throw new ArgumentException("fieldData length does not match dataFields length");
            }
            //write the deleted flag
            dbfStream.WriteByte(0x20); //0x20 => record is valid
            for(int n=0;n<fieldData.Length;n++)
            {
                byte[] data = Encoding.UTF8.GetBytes(fieldData[n]);//System.Text.UTF8Encoding.UTF8.GetBytes(fieldData[n]);
                //if(fieldData[n].Length >= dataFields[n].FieldLength)
                //{

                //    dbfStream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(fieldData[n].Substring(0, dataFields[n].FieldLength)), 0, dataFields[n].FieldLength);                
                //}
                //else
                //{
                //    //write the field data and padd with spaces
                //    dbfStream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(fieldData[n]), 0, fieldData[n].Length);                
                //    dbfStream.Write(PaddingString,0,dataFields[n].FieldLength-fieldData[n].Length);                
                //}
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
            numRecords++;            
        }

        /// <summary>
        /// Closes the DBF file and the underlying stream used to write the DBF file       
        /// </summary>
        /// <remarks>
        /// The Close method must be called after all records have been added. This method will update the number
        /// of records in the file's main header and outputs the end of file marker to the end of the file.
        /// <para>Failure to call this method will result in an invalid DBF file being generated</para></remarks>
        public void Close()
        {
            CloseDbfFile();
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

        private void Dispose(bool disposing)
        {
            if (disposing) //dispose managed resources
            {
                if(dbfStream != null) dbfStream.Close();                
            }
        }

        #endregion
    }



}
