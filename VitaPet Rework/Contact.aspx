<%@ Page Title="<%$ Resources:Textos, UI_Contacto %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="Contact" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%: Title %>.</h2>
    <h3><%: VitaPet.GUI.Resources.Textos.UI_PaginaDeContacto %></h3>
    <address>
        One Microsoft Way<br />
        Redmond, WA 98052-6399<br />
        <abbr title="<%: VitaPet.GUI.Resources.Textos.UI_Telefono %>">P:</abbr>
        425.555.0100
    </address>

    <address>
        <strong><%: VitaPet.GUI.Resources.Textos.UI_Soporte %></strong>   <a href="mailto:Support@example.com">Support@example.com</a><br />
        <strong><%: VitaPet.GUI.Resources.Textos.UI_Marketing %></strong> <a href="mailto:Marketing@example.com">Marketing@example.com</a>
    </address>
</asp:Content>
