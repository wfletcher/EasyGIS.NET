
var url3 = 'http://localhost:64200/demos/TestVectorTileHandler.ashx?tx={x}&ty={y}&zoom={z}&mapid=none&version=6.91';



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

var randomColor = function(){
	var r = Math.floor(Math.random() * 200) + 20; 
	var g = Math.floor(Math.random() * 200) + 20; 
	var b = Math.floor(Math.random() * 256); 
	return '#' + fullColorHex(r,g,b);
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

var map = new ol.Map({
  target: 'map',
  layers: [
    
	new ol.layer.Tile({
            source: new ol.source.OSM(),
		    opacity: 1.0
            }),
    
	new ol.layer.VectorTile({
      declutter: true,
      source: new ol.source.VectorTile({
        
        format: new ol.format.MVT(),
        url: url3
      }),
	  style: function(feature, number) {
		  /*
		  var col = featureColors[''+feature.id_];
		  if(!col){
			  col = randomColor();
			  featureColors[''+feature.id_] = col;			  
		  }
		  style.getStroke().setColor(col);
		  */
		  
		  if(map.getView().getZoom() > 14)
		  {
			style.getText().setText(feature.get('NAME'));
		  }
		  else{
			  style.getText().setText('');
		  }
		  var w = 6/number;
		  if(w < 1) w = 1;
		  if(w > 20) w = 20;
		style.getStroke().setWidth(w);	
		style2.getStroke().setWidth(w+2);
		  return [style2,style];
    },
	  opacity:1.0
	  
	})
	
  ],
  view: new ol.View({
    //center: [0, 0],
	center: ol.proj.fromLonLat([-72, 44]),
    zoom: 8
  })
});

