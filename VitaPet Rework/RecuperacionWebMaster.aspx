<%@ Page Title="<%$ Resources:Textos, UI_RecuperarAccesoWebmaster %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecuperacionWebMaster.aspx.cs" Inherits="RecuperacionWebMasterPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h1><%: VitaPet.GUI.Resources.Textos.UI_RecuperarAccesoWebmaster %></h1>
    <p><%: VitaPet.GUI.Resources.Textos.UI_AccesoDeEmergenciaExclusivoParaRestaurarElSistema %></p>
    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-info" role="alert"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
    <asp:Panel ID="pnlAcceso" runat="server">
        <asp:Label runat="server" AssociatedControlID="txtCorreo" Text="<%$ Resources:Textos, UI_CorreoDeEmergencia %>" />
        <asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control" MaxLength="100" TextMode="Email" />
        <asp:Label runat="server" AssociatedControlID="txtClave" Text="<%$ Resources:Textos, UI_ContrasenaDeEmergencia %>" />
        <asp:TextBox ID="txtClave" runat="server" CssClass="form-control" MaxLength="100" TextMode="Password" autocomplete="current-password" />
        <asp:Button ID="btnIngresar" runat="server" Text="<%$ Resources:Textos, UI_AccederARecuperacion %>" OnClick="Ingresar_Click" CssClass="btn btn-primary" />
    </asp:Panel>
    <asp:Panel ID="pnlRestaurar" runat="server" Visible="false">
        <asp:Label runat="server" AssociatedControlID="ddlRespaldos" Text="<%$ Resources:Textos, UI_PuntoDeRecuperacion %>" />
        <asp:DropDownList ID="ddlRespaldos" runat="server" CssClass="form-control" />
        <asp:Button ID="btnSeleccionar" runat="server" Text="<%$ Resources:Textos, UI_RestaurarPuntoSeleccionado %>" OnClick="Seleccionar_Click" CssClass="btn btn-warning" />
        <asp:Panel ID="pnlConfirmar" runat="server" Visible="false" CssClass="alert alert-warning">
            <p><asp:Literal ID="litConfirmacion" runat="server" /></p>
            <asp:Button ID="btnConfirmar" runat="server" Text="<%$ Resources:Textos, UI_ConfirmarRestauracion %>" OnClick="Confirmar_Click" CssClass="btn btn-danger" />
            <asp:Button ID="btnCancelar" runat="server" Text="<%$ Resources:Textos, UI_Cancelar %>" OnClick="Cancelar_Click" CssClass="btn btn-default" />
        </asp:Panel>
    </asp:Panel>
    <asp:Button ID="btnSalir" runat="server" Text="<%$ Resources:Textos, UI_CerrarRecuperacion %>" OnClick="Salir_Click" CssClass="btn btn-default" />
</asp:Content>
