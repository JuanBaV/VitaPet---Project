<%@ Page Title="<%$ Resources:Textos, PlanAdmin_Modificar %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ModificarPlan.aspx.cs" Inherits="ModificarPlanPage" %>
<%@ Register Src="~/Controls/EditorPlan.ascx" TagPrefix="vp" TagName="EditorPlan" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server"><main class="planes-page"><section class="planes-card plan-admin-card">
<header class="login-header"><h1><%: TextosUI.Obtener("PlanAdmin_Modificar") %></h1></header>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" Visible="false" DefaultButton="btnGuardarPlan"><dl class="empresa-resumen"><div><dt><%: TextosUI.Obtener("Empresa_Id") %></dt><dd><asp:Label ID="lblIdPlan" runat="server" /></dd></div></dl><vp:EditorPlan ID="editorPlan" runat="server" OnErrorPlan="ErrorEditor" /><div class="empresa-acciones"><asp:Button ID="btnGuardarPlan" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_Guardar %>" OnClick="Guardar" /></div></asp:Panel>
</section></main></asp:Content>
