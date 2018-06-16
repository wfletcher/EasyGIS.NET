using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EGIS.ShapeFileLib
{
    public class CsvUtil
    {

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
                                if (string.IsNullOrEmpty(fields[n])) throw new Exception(string.Format("Blank field names are not allowed (column number {0})", (n + 1)));
                            }
                        }
                    }
                }
            }

            return fields;
        }

        public static void TrimValues(string[] values)
        {
            if (values == null) return;
            for (int n = values.Length - 1; n >= 0; --n)
            {
                values[n] = values[n].Trim();
            }
        }

        public static void TrimValues(string[] values, char[] trimChars)
        {
            if (values == null) return;
            for (int n = values.Length - 1; n >= 0; --n)
            {
                values[n] = values[n].Trim(trimChars);
            }
        }

        public static int IndexOfField(string[] fields, string fieldName, bool ignoreCase)
        {
            if (fields == null || string.IsNullOrEmpty(fieldName)) return -1;
            int index;
            for (index = fields.Length - 1; index >= 0; --index)
            {
                if (ignoreCase)
                {
                    if (string.Compare(fields[index], fieldName, StringComparison.OrdinalIgnoreCase) == 0) return index;
                }
                else
                {
                    if (string.Compare(fields[index], fieldName, StringComparison.Ordinal) == 0) return index;
                }
            }
            return index;
        }

        public static void ConvertCsvToShapeFile(string csvPath, string shapefilePath, string xCoordFieldName, string yCoordFieldName, bool matchFieldsExact = true,ConvertShapeFileProgress progressHandler = null)
        {
            string[] fieldNames = CsvUtil.ReadFieldHeaders(csvPath);
            CsvUtil.TrimValues(fieldNames);
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
                throw new Exception(string.Format("Could not find '{0}' or '{1}' field", xCoordFieldName, yCoordFieldName) );                
            }

            DbfFieldDesc[] fields = new DbfFieldDesc[fieldNames.Length];
            for (int n = 0; n < fieldNames.Length; ++n)
            {
                fields[n].FieldName = fieldNames[n].Length <= 10 ? fieldNames[n] : fieldNames[n].Substring(0, 10);
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
                    for (int n = values.Length - 1; n >= 0; --n)
                    {
                        fields[n].FieldLength = Math.Max(fields[n].FieldLength, values.Length);
                    }
                    totalRecords++;
                }
            }

            using (ShapeFileWriter writer = ShapeFileWriter.CreateWriter(System.IO.Path.GetDirectoryName(shapefilePath), System.IO.Path.GetFileNameWithoutExtension(shapefilePath), ShapeType.Point, fields))
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

