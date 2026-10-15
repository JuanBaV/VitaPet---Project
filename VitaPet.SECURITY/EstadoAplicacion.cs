using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace VitaPet.SECURITY
{
    public static class EstadoAplicacion
    {
        public static string ResolverRuta(string setting, string fallback)
        {
            string value = ConfigurationManager.AppSettings[setting] ?? fallback;
            return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, value.Replace("~/", "")));
        }
        public static string Directorio => ResolverRuta("RutaRecuperacion", "~/App_Data/Recuperacion");
        public static string Archivo(string nombre) { Directory.CreateDirectory(Directorio); return Path.Combine(Directorio,nombre); }
        public static bool Mantenimiento => File.Exists(Archivo("mantenimiento.txt"));
        public static void IniciarMantenimiento(string operacion) { File.WriteAllText(Archivo("mantenimiento.txt"),operacion); }
        public static void FinalizarMantenimiento() { File.Delete(Archivo("mantenimiento.txt")); }
        // Una generación externa a la BD invalida todas las sesiones, también entre procesos IIS.
        public static string Generacion => File.Exists(Archivo("sesiones.txt")) ? File.ReadAllText(Archivo("sesiones.txt")) : "inicial";
        public static void InvalidarSesiones() { EscribirAtomico(Archivo("sesiones.txt"),Guid.NewGuid().ToString("N")); }
        public static IDisposable IntentarBloqueo()
        {
            try { return new FileStream(Archivo("operacion.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); }
            catch (IOException) { return null; }
        }
        public static IDisposable BloquearSolicitud(bool permitirMantenimiento = false)
        {
            var espera=System.Diagnostics.Stopwatch.StartNew();
            do
            {
                if(Mantenimiento && !permitirMantenimiento) return null;
                var bloqueo=IntentarBloqueo();
                if(bloqueo!=null) return bloqueo;
                System.Threading.Thread.Sleep(25);
            } while(espera.Elapsed < TimeSpan.FromSeconds(3));
            return null;
        }
        public static IDisposable BloquearOperacion()
        {
            if(System.Web.HttpContext.Current?.Items["BloqueoVitaPet"]!=null) return new Vacio();
            return IntentarBloqueo() ?? throw new InvalidOperationException("Hay otra operación en curso. Reintente más tarde.");
        }
        private sealed class Vacio : IDisposable { public void Dispose() { } }
        public static void EscribirAtomico(string archivo,string contenido)
        {
            string temp=archivo+"."+Guid.NewGuid().ToString("N")+".tmp";
            File.WriteAllText(temp,contenido,Encoding.UTF8);
            if(File.Exists(archivo)) File.Replace(temp,archivo,null); else File.Move(temp,archivo);
        }
        public static string CalcularHuella(string valor)
        {
            using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(valor))).Replace("-","").ToLowerInvariant();
        }
    }
}
