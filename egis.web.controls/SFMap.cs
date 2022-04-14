#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.Web.controls class library of Easy GIS .NET.
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


using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using EGIS.ShapeFileLib;

namespace EGIS.Web.Controls
{

    /// <summary>
    /// The SFMap control is no longer supported. This class now only contains some statuc utility methods to load egp xml project files
    /// </summary>
    public class SFMap
    {

       
      
        #region methods to create the map and load project

        

        /// <summary>
        /// static method to read a .egp project file
        /// </summary>
        /// <param name="absPath">Absolute path of the .egp file</param>
        /// <returns>A MapProject object containing the project layers and map background color</returns>
        /// <remarks>This method provides an easy means of creating a List of ShapeFiles to be used
        /// in a MapHandler</remarks>
        public static MapProject ReadEGPProject(string absPath)
        {
            string basePath = absPath.Substring(0, absPath.LastIndexOf('\\') + 1);
            XmlDocument doc = new XmlDocument();
            doc.Load(absPath);
            XmlElement prjElement = (XmlElement)doc.GetElementsByTagName("sfproject").Item(0);
            return ReadXml(prjElement, basePath, null);
        }

       
        internal static MapProject ReadXml(XmlElement projectElement, string rootPath, SFMap mapRef)
        {
            MapProject mapProject = new MapProject();

            XmlNodeList colorList = projectElement.GetElementsByTagName("MapBackColor");
            if (colorList != null && colorList.Count > 0)
            {
                mapProject.BackgroundColor = ColorTranslator.FromHtml(colorList[0].InnerText);
                
            }
            //else if (mapRef != null)
            //{
            //    mapProject.BackgroundColor = mapRef.BackColor;
            //}

            XmlNodeList crsList = projectElement.GetElementsByTagName("MapCoordinateReferenceSystem");
            bool crsSet = false;
            if (crsList != null && crsList.Count > 0)
            {
                try
                {
                    string wkt = crsList[0].InnerText;
                    if (!string.IsNullOrEmpty(wkt))
                    {
                        mapProject.MapCoordinateReferenceSystem = EGIS.Projections.CoordinateReferenceSystemFactory.Default.CreateCRSFromWKT(wkt);
                        crsSet = true;
                    }
                }
                catch
                {
                }
            }
            if (!crsSet)
            {
                //assume old project using wgs84
                mapProject.MapCoordinateReferenceSystem = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode);                
            }

            //clear layers
            List<EGIS.ShapeFileLib.ShapeFile> myShapefiles = new List<EGIS.ShapeFileLib.ShapeFile>();

            XmlNodeList layerNodeList = projectElement.GetElementsByTagName("layers");
            XmlNodeList sfList = ((XmlElement)layerNodeList[0]).GetElementsByTagName("shapefile");

            if (sfList != null && sfList.Count > 0)
            {

                for (int n = 0; n < sfList.Count; n++)
                {
                    EGIS.ShapeFileLib.ShapeFile sf = new EGIS.ShapeFileLib.ShapeFile();
                    XmlElement elem = sfList[n] as XmlElement;
                    elem.GetElementsByTagName("path")[0].InnerText = rootPath + elem.GetElementsByTagName("path")[0].InnerText;
                    sf.ReadXml(elem, rootPath);
                    myShapefiles.Add(sf);
                }                
            }

            //EGIS.ShapeFileLib.ShapeFile.UseMercatorProjection = false;
            mapProject.Layers = myShapefiles;
            return mapProject;
        }

        /// <summary>
        /// updates RenderSettings defined in sourceCrs to their equivalent in destinationCrs
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="sourceCrs"></param>
        /// <param name="destinationCrs"></param>
        /// <remarks>Adjusts min/max zoom levels. This method should be used to update layer's rendersettings that were created using wgs84 
        /// before rendering th elayer in Web Mercator</remarks>
        public static void UpdateRenderSettings(List<ShapeFile> layers, EGIS.Projections.ICRS sourceCrs, EGIS.Projections.ICRS destinationCrs)
        {
            //first check that the shapefiles crs has been set
            foreach (ShapeFile layer in layers)
            {
                if (layer.CoordinateReferenceSystem == null)
                {
                    //assume old shapefile missing prj file
                    layer.SetCoordinateReferenceSystem(EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode));
                }
            }

            //now update the rendersettings from the sourceCrs to the destinationCrs
            RectangleD sourceExtent = GetExtent(layers, sourceCrs);
            PointD sourceCenterPt = new PointD((sourceExtent.Left + sourceExtent.Right) * 0.5, (sourceExtent.Top + sourceExtent.Bottom) * 0.5);
            foreach (ShapeFile layer in layers)
            {                
                RenderSettings.UpdateRenderSettings(layer.RenderSettings, sourceCenterPt, sourceCrs, destinationCrs);
            }

        }


        /// <summary>
        /// updates RenderSettings defined in sourceCrs to their equivalent in Web Mercator EPSG 3857
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="sourceCrs"></param>
        public static void UpdateRenderSettingsForWebMercator(List<ShapeFile> layers, EGIS.Projections.ICRS sourceCrs)
        {
            UpdateRenderSettings(layers, sourceCrs, EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode));            
        }

        /// <summary>
        /// gets the Extent of a given list of shapefiles in coordinates defined by crs
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="crs"></param>
        /// <returns></returns>
        public static RectangleD GetExtent(List<ShapeFile> layers, EGIS.Projections.ICRS crs )
        {                        
            RectangleD r = Rectangle.Empty;                                 
            for(int n=0; n< layers.Count;++n)
            {
                ShapeFile sf = layers[n];
                var extent = sf.Extent.Transform(sf.CoordinateReferenceSystem, crs);
                if (double.IsInfinity(extent.Width) || double.IsInfinity(extent.Height))
                {
                    extent = RestrictExtentToCRS(extent, crs);
                }

                r = n == 0 ? r : RectangleD.Union(r, extent);
            }
            return r;
                       
        }

        private static RectangleD RestrictExtentToCRS(RectangleD extent, EGIS.Projections.ICRS crs)
        {
            if (crs != null && crs.AreaOfUse.IsDefined)
            {
                if (double.IsInfinity(extent.Width) || double.IsInfinity(extent.Height))
                {
                   //convert the area of use from lat/lon degrees to the shapefile CRS
                   EGIS.Projections.ICRS wgs84 = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GetCRSById(EGIS.Projections.CoordinateReferenceSystemFactory.Wgs84EpsgCode);
                    RectangleD areaOfUse = RectangleD.FromLTRB(crs.AreaOfUse.WestLongitudeDegrees,
                        crs.AreaOfUse.SouthLatitudeDegrees,
                        crs.AreaOfUse.EastLongitudeDegrees,
                        crs.AreaOfUse.NorthLatitudeDegrees);
                    areaOfUse = areaOfUse.Transform(wgs84, crs);

                    return areaOfUse;
                }
            }
            return extent;
        }

        #endregion


     

        internal static RectangleF LayerExtent(List<EGIS.ShapeFileLib.ShapeFile> layers)
        {
            if (layers == null || layers.Count == 0)
            {
                return RectangleF.Empty;
            }
            else
            {
                RectangleF r = layers[0].Extent;
                foreach (EGIS.ShapeFileLib.ShapeFile sf in layers)
                {
                    r = RectangleF.Union(r, sf.Extent);
                }
                return r;
            }
        }
     
    }

  

    public class MapProject
    {
        public List<EGIS.ShapeFileLib.ShapeFile> Layers;
        public Color BackgroundColor;
        public EGIS.Projections.ICRS MapCoordinateReferenceSystem;
    }

   
    
}






