/*
import 'ol/ol.css';
import Map from 'ol/Map';
import View from 'ol/View';
import TileLayer from 'ol/layer/Tile';
import WebGLPointsLayer from 'ol/layer/WebGLPoints';
import GeoJSON from 'ol/format/GeoJSON';
import Vector from 'ol/source/Vector';
import OSM from 'ol/source/OSM';

*/


var vectorSource = new ol.source.Vector({
  url: 'data/geojson/world-cities.geojson',
  format: new ol.format.GeoJSON()
});

var predefinedStyles = {
  'icons': {
    symbol: {
      symbolType: 'image',
      src: 'data/icon.png',
      size: [18, 28],
      color: 'lightyellow',
      rotateWithView: false,
      offset: [0, 9]
    }
  },
  'triangles': {
    symbol: {
      symbolType: 'triangle',
      size: 18,
      color: [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        20000, '#5aca5b',
        300000, '#ff6a19'
      ],
      rotateWithView: true
    }
  },
  'triangles-latitude': {
    symbol: {
      symbolType: 'triangle',
      size: [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        40000, 12,
        2000000, 24
      ],
      color: [
        'interpolate',
        ['linear'],
        ['get', 'latitude'],
        -60, '#ff14c3',
        -20, '#ff621d',
        20, '#ffed02',
        60, '#00ff67'
      ],
      offset: [0, 0],
      opacity: 0.95
    }
  },
  'circles': {
    symbol: {
      symbolType: 'circle',
      size: [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        40000, 8,
        2000000, 28
      ],
      color: '#006688',
      rotateWithView: false,
      offset: [0, 0],
      opacity: [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        40000, 0.6,
        2000000, 0.92
      ]
    }
  },
  'circles-zoom': {
    symbol: {
      symbolType: 'circle',
      size: [
        'interpolate',
        ['exponential', 2.5],
        ['zoom'],
        2, 1,
        14, 32
      ],
      color: '#240572',
      offset: [0, 0],
      opacity: 0.95
    }
  },
  'rotating-bars': {
    symbol: {
      symbolType: 'square',
      rotation: ['*', [
        'time'
      ], 0.1],
      size: ['array', 4, [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        20000, 4,
        300000, 28]
      ],
      color: [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        20000, '#ffdc00',
        300000, '#ff5b19'
      ],
      offset: ['array', 0, [
        'interpolate',
        ['linear'],
        ['get', 'population'],
        20000, 2,
        300000, 14]
      ]
    }
  }
};

var map = new ol.Map({
  layers: [
    new ol.layer.Tile({
      source: new ol.source.OSM()
    })
  ],
  target: document.getElementById('map'),
  view: new ol.View({
    center: [0, 0],
    zoom: 2
  })
});

var literalStyle;
var pointsLayer;
function refreshLayer(newStyle) {
  var previousLayer = pointsLayer;
  
  //ol/renderer/webgl/PointsLayer 
  //pointsLayer = new ol.layer.WebGLPoints({
	pointsLayer = new ol.renderer.webgl.PointsLayer({
    source: vectorSource,
    style: newStyle,
    disableHitDetection: true
  });
  map.addLayer(pointsLayer);

  if (previousLayer) {
    map.removeLayer(previousLayer);
    previousLayer.dispose();
  }
  literalStyle = newStyle;
}

var spanValid = document.getElementById('style-valid');
var spanInvalid = document.getElementById('style-invalid');
function setStyleStatus(errorMsg) {
  var isError = typeof errorMsg === 'string';
  spanValid.style.display = errorMsg === null ? 'initial' : 'none';
  spanInvalid.firstElementChild.innerText = isError ? errorMsg : '';
  spanInvalid.style.display = isError ? 'initial' : 'none';
}

var editor = document.getElementById('style-editor');
editor.addEventListener('input', function() {
  var textStyle = editor.value;
  try {
    var newLiteralStyle = JSON.parse(textStyle);
    if (JSON.stringify(newLiteralStyle) !== JSON.stringify(literalStyle)) {
      refreshLayer(newLiteralStyle);
    }
    setStyleStatus(null);
  } catch (e) {
    setStyleStatus(e.message);
  }
});

var select = document.getElementById('style-select');
select.value = 'circles';
function onSelectChange() {
  var style = select.value;
  var newLiteralStyle = predefinedStyles[style];
  editor.value = JSON.stringify(newLiteralStyle, null, 2);
  try {
    refreshLayer(newLiteralStyle);
    setStyleStatus();
  } catch (e) {
	  console.log("error", e);
    setStyleStatus(e.message);
  }
}
onSelectChange();
select.addEventListener('change', onSelectChange);

// animate the map
function animate() {
  map.render();
  window.requestAnimationFrame(animate);
}
animate();
