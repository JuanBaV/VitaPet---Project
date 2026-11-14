using System;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;

namespace VitaPet.SECURITY
{
    public sealed class SessionManager
    {
        private const string ClaveUsuario = "UsuarioAutenticado";
        private const string ClaveUsuarioRecuperacion =
            "IdUsuarioRecuperacion";
        private const string ClaveRecuperacionAutorizada =
            "RecuperacionContrasenaAutorizada";

        private static readonly Lazy<SessionManager> _instancia =
            new Lazy<SessionManager>(() => new SessionManager()); //declaracion de instancia unica de singleton

        private SessionManager() //impide la creacion de instancias desde fuera de la clase
        {
        }
        

        public static SessionManager Instancia //propiedad estatica para acceder a la instancia unica de SessionManager
        {
            get
            {
                return _instancia.Value;
            }
        }

        public UsuarioBE UsuarioActual //propiedad para obtener el usuario actualmente autenticado en la sesión
        {
            get
            {
                return ObtenerSesion()[ClaveUsuario] as UsuarioBE;
            }
        }

        public bool HaySesionActiva //propiedad para verificar si hay una sesión activa con un usuario autenticado
        {
            get
            {
                return UsuarioActual != null;
            }
        }

        public int? IdUsuarioRecuperacion //propiedad para obtener el ID del usuario que está en proceso de recuperación de contraseña
        {
            get
            {
                object valor =
                    ObtenerSesion()[ClaveUsuarioRecuperacion];

                return valor == null
                    ? (int?)null
                    : Convert.ToInt32(valor);
            }
        }

        public bool RecuperacionContrasenaAutorizada //propiedad para verificar si la recuperación de contraseña está autorizada en la sesión
        {
            get
            {
                object valor =
                    ObtenerSesion()[ClaveRecuperacionAutorizada];

                return valor != null &&
                    Convert.ToBoolean(valor);
            }
        }

        public void IniciarSesion(UsuarioBE usuario) //método para iniciar sesión con un usuario autenticado
        {
            if(usuario != null && usuario.RequiereRestablecerContrasena)
                throw new UnauthorizedAccessException("Debe restablecer la contraseña antes de iniciar sesión.");
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }

            HttpSessionState sesion = ObtenerSesion();
            sesion.Clear();
            sesion[ClaveUsuario] = usuario;
            sesion["GeneracionVitaPet"] = EstadoAplicacion.Generacion;
        }

        public bool HaySesionContingencia //propiedad para verificar si hay una sesión de contingencia activa
        {
            get
            {
                var sesion = ObtenerSesion();
                if (sesion["CuentaContingencia"] == null) return false;
                if (!(sesion["VenceContingencia"] is DateTime) || (DateTime)sesion["VenceContingencia"] <= DateTime.UtcNow)
                { sesion.Clear(); return false; }
                return true;
            }
        }
        public string CuentaContingencia => HaySesionContingencia ? (string)ObtenerSesion()["CuentaContingencia"] : null; //propiedad para obtener la cuenta de contingencia activa en la sesión, si existe
        public void IniciarContingencia(string cuenta) //metodo para iniciar una sesión de contingencia con una cuenta específica
        {
            if (string.IsNullOrWhiteSpace(cuenta)) throw new ArgumentException("Cuenta requerida.");
            var sesion = ObtenerSesion();
            sesion.Clear();
            sesion["CuentaContingencia"] = cuenta;
            sesion["VenceContingencia"] = DateTime.UtcNow.AddMinutes(15);
            sesion["GeneracionVitaPet"] = EstadoAplicacion.Generacion;
        }

        public void ActualizarUsuarioActual(UsuarioBE usuario) //metodo para actualizar la informadcion del usuario actualmente autenticado en la sesión
        {
            var actual = UsuarioActual;
            if (usuario != null && actual != null && actual.IdUsuario == usuario.IdUsuario)
                ObtenerSesion()[ClaveUsuario] = usuario;
        }

        public string ObtenerIdiomaUsuario() //metodo para obtener el idioma de la sesion
        {
            string idioma = UsuarioActual?.Idioma ?? ObtenerSesion()["IdiomaPublico"] as string;
            return idioma == "en" ? "en" : "es";
        }

        public void ActualizarIdiomaUsuario(string nuevoIdioma) //metodo para actualizar el idioma de la sesion
        {
            if (nuevoIdioma != "es" && nuevoIdioma != "en")
                throw new ArgumentException("Idioma no admitido.", nameof(nuevoIdioma));
            var usuario = UsuarioActual;
            if (usuario == null)
            {
                ObtenerSesion()["IdiomaPublico"] = nuevoIdioma;
                return;
            }
            usuario.Idioma = nuevoIdioma;
            // Reasignar también para proveedores de sesión fuera de proceso.
            ObtenerSesion()[ClaveUsuario] = usuario;
        }

        public void AutorizarRecuperacionContrasena(int idUsuario) //metodo para autorizar la recuperación de contraseña para un usuario específico en la sesión
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            HttpSessionState sesion = ObtenerSesion();

            sesion["GeneracionVitaPet"] = EstadoAplicacion.Generacion;
            sesion[ClaveUsuarioRecuperacion] =
                idUsuario;

            sesion[ClaveRecuperacionAutorizada] =
                true;
        }

        public void FinalizarRecuperacionContrasena() //metodo para finalizar la recuperación de contraseña y limpiar los datos relacionados en la sesión
        {
            HttpSessionState sesion = ObtenerSesion();

            sesion.Remove(ClaveUsuarioRecuperacion);
            sesion.Remove(ClaveRecuperacionAutorizada);
        }

        public void CerrarSesion() //metodo para cerrar la sesión y limpiar todos los datos relacionados
        {
            HttpSessionState sesion = ObtenerSesion();
            sesion.Clear();
            sesion.Abandon();
        }

        private HttpSessionState ObtenerSesion() //metodo privado para obtener la sesión HTTP actual, asegurando que exista y sea válida
        {
            HttpContext contexto = HttpContext.Current;

            if (contexto == null || contexto.Session == null)
            {
                throw new InvalidOperationException(
                    "No existe una sesión HTTP disponible.");
            }

            var sesion=contexto.Session;
            if(sesion["GeneracionVitaPet"]!=null && !string.Equals((string)sesion["GeneracionVitaPet"],EstadoAplicacion.Generacion,StringComparison.Ordinal))
                sesion.Clear();
            return sesion;
        }
    }
}
