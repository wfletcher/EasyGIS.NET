#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2022 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.Controls class library of Easy GIS .NET.
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
using EGIS.ShapeFileLib;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Cache;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

namespace EGIS.Controls
{
	/// <summary>
	/// TileCollection class used by BaseMapLayer 
	/// </summary>
    class TileCollection
    {
        private int tileCountX;
        private int tileCountY;
        Tile[] tiles;

		private static HttpClient httpClient;

		private const double MaxLLMercProjD = 85.0511287798066;

		//public const string UserAgentHeader = @"Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko"; //ie11

		static TileCollection()
		{


			HttpClientHandler handler = new HttpClientHandler()
			{
				//requires .net 4.7.1 or greater
#if NET471_OR_GREATER
				MaxConnectionsPerServer = 8
#endif
			};
			httpClient = new HttpClient(handler);
			httpClient.DefaultRequestHeaders.Add("User-Agent", "C# App");			
		}

		public TileCollection(double mapScale, PointD mapCenter, int pixelWidth, int pixelHeight, EGIS.Controls.SFMap map, string[] imageUrlFormat, int tileSourceMaxZoomLevel=25, bool useWmsBoundingBoxFormat=false)//, HttpClient httpClient)
		{
			int zoomLevel = WebMercatorScaleToZoomLevel(mapScale);
			long maxPixelsAtZoom = MaxPixelsAtTileZoomLevel(zoomLevel);
			long centerPixelX, centerPixelY, topPixel, bottomPixel, leftPixel, rightPixel;
		
			WebMercatorToPixel(map.CentrePoint2D, zoomLevel, out centerPixelX, out centerPixelY);

            this.ZoomLevel = zoomLevel;
			//Console.Out.WriteLine("ZoomLevel:" + ZoomLevel);
			
			//calculate tlrb mercator pixel coords
			topPixel = centerPixelY - (pixelHeight >> 1);
			bottomPixel = topPixel + pixelHeight;			
			leftPixel = centerPixelX - (pixelWidth >> 1);
			rightPixel = leftPixel + pixelWidth;		

			Point topLeftTile = new Point((int)Math.Floor(leftPixel / 256.0), (int)Math.Floor(Math.Max(0,topPixel) / 256.0));
			Point bottomRightTile = new Point((int)Math.Floor(rightPixel / 256.0), (int)Math.Floor(Math.Min(bottomPixel, maxPixelsAtZoom-1) / 256.0));

			Point topLeftTilePixelOffset = new Point((int)((long)topLeftTile.X * 256 - leftPixel),
				(int)((long)topLeftTile.Y * 256 - topPixel));

			this.tileCountX = bottomRightTile.X - topLeftTile.X + 1;
			this.tileCountY = bottomRightTile.Y - topLeftTile.Y + 1;

            if (tileCountX > 0 && tileCountY > 0)
            {
                tiles = new Tile[tileCountX * tileCountY];

                //Console.Out.WriteLine("mapCenter:" + mapCenter);
                //Console.Out.WriteLine("center pixel x:{0}, y:{1}", centerPixelX, centerPixelY);
                //Console.Out.WriteLine("pixelWidth:{0}, pixelHeight:{1}", pixelWidth, pixelHeight);
                //Console.Out.WriteLine("leftPixel:{0}, rightPixel:{1}", leftPixel, rightPixel);
                //Console.Out.WriteLine("topPixel:{0}, bottomPixel:{1}", topPixel, bottomPixel);
                //Console.Out.WriteLine("topLeftTile:{0},{1}", topLeftTile.X, topLeftTile.Y);
                //Console.Out.WriteLine("bottomRightTile:{0},{1}", bottomRightTile.X, bottomRightTile.Y);
                //Console.Out.WriteLine("topLeftTilePixelOffset:{0},{1}", topLeftTilePixelOffset.X, topLeftTilePixelOffset.Y);
                //Console.Out.WriteLine("tile count:{0},{1}\n", tileCountX, tileCountY);


                int dy = topLeftTilePixelOffset.Y;
                int tileIndex = 0;
                for (int y = 0; y < tileCountY; ++y)
                {
                    int dx = topLeftTilePixelOffset.X;
                    for (int x = 0; x < tileCountX; ++x)
                    {
                        Point tileCoord = new Point(topLeftTile.X + x, topLeftTile.Y + y);
                        Point imageOffset = new Point(0, 0);
                        int tileZCoord = zoomLevel;
                        float scale = 1.0f;


                        //if zoomLevel is > tileSource max zoom level
                        //request tiles at max zoom level and apply an offset + scale when drawing
                        if (zoomLevel > tileSourceMaxZoomLevel)
                        {
                            int xx = tileCoord.X >> (zoomLevel - tileSourceMaxZoomLevel);
                            int yy = tileCoord.Y >> (zoomLevel - tileSourceMaxZoomLevel);
                            scale = 1 << (zoomLevel - tileSourceMaxZoomLevel);
                            imageOffset.X = (tileCoord.X - (xx << (zoomLevel - tileSourceMaxZoomLevel))) * (256 >> (zoomLevel - tileSourceMaxZoomLevel));
                            imageOffset.Y = (tileCoord.Y - (yy << (zoomLevel - tileSourceMaxZoomLevel))) * (256 >> (zoomLevel - tileSourceMaxZoomLevel));

                            tileCoord.X = xx;
                            tileCoord.Y = yy;
                            tileZCoord = tileSourceMaxZoomLevel;

                            //Console.Out.WriteLine("scale: " + scale);
                            //Console.Out.WriteLine("imageOffset: " + imageOffset);

                        }

						if (useWmsBoundingBoxFormat)
						{
							tiles[tileIndex] = new WmsTile(tileCoord.X, tileCoord.Y, tileZCoord, dx, dy, imageOffset, scale, map, imageUrlFormat[x % imageUrlFormat.Length], httpClient);
						}
						else
						{
							tiles[tileIndex] = new Tile(tileCoord.X, tileCoord.Y, tileZCoord, dx, dy, imageOffset, scale, map, imageUrlFormat[x % imageUrlFormat.Length], httpClient);
						}
                        dx += 256;
                        tileIndex++;
                    }
                    dy += 256;
                }

            }
		}


