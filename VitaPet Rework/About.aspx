<%@ Page Title="<%$ Resources:Textos, UI_AcercaDe %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%: Title %>.</h2>
    <h3><%: VitaPet.GUI.Resources.Textos.UI_DescripcionDeLaAplicacion %></h3>
    <p><%: VitaPet.GUI.Resources.Textos.UI_UtilizaEstaSeccionParaIncluirInformacionAdicional %></p>
</asp:Content>
