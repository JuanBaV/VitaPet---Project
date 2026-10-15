<%@ Page Title="<%$ Resources:Textos, UI_RestablecerContrasena %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RestablecerContrasena.aspx.cs" Inherits="RestablecerContrasenaPage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="login-page">
        <section class="login-card" aria-labelledby="tituloRestablecer">
            <header class="login-header">
                <h1 id="tituloRestablecer"><%: VitaPet.GUI.Resources.Textos.UI_RestablecerContrasena %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.UI_IngresaYConfirmaTuNuevaContrasena %></p>
            </header>

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

            <asp:Panel
                ID="pnlFormulario"
                runat="server">

                <asp:ValidationSummary
                    ID="vsRestablecer"
                    runat="server"
                    ValidationGroup="RestablecerContrasena"
                    CssClass="login-validation-summary"
                    HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>"
                    DisplayMode="BulletList" />

                <div class="form-group login-field">
                    <asp:Label
                        ID="lblNuevaContrasena"
                        runat="server"
                        AssociatedControlID="txtNuevaContrasena"
                        CssClass="login-label"
                        Text="<%$ Resources:Textos, UI_NuevaContrasena %>" />

                    <asp:TextBox
                        ID="txtNuevaContrasena"
                        runat="server"
                        TextMode="Password"
                        CssClass="form-control login-input"
                        MaxLength="100"
                        autocomplete="new-password" />

                    <asp:RequiredFieldValidator
                        ID="rfvNuevaContrasena"
                        runat="server"
                        ControlToValidate="txtNuevaContrasena"
                        ValidationGroup="RestablecerContrasena"
                        Display="Dynamic"
                        CssClass="login-validation"
                        ErrorMessage="<%$ Resources:Textos, UI_DebeIngresarLaNuevaContrasena %>" />

                    <asp:RegularExpressionValidator
                        ID="revNuevaContrasena"
                        runat="server"
                        ControlToValidate="txtNuevaContrasena"
                        ValidationGroup="RestablecerContrasena"
                        Display="Dynamic"
                        CssClass="login-validation"
                        ValidationExpression="^(?=.{8,}$)(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).+$"
                        ErrorMessage="<%$ Resources:Textos, UI_LaContrasenaDebeTenerAlMenos8Caracteres %>" />
                </div>

                <div class="form-group login-field">
                    <asp:Label
                        ID="lblConfirmarContrasena"
                        runat="server"
                        AssociatedControlID="txtConfirmarContrasena"
                        CssClass="login-label"
                        Text="<%$ Resources:Textos, UI_ConfirmarNuevaContrasena %>" />

                    <asp:TextBox
                        ID="txtConfirmarContrasena"
                        runat="server"
                        TextMode="Password"
                        CssClass="form-control login-input"
                        MaxLength="100"
                        autocomplete="new-password" />

                    <asp:RequiredFieldValidator
                        ID="rfvConfirmarContrasena"
                        runat="server"
                        ControlToValidate="txtConfirmarContrasena"
                        ValidationGroup="RestablecerContrasena"
                        Display="Dynamic"
                        CssClass="login-validation"
                        ErrorMessage="<%$ Resources:Textos, UI_DebeConfirmarLaNuevaContrasena %>" />
                </div>

                <asp:Button
                    ID="btnRestablecer"
                    runat="server"
                    Text="<%$ Resources:Textos, UI_RestablecerContrasena %>"
                    ValidationGroup="RestablecerContrasena"
                    CausesValidation="true"
                    OnClick="btnRestablecer_Click"
                    CssClass="btn btn-primary btn-block login-submit" />
            </asp:Panel>

            <nav class="login-links" aria-label="<%: VitaPet.GUI.Resources.Textos.UI_OpcionesDeAcceso %>">
                <asp:HyperLink
                    ID="lnkVolverLogin"
                    runat="server"
                    NavigateUrl="~/Login.aspx"
                    Text="<%$ Resources:Textos, UI_VolverAlInicioDeSesion %>" />
            </nav>
        </section>
    </main>
</asp:Content>
