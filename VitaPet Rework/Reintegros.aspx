<%@ Page Title="<%$ Resources:Textos, Reintegro_Titulo %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Reintegros.aspx.cs" Inherits="ReintegrosPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.Reintegro_Titulo %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <h2><%: VitaPet.GUI.Resources.Textos.Reintegro_Listado %></h2>
        <div class="table-responsive">
            <asp:GridView ID="gvReintegros" runat="server" AutoGenerateColumns="false" DataKeyNames="IdReintegro" CssClass="table table-striped" GridLines="None" OnRowCommand="ConsultarReintegro">
                <Columns>
                    <asp:BoundField DataField="IdReintegro" HeaderText="<%$ Resources:Textos, Reintegro_Numero %>" />
                    <asp:BoundField DataField="FechaSolicitud" HeaderText="<%$ Resources:Textos, Reintegro_Fecha %>" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="Importe" HeaderText="<%$ Resources:Textos, Reintegro_Importe %>" DataFormatString="{0:N2}" />
                    <asp:TemplateField HeaderText="<%$ Resources:Textos, Reintegro_Estado %>"><ItemTemplate><%#: TextosUI.Traducir((string)Eval("Estado")) %></ItemTemplate></asp:TemplateField>
                    <asp:TemplateField><ItemTemplate><asp:LinkButton ID="btnConsultar" runat="server" Text="<%$ Resources:Textos, Reintegro_Consultar %>" CommandName="Consultar" CommandArgument='<%# ((System.Web.UI.WebControls.GridViewRow)Container).RowIndex %>' CausesValidation="false" /></ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="plan-resumen">
            <h2><%: VitaPet.GUI.Resources.Textos.Reintegro_Seleccionado %></h2>
            <dl class="plan-datos">
                <dt><%: VitaPet.GUI.Resources.Textos.Reintegro_Numero %></dt><dd><asp:Label ID="lblNumeroReintegro" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Reintegro_Fecha %></dt><dd><asp:Label ID="lblFechaSolicitud" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Reintegro_Importe %></dt><dd><asp:Label ID="lblImporte" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Reintegro_Estado %></dt><dd><asp:Label ID="lblEstado" runat="server" /></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Reintegro_Detalle %></dt><dd class="reintegro-detalle"><asp:Label ID="lblDetalle" runat="server" /></dd>
            </dl>
        </asp:Panel>
        <asp:Button ID="btnExportarXml" runat="server" Text="<%$ Resources:Textos, Reintegro_Exportar %>" Enabled="false" CssClass="btn btn-default" OnClick="ExportarXml" CausesValidation="false" />
    </section></main>
</asp:Content>
