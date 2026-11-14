<%@ Page Title="<%$ Resources:Textos, UI_Planes %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Planes.aspx.cs" Inherits="PlanesPage" %>
<%@ Register Src="~/Controls/ListadoPlanes.ascx" TagPrefix="vp" TagName="ListadoPlanes" %>
<%@ Register Src="~/Controls/ListadoFacturas.ascx" TagPrefix="vp" TagName="ListadoFacturas" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.Planes_Titulo %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <section class="facturas-seccion" aria-labelledby="tituloFacturas">
            <h2 id="tituloFacturas"><%: VitaPet.GUI.Resources.Textos.Factura_MisFacturas %></h2>
            <%-- implementacion del componente ListadoFacturas.ascx --%><vp:ListadoFacturas ID="ucFacturas" runat="server" />
        </section>
        <%-- implementacion del componente ListadoPlanes.ascx --%><vp:ListadoPlanes ID="ucPlanes" runat="server" OnPlanSeleccionado="PlanSeleccionado" />
    </section></main>
</asp:Content>
