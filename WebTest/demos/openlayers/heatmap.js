/*
import 'ol/ol.css';
import Map from 'ol/Map';
import View from 'ol/View';
import KML from 'ol/format/KML';
import {Heatmap as HeatmapLayer, Tile as TileLayer} from 'ol/layer';
import Stamen from 'ol/source/Stamen';
import VectorSource from 'ol/source/Vector';
*/

var blur = document.getElementById('blur');
var radius = document.getElementById('radius');

var vector = new ol.layer.Heatmap({
  source: new ol.source.Vector({
    url: 'data/kml/2012_Earthquakes_Mag5.kml',
    format: new ol.format.KML({
      extractStyles: false
    })
  }),
  blur: parseInt(blur.value, 10),
  radius: parseInt(radius.value, 10),
  weight: function(feature) {
    // 2012_Earthquakes_Mag5.kml stores the magnitude of each earthquake in a
    // standards-violating <magnitude> tag in each Placemark.  We extract it from
    // the Placemark's name instead.
    var name = feature.get('name');
    var magnitude = parseFloat(name.substr(2));
    return 0.98;//0.5*(magnitude - 5);
  },
  gradient:  ['#00d', '#0ff', '#0f0', '#ff0', '#0000df']
});

var raster = new ol.layer.Tile({
  source: new ol.source.Stamen({
    layer: 'toner'
  })
});

var map = new ol.Map({
  layers: [raster, vector],
  target: 'map',
  view: new ol.View({
    center: [0, 0],
    zoom: 2
  })
});

var blurHandler = function() {
  vector.setBlur(parseInt(blur.value, 10));
};
blur.addEventListener('input', blurHandler);
blur.addEventListener('change', blurHandler);

var radiusHandler = function() {
  vector.setRadius(parseInt(radius.value, 10));
};
radius.addEventListener('input', radiusHandler);
radius.addEventListener('change', radiusHandler);

// animate the map
var cnt = 0;
var id=0;
function animate() {
	
	++cnt;

	if(cnt == 20)
	{
		++id;
		cnt = 0;
	  map.render();
	  
	  
	  var source = vector.getSource();
	  if(source !== undefined){
		  var features = [];
		  var i =0;		  
		  for(i=0;i<200;++i)
		  {
			  var coords = ol.proj.fromLonLat([-180 + 360*Math.random(),-80 + 160*Math.random()]);
	  
		  var feature = new ol.Feature({
			geometry: new ol.geom.Point(coords),			
			name: 'M ' + Math.random()*2
			});
		features.push(feature);
		  }
		source.addFeatures(features);
		console.log('features.length:', source.getFeatures().length);
		}
	}
  
  window.requestAnimationFrame(animate);
}
animate();