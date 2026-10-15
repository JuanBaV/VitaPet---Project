<%@ Page Title="<%$ Resources:Textos, UI_ConsultaDeUsuario %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ConsultarUsuario.aspx.cs" Inherits="ConsultarUsuarioPage" EnableViewStateMac="true" EnableEventValidation="true" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<section class="usuarios-page usuario-operacion" aria-labelledby="tituloOperacion">
<h1 id="tituloOperacion"><%: VitaPet.GUI.Resources.Textos.UI_ConsultaDeUsuario %></h1>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-warning" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:HyperLink ID="lnkRecargar" runat="server" Visible="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_VolverACargarLosDatos %>" />
<asp:Panel ID="pnlDatos" runat="server" Visible="false" CssClass="usuario-datos">
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Identificador %></strong><br /><asp:Label ID="lblIdUsuario" runat="server" /></div>
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Nombre %></strong><br /><asp:Label ID="lblNombre" runat="server" /></div>
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_CorreoElectronico %></strong><br /><asp:Label ID="lblCorreo" runat="server" /></div>
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Rol %></strong><br /><asp:Label ID="lblRol" runat="server" /></div>
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Estado %></strong><br /><asp:Label ID="lblEstado" runat="server" /></div>
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Bloqueo %></strong><br /><asp:Label ID="lblBloqueado" runat="server" /></div>
</asp:Panel>
<asp:Button ID="btnVolver" runat="server" Text="<%$ Resources:Textos, UI_Volver %>" OnClick="Volver_Click" CssClass="btn btn-default usuario-volver" CausesValidation="false" />
<asp:HyperLink ID="lnkInicio" runat="server" Visible="false" NavigateUrl="~/Home.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_IrAlInicio %>" />
</section></asp:Content>
