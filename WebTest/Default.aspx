<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebTest.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Web Test Default Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:HyperLink ID="HyperLink1" runat="server" NavigateUrl="~/demos/Demo1.aspx">Demo 1</asp:HyperLink> - Demo of using the SFMap control in a webpage</div>
    <div>
        <asp:HyperLink ID="HyperLink2" runat="server" NavigateUrl="~/demos/Demo3.aspx">Demo 2</asp:HyperLink> - Demo of using the SFMap control in a webpage with custom render settings</div>
    
    <div>
        <asp:HyperLink ID="HyperLink3" runat="server" NavigateUrl="~/demos/TileDemo1.aspx">Tile Demo 1</asp:HyperLink> - Demo of using the TiledSFMap control in a webpage
    </div>
    
    <div>
        <asp:HyperLink ID="HyperLink4" runat="server" NavigateUrl="~/demos/BingMapsTest.html">Bing Maps Demo</asp:HyperLink> - Demo of overlaying tiles on Bing Maps
    </div>
    
    <div>
        <asp:HyperLink ID="HyperLink5" runat="server" NavigateUrl="~/demos/GoogleMapsTest.html">Google Maps Demo</asp:HyperLink> - Demo of overlaying tiles on Google Maps
    </div>
    
    </form>
</body>
</html>
