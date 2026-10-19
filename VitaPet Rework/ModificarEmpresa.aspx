<%@ Page Title="<%$ Resources:Textos, Empresa_Modificar %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ModificarEmpresa.aspx.cs" Inherits="ModificarEmpresaPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card empresa-card">
  <header class="login-header"><h1><%: TextosUI.Obtener("Empresa_Modificar") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
  <asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnGuardarCambios">
   <div class="empresa-campos"><div class="empresa-dato"><span class="login-label"><%: TextosUI.Obtener("Empresa_Id") %></span><asp:Label ID="lblIdEmpresa" runat="server" /></div><div class="empresa-dato"><span class="login-label"><%: TextosUI.Obtener("Empresa_Estado") %></span><asp:Label ID="lblEstado" runat="server" /></div><div class="form-group login-field"><asp:Label ID="labeltxtRazonSocial" runat="server" AssociatedControlID="txtRazonSocial" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_RazonSocial %>" /><asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="100" CssClass="form-control login-input" /></div>
<div class="form-group login-field"><asp:Label ID="labeltxtCuit" runat="server" AssociatedControlID="txtCuit" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_Cuit %>" /><asp:TextBox ID="txtCuit" runat="server" MaxLength="20" CssClass="form-control login-input" /></div>
<div class="form-group login-field"><asp:Label ID="labeltxtDireccion" runat="server" AssociatedControlID="txtDireccion" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_Direccion %>" /><asp:TextBox ID="txtDireccion" runat="server" MaxLength="150" CssClass="form-control login-input" /></div>
<div class="form-group login-field"><asp:Label ID="labeltxtTelefono" runat="server" AssociatedControlID="txtTelefono" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_Telefono %>" /><asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" CssClass="form-control login-input" /></div></div>
   <div class="empresa-acciones"><asp:Button ID="btnGuardarCambios" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_Guardar %>" OnClick="Guardar" /></div>
  </asp:Panel>
 </section></main>
</asp:Content>
