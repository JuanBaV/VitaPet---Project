 <%@ Page Title="<%$ Resources:Textos, UI_IniciarSesion %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LoginPage" %>

<%@ Register Src="~/Controls/ValidacionEmail.ascx" TagPrefix="vp" TagName="ValidacionEmail" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <asp:HyperLink ID="lnkCatalogoErrores" runat="server"
        NavigateUrl="~/CatalogoErrores.aspx" CssClass="login-errors-shortcut"
        Text="<%$ Resources:Textos, UI_Errores %>" ToolTip="<%$ Resources:Textos, UI_VerMensajesDeErrorParaCapturas %>" />
    <main class="login-page">
        <section class="login-card" aria-labelledby="tituloLogin">
            <header class="login-header">
                <h1 id="tituloLogin"><%: VitaPet.GUI.Resources.Textos.UI_IniciarSesion %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.UI_IngresaAVitapetConTuCorreoElectronicoY %></p>
            </header>

            <asp:ValidationSummary
                ID="vsLogin"
                runat="server"
                ValidationGroup="Login"
                CssClass="login-validation-summary"
                HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>"
                DisplayMode="BulletList" /> 

            <asp:Panel
                ID="pnlMensaje"
                runat="server"
                style="display: none"
                CssClass="login-message"
                role="alert">

                <asp:Literal
                    ID="litMensaje"
                    runat="server" />
            </asp:Panel>

            <vp:ValidacionEmail
                ID="ucCorreo"
                runat="server"
                ValidationGroup="Login" />

            <div class="form-group login-field">
                <asp:Label
                    ID="lblContrasena"
                    runat="server"
                    AssociatedControlID="txtContrasena"
                    CssClass="login-label"
                    Text="<%$ Resources:Textos, UI_Contrasena %>" />

                <asp:TextBox
                    ID="txtContrasena"
                    runat="server"
                    TextMode="Password"
                    CssClass="form-control login-input"
                    MaxLength="100"
                    autocomplete="current-password" />

                <asp:RequiredFieldValidator
                    ID="rfvContrasena"
                    runat="server"
                    ControlToValidate="txtContrasena"
                    ValidationGroup="Login"
                    Display="Dynamic"
                    CssClass="login-validation"
                    ErrorMessage="<%$ Resources:Textos, UI_DebeIngresarSuContrasena %>" />
            </div>

            <asp:Button
                ID="btnIngresar"
                runat="server"
                Text="<%$ Resources:Textos, UI_Ingresar %>"
                ValidationGroup="Login"
                CausesValidation="true"
                OnClick="btnIngresar_Click"
                OnClientClick="return mostrarErrorValidacionLogin();"
                CssClass="btn btn-primary btn-block login-submit" />

            <nav class="login-links" aria-label="<%: VitaPet.GUI.Resources.Textos.UI_OpcionesDeAcceso %>">
                <asp:HyperLink
                    ID="lnkOlvideContrasena"
                    runat="server"
                    NavigateUrl="~/OlvideContrasena.aspx"
                    Text="<%$ Resources:Textos, UI_OlvideMiContrasena %>" />

                <asp:HyperLink
                    ID="lnkRegistroCliente"
                    runat="server"
                    NavigateUrl="~/RegistroCliente.aspx"
                    Text="<%$ Resources:Textos, UI_CrearUnaCuenta %>" />
            </nav>
        </section>
    </main>

    <script type="text/javascript">
        function mostrarErrorValidacionLogin() {
            if (typeof (Page_ClientValidate) !== "function" ||
                Page_ClientValidate("Login")) {
                return true;
            }

            var correo = document.querySelector("[id$='_txtCorreo']");
            var contrasena = document.querySelector("[id$='_txtContrasena']");
            var mensaje = document.getElementById("<%= pnlMensaje.ClientID %>");
            var incompletos =
                !correo || !correo.value.trim() ||
                !contrasena || !contrasena.value.trim();

            mensaje.textContent = mensaje.getAttribute(
                incompletos
                    ? "data-error-incompletos"
                    : "data-error-invalidos");
            mensaje.style.display = "block";
            mensaje.setAttribute("role", "alert");

            return false;
        }
    </script>
</asp:Content>
