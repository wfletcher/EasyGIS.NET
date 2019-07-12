using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EGIS.Projections
{
    public class CoordinateReferenceSystemFactory : ICRSFactory
    {
        /// <summary>
        /// key = srid, value = wkt
        /// </summary>
        private Dictionary<int, string> coordinateSystems = new Dictionary<int, string>();
        
        private List<IProjectedCRS> projectedCoordinateSystems = new List<IProjectedCRS>();
        private List<IGeographicCRS> geographicCoordinateSystems = new List<IGeographicCRS>();


        protected CoordinateReferenceSystemFactory(string sridFilename)
        {
        }

        protected CoordinateReferenceSystemFactory()
        {
            LoadData();
        }

        private static CoordinateReferenceSystemFactory _instance;

        public static ICRSFactory Default
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CoordinateReferenceSystemFactory();
                }
                return _instance;
            }
        }

        public const int Wgs84EpsgCode = 4326;


        private void LoadData()
        {
            var resourceName = "EGIS.Projections.SRID.csv.gz";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(decompressionStream))
            {
                LoadData(reader);
            }
            
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

        public List<IGeographicCRS> GeographicCoordinateSystems
        {
            get { return this.geographicCoordinateSystems; }
        }

        public List<IProjectedCRS> ProjectedCoordinateSystems
        {
            get { return this.projectedCoordinateSystems; }
        }

        public ICRS GetCRSById(int id)
        {
            string wkt;
            if (this.coordinateSystems.TryGetValue(id, out wkt))
            {
                ICRS crs =  CreateCRSFromWKT(wkt);
                if (string.IsNullOrEmpty(crs.Id))
                {
                    ((Proj6.CRS)crs).Id = id.ToString();
                }
                return crs;
            }
            return null;
        }

        public ICRS CreateCRSFromWKT(string wkt)
        {
            return Proj6.CRS.FromWKT(wkt);
        }

        public ICoordinateTransformation CreateCoordinateTrasformation(ICRS source, ICRS target)
        {
            if (source == null || target == null) throw new Exception("source and target ICRS cannot be null");

            return new Proj6.CoordinateTransformation(source, target);
        }

        public ICoordinateTransformation CreateCoordinateTrasformation(string sourceWKT, string targetWKT)
        {
            return CreateCoordinateTrasformation(Proj6.CRS.FromWKT(sourceWKT), Proj6.CRS.FromWKT(targetWKT));
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
