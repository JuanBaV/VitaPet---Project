<%@ Page Title="<%$ Resources:Textos, UI_Turnos %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Turnos.aspx.cs" Inherits="TurnosPage" %>
<%@ Register Src="~/Controls/SelectorMascota.ascx" TagPrefix="uc" TagName="SelectorMascota" %>
<%@ Register Src="~/Controls/FiltroZona.ascx" TagPrefix="uc" TagName="FiltroZona" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card turnos-card">
  <nav class="turnos-vistas" aria-label="<%: VitaPet.GUI.Presentacion.TextosUI.Obtener("UI_Turnos") %>">
   <asp:HyperLink ID="lnkSolicitar" runat="server" NavigateUrl="~/Turnos.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, Turno_Solicitar %>" />
   <asp:HyperLink ID="lnkConsultar" runat="server" NavigateUrl="~/Turnos.aspx?vista=consulta" CssClass="btn btn-default" Text="<%$ Resources:Textos, Turno_Consultar %>" />
  </nav>
  <header class="login-header"><h1><asp:Literal ID="litTitulo" runat="server" /></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
  <h2 class="turnos-subtitulo"><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Turno_Mascota") %></h2>
  <uc:SelectorMascota ID="ucSelectorMascota" runat="server" OnMascotaSeleccionada="SeleccionarMascota" />
  <asp:Panel ID="pnlSolicitud" runat="server">
   <div class="turnos-filtros"><div class="turnos-zona"><uc:FiltroZona ID="ucFiltroZona" runat="server" OnZonaCambiada="CambiarZona" /></div>
    <asp:Button ID="btnBuscarDisponibilidad" runat="server" Enabled="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Turno_Buscar %>" OnClick="BuscarDisponibilidad" />
   </div>
   <div class="table-responsive"><asp:GridView ID="gvDisponibilidad" runat="server" AutoGenerateColumns="false" DataKeyNames="IdAgendaTurno,Fecha,Horario" CssClass="table table-bordered planes-grilla" AllowPaging="true" PageSize="12" OnPageIndexChanging="PaginarDisponibilidad" OnSelectedIndexChanged="SeleccionarTurno">
    <SelectedRowStyle CssClass="mascota-seleccionada" /><Columns>
     <asp:BoundField DataField="Prestacion" HeaderText="<%$ Resources:Textos, Turno_Prestacion %>" HtmlEncode="true" />
     <asp:BoundField DataField="Fecha" HeaderText="<%$ Resources:Textos, Turno_Fecha %>" DataFormatString="{0:dd/MM/yyyy}" HtmlEncode="true" />
     <asp:BoundField DataField="Horario" HeaderText="<%$ Resources:Textos, Turno_Horario %>" HtmlEncode="true" />
     <asp:BoundField DataField="Zona" HeaderText="<%$ Resources:Textos, Turno_Zona %>" HtmlEncode="true" />
     <asp:TemplateField><ItemTemplate><asp:LinkButton ID="lnkSeleccionarTurno" runat="server" CommandName="Select" CausesValidation="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Mascota_Seleccionar %>" /></ItemTemplate></asp:TemplateField>
    </Columns><PagerStyle CssClass="turnos-paginador" /></asp:GridView></div>
   <div class="turnos-acciones">
    <asp:Label ID="lblTurnoSeleccionado" runat="server" Visible="false" CssClass="turnos-seleccion" />
    <asp:Button ID="btnConfirmarSolicitud" runat="server" Enabled="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Turno_Confirmar %>" OnClick="ConfirmarSolicitud" />
   </div>
   <asp:Label ID="lblConfirmacion" runat="server" Visible="false" CssClass="login-message registro-confirmacion" role="status" />
  </asp:Panel>
  <asp:Panel ID="pnlConsulta" runat="server" Visible="false">
   <div class="table-responsive"><asp:GridView ID="gvTurnos" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered planes-grilla"><Columns>
    <asp:BoundField DataField="IdTurno" HeaderText="<%$ Resources:Textos, Turno_Numero %>" HtmlEncode="true" />
    <asp:BoundField DataField="Prestacion" HeaderText="<%$ Resources:Textos, Turno_Prestacion %>" HtmlEncode="true" />
    <asp:BoundField DataField="Fecha" HeaderText="<%$ Resources:Textos, Turno_Fecha %>" DataFormatString="{0:dd/MM/yyyy}" HtmlEncode="true" />
    <asp:BoundField DataField="Horario" HeaderText="<%$ Resources:Textos, Turno_Horario %>" HtmlEncode="true" />
    <asp:BoundField DataField="Zona" HeaderText="<%$ Resources:Textos, Turno_Zona %>" HtmlEncode="true" />
    <asp:TemplateField HeaderText="<%$ Resources:Textos, Turno_Estado %>"><ItemTemplate><%#: (string)Eval("Estado")=="Confirmado" ? VitaPet.GUI.Presentacion.TextosUI.Obtener("Turno_Confirmado") : VitaPet.GUI.Presentacion.TextosUI.Traducir((string)Eval("Estado")) %></ItemTemplate></asp:TemplateField>
   </Columns></asp:GridView></div>
   <asp:Button ID="btnExportarXml" runat="server" Enabled="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Turno_Exportar %>" OnClick="ExportarXml" />
  </asp:Panel>
 </section></main>
</asp:Content>
