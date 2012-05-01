<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TileDemo2.aspx.cs" Inherits="WebTest.demos.TileDemo2" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head id="Head1" runat="server">
    <title>TiledSFMap Demo 2</title>
    
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
        function MapZoomChanged(type, args, obj)
        {        
            var debugpanel = document.getElementById('debugpanel');
            debugpanel.innerHTML = '[' + obj.toString() + ',' + type + ']Current Zoom: ' + args[0] + '<br/>' + debugpanel.innerHTML;            
        }
        
        function MapBoundsChanged(type, args, obj)
        {
            try
            {        
                var debugPanel = document.getElementById('debugpanel');            
                debugPanel.innerHTML = '[' + obj.toString() + ',' + type + ']Current Bounds: ' + args[0] + ',' + args[1] +  ',' + args[2] + ',' + args[3] + '<br/>' + debugPanel.innerHTML ;
            }
            catch(ex)
            {
                window.status = ex.description;
            }
        }
        
        </script>
        <div>
         <cc1:TiledSFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="435px" Width="750px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px"
         ProjectName="~/demos/demo2.egp"  HttpHandlerName="EGPMapTileHandler.ashx" CacheOnServer="true" ServerCacheDirectoryUrl="cache" CacheOnClient="True"  MinZoomLevel="20" MaxZoomLevel="50000" Style="margin:5px"
         OnClientBoundsChanged="MapBoundsChanged" OnClientZoomChanged="MapZoomChanged"/>
                
        <cc1:MapPanControl  CssClass="test" MapReferenceId="SFMap1" ID="MapPanControl1" runat="server" Style="z-index: 102; position:absolute; top: 75px; left:25px; text-align: center; padding: 2px;"  BorderColor="White" BorderWidth="2px" />
         </div>
        
    </div>
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
