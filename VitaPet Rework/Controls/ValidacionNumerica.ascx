<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ValidacionNumerica.ascx.cs" Inherits="Controls_ValidacionNumerica" %>
<div class="form-group login-field">
    <asp:Label ID="lblNumero" runat="server" AssociatedControlID="txtNumero" CssClass="login-label" Text="<%$ Resources:Textos, Numerico_Etiqueta %>" />
    <asp:TextBox ID="txtNumero" runat="server" inputmode="decimal" CssClass="form-control login-input" />
    <asp:RequiredFieldValidator ID="rfvNumero" runat="server" ControlToValidate="txtNumero" Display="Dynamic" CssClass="login-validation" ErrorMessage="<%$ Resources:Textos, Alta_Obligatorio %>" />
    <asp:RegularExpressionValidator ID="revNumero" runat="server" ControlToValidate="txtNumero" Display="Dynamic" CssClass="login-validation" ValidationExpression="^[0-9]+(?:[.,][0-9]{1,2})?$" ErrorMessage="<%$ Resources:Textos, Numerico_FormatoInvalido %>" />
</div>
