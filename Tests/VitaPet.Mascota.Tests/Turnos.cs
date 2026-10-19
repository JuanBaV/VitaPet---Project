using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Protocols;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.GUI.WebServices;
partial class Program
{
    static void TurnoError(Action action,string code)
    {try{action();throw new Exception("Expected "+code);}catch(TurnoException ex){Check(ex.Codigo==code,"Turnos informa "+code);}}
    static void ProbarTurnos()
    {
        var sec=new SecurityManager();var solicitud=new SolicitudTurnoBLL();var consulta=new ConsultaTurnosBLL();var ws=new TurnoService();
        Check(Count("AgendaTurno")==12&&sec.VerificarIntegridad(),"Agenda de 12 combinaciones inicializada de forma idempotente con DVH/DVV/referencias");
        Check((int)Scalar("SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'sp_AgendaTurno_%' OR name LIKE 'sp_Turno_%'")==6,"Seis SP nuevos instalados");
        Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER065")!=null&&new ErrorSistemaDAL().ObtenerPorCodigo("ER066")!=null,"ER065 y ER066 registrados");
        foreach(int actor in new[]{0,2,3})
        {
            SessionAs(actor);
            foreach(Action operacion in new Action[]{()=>solicitud.Zonas(),()=>solicitud.Disponibilidad(1,"CABA"),()=>solicitud.Verificar(1,1,DateTime.Today.AddDays(1),"10:00"),()=>solicitud.Registrar(1,1,DateTime.Today.AddDays(1),"10:00"),()=>consulta.Consultar(1),()=>consulta.ExportarXml(1)})
            {bool denied=false;try{operacion();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Turnos exclusivo Cliente: "+actor);}
            bool soap=false;try{ws.ObtenerTurnosMascota(1);}catch(SoapException){soap=true;}Check(soap,"SOAP requiere Cliente autenticado: "+actor);
        }
        SessionAs(5);TurnoError(()=>solicitud.Zonas(),"ER011");SessionAs(1);
        Check(solicitud.Zonas().SequenceEqual(new[]{"CABA","Norte","Oeste","Sur"}),"Zonas locales obtenidas desde SQL");
        TurnoError(()=>solicitud.Disponibilidad(0,"CABA"),"ER002");TurnoError(()=>solicitud.Disponibilidad(1,""),"ER002");
        TurnoError(()=>solicitud.Disponibilidad(1,"Inexistente"),"ER025");TurnoError(()=>solicitud.Disponibilidad(4,"CABA"),"ER011");
        TurnoError(()=>consulta.Consultar(1),"ER014");TurnoError(()=>consulta.ExportarXml(1),"ER014");
        var datos=ws.ObtenerDisponibilidadPorZona(1,"CABA");
        Check(datos.CodigoError==null&&datos.Turnos.Count>100,"Webservice devuelve disponibilidad local por zona");
        Check(datos.Turnos.All(t=>t.Zona=="CABA"&&t.Fecha<DateTime.Today.AddDays(30)&&t.Fecha>=DateTime.Today&&t.Fecha.DayOfWeek!=DayOfWeek.Sunday&&TimeSpan.Parse(t.Horario).Hours>=9&&TimeSpan.Parse(t.Horario).Hours<18&&TimeSpan.Parse(t.Horario).Minutes%30==0&&t.Fecha.Add(TimeSpan.Parse(t.Horario))>DateTime.Now),"Agenda futura de 30 dias, lunes a sabado, 09 a 18 y bloques de 30 minutos");
        Check(datos.Turnos.Select(t=>t.Prestacion).Distinct().Count()==3,"Incluye consulta general, vacunacion y control preventivo");
        var elegido=datos.Turnos.First(t=>t.Fecha>DateTime.Today);int eventos=Count("Bitacora");
        Check(ws.VerificarDisponibilidadTurno(1,elegido.IdAgendaTurno,elegido.Fecha,elegido.Horario).Turno!=null&&Count("Turno")==0&&Count("Bitacora")==eventos,"Seleccion verifica disponibilidad sin reservar ni registrar");
        TurnoError(()=>solicitud.Verificar(1,0,elegido.Fecha,elegido.Horario),"ER002");
        foreach(string horario in new[]{"29:00","9:00","09:00:00","x","09:01"})TurnoError(()=>solicitud.Verificar(1,elegido.IdAgendaTurno,elegido.Fecha,horario),"ER025");
        TurnoError(()=>solicitud.Verificar(1,elegido.IdAgendaTurno,DateTime.Today.AddDays(-1),"09:00"),"ER013");
        TurnoError(()=>solicitud.Verificar(1,elegido.IdAgendaTurno,DateTime.Today.AddDays(100),"09:00"),"ER025");
        TurnoError(()=>solicitud.Verificar(4,elegido.IdAgendaTurno,elegido.Fecha,elegido.Horario),"ER011");
        TurnoError(()=>solicitud.Verificar(1,99999,elegido.Fecha,elegido.Horario),"ER013");
        var r=ws.RegistrarTurno(1,elegido.IdAgendaTurno,elegido.Fecha,elegido.Horario);
        Check(r.CodigoError==null&&r.Turno.IdTurno>0&&r.Turno.IdMascota==1&&r.Turno.Estado=="Confirmado"&&Count("Turno")==1,"Registra el turno confirmado para la mascota propia");
        Check(Count("Bitacora")==eventos+1&&new BitacoraDAL().ObtenerParaIntegridad().Last().Accion=="Solicitud de turno veterinario"&&sec.VerificarIntegridad(),"Solicitud auditada y verificadores validos");
        Check(ws.RegistrarTurno(1,elegido.IdAgendaTurno,elegido.Fecha,elegido.Horario).CodigoError=="ER013"&&Count("Turno")==1,"Repetir confirmacion no duplica la reserva");
        Check(!solicitud.Disponibilidad(1,"CABA").Any(t=>t.IdAgendaTurno==elegido.IdAgendaTurno&&t.Fecha==elegido.Fecha&&t.Horario==elegido.Horario),"Un turno confirmado desaparece de la disponibilidad");
        var listado=ws.ObtenerTurnosMascota(1);Check(listado.CodigoError==null&&listado.Mascota.Nombre=="Luna"&&listado.Turnos.Count==1,"Consulta propia mediante webservice");
        Check(new BitacoraDAL().ObtenerParaIntegridad().Last().Accion=="Consulta de turnos"&&sec.VerificarIntegridad(),"Consulta auditada sin modificar los turnos");
        Check(ws.ObtenerTurnosMascota(2).CodigoError=="ER014"&&ws.ObtenerTurnosMascota(4).CodigoError=="ER011","Sin turnos o mascota ajena informa el error correspondiente");
        byte[] bytes=consulta.ExportarXml(1);var xml=new XmlDocument();xml.LoadXml(Encoding.UTF8.GetString(bytes));
        Check(xml.DocumentElement.Name=="Turnos"&&xml.SelectNodes("/Turnos/Turno").Count==1&&xml.SelectSingleNode("//idTurno").InnerText==r.Turno.IdTurno.ToString(),"XML descargable con solo los turnos de la mascota propia");
        Check(xml.SelectSingleNode("//prestacion")!=null&&xml.SelectSingleNode("//fecha").InnerText==elegido.Fecha.ToString("yyyy-MM-dd")&&xml.SelectSingleNode("//horario").InnerText==elegido.Horario&&xml.SelectSingleNode("//zona").InnerText=="CABA"&&xml.SelectSingleNode("//estado").InnerText=="Confirmado","XML contiene todos los campos documentados y fecha interoperable");
        var siguiente=solicitud.Disponibilidad(1,"CABA").First(t=>t.Fecha>DateTime.Today);
        Sql("CREATE TRIGGER dbo.TestTurnoRegistroFail ON dbo.Turno AFTER INSERT AS THROW 50066,'Test booking failure',1;");
        try{TurnoError(()=>solicitud.Registrar(1,siguiente.IdAgendaTurno,siguiente.Fecha,siguiente.Horario),"ER066");}finally{Sql("DROP TRIGGER dbo.TestTurnoRegistroFail");}
        Check(Count("Turno")==1&&sec.VerificarIntegridad(),"Falla de registro revierte el turno y sus controles");
        Sql("CREATE TRIGGER dbo.TestTurnoAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
        try
        {
            TurnoError(()=>solicitud.Registrar(1,siguiente.IdAgendaTurno,siguiente.Fecha,siguiente.Horario),"ER040");
            TurnoError(()=>consulta.Consultar(1),"ER033");
        }
        finally{Sql("DROP TRIGGER dbo.TestTurnoAuditFail");}
        Check(Count("Turno")==1&&sec.VerificarIntegridad(),"Falla de bitacora revierte la reserva completa y deja disponible el cupo");
        int exitos=0,ocupados=0;
        Parallel.For(0,2,i=>{SessionAs(i==0?1:4);try{new SolicitudTurnoBLL().Registrar(i==0?1:4,siguiente.IdAgendaTurno,siguiente.Fecha,siguiente.Horario);Interlocked.Increment(ref exitos);}catch(TurnoException ex){if(ex.Codigo!="ER013")throw;Interlocked.Increment(ref ocupados);}});
        SessionAs(1);Check(exitos==1&&ocupados==1&&Count("Turno")==2&&sec.VerificarIntegridad(),"Dos clientes concurrentes no pueden reservar el mismo turno");
        FalloSp("sp_Turno_ConsultarMascota","ER065",()=>Check(ws.ObtenerTurnosMascota(1).CodigoError=="ER065","Falla de consulta del servicio informa ER065"));
        Sql("UPDATE dbo.AgendaTurno SET horaInicio='00:00',horaFin='00:30',horizonteDias=1 WHERE zona='Sur'");Refrescar("AgendaTurno");
        Check(ws.ObtenerDisponibilidadPorZona(1,"Sur").CodigoError=="ER012","Agenda sin cupos futuros informa ER012");
        Sql("UPDATE dbo.AgendaTurno SET horaInicio='09:00',horaFin='18:00',horizonteDias=30 WHERE zona='Sur'");Refrescar("AgendaTurno");
        Sql("UPDATE dbo.Turno SET prestacion=prestacion+CHAR(1) WHERE idTurno="+r.Turno.IdTurno);Refrescar("Turno");
        TurnoError(()=>consulta.ExportarXml(1),"ER015");
        Sql("UPDATE dbo.Turno SET prestacion=REPLACE(prestacion,CHAR(1),'') WHERE idTurno="+r.Turno.IdTurno);Refrescar("Turno");
        Sql("UPDATE dbo.AgendaTurno SET horizonteDias=29 WHERE idAgendaTurno=1");TurnoError(()=>solicitud.Zonas(),"ER023");
        Check(ws.ObtenerTurnosMascota(1).CodigoError=="ER023","Corrupcion de agenda bloquea tambien las consultas");
        Sql("UPDATE dbo.AgendaTurno SET horizonteDias=30 WHERE idAgendaTurno=1");Check(sec.VerificarIntegridad(),"Valor original restaura integridad sin recalcular controles");
        SessionAs(4);TurnoError(()=>consulta.ExportarXml(1),"ER011");SessionAs(1);
        Sql("DELETE dbo.AgendaTurno");Refrescar("AgendaTurno");
        Check(solicitud.Zonas().Count==4&&Count("AgendaTurno")==12&&sec.VerificarIntegridad(),"Agenda vacia tras restauracion historica se inicializa sin reemplazar datos existentes");
        var cfg=System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
        var original=cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString;
        try
        {
            var missing=new SqlConnectionStringBuilder(original){InitialCatalog=new SqlConnectionStringBuilder(original).InitialCatalog+"_Ausente"};
            cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=missing.ConnectionString;cfg.Save();System.Configuration.ConfigurationManager.RefreshSection("connectionStrings");
            TurnoError(()=>solicitud.Disponibilidad(1,"CABA"),"ER003");
        }
        finally{cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=original;cfg.Save();System.Configuration.ConfigurationManager.RefreshSection("connectionStrings");}
        Check(sec.VerificarIntegridad(),"Pruebas terminan con integridad valida");
    }
}
