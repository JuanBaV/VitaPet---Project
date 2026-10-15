using System.Globalization;
using System.Threading;

namespace VitaPet.SERVICES
{
    public static class LanguageManager
    {
        private const string IdiomaPredeterminado = "es";

        public static string Aplicar(string idioma)
        {
            CultureInfo cultura = ObtenerCultura(idioma);

            Thread.CurrentThread.CurrentUICulture =
                cultura;

            return cultura.Name;
        }

        private static CultureInfo ObtenerCultura(
            string idioma)
        {
            string codigoIdioma =
                string.IsNullOrWhiteSpace(idioma)
                    ? IdiomaPredeterminado
                    : idioma.Trim();

            try
            {
                return CultureInfo.GetCultureInfo(
                    codigoIdioma);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.GetCultureInfo(
                    IdiomaPredeterminado);
            }
        }
    }
}
