using System;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.SECURITY;
using VitaPet.GUI.Presentacion;

public partial class Controls_Header : UserControl
{
    private const string RolCliente = "Cliente";
    private const string RolAdministrador = "Administrador";
    private const string RolWebmaster = "Webmaster";

    protected void Page_Load(object sender, EventArgs e)
    {
        CargarSesion();
    }

    private void CargarSesion()
    {
        if (!SessionManager.Instancia.HaySesionActiva)
        {
            pnlSesion.Visible = false;
            return;
        }

        UsuarioBE usuario =
            SessionManager.Instancia.UsuarioActual;

        if (usuario == null)
        {
            pnlSesion.Visible = false;
            return;
        }

        litNombreUsuario.Text =
            Server.HtmlEncode(usuario.Nombre);

        litRolUsuario.Text =
            Server.HtmlEncode(TextosUI.Traducir(usuario.Rol));

        string idioma = SessionManager.Instancia.ObtenerIdiomaUsuario();
        litIdiomaActual.Text = idioma.ToUpperInvariant();
        btnIdiomaEs.Attributes["aria-current"] = (idioma == "es").ToString().ToLowerInvariant();
        btnIdiomaEn.Attributes["aria-current"] = (idioma == "en").ToString().ToLowerInvariant();

        CargarMenu(usuario.Rol);

        pnlSesion.Visible = true;
    }

    private void CargarMenu(string rol)
    {
        pnlMenuCliente.Visible =
            string.Equals(
                rol,
                RolCliente,
                StringComparison.OrdinalIgnoreCase);

        pnlMenuAdministrador.Visible =
            string.Equals(
                rol,
                RolAdministrador,
                StringComparison.OrdinalIgnoreCase);

        pnlMenuWebmaster.Visible =
            string.Equals(
                rol,
                RolWebmaster,
                StringComparison.OrdinalIgnoreCase);
    }

    protected void btnIdiomaEs_Click(object sender, EventArgs e)
    {
        CambiarIdioma("es");
    }

    protected void btnIdiomaEn_Click(object sender, EventArgs e)
    {
        CambiarIdioma("en");
    }

    private void CambiarIdioma(string idioma)
    {
        var usuario = SessionManager.Instancia.UsuarioActual;
        if (usuario == null) return;
        try
        {
            new UsuarioBLL().ActualizarIdioma(usuario.IdUsuario, idioma);
            SessionManager.Instancia.ActualizarIdiomaUsuario(idioma);
        }
        catch (Exception ex) when (ex is System.Data.SqlClient.SqlException
            || ex is System.Configuration.ConfigurationErrorsException
            || ex is System.Transactions.TransactionException
            || ex is InvalidOperationException || ex is UnauthorizedAccessException)
        {
            lblErrorIdioma.Visible = true;
            return;
        }
        Response.Redirect(Request.RawUrl, false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
