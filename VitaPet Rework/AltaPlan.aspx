<%@ Page Title="<%$ Resources:Textos, PlanAdmin_Alta %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AltaPlan.aspx.cs" Inherits="AltaPlanPage" %>
<%@ Register Src="~/Controls/EditorPlan.ascx" TagPrefix="vp" TagName="EditorPlan" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server"><main class="planes-page"><section class="planes-card plan-admin-card">
<header class="login-header"><h1><%: TextosUI.Obtener("PlanAdmin_Alta") %></h1></header>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnRegistrarPlan"><vp:EditorPlan ID="editorPlan" runat="server" ValidationGroup="AltaPlan" OnErrorPlan="ErrorEditor" /><div class="empresa-acciones"><asp:Button ID="btnRegistrarPlan" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:Textos, PlanAdmin_Registrar %>" CausesValidation="true" ValidationGroup="AltaPlan" OnClick="Registrar" /></div></asp:Panel>
</section></main></asp:Content>
