using VitaPet.GUI.Presentacion;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Estados;
using VitaPet.BLL.Seguridad;
using VitaPet.SECURITY;

public partial class LoginPage : VitaPet.GUI.Presentacion.PaginaConIdioma
{
    private const string RutaHome =
        "~/Home.aspx";
    private const string AccionLoginExitoso =
        "Login exitoso";
    private const string AccionLoginFallido =
        "Login fallido";
    private const string AccionUsuarioBloqueado =
        "Usuario bloqueado";
    private const string ModuloAutenticacion =
        "Autenticaci\u00F3n";
    private const string CriticidadAdvertencia =
        "Advertencia";
    private const string CriticidadInformativa =
        "Informativa";
    private const string CodigoErrorConexion = "ER003";
    private const string CodigoErrorCredenciales = "ER018";
    private const string CodigoErrorIntegridad = "ER023";
    private const string CodigoErrorEstadoUsuario = "ER024";
    private const string CodigoErrorDatosIncompletos = "ER026";
    private const string CodigoErrorDatosInvalidos = "ER025";
    private const string MensajeErrorConexion = "Fallo de conexión";
    private const string MensajeErrorCredenciales =
        "Credenciales incorrectas";
    private const string MensajeErrorIntegridad =
        "Inconsistencia en la verificación de integridad";
    private const string MensajeErrorEstadoUsuario =
        "Usuario inactivo o bloqueado";
    private const string MensajeErrorDatosIncompletos =
        "Datos incompletos";
    private const string MensajeErrorDatosInvalidos =
        "Datos inválidos";