        public TileCollection(double mapScale, PointD mapCenter, int pixelWidth, int pixelHeight, EGIS.Controls.SFMap map)//, HttpClient httpClient)
            : this(mapScale, mapCenter, pixelWidth, pixelHeight, map, new string[]{ Tile.DefaultImageUrl})//, httpClient)
        {
                        
        }

		public int ZoomLevel
		{
			get;
			private set;
		}

		private static long MaxPixelsAtTileZoomLevel(int zoomLevel, int tileSize = 256)
		{
			long maxTilesAtZoomLevel = 1 << zoomLevel;
			return maxTilesAtZoomLevel * tileSize;
		}

        
		private const double Wgs84SemiMajorAxis = 6378137;
		private const long l = 1;

		
		public static double ZoomLevelToWebMercatorScale(int zoomLevel, int tileSize = 256)
		{
			if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
			return ((double)tileSize / (Math.PI * 2 * Wgs84SemiMajorAxis)) * (l << zoomLevel);
		}
		

		public static int WebMercatorScaleToZoomLevel(double scale, int tileSize = 256)
		{
			return (int)Math.Round(Math.Log(scale * (Math.PI * 2 * Wgs84SemiMajorAxis) / (double)tileSize, 2));
		}

		public static void WebMercatorToPixel(PointD coord, int zoomLevel, out long x, out long y, int tileSize = 256)
		{
			//double scale = ZoomLevelToWebMercatorScale(zoomLevel);
			//x = (long)Math.Round( (coord.X - (Math.PI * Wgs84SemiMajorAxis)) * scale);
			//y = (long)Math.Round((coord.Y - (Math.PI * Wgs84SemiMajorAxis)) * -scale);

			long scale = l << zoomLevel;
			x = (long)Math.Floor(tileSize * (0.5 + coord.X / (Math.PI * 2 * Wgs84SemiMajorAxis)) * scale);
			y = (long)Math.Floor(tileSize * (0.5 - coord.Y / (Math.PI * 2 * Wgs84SemiMajorAxis)) * scale);
		}
		

		/// <summary>
		/// Render the TileCollection
		/// </summary>
		/// <param name="g"></param>
		/// <param name="transparency">trapsarency between 0.0 - 1.0 </param>
		public void Render(Graphics g, float transparency)
        {
            if (tiles == null) return;
            for (int n = 0; n < this.tiles.Length; ++n)
            {
                tiles[n].Render(g, transparency);
            }                   
        }

		/// <summary>
		/// Aborts any pending tile requests
		/// </summary>
		public void Abort()
		{
			httpClient.CancelPendingRequests();
            if (tiles == null) return;
			foreach(var tile in tiles)
			{
				tile.Abort();
			}
		}
    }

    
    class Tile
    {        
        private EGIS.Controls.SFMap map;
        protected int x, y;
        protected int zoomLevel;
        private int pixOffX, pixOffY;
		private Point imageOffset;
		private float scale;
		private HttpClient httpClient;

        protected string imageUrlFormat = "";

        internal const string DefaultImageUrl = "https://a.tile.openstreetmap.org/{0}/{1}/{2}.png";

        public Tile(int x, int y, int zoomLevel, int pixOffX, int pixOffY, EGIS.Controls.SFMap map, HttpClient httpClient)
            : this(x, y, zoomLevel, pixOffX, pixOffY, Point.Empty, 1, map, DefaultImageUrl, httpClient)
        {
            
        }

