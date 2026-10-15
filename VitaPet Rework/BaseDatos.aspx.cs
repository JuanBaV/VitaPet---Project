using System;
using System.IO;
using System.Linq;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;
public partial class BaseDatosPage : PaginaBase
{
    private readonly SecurityManager seguridad = new SecurityManager();
    protected override string[] RolesPermitidos => new[] { "Webmaster" };
    protected override void OnInit(EventArgs e)
    {
        try { seguridad.ExigirWebmaster(); }
        catch (UnauthorizedAccessException) { throw new HttpException(403,"Acceso exclusivo para Webmaster."); }
        ViewStateUserKey=Session.SessionID;
        base.OnInit(e);
    }
    protected override void EjecutarFuncionalidad()
    {
        if(!IsPostBack) Ejecutar(Cargar);
    }
    private void Cargar()
    {
        var resultado=seguridad.DiagnosticarIntegridad();
        grdIntegridad.DataSource=resultado.Detalles; grdIntegridad.DataBind();
        btnCrear.Enabled=resultado.EsValida;
        if(!resultado.EsValida) MostrarError("ER023","Inconsistencia en la verificación de integridad");
        try
        {
            var backups=new BackupBLL().Listar().Where(b=>b.Verificado).ToList();
            ddlBackups.DataSource=backups.Select(b => new { b.Id, Descripcion=TextosUI.Traducir(b.Descripcion) }); ddlBackups.DataTextField="Descripcion"; ddlBackups.DataValueField="Id"; ddlBackups.DataBind();
            btnRestaurar.Enabled=backups.Count>0;
            if(backups.Count==0) MostrarError("ER022","No existen archivos de respaldo");
        }
        catch(IOException) { MostrarError("ER021","Error al obtener los archivos de respaldo"); btnRestaurar.Enabled=false; }
    }
    protected void Verificar_Click(object sender,EventArgs e) { Ejecutar(Cargar); }
    protected void Crear_Click(object sender,EventArgs e)
    {
        Ejecutar(()=> { seguridad.ExigirIntegridad(); Confirmacion("backup",null,TextosUI.Traducir("¿Confirma la creación de un backup completo de la base de datos?")); });
    }
    protected void Restaurar_Click(object sender,EventArgs e)
    {
        Ejecutar(()=>
        {
            var elegido=new BackupBLL().Listar().Single(b=>b.Id==ddlBackups.SelectedValue && b.Verificado);
            Confirmacion("restore",elegido.Id,TextosUI.Formato("ConfirmarRestauracionBackup", TextosUI.Traducir(elegido.Descripcion)));
        });
    }
    private void Confirmacion(string accion,string backup,string texto)
    {
        string token=Guid.NewGuid().ToString("N");
        Session["ConfirmacionBD"]=token; ViewState["TokenBD"]=token;
        ViewState["AccionBD"]=accion; ViewState["BackupBD"]=backup;
        litConfirmacion.Text=Server.HtmlEncode(TextosUI.Traducir(texto)); pnlConfirmacion.Visible=true;
    }
    protected void Cancelar_Click(object sender,EventArgs e) { Session.Remove("ConfirmacionBD"); pnlConfirmacion.Visible=false; }
    protected void Confirmar_Click(object sender,EventArgs e)
    {
        Ejecutar(()=>
        {
            string token=ViewState["TokenBD"] as string;
            if(token==null || token!=(Session["ConfirmacionBD"] as string)) throw new InvalidOperationException("La confirmación venció. Seleccione nuevamente la operación.");
            Session.Remove("ConfirmacionBD"); pnlConfirmacion.Visible=false;
            if((string)ViewState["AccionBD"]=="backup")
            {
                var backup=new BackupBLL().Crear(); Cargar(); Mensaje(TextosUI.Formato("BackupCreado", backup.Id));
            }
            else if((string)ViewState["AccionBD"]=="restore")
            {
                new BackupBLL().Restaurar((string)ViewState["BackupBD"]);
                SessionManager.Instancia.CerrarSesion();
                Response.Redirect(ResolveUrl("~/Login.aspx?restaurado=1"),false); Context.ApplicationInstance.CompleteRequest();
            }
            else throw new InvalidOperationException("Operación inválida.");
        });
    }
    protected void ConfigurarEmergencia_Click(object sender, EventArgs e)
    {
        Ejecutar(() =>
        {
            new ContingenciaWebMasterBLL().Configurar(txtCorreoEmergencia.Text.Trim(), txtClaveEmergencia.Text);
            Response.Redirect(ResolveUrl("~/Login.aspx"), false);
            Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected void XmlEmergencia_Click(object sender, EventArgs e)
    {
        Ejecutar(() => Mensaje(TextosUI.Formato("XmlCreado", new ContingenciaWebMasterBLL().CrearRespaldo())));
    }
    private void Ejecutar(Action accion)
    {
        try { seguridad.ExigirWebmaster(); accion(); }
        catch(UnauthorizedAccessException) { throw new HttpException(403,"Acceso exclusivo para Webmaster."); }
        catch(SqlException) { MostrarError("ER003","Fallo de conexión"); }
        catch(ConfigurationErrorsException) { MostrarError("ER003","Fallo de configuración"); }
        catch(IOException) { MostrarError("ER003","No se pudo completar la operación de respaldo o restauración"); }
        catch(ArgumentException ex) { Mensaje(ex.Message); }
        catch(System.Security.Cryptography.CryptographicException) { Mensaje("No se pudo verificar el material criptográfico de la cuenta de emergencia."); }
        catch(InvalidOperationException ex) { Mensaje(ex.Message); }
    }
    private void MostrarError(string codigo,string alternativa)
    {
        try { var error=seguridad.ObtenerError(codigo); Mensaje(codigo+" - "+TextosUI.Error(codigo, error?.Descripcion ?? alternativa)); }
        catch(SqlException) { Mensaje(codigo+" - "+TextosUI.Error(codigo, alternativa)); }
    }
    private void Mensaje(string texto) { litMensaje.Text=Server.HtmlEncode(TextosUI.Traducir(texto)); pnlMensaje.Visible=true; }
}
