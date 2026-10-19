using System;
using System.ComponentModel;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.SECURITY;
namespace VitaPet.GUI.WebServices
{
    [WebService(Namespace="https://vitapet.local/services/")]
    [WebServiceBinding(ConformsTo=WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class TurnoService : WebService
    {
        private static T ConBloqueo<T>(Func<T> operacion)
        {
            try
            {
                if(HttpContext.Current!=null&&HttpContext.Current.Items["BloqueoVitaPet"]!=null)return operacion();
                using(var bloqueo=EstadoAplicacion.BloquearSolicitud())
                {if(bloqueo==null)throw new HttpException(503,"El sistema se encuentra en mantenimiento o procesando una operación.");return operacion();}
            }
            catch(UnauthorizedAccessException){throw new SoapException("Acceso denegado.",SoapException.ClientFaultCode);}
        }
        [WebMethod(EnableSession=true)]
        public ResultadoDisponibilidadTurnoBE ObtenerDisponibilidadPorZona(int idMascota,string zona)
        {
            return ConBloqueo(()=>{try{return new ResultadoDisponibilidadTurnoBE {Turnos=new SolicitudTurnoBLL().Disponibilidad(idMascota,zona)};}
                catch(TurnoException ex){return new ResultadoDisponibilidadTurnoBE {CodigoError=ex.Codigo};}});
        }
        [WebMethod(EnableSession=true)]
        public ResultadoValidacionTurnoBE VerificarDisponibilidadTurno(int idMascota,int idAgendaTurno,DateTime fecha,string horario)
        {
            return ConBloqueo(()=>{try{return new ResultadoValidacionTurnoBE {Turno=new SolicitudTurnoBLL().Verificar(idMascota,idAgendaTurno,fecha,horario)};}
                catch(TurnoException ex){return new ResultadoValidacionTurnoBE {CodigoError=ex.Codigo};}});
        }
        [WebMethod(EnableSession=true)]
        public ResultadoRegistroTurnoBE RegistrarTurno(int idMascota,int idAgendaTurno,DateTime fecha,string horario)
        {
            return ConBloqueo(()=>{try{return new ResultadoRegistroTurnoBE {Turno=new SolicitudTurnoBLL().Registrar(idMascota,idAgendaTurno,fecha,horario)};}
                catch(TurnoException ex){return new ResultadoRegistroTurnoBE {CodigoError=ex.Codigo};}});
        }
        [WebMethod(EnableSession=true)]
        public ResultadoConsultaTurnosBE ObtenerTurnosMascota(int idMascota)
        {
            return ConBloqueo(()=>{try{return new ConsultaTurnosBLL().Consultar(idMascota);}
                catch(TurnoException ex){return new ResultadoConsultaTurnosBE {CodigoError=ex.Codigo};}});
        }
    }
}
