
var blur = document.getElementById('blur');
var radius = document.getElementById('radius');
var statusElement = document.getElementById('status');


var vectorSource = new ol.source.Vector({
   
  });

var vector = new ol.layer.Heatmap({
  source: vectorSource,
  blur: parseInt(blur.value, 10),
  radius: parseInt(radius.value, 10),
  weight: function(feature) {
    return 1.0;
  },
  //gradient:  ['#00d', '#0ff', '#0f0', '#ff0', '#ff001f'] //note #ff0 = #ffff00, #0f0 = #00ff00
  gradient:  ['#dd1010', '#ffefef']
});



var style = new ol.style.Style({
  fill: new ol.style.Fill({
    color: 'rgba(36, 13, 14, 0.8)'
	//240D0E
  }),
  stroke: new ol.style.Stroke({
    color: '#2F0A0A',
	//color: 'rgba(46, 23, 24, 1.0)',
	//color: 'rgba(46, 23, 24, 1.0)',
    width: 1
  }),
  text: new ol.style.Text({
    font: '12px Calibri,sans-serif',
    fill: new ol.style.Fill({
      color: '#000'
    }),
    stroke: new ol.style.Stroke({
      color: '#fff',
      width: 3
    })
  })
});

var worldLayer = new ol.layer.Vector({
  source: new ol.source.Vector({
    //url: 'data/geojson/countries.geojson',
	url: 'data/geojson/custom_hr.geojson',
	//url: 'data/geojson/countries-110m.geojson',
	
    format: new ol.format.GeoJSON()
  }),
  style: function(feature) {
    //style.getText().setText(feature.get('name'));
    return style;
  }
});


var map_center = ol.proj.transform([140, -35], 'EPSG:4326', 'EPSG:3857');

var map = new ol.Map({
  layers: [worldLayer, vector],//[raster, vector],
  target: 'map',
  view: new ol.View({
    center: map_center,
    zoom: 4
  })
});

/*
 generic AJAX function to load data from a server
*/
function loadData(url, callbackFunction) {
  var client;
  client = new XMLHttpRequest();
  
  client.onload = function(){
	  callbackFunction(this);
  }
  
  client.open("GET", url, true);
  client.send();
}


var vehicleMetadata = null;

function readVehicleMetadata(xhttpClient){

  vehicleMetadata = JSON.parse(xhttpClient.responseText);
  
  console.log('vehicleMetadata', vehicleMetadata);
  
};


var vehicleFeatures = [];
var dataLoaded = false;
var vehicleFeaturesIndex = -1;
var vehicleFeaturesIntervalSize = 2000;
var vehicleFeaturesIncrement = 96;

//records[0].LocationRecord.TimeStamp:20/01/2016 4:14:23 PM +00:00
var dateZero = new Date(2016,0,20,16,14,23);

function readVehicleData(xhttpClient){

  var csv = xhttpClient.responseText;
  
  var prevIndex = csv.indexOf('\n') + 1; // scan past the header line

  var curIndex;
  var timeZero = -1;
  while ((curIndex = csv.indexOf('\n', prevIndex)) != -1) {
    var line = csv.substr(prevIndex, curIndex - prevIndex).split(',');
    prevIndex = curIndex + 1;

    var coords = ol.proj.fromLonLat([parseFloat(line[2]), parseFloat(line[1])]);
    if (isNaN(coords[0]) || isNaN(coords[1])) {
      // guard against bad data
      continue;
    }
	
	if(timeZero < 0)
	{
		timeZero = Number(line[3]);
	}

	var days = Number(line[3]) - timeZero;
	days = days / (60*24); //time in days
    vehicleFeatures.push(new ol.Feature({
      veh: line[0], 
	  time: days,
      geometry: new ol.geom.Point(coords)
    }));
  }

  vehicleFeaturesIndex = 0;
  var indexEnd = Math.min(vehicleFeaturesIndex+vehicleFeaturesIntervalSize,vehicleFeatures.length-1); 
  vectorSource.addFeatures(vehicleFeatures.slice(0,indexEnd));
  dataLoaded = true;
	
};

/*
var client = new XMLHttpRequest();
client.open('GET', 'data/csv/vehicles.csv');
client.onload = function() {
  var csv = client.responseText;
  
  var prevIndex = csv.indexOf('\n') + 1; // scan past the header line

  var curIndex;
  var timeZero = -1;
  while ((curIndex = csv.indexOf('\n', prevIndex)) != -1) {
    var line = csv.substr(prevIndex, curIndex - prevIndex).split(',');
    prevIndex = curIndex + 1;

    var coords = ol.proj.fromLonLat([parseFloat(line[2]), parseFloat(line[1])]);
    if (isNaN(coords[0]) || isNaN(coords[1])) {
      // guard against bad data
      continue;
    }
	
	if(timeZero < 0)
	{
		timeZero = Number(line[3]);
	}

	var days = Number(line[3]) - timeZero;
	days = days / (60*24); //time in days
    vehicleFeatures.push(new ol.Feature({
      veh: line[0], 
	  time: days,
      geometry: new ol.geom.Point(coords)
    }));
  }

  vehicleFeaturesIndex = 0;
  var indexEnd = Math.min(vehicleFeaturesIndex+vehicleFeaturesIntervalSize,vehicleFeatures.length-1); 
  vectorSource.addFeatures(vehicleFeatures.slice(0,indexEnd));
  dataLoaded = true;
      
  
};
client.send();

*/


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
	
	if(cnt >= 20 && dataLoaded)
	{			
		++id;
		cnt = 0;
		
		vehicleFeaturesIndex += vehicleFeaturesIncrement;
		if(vehicleFeaturesIndex >= vehicleFeatures.length)
		{
			vehicleFeaturesIndex = 0;
		}
		
		var indexEnd = Math.min(vehicleFeaturesIndex+vehicleFeaturesIntervalSize,vehicleFeatures.length-1); 
		
		var dayFrom = vehicleFeatures[vehicleFeaturesIndex].get('time');
		var dayTo = vehicleFeatures[indexEnd].get('time');
		
		var dateFrom = new Date();
		dateFrom.setTime( Math.round(dateZero.getTime() + dayFrom * 24*60*60*1000));
		
		//convert day to actual date
		var dateTo = new Date();
		dateTo.setTime( Math.round(dateZero.getTime() + dayTo * 24*60*60*1000));
		
		//statusElement.innerHTML = 'Day: ' /*+ Math.round(dayFrom) + ' to '*/ + Math.round(dayTo) + ' ' + dateTo.toDateString() + '<br/>';
		statusElement.innerHTML = /*dateFrom.toDateString() + ' to ' + */ dateTo.toDateString() + '<br/>';
		
		vectorSource.clear(false);
		
		vectorSource.addFeatures(vehicleFeatures.slice(vehicleFeaturesIndex,indexEnd));
		
		vector.setSource(null);
		vector.setSource(vectorSource);
		vector.changed();
		
		map.render();	  	  	
	}
  
  window.requestAnimationFrame(animate);
}

loadData('data/csv/vehicles_metadata.json', readVehicleMetadata);
loadData('data/csv/vehicles.csv', readVehicleData);

animate();