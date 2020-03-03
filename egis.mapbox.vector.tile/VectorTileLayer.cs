using System.Collections.Generic;

namespace EGIS.Mapbox.Vector.Tile
{
	public class VectorTileLayer
	{
        public VectorTileLayer()
        {
            VectorTileFeatures = new List<VectorTileFeature>();
            Version = 2;
        }

		public List<VectorTileFeature> VectorTileFeatures { get;set; }
		public string Name { get; set; }
		public uint Version { get; set; }
		public uint Extent { get; set; }
	}
}

