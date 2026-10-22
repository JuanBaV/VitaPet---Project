using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
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
    private List<DisponibilidadTurnoBE> disponibilidad;
    private readonly HashSet<DateTime> diasDisponibles = new HashSet<DateTime>();
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
            if(!Consulta){ucFiltroZona.Vincular(new SolicitudTurnoBLL().Zonas());LimpiarEspecialidades();}
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
    private void LimpiarSolicitud(bool horarios=true)
    {
        Agenda=0;Fecha=default(DateTime);Horario=string.Empty;btnConfirmarSolicitud.Enabled=false;
        lblTurnoSeleccionado.Visible=false;lblConfirmacion.Visible=false;
        if(horarios)rblHorarios.Items.Clear();
    }
    private void LimpiarCalendario()
    {
        LimpiarSolicitud();pnlCalendario.Visible=false;calDisponibilidad.SelectedDates.Clear();diasDisponibles.Clear();
    }
    private void LimpiarEspecialidades()
    {
        ddlEspecialidad.Items.Clear();
        ddlEspecialidad.Items.Add(new ListItem(TextosUI.Obtener("Turno_SeleccionarEspecialidad"),string.Empty));
        ddlEspecialidad.Enabled=false;
    }
    private void LimpiarConsulta(){gvTurnos.DataSource=null;gvTurnos.DataBind();btnExportarXml.Enabled=false;}
    protected void SeleccionarMascota(object sender,EventArgs e)
    {
        disponibilidad=null;LimpiarCalendario();LimpiarEspecialidades();LimpiarConsulta();
        EjecutarPlan(()=>{
            var propia=new MascotaBLL().Seleccionar(Mascota);ViewState["TurnoMascotaNombre"]=propia.Nombre;
            if(Consulta)CargarConsulta();
            else {ucFiltroZona.LimpiarSeleccion();ucFiltroZona.Habilitado=true;}
        });
    }
    private List<DisponibilidadTurnoBE> ObtenerDisponibilidad()
    {
        if(disponibilidad==null)
        {
            var r=Servicio(()=>new TurnoService().ObtenerDisponibilidadPorZona(Mascota,ucFiltroZona.ZonaSeleccionada));
            Validar(r.CodigoError);if(r.Turnos==null)throw new TurnoException("ER065");
            disponibilidad=r.Turnos;
        }
        return disponibilidad;
    }
    private List<DisponibilidadTurnoBE> DisponibilidadEspecialidad()
    {
        return ObtenerDisponibilidad().Where(t=>string.Equals(t.Prestacion,ddlEspecialidad.SelectedValue,StringComparison.Ordinal)).ToList();
    }
    protected void CambiarZona(object sender,EventArgs e)
    {
        disponibilidad=null;LimpiarCalendario();LimpiarEspecialidades();
        if(string.IsNullOrEmpty(ucFiltroZona.ZonaSeleccionada))return;
        EjecutarPlan(()=>{
            foreach(string especialidad in ObtenerDisponibilidad().Select(t=>t.Prestacion).Distinct().OrderBy(p=>p))
                ddlEspecialidad.Items.Add(new ListItem(especialidad,especialidad));
            ddlEspecialidad.Enabled=ddlEspecialidad.Items.Count>1;
        });
    }
    protected void CambiarEspecialidad(object sender,EventArgs e)
    {
        LimpiarCalendario();
        if(string.IsNullOrEmpty(ddlEspecialidad.SelectedValue))return;
        EjecutarPlan(()=>{
            var turnos=DisponibilidadEspecialidad();
            pnlCalendario.Visible=true;
            lblDisponibilidad.Text=TextosUI.Obtener(turnos.Count==0?"Turno_SinHorarios":"Turno_SeleccionarFecha");
            if(turnos.Count>0)calDisponibilidad.VisibleDate=turnos.Min(t=>t.Fecha).Date;
        });
    }
    protected void MostrarDia(object sender,DayRenderEventArgs e)
    {
        bool disponible=diasDisponibles.Contains(e.Day.Date.Date);
        e.Day.IsSelectable=disponible;
        e.Cell.CssClass+=(disponible?" turnos-dia-disponible":" turnos-dia-no-disponible");
        e.Cell.ToolTip=TextosUI.Obtener(disponible?"Turno_DiaDisponible":"Turno_DiaSinDisponibilidad");
    }
    protected void CambiarMes(object sender,MonthChangedEventArgs e)
    {
        LimpiarSolicitud();calDisponibilidad.SelectedDates.Clear();
        lblDisponibilidad.Text=TextosUI.Obtener("Turno_SeleccionarFecha");
    }
    private static string ClaveHorario(DisponibilidadTurnoBE turno)
    {return turno.IdAgendaTurno.ToString(CultureInfo.InvariantCulture)+"|"+turno.Horario;}
    private void CargarHorarios()
    {
        var turnos=DisponibilidadEspecialidad().Where(t=>t.Fecha.Date==calDisponibilidad.SelectedDate.Date).OrderBy(t=>t.Horario).ToList();
        rblHorarios.Items.Clear();
        foreach(var turno in turnos)rblHorarios.Items.Add(new ListItem(turno.Horario,ClaveHorario(turno)));
        lblDisponibilidad.Text=turnos.Count==0?TextosUI.Obtener("Turno_SinHorarios"):calDisponibilidad.SelectedDate.ToString("D",CultureInfo.CurrentCulture);
    }
    protected void SeleccionarFecha(object sender,EventArgs e)
    {
        LimpiarSolicitud();EjecutarPlan(CargarHorarios);
    }
    protected void SeleccionarHorario(object sender,EventArgs e)
    {
        string clave=rblHorarios.SelectedValue;LimpiarSolicitud(false);
        EjecutarPlan(()=>{
            var elegido=DisponibilidadEspecialidad().SingleOrDefault(t=>t.Fecha.Date==calDisponibilidad.SelectedDate.Date&&ClaveHorario(t)==clave);
            if(elegido==null)throw new TurnoException("ER013");
            var r=Servicio(()=>new TurnoService().VerificarDisponibilidadTurno(Mascota,elegido.IdAgendaTurno,elegido.Fecha,elegido.Horario));
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
            var t=r.Turno;LimpiarCalendario();ddlEspecialidad.SelectedIndex=0;disponibilidad=null;
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
        if(!Consulta&&pnlCalendario.Visible&&!pnlMensaje.Visible)EjecutarPlan(()=>{
            diasDisponibles.UnionWith(DisponibilidadEspecialidad().Select(t=>t.Fecha.Date));
        });
        if(pnlMensaje.Visible&&!litMensaje.Text.StartsWith("ER015",StringComparison.Ordinal)){LimpiarSolicitud();diasDisponibles.Clear();}
        if(gvTurnos.HeaderRow!=null)gvTurnos.HeaderRow.TableSection=TableRowSection.TableHeader;
        base.OnPreRender(e);
    }
    protected override void Render(HtmlTextWriter writer){if(!descargando)base.Render(writer);}
}
