using VitaPet.GUI.Presentacion;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web.UI;
using VitaPet.BLL;
using VitaPet.SECURITY;
public partial class RecuperacionWebMasterPage : VitaPet.GUI.Presentacion.PaginaConIdioma
{
    private readonly ContingenciaWebMasterBLL contingencia = new ContingenciaWebMasterBLL();
    protected override void OnPreInit(EventArgs e)
    {
        // La URL no habilita la contingencia: revalidar también los POST directos.
        if (!contingencia.Configurada || !contingencia.Necesaria())
        {
            Response.Redirect(ResolveUrl("~/Login.aspx"), true);
            return;
        }
        base.OnPreInit(e);
    }

    protected override void OnInit(EventArgs e)
    {
        ViewStateUserKey = Session.SessionID;
        Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        base.OnInit(e);
    }
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack) Ejecutar(() =>
        {
            if (SessionManager.Instancia.HaySesionContingencia) Cargar();
        });
    }
    protected void Ingresar_Click(object sender, EventArgs e)
    {
        Ejecutar(() =>
        {
            if (!contingencia.Autenticar(txtCorreo.Text.Trim(), txtClave.Text))
            { Mensaje("No se pudo autorizar el acceso. Revise sus credenciales o espere si hubo intentos fallidos."); return; }
            // Nueva carga para descartar cualquier formulario anterior a la autenticación.
            Response.Redirect(ResolveUrl("~/RecuperacionWebMaster.aspx"), false);
            Context.ApplicationInstance.CompleteRequest();
        });
    }
    private void Cargar()
    {
        var puntos = contingencia.Listar();
        ddlRespaldos.DataSource = puntos.Select(b => new { b.Id, Descripcion=TextosUI.Traducir(b.Descripcion) }); ddlRespaldos.DataTextField = "Descripcion";
        ddlRespaldos.DataValueField = "Id"; ddlRespaldos.DataBind();
        pnlAcceso.Visible = false; pnlRestaurar.Visible = true; btnSeleccionar.Enabled = puntos.Count > 0;
        if (puntos.Count == 0) Mensaje("No hay respaldos XML válidos disponibles.");
    }
    protected void Seleccionar_Click(object sender, EventArgs e)
    {
        Ejecutar(() =>
        {
            var punto = contingencia.Listar().Single(b => b.Id == ddlRespaldos.SelectedValue);
            string token = Guid.NewGuid().ToString("N");
            Session["TokenContingencia"] = token; Session["RespaldoContingencia"] = punto.Id;
            ViewState["TokenContingencia"] = token; pnlConfirmar.Visible = true;
            litConfirmacion.Text = Server.HtmlEncode(TextosUI.Formato("ConfirmarRestauracionPunto", TextosUI.Traducir(punto.Descripcion)));
        });
    }
    protected void Confirmar_Click(object sender, EventArgs e)
    {
        Ejecutar(() =>
        {
            string token = ViewState["TokenContingencia"] as string;
            string id = Session["RespaldoContingencia"] as string;
            if (token == null || id == null || token != Session["TokenContingencia"] as string)
                throw new InvalidOperationException("La confirmación venció. Seleccione nuevamente el respaldo.");
            Session.Remove("TokenContingencia"); Session.Remove("RespaldoContingencia"); pnlConfirmar.Visible = false;
            contingencia.Restaurar(id);
            Response.Redirect(ResolveUrl("~/Login.aspx?restaurado=1"), false); Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected void Cancelar_Click(object sender, EventArgs e)
    { Session.Remove("TokenContingencia"); Session.Remove("RespaldoContingencia"); pnlConfirmar.Visible = false; }
    protected void Salir_Click(object sender, EventArgs e)
    {
        SessionManager.Instancia.CerrarSesion();
        Response.Redirect(ResolveUrl("~/Login.aspx"), false); Context.ApplicationInstance.CompleteRequest();
    }
    private void Ejecutar(Action accion)
    {
        try { accion(); }
        catch (UnauthorizedAccessException) { pnlRestaurar.Visible = false; pnlAcceso.Visible = true; Mensaje("Acceso de emergencia no autorizado o vencido."); }
        catch (CryptographicException) { Mensaje("No se pudo verificar el material criptográfico de la cuenta de emergencia."); }
        catch (IOException) { Mensaje("No se pudo acceder a un respaldo válido. La recuperación no se completó."); }
        catch (SqlException) { Mensaje("No se pudo completar la recuperación en SQL Server. Verifique su disponibilidad y reintente."); }
        catch (InvalidOperationException) { Mensaje("La operación no pudo completarse. Seleccione un respaldo válido y vuelva a intentarlo."); }
        catch (ArgumentException) { Mensaje("Los datos de recuperación no son válidos."); }
    }
    private void Mensaje(string texto) { pnlMensaje.Visible = true; litMensaje.Text = Server.HtmlEncode(TextosUI.Traducir(texto)); }
}
