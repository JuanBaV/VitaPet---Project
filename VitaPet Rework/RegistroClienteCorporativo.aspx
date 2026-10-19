<%@ Page Title="<%$ Resources:Textos, Corporativo_Titulo %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistroClienteCorporativo.aspx.cs" Inherits="RegistroClienteCorporativoPage" %>
<%@ Register Src="~/Controls/ValidacionEmail.ascx" TagPrefix="vp" TagName="ValidacionEmail" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card empresa-card">
  <header class="login-header"><h1><%: TextosUI.Obtener("Corporativo_Titulo") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
  <asp:Panel ID="pnlEmpresas" runat="server"><div class="table-responsive">
   <asp:GridView ID="gvEmpresas" runat="server" AutoGenerateColumns="false" DataKeyNames="IdEmpresa" CssClass="table table-bordered planes-grilla empresa-grilla" OnSelectedIndexChanged="Seleccionar" aria-label="Empresas">
    <Columns><asp:BoundField DataField="IdEmpresa" HtmlEncode="true" HeaderText="<%$ Resources:Textos, Empresa_Id %>" />
     <asp:BoundField DataField="RazonSocial" HtmlEncode="true" HeaderText="<%$ Resources:Textos, Empresa_RazonSocial %>" />
     <asp:BoundField DataField="Cuit" HtmlEncode="true" HeaderText="<%$ Resources:Textos, Empresa_Cuit %>" />
     <asp:TemplateField HeaderText="<%$ Resources:Textos, Empresa_Estado %>"><ItemTemplate><%#: TextosUI.Obtener((bool)Eval("Estado") ? "Empresa_Activo" : "Empresa_Inactivo") %></ItemTemplate></asp:TemplateField>
     <asp:TemplateField><ItemTemplate><asp:LinkButton ID="lnkSeleccionarEmpresa" runat="server" CommandName="Select" CausesValidation="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_Seleccionar %>" /></ItemTemplate></asp:TemplateField>
    </Columns>
   </asp:GridView></div></asp:Panel>
  <asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="corporativo-form">
   <dl class="empresa-resumen"><div><dt><%: TextosUI.Obtener("Corporativo_Empresa") %></dt><dd><asp:Label ID="lblEmpresa" runat="server" /></dd></div>
    <div><dt><%: TextosUI.Obtener("Corporativo_Plan") %></dt><dd><asp:Label ID="lblPlanCorporativo" runat="server" Text="<%$ Resources:Textos, Corporativo_NombrePlan %>" /></dd></div></dl>
   <asp:Label ID="lblCondicionesPlan" runat="server" CssClass="corporativo-condiciones" Text="<%$ Resources:Textos, Corporativo_Condiciones %>" />
   <div class="corporativo-correo"><vp:ValidacionEmail ID="ucCorreo" runat="server" MaxLength="100" ValidationGroup="Corporativo" EnableClientValidation="false" />
    <asp:Button ID="btnValidarCorreo" runat="server" CssClass="btn btn-primary" CausesValidation="false" formnovalidate="formnovalidate" Text="<%$ Resources:Textos, Corporativo_ValidarCorreo %>" OnClick="ValidarCorreo" /></div>
   <asp:Panel ID="pnlDatos" runat="server" CssClass="empresa-campos" Enabled="false">
    <div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtNombre" CssClass="login-label" Text="<%$ Resources:Textos, UI_Nombre %>" /><asp:TextBox ID="txtNombre" runat="server" MaxLength="60" CssClass="form-control login-input" autocomplete="given-name" /></div>
    <div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtApellido" CssClass="login-label" Text="<%$ Resources:Textos, Registro_Apellido %>" /><asp:TextBox ID="txtApellido" runat="server" MaxLength="60" CssClass="form-control login-input" autocomplete="family-name" /></div>
    <div class="form-group login-field"><asp:Label runat="server" AssociatedControlID="txtTelefono" CssClass="login-label" Text="<%$ Resources:Textos, UI_Telefono %>" /><asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" CssClass="form-control login-input" TextMode="Phone" autocomplete="tel" /></div>
   </asp:Panel>
   <div class="empresa-acciones"><asp:Button ID="btnRegistrarCliente" runat="server" Enabled="false" CssClass="btn btn-primary" CausesValidation="false" Text="<%$ Resources:Textos, Corporativo_Registrar %>" OnClick="Registrar" /></div>
   <asp:Label ID="lblConfirmacion" runat="server" Visible="false" CssClass="login-message empresa-confirmacion" role="status" />
  </asp:Panel>
 </section></main>
 <script src="<%= ResolveUrl("~/Scripts/cliente-corporativo.js") %>"></script>
</asp:Content>
