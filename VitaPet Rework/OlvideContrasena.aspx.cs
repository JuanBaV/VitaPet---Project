using VitaPet.GUI.Presentacion;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.SECURITY;

public partial class OlvideContrasenaPage : VitaPet.GUI.Presentacion.PaginaConIdioma
{
    private const string CodigoErrorConexion = "ER003";
    private const string CodigoErrorRecuperacion = "ER035";
    private const string MensajeErrorConexion = "Fallo de conexión";
    private const string MensajeErrorRecuperacion =
        "Correo electrónico o respuesta incorrectos";
    private const string RutaRestablecerContrasena =
        "~/RestablecerContrasena.aspx";

    protected void Page_Load(object sender, EventArgs e)
    {
        if(!IsPostBack && Session["CorreoRestablecimientoObligatorio"] is string)
        {
            ucCorreo.Correo=(string)Session["CorreoRestablecimientoObligatorio"];
            Session.Remove("CorreoRestablecimientoObligatorio");
            MostrarMensaje("",TextosUI.Obtener("Corporativo_Restablecer"));
        }
    }

    protected void btnContinuar_Click(
        object sender,
        EventArgs e)
    {
        Page.Validate("Recuperacion");

        if (!Page.IsValid)
        {
            OcultarMensaje();
            return;
        }

        try
        {
            RecuperacionContrasenaBLL recuperacionBLL =
                new RecuperacionContrasenaBLL();

            UsuarioBE usuario =
                recuperacionBLL.ValidarIdentidad(
                    ucCorreo.Correo,
                    txtRespuesta.Text);

            if (usuario == null)
            {
                SecurityManager seguridad =
                    new SecurityManager();

                MostrarError(
                    seguridad,
                    CodigoErrorRecuperacion,
                    MensajeErrorRecuperacion);

                return;
            }

            SessionManager.Instancia
                .AutorizarRecuperacionContrasena(
                    usuario.IdUsuario);

            OcultarMensaje();

            Response.Redirect(
                ResolveUrl(RutaRestablecerContrasena),
                false);

            Context.ApplicationInstance.CompleteRequest();

            return;
        }
        catch (SqlException)
        {
            MostrarMensaje(
                CodigoErrorConexion,
                MensajeErrorConexion);
        }
        catch (ConfigurationErrorsException)
        {
            MostrarMensaje(
                CodigoErrorConexion,
                MensajeErrorConexion);
        }
    }

    private void MostrarError(
        SecurityManager seguridad,
        string codigo,
        string mensajeAlternativo)
    {
        try
        {
            ErrorSistemaBE error =
                seguridad.ObtenerError(codigo);

            if (error != null)
            {
                MostrarMensaje(
                    error.Codigo,
                    error.Descripcion);

                return;
            }
        }
        catch (SqlException)
        {
        }

        MostrarMensaje(
            codigo,
            mensajeAlternativo);
    }

    private void MostrarMensaje(
        string codigo,
        string descripcion)
    {
        litMensaje.Text = Server.HtmlEncode(
            string.IsNullOrEmpty(codigo) ? descripcion : TextosUI.Traducir(codigo) + " - " + TextosUI.Error(codigo, descripcion));

        pnlMensaje.Visible = true;
    }

    private void OcultarMensaje()
    {
        litMensaje.Text = string.Empty;
        pnlMensaje.Visible = false;
    }
}
