<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="SelectorMascota.ascx.cs" Inherits="Controls_SelectorMascota" %>
<div class="table-responsive"><asp:GridView ID="gvMascotas" runat="server" AutoGenerateColumns="false" DataKeyNames="IdMascota" CssClass="table table-bordered planes-grilla" OnSelectedIndexChanged="SeleccionarMascota">
 <SelectedRowStyle CssClass="mascota-seleccionada" /><Columns><asp:BoundField DataField="Nombre" HeaderText="<%$ Resources:Textos, Mascota_Nombre %>" HtmlEncode="true" />
<asp:BoundField DataField="Especie" HeaderText="<%$ Resources:Textos, Mascota_Especie %>" HtmlEncode="true" />
<asp:BoundField DataField="Raza" HeaderText="<%$ Resources:Textos, Mascota_Raza %>" HtmlEncode="true" />
<asp:BoundField DataField="FechaNacimiento" HeaderText="<%$ Resources:Textos, Mascota_FechaNacimiento %>" HtmlEncode="true" DataFormatString="{0:dd/MM/yyyy}" />

 <asp:TemplateField HeaderText="<%$ Resources:Textos, Mascota_Estado %>"><ItemTemplate><%#: VitaPet.GUI.Presentacion.TextosUI.Obtener((bool)Eval("Estado")?"Mascota_Activa":"Mascota_Inactiva") %></ItemTemplate></asp:TemplateField>
 <asp:TemplateField><ItemTemplate><asp:LinkButton ID="lnkSeleccionarMascota" runat="server" CommandName="Select" CausesValidation="false" CssClass="btn btn-primary" Text="<%$ Resources:Textos, Mascota_Seleccionar %>" /></ItemTemplate></asp:TemplateField>
 </Columns></asp:GridView></div>
