
var url3 = 'http://localhost:64200/demos/HiVectorTileHandler.ashx?tx={x}&ty={y}&zoom={z}&mapid=none&version=1.0';

var rgbToHex = function (rgb) { 
  var hex = Number(rgb).toString(16);
  if (hex.length < 2) {
       hex = "0" + hex;
  }
  return hex;
};

var fullColorHex = function(r,g,b) {   
  var red = rgbToHex(r);
  var green = rgbToHex(g);
  var blue = rgbToHex(b);
  return red+green+blue;
}; 

var colorLerp = function (a, b, l)
      {            
        var rr = Math.round(a.red + (b.red - a.red) * l);			
        var gg = Math.round(a.green + (b.green - a.green) * l);
        var bb = Math.round(a.blue + (b.blue - a.blue) * l);
			
        return {
	        red: rr,
	        green: gg,
	        blue: bb
        };
      }

var getRecordColor = function(attributeValue, color1, color2, color3){
	
	color1 = color1 || { red: 0, green: 192, blue: 0 };
    color2 = color2 || {red:255,green:255,blue:0};
    color3 = color3 || { red: 255, green: 0, blue: 0 };
		
	var mean = -11;
	var sd = 21;
	var minDataValue = mean - sd*2;
	var dataRange = 60;
	var intervalCount = 10;
	
	attributeValue = Number(attributeValue);
	var v = (attributeValue - minDataValue) / dataRange;
    if (v < 0) v = 0;
    if (v > 1) v = 1;
	
	var l = Math.round(v * intervalCount) / intervalCount;

    var rgb;
    if (l < 0.5)
    {
		
		rgb = colorLerp(color1, color2,l * 2);
    }
    else
    {
		rgb = colorLerp(color2, color3, (l - 0.5) * 2);
	}
             
	//console.log('v:', v);
	rgb.red = 220;rgb.green=20;rgb.blue=60;
	
	return '#' + fullColorHex(rgb.red,rgb.green, rgb.blue);
};

var featureColors = {};

var style = new ol.style.Style({
  text: new ol.style.Text({
    font: '12px "Open Sans", "Arial Unicode MS", "sans-serif"',
    placement: 'line',
    fill: new ol.style.Fill({
       color: '#606060'
     })
  }),
  fill : new ol.style.Fill( {
	  color: '#5060ab'
  }),
  stroke : new ol.style.Stroke( {
	  color: '#cdcddd',
	  width:1
  }),
  zIndex:2
});

var style2 = new ol.style.Style({  
  stroke : new ol.style.Stroke( {
	  color: '#506055',
	  width:3
  }),
  zIndex:1
});

var vectorLayer = new ol.layer.VectorTile({
      declutter: true,
      source: new ol.source.VectorTile({
        
        format: new ol.format.MVT(),
        url: url3
      }),
        style: function (feature, number) {
            
		  if(map.getView().getZoom() > 14)
		  {
			style.getText().setText(feature.get('ROAD_ID'));
		  }
		  else{
			  style.getText().setText('');
		  }

		  
		  var col = getRecordColor(feature.get('D0'));
		  if(col){					 
			style.getStroke().setColor(col);
		  }		  

		var w = 8/number;
          if (w < 1) w = 1;
          if (w > 15) w = 15;
		style.getStroke().setWidth(w);	
		style2.getStroke().setWidth(w+2);
		
		
		
		  return [style2,style];
    },
	  opacity:1.0
	  
	});
	
var map = new ol.Map({
  target: 'map',
  layers: [
    
	new ol.layer.Tile({
            source: new ol.source.OSM(),
		    opacity: 1.0
            }),
    
	vectorLayer
	
  ],
  view: new ol.View({
    //center: [0, 0],
	center: ol.proj.fromLonLat([-72, 44]),
    zoom: 8
  })
});

var highlightStyle = new ol.style.Style({
  stroke: new ol.style.Stroke({
    color: '#f00',
    width: 10
  }),
  fill: new ol.style.Fill({
    color: 'rgba(255,0,0,0.1)'
  }),
  text: new ol.style.Text({
    font: '12px Calibri,sans-serif',
    fill: new ol.style.Fill({
      color: '#000'
    }),
    stroke: new ol.style.Stroke({
      color: '#f00',
      width: 30
    })
  })
});

var featureOverlay = new ol.layer.Vector({
  source: new ol.source.Vector(),
  map: map,
  style: function(feature) {
    highlightStyle.getText().setText(feature.get('D0'));
    return highlightStyle;
  }
});

var highlight;
var displayFeatureInfo = function(pixel) {

  vectorLayer.getFeatures(pixel).then(function(features) {
    var feature = features.length ? features[0] : undefined;
    /*var info = document.getElementById('info');
    if (features.length) {
      info.innerHTML = feature.getId() + ': ' + feature.get('name');
    } else {
      info.innerHTML = '&nbsp;';
    }*/

    if (feature !== highlight) {
      if (highlight) {
        featureOverlay.getSource().removeFeature(highlight);
      }
      if (feature) {
        featureOverlay.getSource().addFeature(feature);
      }
      highlight = feature;
    }
  });

};
/*
map.on('pointermove', function(evt) {
  if (evt.dragging) {
    return;
  }
  var pixel = map.getEventPixel(evt.originalEvent);
  displayFeatureInfo(pixel);
});

map.on('click', function(evt) {
  displayFeatureInfo(evt.pixel);
});

*/

var selected = null;
//var status = document.getElementById('status');

/*
map.on('pointermove', function(e) {
  if (selected !== null) {
    selected.setStyle(undefined);
    selected = null;
  }

  map.forEachFeatureAtPixel(e.pixel, function(f) {
    selected = f;
    f.setStyle(highlightStyle);
    return true;
  });

  
});
*/



