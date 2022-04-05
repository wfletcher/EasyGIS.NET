#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2020 Winston Fletcher.
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EGIS.Projections
{
    /// <summary>
    /// Factory class with members to assist in creating and transforming between Coordinate Reference Systems (CRS) 
    /// </summary>
    /// <remarks>
    /// <para>
    /// To access the factory class use the EGIS.Projections.CoordinateReferenceSystemFactory.Default member
    /// </para>
    /// <para>
    /// To create a CRS from Well Known Text (as stored in a shapefile .prj file) use: <br/>
    /// EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCRSFromWKT(wktString);
    /// <br/>
    /// To create a CRS from a EPGS code use: <br/>
    /// EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(code);
    ///     
    /// </para>
    /// <para>
    /// To transform coordinates from one CRS to another CRS create a ITransformation object:
    /// EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sourceCRS, targetCRS);
    /// <br/> or 
    /// <br/>
    /// EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(sourceWKT, targetWKT);
    /// </para>
    /// 
    /// <para>
    /// Internally EGIS uses Proj6 to support CRS operations.<br/>
    /// To use EGIS.Projections in your own projects make sure that the "Proj6" directory and all of its contents is 
    /// copied to the output directory of your project.
    /// </para>
    /// </remarks>
    public class CoordinateReferenceSystemFactory : ICRSFactory
    {
        /// <summary>
        /// key = srid, value = wkt
        /// </summary>
        private Dictionary<int, string> coordinateSystems = new Dictionary<int, string>();
        
        private List<IProjectedCRS> projectedCoordinateSystems = new List<IProjectedCRS>();
        private List<IGeographicCRS> geographicCoordinateSystems = new List<IGeographicCRS>();

        private object _sync = new object();

        protected CoordinateReferenceSystemFactory(string sridFilename)
        {
        }

        protected CoordinateReferenceSystemFactory()
        {
            LoadData();
        }

        private static object instance_sync = new object();
        private static CoordinateReferenceSystemFactory _instance;

        /// <summary>
        /// The default ICRSFactory instance.
        /// </summary>
        public static ICRSFactory Default
        {
            get
            {
                lock (instance_sync)
                {
                    if (_instance == null)
                    {
                        _instance = new CoordinateReferenceSystemFactory();
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// EPSG code for WGS84 (World Geodetic System) Ellipsoid
        /// </summary>
        public const int Wgs84EpsgCode = 4326;

        /// <summary>
        /// EPSG code for popular Web Mercator projection used for GoogleMaps, MapBox, OSM 
        /// </summary>
        public const int Wgs84PseudoMercatorEpsgCode = 3857;


        /// <summary>
        /// Confidence Threshold when identifying a CRS loaded from WKT. Default is 70, meaning two CRSs are equivalent but names are diferent.
        /// </summary>
        /// <remarks>
        /// See proj.org proj_identify function for more detail
        /// </remarks>
        public int IdentificationConfidenceThreshold
        {
            get { return Proj6.CRS.IdentificationConfidenceThreshold; }
            set {  Proj6.CRS.IdentificationConfidenceThreshold = value; }
        }


        /// <summary>
        /// Utility function to return the WGS84 UTM EPSG code for a given lat/lon coordinate
        /// </summary>
        /// <param name="longitude">longitude in decimal degrees</param>
        /// <param name="latitude">latitude in decimal degrees</param>
        /// <returns></returns>
        public static int GetWgs84UtmEpsgCode(double longitude, double latitude)
        {
            //Make sure the longitude is between -180.00 .. +180
            double longTemp = (longitude + 180) - (int)((longitude + 180) / 360) * 360 - 180; 
           
            int zoneNumber = (int)((longTemp + 180) / 6) + 1;

            
            //Norway exception which extends zone 32
            if (latitude >= 56.0 && latitude < 64.0 && longTemp >= 3.0 && longTemp < 12.0)  zoneNumber = 32;

            // Special zones for Svalbard
            if (latitude >= 72.0 && latitude < 84.0)
            {
                if (longTemp >= 0.0 && longTemp < 9.0) zoneNumber = 31;
                else if (longTemp >= 9.0 && longTemp < 21.0) zoneNumber = 33;
                else if (longTemp >= 21.0 && longTemp < 33.0) zoneNumber = 35;
                else if (longTemp >= 33.0 && longTemp < 42.0) zoneNumber = 37;
            }


            int epsg_code = 32600;
            epsg_code += zoneNumber;
            if (latitude < 0) // Southern zones
            {
                epsg_code += 100;
            }
            return epsg_code;
        }

        private void LoadData()
        {
            DateTime tick = DateTime.Now, tock;
            
            //var resourceName = "EGIS.Projections.SRID.csv.gz";
            //var assembly = Assembly.GetExecutingAssembly();
            //using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            //using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
            //using (StreamReader reader = new StreamReader(decompressionStream))
            //{
            //    LoadData(reader);
            //}

            //tock = DateTime.Now;
            //Console.Out.WriteLine("Loaded {0} Geographic Systems", GeographicCoordinateSystems.Count);
            //Console.Out.WriteLine("Loaded {0} Projection Systems", ProjectedCoordinateSystems.Count);
            //Console.Out.WriteLine("Total Systems: {0}", coordinateSystems.Count);
            //Console.Out.WriteLine("load time:" + tock.Subtract(tick).TotalSeconds + "s");

            tick = DateTime.Now;

            LoadDataFromDatabase();
            tock = DateTime.Now;
            Console.Out.WriteLine("Loaded {0} Geographic Systems", GeographicCoordinateSystems.Count);
            Console.Out.WriteLine("Loaded {0} Projection Systems", ProjectedCoordinateSystems.Count);
            Console.Out.WriteLine("Total Systems: {0}", coordinateSystems.Count);
            Console.Out.WriteLine("load time:" + tock.Subtract(tick).TotalSeconds + "s");


            bool networkEnabled = Proj6Native.proj_context_is_network_enabled(IntPtr.Zero) != 0;

            Console.Out.WriteLine("Proj9 network enabled:" + networkEnabled);


            //Use the following code to export to csv file
            //         List<string> authorites = new List<string>(new string[] { "EPSG" });// Proj6Native.Proj_get_authorities_from_database(IntPtr.Zero);
            //using (System.IO.StreamWriter writer = new StreamWriter(@"c:\temp\EPSG.csv"))
            //{
            //	foreach (string authority in authorites)
            //	{
            //		Console.Out.WriteLine(authority);
            //		List<string> codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_GEOGRAPHIC_2D_CRS codes.Count: " + codes.Count);

            //		for (int n = 0; n < codes.Count; ++n)
            //		{
            //			string code = codes[n];
            //			IntPtr p = Proj6Native.proj_create_from_database(IntPtr.Zero, authority, code, Proj6Native.PJ_CATEGORY.PJ_CATEGORY_CRS, 0, null);
            //			if (p != IntPtr.Zero)
            //			{
            //				string wkt = Proj6Native.Proj_as_wkt(IntPtr.Zero, p, PJ_WKT_TYPE.PJ_WKT2_2018_SIMPLIFIED, false);

            //				if (wkt != null)
            //				{
            //					writer.Write(code);
            //					writer.Write(";");
            //					writer.WriteLine(wkt);
            //				}
            //				Proj6Native.proj_destroy(p);
            //			}
            //		}

            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_PROJECTED_CRS codes.Count: " + codes.Count);

            //		for (int n = 0; n < codes.Count; ++n)
            //		{
            //			string code = codes[n];
            //			IntPtr p = Proj6Native.proj_create_from_database(IntPtr.Zero, authority, code, Proj6Native.PJ_CATEGORY.PJ_CATEGORY_CRS, 0, null);
            //			if (p != IntPtr.Zero)
            //			{
            //				string wkt = Proj6Native.Proj_as_wkt(IntPtr.Zero, p, PJ_WKT_TYPE.PJ_WKT2_2018_SIMPLIFIED, false);

            //				if (!string.IsNullOrEmpty(wkt))
            //				{
            //					writer.Write(code);
            //					writer.Write(";");
            //					writer.WriteLine(wkt);
            //				}
            //				Proj6Native.proj_destroy(p);
            //			}
            //		}

            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_CRS, 0);
            //		Console.Out.WriteLine("PJ_TYPE_CRS no depracated codes.Count: " + codes.Count);

            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_GEOGRAPHIC_2D_CRS codes.Count: " + codes.Count);
            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_3D_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_GEOGRAPHIC_3D_CRS codes.Count: " + codes.Count);

            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_PROJECTED_CRS codes.Count: " + codes.Count);

            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_BOUND_CRS codes.Count: " + codes.Count);

            //		codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_COMPOUND_CRS, 1);
            //		Console.Out.WriteLine("PJ_TYPE_COMPOUND_CRS codes.Count: " + codes.Count);




            //		Console.Out.WriteLine("-------");
            //	}
            //}
            //Console.Out.WriteLine("authorites.Count: " + authorites.Count);

            //tock = DateTime.Now;
            //Console.Out.WriteLine("load time:" + tock.Subtract(tick).TotalSeconds + "s");

        }

        private void LoadData(StreamReader reader)
        {
            this.coordinateSystems.Clear();
            this.projectedCoordinateSystems.Clear();
            this.geographicCoordinateSystems.Clear();

            int count = 0;
            {

                foreach (WKTString wkt in GetSRIDs(reader))
                {
                    try
                    {
                        
                        ++count;
                        ICRS crs = Proj6.CRS.FromWKT(wkt.WKT);
                        if(crs == null) continue;

                        if (string.IsNullOrEmpty(crs.Id))
                        {
                            ((Proj6.CRS)crs).Id = wkt.WKID.ToString();
                        }
                        
                        coordinateSystems.Add(wkt.WKID, wkt.WKT);
                        if (crs as IGeographicCRS != null)
                        {
                            geographicCoordinateSystems.Add(crs as IGeographicCRS);
                        }
                        else if (crs as IProjectedCRS != null)
                        {
                            projectedCoordinateSystems.Add(crs as IProjectedCRS);
                        }

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error loading wkt " + ex);
                    }
                }
            }            
        }


        private void LoadDataFromDatabase()
        {
            this.coordinateSystems.Clear();
            this.projectedCoordinateSystems.Clear();
            this.geographicCoordinateSystems.Clear();

            int count = 0;

            //Use the following code to export to csv file
            List<string> authorites = new List<string>(new string[] { "EPSG" });// Proj6Native.Proj_get_authorities_from_database(IntPtr.Zero);
            {
                foreach (string authority in authorites)
                {
                    Console.Out.WriteLine(authority);
                    List<string> codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_GEOGRAPHIC_2D_CRS codes.Count: " + codes.Count);

                    for (int n = 0; n < codes.Count; ++n)
                    {
                        string code = codes[n];
                        LoadCRS(authority, code);                        
                    }

                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_PROJECTED_CRS codes.Count: " + codes.Count);

                    for (int n = 0; n < codes.Count; ++n)
                    {
                        string code = codes[n];
                        LoadCRS(authority, code);
                    }

                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_CRS, 0);
                    Console.Out.WriteLine("PJ_TYPE_CRS no depracated codes.Count: " + codes.Count);

                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_GEOGRAPHIC_2D_CRS codes.Count: " + codes.Count);
                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_3D_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_GEOGRAPHIC_3D_CRS codes.Count: " + codes.Count);

                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_PROJECTED_CRS codes.Count: " + codes.Count);

                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_BOUND_CRS codes.Count: " + codes.Count);

                    codes = Proj6Native.Proj_get_codes_from_database(IntPtr.Zero, authority, Proj6Native.PJ_TYPE.PJ_TYPE_COMPOUND_CRS, 1);
                    Console.Out.WriteLine("PJ_TYPE_COMPOUND_CRS codes.Count: " + codes.Count);

                    Console.Out.WriteLine("-------");
                }
            }
            Console.Out.WriteLine("authorites.Count: " + authorites.Count);

        }

        private bool LoadCRS(string authority, string code)
        {
            IntPtr context = IntPtr.Zero;// Proj6Native.proj_context_create();
            IntPtr p = Proj6Native.proj_create_from_database(IntPtr.Zero, authority, code, Proj6Native.PJ_CATEGORY.PJ_CATEGORY_CRS, 0, null);
            if (p == IntPtr.Zero)
            {
                return false;
            }
            try
            {
                string wkt = Proj6Native.Proj_as_wkt(IntPtr.Zero, p, PJ_WKT_TYPE.PJ_WKT2_2018_SIMPLIFIED, false);

                if (wkt != null)
                {
                    ICRS crs = null;
                   
                    Proj6Native.PJ_TYPE pType = Proj6Native.proj_get_type(p);
                    string name = Proj6Native.GetName(p);
                    string authName = authority;// Proj6Native.GetAuthName(p);
                    string id = Proj6Native.ProjGetIdCode(p);

                    //this seems to be only needed if the crs was loaded from wkt
                    if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS && string.IsNullOrEmpty(id))
                    {
                        IntPtr srcCrs = Proj6Native.proj_get_source_crs(context, p);
                        if (srcCrs != IntPtr.Zero)
                        {
                            try
                            {
                                authName = Proj6Native.GetAuthName(srcCrs);
                                id = Proj6Native.ProjGetIdCode(srcCrs);
                            }
                            finally
                            {
                                Proj6Native.proj_destroy(srcCrs);
                            }
                        }
                    }

                    CRSBoundingBox areaOfUse = new CRSBoundingBox()
                    {
                        WestLongitudeDegrees = -1000,
                        NorthLatitudeDegrees = -1000,
                        EastLongitudeDegrees = -1000,
                        SouthLatitudeDegrees = -1000
                    };

                    Proj6Native.proj_get_area_of_use(context, p,
                        ref areaOfUse.WestLongitudeDegrees,
                        ref areaOfUse.SouthLatitudeDegrees,
                        ref areaOfUse.EastLongitudeDegrees,
                        ref areaOfUse.NorthLatitudeDegrees,
                        IntPtr.Zero);

                    if (!id.Equals(code))
                    {
                        Console.Out.WriteLine("id = " + id + ", code=" + code);
                    }

                    if (!authName.Equals(authority))
                    {
                        Console.Out.WriteLine("authName = " + authName + ", authority=" + authority);
                    }

                    if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS)
                    {
                        crs =  new Proj6.GeographicCRS()
                        {
                            Id = id,
                            Name = name,
                            Authority = authName,
                            WKT = wkt,
                            AreaOfUse = areaOfUse
                        };
                    }
                    else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS)
                    {
                        crs =  new Proj6.ProjectedCRS()
                        {
                            Id = id,
                            Name = name,
                            Authority = authName,
                            WKT = wkt,
                            UnitsToMeters = 1,
                            AreaOfUse = areaOfUse
                        };
                    }
                    else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS)
                    {
                        if (wkt.IndexOf("PROJECTION", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            crs =  new Proj6.ProjectedCRS()
                            {
                                Id = id,
                                Name = name,
                                Authority = authName,
                                WKT = wkt,
                                UnitsToMeters = 1,
                                AreaOfUse = areaOfUse
                            };
                        }
                        else
                        {
                            crs =  new Proj6.GeographicCRS()
                            {
                                Id = id,
                                Name = name,
                                Authority = authName,
                                WKT = wkt,
                                AreaOfUse = areaOfUse
                            };
                        }
                    }
                    else
                    {
                        //Console.Out.WriteLine("pType = " + pType);
                    }

                    if (crs == null) return false; 

                    if (string.IsNullOrEmpty(crs.Id))
                    {
                        ((Proj6.CRS)crs).Id = code;
                    }

                    coordinateSystems.Add(int.Parse(id, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture), wkt);
                    if (crs as IGeographicCRS != null)
                    {
                        geographicCoordinateSystems.Add(crs as IGeographicCRS);
                    }
                    else if (crs as IProjectedCRS != null)
                    {
                        projectedCoordinateSystems.Add(crs as IProjectedCRS);
                    }
                   
                }
            }
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Error loading CRS from database " + ex);
				return false;
			}
			finally
            {
                Proj6Native.proj_destroy(p);
            }
            return true;                        
        }

        private IEnumerable<WKTString> GetSRIDs(System.IO.StreamReader sr)
        {            
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                int split = line.IndexOf(';');
                if (split > -1)
                {
                    WKTString wkt = new WKTString();
                    wkt.WKID = int.Parse(line.Substring(0, split));
                    wkt.WKT = line.Substring(split + 1);
                    yield return wkt;
                }
            }            
        }


        #region ICRSFactory

        /// <summary>
        /// return list of geographic CRS
        /// </summary>
        public List<IGeographicCRS> GeographicCoordinateSystems
        {
            get { return this.geographicCoordinateSystems; }
        }

        /// <summary>
        /// return list of projected CRS
        /// </summary>
        public List<IProjectedCRS> ProjectedCoordinateSystems
        {
            get { return this.projectedCoordinateSystems; }
        }

        /// <summary>
        /// Get a CRS by (EPSG) code
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ICRS GetCRSById(int id)
        {
            string wkt;
            if (this.coordinateSystems.TryGetValue(id, out wkt))
            {
                ICRS crs = CreateCRSFromWKT(wkt);
                
                if (string.IsNullOrEmpty(crs.Id))
                {
                    ((Proj6.CRS)crs).Id = id.ToString();
                }
                return crs;
            }
            return null;
        }

        /// <summary>
        /// Creates a ICRS CoordinateReferenceSystem from a well known text string
        /// </summary>
        /// <param name="wkt"></param>
        /// <returns></returns>
        public ICRS CreateCRSFromWKT(string wkt)
        {
            lock (_sync)
            {
                return Proj6.CRS.FromWKT(wkt, true);
            }
        }

		/// <summary>
		/// Creates a ICRS CoordinateReferenceSystem from an ESRI prj file
		/// </summary>
		/// <param name="prjFile">path to shapefile .prj file</param>
		/// <returns></returns>
		public ICRS CreateCRSFromPrjFile(string prjFile)
		{
			using (System.IO.StreamReader reader = new StreamReader(prjFile))
			{
				string wkt = reader.ReadToEnd();
				return CreateCRSFromWKT(wkt);
			}				
		}

		/// <summary>
		/// creates a ICoordinateTransformation object used to transform coordinates
		/// from source CRS to target CRS
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public ICoordinateTransformation CreateCoordinateTrasformation(ICRS source, ICRS target)
        {
            if (source == null || target == null) throw new Exception("source and target ICRS cannot be null");

            lock (_sync)
            {
                return new Proj6.CoordinateTransformation(source, target);
            }
        }

        /// <summary>
        /// creates a ICoordinateTransformation object used to transform coordinates
        /// from source CRS to target CRS        
        /// </summary>
        /// <param name="sourceWKT"></param>
        /// <param name="targetWKT"></param>
        /// <returns></returns>
        public ICoordinateTransformation CreateCoordinateTrasformation(string sourceWKT, string targetWKT)
        {
            lock (_sync)
            {
                return CreateCoordinateTrasformation(Proj6.CRS.FromWKT(sourceWKT), Proj6.CRS.FromWKT(targetWKT));
            }
        }

        #endregion


        internal struct WKTString
        {
            /// <summary>Well-known ID</summary>
            public int WKID;
            /// <summary>Well-known Text</summary>
            public string WKT;
        }        

    }

    
}
