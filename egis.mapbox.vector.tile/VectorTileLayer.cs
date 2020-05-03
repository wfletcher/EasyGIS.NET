using System.Collections.Generic;

namespace EGIS.Mapbox.Vector.Tile
{
    /// <summary>
    /// Class representing a Mapbox Vector Tile Layer. A Vector tile should contain at least one VectorTileLayer
    /// </summary>
	public class VectorTileLayer
	{
        public VectorTileLayer()
        {
            VectorTileFeatures = new List<VectorTileFeature>();
            Version = 2;
        }

        /// <summary>
        /// List of VectorTileFeature. A VectorTileLayer should contain at least one feature
        /// </summary>
		public List<VectorTileFeature> VectorTileFeatures { get;set; }

        /// <summary>
        /// get/set the name of the Layer. A Layer MUST contain a name.
        /// </summary>
        /// <remarks>
        /// A Vector Tile MUST NOT contain multiple VectorTileLayers with the same Name
        /// </remarks>
		public string Name { get; set; }

        /// <summary>
        /// get/set the version of the VectorTileLayer. Default is 2
        /// </summary>
		public uint Version { get; set; }

        /// <summary>
        /// get/set the size of the Layer. Typical values are 512 or 1024
        /// </summary>
		public uint Extent { get; set; }
	}
}

