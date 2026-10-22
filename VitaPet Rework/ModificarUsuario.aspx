<%@ Page Title="<%$ Resources:Textos, UI_ModificacionDeUsuario %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ModificarUsuario.aspx.cs" Inherits="ModificarUsuarioPage" EnableViewStateMac="true" EnableEventValidation="true" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<section class="usuarios-page usuario-operacion" aria-labelledby="tituloOperacion">
<asp:LinkButton ID="lnkVolverFormulario" runat="server" OnClick="Volver_Click" CssClass="btn btn-default formulario-volver" CausesValidation="false"><span aria-hidden="true">&larr;</span> <asp:Literal runat="server" Text="<%$ Resources:Textos, UI_Volver %>" /></asp:LinkButton>
<h1 id="tituloOperacion"><%: VitaPet.GUI.Resources.Textos.UI_ModificacionDeUsuario %></h1>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-warning" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:HyperLink ID="lnkRecargar" runat="server" Visible="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_VolverACargarLosDatos %>" />
<asp:Panel ID="pnlDatos" runat="server" Visible="false" CssClass="usuario-datos">
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Identificador %></strong><br /><asp:Label ID="lblIdUsuario" runat="server" /></div>
<div class="form-group"><asp:Label ID="lblNombreCampo" runat="server" AssociatedControlID="txtNombre" Text="<%$ Resources:Textos, UI_Nombre %>" /><asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100" /></div>
<div class="form-group"><asp:Label ID="lblCorreoCampo" runat="server" AssociatedControlID="txtCorreo" Text="<%$ Resources:Textos, UI_CorreoElectronico %>" /><asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control" MaxLength="100" /></div>
<div class="form-group"><asp:Label ID="lblRolCampo" runat="server" AssociatedControlID="ddlRol" Text="<%$ Resources:Textos, UI_Rol %>" /><asp:DropDownList ID="ddlRol" runat="server" CssClass="form-control"><asp:ListItem Value="Cliente" Text="<%$ Resources:Textos, UI_Cliente %>" /><asp:ListItem Value="Administrador" Text="<%$ Resources:Textos, UI_Administrador %>" /><asp:ListItem Value="Webmaster" Text="<%$ Resources:Textos, UI_Webmaster %>" /></asp:DropDownList></div>
<div class="form-group"><asp:Label ID="lblEstadoCampo" runat="server" AssociatedControlID="ddlEstado" Text="<%$ Resources:Textos, UI_Estado %>" /><asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control"><asp:ListItem Value="True" Text="<%$ Resources:Textos, UI_Activo %>" /><asp:ListItem Value="False" Text="<%$ Resources:Textos, UI_Inactivo %>" /></asp:DropDownList></div>
<div class="form-group"><strong><%: VitaPet.GUI.Resources.Textos.UI_Bloqueo %></strong><br /><asp:Label ID="lblBloqueado" runat="server" /></div>
<asp:Button ID="btnGuardar" runat="server" Text="<%$ Resources:Textos, UI_Guardar %>" OnClick="Guardar_Click" CssClass="btn btn-primary" CausesValidation="false" />
</asp:Panel>
<asp:Button ID="btnCancelar" runat="server" Text="<%$ Resources:Textos, UI_Cancelar %>" OnClick="Volver_Click" CssClass="btn btn-default usuario-volver" CausesValidation="false" />
<asp:HyperLink ID="lnkInicio" runat="server" Visible="false" NavigateUrl="~/Home.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_IrAlInicio %>" />
</section></asp:Content>
