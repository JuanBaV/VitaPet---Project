<%@ Page Title="<%$ Resources:Textos, UI_CrearUnaCuenta %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistroCliente.aspx.cs" Inherits="RegistroClientePage" %>
<%@ Register Src="~/Controls/ValidacionEmail.ascx" TagPrefix="vp" TagName="ValidacionEmail" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="login-page">
        <section class="login-card registro-card" aria-labelledby="tituloRegistro">
            <header class="login-header">
                <h1 id="tituloRegistro"><%: VitaPet.GUI.Resources.Textos.UI_CrearUnaCuenta %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.Registro_Descripcion %></p>
            </header>
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message" role="alert">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>
            <asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnRegistrar">
                <div class="form-group login-field">
                    <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" CssClass="login-label" Text="<%$ Resources:Textos, UI_Nombre %>" />
                    <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control login-input" MaxLength="60" autocomplete="given-name" />
                </div>
                <div class="form-group login-field">
                    <asp:Label ID="lblApellido" runat="server" AssociatedControlID="txtApellido" CssClass="login-label" Text="<%$ Resources:Textos, Registro_Apellido %>" />
                    <asp:TextBox ID="txtApellido" runat="server" CssClass="form-control login-input" MaxLength="60" autocomplete="family-name" />
                </div>
                <vp:ValidacionEmail ID="ucCorreo" runat="server" ValidationGroup="RegistroCliente" MaxLength="100" />
                <div class="form-group login-field">
                    <asp:Label ID="lblContrasena" runat="server" AssociatedControlID="txtContrasena" CssClass="login-label" Text="<%$ Resources:Textos, UI_Contrasena %>" />
                    <asp:TextBox ID="txtContrasena" runat="server" TextMode="Password" CssClass="form-control login-input" MaxLength="100" autocomplete="new-password" aria-describedby="ayudaContrasena" />
                    <p id="ayudaContrasena" class="registro-ayuda"><%: VitaPet.GUI.Resources.Textos.Registro_RequisitosContrasena %></p>
                </div>
                <div class="form-group login-field">
                    <asp:Label ID="lblTelefono" runat="server" AssociatedControlID="txtTelefono" CssClass="login-label" Text="<%$ Resources:Textos, UI_Telefono %>" />
                    <asp:TextBox ID="txtTelefono" runat="server" TextMode="Phone" CssClass="form-control login-input" MaxLength="30" autocomplete="tel" />
                </div>
                <asp:Button ID="btnRegistrar" runat="server" Text="<%$ Resources:Textos, UI_CrearUnaCuenta %>" CausesValidation="false" OnClick="btnRegistrar_Click" CssClass="btn btn-primary btn-block login-submit" />
            </asp:Panel>
            <nav class="login-links" aria-label="<%: VitaPet.GUI.Resources.Textos.UI_OpcionesDeAcceso %>">
                <asp:HyperLink ID="lnkLogin" runat="server" NavigateUrl="~/Login.aspx" Text="<%$ Resources:Textos, UI_VolverAlInicioDeSesion %>" />
            </nav>
        </section>
    </main>
</asp:Content>
