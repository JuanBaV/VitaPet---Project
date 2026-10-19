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
    public abstract class PaginaEmpresa : PaginaBase
    {
        private string errorInicial;
        protected override string[] RolesPermitidos { get { return new[]{"Administrador"}; } }
        protected int IdEmpresa
        {
            get
            {
                int id;
                if (!int.TryParse(Request.QueryString["id"],NumberStyles.None,CultureInfo.InvariantCulture,out id) || id<=0)
                    throw new EmpresaException("ER011");
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

        protected bool EjecutarEmpresa(Action accion)
        {
            if (errorInicial!=null) { MostrarErrorEmpresa(errorInicial);return false; }
            try { accion();return true; }
            catch (EmpresaException ex) { MostrarErrorEmpresa(ex.Codigo);return false; }
            catch (UnauthorizedAccessException) { Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true);return false; }
        }

        protected virtual void AlFallarEmpresa(string codigo) { }

        private void MostrarErrorEmpresa(string codigo)
        {
            var panel=Buscar(this,"pnlMensaje") as Panel;var texto=Buscar(this,"litMensaje") as Literal;
            if (panel!=null) panel.Visible=true;
            if (texto!=null) texto.Text=Server.HtmlEncode(codigo+" - "+TextosUI.Error(codigo,Mensaje(codigo)));
            AlFallarEmpresa(codigo);
        }

        private static string Mensaje(string codigo)
        {
            switch(codigo)
            {
                case "ER067":return "No se pudo consultar la información de empresas";
                case "ER068":return "El CUIT ya se encuentra registrado para otra empresa";
                case "ER069":return "No se pudo registrar la empresa";
                case "ER070":return "No se pudieron actualizar los datos de la empresa";
                case "ER017":return "No existen empresas registradas";
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
