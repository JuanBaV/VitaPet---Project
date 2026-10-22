<%@ Page Title="<%$ Resources:Textos, Mascota_ConsultarTitulo %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ConsultaMascota.aspx.cs" Inherits="ConsultaMascotaPage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card">
  <a runat="server" href="~/Mascotas.aspx" class="btn btn-default formulario-volver"><span aria-hidden="true">&larr;</span> <%: VitaPet.GUI.Resources.Textos.UI_Volver %></a>
  <header class="login-header"><h1><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_ConsultarTitulo") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" Visible="false"><dl class="mascota-detalle"><dt><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_Nombre") %></dt><dd><asp:Label ID="lblNombre" runat="server" /></dd>
<dt><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_Especie") %></dt><dd><asp:Label ID="lblEspecie" runat="server" /></dd>
<dt><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_Raza") %></dt><dd><asp:Label ID="lblRaza" runat="server" /></dd>
<dt><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_FechaNacimiento") %></dt><dd><asp:Label ID="lblFechaNacimiento" runat="server" /></dd>
<dt><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_Estado") %></dt><dd><asp:Label ID="lblEstado" runat="server" /></dd>
</dl></asp:Panel>
 </section></main>
</asp:Content>
