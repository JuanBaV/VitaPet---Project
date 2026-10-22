<%@ Page Title="<%$ Resources:Textos, Mascota_ActualizarTitulo %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ActualizarMascota.aspx.cs" Inherits="ActualizarMascotaPage" %>
<%@ Register Assembly="VitaPet.GUI" Namespace="VitaPet.GUI.Presentacion" TagPrefix="vp" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card">
  <a runat="server" href="~/Mascotas.aspx" class="btn btn-default formulario-volver"><span aria-hidden="true">&larr;</span> <%: VitaPet.GUI.Resources.Textos.UI_Volver %></a>
  <header class="login-header"><h1><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_ActualizarTitulo") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" CssClass="mascota-formulario" DefaultButton="btnGuardarCambios"><div class="form-group login-field"><asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_Nombre %>" /><asp:TextBox ID="txtNombre" runat="server" MaxLength="60" CssClass="form-control login-input" /></div>
<div class="form-group login-field"><asp:Label ID="lblEspecie" runat="server" AssociatedControlID="txtEspecie" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_Especie %>" /><asp:TextBox ID="txtEspecie" runat="server" MaxLength="40" CssClass="form-control login-input" /></div>
<div class="form-group login-field"><asp:Label ID="lblRaza" runat="server" AssociatedControlID="txtRaza" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_Raza %>" /><asp:TextBox ID="txtRaza" runat="server" MaxLength="60" CssClass="form-control login-input" /></div>
<div class="form-group login-field"><asp:Label ID="lblFechaNacimiento" runat="server" AssociatedControlID="dtpFechaNacimiento" CssClass="login-label" Text="<%$ Resources:Textos, Mascota_FechaNacimiento %>" /><vp:DateTimePicker ID="dtpFechaNacimiento" runat="server" CssClass="form-control login-input" /></div>
<asp:Button ID="btnGuardarCambios" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Mascota_Guardar %>" OnClick="Guardar" /></asp:Panel>
<asp:Label ID="lblConfirmacion" runat="server" Visible="false" CssClass="login-message registro-confirmacion mascota-confirmacion" role="status" Text="<%$ Resources:Textos, Mascota_Actualizada %>" />
 </section></main>
</asp:Content>
