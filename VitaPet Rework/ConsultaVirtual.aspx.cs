using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;
using VitaPet.SERVICES;

public partial class ConsultaVirtualPage : PaginaClientePlanes
{
    private bool habilitado;
    private string codigoError;
    private string accionCompletada;

    private CultureInfo culturaInterfaz;

    protected string Texto(string clave)
    { return VitaPet.GUI.Resources.Textos.ResourceManager.GetString("ConsultaVirtual_" + clave, culturaInterfaz) ?? clave; }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);
        culturaInterfaz = CultureInfo.GetCultureInfo(SessionManager.Instancia.ObtenerIdiomaUsuario());
        Title = Texto("Titulo");
        btnNuevaConsulta.Attributes["title"] = Texto("Nueva");
        btnEnviarConsulta.Attributes["title"] = Texto("Enviar");
        btnEnviarConsulta.Attributes["aria-label"] = Texto("Enviar");
        if (Session["ConsultaVirtualCsrf"] == null)
        {
            var bytes = new byte[32];
            using (var generador = RandomNumberGenerator.Create()) generador.GetBytes(bytes);
            Session["ConsultaVirtualCsrf"] = Convert.ToBase64String(bytes);
        }
        hfTokenConsulta.Value = (string)Session["ConsultaVirtualCsrf"];
        var ajax = ScriptManager.GetCurrent(this);
        ajax.AsyncPostBackTimeout = 60;
        ajax.RegisterAsyncPostBackControl(btnEnviarConsulta);
        ajax.RegisterAsyncPostBackControl(btnNuevaConsulta);
        consultaVirtualTexto.Attributes["maxlength"] = ConsultaVirtualBLL.MaximoTexto.ToString(CultureInfo.InvariantCulture);
        consultaVirtualTexto.Attributes["placeholder"] = Texto("Escribir");
    }

    protected override void EjecutarFuncionalidad()
    {
        if (!IsPostBack) EjecutarSeguro(() => ObtenerConversacion(false));
    }

    protected void NuevaConsulta(object sender, EventArgs e)
    {
        EjecutarSeguro(() =>
        {
            ComprobarSolicitud();
            ObtenerConversacion(true);
            accionCompletada = "limpiar";
        });
    }

    protected void EnviarConsulta(object sender, EventArgs e)
    { RegisterAsyncTask(new PageAsyncTask(EnviarAsync)); }

    private async Task EnviarAsync()
    {
        try
        {
            ComprobarSolicitud();
            var conversacion = ObtenerConversacion(false);
            if (conversacion.UltimoEnvioUtc.HasValue &&
                DateTime.UtcNow - conversacion.UltimoEnvioUtc.Value < TimeSpan.FromSeconds(10))
                throw new ConsultaVirtualException("Espera");

            var mensaje = new ConsultaVirtualBLL().PrepararMensaje(consultaVirtualTexto.Text, DecodificarFoto(hfFotoBase64.Value));
            var historial = conversacion.Mensajes.ToList();
            if (historial.Count >= 12) historial.RemoveRange(0, historial.Count - 10);
            historial.Add(mensaje);
            var servicio = new GeminiConsultaService(Server.MapPath("~/App_Data/secrets.env"));
            conversacion.UltimoEnvioUtc = DateTime.UtcNow;
            Session["ConsultaVirtualConversacion"] = conversacion;
            // La tarea de página espera la respuesta sin retener transacción ni bloqueo de mantenimiento.
            var bloqueoSolicitud = Context.Items["BloqueoVitaPet"] as IDisposable;
            if (bloqueoSolicitud != null)
            {
                Context.Items.Remove("BloqueoVitaPet");
                bloqueoSolicitud.Dispose();
            }
            string respuesta = await servicio.ConsultarAsync(historial, SessionManager.Instancia.ObtenerIdiomaUsuario());
            historial.Add(new MensajeConsultaVirtualBE { Rol = "model", Texto = respuesta });
            conversacion.Mensajes = historial;
            Session["ConsultaVirtualConversacion"] = conversacion;
            accionCompletada = "mensaje";
        }
        catch (Exception ex) { MostrarFallo(ex); }
    }

    private ConversacionVirtualBE ObtenerConversacion(bool nueva)
    {
        if (!SessionManager.Instancia.HaySesionActiva || SessionManager.Instancia.HaySesionContingencia)
            throw new ConsultaVirtualException("Sesion");
        var conversacion = Session["ConsultaVirtualConversacion"] as ConversacionVirtualBE;
        using (var bloqueo = EstadoAplicacion.BloquearOperacion())
        {
            if (bloqueo == null) throw new ConsultaVirtualException("Verificacion");
            var bll = new ConsultaVirtualBLL();
            bool iniciar = nueva || conversacion == null ||
                conversacion.IdUsuario != SessionManager.Instancia.UsuarioActual.IdUsuario;
            var usuario = iniciar ? bll.IniciarConsulta() : bll.ExigirAcceso();
            if (iniciar)
            {
                // Reemplazar el estado sólo después de confirmar la bitácora con DVH, DVV y referencias.
                conversacion = new ConversacionVirtualBE
                {
                    IdUsuario = usuario.IdUsuario,
                    UltimoEnvioUtc = conversacion != null && conversacion.IdUsuario == usuario.IdUsuario
                        ? conversacion.UltimoEnvioUtc : null
                };
                Session["ConsultaVirtualConversacion"] = conversacion;
            }
        }
        habilitado = true;
        return conversacion;
    }

    private void ComprobarSolicitud()
    {
        string esperado = Session["ConsultaVirtualCsrf"] as string;
        if (Request.HttpMethod != "POST" || string.IsNullOrEmpty(esperado) ||
            !string.Equals(esperado, hfTokenConsulta.Value, StringComparison.Ordinal))
            throw new ConsultaVirtualException("Acceso");
    }

    private static byte[] DecodificarFoto(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return null;
        int maximoCodificado = 4 * ((ConsultaVirtualBLL.MaximoImagenBytes + 2) / 3);
        if (base64.Length > maximoCodificado) throw new ConsultaVirtualException("ImagenGrande");
        try
        {
            byte[] foto = Convert.FromBase64String(base64);
            if (foto.Length > ConsultaVirtualBLL.MaximoImagenBytes) throw new ConsultaVirtualException("ImagenGrande");
            return foto;
        }
        catch (FormatException) { throw new ConsultaVirtualException("ImagenInvalida"); }
    }

    private void EjecutarSeguro(Action accion)
    { try { accion(); } catch (Exception ex) { MostrarFallo(ex); } }

    private void MostrarFallo(Exception ex)
    {
        var consulta = ex as ConsultaVirtualException;
        var agente = ex as GeminiConsultaException;
        codigoError = consulta != null ? consulta.Codigo : agente != null ? agente.Codigo :
            ex is UnauthorizedAccessException ? "Acceso" :
            ex is IOException || ex is System.Security.SecurityException ? "SinConfigurar" : "NoDisponible";
        if (new[] { "SinPlan", "Acceso", "Sesion", "SinConfigurar", "Verificacion", "Bitacora" }.Contains(codigoError))
            habilitado = false;
        if (consulta == null && agente == null && !(ex is UnauthorizedAccessException) && !(ex is IOException))
            System.Diagnostics.Trace.TraceError("Consulta virtual: error interno.");
    }

    protected override void OnPreRenderComplete(EventArgs e)
    {
        // Las tareas asíncronas de la página ya terminaron; renderizar sólo texto codificado en el Repeater.
        var conversacion = Session["ConsultaVirtualConversacion"] as ConversacionVirtualBE;
        var usuarioActual = SessionManager.Instancia.UsuarioActual;
        if (habilitado && (usuarioActual == null || !SessionManager.Instancia.HaySesionActiva))
        {
            habilitado = false;
            codigoError = "Sesion";
            accionCompletada = null;
        }
        var mensajes = habilitado && conversacion != null &&
            usuarioActual != null && conversacion.IdUsuario == usuarioActual.IdUsuario
            ? conversacion.Mensajes : new System.Collections.Generic.List<MensajeConsultaVirtualBE>();
        rptMensajes.DataSource = mensajes.Select(m => new
        {
            EsCliente = m.Rol == "user", Autor = Texto(m.Rol == "user" ? "Tu" : "Asistente"),
            m.Texto, TieneFoto = !string.IsNullOrEmpty(m.ImagenBase64)
        });
        rptMensajes.DataBind();
        if (mensajes.Count > 0) bienvenida.Attributes["hidden"] = "hidden";
        else bienvenida.Attributes.Remove("hidden");
        estadoServidor.Attributes["data-habilitado"] = habilitado ? "true" : "false";
        estadoServidor.Attributes["data-accion"] = accionCompletada ?? "";
        estadoServidor.Attributes["data-codigo"] = codigoError ?? "";
        ofertaPlanes.Visible = codigoError == "SinPlan";
        mensajeEstado.InnerText = codigoError == null ? "" :
            VitaPet.GUI.Resources.Textos.ResourceManager.GetString(codigoError == "Bitacora" ? "Error_ER033" : "ConsultaVirtual_Error_" + codigoError, culturaInterfaz) ?? Texto("Error_NoDisponible");
        mensajeEstado.Attributes["class"] = "consulta-virtual-estado" + (codigoError == null ? "" : " es-error");
        // No conservar fotografías ni borradores en ViewState ni reflejarlos en el HTML de respuesta.
        hfFotoBase64.Value = "";
        base.OnPreRenderComplete(e);
    }
}
