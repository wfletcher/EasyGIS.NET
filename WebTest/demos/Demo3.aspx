<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Demo3.aspx.cs" Inherits="WebTest.demos.Demo3" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Demo 3</title>
    <style type="text/css">.mpc input {padding:2px;} </style>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Demo 3<br />        
        Select Custom Render Settings<br />
        <asp:DropDownList ID="DropDownList1" runat="server" Width="175px">
            <asp:ListItem>Please Select..</asp:ListItem>
            <asp:ListItem>Population Density</asp:ListItem>
            <asp:ListItem>Average House Sale</asp:ListItem>
            <asp:ListItem>Divorced</asp:ListItem>
            <asp:ListItem>Median Rent</asp:ListItem>
            
        </asp:DropDownList>
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Generate Map"
            Width="103px" /><br />
        <br />
        <cc1:SFMap ID="SFMap1" runat="server" BackColor="#C0C0C0" Height="535px" Width="770px" BorderColor="LightGray"
        BorderStyle="Dashed" BorderWidth="2px" ProjectName="us_demo.egp" CacheOnClient="false"
         MinZoomLevel="2.0" MaxZoomLevel="500"/>
        <br />
        <cc1:MapPanControl class="mpc" ID="MapPanControl1" runat="server" Style="z-index: 102; left: 27px;
             position: absolute; top: 182px; text-align: center; padding-right: 2px; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" />
    </div>
    </form>
</body>
</html>
