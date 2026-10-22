<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CatalogoErrores.aspx.cs" Inherits="CatalogoErroresPage" %>
<!DOCTYPE html>
<html lang="<%: System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName %>">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title><%: VitaPet.GUI.Resources.Textos.UI_ErroresDelSistemaVitapet %></title>
    <style>
        * { box-sizing: border-box; }
        body { margin: 0; background: #fff; color: #174d48; font: 14px/20px Arial, sans-serif; }
        header { padding: 20px 24px; }
        h1 { margin: 0 0 10px; font-size: 22px; }
        header p { margin: 0 0 10px; }
        a { color: #174d48; }
        .formulario-volver { display: inline-flex; align-items: center; gap: 8px; min-height: 44px; margin-bottom: 16px; padding: 8px 12px; border: 1px solid #b8d9d5; border-radius: 4px; text-decoration: none; }
        .formulario-volver:focus { outline: 2px solid #174d48; outline-offset: 2px; }
        main { padding: 7px 6px; }
        .error-capture { width: 352px; max-width: 100%; margin: 0 0 24px; padding: 12px 16px;
            border: 1px solid #b8d9d5; border-radius: 6px; background: #e9f7f5; color: #174d48;
            overflow-wrap: anywhere; }
        .status { margin: 0 0 24px; }
    </style>
</head>
<body>
    <form id="formCatalogo" runat="server">
        <header>
            <asp:HyperLink runat="server" NavigateUrl="~/Login.aspx" CssClass="formulario-volver"><span aria-hidden="true">&larr;</span> <asp:Literal runat="server" Text="<%$ Resources:Textos, UI_Volver %>" /></asp:HyperLink>
            <h1><%: VitaPet.GUI.Resources.Textos.UI_ErroresDelSistema %></h1>
            <p><%: VitaPet.GUI.Resources.Textos.UI_CadaMensajeEstaSeparadoParaQuePuedasRecortarlo %></p>
        </header>
        <main>
            <asp:Label ID="lblEstado" runat="server" Visible="false" CssClass="status" role="status" />
            <asp:Repeater ID="rptErrores" runat="server">
                <ItemTemplate>
                    <div class="error-capture"><%#: Eval("Codigo") %> - <%#: TextosUI.Error(Convert.ToString(Eval("Codigo")), Convert.ToString(Eval("Descripcion"))) %></div>
                </ItemTemplate>
            </asp:Repeater>
        </main>
    </form>
</body>
</html>
