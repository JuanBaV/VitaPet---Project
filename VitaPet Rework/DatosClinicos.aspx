<%@ Page Title="<%$ Resources:Textos, Mascota_DatosClinicos %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DatosClinicos.aspx.cs" Inherits="DatosClinicosPage" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card">
  <header class="login-header"><h1><%: VitaPet.GUI.Presentacion.TextosUI.Obtener("Mascota_DatosClinicos") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDatos" runat="server" Visible="false">
 <asp:Label ID="lblMascota" runat="server" CssClass="mascota-identificacion" />
 <div class="table-responsive"><asp:GridView ID="gvDatosClinicos" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered planes-grilla mascota-clinica"><Columns><asp:BoundField DataField="Peso" HeaderText="<%$ Resources:Textos, Mascota_Peso %>" HtmlEncode="true" DataFormatString="{0:N2}" />
<asp:BoundField DataField="Temperatura" HeaderText="<%$ Resources:Textos, Mascota_Temperatura %>" HtmlEncode="true" DataFormatString="{0:N2}" />
<asp:BoundField DataField="FrecuenciaCardiaca" HeaderText="<%$ Resources:Textos, Mascota_FrecuenciaCardiaca %>" HtmlEncode="true" />
<asp:BoundField DataField="Observaciones" HeaderText="<%$ Resources:Textos, Mascota_Observaciones %>" HtmlEncode="true" />
</Columns></asp:GridView></div></asp:Panel>
 </section></main>
</asp:Content>
