<%@ Page Title="Consultar perfiles" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ConsultarPerfiles.aspx.cs" Inherits="ConsultarPerfilesPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server"><main class="planes-page"><section class="planes-card">
<a runat="server" href="~/Home.aspx" class="btn btn-default formulario-volver">&larr; <%: TextosUI.Traducir("Volver") %></a>
<h1><%: TextosUI.Traducir("Perfiles y permisos") %></h1>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-danger" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:TreeView ID="arbol" runat="server" ShowLines="true" />
<p><%: TextosUI.Traducir("Los usuarios reciben los permisos de su perfil; no se asignan permisos individuales.") %></p>
</section></main></asp:Content>
