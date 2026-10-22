using System;
using System.Linq;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.SECURITY;
using VitaPet.SERVICES;

namespace VitaPet.GUI.Presentacion
{
    public abstract class PaginaBase : PaginaConIdioma
    {
        private const string RutaLogin = "~/Login.aspx";
        private const string RutaAccesoDenegado = "~/AccesoDenegado.aspx";

        protected abstract string[] RolesPermitidos
        {
            get;
        }

        protected sealed override void OnLoad(EventArgs e)
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

        private bool ValidarSesion()
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