        public Tile(int x, int y, int zoomLevel, int pixOffX, int pixOffY, Point imageOffset, float scale, EGIS.Controls.SFMap map, string imageUrlFormat, HttpClient httpClient)
        {
			TileUtil.NormaliseTileCoordinates(ref x, ref y, zoomLevel);
			this.x = x;
            this.y = y;
            this.zoomLevel = zoomLevel;
			
            this.pixOffX = pixOffX;
            this.pixOffY = pixOffY;
			this.imageOffset = imageOffset;
			this.scale = scale;
            this.map = map;
            this.imageUrlFormat = imageUrlFormat;
			this.httpClient = httpClient;
        }


        private const HttpRequestCacheLevel CacheLevel = HttpRequestCacheLevel.CacheIfAvailable;


#region methods to handle pending requests
        
        private static System.Collections.Concurrent.ConcurrentDictionary<string, bool> pendingRequests = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

        private static bool RequestPending(string url)
        {
            return pendingRequests.ContainsKey(url);
        }

        private static void RemovePendingRequest(string url)
        {
            bool b;
            pendingRequests.TryRemove(url, out b);
        }

        private static void AddPendingRequest(string url)
        {
            pendingRequests[url] = true;
        }
     
#endregion

        //public string UserAgentHeader = @"Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko"; //ie11
        
        //public string UserAgentHeader = @"Mozilla/5.0 (Windows NT 6.1; rv:72.0) Gecko/20100101 Firefox/72.0";  //firefox

	

#region image caching

		private static ImageCache<System.IO.MemoryStream> imageCache = new ImageCache<System.IO.MemoryStream>(500);

		private static System.IO.MemoryStream GetFromCache(string key)
		{
			return imageCache.GetItem(key);			
		}

		private static void PutInCache(string key, System.IO.MemoryStream ms)
		{
			imageCache.AddItem(key, ms);			
		}

#endregion

#region methods to retrieve image tiles and create tile bitmaps

		private void /*async Task*/ GetBitmapAsync(string url)
		{
			if (RequestPending(url))
			{
				return; //request already pending
			}

			AddPendingRequest(url);

			Task task = Task.Run(async () =>
			{
				System.IO.Stream responseStream = await httpClient.GetStreamAsync(url);
				var bitmapStream = new System.IO.MemoryStream();
				responseStream.CopyTo(bitmapStream);				
				PutInCache(url, bitmapStream);
				this.map.Invoke(new Action(() => this.map.InvalidateAndClearBackground()));
				RemovePendingRequest(url);
			});

		}
		

		protected virtual string FormatTileUrl()
		{
			string strUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, imageUrlFormat, this.zoomLevel, this.x, this.y);
			return strUrl;
		}

		private System.Drawing.Bitmap CreateBitmap()
		{			
			try
			{
				//string strUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,imageUrlFormat, this.zoomLevel, this.x, this.y);
				string strUrl = FormatTileUrl();

				System.IO.MemoryStream bitmapStream = GetFromCache(strUrl);
				if (bitmapStream != null)
				{
					bitmapStream.Seek(0, System.IO.SeekOrigin.Begin);
					using (Image img = Bitmap.FromStream(bitmapStream))
					{
						//copy the returned image to a new bitmap. If we don't do this then the transparency may not
						//display properly
						Bitmap bm = new Bitmap(img.Width, img.Height);
						using (Graphics g = Graphics.FromImage(bm))
						{
							g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
						}
						return bm;
					}
				}

				//request not in local cache - retrieve asyncronously
				//dont await this call!
				GetBitmapAsync(strUrl);

				Bitmap blankBitmap = new Bitmap(256, 256);
				using (Graphics g = Graphics.FromImage(blankBitmap))
				{
					g.Clear(Color.LightGray);
				}
				return blankBitmap;
			}
			catch// (Exception ex)
			{				
				Bitmap blankBitmap = new Bitmap(256, 256);
				using (Graphics g = Graphics.FromImage(blankBitmap))
				{
					g.Clear(Color.LightGray);

				}
				return blankBitmap;
			}
		}

#endregion

