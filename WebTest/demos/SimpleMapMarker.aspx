<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SimpleMapMarker.aspx.cs" Inherits="WebTest.demos.SimpleMapMarker" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">      

    <div style="height: 500px; width: 700px">
        <cc1:TiledSFMap ID="TiledSFMap1" runat="server" Height="450px" Width="650px"
          CacheOnServer="false" ProjectName="~/demos/world.egp" CacheOnClient="true" ZoomLevel = "5"
          style = "border:2px solid #dddddd" />          
    </div>
    
    <script type="text/javascript" language="javascript">

        //Add the map pins once the page has finshed loading to ensure that the map is ready
        egis.AddWindowLoadEventHandler(AddMarkersToMap);

        
        function GetEventTarget(evt) {
            var target;
            if (evt["srcElement"]) {
                target = evt["srcElement"];
            }
            else {
                target = evt["target"];
            }
            return target;
        };


        function MapClick(type, args, obj) {
            //Mapclicked event handler
            //type = "MapClicked"
            //args[0].lat = latitude
            //args[0].lon = "longitude"
            //args[0].px = mouse x position
            //args[0].py = mouse y position
            alert(args[0].lat + "," + args[0].lon);    
        };

        function SetupMapEventHandlers(map)
        {
            map.SetMapClickHandler(MapClick);
        };

        function MarkerClicked(evt) {
            var target = GetEventTarget(evt);
            alert("Marker " + target.id + " clicked");
        };

        //Add Markers to the map
        function AddMarkersToMap() {
            var map = egis.GetMap(0);

            if (map != null) {
                map.AddMarker({ markerId: 'India',
                    imgUrl: 'mappin.png',
                    imgWidth: 44,
                    imgHeight: 50,
                    lat: 20.0,
                    lon: 77.0,
                    clickHandler: MarkerClicked
                });

                map.AddMarker({ markerId: 'Australia',
                    imgUrl: 'mappin.png',
                    imgWidth: 44,
                    imgHeight: 50,
                    lat: -25,
                    lon: 132,
                    clickHandler: MarkerClicked
                });

                map.AddMarker({ markerId: 'China',
                    imgUrl: 'mappin.png',
                    imgWidth: 44,
                    imgHeight: 50,
                    lat: 34,
                    lon: 102,
                    clickHandler: MarkerClicked
                });

                map.SetMapCenterAndZoomLevel(90, -0, 2);

                SetupMapEventHandlers(map);
                             
            }

            else {
                alert("woops - map not found");
            }
        }


        
       </script>

       <script>
       function RemoveMarker(markerId) {
        var map = egis.GetMap(0);
        if (map != null) {
            for (var n = 0; n < map.Markers.length; n++) {
                if (map.Markers[n].id == markerId) {
                    //remove the mapPin
                    var marker = map.Markers[n];
                    marker.parentNode.removeChild(marker);
                    map.Markers.splice(n, 1);
                }
            }
        }               
       }
       </script>
    <button onclick="RemoveMarker('Australia');">Remove marker</button>
    </form>
</body>
</html>
