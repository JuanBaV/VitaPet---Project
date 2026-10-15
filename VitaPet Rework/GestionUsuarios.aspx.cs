using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class GestionUsuariosPage : PaginaBase
{
    private const string PrefijoConsulta = "GestionUsuarios:";
    private SecurityManager seguridad;
    private UsuarioBLL usuarios;
    private bool autorizado;
    private string errorInicial;
    protected override string[] RolesPermitidos { get { return new[] { "Webmaster" }; } }

    // Identificador de la selección validada por el servidor.
    protected int? IdUsuarioSeleccionado
    {
        get { return ViewState["UsuarioSeleccionado"] as int?; }
        private set { ViewState["UsuarioSeleccionado"] = value; }
    }

    protected override void OnPreInit(EventArgs e)
    {
        ViewStateUserKey = Session.SessionID;
        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        try
        {
            if (!SessionManager.Instancia.HaySesionActiva)
                Response.Redirect(ResolveUrl("~/Login.aspx") + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), true);
            seguridad = new SecurityManager();
            seguridad.ExigirWebmaster();
            usuarios = new UsuarioBLL();
            autorizado = true;
        }
        catch (UnauthorizedAccessException) { DenegarAcceso(); }
        catch (SqlException) { errorInicial = "ER003"; }
        catch (ConfigurationErrorsException) { errorInicial = "ER003"; }
        catch (InvalidOperationException) { errorInicial = "ER023"; }
        base.OnPreInit(e);
    }

    protected override void MostrarMensajes()
    {
        pnlMensaje.Visible = false;
        litMensaje.Text = string.Empty;
    }

    protected override void EjecutarFuncionalidad()
    {
        if (!autorizado) { btnBuscar.Enabled = false; MostrarError(errorInicial ?? "ER003"); return; }
        if (!IsPostBack) Ejecutar(() => {
            var retorno = Session["Usuarios:FiltroRetorno"] as FiltroUsuariosBE;
            Session.Remove("Usuarios:FiltroRetorno");
            if (retorno != null) { txtBuscarUsuario.Text = retorno.Nombre; ddlEstado.SelectedValue = retorno.Estado.ToString(); }
            Consultar(LeerFiltros());
            var aviso = Session["Usuarios:Resultado"] as string;
            Session.Remove("Usuarios:Resultado");
            if (aviso != null) { pnlResultado.Visible = true; litResultado.Text = Server.HtmlEncode(TextosUI.Traducir(aviso)); }
        });
    }

    protected void Buscar_Click(object sender, EventArgs e)
    {
        Ejecutar(() => Consultar(LeerFiltros()));
    }

    protected void Usuarios_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        Ejecutar(() => {
            var filtro = LeerFiltros();
            var consulta = ObtenerConsulta();
            if (consulta == null || !Iguales(consulta.Filtro, filtro)) { Consultar(filtro); return; }
            seguridad.ExigirIntegridad();
            LimpiarSeleccion();
            Vincular(consulta, e.NewPageIndex);
        });
    }

    protected void Usuarios_SelectedIndexChanged(object sender, EventArgs e)
    {
        Ejecutar(() => {
            var filtro = LeerFiltros();
            var consulta = ObtenerConsulta();
            if (consulta == null || !Iguales(consulta.Filtro, filtro)) { Consultar(filtro); return; }
            if (gvUsuarios.SelectedDataKey == null) { LimpiarSeleccion(); return; }
            int id = Convert.ToInt32(gvUsuarios.SelectedDataKey.Value);
            var seleccionado = usuarios.ObtenerUsuarioSeleccionado(id, consulta.Filtro);
            if (seleccionado == null) { Consultar(filtro); return; }
            int indice = consulta.Resultado.Usuarios.FindIndex(u => u.IdUsuario == id);
            if (indice < 0) { Consultar(filtro); return; }
            consulta.Resultado.Usuarios[indice] = seleccionado;
            Vincular(consulta, gvUsuarios.PageIndex);
            IdUsuarioSeleccionado = id;
            litSeleccion.Text = Server.HtmlEncode(seleccionado.Nombre + " · " + seleccionado.CorreoElectronico);
            btnBloquear.Visible = seleccionado.PuedeBloquear;
            btnDesbloquear.Visible = seleccionado.PuedeDesbloquear;
            pnlAcciones.Visible = true;
        });
    }

    protected void Consultar_Click(object sender, EventArgs e) { AbrirOperacion(OperacionUsuario.Consultar); }
    protected void Modificar_Click(object sender, EventArgs e) { AbrirOperacion(OperacionUsuario.Modificar); }
    protected void Bloquear_Click(object sender, EventArgs e) { AbrirOperacion(OperacionUsuario.Bloquear); }
    protected void Desbloquear_Click(object sender, EventArgs e) { AbrirOperacion(OperacionUsuario.Desbloquear); }

    private void AbrirOperacion(OperacionUsuario operacion)
    {
        Ejecutar(() => {
            if (!IdUsuarioSeleccionado.HasValue) throw new ConsultaUsuariosException("ER036", "Seleccioná un usuario.");
            var filtro = LeerFiltros();
            var seleccionado = usuarios.ObtenerUsuarioSeleccionado(IdUsuarioSeleccionado.Value, filtro);
            if (seleccionado == null) throw new ConsultaUsuariosException("ER036", "El usuario seleccionado ya no existe.");
            Session["Usuarios:FiltroRetorno"] = filtro;
            OperacionUsuarioPage.Abrir(this, seleccionado.IdUsuario, operacion);
        });
    }

    private void Consultar(FiltroUsuariosBE filtro)
    {
        LimpiarSeleccion();
        var consulta = new ConsultaGuardada { Filtro = filtro, Resultado = usuarios.ConsultarUsuarios(filtro) };
        string clave = ViewState["ConsultaId"] as string;
        if (clave == null)
        {
            var anteriores = Session.Keys.Cast<string>().Where(k => k.StartsWith(PrefijoConsulta, StringComparison.Ordinal)).ToList();
            foreach (var anterior in anteriores.Take(Math.Max(0, anteriores.Count - 4))) Session.Remove(anterior);
            clave = PrefijoConsulta + Guid.NewGuid().ToString("N");
            ViewState["ConsultaId"] = clave;
        }
        Session[clave] = consulta;
        Vincular(consulta, 0);
        if (consulta.Resultado.Usuarios.Count == 0)
            MostrarError(consulta.Resultado.HayUsuariosRegistrados ? "ER028" : "ER020");
    }

    private ConsultaGuardada ObtenerConsulta()
    {
        string clave = ViewState["ConsultaId"] as string;
        return clave == null ? null : Session[clave] as ConsultaGuardada;
    }

    private void Vincular(ConsultaGuardada consulta, int pagina)
    {
        int total = consulta.Resultado.Usuarios.Count;
        int paginas = Math.Max(1, (total + gvUsuarios.PageSize - 1) / gvUsuarios.PageSize);
        gvUsuarios.PageIndex = Math.Max(0, Math.Min(pagina, paginas - 1));
        gvUsuarios.DataSource = consulta.Resultado.Usuarios;
        gvUsuarios.DataBind();
        if (gvUsuarios.HeaderRow != null) gvUsuarios.HeaderRow.TableSection = TableRowSection.TableHeader;
        litResultados.Text = TextosUI.Formato("ResumenUsuarios", total, gvUsuarios.PageIndex + 1, paginas);
    }

    private FiltroUsuariosBE LeerFiltros()
    {
        EstadoFiltroUsuario estado;
        if (!Enum.TryParse(ddlEstado.SelectedValue, out estado))
            throw new ConsultaUsuariosException("ER025", "Estado de filtro inválido.");
        return UsuarioBLL.NormalizarFiltro(new FiltroUsuariosBE { Nombre = txtBuscarUsuario.Text, Estado = estado });
    }

    private static bool Iguales(FiltroUsuariosBE a, FiltroUsuariosBE b)
    {
        return a.Nombre == b.Nombre && a.Estado == b.Estado;
    }

    private void LimpiarSeleccion()
    {
        IdUsuarioSeleccionado = null;
        gvUsuarios.SelectedIndex = -1;
        pnlAcciones.Visible = false;
        litSeleccion.Text = string.Empty;
    }

    private void Ejecutar(Action accion)
    {
        if (!autorizado) return;
        try { accion(); }
        catch (ConsultaUsuariosException ex) { MostrarError(ex.Codigo); }
        catch (UnauthorizedAccessException) { DenegarAcceso(); }
        catch (SqlException) { MostrarError("ER003"); }
        catch (ConfigurationErrorsException) { MostrarError("ER003"); }
        catch (TransactionException) { MostrarError("ER032"); }
        catch (InvalidOperationException ex) { MostrarError(ex.Message.StartsWith("ER023", StringComparison.Ordinal) ? "ER023" : "ER032"); }
    }

    private void MostrarError(string codigo)
    {
        string mensaje;
        switch (codigo)
        {
            case "ER003": mensaje = "Fallo de conexión o configuración. Reintentá la operación."; break;
            case "ER020": mensaje = "No existen usuarios registrados."; break;
            case "ER023": mensaje = "Inconsistencia en la verificación de integridad."; break;
            case "ER025": mensaje = "Revisá el nombre y el estado de la búsqueda."; break;
            case "ER028": mensaje = "No se encontraron usuarios que coincidan con los parámetros de filtrado."; break;
            case "ER036": mensaje = "El usuario seleccionado ya no existe."; break;
            case "ER033": mensaje = "No se pudo registrar la consulta en bitácora."; break;
            default: mensaje = "No se pudo completar la operación."; break;
        }
        try { var error = seguridad == null ? null : seguridad.ObtenerError(codigo); if (error != null) mensaje = error.Descripcion; }
        catch (SqlException) { }
        catch (ConfigurationErrorsException) { }
        LimpiarSeleccion();
        if (codigo != "ER020" && codigo != "ER028")
        {
            string clave = ViewState["ConsultaId"] as string;
            if (clave != null) Session.Remove(clave);
            gvUsuarios.DataSource = new UsuarioListadoBE[0]; gvUsuarios.DataBind();
            litResultados.Text = string.Empty;
        }
        litMensaje.Text = Server.HtmlEncode(codigo + " - " + TextosUI.Error(codigo, mensaje));
        pnlMensaje.Visible = true;
    }

    private void DenegarAcceso() { Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"), true); }

    private sealed class ConsultaGuardada
    {
        public FiltroUsuariosBE Filtro { get; set; }
        public ResultadoConsultaUsuariosBE Resultado { get; set; }
    }
}
