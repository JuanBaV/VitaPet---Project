<%@ Page Title="<%$ Resources:Textos, Planes_GestionPago %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GestionPago.aspx.cs" Inherits="GestionPagoPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="planes-page"><section class="planes-card planes-detalle pago-card">
        <header class="login-header"><h1><%: VitaPet.GUI.Resources.Textos.Planes_GestionPago %></h1></header>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
        <asp:Panel ID="pnlPendiente" runat="server" Visible="false" DefaultButton="btnPagar">
            <h2><asp:Label ID="lblNombrePlan" runat="server" /></h2>
            <dl class="planes-resumen"><dt><%: VitaPet.GUI.Resources.Textos.Pago_PrecioMascota %></dt><dd><asp:Label ID="lblPrecioMascota" runat="server" /></dd>
<dt><%: VitaPet.GUI.Resources.Textos.Planes_MascotasActivas %></dt><dd><asp:Label ID="lblCantidadMascotas" runat="server" /></dd>
<dt><%: VitaPet.GUI.Resources.Textos.Planes_TotalMensual %></dt><dd><asp:Label ID="lblImporte" runat="server" /></dd></dl>
            <div class="pago-campos">
                <div class="form-group login-field pago-ancho">
                    <asp:Label runat="server" AssociatedControlID="ddlMedioPago" CssClass="login-label" Text="<%$ Resources:Textos, Pago_Medio %>" />
                    <asp:DropDownList ID="ddlMedioPago" runat="server" CssClass="form-control login-input">
                        <asp:ListItem Value="" Text="<%$ Resources:Textos, Pago_Seleccionar %>" />
                        <asp:ListItem Value="Tarjeta de credito" Text="<%$ Resources:Textos, Pago_Credito %>" />
                        <asp:ListItem Value="Tarjeta de debito" Text="<%$ Resources:Textos, Pago_Debito %>" />
                    </asp:DropDownList>
                </div><div class="form-group login-field pago-ancho">
 <asp:Label runat="server" AssociatedControlID="txtTitular" CssClass="login-label" Text="<%$ Resources:Textos, Pago_Titular %>" />
 <asp:TextBox ID="txtTitular" runat="server" EnableViewState="false" MaxLength="100" CssClass="form-control login-input" autocomplete="off"  />
 </div>
<div class="form-group login-field pago-ancho">
 <asp:Label runat="server" AssociatedControlID="txtNumeroTarjeta" CssClass="login-label" Text="<%$ Resources:Textos, Pago_NumeroTarjeta %>" />
 <asp:TextBox ID="txtNumeroTarjeta" runat="server" EnableViewState="false" MaxLength="25" CssClass="form-control login-input" autocomplete="off" inputmode="numeric" />
 </div>
<div class="form-group login-field ">
 <asp:Label runat="server" AssociatedControlID="txtVencimiento" CssClass="login-label" Text="<%$ Resources:Textos, Pago_Vencimiento %>" />
 <asp:TextBox ID="txtVencimiento" runat="server" EnableViewState="false" MaxLength="7" CssClass="form-control login-input" autocomplete="off" placeholder="MM/AAAA" />
 </div>
<div class="form-group login-field ">
 <asp:Label runat="server" AssociatedControlID="txtCodigoSeguridad" CssClass="login-label" Text="<%$ Resources:Textos, Pago_Codigo %>" />
 <asp:TextBox ID="txtCodigoSeguridad" runat="server" EnableViewState="false" MaxLength="4" CssClass="form-control login-input" autocomplete="off" TextMode="Password" inputmode="numeric" />
 </div>

            </div>
            <div class="usuarios-botones">
                <asp:Button ID="btnPagar" runat="server" Text="<%$ Resources:Textos, Pago_Pagar %>" CausesValidation="false" OnClick="Pagar_Click" CssClass="btn btn-primary" OnClientClick="if(this.dataset.enviado==='1')return false;this.dataset.enviado='1';this.setAttribute('aria-disabled','true');this.classList.add('pago-enviado');" />
                <asp:HyperLink ID="lnkVolver" runat="server" NavigateUrl="~/Planes.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_Volver %>" />
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlComprobante" runat="server" Visible="false">
            <h2><%: VitaPet.GUI.Resources.Textos.Pago_Confirmado %></h2>
            <dl class="planes-resumen"><dt><%: VitaPet.GUI.Resources.Textos.Pago_Numero %></dt><dd><asp:Label ID="lblNumeroPago" runat="server" /></dd>
<dt><%: VitaPet.GUI.Resources.Textos.Pago_Plan %></dt><dd><asp:Label ID="lblPlanPagado" runat="server" /></dd>
<dt><%: VitaPet.GUI.Resources.Textos.Pago_Importe %></dt><dd><asp:Label ID="lblImportePagado" runat="server" /></dd>
<dt><%: VitaPet.GUI.Resources.Textos.Pago_Medio %></dt><dd><asp:Label ID="lblMedioPagado" runat="server" /></dd>
<dt><%: VitaPet.GUI.Resources.Textos.Pago_Fecha %></dt><dd><asp:Label ID="lblFechaPago" runat="server" /></dd></dl>
            <asp:Button ID="btnContinuar" runat="server" Text="<%$ Resources:Textos, Pago_Continuar %>" CausesValidation="false" OnClick="Continuar_Click" CssClass="btn btn-primary" />
        </asp:Panel>
    </section></main>
    <asp:Panel ID="pnlDummy" runat="server" Visible="false" EnableViewState="false">
        <button id="btnCompletarDummy" type="button" class="pago-dummy"><%: VitaPet.GUI.Resources.Textos.Pago_Dummy %></button>
        <script>
            document.getElementById('btnCompletarDummy').addEventListener('click', function () {
                document.getElementById('<%= ddlMedioPago.ClientID %>').value = 'Tarjeta de credito';
                document.getElementById('<%= txtTitular.ClientID %>').value = 'Prueba Prueba';
                document.getElementById('<%= txtNumeroTarjeta.ClientID %>').value = '1111111111111111';
                document.getElementById('<%= txtVencimiento.ClientID %>').value = '12/' + (new Date().getFullYear() + 2);
                document.getElementById('<%= txtCodigoSeguridad.ClientID %>').value = '111';
            });
        </script>
    </asp:Panel>
</asp:Content>
