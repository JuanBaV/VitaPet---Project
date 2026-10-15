<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ListadoPlanes.ascx.cs" Inherits="Controls_ListadoPlanes" %>
<div class="table-responsive">
    <asp:GridView ID="gvPlanes" runat="server" AutoGenerateColumns="false" DataKeyNames="IdPlan" CssClass="table table-bordered planes-grilla" OnSelectedIndexChanged="SeleccionarPlan" EmptyDataText="<%$ Resources:Textos, Error_ER051 %>">
        <Columns>
            <asp:BoundField DataField="Nombre" HeaderText="<%$ Resources:Textos, UI_Nombre %>" HtmlEncode="true" />
            <asp:BoundField DataField="Descripcion" HeaderText="<%$ Resources:Textos, Planes_Descripcion %>" HtmlEncode="true" />
            <asp:TemplateField HeaderText="<%$ Resources:Textos, Planes_PrecioMascota %>">
                <ItemTemplate><span class="planes-precio">USD <%#: Eval("Precio","{0:N2}") %></span></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate><asp:LinkButton ID="lnkSeleccionarPlan" runat="server" CommandName="Select" CausesValidation="false" Text="<%$ Resources:Textos, Planes_Seleccionar %>" CssClass="btn btn-primary" /></ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>
