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
        private bool dbfReaderIsOwned;// = false;

        private Font renderFont;
        private Color fontColor = Color.Black;
        private bool _shadowText = true;
        private Color shadowTextColor = Color.White;

        private Color _fillColor = Color.FromArgb(252, 252, 252);
        private Color _outlineColor = Color.FromArgb(150, 150, 150);

        private Color _selectFillColor = Color.SlateBlue;
        private Color _selectOutlineColor = Color.Yellow;
        
        private float _penWidthScale = 0.00025f;

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

        private System.Drawing.Drawing2D.LineCap _lineStartCap = System.Drawing.Drawing2D.LineCap.Round;
        private System.Drawing.Drawing2D.LineCap _lineEndCap = System.Drawing.Drawing2D.LineCap.Round;

        internal static RenderSettings PropertyGridInstance;

        internal RenderSettings(string shapeFileName)
        {
            dbfReader = new DbfReader(shapeFileName);
			dbfReaderIsOwned = true;

		}

        internal RenderSettings(DbfReader dbfReader)
        {
			this.dbfReader = dbfReader;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~RenderSettings()
        {
            if (dbfReader != null && dbfReaderIsOwned)
            {
                dbfReader.Close();
				dbfReader = null;
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
			dbfReaderIsOwned = true;

			fieldIndex = FindFieldIndex(fieldName);
            FieldName = fieldName;
            this.renderFont = renderFont;

        }

		/// <summary>
		/// Constructs a new RenderSettings
		/// </summary>
		/// <param name="dbfReader">shapefile DbfReader</param>
		/// <param name="fieldName">The name of the DBF field to use when labelling shapes</param>
		/// <param name="renderFont">The Font to use when labelling shapes in the shape file</param>
		public RenderSettings(DbfReader dbfReader, string fieldName, Font renderFont)
        {            
            if (fieldName == null)
            {
                throw new ArgumentException("fieldName can not be null");
            }

			this.dbfReader = dbfReader;

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
		/// This property will be ignored MinZoomLevelif it is set to a number less than 0</para>
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
        /// Gets / Sets the Font Color to use when rendering shape labels
        /// </summary>
        [Category("Label Render Settings"), Description("Shadow text color.")]
        public Color ShadowTextColor
        {
            get
            {
                return shadowTextColor;
            }
            set
            {
                shadowTextColor = value;
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
        [Category("Layer Render Settings"), Description("Maximum width of lines in pixels (PolyLines only).")]
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
		[Category("Layer Render Settings"), Description("Minimum width of lines in pixels (PolyLines only).")]
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
        /// The Line DashStyle is ignored if LineType is not LineType.Solid when rendering PolyLine, polyLineM or PolyLineZ shapefiles<br/>
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

        [Category("Layer Render Settings"), Description("The start LineCap to use when rending solid lines and the outline of polygons")]
        public System.Drawing.Drawing2D.LineCap LineStartCap
        {
            get
            {
                return _lineStartCap;
            }
            set
            {
                _lineStartCap = value;
            }
        }

        [Category("Layer Render Settings"), Description("The End LineCap to use when rending solid lines and the outline of polygons")]
        public System.Drawing.Drawing2D.LineCap LineEndCap
        {
            get
            {
                return _lineEndCap;
            }
            set
            {
                _lineEndCap = value;
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
        /// Gets/Sets the Image Symbol to use for Point records supplying a file path
        /// </summary>
        /// <remarks>
        /// <para>
        /// This pr
        /// </para>
        /// </remarks>
        [Editor(typeof(ImageFileNameEditor), typeof(System.Drawing.Design.UITypeEditor)),
        Category("Point Render Settings"), Description("The symbol image used to label points (If not specified points will be drawn with a circle)." +
           "This settings is only used for Point type shapefiles")]
        [BrowsableAttribute(false)]
        [Obsolete("This property is obsolete. Use PointSymbol")]
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
        /// Returns the symbol Image if ImageSymbol has been set
        /// </summary>
        /// <returns></returns>
        public Image GetImageSymbol()
        {
            return this.symbolImage;
        }

        /// <summary>
        /// Gets/Sets the Image Symbol to use for Point records
        /// </summary>
        [Category("Point Render Settings"), Description("The symbol image used to label points (If not specified points will be drawn with a circle)." +
           "This settings is only used for Point type shapefiles")]
        public Image PointSymbol
        {
            get { return this.symbolImage; }
            set
            {
                if (this.symbolImage != value)
                {
                    //don't dispose as the caller may set the same symbol in different RenderSettings objects
                    //could clone the image in the set method but we'll keep it simple and let user handle this
                    //or let the garbage collector take care of it 
                    //if (this.symbolImage != null)
                    //{
                    //    this.symbolImage.Dispose();
                    //}
                    this.symbolImage = value;

                    string s = EncodeImageAsBase64String(this.symbolImage);
                    var img = DecodeImageFromBase64String(s);

                }
            }
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

        private RenderQuality renderQuality = RenderQuality.Auto;

        /// <summary>
        /// Gets / Sets the RenderQuality of the layer.
        /// </summary>
        /// <remarks>
        /// If the RenderQuality is the default value of RenderQuality.Auto the quality of the rendering is determined by 
        /// the ShapeFile.RenderQuality static member        
        /// </remarks>
        // Thanks to M Gerginski for suggestion
        [Category("Layer Render Settings"), Description("The RenderQuality of the layer."), DefaultValue(RenderQuality.Auto)]
        public RenderQuality RenderQuality
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

        private bool _drawDirectionArrows;// = false;
		private int _directionArrowWidth=2;
		private int _directionArrowLength = 50;
		private float _directionArrowMinZoomLevel = -1;
		private Color _directionArrowColor = Color.LightGray;

		/// <summary>
		/// Whether to draw direction arrows on polylines. Only applies to PolyLine, PolyLineM and PolylineZ shapefiles. Default value is false
		/// </summary>
		[Category("Direction Arrows"), Description("Whether to draw direction arrows to indicate order of points (PolyLines only).")]
		public bool DrawDirectionArrows
		{
			get
			{
				return _drawDirectionArrows;
			}
			set
			{
				_drawDirectionArrows = value;
			}
		}

		/// <summary>
		/// The width of direction arrows if DrawDirectionArrows is true. Only applies to PolyLine, PolyLineM and PolylineZ shapefiles
		/// </summary>
		[Category("Direction Arrows"), Description("Width of direction arrows in pixels (PolyLines only).")]
		public int DirectionArrowWidth
		{
			get
			{
				return _directionArrowWidth;
			}
			set
			{
				_directionArrowWidth = Math.Max(1, value);
			}
		}

		/// <summary>
		/// The length of direction arrows if DrawDirectionArrows is true. Only applies to PolyLine, PolyLineM and PolylineZ shapefiles
		/// </summary>
		[Category("Direction Arrows"), Description("Length of direction arrows in pixels (PolyLines only).")]
		public int DirectionArrowLength
		{
			get
			{
				return _directionArrowLength;
			}
			set
			{
				_directionArrowLength = Math.Max(1, value);
			}
		}

		/// <summary>
		/// The minimum zoom level before drawing direction arrows if DrawDirectionArrows is true. Only applies to PolyLine, PolyLineM and PolylineZ shapefiles
		/// </summary>
		[Category("Direction Arrows"), Description("Minimum zoom level before drawing direction arrows (PolyLines only).")]
		public float DirectionArrowMinZoomLevel
		{
			get
			{
				return _directionArrowMinZoomLevel;
			}
			set
			{
				_directionArrowMinZoomLevel = value;
			}
		}

		/// <summary>
		/// The pen color to use when drawing direction arrows. Only applies to PolyLine, PolyLineM and PolylineZ shapefiles
		/// </summary>
		[Category("Direction Arrows"), Description("Direction arrow pen color (PolyLines only).")]
		public Color DirectionArrowColor
		{
			get
			{
				return _directionArrowColor;
			}
			set
			{
				_directionArrowColor = value;
			}
		}

        private bool _visible = true;

        /// <summary>
        /// Get/Set whether the layer is visible
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property can be used to hide/show a layer without removing it from the map
        /// </para>
        /// </remarks>
        [Category("Layer Render Settings"), Description("Layer visibility"), DefaultValue(true)]
        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
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

            writer.WriteStartElement("LineStartCap");
            writer.WriteString(this.LineStartCap.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("LineEndCap");
            writer.WriteString(this.LineEndCap.ToString());
            writer.WriteEndElement();

            //if (!string.IsNullOrEmpty(PointImageSymbol) && System.IO.File.Exists(PointImageSymbol))
            //{
            //    writer.WriteStartElement("PointImageSymbol");
            //    writer.WriteString(PointImageSymbol);
            //    writer.WriteEndElement();
            //}

            if (PointSymbol != null)
            {
                writer.WriteStartElement("PointSymbol");
                writer.WriteString(EncodeImageAsBase64String(PointSymbol));
                writer.WriteEndElement();
            }

            writer.WriteStartElement("ShadowText");
            writer.WriteString(this.ShadowText.ToString());
            writer.WriteEndElement();

            writer.WriteStartElement("ShadowTextColor");
            if (ShadowTextColor.A < 255)
            {
                writer.WriteAttributeString("alpha", ShadowTextColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            writer.WriteString(ColorTranslator.ToHtml(ShadowTextColor));
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

            writer.WriteStartElement("RenderQuality");
            writer.WriteString(this.RenderQuality.ToString());
            writer.WriteEndElement();

			writer.WriteStartElement("DrawDirectionArrows");
			writer.WriteString(this.DrawDirectionArrows.ToString());
			writer.WriteEndElement();

			writer.WriteStartElement("DirectionArrowWidth");
			writer.WriteString(this.DirectionArrowWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
			writer.WriteEndElement();

			writer.WriteStartElement("DirectionArrowLength");
			writer.WriteString(this.DirectionArrowLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
			writer.WriteEndElement();

			writer.WriteStartElement("DirectionArrowMinZoomLevel");
			writer.WriteString(this.DirectionArrowMinZoomLevel.ToString(System.Globalization.CultureInfo.InvariantCulture));
			writer.WriteEndElement();
			
			writer.WriteStartElement("DirectionArrowColor");
			if (DirectionArrowColor.A < 255)
			{
				writer.WriteAttributeString("alpha", DirectionArrowColor.A.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
			writer.WriteString(ColorTranslator.ToHtml(DirectionArrowColor));
			writer.WriteEndElement();

            writer.WriteStartElement("Visible");
            writer.WriteString(this.Visible.ToString());
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

            PointImageSymbol = null;

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
            }
            
            nl = element.GetElementsByTagName("PointSymbol");
            if (nl != null && nl.Count > 0)
            {
                string base64String = nl[0].InnerText;
                PointSymbol = DecodeImageFromBase64String(base64String);
            }            

            nl = element.GetElementsByTagName("ShadowText");
            if (nl != null && nl.Count > 0)
            {
                ShadowText = bool.Parse(nl[0].InnerText);
            }

            nl = element.GetElementsByTagName("ShadowTextColor");
            if (nl != null && nl.Count > 0)
            {
                ShadowTextColor = ColorTranslator.FromHtml(nl[0].InnerText);
                if (nl[0].Attributes["alpha"] != null)
                {
                    int alpha;
                    if (int.TryParse(((XmlElement)nl[0]).GetAttribute("alpha"), out alpha))
                    {
                        ShadowTextColor = Color.FromArgb(alpha, ShadowTextColor);
                    }
                }
            }
            else
            {
                ShadowTextColor = Color.White;
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

            nl = element.GetElementsByTagName("RenderQuality");
            if (nl != null && nl.Count > 0)
            {
                this.RenderQuality = (RenderQuality)Enum.Parse(typeof(RenderQuality), nl[0].InnerText, true);                                       
            }
            else
            {
                this.RenderQuality = RenderQuality.Auto;
            }

            nl = element.GetElementsByTagName("LineStartCap");
            if (nl != null && nl.Count > 0)
            {
                LineStartCap = (System.Drawing.Drawing2D.LineCap)Enum.Parse(typeof(System.Drawing.Drawing2D.LineCap), nl[0].InnerText, true);
            }

            nl = element.GetElementsByTagName("LineEndCap");
            if (nl != null && nl.Count > 0)
            {
                LineEndCap = (System.Drawing.Drawing2D.LineCap)Enum.Parse(typeof(System.Drawing.Drawing2D.LineCap), nl[0].InnerText, true);
            }

			nl = element.GetElementsByTagName("DrawDirectionArrows");
			if (nl != null && nl.Count > 0)
			{
				DrawDirectionArrows = bool.Parse(nl[0].InnerText);
			}


			nl = element.GetElementsByTagName("DirectionArrowWidth");
			if (nl != null && nl.Count > 0)
			{
				this.DirectionArrowWidth = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
			}
			else
			{
				this.DirectionArrowWidth = 2;
			}

			nl = element.GetElementsByTagName("DirectionArrowLength");
			if (nl != null && nl.Count > 0)
			{
				this.DirectionArrowLength = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
			}
			else
			{
				this.DirectionArrowLength = 50;
			}

			nl = element.GetElementsByTagName("DirectionArrowMinZoomLevel");
			if (nl != null && nl.Count > 0)
			{
				this.DirectionArrowMinZoomLevel = int.Parse(nl[0].InnerText, System.Globalization.CultureInfo.InvariantCulture);
			}
			else
			{
				this.DirectionArrowMinZoomLevel = -1;
			}

			nl = element.GetElementsByTagName("DirectionArrowColor");
			if (nl != null && nl.Count > 0)
			{
				DirectionArrowColor = ColorTranslator.FromHtml(nl[0].InnerText);
				if (nl[0].Attributes["alpha"] != null)
				{
					int alpha;
					if (int.TryParse(((XmlElement)nl[0]).GetAttribute("alpha"), out alpha))
					{
						DirectionArrowColor = Color.FromArgb(alpha, DirectionArrowColor);
					}
				}
			}

            nl = element.GetElementsByTagName("Visible");
            if (nl != null && nl.Count > 0)
            {
                Visible = bool.Parse(nl[0].InnerText);
            }


        }


        /// <summary>
        /// Base64 Encode and image
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Internally the image is saved as a PNG before converting
        /// the PNG bytes to a base64 string</para>
        /// </remarks>
        protected static string EncodeImageAsBase64String(Image img)
        {
            if(img == null) return "";

            using (System.IO.MemoryStream m = new System.IO.MemoryStream())
            {
                img.Save(m, System.Drawing.Imaging.ImageFormat.Png);
                                
                // Convert byte[] to Base64 String
                return Convert.ToBase64String(m.ToArray());                
            }
        }

        /// <summary>
        /// Decode an image from a base64 encoded image string
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        protected static Image DecodeImageFromBase64String(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;            
            return Image.FromStream(new System.IO.MemoryStream(Convert.FromBase64String(base64String)));
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
                if (this.dbfReader != null && this.dbfReaderIsOwned) dbfReader.Dispose();
				this.dbfReader = null;
                //don't dispose as symbolImage may have been set 
                //in multiple RenderSettings objects by the user 
                //if (symbolImage != null)
                //{
                //    symbolImage.Dispose();
                //}
                symbolImage = null;
            }
        }

        #endregion

        #region Update RenderSettings for new CRS

        public static void UpdateRenderSettings(RenderSettings renderSettings, PointD sourceCenterPoint, EGIS.Projections.ICRS sourceCRS, EGIS.Projections.ICRS targetCRS)
        {
            using (EGIS.Projections.ICoordinateTransformation transform = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sourceCRS, targetCRS))
            {
                if (renderSettings.MinZoomLevel > 0)
                {
                    //calculate sourceCRS extent at the zoom level for a 256x256 pixel rectangle
                    PointD bl = PixelCoordToGisPoint(0, 256, sourceCenterPoint, renderSettings.MinZoomLevel, 256, 256);
                    PointD tr = PixelCoordToGisPoint(256, 0, sourceCenterPoint, renderSettings.MinZoomLevel, 256, 256);
                    RectangleD bounds = RectangleD.FromLTRB(bl.X, bl.Y, tr.X, tr.Y);
                    //convert the bounds into the targetCRS bounds to calculate the new scale
                    RectangleD targetBounds = transform.Transform(bounds);
                    double scale = 256 / targetBounds.Width;
                    if (!double.IsNaN(scale))
                    {
                        renderSettings.MinZoomLevel = (float)scale;
                    }
                }

                if (renderSettings.MaxZoomLevel > 0)
                {
                    PointD bl = PixelCoordToGisPoint(0, 256, sourceCenterPoint, renderSettings.MaxZoomLevel, 256, 256);
                    PointD tr = PixelCoordToGisPoint(256, 0, sourceCenterPoint, renderSettings.MaxZoomLevel, 256, 256);
                    RectangleD bounds = RectangleD.FromLTRB(bl.X, bl.Y, tr.X, tr.Y);
                    RectangleD targetBounds = transform.Transform(bounds);
                    double scale = 256 / targetBounds.Width;
                    if (!double.IsNaN(scale))
                    {
                        renderSettings.MaxZoomLevel = (float)scale;
                    }
                }

                if (renderSettings.MinRenderLabelZoom > 0)
                {
                    PointD bl = PixelCoordToGisPoint(0, 256, sourceCenterPoint, renderSettings.MinRenderLabelZoom, 256, 256);
                    PointD tr = PixelCoordToGisPoint(256, 0, sourceCenterPoint, renderSettings.MinRenderLabelZoom, 256, 256);
                    RectangleD bounds = RectangleD.FromLTRB(bl.X, bl.Y, tr.X, tr.Y);
                    RectangleD targetBounds = transform.Transform(bounds);
                    double scale = 256 / targetBounds.Width;
                    if (!double.IsNaN(scale))
                    {
                        renderSettings.MinRenderLabelZoom = (float)scale;
                    }
                }

                if (renderSettings.PenWidthScale > 0)
                {
                    double scale = 1 / renderSettings.PenWidthScale;
                    PointD bl = PixelCoordToGisPoint(0, 256, sourceCenterPoint, scale, 256, 256);
                    PointD tr = PixelCoordToGisPoint(256, 0, sourceCenterPoint, scale, 256, 256);
                    RectangleD bounds = RectangleD.FromLTRB(bl.X, bl.Y, tr.X, tr.Y);
                    RectangleD targetBounds = transform.Transform(bounds);
                    scale = targetBounds.Width/256;
                    if (!double.IsNaN(scale))
                    {
                        renderSettings.PenWidthScale = (float)scale;
                    }
                }


            }
        }

        private static PointD PixelCoordToGisPoint(int pixX, int pixY, PointD centerPt, double scale, int w, int h)
        {
            double s = 1.0 / scale;           
            return new PointD(centerPt.X - (s * ((w >> 1) - pixX)), centerPt.Y + (s * ((h >> 1) - pixY)));
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

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private string[] standardValues;// = null;

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
