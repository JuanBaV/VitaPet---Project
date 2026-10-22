<%@ Page Title="<%$ Resources:Textos, Mascota_RegistroTitulo %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistroMascota.aspx.cs" Inherits="RegistroMascotaPage" %>
<%@ Register Assembly="VitaPet.GUI" Namespace="VitaPet.GUI.Presentacion" TagPrefix="vp" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="login-page"><section class="login-card registro-card">
        <a runat="server" href="~/Mascotas.aspx" class="btn btn-default formulario-volver"><span aria-hidden="true">&larr;</span> <%: VitaPet.GUI.Resources.Textos.UI_Volver %></a>
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.Mascota_RegistroTitulo %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnRegistrar">
<asp:ValidationSummary ID="vsRegistroMascota" runat="server" ValidationGroup="RegistroMascota" CssClass="login-validation-summary" HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>" DisplayMode="BulletList" role="alert" />
            <div class="form-group login-field"><asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_Nombre %>" /><asp:TextBox ID="txtNombre" runat="server" MaxLength="60" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revNombre" runat="server" ControlToValidate="txtNombre" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,60}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
            <div class="form-group login-field"><asp:Label ID="lblEspecie" runat="server" AssociatedControlID="txtEspecie" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_Especie %>" /><asp:TextBox ID="txtEspecie" runat="server" MaxLength="40" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvEspecie" runat="server" ControlToValidate="txtEspecie" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revEspecie" runat="server" ControlToValidate="txtEspecie" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,40}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
            <div class="form-group login-field"><asp:Label ID="lblRaza" runat="server" AssociatedControlID="txtRaza" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_Raza %>" /><asp:TextBox ID="txtRaza" runat="server" MaxLength="60" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvRaza" runat="server" ControlToValidate="txtRaza" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revRaza" runat="server" ControlToValidate="txtRaza" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,60}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
            <div class="form-group login-field"><asp:Label ID="lblFechaNacimiento" runat="server" AssociatedControlID="dtpFechaNacimiento" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_FechaNacimiento %>" /><vp:DateTimePicker ID="dtpFechaNacimiento" runat="server" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvFechaNacimiento" runat="server" ControlToValidate="dtpFechaNacimiento" ValidationGroup="RegistroMascota" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:CustomValidator ID="cvFechaNacimiento" runat="server" ControlToValidate="dtpFechaNacimiento" ValidationGroup="RegistroMascota" OnServerValidate="ValidarFechaNacimiento" EnableClientScript="false" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_FechaNacimientoInvalida %>" /></div>
            <asp:CustomValidator ID="cvDatosMascota" runat="server" ValidationGroup="RegistroMascota" ValidateEmptyText="true" EnableClientScript="false" OnServerValidate="ValidarDatos" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_DatosInvalidos %>" />
<asp:Button ID="btnRegistrar" runat="server" CssClass="btn btn-primary btn-block login-submit" Text="<%$ Resources:Textos, Mascota_BotonRegistrar %>" CausesValidation="true" ValidationGroup="RegistroMascota" OnClick="Registrar" />
        </asp:Panel>
        <asp:Label ID="lblConfirmacion" runat="server" Visible="false" CssClass="login-message registro-confirmacion mascota-confirmacion" role="status" Text="<%$ Resources:Textos, Mascota_Confirmacion %>" />
    </section></main>
</asp:Content>
