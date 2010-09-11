using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    ///  The ICustomRenderSettings allows applying custom render settings to a shapefile layer
    /// </summary>
    /// <remarks>
    /// By setting an ICustomRenderSettings object on a shapefile's RenderSettings, you can gain full control
    /// over whether indivdual shapes should be rendered and the color to use when rendering the shapes. The ICustomRenderSettings interface also
    /// supplies methods to customize any tooltips used by the shapefile layer.
    /// </remarks>
    /// <seealso cref="EGIS.ShapeFileLib.RenderSettings"/>    
    public interface ICustomRenderSettings
    {
        /// <summary>
        /// Returns the FillColor to use when rendering the specified shape
        /// </summary>
        /// <param name="recordNumber">zero-based record number of the shape</param>
        /// <returns></returns>
        System.Drawing.Color GetRecordFillColor(int recordNumber);

        /// <summary>
        /// Returns the OutlineColor to use when rendering the specified shape
        /// </summary>
        /// <param name="recordNumber">zero-based record number of the shape</param>
        /// <returns></returns>
        System.Drawing.Color GetRecordOutlineColor(int recordNumber);


        /// <summary>
        /// Returns the Font Color to use when labelling the specified shape
        /// </summary>
        /// <param name="recordNumber">zero-based record number of the shape</param>
        /// <returns></returns>
        System.Drawing.Color GetRecordFontColor(int recordNumber);


        /// <summary>
        /// Returns whether to render the specified shape
        /// </summary>
        /// <param name="recordNumber">zero-based record number of the shape</param>
        /// <returns></returns>
        bool RenderShape(int recordNumber);

        /// <summary>
        /// returns whether to use custom tooltips
        /// </summary>
        bool UseCustomTooltips
        {
            get;
        }

        /// <summary>
        /// returns the custom tooltip string for the specified shape
        /// </summary>
        /// <param name="recordNumber">zero-based record number of the shape</param>
        /// <returns></returns>
        string GetRecordToolTip(int recordNumber);

        /// <summary>
        /// returns whether custom Image Symbols should be used for shapes. Applys
        /// to Point type shapefiles only
        /// </summary>
        bool UseCustomImageSymbols
        {
            get;
        }

        /// <summary>
        /// returns the custom image symbol for the specified symbol.        
        /// </summary>
        /// <remarks>
        /// Implementing classes must ensure that a valid image is returned when this method
        /// is called. If a null Image is returned then an error will occur when the shapefile
        /// is rendered
        /// </remarks>
        /// <param name="recordNumber">zero-based record number of the shape</param>
        /// <returns></returns>
        System.Drawing.Image GetRecordImageSymbol(int recordNumber);
        


    }
}
