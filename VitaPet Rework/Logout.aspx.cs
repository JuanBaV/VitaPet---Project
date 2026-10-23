using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class LogoutPage : PaginaBase
{
    protected override string[] RolesPermitidos
    {
        get { return new[] { "Cliente", "Administrador", "Webmaster" }; }
    }

    protected override void OnPreInit(EventArgs e)
    {
        // Stop unauthenticated POSTs before Web Forms dispatches their events.
        var usuario = SessionManager.Instancia.UsuarioActual;
        if (usuario == null)
            Response.Redirect(ResolveUrl("~/Login.aspx") + "?ReturnUrl=" +
                Server.UrlEncode(Request.RawUrl), true);
        if (!new PerfilBLL().PuedeAcceder("Logout"))
            Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"), true);
        ViewStateUserKey = Session.SessionID;
        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        base.OnPreInit(e);
    }

    protected override void MostrarMensajes()
    {
        pnlMensaje.Visible = false;
        litMensaje.Text = string.Empty;
    }

    protected override void EjecutarFuncionalidad()
    {
        if (!IsPostBack)
            Ejecutar(() => new SecurityManager().ExigirIntegridad());
    }

    protected void btnConfirmarLogout_Click(object sender, EventArgs e)
    {
        bool completado = Ejecutar(() =>
        {
            var usuario = SessionManager.Instancia.UsuarioActual;
            if (usuario == null) throw new InvalidOperationException("La sesión ya no está disponible.");
            new SecurityManager().RegistrarBitacora(new BitacoraBE {
                IdUsuario = usuario.IdUsuario,
                Fecha = DateTime.Now,
                Accion = "Logout",
                Modulo = "Autenticación",
                Criticidad = "Informativa"
            });
            // Close only after auditing succeeds; failures keep the authenticated session.
            SessionManager.Instancia.CerrarSesion();
        });
        if (!completado) return;
        Response.Redirect(ResolveUrl("~/Home.aspx"), false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private bool Ejecutar(Action accion)
    {
        try { accion(); return true; }
        catch (SqlException) { MostrarError("ER003"); }
        catch (ConfigurationErrorsException) { MostrarError("ER003"); }
        catch (InvalidOperationException ex)
        {
            MostrarError(ex.Message.StartsWith("ER023", StringComparison.Ordinal) ? "ER023" : "ER034");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
            ex is XmlException || ex is TransactionException || ex is System.Security.Cryptography.CryptographicException)
        {
            MostrarError("ER034");
        }
        return false;
    }

    private void MostrarError(string codigo)
    {
        string mensaje = codigo == "ER003" ? "Fallo de conexión" :
            codigo == "ER023" ? "Inconsistencia en la verificación de integridad" :
            "No se pudo completar el cierre de sesión";
        try
        {
            var error = new ErrorSistemaBLL().ObtenerPorCodigo(codigo);
            if (error != null) mensaje = error.Descripcion;
        }
        catch (SqlException) { }
        catch (ConfigurationErrorsException) { }
        litMensaje.Text = Server.HtmlEncode(codigo + " - " + TextosUI.Error(codigo, mensaje));
        pnlMensaje.Visible = true;
    }
}
