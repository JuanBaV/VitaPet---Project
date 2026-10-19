<%@ Page Title="<%$ Resources:Textos, Mascota_Historial %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="HistorialClinico.aspx.cs" Inherits="HistorialClinicoPage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card">
  <header class="login-header"><h1><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_Historial") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" Visible="false">
 <asp:Label ID="lblMascota" runat="server" CssClass="mascota-identificacion" />
 <div class="table-responsive"><asp:GridView ID="gvHistorialClinico" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered planes-grilla mascota-clinica"><Columns><asp:BoundField DataField="FechaRegistro" HeaderText="<%$ Resources:Textos, Mascota_FechaRegistro %>" HtmlEncode="true" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
<asp:BoundField DataField="Diagnostico" HeaderText="<%$ Resources:Textos, Mascota_Diagnostico %>" HtmlEncode="true" />
<asp:BoundField DataField="Tratamiento" HeaderText="<%$ Resources:Textos, Mascota_Tratamiento %>" HtmlEncode="true" />
<asp:BoundField DataField="Observaciones" HeaderText="<%$ Resources:Textos, Mascota_Observaciones %>" HtmlEncode="true" />
</Columns></asp:GridView></div></asp:Panel>
 </section></main>
</asp:Content>
