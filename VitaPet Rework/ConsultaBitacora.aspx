<%@ Page Title="<%$ Resources:Textos, UI_ConsultaDeBitacora %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ConsultaBitacora.aspx.cs" Inherits="ConsultaBitacoraPage" EnableViewStateMac="true" EnableEventValidation="true" %>
<%@ Assembly Name="System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Register TagPrefix="vp" Namespace="VitaPet.GUI.Presentacion" Assembly="VitaPet.GUI" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h1><%: VitaPet.GUI.Resources.Textos.UI_ConsultaDeBitacora %></h1>
    <p><%: VitaPet.GUI.Resources.Textos.UI_ConsultaLosEventosDelSistemaPorFechaY %></p>
    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-warning" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>
    <div class="row">
        <div class="col-sm-4 form-group">
            <asp:Label ID="lblDesde" runat="server" AssociatedControlID="dtpFechaDesde" Text="<%$ Resources:Textos, UI_FechaDesde %>" />
            <vp:DateTimePicker ID="dtpFechaDesde" runat="server" CssClass="form-control" />
        </div>
        <div class="col-sm-4 form-group">
            <asp:Label ID="lblHasta" runat="server" AssociatedControlID="dtpFechaHasta" Text="<%$ Resources:Textos, UI_FechaHasta %>" />
            <vp:DateTimePicker ID="dtpFechaHasta" runat="server" CssClass="form-control" />
        </div>
        <div class="col-sm-4 form-group">
            <asp:Label ID="lblCriticidad" runat="server" AssociatedControlID="ddlCriticidad" Text="<%$ Resources:Textos, UI_Criticidad %>" />
            <asp:DropDownList ID="ddlCriticidad" runat="server" CssClass="form-control">
                <asp:ListItem Value="" Text="<%$ Resources:Textos, UI_Todas %>" />
                <asp:ListItem Value="Informativa" Text="<%$ Resources:Textos, UI_Informativa %>" />
                <asp:ListItem Value="Advertencia" Text="<%$ Resources:Textos, UI_Advertencia %>" />
                <asp:ListItem Value="Grave" Text="<%$ Resources:Textos, UI_Grave %>" />
                <asp:ListItem Value="Crítica" Text="<%$ Resources:Textos, UI_Critica %>" />
            </asp:DropDownList>
        </div>
    </div>
    <div class="form-group">
        <asp:Button ID="btnConsultar" runat="server" Text="<%$ Resources:Textos, UI_Consultar %>" OnClick="Consultar_Click" CssClass="btn btn-primary" CausesValidation="false" />
        <asp:Button ID="btnExportarXml" runat="server" Text="<%$ Resources:Textos, UI_ExportarAXml %>" OnClick="ExportarXml_Click" CssClass="btn btn-default" CausesValidation="false" Enabled="false" />
    </div>
    <p aria-live="polite"><asp:Literal ID="litResultados" runat="server" /></p>
    <div class="table-responsive">
        <asp:GridView ID="gvBitacora" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="20"
            OnPageIndexChanging="Bitacora_PageIndexChanging" CssClass="table table-striped table-bordered bitacora-grilla"
            GridLines="None" EmptyDataText="<%$ Resources:Textos, UI_NoSeEncontraronEventosParaLaConsulta %>" UseAccessibleHeader="true">
            <Columns>
                <asp:BoundField DataField="IdBitacora" HeaderText="<%$ Resources:Textos, UI_Evento %>" HtmlEncode="true" />
                <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Usuario %>"><ItemTemplate><%#: Eval("IdUsuario") == null ? VitaPet.GUI.Resources.Textos.UI_SinUsuarioAsociado : TextosUI.Formato("UsuarioNumero", Eval("IdUsuario")) %></ItemTemplate></asp:TemplateField>
                <asp:BoundField DataField="Fecha" HeaderText="<%$ Resources:Textos, UI_FechaYHora %>" DataFormatString="{0:dd/MM/yyyy HH:mm:ss}" HtmlEncode="true" />
                <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Accion %>"><ItemTemplate><%#: TextosUI.Traducir(Convert.ToString(Eval("Accion"))) %></ItemTemplate></asp:TemplateField>
                <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Modulo %>"><ItemTemplate><%#: TextosUI.Traducir(Convert.ToString(Eval("Modulo"))) %></ItemTemplate></asp:TemplateField>
                <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Criticidad %>"><ItemTemplate><%#: TextosUI.Traducir(Convert.ToString(Eval("Criticidad"))) %></ItemTemplate></asp:TemplateField>
            </Columns>
            <PagerSettings Mode="NumericFirstLast" FirstPageText="<%$ Resources:Textos, UI_Primera %>" LastPageText="<%$ Resources:Textos, UI_Ultima %>" PageButtonCount="5" />
            <PagerStyle CssClass="bitacora-paginacion" />
        </asp:GridView>
    </div>
</asp:Content>
