
var blur = document.getElementById('blur');
var radius = document.getElementById('radius');
var statusElement = document.getElementById('status');

var vehicleFeatures = [];
var dataLoaded = false;
var vehicleFeaturesIndex = -1;
var vehicleFeaturesIntervalSize = 2000;
var vehicleFeaturesIncrement = 96;


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

    var coords = [parseFloat(line[1]), parseFloat(line[2])];
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
	
	var feature = 
	{
		location: new google.maps.LatLng({lat:coords[0], lng:coords[1]}),
		veh :  line[0],
		time : days,
		weight: 2
	}
	
    vehicleFeatures.push(feature);
  }

  vehicleFeaturesIndex = 0;
  //var indexEnd = Math.min(vehicleFeaturesIndex+vehicleFeaturesIntervalSize,vehicleFeatures.length-1); 
 // vectorSource.addFeatures(vehicleFeatures.slice(0,indexEnd));
  dataLoaded = true;
      
  
};
//client.send();


var blurHandler = function() {
  //vector.setBlur(parseInt(blur.value, 10));
};
blur.addEventListener('input', blurHandler);
blur.addEventListener('change', blurHandler);

var radiusHandler = function() {
  //vector.setRadius(parseInt(radius.value, 10));
};
radius.addEventListener('input', radiusHandler);
radius.addEventListener('change', radiusHandler);

// animate the map
var cnt = 0;
var id=0;
function animate() {
	
	
	++cnt;
	
	if(cnt >= 10 && dataLoaded)
	{			
		++id;
		cnt = 0;
		
		vehicleFeaturesIndex += vehicleFeaturesIncrement;
		if(vehicleFeaturesIndex >= vehicleFeatures.length)
		{
			vehicleFeaturesIndex = 0;
		}
		
		var indexEnd = Math.min(vehicleFeaturesIndex+vehicleFeaturesIntervalSize,vehicleFeatures.length-1); 
		
		var dayFrom = vehicleFeatures[vehicleFeaturesIndex].time;
		var dayTo = vehicleFeatures[indexEnd].time;
		
		statusElement.innerHTML = 'Day :' /*+ Math.round(dayFrom) + ' to '*/ + Math.round(dayTo) + '<br/>';
		
		//vectorSource.clear(false);
		
		//vectorSource.addFeatures(vehicleFeatures.slice(vehicleFeaturesIndex,indexEnd));
		
		//vector.setSource(null);
		//vector.setSource(vectorSource);
		//vector.changed();
		
		//map.render();	  	  	
		
		heatmap.setData(vehicleFeatures.slice(vehicleFeaturesIndex,indexEnd));
	}
  
  window.requestAnimationFrame(animate);
}

var map, heatmap;

