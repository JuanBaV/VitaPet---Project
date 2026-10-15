<%@ Page Title="<%$ Resources:Textos, UI_RecuperarContrasena %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="OlvideContrasena.aspx.cs" Inherits="OlvideContrasenaPage" %>

<%@ Register Src="~/Controls/ValidacionEmail.ascx" TagPrefix="vp" TagName="ValidacionEmail" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="login-page">
        <section class="login-card" aria-labelledby="tituloRecuperacion">
            <header class="login-header">
                <h1 id="tituloRecuperacion"><%: VitaPet.GUI.Resources.Textos.UI_RecuperarContrasena %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.UI_IngresaTuCorreoElectronicoYRespondeLaPregunta %></p>
            </header>

            <asp:ValidationSummary
                ID="vsRecuperacion"
                runat="server"
                ValidationGroup="Recuperacion"
                CssClass="login-validation-summary"
                HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>"
                DisplayMode="BulletList" />

            <asp:Panel
                ID="pnlMensaje"
                runat="server"
                Visible="false"
                CssClass="login-message"
                role="alert">

                <asp:Literal
                    ID="litMensaje"
                    runat="server" />
            </asp:Panel>

            <vp:ValidacionEmail
                ID="ucCorreo"
                runat="server"
                ValidationGroup="Recuperacion" />

            <div class="form-group login-field">
                <asp:Label
                    ID="lblRespuesta"
                    runat="server"
                    AssociatedControlID="txtRespuesta"
                    CssClass="login-label"
                    Text="<%$ Resources:Textos, UI_CualEsSuFacultadFavorita %>" />

                <asp:TextBox
                    ID="txtRespuesta"
                    runat="server"
                    CssClass="form-control login-input"
                    autocomplete="off" />

                <asp:RequiredFieldValidator
                    ID="rfvRespuesta"
                    runat="server"
                    ControlToValidate="txtRespuesta"
                    ValidationGroup="Recuperacion"
                    Display="Dynamic"
                    CssClass="login-validation"
                    ErrorMessage="<%$ Resources:Textos, UI_DebeIngresarLaRespuesta %>" />
            </div>

            <asp:Button
                ID="btnContinuar"
                runat="server"
                Text="<%$ Resources:Textos, UI_Continuar %>"
                ValidationGroup="Recuperacion"
                CausesValidation="true"
                OnClick="btnContinuar_Click"
                CssClass="btn btn-primary btn-block login-submit" />

            <nav class="login-links" aria-label="<%: VitaPet.GUI.Resources.Textos.UI_OpcionesDeRecuperacion %>">
                <asp:HyperLink
                    ID="lnkVolverLogin"
                    runat="server"
                    NavigateUrl="~/Login.aspx"
                    Text="<%$ Resources:Textos, UI_VolverAlInicioDeSesion %>" />
            </nav>
        </section>
    </main>
</asp:Content>
