<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TileDemo2.aspx.cs" Inherits="WebTest.demos.TileDemo2" %>

<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <cc1:TiledSFMap ID="TiledSFMap1" runat="server" Height="294px" Width="443px" MaxZoomLevel="100000" MinZoomLevel="2"  CacheOnServer="false" CacheOnClient="false" ProjectName="~/demos/us_demo.egp" BackColor="#E0E0E0" BorderStyle="Dashed" BorderWidth="1px" />
        <cc1:MapPanControl ID="MapPanControl1" runat="server" Style="z-index: 100; left: 25px;
            position: absolute; top: 30px; text-align:center" MapReferenceId="TiledSFMap1" />
    
    </div>
    </form>
</body>
</html>
