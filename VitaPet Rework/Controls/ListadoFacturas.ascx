<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ListadoFacturas.ascx.cs" Inherits="Controls_ListadoFacturas" %>
<asp:Panel ID="pnlSinFacturas" runat="server" Visible="false" CssClass="planes-nota"><%: VitaPet.GUI.Resources.Textos.Factura_Vacio %></asp:Panel>
<asp:Repeater ID="rptFacturas" runat="server">
    <ItemTemplate>
        <article class="factura-item">
            <h3><%#: Eval("Plan") %></h3>
            <dl class="planes-resumen">
                <dt><%: VitaPet.GUI.Resources.Textos.Factura_Numero %></dt><dd><%#: string.IsNullOrEmpty((string)Eval("NumeroFactura"))?TextosUI.Obtener("Factura_Pendiente"):Eval("NumeroFactura") %></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Pago_Fecha %></dt><dd><%#: Eval("FechaPago","{0:d}") %></dd>
                <dt><%: VitaPet.GUI.Resources.Textos.Pago_Importe %></dt><dd>USD <%#: Eval("Importe","{0:N2}") %></dd>
            </dl>
            <div class="usuarios-botones">
                <asp:HyperLink ID="lnkDescargarFactura" runat="server" Visible='<%# !string.IsNullOrEmpty((string)Eval("NumeroFactura")) %>' NavigateUrl='<%# "~/GestionPago.aspx?contratacion="+Eval("IdSuscripcion")+"&descargar=1" %>' CssClass="btn btn-primary" Text="<%$ Resources:Textos, Factura_Descargar %>" />
                <asp:HyperLink ID="lnkConsultarFactura" runat="server" NavigateUrl='<%# "~/GestionPago.aspx?contratacion="+Eval("IdSuscripcion")+"&comprobante=1"+(Page.AppRelativeVirtualPath=="~/Cliente.aspx" ? "&origen=perfil" : "") %>' CssClass="btn btn-default" Text="<%$ Resources:Textos, Factura_Consultar %>" />
            </div>
        </article>
    </ItemTemplate>
</asp:Repeater>
