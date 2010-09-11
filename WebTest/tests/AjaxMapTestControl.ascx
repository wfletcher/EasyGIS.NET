<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AjaxMapTestControl.ascx.cs" Inherits="WebTest.tests.AjaxMapTestControl" %>
<%@ Register Assembly="EGIS.Web.Controls" Namespace="EGIS.Web.Controls" TagPrefix="cc1" %>

<div style="width:500px;height:400px; border:solid 1px gray; position:relative; background:#fafafa">
<cc1:SFMap ID="SFMap1" style="position:absolute;left:0px;top:0px" runat="server" Height="400px" Width="500px" ProjectName="~/demos/demo2.egp" />
<cc1:MapPanControl ID="MapPanControl1" runat="server" Style="z-index: 100; left: 10px;
    position: absolute; top: 10px; text-align:center"  MapReferenceId="SFMap1"/>
</div>
Select Project to view
<asp:DropDownList ID="DropDownList1" runat="server" Height="23px" Width="148px">
</asp:DropDownList>
<asp:Button ID="Button1" runat="server" Text="Update" OnClick="Button1_Click" />
