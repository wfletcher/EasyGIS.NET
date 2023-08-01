using EGIS.Projections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Utility class with methods to assist with reading reading data from CSV files
    /// </summary>
    public class CsvUtil
    {
        /// <summary>
        /// extracts and returns field headers from a CSV file
        /// </summary>
        /// <param name="csvFile"></param>
        /// <param name="allowBlankFieldNames"></param>
        /// <returns></returns>
        public static string[] ReadFieldHeaders(string csvFile, bool allowBlankFieldNames = false)
        {
            string[] fields = null;

            using (var stream = new FileStream(csvFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(stream))
                {
                    //read header
                    string nextLine = sr.ReadLine();
                    if (nextLine != null)
                    {
                        fields = nextLine.Split(',');
                        TrimValues(fields);
                        if (!allowBlankFieldNames)
                        {
                            for (int n = fields.Length - 1; n >= 0; --n)
                            {
                                if (string.IsNullOrEmpty(fields[n])) throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,"Blank field names are not allowed (column number {0})", (n + 1)));
                            }
                        }
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// Trims whitespace from an array of field values
        /// </summary>
        /// <param name="values"></param>
        public static void TrimValues(string[] values)
        {
            if (values == null) return;
            for (int n = values.Length - 1; n >= 0; --n)
            {
                values[n] = values[n].Trim();
            }
        }

        /// <summary>
        /// Trims whitespace from an array of field values using supplied trim characters
        /// </summary>
        /// <param name="values"></param>
        /// <param name="trimChars"></param>
        public static void TrimValues(string[] values, char[] trimChars)
        {
            if (values == null) return;
            for (int n = values.Length - 1; n >= 0; --n)
            {
                values[n] = values[n].Trim(trimChars);
            }
        }

        /// <summary>
        /// Returns zero-based index of a given field name in an array of field names
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="fieldName"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static int IndexOfField(string[] fields, string fieldName, bool ignoreCase)
        {
            if (fields == null || string.IsNullOrEmpty(fieldName)) return -1;
            int index;
            for (index = fields.Length - 1; index >= 0; --index)
            {
                if (ignoreCase)
                {
                    if (string.Equals(fields[index], fieldName, StringComparison.OrdinalIgnoreCase) ) return index;
                }
                else
                {
                    if (string.Equals(fields[index], fieldName, StringComparison.Ordinal) ) return index;
                }
            }
            return index;
        }

        /// <summary>
        /// Converts a CSV file into a point shapefile
        /// </summary>
        /// <param name="csvPath"></param>
        /// <param name="shapefilePath"></param>
        /// <param name="xCoordFieldName"></param>
        /// <param name="yCoordFieldName"></param>
        /// <param name="matchFieldsExact"></param>
        /// <param name="progressHandler"></param>
        /// <param name="trimQuotesFromValues"></param>
        /// <param name="coordinateReferenceSystem"></param>
        /// <remarks>
        /// CSV data must contain fields with point coordinates
        /// </remarks>
        public static void ConvertCsvToShapeFile(string csvPath, string shapefilePath, string xCoordFieldName, string yCoordFieldName, bool matchFieldsExact = true,ConvertShapeFileProgress progressHandler = null, bool trimQuotesFromValues=true, ICRS coordinateReferenceSystem = null)
        {
            string[] fieldNames = CsvUtil.ReadFieldHeaders(csvPath);
            CsvUtil.TrimValues(fieldNames);
            CsvUtil.TrimValues(fieldNames, new char[] { '"', '\'' });
            int yCoordIndex = -1, xCoordIndex = -1;
            for (int n = 0; n < fieldNames.Length; ++n)
            {
                if (matchFieldsExact)
                {
                    if (yCoordIndex < 0 && fieldNames[n].Equals(yCoordFieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        yCoordIndex = n;
                    }
                }
                else
                {
                    if (yCoordIndex < 0 && fieldNames[n].IndexOf(yCoordFieldName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        yCoordIndex = n;
                    }
                }

                if (matchFieldsExact)
                {
                    if (xCoordIndex < 0 && fieldNames[n].Equals(xCoordFieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        xCoordIndex = n;
                    }
                }
                else
                {
                    if (xCoordIndex < 0 && fieldNames[n].IndexOf(xCoordFieldName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        xCoordIndex = n;
                    }
                }
            }

            if (yCoordIndex < 0 || xCoordIndex < 0)
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture,"Could not find '{0}' or '{1}' field", xCoordFieldName, yCoordFieldName) );                
            }

            //ensure no duplicate field names after trimming to 10 characters
            string[] dbfFieldNames = GetDbfFieldNames(fieldNames);

            DbfFieldDesc[] fields = new DbfFieldDesc[fieldNames.Length];
            for (int n = 0; n < fieldNames.Length; ++n)
            {
                fields[n].FieldName = dbfFieldNames[n];
                fields[n].FieldLength = 1;
                fields[n].FieldType = DbfFieldType.Character;
            }

            int totalRecords = 0;
            using (System.IO.StreamReader reader = new StreamReader(new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                //skip header
                string nextLine = reader.ReadLine();
                while ((nextLine = reader.ReadLine()) != null)
                {
                    string[] values = nextLine.Split(',');
                    if (values.Length != fieldNames.Length) continue;
                    CsvUtil.TrimValues(values);
                    CsvUtil.TrimValues(values, new char[] { '"', '\'' });
                    for (int n = values.Length - 1; n >= 0; --n)
                    {
                        fields[n].FieldLength = Math.Max(fields[n].FieldLength, values[n].Length);
                    }
                    totalRecords++;
                }
            }

            using (ShapeFileWriter writer = ShapeFileWriter.CreateWriter(System.IO.Path.GetDirectoryName(shapefilePath), System.IO.Path.GetFileNameWithoutExtension(shapefilePath), ShapeType.Point, fields,coordinateReferenceSystem!= null? coordinateReferenceSystem.GetWKT(PJ_WKT_TYPE.PJ_WKT1_GDAL, false) : null))
            {
                using (System.IO.StreamReader reader = new StreamReader(new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    double[] pts = new double[2];
                    //skip header
                    string nextLine = reader.ReadLine();
                    int progress = 0;
                    int count = 0;
                    while ((nextLine = reader.ReadLine()) != null)
                    {
                        string[] values = nextLine.Split(',');
                        CsvUtil.TrimValues(values);
                        if(trimQuotesFromValues) CsvUtil.TrimValues(values, new char[] { '"', '\'' });                    
                        string yString = values[yCoordIndex];
                        if (yString.Length > 0)
                        {
                            //trim any quotes
                            yString = yString.Trim('"', '\'');
                        }
                        string xString = values[xCoordIndex];
                        if (xString.Length > 0)
                        {
                            //trim any quotes                            
                            xString = xString.Trim('"', '\'');
                        }
                        if (!double.TryParse(yString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pts[1]))
                        {
                            continue;
                        }
                        if (!double.TryParse(xString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pts[0]))
                        {
                            continue;
                        }

                        if(Math.Abs(pts[0] - -100000000) < 0.000001  || Math.Abs(pts[1] - -100000000) < 0.000001)
                        {
                            continue;                              
                        }

                        writer.AddRecord(pts, 1, values);
                        ++count;
                        if (progressHandler != null && (totalRecords > 0))
                        {
                            int currentProgress = (int)Math.Round(100.0* (double)count / totalRecords);
                            if (currentProgress != progress)
                            {
                                progressHandler(new ConvertShapeFileEventArgs() { ProgressPercent = currentProgress });
                                progress = currentProgress;
                            }
                        }
                    }
                }
            }
            if (progressHandler != null)
            {
                progressHandler(new ConvertShapeFileEventArgs() { ProgressPercent = 100 });                
            }
        }

        /// <summary>
        /// creates dbf field names(limited to max 10 charactrs) from field names, ensuring no duplicates
        /// </summary>
        /// <param name="fieldNames"></param>
        /// <returns></returns>
        private static string[] GetDbfFieldNames(string[] fieldNames)
        {
            Dictionary<string, int> fieldNameCount = new Dictionary<string, int>();
            string[] dbfFieldNames = new string[fieldNames.Length];
            //check for any duplicates
            bool duplicates = false;
            for (int n = 0; n < fieldNames.Length; ++n)
            {
                string name = fieldNames[n].Length <= 10 ? fieldNames[n] : fieldNames[n].Substring(0, 10);
                if (!fieldNameCount.ContainsKey(name))
                {
                    dbfFieldNames[n] = name;
                    fieldNameCount.Add(name, 1);
                }
                else
                {
                    duplicates = true;
                }
            }

            if (!duplicates) return dbfFieldNames;

            fieldNameCount = new Dictionary<string, int>();
            for (int n = 0; n < fieldNames.Length; ++n)
            {
                string name = fieldNames[n].Length <= 9 ? fieldNames[n] : fieldNames[n].Substring(0, 9);
                if (!fieldNameCount.ContainsKey(name))
                {
                    dbfFieldNames[n] = name;
                    fieldNameCount.Add(name, 1);
                }
                else
                {
                    int c = fieldNameCount[name]+1;
                    fieldNameCount[name] = c;
                    dbfFieldNames[n] = name + c.ToString(System.Globalization.CultureInfo.InvariantCulture);                    
                }
            }

            return dbfFieldNames;
        }

    }


    public delegate void ConvertShapeFileProgress(ConvertShapeFileEventArgs args);

    public class ConvertShapeFileEventArgs : EventArgs
    {
        public int ProgressPercent
        {
            get;
            set;
        }
    }
}

