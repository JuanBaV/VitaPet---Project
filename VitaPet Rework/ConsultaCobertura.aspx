<%@ Page Title="<%$ Resources:Textos, Cobertura_Titulo %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ConsultaCobertura.aspx.cs" Inherits="ConsultaCoberturaPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card planes-detalle">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.Cobertura_Titulo %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <asp:Panel ID="pnlCobertura" runat="server" Visible="false" EnableViewState="false">
            <h2><asp:Label ID="lblNombrePlan" runat="server" /></h2>
            <p class="planes-nota"><asp:Label ID="lblDescripcionPlan" runat="server" /></p>
            <dl class="planes-resumen">
                <dt><%: VitaPet.GUI.Resources.Textos.Cobertura_Estado %></dt><dd><asp:Label ID="lblEstadoSuscripcion" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Cobertura_FechaAlta %></dt><dd><asp:Label ID="lblFechaAlta" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Cobertura_FechaVencimiento %></dt><dd><asp:Label ID="lblFechaVencimiento" runat="server" /></dd>
            </dl>
            <h3><%: VitaPet.GUI.Resources.Textos.Cobertura_Prestaciones %></h3>
            <div class="table-responsive">
                <asp:GridView ID="gvCoberturas" runat="server" AutoGenerateColumns="false" EnableViewState="false" CssClass="table table-bordered table-striped cobertura-grilla" UseAccessibleHeader="true">
                    <Columns>
                        <asp:BoundField DataField="Prestacion" HeaderText="<%$ Resources:Textos, Cobertura_Prestacion %>" HtmlEncode="true" />
                        <asp:BoundField DataField="Detalle" HeaderText="<%$ Resources:Textos, UI_Detalle %>" HtmlEncode="true" />
                    </Columns>
                </asp:GridView>
            </div>
        </asp:Panel>
        <asp:HyperLink ID="lnkVolver" runat="server" NavigateUrl="~/Cliente.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_Volver %>" />
    </section></main>
</asp:Content>