    protected override void OnPreInit(EventArgs e)
    {
        // Detectar antes de validar credenciales o despachar un POST del login.
        var contingencia = new ContingenciaWebMasterBLL();
        if (contingencia.RequiereAccesoExterno())
        {
            Response.Redirect(ResolveUrl("~/RecuperacionWebMaster.aspx"), true);
            return;
        }
        base.OnPreInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ConfigurarMensajesValidacionCliente();

        if (!IsPostBack && Request.QueryString["restaurado"] == "1")
        {
            MostrarMensaje(
                "Restauración completada",
                "La integridad fue verificada. Inicie sesión nuevamente.");
        }
    }
    protected void btnIngresar_Click(object sender, EventArgs e)
    {
        Page.Validate("Login");
        

        if (!Page.IsValid)
        {
            MostrarErrorValidacion();
            return;
        }

        try
        {
            SecurityManager seguridad = new SecurityManager();

            if (!seguridad.VerificarIntegridad())
            {
                UsuarioBE webmaster = seguridad.AutenticarRecuperacion(ucCorreo.Correo, txtContrasena.Text);
                if (webmaster != null)
                {
                    SessionManager.Instancia.IniciarSesion(webmaster);
                    try { new BitacoraBLL().RegistrarErroresIntegridad(); }
                    catch { SessionManager.Instancia.CerrarSesion(); throw; }
                    Response.Redirect(ResolveUrl("~/BaseDatos.aspx"), false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }
                MostrarError(
                    seguridad,
                    CodigoErrorIntegridad,
                    MensajeErrorIntegridad);

                return;
            }

            UsuarioBLL usuarioBLL = new UsuarioBLL();

            UsuarioBE usuario = usuarioBLL.ObtenerPorCorreo(
                ucCorreo.Correo);

            if (usuario != null && usuario.RequiereRestablecerContrasena && usuario.Estado && !usuario.Bloqueado)
            {
                // Solo deriva al recorrido existente; no autoriza la recuperación.
                Session.Clear();
                Session["CorreoRestablecimientoObligatorio"] = usuario.CorreoElectronico;
                Response.Redirect(ResolveUrl("~/OlvideContrasena.aspx"),false);
                Context.ApplicationInstance.CompleteRequest();return;
            }

            string hashContrasena = seguridad.CalcularHash(
                txtContrasena.Text);

            bool credencialesCoinciden =
                usuario != null &&
                string.Equals(
                    usuario.Contrasena,
                    hashContrasena,
                    StringComparison.OrdinalIgnoreCase);

            if (!credencialesCoinciden)
            {
                if (usuario != null)
                {
                    usuarioBLL.RegistrarIntentoFallido(usuario);
                }

                BitacoraBE evento = new BitacoraBE
                {
                    IdUsuario = usuario?.IdUsuario,
                    Fecha = DateTime.Now,
                    Accion =
                        usuario != null && usuario.Bloqueado
                            ? AccionUsuarioBloqueado
                            : AccionLoginFallido,
                    Modulo = ModuloAutenticacion,
                    Criticidad = CriticidadAdvertencia
                };

                seguridad.RegistrarBitacora(evento);

                MostrarError(
                    seguridad,
                    usuario != null && usuario.Bloqueado ? CodigoErrorEstadoUsuario : CodigoErrorCredenciales,
                    usuario != null && usuario.Bloqueado ? MensajeErrorEstadoUsuario : MensajeErrorCredenciales);

                return;
            }

            UsuarioStateBLL estadoUsuario =
                new UsuarioStateBLL(usuario);

            if (!estadoUsuario.PuedeIniciarSesion())
            {
                BitacoraBE eventoEstadoUsuario =
                    new BitacoraBE
                    {
                        IdUsuario = usuario.IdUsuario,
                        Fecha = DateTime.Now,
                        Accion = usuario.Bloqueado
                            ? AccionUsuarioBloqueado
                            : AccionLoginFallido,
                        Modulo = ModuloAutenticacion,
                        Criticidad = CriticidadAdvertencia
                    };

                seguridad.RegistrarBitacora(
                    eventoEstadoUsuario);

                MostrarError(
                    seguridad,
                    CodigoErrorEstadoUsuario,
                    MensajeErrorEstadoUsuario);

                return;
            }

            if (usuario.IntentosFallidos > 0)
            {
                usuarioBLL.RegistrarIngresoExitoso(usuario);
            }

            SessionManager.Instancia.IniciarSesion(usuario); //login con singleton

            BitacoraBE eventoLoginExitoso =
                new BitacoraBE
                {
                    IdUsuario = usuario.IdUsuario,
                    Fecha = DateTime.Now,
                    Accion = AccionLoginExitoso,
                    Modulo = ModuloAutenticacion,
                    Criticidad = CriticidadInformativa
                };

            seguridad.RegistrarBitacora(
                eventoLoginExitoso);

            Response.Redirect(
                ObtenerDestinoPosteriorAlLogin(),
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
        catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException ||
            ex is System.Xml.XmlException || ex is BitacoraException)
        {
            MostrarMensaje("ER033", "No se pudo registrar el diagnóstico en bitácora");
        }
    }

    private void ConfigurarMensajesValidacionCliente()
    {
        string datosIncompletos = ObtenerMensajeValidacion(
            CodigoErrorDatosIncompletos,
            MensajeErrorDatosIncompletos);

        string datosInvalidos = ObtenerMensajeValidacion(
            CodigoErrorDatosInvalidos,
            MensajeErrorDatosInvalidos);

        pnlMensaje.Attributes["data-error-incompletos"] =
            datosIncompletos;
        pnlMensaje.Attributes["data-error-invalidos"] =
            datosInvalidos;
    }

    private string ObtenerMensajeValidacion(
        string codigo,
        string mensajeAlternativo)
    {
        try
        {
            ErrorSistemaBE error =
                new SecurityManager().ObtenerError(codigo);

            return error == null
                ? codigo + " - " + TextosUI.Error(codigo, mensajeAlternativo)
                : error.Codigo + " - " + TextosUI.Error(error.Codigo, error.Descripcion);
        }
        catch (SqlException)
        {
            return codigo + " - " + TextosUI.Error(codigo, mensajeAlternativo);
        }
        catch (ConfigurationErrorsException)
        {
            return codigo + " - " + TextosUI.Error(codigo, mensajeAlternativo);
        }
    }
    private void MostrarErrorValidacion()
    {
        bool existenDatosIncompletos =
            string.IsNullOrWhiteSpace(ucCorreo.Correo) ||
            string.IsNullOrWhiteSpace(txtContrasena.Text);

        string codigo = existenDatosIncompletos
            ? CodigoErrorDatosIncompletos
            : CodigoErrorDatosInvalidos;

        string mensajeAlternativo = existenDatosIncompletos
            ? MensajeErrorDatosIncompletos
            : MensajeErrorDatosInvalidos;

        try
        {
            MostrarError(
                new SecurityManager(),
                codigo,
                mensajeAlternativo);
        }
        catch (SqlException)
        {
            MostrarMensaje(codigo, mensajeAlternativo);
        }
        catch (ConfigurationErrorsException)
        {
            MostrarMensaje(codigo, mensajeAlternativo);
        }
    }
    private string ObtenerDestinoPosteriorAlLogin()
    {
        string returnUrl =
            Request.QueryString["ReturnUrl"];

        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith(
                "/",
                StringComparison.Ordinal) ||
            returnUrl.StartsWith(
                "//",
                StringComparison.Ordinal) ||
            returnUrl.Contains("\\"))
        {
            return ResolveUrl(RutaHome);
        }

        return returnUrl;
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

        MostrarMensaje(codigo, mensajeAlternativo);
    }

    private void MostrarMensaje(
        string codigo,
        string descripcion)
    {
        litMensaje.Text = Server.HtmlEncode(
            TextosUI.Traducir(codigo) + " - " + TextosUI.Error(codigo, descripcion));

        pnlMensaje.Style.Remove("display");
    }

    private void OcultarMensaje()
    {
        litMensaje.Text = string.Empty;
        pnlMensaje.Style["display"] = "none";
    }
}
