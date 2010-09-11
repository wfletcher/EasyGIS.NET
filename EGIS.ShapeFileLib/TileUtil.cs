using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.ShapeFileLib
{
    /// <summary>
    /// Utility class with methods to convert Lat Long locations to Tile Coordinates
    /// </summary>
    /// <remarks>
    /// <para>
    /// The TileUtil class is used by the EGIS.Web.Controls TiledSFMap to convert map tile image
    /// requests to actual Lat Long position and zoom level for rendering purposes
    /// </para>
    /// <para>Note that in order to use Map tiles the <see cref="EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection"/> property
    /// must be set to true</para>
    /// <para>
    /// Tiles are organised in a manner similar to the approach used by google maps and bing maps. Each tiles dimension is 256x256 pixels.
    /// A tile request is made up of a zoom-level between 0 and 16(inclusive), tile x-ccord and a tile y-ccord. At zoom-level
    /// 0 the entire world (-180 lon -> +180 lon) is scaled to fit in 1 tile. At level 1 the world will fit
    /// in 2 tiles x 2 tiles, at level 2 the world will fit into 4 tiles x 4 tiles, .. etc.     
    /// </para>
    /// <para>Tiles are numbered from zero in the upper left corner to (NumTiles at zoom-level)-1 as below:</para>
    /// <para>
    /// <code>
    /// (0,0) (1,0) (2,0) ..
    /// (0,1) (1,1) (2,1) ..
    /// (0,2) (1,2) (2,2) ..
    /// ..
    /// </code>      
    /// </para>
    /// <seealso cref="EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection"/>
    /// </remarks>
    
    public sealed class TileUtil
    {
        private TileUtil()
        {
        }

        /// <summary>
        /// Returns the tile number a given lat/long location lies in at a given zoom level
        /// </summary>
        /// <param name="lon"></param>
        /// <param name="lat"></param>
        /// <param name="zoomLevel"></param>
        /// <returns>a Point containing the tiles x,y coordinates</returns>
        public static System.Drawing.Point GetTileFromGisLocation(double lon, double lat, int zoomLevel)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", "zoomLevel");
            long x, y;
            LLToPixel(new PointD(lon, lat), zoomLevel, out x, out y);
            return new System.Drawing.Point((int)(x/256), (int)(y/256));
        }

        /// <summary>
        /// Returns the centre point (in Mercator Projection coordinates) of a given tile
        /// </summary>
        /// <param name="tileX">zero based tile x-coord</param>
        /// <param name="tileY">zero based tile y-ccord</param>
        /// <param name="zoomLevel"></param>
        /// /// <exception cref="System.ArgumentException">If zoomLevel less than zero</exception>
        /// <returns></returns>
        public static PointD GetMercatorCenterPointFromTile(int tileX, int tileY, int zoomLevel)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", "zoomLevel");
            return PixelToMerc(128 + (tileX * 256), 128+(tileY * 256), zoomLevel);
        }

        private const long l = 1;
        /// <summary>
        /// Converts a zoomLevel to its equivalent double-precision scaling
        /// </summary>
        /// <exception cref="System.ArgumentException">If zoomLevel less than zero</exception>
        /// <param name="zoomLevel"></param>
        /// <returns></returns>
        public static double ZoomLevelToScale(int zoomLevel)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", "zoomLevel");
            return (256.0/360.0)*(l<<zoomLevel);
        }

        /// <summary>
        /// Converts a double-precision scaling to equivalent tile zoom-level
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static int ScaleToZoomLevel(double scale)
        {
            return (int)Math.Round(Math.Log(scale*360/256.0, 2));
        }

        private static readonly PointD MaxMerc = ShapeFile.LLToMercator(new PointD(180, 90));

        public static void LLToPixel(PointD latLong, int zoomLevel, out long x, out long y)
        {
            //convert LL to Mercatator
            PointD merc = ShapeFile.LLToMercator(latLong);
            double scale = ZoomLevelToScale(zoomLevel);
            x = (long)Math.Round((merc.X+MaxMerc.X) * scale);
            y = (long)Math.Round((MaxMerc.Y-merc.Y) * scale);
        }

        public static PointD PixelToLL(int pixX, int pixY, int zoomLevel)
        {
            return ShapeFile.MercatorToLL(PixelToMerc(pixX, pixY, zoomLevel));
        }

        public static PointD PixelToMerc(int pixX, int pixY, int zoomLevel)
        {
            double d = 1.0 / ZoomLevelToScale(zoomLevel);
            return new PointD((d * pixX) - 180, 180 - (d * pixY));
        }
        
    }


}
