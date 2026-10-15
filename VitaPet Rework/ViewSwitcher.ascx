<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ViewSwitcher.ascx.cs" Inherits="ViewSwitcher" %>
<div id="viewSwitcher">
    <%: VitaPet.GUI.Resources.Textos.UI_Vista %>: <%: TextosUI.Traducir(CurrentView) %> | <a href="<%: SwitchUrl %>" data-ajax="false"><%: VitaPet.GUI.Resources.Textos.UI_CambiarA %> <%: TextosUI.Traducir(AlternateView) %></a>
</div>
