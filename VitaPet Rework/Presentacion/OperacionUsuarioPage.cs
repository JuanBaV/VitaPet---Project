using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.SECURITY;

namespace VitaPet.GUI.Presentacion
{
    public abstract class OperacionUsuarioPage : PaginaBase
    {
        [Serializable]
        private sealed class ContextoOperacion
        {
            public int Id;
            public OperacionUsuario Operacion;
            public EdicionUsuarioBE Datos;
        }
        private const string Prefijo = "Usuarios:Operacion:";
        private ContextoOperacion operacion;
        private bool autorizado;
        private string errorInicial;
        private string Clave { get { return Prefijo + (Request.QueryString["operacion"] ?? ""); } }
        protected abstract OperacionUsuario Operacion { get; }
        protected override string[] RolesPermitidos { get { return new[] { "Webmaster" }; } }

        public static void Abrir(Page pagina, int id, OperacionUsuario tipo)
        {
            var claves = pagina.Session.Keys.Cast<string>().Where(k => k.StartsWith(Prefijo, StringComparison.Ordinal)).ToList();
            foreach (var clave in claves.Take(Math.Max(0, claves.Count - 9))) pagina.Session.Remove(clave);
            string token = Guid.NewGuid().ToString("N");
            pagina.Session[Prefijo + token] = new ContextoOperacion { Id=id, Operacion=tipo };
            pagina.Response.Redirect(pagina.ResolveUrl("~/" + NombrePagina(tipo) + ".aspx") + "?operacion=" + token, true);
        }

        private static string NombrePagina(OperacionUsuario tipo)
        {
            return tipo == OperacionUsuario.Consultar ? "ConsultarUsuario" : tipo == OperacionUsuario.Modificar ? "ModificarUsuario" :
                tipo == OperacionUsuario.Bloquear ? "BloquearUsuario" : "DesbloquearUsuario";
        }

