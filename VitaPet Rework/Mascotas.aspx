<%@ Page Title="<%$ Resources:Textos, UI_Mascotas %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Mascotas.aspx.cs" Inherits="MascotasPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.UI_Mascotas %></h1></header>
        <asp:HyperLink ID="lnkRegistrarMascota" runat="server" NavigateUrl="~/RegistroMascota.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, Mascota_Registrar %>" />
    </section></main>
</asp:Content>
