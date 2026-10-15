<%@ Page Title="<%$ Resources:Textos, Planes_Contratar %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContratarPlan.aspx.cs" Inherits="ContratarPlanPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card planes-detalle">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.Planes_Contratar %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
            <h2><asp:Label ID="lblNombrePlan" runat="server" /></h2>
            <p><asp:Label ID="lblDescripcionPlan" runat="server" /></p>
            <dl class="planes-resumen">
                <dt><%: VitaPet.GUI.Resources.Textos.Planes_PrecioMascota %></dt><dd><asp:Label ID="lblPrecioPlan" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Planes_MascotasActivas %></dt><dd><asp:Label ID="lblCantidadMascotas" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Planes_TotalMensual %></dt><dd class="planes-total"><asp:Label ID="lblTotalMensual" runat="server" /></dd>
            </dl>
            <h3><%: VitaPet.GUI.Resources.Textos.Planes_Coberturas %></h3>
            <asp:GridView ID="gvCoberturas" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered" EmptyDataText="<%$ Resources:Textos, Planes_CoberturaVacia %>">
                <Columns><asp:BoundField DataField="Prestacion" HeaderText="<%$ Resources:Textos, Planes_Prestacion %>" HtmlEncode="true"/><asp:BoundField DataField="Detalle" HeaderText="<%$ Resources:Textos, Planes_Descripcion %>" HtmlEncode="true"/></Columns>
            </asp:GridView>
            <asp:Label ID="lblErrorCobertura" runat="server" CssClass="text-danger" role="alert" />
            <p class="planes-nota"><%: VitaPet.GUI.Resources.Textos.Planes_PendienteAviso %></p>
            <asp:Button ID="btnContratar" runat="server" Text="<%$ Resources:Textos, Planes_Contratar %>" CausesValidation="false" OnClick="Contratar_Click" CssClass="btn btn-primary login-submit" />
        </asp:Panel>
        <asp:HyperLink ID="btnVolver" runat="server" NavigateUrl="~/Planes.aspx" CssClass="btn btn-default usuario-volver" Text="<%$ Resources:Textos, UI_Volver %>" />
    </section></main>
</asp:Content>
