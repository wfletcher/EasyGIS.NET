<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Demo2.aspx.cs" Inherits="WebTest.demos.Demo2" %>

<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<%--<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>--%>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        This is demo 2<br />
        <br />
        <cc1:SFMap ID="SFMap1" runat="server" BackColor="#C0C0FF" Height="535px" Width="770px" BorderColor="LightGray" BorderStyle="Dashed" BorderWidth="2px" ProjectName="~/demos/demo2maps/Framework/demo2.egp" />
        &nbsp; &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &nbsp;&nbsp; &nbsp;&nbsp; &nbsp;&nbsp;
        &nbsp; &nbsp;<br />
        &nbsp; &nbsp;&nbsp;
        &nbsp;&nbsp;<br />
        &nbsp;&nbsp;<br />
        &nbsp; &nbsp;<asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Button" /><br />
        <cc1:MapPanControl  MapReferenceId="SFMap1" ID="MapPanControl1" runat="server" Style="z-index: 102; left: 27px;
             position: absolute; top: 182px; text-align: center; padding-right: 2px; padding-left: 2px; padding-bottom: 2px; padding-top: 2px;" Width="66px" />
    </div>
    </form>
</body>
</html>
