using VitaPet.GUI.Presentacion;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.SECURITY;

public partial class RestablecerContrasenaPage : VitaPet.GUI.Presentacion.PaginaConIdioma
{
    private const string CodigoErrorAutorizacion = "ER030";
    private const string MensajeErrorAutorizacion =
        "No posee autorización para restablecer la contraseña";
    private const string CodigoErrorContrasenasNoCoinciden =
        "ER029";
    private const string MensajeErrorContrasenasNoCoinciden =
        "Las contraseñas no coinciden";
    private const string CodigoErrorConexion = "ER003";
    private const string MensajeErrorConexion =
        "Fallo de conexión";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!TieneAutorizacionRecuperacion())
        {
            pnlFormulario.Visible = false;

            MostrarError(
                CodigoErrorAutorizacion,
                MensajeErrorAutorizacion);

            return;
        }

        pnlFormulario.Visible = true;
        OcultarMensaje();
    }

    protected void btnRestablecer_Click(
        object sender,
        EventArgs e)
    {
        if (!TieneAutorizacionRecuperacion())
        {
            pnlFormulario.Visible = false;

            MostrarError(
                CodigoErrorAutorizacion,
                MensajeErrorAutorizacion);

            return;
        }

        Page.Validate("RestablecerContrasena");

        if (!Page.IsValid)
        {
            OcultarMensaje();
            return;
        }

        if (!string.Equals(
            txtNuevaContrasena.Text,
            txtConfirmarContrasena.Text,
            StringComparison.Ordinal))
        {
            MostrarError(
                CodigoErrorContrasenasNoCoinciden,
                MensajeErrorContrasenasNoCoinciden);

            return;
        }

        try
        {
            SessionManager sesion =
                SessionManager.Instancia;

            RecuperacionContrasenaBLL recuperacionBLL =
                new RecuperacionContrasenaBLL();

            recuperacionBLL.ActualizarContrasena(
                sesion.IdUsuarioRecuperacion.Value,
                txtNuevaContrasena.Text);

            SecurityManager seguridad =
                new SecurityManager();

            seguridad.RegistrarBitacora(
                new BitacoraBE
                {
                    IdUsuario =
                        sesion.IdUsuarioRecuperacion.Value,
                    Fecha = DateTime.Now,
                    Accion =
                        "Contraseña restablecida",
                    Modulo =
                        "Autenticación",
                    Criticidad =
                        "Informativa"
                });

            sesion.FinalizarRecuperacionContrasena();

            Response.Redirect(
                ResolveUrl("~/Login.aspx"),
                false);

            Context.ApplicationInstance.CompleteRequest();

            return;
        }
        catch (SqlException)
        {
            MostrarError(
                CodigoErrorConexion,
                MensajeErrorConexion);
        }
        catch (ConfigurationErrorsException)
        {
            MostrarError(
                CodigoErrorConexion,
                MensajeErrorConexion);
        }
    }

    private bool TieneAutorizacionRecuperacion()
    {
        SessionManager sesion =
            SessionManager.Instancia;

        return sesion.RecuperacionContrasenaAutorizada &&
            sesion.IdUsuarioRecuperacion.HasValue;
    }

    private void MostrarError(
        string codigo,
        string mensajeAlternativo)
    {
        try
        {
            SecurityManager seguridad =
                new SecurityManager();

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
        catch (ConfigurationErrorsException)
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
            TextosUI.Traducir(codigo) + " - " + TextosUI.Error(codigo, descripcion));

        pnlMensaje.Visible = true;
    }

    private void OcultarMensaje()
    {
        litMensaje.Text = string.Empty;
        pnlMensaje.Visible = false;
    }
}
