<%@ Page Title="<%$ Resources:Textos, UI_Inicio %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="HomePage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="home-page" aria-labelledby="home-titulo">
        <img class="home-foto" src="<%: ResolveUrl("~/Content/Images/HomeMascotas.png") %>" alt="<%: VitaPet.GUI.Resources.Textos.Home_FotoDescripcion %>" width="1672" height="941" fetchpriority="high" />
        <div class="home-contenido">
            <section class="home-presentacion">
                <asp:Panel ID="pnlSaludo" runat="server" CssClass="home-saludo" Visible="false"><asp:Literal ID="litSaludo" runat="server" /></asp:Panel>
                <h1 id="home-titulo"><%: VitaPet.GUI.Resources.Textos.Home_Titulo %></h1>
                <p class="home-bajada"><%: VitaPet.GUI.Resources.Textos.Home_Bajada %></p>
                <p><%: VitaPet.GUI.Resources.Textos.Home_Descripcion %></p>
                <p><%: VitaPet.GUI.Resources.Textos.Home_Gestiones %></p>
            </section>
        </div>
    </main>
</asp:Content>
