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
    /// Tiles are organised in a manner similar to the approach used by google maps and bing maps. Each tiles dimension is by default 256x256 pixels.
    /// A tile request is made up of a zoom-level between 0 and 16(inclusive), tile x-ccord and a tile y-coord. At zoom-level
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
        /// <param name="tileSize"></param>
        /// <returns>a Point containing the tiles x,y coordinates</returns>
        public static System.Drawing.Point GetTileFromGisLocation(double lon, double lat, int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
            long x, y;
            LLToPixel(new PointD(lon, lat), zoomLevel, out x, out y, tileSize);
            return new System.Drawing.Point((int)(x/ tileSize), (int)(y/ tileSize));
        }

        /// <summary>
        /// Returns the centre point (in Mercator Projection coordinates) of a given tile
        /// </summary>
        /// <param name="tileX">zero based tile x-coord</param>
        /// <param name="tileY">zero based tile y-ccord</param>
        /// <param name="zoomLevel"></param>
        /// <param name="tileSize">Size of tiles. must be power of 2. Default size is 256</param>
        /// <exception cref="System.ArgumentException">If zoomLevel less than zero</exception>
        /// <returns></returns>
        public static PointD GetMercatorCenterPointFromTile(int tileX, int tileY, int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
            return PixelToMerc((tileSize>>1) + (tileX * tileSize), (tileSize>>1)+(tileY * tileSize), zoomLevel, tileSize);
        }

        public static RectangleD GetTileLatLonBounds(int tileX, int tileY, int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
            PointD topLeft = PixelToLL((tileX * tileSize), (tileY * tileSize), zoomLevel, tileSize);
            PointD bottomRight = PixelToLL(((tileX+1) * tileSize), ((tileY+1) * tileSize), zoomLevel, tileSize);
            return RectangleD.FromLTRB(topLeft.X, bottomRight.Y, bottomRight.X, topLeft.Y);
        }

        private const long l = 1;
        /// <summary>
        /// Converts a zoomLevel to its equivalent double-precision scaling
        /// </summary>
        /// <exception cref="System.ArgumentException">If zoomLevel less than zero</exception>
        /// <param name="zoomLevel"></param>
        /// <param name="tileSize">Size of tiles. must be power of 2. Default size is 256</param>
        /// <returns></returns>
        public static double ZoomLevelToScale(int zoomLevel, int tileSize=256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
            return ((double)tileSize/360.0)*(l<<zoomLevel);
        }

        /// <summary>
        /// Converts a double-precision scaling to equivalent tile zoom-level
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="tileSize">tile size. Must be power of 2. Default size is 256</param>
        /// <returns></returns>
        public static int ScaleToZoomLevel(double scale, int tileSize=256)
        {
            return (int)Math.Round(Math.Log(scale*360/(double)tileSize, 2));
        }

        private static readonly PointD MaxMerc = ShapeFile.LLToMercator(new PointD(180, 90));

       
        public static void LLToPixel(PointD latLong, int zoomLevel, out long x, out long y, int tileSize=256)
        {
            //convert LL to Mercatator
            PointD merc = ShapeFile.LLToMercator(latLong);
            double scale = ZoomLevelToScale(zoomLevel, tileSize);
            x = (long)Math.Round((merc.X+MaxMerc.X) * scale);
            y = (long)Math.Round((MaxMerc.Y-merc.Y) * scale);
        }   

        public static PointD PixelToLL(int pixX, int pixY, int zoomLevel, int tileSize=256)
        {
            return ShapeFile.MercatorToLL(PixelToMerc(pixX, pixY, zoomLevel, tileSize));
        }

        public static PointD PixelToMerc(int pixX, int pixY, int zoomLevel, int tileSize=256)
        {
            double d = 1.0 / ZoomLevelToScale(zoomLevel, tileSize);
            return new PointD((d * pixX) - 180, 180 - (d * pixY));
        }

        /// <summary>
        /// normalises tile coordinates. tile coordiates wrap around to zero at max tile number
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="zoomLevel"></param>
        public static void NormaliseTileCoordinates(ref int tileX, ref int tileY, int zoomLevel)
        {
            if (zoomLevel < 0) return;
            int maxTilesAtZoomLevel = 1 << zoomLevel;
            tileX = tileX % maxTilesAtZoomLevel;
            if (tileX < 0) tileX += maxTilesAtZoomLevel;
            tileY = tileY % maxTilesAtZoomLevel;
            if (tileY < 0) tileY += maxTilesAtZoomLevel;
        }


        private const double Wgs84SemiMajorAxis = 6378137;


        public static PointD GetWebMercatorCenterPointFromTile(int tileX, int tileY, int zoomLevel, int tileSize = 256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
            PointD merc =  PixelToMerc((tileSize >> 1) + (tileX * tileSize), (tileSize >> 1) + (tileY * tileSize), zoomLevel, tileSize);            
            return new PointD(merc.X * Wgs84SemiMajorAxis * Math.PI / 180, merc.Y * Wgs84SemiMajorAxis * Math.PI / 180);
        }
       
        public static double ZoomLevelToWebMercatorScale(int zoomLevel, int tileSize = 256)
        {
            if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
            return ((double)tileSize / (Math.PI * 2 * Wgs84SemiMajorAxis)) * (l << zoomLevel);
        }


        /// <summary>
        /// returns (integer) tile-based zoom level from double precision map scale when using Wgs84PseudoMercator
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        public static int WebMercatorScaleToZoomLevel(double scale, int tileSize = 256)
        {
            return (int)Math.Round(Math.Log(scale * (Math.PI * 2 * Wgs84SemiMajorAxis) / (double)tileSize, 2));
        }        

       

        /// <summary>
        /// return the maximum number of pixels at a given tile zoom level
        /// </summary>
        /// <param name="zoomLevel"></param>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        /// <remarks>
        /// At tile level 0 max pixels will be the tileSize, at level 1 it will be 2xtilesize, doubling for each zoom level
        /// </remarks>
        private static long MaxPixelsAtTileZoomLevel(int zoomLevel, int tileSize = 256)
        {
            long maxTilesAtZoomLevel = 1 << zoomLevel;
            return maxTilesAtZoomLevel * tileSize;
        }

                      
        /// <summary>
        /// returns the pixel coordinates of a web mercator coordinate at a given tile zoom level
        /// </summary>
        /// <param name="coord">the coordinate in Wgs84PseudoMercator coordinates</param>
        /// <param name="zoomLevel">0 based zoom level</param>
        /// <param name="x">the calculated pixel x coord</param>
        /// <param name="y">the calculated pixel y coord</param>
        /// <param name="tileSize"></param>
        public static void WebMercatorToPixel(PointD coord, int zoomLevel, out long x, out long y, int tileSize=256)
        {
            //double scale = ZoomLevelToWebMercatorScale(zoomLevel, tileSize);
            //x = (long)Math.Round((coord.X - (Math.PI * Wgs84SemiMajorAxis)) * scale);
            //y = (long)Math.Round((coord.Y - (Math.PI * Wgs84SemiMajorAxis)) * -scale);
            long scale = l << zoomLevel;
            x = (long)Math.Floor(tileSize * (0.5 + coord.X / (Math.PI * 2 * Wgs84SemiMajorAxis))*scale) ;
            y = (long)Math.Floor(tileSize * (0.5 - coord.Y / (Math.PI * 2 * Wgs84SemiMajorAxis))*scale) ;
        }

        /// <summary>
        /// returns the Wgs84PseudoMercator coordinate from given pixel and zoomLevel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoomLevel"></param>
        /// <param name="tileSize"></param>
        /// <returns></returns>
        public static PointD PixelToWebMercator(long x, long y, int zoomLevel, int tileSize=256)
        {
            PointD coord;
            //double scale = (1 << zoomLevel)*tileSize;
            double scale = ((long)tileSize) << zoomLevel;

            coord = new PointD(((double)x / scale - 0.5) * (Math.PI * 2 * Wgs84SemiMajorAxis), -((double)y / scale - 0.5) * Math.PI * 2 * Wgs84SemiMajorAxis);
            return coord;
        }




        public static void Test()
        {
            PointD pt1 = new PointD(-180, 0);
            PointD pt2 = new PointD(180, 0);
            PointD pt3 = new PointD(0, 0);
            Int64 x, y;
            int zoomLevel = 0;
            LLToPixel(pt1, zoomLevel, out x, out y);
            Console.Out.WriteLine("LLToPixel: Zoom:{3} LL:{0}, x:{1},y:{2}", pt1, x, y, zoomLevel);
            LLToPixel(pt2 , zoomLevel, out x, out y);
            Console.Out.WriteLine("LLToPixel: Zoom:{3} LL:{0}, x:{1},y:{2}", pt2, x, y, zoomLevel);
            LLToPixel(pt3, zoomLevel, out x, out y);
            Console.Out.WriteLine("LLToPixel: Zoom:{3} LL:{0}, x:{1},y:{2}", pt3, x, y, zoomLevel);


            zoomLevel = 1;
            LLToPixel(pt1, zoomLevel, out x, out y);
            Console.Out.WriteLine("LLToPixel: Zoom:{3} LL:{0}, x:{1},y:{2}", pt1, x, y, zoomLevel);
            LLToPixel(pt2, zoomLevel, out x, out y);
            Console.Out.WriteLine("LLToPixel: Zoom:{3} LL:{0}, x:{1},y:{2}", pt2, x, y, zoomLevel);
            LLToPixel(pt3, zoomLevel, out x, out y);
            Console.Out.WriteLine("LLToPixel: Zoom:{3} LL:{0}, x:{1},y:{2}", pt3, x, y, zoomLevel);

            RectangleD tileBounds = GetTileLatLonBounds(0, 0, 0);
            Console.Out.WriteLine("zoom:{0} x:{1} y:{2} bounds:{3}", 0, 0, 0, tileBounds);

            tileBounds = GetTileLatLonBounds(0, 0, 1);
            Console.Out.WriteLine("zoom:{0} x:{1} y:{2} bounds:{3}", 0, 0, 0, tileBounds);

            zoomLevel=14;
            double scale;

            scale = ZoomLevelToScale(zoomLevel);
            Console.Out.WriteLine("zoomLevel {0} ZoomLevelToScale -> scale = {1:0.00000}", zoomLevel, scale);
            zoomLevel = ScaleToZoomLevel(scale);
            Console.Out.WriteLine("scale {0} ScaleToZoomLevel -> zoomLevel = {1:0.00000}", scale, zoomLevel);


            scale = ZoomLevelToWebMercatorScale(zoomLevel);
            Console.Out.WriteLine("zoomLevel {0} ZoomLevelToWebMercatorScale -> scale = {1:0.00000}", zoomLevel, scale);
            zoomLevel = WebMercatorScaleToZoomLevel(scale);
            Console.Out.WriteLine("scale {0:0.00000} WebMercatorScaleToZoomLevel -> zoomLevel = {1}", scale, zoomLevel);

            zoomLevel = 0;
            scale = ZoomLevelToWebMercatorScale(zoomLevel);
            Console.Out.WriteLine("zoomLevel {0} ZoomLevelToWebMercatorScale -> scale = {1:0.00000}", zoomLevel, scale);
            zoomLevel = WebMercatorScaleToZoomLevel(scale);
            Console.Out.WriteLine("scale {0:0.00000} WebMercatorScaleToZoomLevel -> zoomLevel = {1}", scale, zoomLevel);

            zoomLevel = 10;
            scale = ZoomLevelToWebMercatorScale(zoomLevel);
            Console.Out.WriteLine("zoomLevel {0} ZoomLevelToWebMercatorScale -> scale = {1:0.00000}", zoomLevel, scale);
            zoomLevel = WebMercatorScaleToZoomLevel(scale);
            Console.Out.WriteLine("scale {0:0.00000} WebMercatorScaleToZoomLevel -> zoomLevel = {1}", scale, zoomLevel);

            zoomLevel = 31;
            scale = ZoomLevelToWebMercatorScale(zoomLevel);
            Console.Out.WriteLine("zoomLevel {0} ZoomLevelToWebMercatorScale -> scale = {1:0.00000}", zoomLevel, scale);
            zoomLevel = WebMercatorScaleToZoomLevel(scale);
            Console.Out.WriteLine("scale {0:0.00000} WebMercatorScaleToZoomLevel -> zoomLevel = {1}", scale, zoomLevel);

            zoomLevel = 0;

            PointD coord = new PointD(0, 0);
            
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(-Math.PI * Wgs84SemiMajorAxis + 0.1, -Math.PI * Wgs84SemiMajorAxis + 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(Math.PI * Wgs84SemiMajorAxis - 0.1, -Math.PI * Wgs84SemiMajorAxis + 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(Math.PI * Wgs84SemiMajorAxis - 0.1, Math.PI * Wgs84SemiMajorAxis - 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(-Math.PI * Wgs84SemiMajorAxis + 0.1, Math.PI * Wgs84SemiMajorAxis - 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);


            
            zoomLevel+=4;
            coord = new PointD(0, 0);

            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(-Math.PI * Wgs84SemiMajorAxis + 0.1, -Math.PI * Wgs84SemiMajorAxis + 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(Math.PI * Wgs84SemiMajorAxis - 0.1, -Math.PI * Wgs84SemiMajorAxis + 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(Math.PI * Wgs84SemiMajorAxis - 0.1, Math.PI * Wgs84SemiMajorAxis - 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(-Math.PI * Wgs84SemiMajorAxis + 0.1, Math.PI * Wgs84SemiMajorAxis - 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);

            WebMercatorToPixel(coord, zoomLevel, out x, out y, 512);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            coord = new PointD(Math.PI * Wgs84SemiMajorAxis - 0.1, -Math.PI * Wgs84SemiMajorAxis + 0.1);
            WebMercatorToPixel(coord, zoomLevel, out x, out y, 512);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);


            zoomLevel = 1;
            x = y = 255;
            coord = PixelToWebMercator(x, y, zoomLevel);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);


            x = y = 0;
            coord = PixelToWebMercator(x, y, zoomLevel);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);

            x = 511;
            coord = PixelToWebMercator(x, y, zoomLevel);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);


            zoomLevel = 10;
            x = y = 255;
            coord = PixelToWebMercator(x, y, zoomLevel);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);


            x = y = 0;
            coord = PixelToWebMercator(x, y, zoomLevel);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);

            x = 511;
            coord = PixelToWebMercator(x, y, zoomLevel);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);

            x = y = 511;
            coord = PixelToWebMercator(x, y, zoomLevel, 512);
            Console.Out.WriteLine("PixelToWebMercator: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);
            WebMercatorToPixel(coord, zoomLevel, out x, out y, 512);
            Console.Out.WriteLine("WebMercatorToPixel: zoom level:{0} coord:{1} -> pixX:{2}, pixY:{3}", zoomLevel, coord, x, y);





        }
    }


}
