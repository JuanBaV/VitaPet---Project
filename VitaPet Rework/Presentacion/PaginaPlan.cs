using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.SECURITY;

namespace VitaPet.GUI.Presentacion
{
    public abstract class PaginaPlan : PaginaBase
    {
        private string errorInicial;
        protected override string[] RolesPermitidos { get { return new[]{"Administrador"}; } }
        protected int IdPlan
        {
            get
            {
                int id;
                if (!int.TryParse(Request.QueryString["id"],NumberStyles.None,CultureInfo.InvariantCulture,out id) || id<=0)
                    throw new PlanException("ER011");
                return id;
            }
        }

        protected override void OnPreInit(EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);Response.Cache.SetNoStore();
            ViewStateUserKey=Session.SessionID;
            try
            {
                if (!SessionManager.Instancia.HaySesionActiva)
                    Response.Redirect(ResolveUrl("~/Login.aspx")+"?ReturnUrl="+Server.UrlEncode(Request.RawUrl),true);
                new SecurityManager().ExigirAdministrador();
            }
            catch (UnauthorizedAccessException) { Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true); }
            catch (SqlException) { errorInicial="ER003"; }
            catch (ConfigurationErrorsException) { errorInicial="ER003"; }
            base.OnPreInit(e);
        }

        protected bool EjecutarPlan(Action accion)
        {
            if (errorInicial!=null) { MostrarErrorPlan(errorInicial);return false; }
            try { accion();return true; }
            catch (PlanException ex) { MostrarErrorPlan(ex.Codigo);return false; }
            catch (UnauthorizedAccessException) { Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true);return false; }
        }

        protected virtual void AlFallarPlan(string codigo) { }

        private void MostrarErrorPlan(string codigo)
        {
            var panel=Buscar(this,"pnlMensaje") as Panel;var texto=Buscar(this,"litMensaje") as Literal;
            if (panel!=null) panel.Visible=true;
            if (texto!=null) texto.Text=Server.HtmlEncode(codigo+" - "+TextosUI.Error(codigo,Mensaje(codigo)));
            AlFallarPlan(codigo);
        }

        private static string Mensaje(string codigo)
        {
            switch(codigo)
            {
                case "ER075":return "No se pudo consultar la información del plan";
                case "ER076":return "No se pudo registrar el plan";
                case "ER077":return "No se pudieron actualizar los datos del plan";
                case "ER078":return "El nombre del plan ya se encuentra registrado";
                case "ER016":return "No existen planes registrados en el sistema";
                default:return PaginaClientePlanes.TextoErrorPlan(codigo);
            }
        }

        private static Control Buscar(Control control,string id)
        {
            if (control.ID==id) return control;
            foreach(Control child in control.Controls) {var encontrado=Buscar(child,id);if(encontrado!=null)return encontrado;}
            return null;
        }
    }
}
