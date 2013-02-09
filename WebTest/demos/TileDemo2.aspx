<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TileDemo2.aspx.cs" Inherits="WebTest.demos.TileDemo2" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head id="Head1" runat="server">
    <title>TiledSFMap Demo 2</title>
    
    <style type="text/css">
        #Text1
        {
            width: 37px;
        }
    </style>
    

</head>
<body>
    <form id="form1" runat="server">
    <div style="width:800px; font-size:0.9em; margin:8px">
    <p>
        This Demo shows an example Easy GIS .NET project loaded and run in a web page.
        The project is the same as Tile Demo 1 but uses a generic Web Handler instead of the control's in-built
        handler.
    </p>
    </div>
    <div >
    
        <script type="text/javascript" language="javascript" >
            
        //centers the map at lat,lon 
        //zoom is unchanged

        function SetMapLocation()
        {
            var map = egis.GetMap(0);
            if(map != null)
            {
                //obtain you vehicle position and center the map on the vehicle
                //could use ajax for a nice update every 30 seconds
                //lon = 144.98, lat = -37.792               
                map.SetMapCenter(144.98, -37.792);                
            }
            else
            {
            alert("null map");
            }
            return false;
        }

        function SetZoomLevel() {
            var map = egis.GetMap(0);
            if (map != null) {
                var zoomLevel = document.getElementById("TextZoomLevel").value;
                map.SetZoomLevel(zoomLevel);            }
            else {
                alert("null map");
            }
            return false;
        }
        
        var pinsAdded = false;
        var initTimer = setInterval(function () { UpdateMapPins() }, 1000);
        

        function MarkerClicked(evt) {
        var target;
        if (evt["srcElement"]) {
            target = evt["srcElement"];
        }
        else {
            target = evt["target"];
        }
        alert("Marker " + target.id + " clicked");
    };
        //Add the map pins here
        function UpdateMapPins() {
            var map = egis.GetMap(0);

            if (map != null) {
                if (pinsAdded == false)
                {
                    map.AddMarker( { markerId: 'pin1',
                                     imgUrl: 'mappin.png', 
                                     imgWidth: 44,
                                     imgHeight: 50,
                                     lat: -37.7765,
                                     lon: 144.9609
                                 });

                    map.AddMarker({ markerId: 'pin2',
                                     imgUrl: 'mappin.png',
                                     imgWidth: 44,
                                     imgHeight: 50,
                                     lat: -37.7765,
                                     lon: 144.9220,
                                     clickHandler: MarkerClicked
                                 });

                    map.AddMarker({ markerId: 'pin3',
                                     imgUrl: 'mappin.png',
                                     imgWidth: 44,
                                     imgHeight: 50,
                                     lat: -37.7334,
                                     lon: 144.9220
                                 });
                
                    pinsAdded = true;
                }
                else {
                    //could obtain a live position from server using Ajax, but we wil just simulate by moving a random amount
                    map.Markers[0].lat += Math.random() * 0.001 - 0.0005;
                    map.Markers[0].lon += Math.random() * 0.001;
                    map.UpdateMarkers();                    
                }
                
            }
        }
        

        function MapZoomChanged(type, args, obj)
        {        
            var debugpanel = document.getElementById('debugpanel');
            debugpanel.innerHTML = '[' + obj.toString() + ',' + type + ']Current Zoom: ' + args[0] + '<br/>' + debugpanel.innerHTML;
            document.getElementById("TextZoomLevel").value = "" + egis.GetMap(0).GetZoomLevel();
            
        }
        
        function MapBoundsChanged(type, args, obj)
        {
            try
            {        
                var debugPanel = document.getElementById('debugpanel');
                debugPanel.innerHTML = '[' + obj.toString() + ',' + type + ']Current Bounds: ' + args[0] + ',' + args[1] + ',' + args[2] + ',' + args[3] + '<br/>' + debugPanel.innerHTML;
                var mapLoc = egis.GetMap(0).GetMapCenter();
                debugPanel.innerHTML = '[' + mapLoc[0] + ',' + mapLoc[1] + ']<br/>' + debugPanel.innerHTML;                                
                //obj.UpdateMapPins();
            }
            catch(ex)
            {
                window.status = ex.description;
            }
        }
        
        </script>
        <div>
         <cc1:TiledSFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="435px" Width="750px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px"
         ProjectName="~/demos/demo2.egp"  HttpHandlerName="EGPMapTileHandler.ashx" CacheOnServer="true" ServerCacheDirectoryUrl="cache" CacheOnClient="True"  MinZoomLevel="10" MaxZoomLevel="16" Style="margin:5px"
         OnClientBoundsChanged="MapBoundsChanged" OnClientZoomChanged="MapZoomChanged"/>
            &nbsp;
                
        <cc1:MapPanControl  CssClass="test" MapReferenceId="SFMap1" ID="MapPanControl1" 
                runat="server" 
                Style="z-index: 102; position:absolute; top: 75px; left:25px; text-align: center; padding: 2px; width: 69px;"  
                BorderColor="White" BorderWidth="2px" />
        <asp:Button ID="Button1" runat="server" Text="Set Map Location" OnClientClick="return SetMapLocation();"  />&nbsp;
        <asp:Button ID="Button2" runat="server" Text="Zoom Level:" OnClientClick="return SetZoomLevel();"/>
&nbsp;<input id="TextZoomLevel" type="text" value="1" /></div>                
    </div>
    <asp:TextBox ID="tb1" runat="server" Width="100px"></asp:TextBox>
    <div id = "debugpanel" style="width: 750px; height: 86px; border:solid 2px #404040;  overflow:auto; font-size:0.75em; margin:5px">
        </div>
        
    <p style="font-size:0.75em">
    * This Map incorporates Data which is Copyright Commonwealth of Australia 2005. The Data has been used with the 
      permission of the Commonwealth.  The Commonwealth has not evaluated the data as altered and incorporated within Easy GIS .NET,
      and therefore gives no warranty regarding its accuracy, completeness, currency or suitability for any particular purpose.<br />

    </p>
    </form>
</body>
</html>
