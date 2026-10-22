<%@ Page Title="<%$ Resources:Textos, Empresa_Alta %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AltaEmpresa.aspx.cs" Inherits="AltaEmpresaPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card empresa-card">
  <a runat="server" href="~/Empresas.aspx" class="btn btn-default formulario-volver"><span aria-hidden="true">&larr;</span> <%: VitaPet.GUI.Resources.Textos.UI_Volver %></a>
  <header class="login-header"><h1><%: TextosUI.Obtener("Empresa_Alta") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
  <asp:Panel ID="pnlDatos" runat="server" DefaultButton="btnRegistrarEmpresa">
<asp:ValidationSummary ID="vsAltaEmpresa" runat="server" ValidationGroup="AltaEmpresa" CssClass="login-validation-summary" HeaderText="<%$ Resources:Textos, UI_RevisaLosSiguientesDatos %>" DisplayMode="BulletList" role="alert" />
   <div class="empresa-campos"><div class="form-group login-field"><asp:Label ID="labeltxtRazonSocial" runat="server" AssociatedControlID="txtRazonSocial" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_RazonSocial %>" /><asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="100" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvRazonSocial" runat="server" ControlToValidate="txtRazonSocial" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revRazonSocial" runat="server" ControlToValidate="txtRazonSocial" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,100}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
<div class="form-group login-field"><asp:Label ID="labeltxtCuit" runat="server" AssociatedControlID="txtCuit" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_Cuit %>" /><asp:TextBox ID="txtCuit" runat="server" MaxLength="20" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvCuit" runat="server" ControlToValidate="txtCuit" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revCuit" runat="server" ControlToValidate="txtCuit" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ValidationExpression="^(?:[0-9]{11}|[0-9]{2}-[0-9]{8}-[0-9])$" ErrorMessage="<%$ Resources:Textos, Alta_CuitInvalido %>" /></div>
<div class="form-group login-field"><asp:Label ID="labeltxtDireccion" runat="server" AssociatedControlID="txtDireccion" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_Direccion %>" /><asp:TextBox ID="txtDireccion" runat="server" MaxLength="150" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvDireccion" runat="server" ControlToValidate="txtDireccion" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revDireccion" runat="server" ControlToValidate="txtDireccion" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[^\x00-\x1F\x7F]{1,150}$" ErrorMessage="<%$ Resources:Textos, Alta_TextoInvalido %>" /></div>
<div class="form-group login-field"><asp:Label ID="labeltxtTelefono" runat="server" AssociatedControlID="txtTelefono" CssClass="login-label" Text="<%$ Resources:Textos, Empresa_Telefono %>" /><asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" CssClass="form-control login-input" />
<asp:RequiredFieldValidator ID="rfvTelefono" runat="server" ControlToValidate="txtTelefono" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
<asp:RegularExpressionValidator ID="revTelefono" runat="server" ControlToValidate="txtTelefono" ValidationGroup="AltaEmpresa" Display="Dynamic" CssClass="login-validation" ValidationExpression="^(?=.{1,30}$)(?=.*[0-9])[0-9 +()\-]+$" ErrorMessage="<%$ Resources:Textos, Alta_TelefonoInvalido %>" /></div></div>
   <asp:CustomValidator ID="cvDatosEmpresa" runat="server" ValidationGroup="AltaEmpresa" ValidateEmptyText="true" EnableClientScript="false" OnServerValidate="ValidarDatos" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_DatosInvalidos %>" />
<div class="empresa-acciones"><asp:Button ID="btnRegistrarEmpresa" runat="server" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_Registrar %>" CausesValidation="true" ValidationGroup="AltaEmpresa" OnClick="Registrar" /></div>
  </asp:Panel>
  <asp:Label ID="lblConfirmacion" runat="server" Visible="false" CssClass="login-message registro-confirmacion empresa-confirmacion" role="status" />
 </section></main>
</asp:Content>