var mapStyle = {
                id: 'mapStyleTheme_dark',
                name: 'Dark Theme',
                style: [{ "featureType": "all", "elementType": "labels.text.fill", "stylers": [{ "saturation": 36 }, { "color": "#000000" }, { "lightness": 40 }] }, { "featureType": "all", "elementType": "labels.text.stroke", "stylers": [{ "visibility": "on" }, { "color": "#000000" }, { "lightness": 16 }] }, { "featureType": "all", "elementType": "labels.icon", "stylers": [{ "visibility": "off" }] }, { "featureType": "administrative", "elementType": "geometry.fill", "stylers": [{ "color": "#000000" }, { "lightness": 20 }] }, { "featureType": "administrative", "elementType": "geometry.stroke", "stylers": [{ "color": "#000000" }, { "lightness": 17 }, { "weight": 1.2 }] }, { "featureType": "administrative", "elementType": "labels", "stylers": [{ "visibility": "off" }] }, { "featureType": "administrative.country", "elementType": "all", "stylers": [{ "visibility": "simplified" }] }, { "featureType": "administrative.country", "elementType": "geometry", "stylers": [{ "visibility": "simplified" }] }, { "featureType": "administrative.country", "elementType": "labels.text", "stylers": [{ "visibility": "simplified" }] }, { "featureType": "administrative.province", "elementType": "all", "stylers": [{ "visibility": "off" }] }, { "featureType": "administrative.locality", "elementType": "all", "stylers": [{ "visibility": "simplified" }, { "saturation": "-100" }, { "lightness": "30" }] }, { "featureType": "administrative.neighborhood", "elementType": "all", "stylers": [{ "visibility": "off" }] }, { "featureType": "administrative.land_parcel", "elementType": "all", "stylers": [{ "visibility": "on" }] }, { "featureType": "landscape", "elementType": "all", "stylers": [{ "visibility": "simplified" }, { "gamma": "0.00" }, { "lightness": "74" }] }, { "featureType": "landscape", "elementType": "geometry", "stylers": [{ "color": "#000000" }, { "lightness": "20" }] }, { "featureType": "landscape.man_made", "elementType": "all", "stylers": [{ "lightness": "3" }] }, { "featureType": "poi", "elementType": "all", "stylers": [{ "visibility": "off" }] }, { "featureType": "poi", "elementType": "geometry", "stylers": [{ "color": "#000000" }, { "lightness": 21 }] }, { "featureType": "road", "elementType": "geometry", "stylers": [{ "visibility": "simplified" }] }, { "featureType": "road.highway", "elementType": "geometry.fill", "stylers": [{ "lightness": "-81" }, { "hue": "#ff7b00" }, { "saturation": "-100" }] }, { "featureType": "road.highway", "elementType": "geometry.stroke", "stylers": [{ "lightness": "-84" }, { "hue": "#ff4500" }, { "saturation": "-100" }, { "weight": "0.20" }] }, { "featureType": "road.highway.controlled_access", "elementType": "labels.icon", "stylers": [{ "visibility": "simplified" }, { "lightness": "-20" }] }, { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "lightness": "-83" }, { "hue": "#ff7b00" }, { "saturation": "-100" }] }, { "featureType": "road.local", "elementType": "geometry", "stylers": [{ "lightness": "-84" }, { "hue": "#ff7300" }, { "saturation": "-100" }] }, { "featureType": "transit", "elementType": "geometry", "stylers": [{ "color": "#000000" }, { "lightness": 19 }] }, { "featureType": "water", "elementType": "geometry", "stylers": [{ "color": "#050607" }, { "lightness": "12" }] }],
                enabled: true,
                editable: function () { return false; },
                visible: function () { return true; }
            };
			

function initMap() {
        map = new google.maps.Map(document.getElementById('map'), {
          zoom: 3,
          center: {lat: -35, lng: 140}
          
        });

        heatmap = new google.maps.visualization.HeatmapLayer({
          data: [],
          map: map,		  
		  radius:4,
		  opacity:0.9,
		  dissipating: true
        });
		
		var gradient = [
          'rgba(0, 255, 255, 0)',
          'rgba(0, 255, 255, 1)',
          'rgba(0, 191, 255, 1)',
          'rgba(0, 127, 255, 1)',
          'rgba(0, 63, 255, 1)',
          'rgba(0, 0, 255, 1)',
          'rgba(0, 0, 223, 1)',
          'rgba(0, 0, 191, 1)',
          'rgba(0, 0, 159, 1)',
          'rgba(0, 0, 127, 1)',
          'rgba(63, 0, 91, 1)',
          'rgba(127, 0, 63, 1)',
          'rgba(191, 0, 31, 1)',
          'rgba(255, 0, 0, 1)'
        ];
		
		//gradient = ['rgba(0, 255, 255, 0)',  'rgba(255, 240, 240, 1)', 'rgba(225, 10, 10, 1)'];//'#dd1010', '#ffefef'];
		
		heatmap.set('gradient', gradient);//['rgba(225, 10, 10, 1)', 'rgba(255, 240, 240, 1)']);
		
		map.setOptions({ styles: mapStyle.style });
		
		map.setStyle
		client.send();
		animate();
      }