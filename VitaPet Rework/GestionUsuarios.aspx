<%@ Page Title="<%$ Resources:Textos, UI_GestionDeUsuarios %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GestionUsuarios.aspx.cs" Inherits="GestionUsuariosPage" EnableViewStateMac="true" EnableEventValidation="true" %>
<%@ Assembly Name="System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="usuarios-page" aria-labelledby="tituloUsuarios">
        <h1 id="tituloUsuarios"><%: VitaPet.GUI.Resources.Textos.UI_GestionDeUsuarios %></h1>
        <p><%: VitaPet.GUI.Resources.Textos.UI_BuscaPorNombreYFiltraPorEstadoSelecciona %></p>
        <asp:Panel ID="pnlResultado" runat="server" Visible="false" CssClass="alert alert-success" role="status"><asp:Literal ID="litResultado" runat="server" /></asp:Panel>
        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-warning" role="alert">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>
        <asp:Panel ID="pnlFiltros" runat="server" DefaultButton="btnBuscar" CssClass="usuarios-filtros">
            <div class="row">
                <div class="col-sm-7 form-group">
                    <asp:Label ID="lblBuscar" runat="server" AssociatedControlID="txtBuscarUsuario" Text="<%$ Resources:Textos, UI_NombreDeUsuario %>" />
                    <asp:TextBox ID="txtBuscarUsuario" runat="server" CssClass="form-control" MaxLength="100" placeholder="<%$ Resources:Textos, UI_BuscarPorNombreCompletoOParcial %>" />
                </div>
                <div class="col-sm-3 form-group">
                    <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado" Text="<%$ Resources:Textos, UI_Estado %>" />
                    <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                        <asp:ListItem Value="Todos" Text="<%$ Resources:Textos, UI_Todos %>" />
                        <asp:ListItem Value="Activo" Text="<%$ Resources:Textos, UI_Activo %>" />
                        <asp:ListItem Value="Inactivo" Text="<%$ Resources:Textos, UI_Inactivo %>" />
                        <asp:ListItem Value="Bloqueado" Text="<%$ Resources:Textos, UI_Bloqueado %>" />
                        <asp:ListItem Value="Desbloqueado" Text="<%$ Resources:Textos, UI_Desbloqueado %>" />
                    </asp:DropDownList>
                </div>
                <div class="col-sm-2 form-group usuarios-buscar">
                    <asp:Button ID="btnBuscar" runat="server" Text="<%$ Resources:Textos, UI_Buscar %>" CssClass="btn btn-primary" OnClick="Buscar_Click" CausesValidation="false" />
                </div>
            </div>
        </asp:Panel>
        <p aria-live="polite"><asp:Literal ID="litResultados" runat="server" /></p>
        <div class="table-responsive">
            <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" DataKeyNames="IdUsuario"
                AllowPaging="true" PageSize="20" OnPageIndexChanging="Usuarios_PageIndexChanging" OnSelectedIndexChanged="Usuarios_SelectedIndexChanged"
                CssClass="table table-striped table-bordered usuarios-grilla" GridLines="None" UseAccessibleHeader="true"
                EmptyDataText="<%$ Resources:Textos, UI_NoSeEncontraronUsuariosParaLaConsulta %>">
                <Columns>
                    <asp:BoundField DataField="Nombre" HeaderText="<%$ Resources:Textos, UI_Nombre %>" HtmlEncode="true" />
                    <asp:BoundField DataField="CorreoElectronico" HeaderText="<%$ Resources:Textos, UI_CorreoElectronico %>" HtmlEncode="true" />
                    <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Rol %>"><ItemTemplate><%#: TextosUI.Traducir(Convert.ToString(Eval("Rol"))) %></ItemTemplate></asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Estado %>"><ItemTemplate><%#: (bool)Eval("Estado") ? VitaPet.GUI.Resources.Textos.UI_Activo : VitaPet.GUI.Resources.Textos.UI_Inactivo %></ItemTemplate></asp:TemplateField>
                    <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Bloqueo %>"><ItemTemplate><%#: (bool)Eval("Bloqueado") ? VitaPet.GUI.Resources.Textos.UI_Bloqueado : VitaPet.GUI.Resources.Textos.UI_Desbloqueado %></ItemTemplate></asp:TemplateField>
                    <asp:CommandField ShowSelectButton="true" SelectText="<%$ Resources:Textos, UI_Seleccionar %>" CausesValidation="false" />
                </Columns>
                <SelectedRowStyle CssClass="usuario-seleccionado" />
                <PagerSettings Mode="NumericFirstLast" FirstPageText="<%$ Resources:Textos, UI_Primera %>" LastPageText="<%$ Resources:Textos, UI_Ultima %>" PageButtonCount="5" />
                <PagerStyle CssClass="bitacora-paginacion" />
            </asp:GridView>
        </div>
        <asp:Panel ID="pnlAcciones" runat="server" Visible="false" CssClass="usuarios-acciones" role="region" aria-label="<%$ Resources:Textos, UI_UsuarioSeleccionado %>">
            <h2><%: VitaPet.GUI.Resources.Textos.UI_UsuarioSeleccionado %></h2>
            <p><asp:Literal ID="litSeleccion" runat="server" /></p>
            <div class="usuarios-botones">
                
                <asp:Button ID="btnConsultar" runat="server" OnClick="Consultar_Click" Text="<%$ Resources:Textos, UI_Consultar %>" CssClass="btn btn-default" CausesValidation="false" />
                <asp:Button ID="btnModificar" runat="server" OnClick="Modificar_Click" Text="<%$ Resources:Textos, UI_Modificar %>" CssClass="btn btn-default" CausesValidation="false" />
                <asp:Button ID="btnBloquear" runat="server" OnClick="Bloquear_Click" Text="<%$ Resources:Textos, UI_Bloquear %>" CssClass="btn btn-default" CausesValidation="false" />
                <asp:Button ID="btnDesbloquear" runat="server" OnClick="Desbloquear_Click" Text="<%$ Resources:Textos, UI_Desbloquear %>" CssClass="btn btn-default" CausesValidation="false" />
            </div>
        </asp:Panel>
    </section>
</asp:Content>
