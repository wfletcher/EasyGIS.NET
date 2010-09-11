<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebTest.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:HyperLink ID="HyperLink1" runat="server" NavigateUrl="~/demos/Demo1.aspx">Demo 1</asp:HyperLink></div>
    <div>
        <asp:HyperLink ID="HyperLink2" runat="server" NavigateUrl="~/demos/Demo2.aspx">Demo 1</asp:HyperLink></div>
    
    <div>
        <asp:HyperLink ID="HyperLink3" runat="server" NavigateUrl="~/demos/TileDemo1.aspx">Time Demo 1</asp:HyperLink>
        </div>
    
    </form>
</body>
</html>
