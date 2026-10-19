<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FiltroZona.ascx.cs" Inherits="Controls_FiltroZona" %>
<asp:Label ID="lblZona" runat="server" AssociatedControlID="ddlZona" Text="<%$ Resources:Textos, Turno_Zona %>" />
<asp:DropDownList ID="ddlZona" runat="server" CssClass="form-control" AutoPostBack="true" Enabled="false" OnSelectedIndexChanged="CambiarZona" />
