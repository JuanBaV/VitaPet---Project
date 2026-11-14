using System;
using System.Linq;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.SECURITY;
using VitaPet.SERVICES;

namespace VitaPet.GUI.Presentacion
{
    //Template Method: esta es la pagina base que define la estructura de todas las paginas del sistema
    public abstract class PaginaBase : PaginaConIdioma
    {
        private const string RutaLogin = "~/Login.aspx";
        private const string RutaAccesoDenegado = "~/AccesoDenegado.aspx";

        protected abstract string[] RolesPermitidos
        {
            get;
        }

        protected sealed override void OnLoad(EventArgs e) //sobreescribe OnLoad para validar la sesion y el rol del usuario antes de ejecutar la funcionalidad de la pagina
        {
            if (!ValidarSesion())
            {
                return;
            }

            if (!ValidarRol())
            {
                return;
            }

            base.OnLoad(e);

            CargarEncabezado();
            MostrarMensajes();
            EjecutarFuncionalidad();
        }

        protected virtual void CargarEncabezado()
        {
        }

        protected virtual void MostrarMensajes()
        {
        }

        protected abstract void EjecutarFuncionalidad();

        private bool ValidarSesion() //validar sesion mediante singleton
        {
            if (SessionManager.Instancia.HaySesionActiva)
            {
                return true;
            }

            RedirigirAlLogin();
            return false;
        }

        private bool ValidarRol() 
        {
            UsuarioBE usuario = SessionManager.Instancia.UsuarioActual;

            if (usuario == null)
            {
                RedirigirAlLogin();
                return false;
            }

            string pantalla = System.IO.Path.GetFileNameWithoutExtension(AppRelativeVirtualPath);
            if (new VitaPet.BLL.PerfilBLL().PuedeAcceder(pantalla))
            {
                return true;
            }

            Redirigir(RutaAccesoDenegado);
            return false;
        }

        private void RedirigirAlLogin()
        {
            string returnUrl =
                Server.UrlEncode(Request.RawUrl);

            string destino =
                ResolveUrl(RutaLogin) +
                "?ReturnUrl=" +
                returnUrl;

            Redirigir(destino);
        }

        private void Redirigir(string ruta)
        {
            Response.Redirect(ResolveUrl(ruta), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
