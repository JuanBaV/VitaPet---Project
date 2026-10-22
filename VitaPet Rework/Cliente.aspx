<%@ Page Title="<%$ Resources:Textos, UI_MiPerfil %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Cliente.aspx.cs" Inherits="ClientePage" %>
<%@ Register Src="~/Controls/ListadoFacturas.ascx" TagPrefix="vp" TagName="ListadoFacturas" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.UI_MiPerfil %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <p><asp:HyperLink ID="lnkConsultarCobertura" runat="server" NavigateUrl="~/ConsultaCobertura.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, Cobertura_Consultar %>" /></p>
        <section class="facturas-seccion" aria-labelledby="tituloFacturas">
            <h2 id="tituloFacturas"><%: VitaPet.GUI.Resources.Textos.Factura_MisFacturas %></h2>
            <vp:ListadoFacturas ID="ucFacturas" runat="server" />
        </section>
    </section></main>
</asp:Content>
