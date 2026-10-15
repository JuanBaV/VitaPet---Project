<%@ Page Title="<%$ Resources:Textos, UI_AccesoDenegado %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeBehind="AccesoDenegado.aspx.cs" Inherits="AccesoDenegadoPage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="status-page">
        <section class="status-card" aria-labelledby="tituloAccesoDenegado">
            <div class="status-icon" aria-hidden="true">!</div>

            <header class="status-header">
                <h1 id="tituloAccesoDenegado"><%: VitaPet.GUI.Resources.Textos.UI_AccesoDenegado %></h1>
                <p><%: VitaPet.GUI.Resources.Textos.UI_NoTenesPermisosParaAccederAEstaPagina %></p>
                <p><%: VitaPet.GUI.Resources.Textos.UI_VolveAlInicioParaUtilizarLasFuncionesHabilitadas %></p>
            </header>

            <asp:HyperLink
                ID="lnkVolverInicio"
                runat="server"
                NavigateUrl="~/Home.aspx"
                CssClass="btn btn-primary status-action"
                Text="<%$ Resources:Textos, UI_VolverAlInicio %>" />
        </section>
    </main>
</asp:Content>
