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
using System.Drawing;
using System.Xml;
using System.ComponentModel;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Enumeration defining a LineType when rendering PolyLine ShapeFiles
    /// </summary>
    public enum LineType { 
        /// <summary>
        /// Solid line
        /// </summary>
        Solid, 
        /// <summary>
        /// A line with an outlined color
        /// </summary>
        Outline, 
        /// <summary>
        /// A line representing a railway
        /// </summary>
        Railway };

    /// <summary>
    /// Encapsulates settings used when rendering a ShapeFile
    /// </summary>
    public class RenderSettings : IDisposable
    {
        private string fieldName;
        private int fieldIndex;
        //private float minExtentDimension;
        private float _minRenderLabelZoom = -1;
        private bool renderDuplicateFields = true;

        private DbfReader dbfReader;

        private Font renderFont;
        private Color fontColor = Color.Black;
        private bool _shadowText = true;

        private Color _fillColor = Color.FromArgb(252, 252, 252);
        private Color _outlineColor = Color.FromArgb(150, 150, 150);

        private Color _selectFillColor = Color.SlateBlue;
        private Color _selectOutlineColor = Color.Yellow;
        
        private float _penWidthScale = 0.001f;

        private int maxPixelPenWidth = -1;
        private int minPixelPenWidth = -1;

        private bool _fillInterior = true;
        private LineType _lineType = LineType.Outline;

        private System.Drawing.Drawing2D.DashStyle _lineDashStyle = System.Drawing.Drawing2D.DashStyle.Solid;

        private float _minRenderZoomLevel = -1f;
        private float _maxRenderZoomLevel = -1f;

        private bool _isSelectable;

        private string symbolImagePath;
        private Image symbolImage;

        private int _pointSize = 5;

        private string toolTipFieldName;
        private int toolTipFieldIndex;
        private bool useToolTip;


        internal static RenderSettings PropertyGridInstance;

        internal RenderSettings(string shapeFileName)
        {
            dbfReader = new DbfReader(shapeFileName);

        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~RenderSettings()
        {
            if (dbfReader != null)
            {
                dbfReader.Close();
            }
        }

        /// <summary>
        /// Constructs a new RenderSettings object
        /// </summary>
        /// <param name="shapeFilePath">The path to the shape file without the .shp file extension</param>
        /// <param name="fieldName">The name of the DBF field to use when labelling shapes</param>
        /// <param name="renderFont">The Font to use when labelling shapes in the shape file</param>
        public RenderSettings(string shapeFilePath, string fieldName,  Font renderFont)
        {
            if (shapeFilePath == null)
            {
                throw new ArgumentException("shapeFileName can not be null");
            }
            if (fieldName == null)
            {
                throw new ArgumentException("fieldName can not be null");
            }

            
            dbfReader = new DbfReader(shapeFilePath);

            fieldIndex = FindFieldIndex(fieldName);
            FieldName = fieldName;
            this.renderFont = renderFont;

        }

        private int FindFieldIndex(string fieldName)
        {
            if (fieldName == null) return -1;
            for (int n = dbfReader.DbfRecordHeader.FieldCount - 1; n >= 0; n--)
            {
                if (string.Compare(dbfReader.DbfRecordHeader.GetFieldDescriptions()[n].FieldName, fieldName, StringComparison.OrdinalIgnoreCase)== 0)
                {
                    return n;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets the DbfReader of this RenderSettings ShapeFile
        /// </summary>
        [BrowsableAttribute(false)]        
        public DbfReader DbfReader
        {
            get
            {
                return dbfReader;
            }
        }
        /// <summary>
        /// Gets/Sets the text encoding used when reading string attribbutes from the shapefile's DBF file
        /// </summary>
        [TypeConverter(typeof(EncodingConverter)),
        Category("Data Settings"), Description("System.Text.Encoding used when reading string attributes from the shapefile's DBF file")]
        public System.Text.Encoding StringEncoding
        {
            get
            {
                if (this.dbfReader != null) return this.dbfReader.StringEncoding;
                return System.Text.Encoding.Default;
            }
            set
            {
                if (this.dbfReader != null) this.dbfReader.StringEncoding = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Minimum Zoom Level before labels will be rendered. 
        /// </summary>
        /// <remarks>If MinRenderLabelZoom is less than zero then
        /// this setting is ignored and shape labels will be rendered regardless
        /// of the zurent zoom level</remarks>
        [Category("Label Render Settings"), Description("Min zoom level before labels are rendered. (This property is ignored if less than zero and labels will be rendered regardless of the current zoom level.)")]
        public float MinRenderLabelZoom
        {
            get
            {
                return _minRenderLabelZoom;
            }
            set
            {
                _minRenderLabelZoom = value;
            }
        }

        /// <summary>
        /// Indicates whether or not to render duplicate field names on a polyline shape
        /// </summary>
        [Category("Layer Render Settings"), Description("Whether to Render duplicate fields names on polyline shapes")]
        public bool RenderDuplicateFields
        {
            get
            {
                return renderDuplicateFields;
            }
            set
            {
                renderDuplicateFields = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Min Zoom level before the layer is rendered.        
        /// </summary>
        /// <remarks>Use this property in conjunction with MaxZoomLevel to control whether
        /// a layer is rendered as a map is zoomed in and out.
        /// <para>
        /// This property will be ignored if it is set to a number less than 0</para>
        /// </remarks>
        /// <seealso cref="RenderSettings.MaxZoomLevel"/>
        [Category("Layer Render Settings"), Description("Min zoom level before the layer is rendered. (This property is ignored if less than zero.)")]
        public float MinZoomLevel
        {
            get
            {
                return _minRenderZoomLevel;
            }
            set
            {
                _minRenderZoomLevel = value;                
            }
        }

        /// <summary>
        /// Gets / Sets the Max Zoom level before the layer is not rendered
        /// </summary>
        /// <remarks>Use this property in conjunction with MinRenderZoomLevel to control whether
        /// a layer is rendered as a map is zoomed in and out
        /// <para>
        /// This property will be ignored if it is set to a number less than 0</para>
        /// </remarks>        
        /// <seealso cref="RenderSettings.MinZoomLevel"/>
        [Category("Layer Render Settings"), Description("Max zoom level before the layer is no longer rendered. (This property is ignored if less than zero.)")]
        public float MaxZoomLevel
        {
            get
            {
                return _maxRenderZoomLevel;
            }
            set
            {
                _maxRenderZoomLevel = value;
            }
        }


        /// <summary>
        /// Gets / Sets the name of the Field in the DBF file that will be used when labeling shapes
        /// </summary>        
        [TypeConverter(typeof(FieldNameConverter)),
        Category("Data Settings"), Description("The name of the field in the DBF file used to label each record")]
        public string FieldName
        {
            get
            {
                return fieldName;
            }
            set
            {
                fieldName = value;
                fieldIndex = FindFieldIndex(fieldName);
            }
        }

        /// <summary>
        /// Gets the zero based field column index of the specifed DBF FieldName
        /// </summary>
        /// <remarks> If no FieldName is set or FieldName is not in the DBF file Field index returns -1</remarks>
        /// <seealso cref="RenderSettings.FieldName"/>
        [BrowsableAttribute(false)]
        public int FieldIndex
        {
            get
            {
                return fieldIndex;
            }
        }

        /// <summary>
        /// Gets / Sets the Font to use when labelling shapes
        /// </summary>
        [Category("Label Render Settings"), Description("The font used when labelling shape.")]
        public Font Font
        {
            get
            {
                return renderFont;
            }
            set
            {
                renderFont = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Font Color to use when rendering shape labels
        /// </summary>
        [Category("Label Render Settings"), Description("The font color.")]
        public Color FontColor
        {
            get
            {
                return fontColor;
            }
            set
            {
                fontColor = value;
            }
        }

        /// <summary>
        /// Gets / Sets whether to use a "Shadow" text effect when labelling shapes
        /// </summary>
        [Category("Label Render Settings"), Description("Whether to render labels using a shadow text effect.")]
        public bool ShadowText
        {
            get
            {
                return _shadowText;
            }
            set
            {
                _shadowText = value;
            }
        }
      
        /// <summary>
        /// Gets / Sets the Fill Color to use when rendering the interior of each shape
        /// </summary>
        [Category("Layer Render Settings"), Description("The color used to fill the interior of each shape.")]
        public Color FillColor
        {
            get
            {
                return _fillColor;
            }
            set
            {
                _fillColor = value;
            }
        }

        /// <summary>
        /// Gets / Sets the color used to draw the outline of each shape.
        /// </summary>
        [Category("Layer Render Settings"), Description("The color used to draw the outline of each shape.")]
        public Color OutlineColor
        {
            get
            {
                return _outlineColor;
            }
            set
            {
                _outlineColor = value;
            }
        }

        /// <summary>
        /// Gets / Sets the width of the pen in ShapeFile coordinate units (PolyLines only).
        /// </summary>
        /// <remarks>
        /// This settings is only used when rendering PolyLine or PolyLineM ShapeFiles
        /// <para>
        /// If a ShapeFile is using UTM coords this is the width of the lines in metres. 
        /// </para>
        /// <para>If a ShapeFile is using Lat Long coordinates this is the width of the line in decimal degrees</para>
        /// <para>
        /// This property is set automatically when using the Desktop version of Easy GIS .NET, using a default value of 8m for UTM ShapeFiles.
        /// For ShapeFiles using Lat Long coordinates, the decimal degree equivalent of the 8m value is calculated based on teh ShapeFiles Extent
        /// </para>
        /// </remarks>
        [Category("Layer Render Settings"), Description("This is the width of the pen in coordinate units (PolyLines only). If using UTM coords this is the width of the lines in metres")]
        public float PenWidthScale
        {
            get
            {
                return _penWidthScale;

            }
            set
            {
                _penWidthScale = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Maximum width of the pen in Pixels.
        /// </summary>
        /// <remarks>
        /// This settings is only used when rendering PolyLine or PolyLineM ShapeFiles
        /// <para>
        /// This property is ignored if its value is set to zero or less.
        /// </para>
        /// </remarks>
        [Category("Layer Render Settings"), Description("This is the width of the pen in coordinate units (PolyLines only). If using UTM coords this is the width of the lines in metres")]
        public int MaxPixelPenWidth
        {
            get
            {
                return this.maxPixelPenWidth;
            }
            set
            {
                this.maxPixelPenWidth = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Minimum width of the pen in Pixels.
        /// </summary>
        /// <remarks>
        /// This settings is only used when rendering PolyLine or PolyLineM ShapeFiles
        /// <para>
        /// This property is ignored if its value is set to zero or less.
        /// </para>
        /// </remarks>
        public int MinPixelPenWidth
        {
            get
            {
                return this.minPixelPenWidth;
            }
            set
            {
                this.minPixelPenWidth = value;
            }
        }


        /// <summary>
        /// Gets / Sets the LineType to use when rending lines (PolyLine and PolyLineM only)
        /// </summary>
        [Category("Layer Render Settings"), Description("The Line Type (PolyLines only)")]
        public LineType LineType
        {
            get
            {
                return _lineType;
            }
            set
            {
                _lineType = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Line DashStyle to use when rending solid lines and the outline of polygons
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Line DashStyle is ignored if LineType is not LineType.Solid when rendering PolyLine, polyLineM or PolyLineZ shapefiles</br>
        /// When rendering Polygon or PolygonZ shapefiles the DashStyle is used when rendering the outline of polygons        
        /// </para>
        /// <para>
        /// The DasyStyle Custom is not supported
        /// </para>
        /// </remarks>
        [Category("Layer Render Settings"), Description("The Line  DashStyle to use when rending solid lines and the outline of polygons")]
        public System.Drawing.Drawing2D.DashStyle LineDashStyle
        {
            get
            {
                return _lineDashStyle;
            }
            set
            {
                _lineDashStyle = value;
            }
        } 

        /// <summary>
        /// Gets / Sets whether to fill the interior of each shape (Polygon)
        /// </summary>
        [Category("Layer Render Settings"), Description("Flag indicating whether to fill the interior of each shape.")]
        public bool FillInterior
        {
            get
            {
                return _fillInterior;
            }
            set
            {
                _fillInterior = value;
            }
        }

        /// <summary>
        /// Gets / Sets whether the ShapeFile is selectable
        /// </summary>
        [Category("Layer Render Settings"), Description("Flag indicating whether shapes in the layer can be selected. Set this to true if you want to search for records in the layer (using the field specified in FieldName).")]
        public bool IsSelectable
        {
            get
            {
                return _isSelectable;
            }
            set
            {
                _isSelectable = value;
            }
        }

        /// <summary>
        /// Gets / Sets the color used to draw the outline of selected shapes.
        /// </summary>
        [Category("Layer Render Settings"), Description("The color used to draw the outline of selected shapes.")]
        public Color SelectOutlineColor
        {
            get
            {
                return _selectOutlineColor;
            }
            set
            {
                _selectOutlineColor = value;
            }
        }

        /// <summary>
        /// Gets / Sets the Fill Color to use when rendering the interior of selected shapes
        /// </summary>
        [Category("Layer Render Settings"), Description("The color used to fill the interior selected shapes.")]
        public Color SelectFillColor
        {
            get
            {
                return _selectFillColor;
            }
            set
            {
                _selectFillColor = value;
            }
        }

        /// <summary>
        /// Gets / Sets the name of the Field in the DBF file that will be used for the ToolTip text
        /// </summary>        
        [TypeConverter(typeof(FieldNameConverter)),
        Category("Tool Tip Settings"), Description("The name of the field in the DBF file used for the ToolTip text")]
        public string ToolTipFieldName
        {
            get
            {
                return toolTipFieldName;
            }
            set
            {
                toolTipFieldName = value;
                toolTipFieldIndex = FindFieldIndex(toolTipFieldName);
            }
        }

        /// <summary>
        /// Gets / Sets whether to use a ToolTip for the layer
        /// </summary>        
        [Category("Tool Tip Settings"), Description("Whether a ToolTip should be used for the layer")]
        public bool UseToolTip
        {
            get
            {
                return useToolTip;
            }
            set
            {
                useToolTip = value;                
            }
        }



        /// <summary>
        /// Gets the zero based field column index of the ToolTipFieldName
        /// </summary>
        /// <remarks> If no FieldName is set or FieldName is not in the DBF file Field index returns -1</remarks>
        /// <seealso cref="RenderSettings.ToolTipFieldName"/>
        [BrowsableAttribute(false)]
        public int ToolTipFieldIndex
        {
            get
            {
                return toolTipFieldIndex;
            }
        }

        /// <summary>
        /// Gets/Sets the Image Symbol to use for Point records
        /// </summary>
        [Editor(typeof(ImageFileNameEditor), typeof(System.Drawing.Design.UITypeEditor)),
        Category("Point Render Settings"), Description("The symbol image used to label points (If not specified points will be drawn with a circle)." +
           "This settings is only used for Point type shapefiles")]
        public string PointImageSymbol
        {
            get
            {
                return symbolImagePath;
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && System.IO.File.Exists(value))
                {
                    Image img = Image.FromFile(value);
                    if (symbolImage != null)
                    {
                        symbolImage.Dispose();
                    }
                    symbolImage = img;
                }
                else
                {
                    if (symbolImage != null)
                    {
                        symbolImage.Dispose();
                    }
                    symbolImage = null;
                }
                symbolImagePath = value;                                
            }
        }

        /// <summary>
        /// Returns the symbol Image if PointImageSymbol has been set
        /// </summary>
        /// <returns></returns>
        public Image GetImageSymbol()
        {
            return this.symbolImage;
        }

        /// <summary>
        /// Gets/Sets the size (in pixels) to use when rendering each point in a Point ShapeFile
        /// </summary>
        [Category("Point Render Settings"), Description("The pixel size of points when rendering Point type shapefiles. This settings is ignored if " + 
            "a PointImageSymbol has been set.")]
        public int PointSize
        {
            get
            {
                return _pointSize;
            }
            set
            {
                _pointSize = value;
            }
        }
        

        private ICustomRenderSettings _customRenderSettings;

        /// <summary>
        /// Gets or sets a ICustomRenderSettings object that should be applied when rendering the shapefile
        /// </summary>
        /// <seealso cref="EGIS.ShapeFileLib.ICustomRenderSettings"/>
        public ICustomRenderSettings CustomRenderSettings
        {
            get
            {
                return _customRenderSettings;       
                //return new TestCustomRenderSettings(this, "NAME", 100);
            }
            set
            {
                _customRenderSettings = value;               
            }
        }

        #region Xml Methods

        /// <summary>
        /// Writes an XML representation of RenderSettings
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("renderer");

            writer.WriteStartElement("MinZoomLevel");
            writer.WriteString(MinZoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            writer.WriteStartElement("MaxZoomLevel");
            writer.WriteString(MaxZoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            //writer.WriteStartElement("MinExtentDimension");
            //writer.WriteString(MinLabelExtentDim.ToString());
            //writer.WriteEndElement();


            writer.WriteStartElement("MinRenderLabelZoom");
            writer.WriteString(MinRenderLabelZoom.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            
            writer.WriteStartElement("FieldName");
            writer.WriteString(FieldName);
            writer.WriteEndElement();

            writer.WriteStartElement("Font");
            writer.WriteAttributeString("Size", this.Font.Size.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Name", this.Font.Name);
            writer.WriteAttributeString("Style", this.Font.Style.ToString());            
            writer.WriteFullEndElement();

            writer.WriteStartElement("FontColor");
            if (FontColor.A < 255)
            {
                writer.WriteAttributeString("alpha", FontColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            writer.WriteString(ColorTranslator.ToHtml(FontColor));
            writer.WriteEndElement();

            writer.WriteStartElement("FillColor");
            if (FillColor.A < 255)
            {
                writer.WriteAttributeString("alpha", FillColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            writer.WriteString(ColorTranslator.ToHtml(FillColor));
            writer.WriteEndElement();
            
            writer.WriteStartElement("OutlineColor");
            if (OutlineColor.A < 255)
            {
                writer.WriteAttributeString("alpha", OutlineColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            writer.WriteString(ColorTranslator.ToHtml(OutlineColor));
            writer.WriteEndElement();

            writer.WriteStartElement("PenWidthScale");
            writer.WriteString(this.PenWidthScale.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            writer.WriteStartElement("MaxPixelPenWidth");
            writer.WriteString(this.MaxPixelPenWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            writer.WriteStartElement("MinPixelPenWidth");
            writer.WriteString(this.MinPixelPenWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            writer.WriteStartElement("FillInterior");
            writer.WriteString(this.FillInterior.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("Selectable");
            writer.WriteString(this.IsSelectable.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("SelectOutlineColor");
            if (SelectOutlineColor.A < 255)
            {
                writer.WriteAttributeString("alpha", SelectOutlineColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            writer.WriteString(ColorTranslator.ToHtml(SelectOutlineColor));
            writer.WriteEndElement();

            writer.WriteStartElement("SelectFillColor");
            if (SelectFillColor.A < 255)
            {
                writer.WriteAttributeString("alpha", SelectFillColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            writer.WriteString(ColorTranslator.ToHtml(SelectFillColor));
            writer.WriteEndElement();

            writer.WriteStartElement("LineType");
            writer.WriteString(this.LineType.ToString());
            writer.WriteEndElement();

            if (!string.IsNullOrEmpty(PointImageSymbol) && System.IO.File.Exists(PointImageSymbol))
            {
                writer.WriteStartElement("PointImageSymbol");
                writer.WriteString(PointImageSymbol);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("ShadowText");
            writer.WriteString(this.ShadowText.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("PointSize");
            writer.WriteString(this.PointSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();


            writer.WriteStartElement("UseToolTip");
            writer.WriteString(this.UseToolTip.ToString());
            writer.WriteEndElement();

            if (!string.IsNullOrEmpty(this.ToolTipFieldName))
            {
                writer.WriteStartElement("ToolTipFieldName");
                writer.WriteString(this.ToolTipFieldName);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("StringEncoding");
            writer.WriteString(this.StringEncoding.CodePage.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteEndElement();

            writer.WriteEndElement();

        }

        /// <summary>
        /// Reads the RenderSettings properties from an Xml representation
        /// </summary>
        /// <param name="element"></param>        
        public void ReadXml(XmlElement element)
        {
            FieldName = element.GetElementsByTagName("FieldName")[0].InnerText;
            XmlElement fontElement = (XmlElement)element.GetElementsByTagName("Font")[0];
            this.Font = new Font(fontElement.GetAttribute("Name"), float.Parse(fontElement.GetAttribute("Size"), System.Globalization.CultureInfo.InvariantCulture), (FontStyle)Enum.Parse(typeof(FontStyle), fontElement.GetAttribute("Style"), true));
            FillColor = ColorTranslator.FromHtml(element.GetElementsByTagName("FillColor")[0].InnerText);
            if (element.GetElementsByTagName("FillColor")[0].Attributes["alpha"] != null)
            {
                int alpha;
                if(int.TryParse( ((XmlElement)element.GetElementsByTagName("FillColor")[0]).GetAttribute("alpha"),out alpha))
                {
                    FillColor = Color.FromArgb(alpha, FillColor);
                }                
            }
            OutlineColor = ColorTranslator.FromHtml(element.GetElementsByTagName("OutlineColor")[0].InnerText);
            if (element.GetElementsByTagName("OutlineColor")[0].Attributes["alpha"] != null)
            {
                int alpha;
                if (int.TryParse(((XmlElement)element.GetElementsByTagName("OutlineColor")[0]).GetAttribute("alpha"), out alpha))
                {
                    OutlineColor = Color.FromArgb(alpha, OutlineColor);
                }
            }

            PenWidthScale = float.Parse(element.GetElementsByTagName("PenWidthScale")[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
            FillInterior = bool.Parse(element.GetElementsByTagName("FillInterior")[0].InnerText);
            LineType = (LineType)Enum.Parse(typeof(LineType), element.GetElementsByTagName("LineType")[0].InnerText, true);
            
            XmlNodeList nl = element.GetElementsByTagName("MinZoomLevel");
            if (nl != null && nl.Count > 0)
            {
                MinZoomLevel = float.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
            }
            nl = element.GetElementsByTagName("MaxZoomLevel");
            if (nl != null && nl.Count > 0)
            {
                MaxZoomLevel = float.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
            }

            nl = element.GetElementsByTagName("MinExtentDimension");
            if (nl != null && nl.Count > 0)
            {
                float med = float.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
                //convert to MinRenderLabelZoom, assuming 512x512 image
                _minRenderLabelZoom = 512 / med;                
            }

            nl = element.GetElementsByTagName("MinRenderLabelZoom");
            if (nl != null && nl.Count > 0)
            {
                _minRenderLabelZoom = float.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);                
            }

            
            nl = element.GetElementsByTagName("Selectable");
            if (nl != null && nl.Count > 0)
            {
                IsSelectable = bool.Parse(nl[0].InnerText);
            }

            nl = element.GetElementsByTagName("FontColor");
            if (nl != null && nl.Count > 0)
            {
                FontColor = ColorTranslator.FromHtml(nl[0].InnerText);
                if (nl[0].Attributes["alpha"] != null)
                {
                    int alpha;
                    if (int.TryParse(((XmlElement)nl[0]).GetAttribute("alpha"), out alpha))
                    {
                        FontColor = Color.FromArgb(alpha, FontColor);
                    }
                }
            }
            else
            {
                FontColor = Color.Black;
            }

            nl = element.GetElementsByTagName("PointImageSymbol");
            if (nl != null && nl.Count > 0)
            {
                string path = nl[0].InnerText;

                if (!System.IO.Path.IsPathRooted(path))
                {
                    string rootDir = "";
                    Uri uri = new Uri(element.OwnerDocument.BaseURI);
                    if (uri.IsFile)
                    {
                        rootDir = System.IO.Path.GetDirectoryName(uri.AbsolutePath);
                    }
                    path = rootDir + "\\" + path;
                }
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    PointImageSymbol = path;
                }
                else
                {
                    PointImageSymbol = null;
                }                
            }
            else
            {
                PointImageSymbol = null;
            }



            nl = element.GetElementsByTagName("ShadowText");
            if (nl != null && nl.Count > 0)
            {
                ShadowText = bool.Parse(nl[0].InnerText);
            }            

            nl = element.GetElementsByTagName("PointSize");
            if (nl != null && nl.Count > 0)
            {
                PointSize = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
            }

            nl = element.GetElementsByTagName("UseToolTip");
            if (nl != null && nl.Count > 0)
            {
                UseToolTip = bool.Parse(nl[0].InnerText);                
            }

            nl = element.GetElementsByTagName("ToolTipFieldName");
            if (nl != null && nl.Count > 0)
            {
                ToolTipFieldName = nl[0].InnerText;
            }
            else
            {
                ToolTipFieldName = "";
            }

            nl = element.GetElementsByTagName("MaxPixelPenWidth");
            if (nl != null && nl.Count > 0)
            {
                this.MaxPixelPenWidth = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                this.MaxPixelPenWidth = -1;
            }

            nl = element.GetElementsByTagName("MinPixelPenWidth");
            if (nl != null && nl.Count > 0)
            {
                this.MinPixelPenWidth = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                this.MinPixelPenWidth = -1;
            }

            nl = element.GetElementsByTagName("StringEncoding");
            if (nl != null && nl.Count > 0)
            {
                int codePage = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
                    System.Text.Encoding enc = System.Text.Encoding.GetEncoding(codePage);
                    if (enc != null)
                    {
                        this.StringEncoding = enc;
                    }
                
            }

            nl = element.GetElementsByTagName("SelectFillColor");
            if (nl != null && nl.Count > 0)
            {
                SelectFillColor = ColorTranslator.FromHtml(nl[0].InnerText);
                if (nl[0].Attributes["alpha"] != null)
                {
                    int alpha;
                    if (int.TryParse(((XmlElement)nl[0]).GetAttribute("alpha"), out alpha))
                    {
                        SelectFillColor = Color.FromArgb(alpha, SelectFillColor);
                    }
                }                
            }

            nl = element.GetElementsByTagName("SelectOutlineColor");
            if (nl != null && nl.Count > 0)
            {
                SelectOutlineColor = ColorTranslator.FromHtml(nl[0].InnerText);
                if (nl[0].Attributes["alpha"] != null)
                {
                    int alpha;
                    if (int.TryParse(((XmlElement)nl[0]).GetAttribute("alpha"), out alpha))
                    {
                        SelectOutlineColor = Color.FromArgb(alpha, SelectOutlineColor);
                    }
                }
            }
                        
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose of the RenderSettings
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            
        }

        /// <summary>
        /// Diposes of the RenderSettings. Derived classes that override this method
        /// should call base.Dispose(disposing)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) //dispose unmanaged resources
            {
                if (this.dbfReader != null) dbfReader.Dispose();
            }
        }

        #endregion
    }


    class FieldNameConverter : StringConverter
    {
        private string[] fieldNames = new string[]{"NAME", "FIELD2", "FIELD3"};
        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            if (context != null)
            {                                
                RenderSettings renderer = context.Instance as RenderSettings;
                if (renderer != null && renderer.DbfReader != null)
                {
                    RenderSettings.PropertyGridInstance = renderer;
                    fieldNames = renderer.DbfReader.GetFieldNames();
                }
                else if (RenderSettings.PropertyGridInstance != null && RenderSettings.PropertyGridInstance.DbfReader != null)
                {
                    fieldNames = RenderSettings.PropertyGridInstance.DbfReader.GetFieldNames();
                }
            }
            return true;
        }
        
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            
            return new StandardValuesCollection(fieldNames);
        }

        

        public override object CreateInstance(ITypeDescriptorContext context, System.Collections.IDictionary propertyValues)
        {
            return base.CreateInstance(context, propertyValues);
        }


    }

    
    
    //class ImageFileNameEditor : System.Windows.Forms.Design.FileNameEditor
    //{
    //    public ImageFileNameEditor()
    //    {
            
    //    }

    //    protected override void InitializeDialog(System.Windows.Forms.OpenFileDialog openFileDialog)
    //    {

    //        base.InitializeDialog(openFileDialog);
    //        openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
    //        openFileDialog.Title = "Select Symbol Image to Label Points.";
            
    //    }
    //}


    /// <summary>
    /// implementing our own editor instead of deriving FileNameEditor due to Client Profile compilation issues
    /// </summary>
    class ImageFileNameEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return System.Drawing.Design.UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (System.Windows.Forms.OpenFileDialog f = new System.Windows.Forms.OpenFileDialog())
            {
                f.AddExtension = true;
                f.CheckFileExists = true;
                f.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
                f.Title = "Select Symbol Image to Label Points.";
                f.DefaultExt = "bmp";
                f.FileName = value as string;

                if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK) return f.FileName;
                return value;
            }
        }

    } 

    class EncodingConverter : StringConverter
    {
        //private string[] fieldNames = new string[] { "NAME", "FIELD2", "FIELD3" };

        //public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        //{
        //    if (context != null)
        //    {
        //        RenderSettings renderer = context.Instance as RenderSettings;
        //        if (renderer != null && renderer.DbfReader != null)
        //        {
        //            RenderSettings.PropertyGridInstance = renderer;
        //            fieldNames = renderer.DbfReader.GetFieldNames();
        //        }
        //        else if (RenderSettings.PropertyGridInstance != null && RenderSettings.PropertyGridInstance.DbfReader != null)
        //        {
        //            fieldNames = RenderSettings.PropertyGridInstance.DbfReader.GetFieldNames();
        //        }
        //    }
        //    return true;
        //}

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private string[] standardValues = null;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (standardValues == null)
            {
                EncodingInfo[] eInfo = System.Text.Encoding.GetEncodings();
                standardValues = new string[eInfo.Length];
                for (int n = 0; n < eInfo.Length; ++n)
                {
                    standardValues[n] = String.Format("{0},{1},{2},{3}", new object[] { eInfo[n].CodePage, eInfo[n].DisplayName, eInfo[n].Name, System.Text.Encoding.GetEncoding(eInfo[n].CodePage).WebName });
                }                
            }
            return new StandardValuesCollection(standardValues);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context,
           Type sourceType)
        {

            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        
        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(ITypeDescriptorContext context,
           System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                string[] v = (value.ToString()).Split(new char[] { ',' });
                return System.Text.Encoding.GetEncoding(int.Parse(v[0]));//(Point(int.Parse(v[0]), int.Parse(v[1]));
            }
            return base.ConvertFrom(context, culture, value);
        }
        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context,
           System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                System.Text.Encoding enc = value as System.Text.Encoding;
                if (enc != null)
                {
                    return String.Format("{0},{1},{2},{3}", enc.CodePage, enc.EncodingName, enc.WebName, enc.WindowsCodePage);
                }
                
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

         

        public override object CreateInstance(ITypeDescriptorContext context, System.Collections.IDictionary propertyValues)
        {
            return base.CreateInstance(context, propertyValues);
        }


    }

    
}
