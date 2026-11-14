<%@ Page Title="<%$ Resources:Textos, UI_CrearUnaCuenta %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistroCliente.aspx.cs" Inherits="RegistroClientePage" %>
<%@ Register Src="~/Controls/ValidacionEmail.ascx" TagPrefix="vp" TagName="ValidacionEmail" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="login-page">
        <section class="login-card registro-card" aria-labelledby="tituloRegistro">
            <asp:HyperLink ID="lnkLogin" runat="server" NavigateUrl="~/Login.aspx" CssClass="btn btn-default formulario-volver"><span aria-hidden="true">&larr;</span> <asp:Literal runat="server" Text="<%$ Resources:Textos, UI_Volver %>" /></asp:HyperLink>
            <header class="login-header">
                <h1 id="tituloRegistro"><%: VitaPet.GUI.Resources.Textos.UI_CrearUnaCuenta %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.Registro_Descripcion %></p>
            </header>
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message" role="alert">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>
            <asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnRegistrar">
<asp:ValidationSummary ID="vsRegistroCliente" runat="server" ValidationGroup="RegistroCliente" CssClass="login-validation-summary" HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>" DisplayMode="BulletList" role="alert" />
                <div class="form-group login-field">
                    <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" CssClass="login-label" Text="<%$ Resources:Textos, UI_Nombre %>" />
                    <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control login-input" MaxLength="60" autocomplete="given-name" />
<asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,60}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" />
                </div>
                <div class="form-group login-field">
                    <asp:Label ID="lblApellido" runat="server" AssociatedControlID="txtApellido" CssClass="login-label" Text="<%$ Resources:Textos, Registro_Apellido %>" />
                    <asp:TextBox ID="txtApellido" runat="server" CssClass="form-control login-input" MaxLength="60" autocomplete="family-name" />
<asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revApellido" runat="server" ControlToValidate="txtApellido" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,60}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" />
                </div>
                <%-- implementacion del componente ValidacionEmail.ascx --%><vp:ValidacionEmail ID="ucCorreo" runat="server" ValidationGroup="RegistroCliente" MaxLength="100" />
                <div class="form-group login-field">
                    <asp:Label ID="lblContrasena" runat="server" AssociatedControlID="txtContrasena" CssClass="login-label" Text="<%$ Resources:Textos, UI_Contrasena %>" />
                    <asp:TextBox ID="txtContrasena" runat="server" TextMode="Password" CssClass="form-control login-input" MaxLength="100" autocomplete="new-password" aria-describedby="ayudaContrasena" />
<asp:RequiredFieldValidator ID="rfvContrasena" runat="server" ControlToValidate="txtContrasena" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revContrasena" runat="server" ControlToValidate="txtContrasena" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ValidationExpression="^(?=.{8,100}$)(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).+$" ErrorMessage="<%$ Resources:Textos, Alta_ContrasenaInvalida %>" />
                    <p id="ayudaContrasena" class="registro-ayuda"><%: VitaPet.GUI.Resources.Textos.Registro_RequisitosContrasena %></p>
                </div>
                <div class="form-group login-field">
                    <asp:Label ID="lblTelefono" runat="server" AssociatedControlID="txtTelefono" CssClass="login-label" Text="<%$ Resources:Textos, UI_Telefono %>" />
                    <asp:TextBox ID="txtTelefono" runat="server" TextMode="Phone" CssClass="form-control login-input" MaxLength="30" autocomplete="tel" />
<asp:RequiredFieldValidator ID="rfvTelefono" runat="server" ControlToValidate="txtTelefono" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revTelefono" runat="server" ControlToValidate="txtTelefono" ValidationGroup="RegistroCliente" Display="Dynamic" CssClass="login-validation" ValidationExpression="^(?=.{1,30}$)\+?(?=(?:[^0-9]*[0-9]){6})[0-9 ()\-.]+$" ErrorMessage="<%$ Resources:Textos, Alta_TelefonoInvalido %>" />
                </div>
                <asp:CustomValidator ID="cvDatosRegistro" runat="server" ValidationGroup="RegistroCliente" ValidateEmptyText="true" EnableClientScript="false" OnServerValidate="ValidarDatos" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_DatosInvalidos %>" />
<asp:Button ID="btnRegistrar" runat="server" Text="<%$ Resources:Textos, UI_CrearUnaCuenta %>" CausesValidation="true" ValidationGroup="RegistroCliente" OnClick="btnRegistrar_Click" CssClass="btn btn-primary btn-block login-submit" />
            </asp:Panel>
        </section>
    </main>
</asp:Content>
