<%@ Page Title="<%$ Resources:Textos, UI_Mascotas %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Mascotas.aspx.cs" Inherits="MascotasPage" %>
<%@ Register Src="~/Controls/SelectorMascota.ascx" TagPrefix="uc" TagName="SelectorMascota" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card">
  <header class="login-header"><h1><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("UI_Mascotas") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<%-- implementacion del componente SelectorMascota.ascx --%><uc:SelectorMascota ID="ucSelectorMascota" runat="server" OnMascotaSeleccionada="Seleccionar" />
<div class="mascota-acciones"><asp:Button ID="btnActualizarDatos" runat="server" Enabled="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, Mascota_Actualizar %>" OnClick="Actualizar" />
<asp:Button ID="btnConsultarDatos" runat="server" Enabled="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, Mascota_Consultar %>" OnClick="Consultar" />
<asp:Button ID="btnHistorialClinico" runat="server" Enabled="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, Mascota_Historial %>" OnClick="Historial" />
<asp:Button ID="btnDatosClinicos" runat="server" Enabled="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, Mascota_DatosClinicos %>" OnClick="Clinicos" />
</div>
<asp:HyperLink ID="lnkRegistrarMascota" runat="server" NavigateUrl="~/RegistroMascota.aspx" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Mascota_Registrar %>" />
 </section></main>
</asp:Content>
