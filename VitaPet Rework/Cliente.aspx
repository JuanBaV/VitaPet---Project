<%@ Page Title="<%$ Resources:Textos, UI_MiPerfil %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Cliente.aspx.cs" Inherits="ClientePage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.UI_MiPerfil %></h1></header>
        <p><asp:HyperLink ID="lnkConsultarCobertura" runat="server" NavigateUrl="~/ConsultaCobertura.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, Cobertura_Consultar %>" /></p>
    </section></main>
</asp:Content>
