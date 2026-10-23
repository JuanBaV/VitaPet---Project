<%@ Page Title="Alta de usuarios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AltaUsuario.aspx.cs" Inherits="AltaUsuarioPage" %>
<%@ Register Src="~/Controls/ValidacionEmail.ascx" TagPrefix="vp" TagName="ValidacionEmail" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server"><main class="login-page registro-page"><section class="login-card registro-card">
<a runat="server" href="~/GestionUsuarios.aspx" class="btn btn-default formulario-volver">&larr; <%: TextosUI.Traducir("Volver") %></a>
<header class="login-header"><h1><%: TextosUI.Traducir("Alta de usuarios") %></h1></header>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-info" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnCrear">
<asp:ValidationSummary ID="resumen" runat="server" ValidationGroup="AltaUsuario" CssClass="login-validation-summary" HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>" role="alert" />
<div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtNombre" Text="<%$ Resources:Textos, UI_Nombre %>" /><asp:TextBox ID="txtNombre" runat="server" MaxLength="60" CssClass="form-control" />
<asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,60}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
<div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtApellido" Text="<%$ Resources:Textos, Registro_Apellido %>" /><asp:TextBox ID="txtApellido" runat="server" MaxLength="60" CssClass="form-control" />
<asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revApellido" runat="server" ControlToValidate="txtApellido" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,60}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
<vp:ValidacionEmail ID="ucCorreo" runat="server" ValidationGroup="AltaUsuario" MaxLength="100" />
<div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtContrasena" Text="<%$ Resources:Textos, UI_Contrasena %>" /><asp:TextBox ID="txtContrasena" runat="server" TextMode="Password" MaxLength="100" CssClass="form-control" autocomplete="new-password" />
<asp:RequiredFieldValidator ID="rfvContrasena" runat="server" ControlToValidate="txtContrasena" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revContrasena" runat="server" ControlToValidate="txtContrasena" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ValidationExpression="^(?=.{8,100}$)(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).+$" ErrorMessage="<%$ Resources:Textos, Alta_ContrasenaInvalida %>" />
<p class="registro-ayuda"><%: VitaPet.GUI.Resources.Textos.Registro_RequisitosContrasena %></p></div>
<div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtTelefono" Text="<%$ Resources:Textos, UI_Telefono %>" /><asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" CssClass="form-control" />
<asp:RequiredFieldValidator ID="rfvTelefono" runat="server" ControlToValidate="txtTelefono" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revTelefono" runat="server" ControlToValidate="txtTelefono" ValidationGroup="AltaUsuario" Display="Dynamic" CssClass="login-validation" ValidationExpression="^(?=.{1,30}$)\+?(?=(?:[^0-9]*[0-9]){6})[0-9 ()\-.]+$" ErrorMessage="<%$ Resources:Textos, Alta_TelefonoInvalido %>" /></div>
<div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="ddlPerfil" Text="Perfil" /><asp:DropDownList ID="ddlPerfil" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="VerPermisos" /></div>
<h2><%: TextosUI.Traducir("Permisos del perfil") %></h2><asp:BulletedList ID="listaPermisos" runat="server" /><p><%: TextosUI.Traducir("Los usuarios reciben los permisos de su perfil; no se asignan permisos individuales.") %></p>
<asp:CustomValidator ID="cvDatosUsuario" runat="server" ValidationGroup="AltaUsuario" ValidateEmptyText="true" EnableClientScript="false" OnServerValidate="ValidarDatos" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_DatosInvalidos %>" />
<asp:Button ID="btnCrear" runat="server" Text="Crear usuario" OnClick="Crear" CssClass="btn btn-primary" CausesValidation="true" ValidationGroup="AltaUsuario" />
</asp:Panel></section></main></asp:Content>
