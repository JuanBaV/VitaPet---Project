using System;
using System.Web;
using VitaPet2._0;
using System.Web.Optimization;
using System.Web.Routing;

namespace VitaPet.GUI
{
    public class Global : HttpApplication
    {
            protected void Application_Start(object sender, EventArgs e)
            {
                // Un fallo de SQL no debe impedir cargar la única vía de recuperación externa.
                try
                {
                    var contingencia = new VitaPet.BLL.ContingenciaWebMasterBLL();
                    // Antes de migrar se validan los datos y los scripts ya aplicados; los nuevos aún están pendientes.
                    if (!VitaPet.SECURITY.EstadoAplicacion.Mantenimiento && !(contingencia.Configurada && contingencia.Necesaria(true)))
                    {
                        new VitaPet.BLL.MigracionRolesBLL().Ejecutar();
                        new VitaPet.BLL.ReferenciaIntegridadBLL().Inicializar();
                        new VitaPet.BLL.PerfilBLL().AsegurarIniciales();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresRestauracion();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresIdioma();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresRegistroCliente();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresPlanes();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresPago();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresFacturacion();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresCobertura();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresReintegros();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresRegistroMascota();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresGestionMascotas();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresTurnos();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresEmpresas();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresClienteCorporativo();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresAdministracionPlanes();
                        new VitaPet.BLL.ErrorSistemaBLL().AsegurarErroresPerfilesUsuarios();
                        new VitaPet.BLL.SolicitudTurnoBLL().AsegurarAgendaPrueba();
                        new VitaPet.BLL.ContratacionPlanBLL().AsegurarCatalogo();
                    }
                }
                catch (System.Data.SqlClient.SqlException ex) { System.Diagnostics.Trace.TraceError("Inicio VitaPet: " + ex.GetType().Name); }
                catch (InvalidOperationException ex) { System.Diagnostics.Trace.TraceError("Inicio VitaPet: " + ex.GetType().Name); }
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
            }
        
            protected void Application_PreRequestHandlerExecute(object sender, EventArgs e)
            {
                if (!(Context.Handler is System.Web.UI.Page)) return;
                string pagina = VirtualPathUtility.GetFileName(((System.Web.UI.Page)Context.Handler).AppRelativeVirtualPath);
                bool recuperacion = string.Equals(pagina, "RecuperacionWebMaster.aspx", StringComparison.OrdinalIgnoreCase);
                bool login = string.Equals(pagina, "Login.aspx", StringComparison.OrdinalIgnoreCase);
                var bloqueo = VitaPet.SECURITY.EstadoAplicacion.BloquearSolicitud(recuperacion || login);
                if (bloqueo == null) throw new HttpException(503, "Hay una operación en curso o mantenimiento. Use Recuperar acceso WebMaster desde el inicio de sesión.");
                Context.Items["BloqueoVitaPet"] = bloqueo;
                // Antes de cualquier evento de página: tampoco se admiten POST a módulos normales.
                if (VitaPet.SECURITY.SessionManager.Instancia.HaySesionContingencia && !recuperacion)
                    throw new HttpException(403, "La sesión de emergencia sólo permite recuperar el sistema.");
                if (recuperacion) return;
                if (VitaPet.SECURITY.EstadoAplicacion.Mantenimiento && !login)
                    throw new HttpException(503, "La base de datos está en mantenimiento.");
                string pantalla = System.IO.Path.GetFileNameWithoutExtension(pagina);
                if (VitaPet.BLL.PerfilBLL.Catalogo.ContainsKey(pantalla))
                {
                    try
                    {
                        // Antes de despachar cualquier evento, también para POST y URLs directas.
                        if (!new VitaPet.BLL.PerfilBLL().PuedeAcceder(pantalla))
                            Response.Redirect(VirtualPathUtility.ToAbsolute(VitaPet.SECURITY.SessionManager.Instancia.HaySesionActiva ? "~/AccesoDenegado.aspx" : "~/Login.aspx"), true);
                    }
                    catch (System.Data.SqlClient.SqlException) { /* La página muestra el error de conexión. */ }
                }
                if (string.Equals(pagina, "Login.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "RegistroCliente.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "AccesoDenegado.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "CatalogoErrores.aspx", StringComparison.OrdinalIgnoreCase)) return;
                // ConsultaBitacora authorizes in OnPreInit and displays its own centralized errors.
                // This also keeps connection/integrity errors inside the use-case flow.
                if (string.Equals(pagina, "ConsultaBitacora.aspx", StringComparison.OrdinalIgnoreCase)) return;
                // Logout validates access before events and displays its own integrity/connection errors.
                if (string.Equals(pagina, "Logout.aspx", StringComparison.OrdinalIgnoreCase)) return;
                // Gestión de usuarios authorizes before events and owns its error messages.
                if (string.Equals(pagina, "GestionUsuarios.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "ConsultarUsuario.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "ModificarUsuario.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "BloquearUsuario.aspx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina, "DesbloquearUsuario.aspx", StringComparison.OrdinalIgnoreCase)) return;
                // Estas páginas autorizan antes de los eventos y muestran errores del CU.
                if (string.Equals(pagina,"AltaPlan.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"GestionPlanes.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ConsultaPlan.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ModificarPlan.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"Planes.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"Cliente.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ContratarPlan.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"GestionPago.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ConsultaCobertura.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"Reintegros.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"RegistroMascota.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"Mascotas.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ActualizarMascota.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ConsultaMascota.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"HistorialClinico.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"DatosClinicos.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"Turnos.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"Empresas.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"RegistroClienteCorporativo.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"AltaEmpresa.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ConsultaEmpresa.aspx",StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pagina,"ModificarEmpresa.aspx",StringComparison.OrdinalIgnoreCase)) return;
                var seguridad = new VitaPet.BLL.Seguridad.SecurityManager();
                if (string.Equals(pagina, "BaseDatos.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    // Antes del ciclo de vida de la página: bloquea también POST directo.
                    try { seguridad.ExigirWebmaster("BaseDatos"); }
                    catch (UnauthorizedAccessException)
                    {
                        string destino = VitaPet.SECURITY.SessionManager.Instancia.HaySesionActiva
                            ? "~/AccesoDenegado.aspx" : "~/Login.aspx";
                        Response.Redirect(VirtualPathUtility.ToAbsolute(destino), false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                    return;
                }
                if (!seguridad.VerificarIntegridad())
                {
                    var usuario = VitaPet.SECURITY.SessionManager.Instancia.UsuarioActual;
                    if (usuario != null && seguridad.EsWebmasterConfiable(usuario))
                        Response.Redirect(VirtualPathUtility.ToAbsolute("~/BaseDatos.aspx"), true);
                    throw new HttpException(503, "ER023 - Inconsistencia en la verificación de integridad.");
                }
            }
            protected void Application_EndRequest(object sender, EventArgs e)
            {
                var bloqueo = Context.Items["BloqueoVitaPet"] as IDisposable;
                if (bloqueo != null) { bloqueo.Dispose(); Context.Items.Remove("BloqueoVitaPet"); }
            }
    }
}
