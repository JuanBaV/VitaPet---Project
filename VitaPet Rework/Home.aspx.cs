using System;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class HomePage : PaginaConIdioma
{
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var usuario = SessionManager.Instancia.UsuarioActual;
        pnlSaludo.Visible = usuario != null;
        if (usuario != null)
            litSaludo.Text = Server.HtmlEncode(VitaPet.GUI.Resources.Textos.Saludo + usuario.Nombre);
    }
}
