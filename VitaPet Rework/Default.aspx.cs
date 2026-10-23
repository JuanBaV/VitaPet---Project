using System;
using VitaPet.GUI.Presentacion;

public partial class _Default : PaginaConIdioma
{
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Response.Redirect(ResolveUrl("~/Home.aspx"), false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
