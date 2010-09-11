<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="3dtest.aspx.cs" Inherits="WebTest.tests._dtest" %>

<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">        
    <div>
    
    <cc1:SFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="435px" Width="650px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px"
         ProjectName="~/demos/melb3d.egp"  CacheOnServer="false" ServerCacheDirectoryUrl="cache" CacheOnClient="False"  MinZoomLevel="20" MaxZoomLevel="50000" />                
    </div>
    </form>
</body>
</html>
