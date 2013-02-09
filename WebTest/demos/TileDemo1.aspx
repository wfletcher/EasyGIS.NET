<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TileDemo1.aspx.cs" Inherits="WebTest.demos.TileDemo1" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head id="Head1" runat="server">
    <title>TiledSFMap Control Demo</title>
    
</head>
<body>
    <form id="form1" runat="server">
    <div style="width:800px; font-size:0.9em; margin:8px">
    <p>
        This Demo shows an example Easy GIS .NET project loaded and run in a web page.
        The project was created in the desktop version of Easy GIS .NET and then loaded
        in an EGIS.Web.Controls TiledSFMap web control.
    </p>
    <p>
        The displayed map is a map of Melbourne, Australia.
        The map data was sourced from the Australian Government GeoScience Australia Website*,
         and consists of 17 layers, including local roads, railways, reserves, lakes and rivers.                        
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
        
         <cc1:TiledSFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="435px" Width="750px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px"
         ProjectName="~/demos/demo2.egp"  CacheOnServer="true" ServerCacheDirectoryUrl="cache" CacheOnClient="True"  MinZoomLevel="10" MaxZoomLevel="16" Style="margin:5px"
         OnClientBoundsChanged="MapBoundsChanged" OnClientZoomChanged="MapZoomChanged"/>
        
        <%--ZoomInImageUrl property allows changing the controls button images--%>
        <cc1:MapPanControl  MapReferenceId="SFMap1" CssClass="test" ID="MapPanControl1" runat="server" Style="z-index: 102; position:absolute; top: 125px; left:25px; width:75px;text-align: center; padding-right: 2px; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" ZoomInImageUrl="zoomin.png"  BorderColor="White" BorderWidth="2px" />
        
        
    </div>
    <div id = "debugpanel" style="width: 750px; height: 86px; border:solid 2px #404040;  overflow:auto; font-size:0.75em; margin:5px">
        </div>
        
    <p style="font-size:0.75em">
    * This Map incorporates Data which is Copyright Commonwealth of Australia 2005. The Data has been used with the 
      permission of the Commonwealth.  The Commonwealth has not evaluated the data as altered and incorporated within Easy GIS .NET,
      and therefore gives no warranty regarding its accuracy, completeness, currency or suitability for any particular purpose.<br />

    </p>
    <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
    </form>
</body>
</html>
