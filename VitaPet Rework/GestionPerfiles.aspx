<%@ Page Title="Perfiles y permisos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GestionPerfiles.aspx.cs" Inherits="GestionPerfilesPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<main class="planes-page"><section class="planes-card">
<a runat="server" href="~/Home.aspx" class="btn btn-default formulario-volver">&larr; <%: TextosUI.Traducir("Volver") %></a>
<header class="login-header"><h1><%: TextosUI.Traducir("Perfiles y permisos") %></h1></header>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-info" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:GridView ID="gvPerfiles" runat="server" AutoGenerateColumns="false" DataKeyNames="Id" CssClass="table table-striped perfiles-grilla" OnSelectedIndexChanged="Seleccionar" EmptyDataText="No hay perfiles.">
<Columns><asp:BoundField DataField="Nombre" HeaderText="Perfil" /><asp:BoundField DataField="Permisos" HeaderText="Pantallas permitidas" /><asp:CommandField ShowSelectButton="true" SelectText="Ver detalle" /></Columns>
</asp:GridView>
<asp:Panel ID="pnlDetalle" runat="server" Visible="false"><h2><asp:Literal ID="litPerfil" runat="server" /></h2><p><%: TextosUI.Traducir("Permisos heredados del perfil y sus componentes") %></p><asp:TreeView ID="arbol" runat="server" ShowLines="true" /><p><%: TextosUI.Traducir("Los usuarios reciben los permisos de su perfil; no se asignan permisos individuales.") %></p></asp:Panel>
<asp:Panel ID="pnlEdicion" runat="server" Visible="false" DefaultButton="btnGuardar">
<asp:Button ID="btnNuevo" runat="server" Text="Nuevo perfil" OnClick="Nuevo" CssClass="btn btn-default" CausesValidation="false" />
<div class="form-group"><asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" Text="Nombre del perfil" /><asp:TextBox ID="txtNombre" runat="server" MaxLength="30" CssClass="form-control" /></div>
<fieldset><legend><%: TextosUI.Traducir("Pantallas y perfiles incluidos") %></legend><asp:CheckBoxList ID="chkComponentes" runat="server" RepeatLayout="UnorderedList" CssClass="perfil-componentes" /></fieldset>
<div class="empresa-acciones"><asp:Button ID="btnGuardar" runat="server" Text="Guardar perfil" OnClick="Guardar" CssClass="btn btn-primary" /><asp:Button ID="btnEliminar" runat="server" Text="Eliminar perfil" OnClick="Eliminar" CssClass="btn btn-danger" CausesValidation="false" /></div>
<asp:Panel ID="pnlConfirmar" runat="server" Visible="false" CssClass="alert alert-warning" role="alert"><p><asp:Literal ID="litConfirmar" runat="server" /></p><asp:Button ID="btnConfirmar" runat="server" Text="Confirmar eliminación" OnClick="ConfirmarEliminacion" CssClass="btn btn-danger" CausesValidation="false" /><asp:Button ID="btnCancelar" runat="server" Text="Cancelar" OnClick="CancelarEliminacion" CssClass="btn btn-default" CausesValidation="false" /></asp:Panel>
</asp:Panel></section></main></asp:Content>
