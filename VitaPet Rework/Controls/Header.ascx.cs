using System;
using System.Web.UI;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.SECURITY;
using VitaPet.GUI.Presentacion;

public partial class Controls_Header : UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {
        CargarSesion();
    }

    private void CargarSesion()
    {
        pnlAccesoPublico.Visible = true;
        string idioma = SessionManager.Instancia.ObtenerIdiomaUsuario();
        litIdiomaActual.Text = litIdiomaPublico.Text = idioma.ToUpperInvariant();
        btnIdiomaEs.Attributes["aria-current"] = btnIdiomaPublicoEs.Attributes["aria-current"] = (idioma == "es").ToString().ToLowerInvariant();
        btnIdiomaEn.Attributes["aria-current"] = btnIdiomaPublicoEn.Attributes["aria-current"] = (idioma == "en").ToString().ToLowerInvariant();
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

        CargarMenu(usuario.Rol);

        pnlSesion.Visible = true;
        pnlAccesoPublico.Visible = false;
    }

    private void CargarMenu(string rol)
    {
        var accesos=new HashSet<string>(new PerfilBLL().PantallasActuales(),StringComparer.OrdinalIgnoreCase);
        foreach(var panel in new[]{pnlMenuCliente,pnlMenuAdministrador,pnlMenuWebmaster})
        {
            panel.Visible=true; bool alguno=false;
            foreach(var enlace in panel.Controls.OfType<HyperLink>())
            {
                bool permitido=accesos.Contains(System.IO.Path.GetFileNameWithoutExtension(enlace.NavigateUrl));
                if(enlace.NavigateUrl=="~/ConsultarPerfiles.aspx" && accesos.Contains("GestionPerfiles")) permitido=false;
                enlace.Visible=permitido;
                alguno|=permitido;
            }
            panel.Visible=alguno;
        }
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
        try
        {
            if (usuario != null)
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
