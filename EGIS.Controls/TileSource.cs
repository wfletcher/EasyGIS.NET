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
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EGIS.Controls
{
	/// <summary>
	/// represents a tile image map service
	/// </summary>
	public class TileSource
	{
		/// <summary>
		/// Name of the Tile Source
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// One or more Urls where the TileSource retrieves a tile
		/// </summary>
		/// <remarks>
		/// <para>
		/// The URL must use the following format <br/>
		/// http[s]://serveraddress/{0}/{1}/{2}.. where {0},{1} and {2} will be substituted for the Z,X,Y coordinates of the requested tile. <br/>
		/// </para>
		/// <para>
		/// Example: "https://b.tile.openstreetmap.org/{0}/{1}/{2}.png"
		/// </para>
		/// </remarks>
		public string[] Urls
		{
			get;
			set;
		}

		/// <summary>
		/// The maximum zoom level of the tile source.
		/// </summary>
		public int MaxZoomLevel
		{
			get;
			set;
		}

		/// <summary>
		/// ToString overrride
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.Name;
		}


		public bool UseWmsBoundingBoxFormat
		{
			get;
			set;
		}

		/// <summary>
		/// returns an array of default TileSource objects
		/// </summary>
		/// <returns></returns>
		public static TileSource[] DefaultTileSources()
		{
			
			var tileSourceList = new List<TileSource>();
			tileSourceList.Add(new TileSource()
			{

				Name = "Open Street Map",
				Urls = new string[]
				{
				"https://a.tile.openstreetmap.org/{0}/{1}/{2}.png",
				"https://b.tile.openstreetmap.org/{0}/{1}/{2}.png",
				"https://c.tile.openstreetmap.org/{0}/{1}/{2}.png"
				},
				MaxZoomLevel = 19
			});

			tileSourceList.Add(new TileSource()
			{

				Name = "Australian National Base Map",
				Urls = new string[]
				{
				"http://services.ga.gov.au/gis/rest/services/NationalBaseMap/MapServer/WMTS/tile/1.0.0/NationalBaseMap/default/GoogleMapsCompatible/{0}/{2}/{1}.png"
				},
				MaxZoomLevel = 16
			});

			tileSourceList.Add(new TileSource()
			{
				Name = "NASA Satellite imagery via ESRI",
				Urls = new string[]
				{
				"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{0}/{2}/{1}.jpg"

				},
				MaxZoomLevel = 19
			});

			tileSourceList.Add(new TileSource()
			{
				Name = "World Topo map via ESRI",
				Urls = new string[]
				{
				"https://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{0}/{2}/{1}.jpg"
				},
				MaxZoomLevel = 19
			});

			tileSourceList.Add(new TileSource()
			{
				Name="Test WMS Service",
				Urls = new string[]
				{
					"https://ies-ows.jrc.ec.europa.eu/gwis?service=WMS&request=GetMap&layers=admin.countries_borders&styles=&format=image%2Fpng&transparent=true&version=1.1.1&singletile=false&width=256&height=256&srs=EPSG%3A3857&bbox={0}"
				},
				MaxZoomLevel= 19,
				UseWmsBoundingBoxFormat = true
			});

			
			// https://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/0/0/0.jpg




			// get a Free API KEY from https://www.maptiler.com/ and uncomment the following lines

			string key = null;


			if (!string.IsNullOrEmpty(key))
			{
				tileSourceList.Add(new TileSource()
				{
					Name = "MapTiler Satellite",
					Urls = new string[]
					{
				"https://api.maptiler.com/tiles/satellite/{0}/{1}/{2}.jpg?key=" + key//YOUR_API_KEY"
					}
				});
				tileSourceList.Add(new TileSource()
				{

					Name = "MapTiler Voyager",
					Urls = new string[]
					{
				"https://api.maptiler.com/maps/voyager/256/{0}/{1}/{2}.png?key=" + key//YOUR_API_KEY"
					}
				});
			}

			return tileSourceList.ToArray();
		}
	}


}
