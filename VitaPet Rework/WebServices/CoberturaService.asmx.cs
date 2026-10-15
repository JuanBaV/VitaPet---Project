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
    [WebService(Namespace = "https://vitapet.local/services/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class CoberturaService : WebService
    {
        // Sobrecarga para consumo desde C# en CU-001-004. No acepta identificadores
        // del navegador: la BLL resuelve la suscripcion del cliente autenticado.
        public ResultadoConsultaCoberturaBE ObtenerCobertura()
        {
            return ObtenerCoberturaCuenta();
        }

        [WebMethod(EnableSession = true)]
        public ResultadoConsultaCoberturaBE ObtenerCoberturaCuenta()
        {
            if (HttpContext.Current != null && HttpContext.Current.Items["BloqueoVitaPet"] != null)
                return ConsultarCuenta();
            using (var bloqueo = EstadoAplicacion.BloquearSolicitud())
            {
                if (bloqueo == null)
                    throw new HttpException(503,"El sistema se encuentra en mantenimiento o procesando una operación.");
                return ConsultarCuenta();
            }
        }

        private static ResultadoConsultaCoberturaBE ConsultarCuenta()
        {
            try {return new ResultadoConsultaCoberturaBE{Consulta=new ConsultaCoberturaBLL().Consultar()};}
            catch (ConsultaCoberturaException ex) {return new ResultadoConsultaCoberturaBE{CodigoError=ex.Codigo};}
            catch (UnauthorizedAccessException) {throw new SoapException("Acceso denegado.",SoapException.ClientFaultCode);}
        }

        [WebMethod(EnableSession = true)]
        public ResultadoCoberturaBE ObtenerCobertura(int idPlan)
        {
            // Las páginas ya mantienen este bloqueo durante la solicitud.
            if (HttpContext.Current != null &&
                HttpContext.Current.Items["BloqueoVitaPet"] != null)
            {
                return ConsultarCobertura(idPlan);
            }

            using (var bloqueo = EstadoAplicacion.BloquearSolicitud())
            {
                if (bloqueo == null)
                {
                    throw new HttpException(503,
                        "El sistema se encuentra en mantenimiento o procesando una operación.");
                }

                return ConsultarCobertura(idPlan);
            }
        }

        private static ResultadoCoberturaBE ConsultarCobertura(int idPlan)
        {
            try
            {
                return new ResultadoCoberturaBE
                {
                    Coberturas = new ContratacionPlanBLL().ConsultarCobertura(idPlan)
                };
            }
            catch (ContratacionPlanException ex)
            {
                return new ResultadoCoberturaBE { CodigoError = ex.Codigo };
            }
            catch (UnauthorizedAccessException)
            {
                throw new SoapException("Acceso denegado.", SoapException.ClientFaultCode);
            }
        }
    }
}
