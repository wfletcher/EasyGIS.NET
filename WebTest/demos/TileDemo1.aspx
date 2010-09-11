<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TileDemo1.aspx.cs" Inherits="WebTest.demos.TileDemo1" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head id="Head1" runat="server">
    <title>Untitled Page</title>
    
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <p style="font-size:0.75em">
        This Demo shows an example Easy GIS .NET project loaded and run in a web page.
        The project was created in the desktop version of Easy GIS .NET and then loaded
        in an EGIS.Web.Controls TiledSFMap web control.
        <br />
        Data sourced from the
        <a target ="_blank" href="http://www.vicroads.vic.gov.au">VicRoads</a> CrashStats web site.
        <br />
        </p>
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
        
        
        <%--<cc1:TiledSFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="435px" Width="650px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px"
         ProjectName="~/demos/victorian_accidents.egp"  CacheOnServer="true" ServerCacheDirectoryUrl="cache" CacheOnClient="False"  MinZoomLevel="20" MaxZoomLevel="50000" OnClientBoundsChanged="MapBoundsChanged" OnClientZoomChanged="MapZoomChanged"> />--%>
         <cc1:TiledSFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="435px" Width="650px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px"
         ProjectName="~/demos/victorian_accidents.egp"  CacheOnServer="true" ServerCacheDirectoryUrl="cache" CacheOnClient="False"  MinZoomLevel="20" MaxZoomLevel="50000" />
        
         <%--OnClientZoomChanged="alert('zoom changed: ' + args[0]);"--%>
        &nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<br />
        &nbsp; &nbsp;&nbsp;
        &nbsp;&nbsp;<asp:Button ID="Button1" runat="server" OnClick="Button1_Click"
            Text="test 1" />
        <asp:Button ID="Button2" runat="server" OnClick="Button2_Click" Text="test2"  />
        <div id = "debugpanel" style="width: 769px; height: 86px; border:solid 2px #404040; overflow:auto; font-size:0.75em">
        </div>
        <br />
        &nbsp;&nbsp;<br />
        &nbsp; &nbsp;<br />
        <cc1:MapPanControl  MapReferenceId="SFMap1" CssClass="test" ID="MapPanControl1" runat="server" Style="z-index: 102;  position: absolute; top: 100px; left:25px; text-align: center; padding-right: 2px; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" ZoomInImageUrl="zoomin.png"  BorderColor="White" BorderWidth="2px" />
        
    </div>
    </form>
</body>
</html>
