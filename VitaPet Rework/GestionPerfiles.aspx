<%@ Page Title="Perfiles y permisos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GestionPerfiles.aspx.cs" Inherits="GestionPerfilesPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<main class="planes-page"><section class="planes-card">
<a runat="server" href="~/Home.aspx" class="btn btn-default formulario-volver">&larr; <%: TextosUI.Traducir("Volver") %></a>
<header class="login-header"><h1><%: TextosUI.Traducir("Perfiles y permisos") %></h1></header>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-info" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:GridView ID="gvPerfiles" runat="server" AutoGenerateColumns="false" DataKeyNames="Id" CssClass="table table-striped perfiles-grilla" OnSelectedIndexChanged="Seleccionar" EmptyDataText="No hay perfiles.">
<Columns><asp:BoundField DataField="Nombre" HeaderText="Perfil" /><asp:BoundField DataField="Permisos" HeaderText="Pantallas permitidas" /><asp:TemplateField><ItemTemplate>
<div class="perfil-acciones">
<asp:LinkButton ID="btnVerDetalle" runat="server" Text="Ver detalle" CommandName="Select" CausesValidation="false" />
<asp:Button ID="btnEliminarPerfil" runat="server" Text="Eliminar perfil" CommandArgument='<%# Eval("Id") %>' OnCommand="Eliminar" Visible="<%# Edita %>" Enabled='<%# !(bool)Eval("Sistema") %>' CssClass="btn btn-danger btn-sm" CausesValidation="false" />
</div>
</ItemTemplate></asp:TemplateField></Columns>
</asp:GridView>
<asp:Panel ID="pnlConfirmar" runat="server" Visible="false" CssClass="alert alert-warning perfil-confirmacion" role="alert" TabIndex="-1"><p><asp:Literal ID="litConfirmar" runat="server" /></p><div class="empresa-acciones"><asp:Button ID="btnConfirmar" runat="server" Text="Confirmar eliminación" OnClick="ConfirmarEliminacion" CssClass="btn btn-danger" CausesValidation="false" /><asp:Button ID="btnCancelar" runat="server" Text="Cancelar" OnClick="CancelarEliminacion" CssClass="btn btn-default" CausesValidation="false" /></div></asp:Panel>
<asp:Panel ID="pnlDetalle" runat="server" Visible="false"><h2><asp:Literal ID="litPerfil" runat="server" /></h2><p><%: TextosUI.Traducir("Permisos heredados del perfil y sus componentes") %></p><asp:TreeView ID="arbol" runat="server" ShowLines="true" /><p><%: TextosUI.Traducir("Los usuarios reciben los permisos de su perfil; no se asignan permisos individuales.") %></p></asp:Panel>
<asp:Panel ID="pnlEdicion" runat="server" Visible="false" DefaultButton="btnGuardar">
<asp:ValidationSummary ID="vsPerfil" runat="server" ValidationGroup="Perfil" CssClass="login-validation-summary" HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>" role="alert" />
<div class="form-group"><asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" Text="Nombre del perfil" /><asp:TextBox ID="txtNombre" runat="server" MaxLength="30" CssClass="form-control" />
<asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="Perfil" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="Perfil" EnableClientScript="false" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[\p{L}\p{Nd} _-]{1,30}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
<fieldset><legend><%: TextosUI.Traducir("Pantallas y perfiles incluidos") %></legend><asp:CheckBoxList ID="chkComponentes" runat="server" RepeatLayout="UnorderedList" CssClass="perfil-componentes" /></fieldset>
<asp:CustomValidator ID="cvComponentes" runat="server" ValidationGroup="Perfil" ValidateEmptyText="true" EnableClientScript="false" OnServerValidate="ValidarComponentes" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<div class="empresa-acciones"><asp:Button ID="btnGuardar" runat="server" Text="Guardar perfil" OnClick="Guardar" CssClass="btn btn-primary" CausesValidation="true" ValidationGroup="Perfil" /></div>
</asp:Panel>
<asp:Button ID="btnNuevo" runat="server" Text="Nuevo perfil" OnClick="Nuevo" CssClass="btn btn-default perfil-nuevo" CausesValidation="false" />
</section></main></asp:Content>
