<%@ Page Title="<%$ Resources:Textos, PlanAdmin_Consulta %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ConsultaPlan.aspx.cs" Inherits="ConsultaPlanPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server"><main class="planes-page"><section class="planes-card plan-admin-card">
<a runat="server" href="~/GestionPlanes.aspx" class="btn btn-default formulario-volver"><span aria-hidden="true">&larr;</span> <%: VitaPet.GUI.Resources.Textos.UI_Volver %></a>
<header class="login-header"><h1><%: TextosUI.Obtener("PlanAdmin_Consulta") %></h1></header>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" Visible="false"><dl class="empresa-resumen"><div><dt><%: TextosUI.Obtener("Empresa_Id") %></dt><dd><asp:Label ID="lblIdPlan" runat="server" /></dd></div>
<div><dt><%: TextosUI.Obtener("UI_Nombre") %></dt><dd><asp:Label ID="lblNombrePlan" runat="server" /></dd></div>
<div><dt><%: TextosUI.Obtener("PlanAdmin_Descripcion") %></dt><dd><asp:Label ID="lblDescripcionPlan" runat="server" /></dd></div>
<div><dt><%: TextosUI.Obtener("PlanAdmin_Precio") %></dt><dd><asp:Label ID="lblPrecioPlan" runat="server" /></dd></div>
<div><dt><%: TextosUI.Obtener("Empresa_Estado") %></dt><dd><asp:Label ID="lblEstadoPlan" runat="server" /></dd></div>
</dl><h2 class="plan-admin-subtitulo"><%: TextosUI.Obtener("PlanAdmin_Coberturas") %></h2><div class="table-responsive plan-admin-tabla"><asp:GridView ID="gvCoberturas" runat="server" AutoGenerateColumns="false" CssClass="table" GridLines="None"><Columns><asp:BoundField DataField="Prestacion" HeaderText="<%$ Resources:Textos, PlanAdmin_Prestacion %>" HtmlEncode="true" /><asp:BoundField DataField="Detalle" HeaderText="<%$ Resources:Textos, PlanAdmin_Detalle %>" HtmlEncode="true" /><asp:TemplateField HeaderText="<%$ Resources:Textos, Empresa_Estado %>"><ItemTemplate><%#: TextosUI.Obtener((bool)Eval("Estado")?"Empresa_Activo":"Empresa_Inactivo") %></ItemTemplate></asp:TemplateField></Columns></asp:GridView></div></asp:Panel>
</section></main></asp:Content>
