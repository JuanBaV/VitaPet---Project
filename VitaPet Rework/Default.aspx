<%@ Page Title="<%$ Resources:Textos, UI_Inicio2 %>" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="_Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>ASP.NET</h1>
        <p class="lead"><%: VitaPet.GUI.Resources.Textos.UI_AspNetEsUnFrameworkWebGratuitoPara %></p>
        <p><a href="http://www.asp.net" class="btn btn-primary btn-lg"><%: VitaPet.GUI.Resources.Textos.UI_MasInformacion %></a></p>
    </div>

    <div class="row">
        <div class="col-md-4">
            <h2><%: VitaPet.GUI.Resources.Textos.UI_PrimerosPasos %></h2>
            <p>
                <%: VitaPet.GUI.Resources.Textos.UI_AspNetWebFormsPermiteCrearSitiosDinamicos %>
            </p>
            <p>
                <a class="btn btn-default" href="https://go.microsoft.com/fwlink/?LinkId=301948"><%: VitaPet.GUI.Resources.Textos.UI_MasInformacion %></a>
            </p>
        </div>
        <div class="col-md-4">
            <h2><%: VitaPet.GUI.Resources.Textos.UI_ObtenerMasBibliotecas %></h2>
            <p>
                <%: VitaPet.GUI.Resources.Textos.UI_NugetEsUnaExtensionGratuitaQueFacilitaAgregar %>
            </p>
            <p>
                <a class="btn btn-default" href="https://go.microsoft.com/fwlink/?LinkId=301949"><%: VitaPet.GUI.Resources.Textos.UI_MasInformacion %></a>
            </p>
        </div>
        <div class="col-md-4">
            <h2><%: VitaPet.GUI.Resources.Textos.UI_AlojamientoWeb %></h2>
            <p>
                <%: VitaPet.GUI.Resources.Textos.UI_PodesElegirUnProveedorDeAlojamientoWebCon %>
            </p>
            <p>
                <a class="btn btn-default" href="https://go.microsoft.com/fwlink/?LinkId=301950"><%: VitaPet.GUI.Resources.Textos.UI_MasInformacion %></a>
            </p>
        </div>
    </div>
</asp:Content>
