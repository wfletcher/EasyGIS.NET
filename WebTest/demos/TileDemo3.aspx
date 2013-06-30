<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TileDemo3.aspx.cs" Inherits="WebTest.demos.TileDemo3" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>

    <script type="text/javascript" language="javascript" >
        function buttonClicked()
        {
            var x = document.getElementById("DropDownList1");
            var map = egis.GetMap();
            if (map != null) {
                //update the MapHandler used based on the selected index
                //the ver parameter is just used to stop images being cached by the browser and is not neccessary
                var newUrl = "TileDemo3Handler.ashx?ver=4&rendertype=" + x.selectedIndex;
                map.SetMapHandler(newUrl);
                //refresh the map after the map handler is changed
                map.refreshMap();
            }
            return false;
        };

    </script>

        Tile Demo 3<br />        
        Select Custom Render Settings<br />
        <asp:DropDownList ID="DropDownList1" runat="server" Width="175px" ClientIDMode="Static">

            <asp:ListItem>Population</asp:ListItem>
            <asp:ListItem>None</asp:ListItem>
            <asp:ListItem>Random Color</asp:ListItem>
            <asp:ListItem>Select First 100 Records</asp:ListItem> 
            <asp:ListItem>Select Records Intersecting Circle</asp:ListItem> 
                       
        </asp:DropDownList>
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click"  OnClientClick = "return buttonClicked()" Text="Update Map"
            Width="103px" /><br />
        <br />
        <cc1:TiledSFMap ID="SFMap1" runat="server" BackColor="#C0C0C0" Height="535px" Width="770px" BorderColor="LightGray"
        BorderStyle="Dashed" BorderWidth="2px" ProjectName="us_demo.egp" CacheOnClient="true"
         MinZoomLevel="2" MaxZoomLevel="16" HttpHandlerName = "TileDemo3Handler.ashx"/>
        <br />
        <cc1:MapPanControl class="mpc" ID="MapPanControl1" runat="server" Style="z-index: 102; left: 27px;
             position: absolute; top: 182px; text-align: center; padding-right: 2px; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" />
    </div>
    </form>
</body>
</html>
