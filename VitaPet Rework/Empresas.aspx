<%@ Page Title="<%$ Resources:Textos, Empresa_Gestion %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Empresas.aspx.cs" Inherits="EmpresasPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
 <main class="planes-page"><section class="planes-card empresa-card">
  <header class="login-header"><h1><%: TextosUI.Obtener("Empresa_Gestion") %></h1></header>
  <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="login-message registro-error" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
  <div class="table-responsive"><asp:GridView ID="gvEmpresas" runat="server" AutoGenerateColumns="false" DataKeyNames="IdEmpresa" CssClass="table table-bordered planes-grilla empresa-grilla" OnSelectedIndexChanged="Seleccionar">
   <SelectedRowStyle CssClass="empresa-seleccionada" /><Columns><asp:BoundField DataField="IdEmpresa" HeaderText="<%$ Resources:Textos, Empresa_Id %>" HtmlEncode="true" /><asp:BoundField DataField="RazonSocial" HeaderText="<%$ Resources:Textos, Empresa_RazonSocial %>" HtmlEncode="true" /><asp:BoundField DataField="Cuit" HeaderText="<%$ Resources:Textos, Empresa_Cuit %>" HtmlEncode="true" /><asp:BoundField DataField="Direccion" HeaderText="<%$ Resources:Textos, Empresa_Direccion %>" HtmlEncode="true" /><asp:BoundField DataField="Telefono" HeaderText="<%$ Resources:Textos, Empresa_Telefono %>" HtmlEncode="true" />
    <asp:TemplateField HeaderText="<%$ Resources:Textos, Empresa_Estado %>"><ItemTemplate><%#: TextosUI.Obtener((bool)Eval("Estado") ? "Empresa_Activo" : "Empresa_Inactivo") %></ItemTemplate></asp:TemplateField>
    <asp:TemplateField><ItemTemplate><asp:LinkButton ID="lnkSeleccionarEmpresa" runat="server" CommandName="Select" CausesValidation="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_Seleccionar %>" /></ItemTemplate></asp:TemplateField>
   </Columns></asp:GridView></div>
  <div class="empresa-acciones"><asp:Button ID="btnConsultar" runat="server" Enabled="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_Consultar %>" OnClick="Consultar" />
   <asp:Button ID="btnModificar" runat="server" Enabled="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Empresa_BotonModificar %>" OnClick="Modificar" /></div>
 </section></main>
</asp:Content>
