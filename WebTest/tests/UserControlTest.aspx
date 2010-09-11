<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UserControlTest.aspx.cs" Inherits="WebTest.tests.UserControlTest" %>

<%@ Register Src="MapTestControl.ascx" TagName="MapTestControl" TagPrefix="uc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <br />
    <p>
    UserControl test
    </p>
        <uc1:MapTestControl ID="MapTestControl1" runat="server" />
    </div>
    </form>
</body>
</html>
