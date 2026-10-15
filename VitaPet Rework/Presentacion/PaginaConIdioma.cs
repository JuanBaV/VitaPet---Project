using System;
using System.Globalization;
using System.Web.UI;
using VitaPet.SECURITY;

namespace VitaPet.GUI.Presentacion
{
    // Cultura compartida por páginas públicas y protegidas, sin exigir login.
    public abstract class PaginaConIdioma : Page
    {
        protected override void InitializeCulture()
        {
            string idioma = SessionManager.Instancia.ObtenerIdiomaUsuario();
            Culture = CultureInfo.CreateSpecificCulture(idioma).Name;
            UICulture = idioma;
            base.InitializeCulture();
        }
    }
}
