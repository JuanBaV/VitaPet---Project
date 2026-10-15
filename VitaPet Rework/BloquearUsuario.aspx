<%@ Page Title="<%$ Resources:Textos, UI_BloqueoDeUsuario %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BloquearUsuario.aspx.cs" Inherits="BloquearUsuarioPage" EnableViewStateMac="true" EnableEventValidation="true" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<section class="usuarios-page usuario-operacion" aria-labelledby="tituloOperacion">
<h1 id="tituloOperacion"><%: VitaPet.GUI.Resources.Textos.UI_BloqueoDeUsuario %></h1>
<asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-warning" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
<asp:HyperLink ID="lnkRecargar" runat="server" Visible="false" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_VolverACargarLosDatos %>" />
<asp:Panel ID="pnlDatos" runat="server" Visible="false" CssClass="usuario-datos">
<p><%: VitaPet.GUI.Resources.Textos.UI_ConfirmasQueQueresBloquearAlSiguienteUsuario %></p>
<asp:DetailsView ID="dvUsuario" runat="server" AutoGenerateRows="false" CssClass="table table-bordered" GridLines="None">
<Fields>
<asp:BoundField DataField="IdUsuario" HeaderText="<%$ Resources:Textos, UI_Identificador %>" HtmlEncode="true" />
<asp:BoundField DataField="Nombre" HeaderText="<%$ Resources:Textos, UI_Nombre %>" HtmlEncode="true" />
<asp:BoundField DataField="CorreoElectronico" HeaderText="<%$ Resources:Textos, UI_CorreoElectronico %>" HtmlEncode="true" />
<asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Rol %>"><ItemTemplate><%#: TextosUI.Traducir(Convert.ToString(Eval("Rol"))) %></ItemTemplate></asp:TemplateField>
<asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Estado %>"><ItemTemplate><%#: (bool)Eval("Estado") ? VitaPet.GUI.Resources.Textos.UI_Activo : VitaPet.GUI.Resources.Textos.UI_Inactivo %></ItemTemplate></asp:TemplateField>
<asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Bloqueo %>"><ItemTemplate><%#: (bool)Eval("Bloqueado") ? VitaPet.GUI.Resources.Textos.UI_Bloqueado : VitaPet.GUI.Resources.Textos.UI_Desbloqueado %></ItemTemplate></asp:TemplateField>
</Fields></asp:DetailsView>
<asp:Button ID="btnConfirmarBloqueo" runat="server" Text="<%$ Resources:Textos, UI_ConfirmarBloqueo %>" OnClick="Guardar_Click" CssClass="btn btn-primary" CausesValidation="false" />
</asp:Panel>
<asp:Button ID="btnCancelar" runat="server" Text="<%$ Resources:Textos, UI_Cancelar %>" OnClick="Volver_Click" CssClass="btn btn-default usuario-volver" CausesValidation="false" />
<asp:HyperLink ID="lnkInicio" runat="server" Visible="false" NavigateUrl="~/Home.aspx" CssClass="btn btn-default" Text="<%$ Resources:Textos, UI_IrAlInicio %>" />
</section></asp:Content>
