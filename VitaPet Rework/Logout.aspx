<%@ Page Title="<%$ Resources:Textos, UI_CerrarSesion %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Logout.aspx.cs" Inherits="LogoutPage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="login-page">
        <section class="login-card" aria-labelledby="tituloLogout">
            <header class="login-header">
                <h1 id="tituloLogout"><%: VitaPet.GUI.Resources.Textos.UI_CerrarSesion %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.UI_DeseaCerrarSuSesionEnVitapet %></p>
            </header>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false"
                CssClass="login-message" role="alert">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Button
                ID="btnConfirmarLogout"
                runat="server"
                Text="<%$ Resources:Textos, UI_CerrarSesion %>"
                CausesValidation="false"
                OnClick="btnConfirmarLogout_Click"
                CssClass="btn btn-primary btn-block login-submit" />

            <nav class="login-links" aria-label="<%: VitaPet.GUI.Resources.Textos.UI_OpcionesDeCierreDeSesion %>">
                <asp:HyperLink
                    ID="lnkCancelar"
                    runat="server"
                    NavigateUrl="~/Home.aspx"
                    Text="<%$ Resources:Textos, UI_Cancelar %>" />
            </nav>
        </section>
    </main>
</asp:Content>
