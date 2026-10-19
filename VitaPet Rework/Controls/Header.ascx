<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Header.ascx.cs" Inherits="Controls_Header" %>

<header class="navbar navbar-inverse navbar-fixed-top">
    <div class="container">
        <div class="navbar-header">
            <a
                class="navbar-brand"
                runat="server"
                href="~/Login.aspx"
                aria-label="VitaPet">
                <asp:Image
                    ID="imgLogoVitaPet"
                    runat="server"
                    ImageUrl="~/Content/Images/VitaPetLogo.png"
                    AlternateText="VitaPet"
                    CssClass="navbar-logo" />
            </a>
        </div>

        <asp:Panel
            ID="pnlSesion"
            runat="server"
            CssClass="navbar-session"
            Visible="false">
            <span class="navbar-user">
                <asp:Literal ID="litSaludo" runat="server" Text="<%$ Resources:Textos, Saludo %>" />
                <asp:Literal ID="litNombreUsuario" runat="server" />
            </span>

            <span class="navbar-role">
                <asp:Literal ID="litRolUsuario" runat="server" />
            </span>

            <nav class="navbar-role-menu" aria-label="<%: VitaPet.GUI.Resources.Textos.UI_FuncionalidadesDisponibles %>">
                <asp:HyperLink
                    ID="lnkInicio"
                    runat="server"
                    NavigateUrl="~/Home.aspx"
                    Text="<%$ Resources:Textos, UI_Inicio %>" />

                <asp:Panel
                    ID="pnlMenuCliente"
                    runat="server"
                    CssClass="navbar-role-links"
                    Visible="false">
                    <asp:HyperLink
                        ID="lnkPerfilCliente"
                        runat="server"
                        NavigateUrl="~/Cliente.aspx"
                        Text="<%$ Resources:Textos, UI_MiPerfil %>" />

                    <asp:HyperLink
                        ID="lnkPlanesCliente"
                        runat="server"
                        NavigateUrl="~/Planes.aspx"
                        Text="<%$ Resources:Textos, UI_Planes %>" />

                    <asp:HyperLink
                        ID="lnkMascotas"
                        runat="server"
                        NavigateUrl="~/Mascotas.aspx"
                        Text="<%$ Resources:Textos, UI_Mascotas %>" />

                    <asp:HyperLink
                        ID="lnkTurnos"
                        runat="server"
                        NavigateUrl="~/Turnos.aspx"
                        Text="<%$ Resources:Textos, UI_Turnos %>" />

                    <asp:HyperLink
                        ID="lnkReintegros"
                        runat="server"
                        NavigateUrl="~/Reintegros.aspx"
                        Text="<%$ Resources:Textos, UI_Reintegros %>" />
                </asp:Panel>

                <asp:Panel
                    ID="pnlMenuAdministrador"
                    runat="server"
                    CssClass="navbar-role-links"
                    Visible="false">
                    <asp:HyperLink
                        ID="lnkGestionEmpresas"
                        runat="server"
                        NavigateUrl="~/Empresas.aspx"
                        Text="<%$ Resources:Textos, UI_Empresas %>" />

                    <asp:HyperLink ID="lnkAltaEmpresa" runat="server" NavigateUrl="~/AltaEmpresa.aspx" Text="<%$ Resources:Textos, Empresa_Alta %>" />

                    <asp:HyperLink
                        ID="lnkGestionPlanes"
                        runat="server"
                        NavigateUrl="~/GestionPlanes.aspx"
                        Text="<%$ Resources:Textos, UI_Planes %>" />

                    <asp:HyperLink
                        ID="lnkAltaPlan" runat="server" NavigateUrl="~/AltaPlan.aspx" Text="<%$ Resources:Textos, PlanAdmin_Alta %>" />
                    <asp:HyperLink
                        ID="lnkClienteCorporativo"
                        runat="server"
                        NavigateUrl="~/RegistroClienteCorporativo.aspx"
                        Text="<%$ Resources:Textos, UI_ClientesCorporativos %>" />
                </asp:Panel>

                <asp:Panel
                    ID="pnlMenuWebmaster"
                    runat="server"
                    CssClass="navbar-role-links"
                    Visible="false">
                    <asp:HyperLink
                        ID="lnkBitacora"
                        runat="server"
                        NavigateUrl="~/ConsultaBitacora.aspx"
                        Text="<%$ Resources:Textos, UI_Bitacora %>" />

                    <asp:HyperLink
                        ID="lnkGestionUsuarios"
                        runat="server"
                        NavigateUrl="~/GestionUsuarios.aspx"
                        Text="<%$ Resources:Textos, UI_Usuarios %>" />

                    <asp:HyperLink
                        ID="lnkBaseDatos"
                        runat="server"
                        NavigateUrl="~/BaseDatos.aspx"
                        Text="<%$ Resources:Textos, UI_BaseDeDatos %>" />
                </asp:Panel>

                <asp:HyperLink
                    ID="lnkCerrarSesion"
                    runat="server"
                    NavigateUrl="~/Logout.aspx"
                    Text="<%$ Resources:Textos, BotonSalir %>" />
                <div class="dropdown language-selector">
                    <button type="button" id="btnSelectorIdioma" class="language-toggle dropdown-toggle" data-toggle="dropdown"
                        aria-haspopup="true" aria-expanded="false" aria-label="<%: TextosUI.Obtener("UI_SeleccionarIdioma") %>">
                        <svg class="language-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                            stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">
                            <path d="M3 5h12M9 3v2m-4 3c1 4 4 6 7 8m1-11c-1 6-4 9-9 12M14 21l4-10 4 10m-6-4h4" />
                        </svg>
                        <span class="language-code"><asp:Literal ID="litIdiomaActual" runat="server" /></span>
                        <span class="language-caret" aria-hidden="true"></span>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-right language-options">
                        <li><asp:LinkButton ID="btnIdiomaEs" runat="server" Text="ES · Español" CausesValidation="false" OnClick="btnIdiomaEs_Click" /></li>
                        <li><asp:LinkButton ID="btnIdiomaEn" runat="server" Text="EN · English" CausesValidation="false" OnClick="btnIdiomaEn_Click" /></li>
                    </ul>
                </div>
            </nav>
            <asp:Label ID="lblErrorIdioma" runat="server" Visible="false" CssClass="text-danger" role="alert"
                Text="<%$ Resources:Textos, ErrorCambioIdioma %>" />
        </asp:Panel>
    </div>
</header>
