using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Web.Script.Serialization;
using EGIS.ShapeFileLib;
using EGIS.Web.Controls;

namespace WebTest.demos
{
    /// <summary>
    /// Summary description for GeoJsonHandler
    /// </summary>
    public class GeoJsonHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(GetGeoJson3(context));


        }


        private string GetGeoJson()
        {
            string s =  @"{ 'type': 'FeatureCollection',
    'features': [
      
      { 'type': 'Feature',
        'properties': {
        'color': 'blue'       
        },
        'geometry': {
          'type': 'LineString',
          'coordinates': [
            [102.0, 0.0], [103.0, 1.0], [104.0, 0.0], [105.0, 1.0]
            ]
          },
        'properties': {
          'prop0': 'value0',
          'prop1': 0.0
          }
        },
      { 'type': 'Feature',
         'geometry': {
           'type': 'Polygon',
           'coordinates': [
             [ [100.0, 0.0], [101.0, 0.0], [101.0, 1.0],
               [100.0, 1.0], [100.0, 0.0] ]
             ]
         },
         'properties': {
           'prop0': 'value0',
           'prop1': {'this': 'that'}
           }
         }
       ]
     }";
            return s.Replace('\'', '"');
        }


        private string GetGeoJson2()
        {
            FeatureCollection featureCollection = new FeatureCollection();
            Feature feature = new Feature();
            feature.geometry = new LineString(new PointD[] { new PointD(145,-37), new PointD(145.2,-37.2), new PointD(145.27,-37.202), new PointD(146,-35), new PointD(144,-34), new PointD(145,-33)});
            StyleOptions featureStyleOptions = new StyleOptions();
            featureStyleOptions.strokeWeight = 3;
            featureStyleOptions.strokeColor = "red";
            featureStyleOptions.strokeOpacity = 0.5f;
            feature.properties = new {id = "0", styleOptions  = featureStyleOptions};
            featureCollection.features.Add(feature);

            
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            return javaScriptSerializer.Serialize(featureCollection);
        
        }

        Random rnd = new Random();

        private string GetGeoJson3(HttpContext context)
        {
            string shapeFilePath = context.Server.MapPath("/demos/demo2_files/j5505_roads.shp");
            ShapeFile sf = new ShapeFile(shapeFilePath);
            int recordIndex = rnd.Next(sf.RecordCount);

            PointD[] pts = sf.GetShapeDataD(recordIndex)[0];

            FeatureCollection featureCollection = new FeatureCollection();
            Feature feature = new Feature();
            feature.geometry = new LineString(pts);
            StyleOptions featureStyleOptions = new StyleOptions();
            featureStyleOptions.strokeWeight = 3;
            featureStyleOptions.strokeColor = "red";
            featureStyleOptions.strokeOpacity = 0.5f;
            feature.properties = new { id = "0", styleOptions = featureStyleOptions };
            featureCollection.features.Add(feature);

            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            return javaScriptSerializer.Serialize(featureCollection);                    
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }

   
}