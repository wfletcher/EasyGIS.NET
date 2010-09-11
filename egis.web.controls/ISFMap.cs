using System;
using System.Collections.Generic;
using System.Text;

namespace EGIS.Web.Controls
{
    /// <summary>
    /// Interface defining methods needed by MapPanControl to interact with SFMap and TiledSFMap classes.
    /// </summary>
    public interface ISFMap
    {
        /// <summary>
        /// the Id of the MapControl
        /// </summary>
        string ControlId
        {
            get;
        }

        /// <summary>
        /// The client Id of the Map's GIS Image
        /// </summary>
        string GisImageClientId
        {
            get;
        }

        /// <summary>
        /// the name of the Javascript resource used by the ISFMap, which is different depending on
        /// whether a TiledSfMap or simple SFMap is used
        /// </summary>
        string ClientJSResouceName
        {
            get;
        }
    }
}
