using System;
using System.Globalization;
using System.Net;
using System.Web;
using System.Web.Services.Protocols;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.GUI.WebServices;
public partial class TurnosPage : PaginaClientePlanes
{
    private bool descargando;
    private bool Consulta {get{return string.Equals(Request.QueryString["vista"],"consulta",StringComparison.Ordinal);}}
    private int Mascota {get{return ucSelectorMascota.IdMascotaSeleccionada;}}
    private int Agenda {get{return ViewState["TurnoAgenda"]==null?0:(int)ViewState["TurnoAgenda"];}set{ViewState["TurnoAgenda"]=value;}}
    private DateTime Fecha {get{return ViewState["TurnoFecha"]==null?default(DateTime):(DateTime)ViewState["TurnoFecha"];}set{ViewState["TurnoFecha"]=value;}}
    private string Horario {get{return (string)ViewState["TurnoHorario"]??string.Empty;}set{ViewState["TurnoHorario"]=value;}}
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;pnlSolicitud.Visible=!Consulta;pnlConsulta.Visible=Consulta;
        litTitulo.Text=Server.HtmlEncode(TextosUI.Obtener(Consulta?"Turno_Consultar":"Turno_Solicitar"));
        (Consulta?lnkConsultar:lnkSolicitar).CssClass="btn btn-primary";
        if(!IsPostBack)EjecutarPlan(()=>{
            ucSelectorMascota.Vincular(new MascotaBLL().Listar());
            if(!Consulta)ucFiltroZona.Vincular(new SolicitudTurnoBLL().Zonas());
        });
    }
    private static T Servicio<T>(Func<T> llamada) where T:class
    {
        try{var r=llamada();if(r==null)throw new TurnoException("ER065");return r;}
        catch(SoapException ex){if(ex.Code==SoapException.ClientFaultCode)throw new UnauthorizedAccessException("Acceso denegado al servicio de turnos.");throw new TurnoException("ER065",ex);}
        catch(HttpException ex){throw new TurnoException("ER065",ex);}
        catch(WebException ex){throw new TurnoException("ER065",ex);}
    }
    private static void Validar(string codigo){if(!string.IsNullOrEmpty(codigo))throw new TurnoException(codigo);}
    private void LimpiarSolicitud(bool listado=true)
    {
        Agenda=0;Fecha=default(DateTime);Horario=string.Empty;btnConfirmarSolicitud.Enabled=false;
        lblTurnoSeleccionado.Visible=false;lblConfirmacion.Visible=false;gvDisponibilidad.SelectedIndex=-1;
        if(listado){gvDisponibilidad.DataSource=null;gvDisponibilidad.DataBind();}
    }
    private void LimpiarConsulta(){gvTurnos.DataSource=null;gvTurnos.DataBind();btnExportarXml.Enabled=false;}
    protected void SeleccionarMascota(object sender,EventArgs e)
    {
        LimpiarSolicitud();LimpiarConsulta();
        EjecutarPlan(()=>{
            var propia=new MascotaBLL().Seleccionar(Mascota);ViewState["TurnoMascotaNombre"]=propia.Nombre;
            if(Consulta)CargarConsulta();
            else {ucFiltroZona.LimpiarSeleccion();ucFiltroZona.Habilitado=true;btnBuscarDisponibilidad.Enabled=true;}
        });
    }
    protected void CambiarZona(object sender,EventArgs e){LimpiarSolicitud();}
    private void CargarDisponibilidad(int pagina)
    {
        var r=Servicio(()=>new TurnoService().ObtenerDisponibilidadPorZona(Mascota,ucFiltroZona.ZonaSeleccionada));Validar(r.CodigoError);
        if(r.Turnos==null)throw new TurnoException("ER065");
        gvDisponibilidad.PageIndex=Math.Max(0,Math.Min(pagina,(r.Turnos.Count-1)/gvDisponibilidad.PageSize));
        gvDisponibilidad.DataSource=r.Turnos;gvDisponibilidad.DataBind();
    }
    protected void BuscarDisponibilidad(object sender,EventArgs e){LimpiarSolicitud();EjecutarPlan(()=>CargarDisponibilidad(0));}
    protected void PaginarDisponibilidad(object sender,GridViewPageEventArgs e){LimpiarSolicitud();EjecutarPlan(()=>CargarDisponibilidad(e.NewPageIndex));}
    protected void SeleccionarTurno(object sender,EventArgs e)
    {
        // Leer la clave protegida por ViewState antes de limpiar la selección anterior.
        var clave=gvDisponibilidad.SelectedDataKey;LimpiarSolicitud(false);
        EjecutarPlan(()=>{
            if(clave==null)throw new TurnoException("ER002");
            var r=Servicio(()=>new TurnoService().VerificarDisponibilidadTurno(Mascota,(int)clave.Values["IdAgendaTurno"],(DateTime)clave.Values["Fecha"],(string)clave.Values["Horario"]));
            Validar(r.CodigoError);if(r.Turno==null)throw new TurnoException("ER065");
            Agenda=r.Turno.IdAgendaTurno;Fecha=r.Turno.Fecha;Horario=r.Turno.Horario;
            lblTurnoSeleccionado.Text=Server.HtmlEncode(r.Turno.Prestacion+" · "+Fecha.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)+" · "+Horario+" · "+r.Turno.Zona);
            lblTurnoSeleccionado.Visible=true;btnConfirmarSolicitud.Enabled=true;
        });
    }
    protected void ConfirmarSolicitud(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            if(Agenda<=0)throw new TurnoException("ER002");
            var r=Servicio(()=>new TurnoService().RegistrarTurno(Mascota,Agenda,Fecha,Horario));Validar(r.CodigoError);
            if(r.Turno==null)throw new TurnoException("ER065");
            var t=r.Turno;LimpiarSolicitud();
            lblConfirmacion.Text=Server.HtmlEncode(string.Format(CultureInfo.CurrentCulture,TextosUI.Obtener("Turno_Confirmacion"),
                t.IdTurno,(string)ViewState["TurnoMascotaNombre"],t.Prestacion,t.Fecha.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),t.Horario,t.Zona));
            lblConfirmacion.Visible=true;
        });
    }
    private void CargarConsulta()
    {
        var r=Servicio(()=>new TurnoService().ObtenerTurnosMascota(Mascota));Validar(r.CodigoError);
        if(r.Mascota==null||r.Turnos==null)throw new TurnoException("ER065");
        gvTurnos.DataSource=r.Turnos;gvTurnos.DataBind();btnExportarXml.Enabled=true;
    }
    protected void ExportarXml(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            // Consumir la operación definida en el CU y volver a verificar los datos al generar el XML.
            var r=Servicio(()=>new TurnoService().ObtenerTurnosMascota(Mascota));Validar(r.CodigoError);
            if(r.Mascota==null||r.Turnos==null)throw new TurnoException("ER065");
            byte[] contenido=new ConsultaTurnosBLL().ExportarXml(Mascota);
            Response.Clear();Response.ContentType="application/xml";Response.ContentEncoding=System.Text.Encoding.UTF8;Response.Charset="utf-8";
            Response.AddHeader("Content-Disposition","attachment; filename=Turnos.xml");Response.Cache.SetCacheability(HttpCacheability.NoCache);Response.Cache.SetNoStore();
            Response.BinaryWrite(contenido);descargando=true;Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void OnPreRender(EventArgs e)
    {
        if(pnlMensaje.Visible&&!litMensaje.Text.StartsWith("ER015",StringComparison.Ordinal)){LimpiarSolicitud();LimpiarConsulta();}
        foreach(var grid in new[]{gvDisponibilidad,gvTurnos})if(grid.HeaderRow!=null)grid.HeaderRow.TableSection=TableRowSection.TableHeader;
        base.OnPreRender(e);
    }
    protected override void Render(HtmlTextWriter writer){if(!descargando)base.Render(writer);}
}
