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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EGIS.Controls
{

	/// <summary>
	/// Base Map Layer for displaying tiled image layers like OSM, ESRI satellite etc.
	/// </summary>
	/// <remarks>
	/// <para>
	/// IF a BaseMapLayer is added to a SFMap the CRS of the map must be set to Wgs84PseudoMercatorEpsgCode (3857)
	/// </para>
	/// </remarks>
	public class BaseMapLayer : IDisposable
	{

		private EGIS.Controls.SFMap mapReference;

		private TileCollection tileCollection;


		/// <summary>
		/// BaseMapLayer constructor
		/// </summary>
		/// <param name="map">Reference to the SFMap control the BaseMapLayer will be rendered</param>
		/// <param name="tileSource">The TileSource where image tiles should be retrieved</param>
		public BaseMapLayer(EGIS.Controls.SFMap map, TileSource tileSource)
		{
			this.mapReference = map;
			map.MapCoordinateReferenceSystem = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);
			map.PaintMapBackground += Map_PaintMapBackground;
			map.ZoomLevelChanged += Map_ZoomLevelChanged;
			this.TileSource = tileSource;
			this.Transparency = 1;
			map.Invalidate();
		}

		private void Map_ZoomLevelChanged(object sender, EventArgs e)
		{
			if (!LayerIsValid()) return;

			//check zoom level is ok
			/*double currentZoom = mapReference.ZoomLevel;
			int zoomLevel = TileCollection.WebMercatorScaleToZoomLevel(currentZoom);
			if (zoomLevel < 0) zoomLevel = 0;
			double requiredZoom = TileCollection.ZoomLevelToWebMercatorScale(zoomLevel);
			if (Math.Abs(currentZoom - requiredZoom) > double.Epsilon)
			{
				mapReference.ZoomLevel = requiredZoom;
			}*/

		}

		private TileSource _tileSource;
		private bool disposedValue;

		/// <summary>
		/// Get/Set the BaseMapLayer TileSource
		/// </summary>
		/// <remarks>
		/// If TileSource is null or Nothing then the BaseMapLayer wil lnot render any tiles. Set null to "hide" the BaseMapLayer
		/// </remarks>
		public TileSource TileSource
		{
			get { return _tileSource; }
			set
			{
				var previousTileSource = _tileSource;
				if (value != previousTileSource)
				{
					_tileSource = value;
					if (previousTileSource == null)
					{
						//if TileSource changed from null set the maps CRS to Wgs84PseudoMercator
						mapReference.MapCoordinateReferenceSystem = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);
					}
					mapReference.InvalidateAndClearBackground();
				}
			}

		}

		/// <summary>
		/// Transparency of the BaseMapLayer. 0 is off, 1 is full opacity
		/// </summary>
		public float Transparency
		{
			get;
			set;
		}

		/// <summary>
		/// checks if TileSource and map refererence in validd state
		/// </summary>
		/// <returns></returns>
		private bool LayerIsValid()
		{
			return !(this.TileSource == null ||
				this.mapReference.MapCoordinateReferenceSystem == null ||
				!string.Equals(this.mapReference.MapCoordinateReferenceSystem.Id, EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
		}

		private void Map_PaintMapBackground(object sender, System.Windows.Forms.PaintEventArgs e)
		{
            try
            {
				if (!LayerIsValid()) return;
				DrawMap(e.Graphics, Math.Max(0, Math.Min(Transparency, 1.0f)), this.TileSource, this.mapReference);
            }
            catch
            {
            }
		}
		 

		private void DrawMap(Graphics g, float transparency, TileSource tileSource, EGIS.Controls.SFMap map )
		{
			
			TileCollection tiles = new TileCollection(map.ZoomLevel, map.CentrePoint2D, map.ClientSize.Width, map.ClientSize.Height, map, tileSource.Urls, tileSource.MaxZoomLevel);
			if (this.tileCollection != null)
			{
				//abort if our zoom level has changed
				if (this.tileCollection.ZoomLevel != tiles.ZoomLevel)
				{
					this.tileCollection.Abort();
				}
			}
			this.tileCollection = tiles;
			try
			{
				tiles.Render(g, transparency);
			}
			catch (Exception ex)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(ex);
				throw;
#endif
			}

		}

		/// <summary>
		/// IDisposable method. Called from Dispose() method. If overrideing this method call base
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}
				this.mapReference.PaintMapBackground -= Map_PaintMapBackground;
				this.mapReference.ZoomLevelChanged -= Map_ZoomLevelChanged;
				this.mapReference = null;
				this.tileCollection = null;
				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~BaseMapLayer()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		/// <summary>
		/// IDisposable Dispose method
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}



	
}