        protected override void OnPreInit(EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
            Response.Cache.SetCacheability(HttpCacheability.NoCache); Response.Cache.SetNoStore();
            try
            {
                if (!SessionManager.Instancia.HaySesionActiva) Response.Redirect(ResolveUrl("~/Login.aspx"), true);
                new SecurityManager().ExigirPermiso(System.IO.Path.GetFileNameWithoutExtension(AppRelativeVirtualPath));
                autorizado = true;
            }
            catch (UnauthorizedAccessException) { Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"), true); }
            catch (SqlException) { errorInicial = "ER003"; }
            catch (ConfigurationErrorsException) { errorInicial = "ER003"; }
            catch (InvalidOperationException) { errorInicial = "ER023"; }
            base.OnPreInit(e);
        }

        protected override void EjecutarFuncionalidad()
        {
            Control<Panel>("pnlMensaje").Visible = false;
            Control<Literal>("litMensaje").Text = string.Empty;
            if (!autorizado) { MostrarError(errorInicial ?? "ER003"); return; }
            operacion = Session[Clave] as ContextoOperacion;
            if (operacion == null || operacion.Operacion != Operacion) { MostrarError("ER036"); return; }
            if (!IsPostBack) Ejecutar(() => {
                operacion.Datos = null;
                operacion.Datos = new UsuarioBLL().PrepararOperacionUsuario(operacion.Id, Operacion);
                Vincular(operacion.Datos.Usuario);
            });
            else if (operacion.Datos == null) MostrarError("ER036");
        }

        protected void Guardar_Click(object sender, EventArgs e)
        {
            Ejecutar(() => {
                if (operacion == null || operacion.Datos == null || operacion.Operacion != Operacion) return;
                var bll = new UsuarioBLL();
                if (Operacion == OperacionUsuario.Modificar)
                {
                    bool estado;
                    if (!bool.TryParse(Control<DropDownList>("ddlEstado").SelectedValue, out estado))
                        throw new ConsultaUsuariosException("ER025", "Estado inválido.");
                    bll.ModificarUsuario(operacion.Id, operacion.Datos.Version, new CambiosUsuarioBE {
                        Nombre=Control<TextBox>("txtNombre").Text, CorreoElectronico=Control<TextBox>("txtCorreo").Text,
                        IdRol=int.Parse(Control<DropDownList>("ddlRol").SelectedValue), Estado=estado
                    });
                }
                else if (Operacion == OperacionUsuario.Bloquear) bll.BloquearUsuarioAdministrado(operacion.Id);
                else if (Operacion == OperacionUsuario.Desbloquear) bll.DesbloquearUsuarioAdministrado(operacion.Id);
                else return;
                Session["Usuarios:Resultado"] = Operacion == OperacionUsuario.Modificar ? "El usuario se modificó correctamente." :
                    Operacion == OperacionUsuario.Bloquear ? "El usuario se bloqueó correctamente." : "El usuario se desbloqueó correctamente.";
                var actual = SessionManager.Instancia.UsuarioActual;
                if (actual != null && (!actual.Estado || actual.Bloqueado || actual.Rol != "Webmaster"))
                {
                    Session.Remove(Clave);
                    Control<Panel>("pnlDatos").Visible = false;
                    Control<Button>("btnCancelar").Visible = false;
                    Control<LinkButton>("lnkVolverFormulario").Visible = false;
                    Control<Literal>("litMensaje").Text = Server.HtmlEncode(TextosUI.Traducir((string)Session["Usuarios:Resultado"]));
                    Control<Panel>("pnlMensaje").CssClass = "alert alert-success";
                    Control<Panel>("pnlMensaje").Visible = true;
                    Control<HyperLink>("lnkInicio").Visible = true;
                    Session.Remove("Usuarios:Resultado");
                    if (!actual.Estado || actual.Bloqueado) SessionManager.Instancia.CerrarSesion();
                    return;
                }
                Volver_Click(sender, e);
            });
        }

        protected void Volver_Click(object sender, EventArgs e)
        {
            Session.Remove(Clave);
            Response.Redirect(ResolveUrl("~/GestionUsuarios.aspx"), true);
        }

        private void Vincular(UsuarioListadoBE u)
        {
            Control<Panel>("pnlDatos").Visible = true;
            var detalles = Control<DetailsView>("dvUsuario");
            if (detalles != null) { detalles.DataSource = new[] { u }; detalles.DataBind(); return; }
            Etiqueta("lblIdUsuario", u.IdUsuario.ToString()); Etiqueta("lblBloqueado", u.Bloqueado ? "Bloqueado" : "Desbloqueado");
            if (Operacion == OperacionUsuario.Modificar)
            {
                var roles=Control<DropDownList>("ddlRol");roles.Items.Clear();
                foreach(var perfil in new PerfilBLL().Consultar().OfType<RolBE>())roles.Items.Add(new ListItem(perfil.Nombre,perfil.Id.ToString()));
                roles.Enabled=SessionManager.Instancia.UsuarioActual.Rol=="Webmaster";
                Control<TextBox>("txtNombre").Text = u.Nombre; Control<TextBox>("txtCorreo").Text = u.CorreoElectronico;
                Control<DropDownList>("ddlRol").SelectedValue = u.IdRol.ToString(); Control<DropDownList>("ddlEstado").SelectedValue = u.Estado.ToString();
            }
            else
            {
                Etiqueta("lblNombre", u.Nombre); Etiqueta("lblCorreo", u.CorreoElectronico);
                Etiqueta("lblRol", u.Rol); Etiqueta("lblEstado", u.Estado ? "Activo" : "Inactivo");
            }
        }

        private void Etiqueta(string id, string texto) { Control<Label>(id).Text = Server.HtmlEncode(TextosUI.Traducir(texto)); }
        private T Control<T>(string id) where T : Control { return Buscar(this, id) as T; }
        private static Control Buscar(Control padre, string id)
        {
            if (padre.ID == id) return padre;
            foreach (Control hijo in padre.Controls) { var encontrado = Buscar(hijo, id); if (encontrado != null) return encontrado; }
            return null;
        }

        private void Ejecutar(Action accion)
        {
            if (!autorizado) return;
            try { accion(); }
            catch (ConsultaUsuariosException ex) { MostrarError(ex.Codigo); }
            catch (UnauthorizedAccessException) { Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"), true); }
        }

        private void MostrarError(string codigo)
        {
            string mensaje;
            switch (codigo)
            {
                case "ER001": mensaje="Correo ya registrado."; break;
                case "ER002": mensaje="Datos incompletos."; break;
                case "ER003": mensaje="Fallo de conexión."; break;
                case "ER023": mensaje="Inconsistencia en la verificación de integridad."; break;
                case "ER025": mensaje="Datos inválidos. Revisá el nombre, correo, rol y estado."; break;
                case "ER033": mensaje="No se pudo registrar la consulta en bitácora."; break;
                case "ER036": mensaje="El usuario seleccionado ya no existe o la selección venció. Volvé a seleccionarlo en Gestión de usuarios."; break;
                case "ER037": mensaje="El estado actual del usuario no permite realizar la operación."; break;
                case "ER038": mensaje="Los datos del usuario cambiaron desde su consulta. Volvé a cargar los datos antes de editar."; break;
                case "ER040": mensaje="No se pudo registrar la operación en bitácora."; break;
                default: mensaje="No se pudo actualizar el usuario."; break;
            }
            Control<Literal>("litMensaje").Text = Server.HtmlEncode(codigo + " - " + TextosUI.Error(codigo, mensaje));
            Control<Panel>("pnlMensaje").Visible = true;
            if (codigo != "ER001" && codigo != "ER002" && codigo != "ER025")
            {
                foreach (string id in new[] { "btnGuardar", "btnConfirmarBloqueo", "btnConfirmarDesbloqueo" })
                { var boton=Control<Button>(id); if (boton != null) boton.Enabled=false; }
                var recargar = Control<HyperLink>("lnkRecargar");
                recargar.Visible = autorizado && operacion != null;
                recargar.NavigateUrl = ResolveUrl("~/" + NombrePagina(Operacion) + ".aspx") + "?operacion=" + Server.UrlEncode(Request.QueryString["operacion"]);
            }
        }
    }
}
