using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.SECURITY;

namespace VitaPet.GUI.Presentacion
{
    public abstract class PaginaClientePlanes : PaginaBase
    {
        private string errorInicial;
        protected override string[] RolesPermitidos { get { return new[]{"Cliente"}; } }
        protected override void OnPreInit(EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);Response.Cache.SetNoStore();
            ViewStateUserKey=Session.SessionID;
            try
            {
                if(!SessionManager.Instancia.HaySesionActiva)
                    Response.Redirect(ResolveUrl("~/Login.aspx")+"?ReturnUrl="+Server.UrlEncode(Request.RawUrl),true);
                new SecurityManager().ExigirCliente();
            }
            catch(UnauthorizedAccessException){Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true);}
            catch(SqlException){errorInicial="ER003";}
            catch(ConfigurationErrorsException){errorInicial="ER003";}
            base.OnPreInit(e);
        }
        protected void EjecutarPlan(Action accion)
        {
            if(errorInicial!=null){MostrarErrorPlan(errorInicial);return;}
            try{accion();}
            catch(ContratacionPlanException ex){MostrarErrorPlan(ex.Codigo);}
            catch(GestionPagoException ex){MostrarErrorPlan(ex.Codigo);}
            catch(ConsultaCoberturaException ex){MostrarErrorPlan(ex.Codigo);}
            catch(ConsultaReintegroException ex){MostrarErrorPlan(ex.Codigo);}
            catch(RegistroMascotaException ex){MostrarErrorPlan(ex.Codigo);}
            catch(UnauthorizedAccessException){Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true);}
        }
        protected void MostrarErrorPlan(string codigo)
        {
            var panel=Buscar(this,"pnlMensaje") as Panel;var texto=Buscar(this,"litMensaje") as Literal;
            if(panel!=null)panel.Visible=true;
            if(texto!=null)texto.Text=Server.HtmlEncode(codigo+" - "+TextosUI.Error(codigo,TextoErrorPlan(codigo)));
        }
        public static string TextoErrorPlan(string codigo)
        {
            switch(codigo)
            {
                case "ER003":return "Fallo de conexión";
                case "ER008":return "No existe una cobertura asociada";
                case "ER033":return "No se pudo registrar la consulta en bitácora";
                case "ER009":return "No existen reintegros asociados";
                case "ER015":return "No se pudo generar la salida XML";
                case "ER060":return "No se pudo registrar la mascota";
                case "ER059":return "No se pudo consultar la información de reintegros";
                case "ER058":return "No se pudo consultar el servicio de cobertura";
                case "ER002":return "Datos incompletos";
                case "ER006":return "Pago rechazado";
                case "ER004":return "Error en la carga de la lista de planes";
                case "ER005":return "El cliente posee un plan incompatible con la contratación solicitada";
                case "ER007":return "No fue posible registrar la contratación del plan";
                case "ER011":return "No existen datos asociados";
                case "ER023":return "Inconsistencia en la verificación de integridad";
                case "ER040":return "No se pudo registrar la operación en bitácora";
                case "ER050":return "El plan seleccionado ya no está disponible";
                case "ER051":return "No existen planes disponibles para contratar";
                case "ER052":return "No existen mascotas activas asociadas al cliente";
                case "ER053":return "Los datos de la contratación cambiaron. Volvé a consultar el plan.";
                case "ER054":return "La suscripción no admite un nuevo pago";
                case "ER055":return "No se pudo iniciar el procesamiento del pago";
                case "ER057":return "No se pudo registrar el pago. No vuelvas a pagar.";
                default:return "Datos inválidos";
            }
        }
        private static Control Buscar(Control c,string id)
        {
            if(c.ID==id)return c;
            foreach(Control child in c.Controls){var r=Buscar(child,id);if(r!=null)return r;}
            return null;
        }
    }
}
