<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ValidacionEmail.ascx.cs" Inherits="Controls_ValidacionEmail" %>

<div class="form-group login-field">
    <asp:Label
        ID="lblCorreo"
        runat="server"
        AssociatedControlID="txtCorreo"
        CssClass="login-label"
        Text="<%$ Resources:Textos, UI_CorreoElectronico %>" />

    <asp:TextBox
        ID="txtCorreo"
        runat="server"
        TextMode="Email"
        CssClass="form-control login-input"
        MaxLength="150"
        autocomplete="email" />

    <asp:RequiredFieldValidator
        ID="rfvCorreo"
        runat="server"
        ControlToValidate="txtCorreo"
        Display="Dynamic"
        CssClass="login-validation"
        ErrorMessage="<%$ Resources:Textos, UI_DebeIngresarSuCorreoElectronico %>" />

    <asp:RegularExpressionValidator
        ID="revCorreo"
        runat="server"
        ControlToValidate="txtCorreo"
        Display="Dynamic"
        CssClass="login-validation"
        ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
        ErrorMessage="<%$ Resources:Textos, UI_ElFormatoDelCorreoElectronicoNoEsValido %>" />
</div>