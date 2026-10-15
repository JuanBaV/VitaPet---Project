<%@ Page Title="<%$ Resources:Textos, UI_BaseDeDatos %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="BaseDatos.aspx.cs" Inherits="BaseDatosPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h1><%: VitaPet.GUI.Resources.Textos.UI_BaseDeDatos %></h1>
    <p><%: VitaPet.GUI.Resources.Textos.UI_AdministracionDeRespaldosYRecuperacionExclusivaParaWebmaster %></p>
    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="alert alert-info" role="status"><asp:Literal ID="litMensaje" runat="server" /></asp:Panel>
    <h2><%: VitaPet.GUI.Resources.Textos.UI_Integridad %></h2>
    <p><%: VitaPet.GUI.Resources.Textos.UI_SeVerificanUsuarioYBitacoraSusDvhDvv %></p>
    <asp:Button ID="btnVerificar" runat="server" Text="<%$ Resources:Textos, UI_VerificarIntegridad %>" OnClick="Verificar_Click" CssClass="btn btn-default" CausesValidation="false" />
    <asp:GridView ID="grdIntegridad" runat="server" AutoGenerateColumns="false" CssClass="table table-striped" EmptyDataText="<%$ Resources:Textos, UI_NoSeDetectaronErroresDeIntegridad %>">
        <Columns>
            <asp:BoundField DataField="Tabla" HeaderText="<%$ Resources:Textos, UI_Tabla %>" HtmlEncode="true" />
            <asp:BoundField DataField="Fila" HeaderText="<%$ Resources:Textos, UI_FilaClave %>" HtmlEncode="true" />
            <asp:BoundField DataField="Columna" HeaderText="<%$ Resources:Textos, UI_Columna %>" HtmlEncode="true" />
            <asp:TemplateField HeaderText="<%$ Resources:Textos, UI_Detalle %>"><ItemTemplate><%#: TextosUI.Traducir(Convert.ToString(Eval("Motivo"))) %></ItemTemplate></asp:TemplateField>
        </Columns>
    </asp:GridView>
    <h2><%: VitaPet.GUI.Resources.Textos.UI_Backups %></h2>
    <p><%: VitaPet.GUI.Resources.Textos.UI_LosBackupsSeVerificanAntesDeOfrecerlosPara %></p>
    <asp:Button ID="btnCrear" runat="server" Text="<%$ Resources:Textos, UI_CrearBackup %>" OnClick="Crear_Click" CssClass="btn btn-primary" CausesValidation="false" />
    <h2><%: VitaPet.GUI.Resources.Textos.UI_RestaurarUnBackup %></h2>
    <asp:Label ID="lblBackup" runat="server" AssociatedControlID="ddlBackups" Text="<%$ Resources:Textos, UI_BackupDisponible %>" />
    <asp:DropDownList ID="ddlBackups" runat="server" CssClass="form-control" />
    <asp:Button ID="btnRestaurar" runat="server" Text="<%$ Resources:Textos, UI_RestaurarBackupSeleccionado %>" OnClick="Restaurar_Click" CssClass="btn btn-warning" CausesValidation="false" />
    <asp:Panel ID="pnlConfirmacion" runat="server" Visible="false" CssClass="alert alert-warning">
        <h3><%: VitaPet.GUI.Resources.Textos.UI_ConfirmarOperacion %></h3>
        <p><asp:Literal ID="litConfirmacion" runat="server" /></p>
        <asp:Button ID="btnConfirmar" runat="server" Text="<%$ Resources:Textos, UI_Confirmar %>" OnClick="Confirmar_Click" CssClass="btn btn-danger" CausesValidation="false" />
        <asp:Button ID="btnCancelar" runat="server" Text="<%$ Resources:Textos, UI_Cancelar %>" OnClick="Cancelar_Click" CssClass="btn btn-default" CausesValidation="false" />
    </asp:Panel>
    <p><%: VitaPet.GUI.Resources.Textos.UI_DuranteLaOperacionEspereLaRespuestaSinVolver %></p>
    <h2><%: VitaPet.GUI.Resources.Textos.UI_RecuperacionDelWebmaster %></h2>
    <p><%: VitaPet.GUI.Resources.Textos.UI_ConfigureUnaCuentaDeEmergenciaAntesDeNecesitarla %></p>
    <asp:Label runat="server" AssociatedControlID="txtCorreoEmergencia" Text="<%$ Resources:Textos, UI_CorreoDeEmergencia %>" />
    <asp:TextBox ID="txtCorreoEmergencia" runat="server" CssClass="form-control" MaxLength="100" TextMode="Email" />
    <asp:Label runat="server" AssociatedControlID="txtClaveEmergencia" Text="<%$ Resources:Textos, UI_ContrasenaDeEmergencia %>" />
    <asp:TextBox ID="txtClaveEmergencia" runat="server" CssClass="form-control" MaxLength="100" TextMode="Password" autocomplete="new-password" />
    <p><%: VitaPet.GUI.Resources.Textos.UI_UseDe12A100CaracteresConMayuscula %></p>
    <asp:Button ID="btnConfigurarEmergencia" runat="server" Text="<%$ Resources:Textos, UI_GuardarCuentaYRespaldoInicial %>" OnClick="ConfigurarEmergencia_Click" CssClass="btn btn-primary" CausesValidation="false" />
    <p><%: VitaPet.GUI.Resources.Textos.UI_AlGuardarSeCerraranLasSesionesLaContrasena %></p>
    <asp:Button ID="btnXmlEmergencia" runat="server" Text="<%$ Resources:Textos, UI_CrearRespaldoXmlDeContingencia %>" OnClick="XmlEmergencia_Click" CssClass="btn btn-default" CausesValidation="false" />
</asp:Content>
