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
 public class HistorialClinicoService : WebService
 {
  private static T ConBloqueo<T>(Func<T> consulta) //evita que dos operaciones se lancen a la vez, y bloquea la solicitud si el sistema está en mantenimiento
        {
   try {
    if(HttpContext.Current!=null&&HttpContext.Current.Items["BloqueoVitaPet"]!=null)return consulta();
    using(var bloqueo=EstadoAplicacion.BloquearSolicitud())
    {if(bloqueo==null)throw new HttpException(503,"El sistema se encuentra en mantenimiento o procesando una operación.");return consulta();}
   }catch(UnauthorizedAccessException){throw new SoapException("Acceso denegado.",SoapException.ClientFaultCode);}
  }
  [WebMethod(EnableSession=true,MessageName="ObtenerHistorialClínico")]
  public ResultadoHistorialClinicoBE ObtenerHistorialClinico(int idMascota) //consultar historial clínico de una mascota
        { return ConBloqueo(()=>{try{return new ConsultaHistorialClinicoBLL().Consultar(idMascota);}catch(GestionMascotaException ex){return new ResultadoHistorialClinicoBE {CodigoError=ex.Codigo};}});}
  [WebMethod(EnableSession=true,MessageName="ObtenerDatosClínicos")]
  public ResultadoDatosClinicosBE ObtenerDatosClinicos(int idMascota) //consultar datos clínicos de una mascota
        { return ConBloqueo(()=>{try{return new ConsultaDatosClinicosBLL().Consultar(idMascota);}catch(GestionMascotaException ex){return new ResultadoDatosClinicosBE {CodigoError=ex.Codigo};}});}
 }
}