		internal void Render(Graphics g, float transparency)
        {
            using(Image bm = this.CreateBitmap())
            {
                float[][] ptsArray ={ 
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, transparency, 0}, 
                    new float[] {0, 0, 0, 0, 1}};
                ColorMatrix clrMatrix = new ColorMatrix(ptsArray);
                ImageAttributes imgAttributes = new ImageAttributes();
                imgAttributes.SetColorMatrix(clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

				imgAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);

				float sw = bm.Width / scale;
				float sh = bm.Height / scale;
				float sx = imageOffset.X;
				float sy = imageOffset.Y;

				if (scale > 1.2)
				{
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				}

				g.DrawImage(bm, new Rectangle(pixOffX, pixOffY, bm.Width, bm.Height), sx, sy,
					sw, sh, GraphicsUnit.Pixel, imgAttributes);


				//g.DrawImage(bm, new RectangleF(pixOffX, pixOffY, bm.Width, bm.Height), new RectangleF(imageOffset.X, imageOffset.Y,
				//	sw, sh), GraphicsUnit.Pixel);


				//g.DrawRectangle(Pens.Red, new Rectangle(pixOffX, pixOffY, bm.Width, bm.Height));

				//using (Font f = new Font("Aerial", 14))
				//{
				//	StringFormat sf = new StringFormat(StringFormatFlags.FitBlackBox);
				//	sf.Alignment = StringAlignment.Center;
				//	sf.LineAlignment = StringAlignment.Center;
				//	string strUrl = string.Format(imageUrlFormat, this.zoomLevel, this.x, this.y);
				//	g.DrawString(string.Format("x:{0},y:{1},z:{2}\n{3}", x, y, zoomLevel, strUrl), f, Brushes.DarkRed, new RectangleF(pixOffX, pixOffY, bm.Width, bm.Height), sf);
				//}
			}
		}


		internal void Abort()
		{
			string strUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,imageUrlFormat, this.zoomLevel, this.x, this.y);
			RemovePendingRequest(strUrl);
		}
    }

	class WmsTile : Tile
	{
		public WmsTile(int x, int y, int zoomLevel, int pixOffX, int pixOffY, Point imageOffset, float scale, EGIS.Controls.SFMap map, string imageUrlFormat, HttpClient httpClient)
			: base(x,y,zoomLevel,pixOffX,pixOffY, imageOffset, scale, map, imageUrlFormat, httpClient)
		{
		}

		static RectangleD GetWebMercatorTileLatLonBounds(int tileX, int tileY, int zoomLevel, int tileSize = 256)
		{
			if (zoomLevel < 0) throw new System.ArgumentException("zoomLevel must be >=0", nameof(zoomLevel));
			PointD topLeft = TileUtil.PixelToWebMercator((tileX * tileSize), (tileY * tileSize), zoomLevel, tileSize);
			PointD bottomRight = TileUtil.PixelToWebMercator(((tileX + 1) * tileSize), ((tileY + 1) * tileSize), zoomLevel, tileSize);
			return RectangleD.FromLTRB(topLeft.X, bottomRight.Y, bottomRight.X, topLeft.Y);
		}

		protected override string FormatTileUrl()
		{


			//https://www.e-education.psu.edu/geog585/node/699
			//bounding box specified by bottom left, top right coords
			//create bounding box
			var bounds = GetWebMercatorTileLatLonBounds(this.x, this.y, this.zoomLevel, 256);
			//0,5009377.085697314,2504688.5428486555,7514065.628545967
			string boundingBox = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00000000},{1:0.00000000},{2:0.00000000},{3:0.00000000}",
				bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
			string strUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, imageUrlFormat, boundingBox);
			return strUrl;
		}

	}

	/// <summary>
	/// simple class used to cache requested Tile responses 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class ImageCache<T>// where T : new()
	{
		private List<ImageCacheItem<T>> cachedItems = new List<ImageCacheItem<T>>();
		private int nextFreeIndex;
		private int maxItems;
		private Dictionary<string, int> cacheIndex = new Dictionary<string, int>();

	
		private object bufferSync = new object();

		public ImageCache(int maxItems = 200)
		{
			this.maxItems = maxItems;
		}

		
		public void AddItem(string key, T value)
		{
			lock (bufferSync)
			{
				int index;
				if (cacheIndex.TryGetValue(key, out index))
				{
					//overwrite and return;
					cachedItems[index].data = value;
					return;
				}
				if (cachedItems.Count < maxItems)
				{
					cachedItems.Add(new ImageCacheItem<T>()
					{
						data = value,
						key = key
					});
					cacheIndex[key] = cachedItems.Count - 1;
				}
				else
				{
					cacheIndex.Remove(cachedItems[nextFreeIndex].key);
					cachedItems[nextFreeIndex] = new ImageCacheItem<T>()
					{
						data = value,
						key = key
					};
					cacheIndex[key] = nextFreeIndex;
				}
				nextFreeIndex = (++nextFreeIndex) % maxItems;

			}
		}

		public T GetItem(string key)
		{
			//return null;
			lock (bufferSync)
			{
				int index = 0;
				if(!cacheIndex.TryGetValue(key, out index)) return default(T);

				return cachedItems[index].data;
			}

		}
	}

	class ImageCacheItem<T>
	{
		public string key;
		public T data;
	}
}
