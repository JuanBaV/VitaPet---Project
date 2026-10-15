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
            new Lazy<SessionManager>(() => new SessionManager());

        private SessionManager()
        {
        }

        public static SessionManager Instancia
        {
            get
            {
                return _instancia.Value;
            }
        }

        public UsuarioBE UsuarioActual
        {
            get
            {
                return ObtenerSesion()[ClaveUsuario] as UsuarioBE;
            }
        }

        public bool HaySesionActiva
        {
            get
            {
                return UsuarioActual != null;
            }
        }

        public int? IdUsuarioRecuperacion
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

        public bool RecuperacionContrasenaAutorizada
        {
            get
            {
                object valor =
                    ObtenerSesion()[ClaveRecuperacionAutorizada];

                return valor != null &&
                    Convert.ToBoolean(valor);
            }
        }

        public void IniciarSesion(UsuarioBE usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }

            HttpSessionState sesion = ObtenerSesion();
            sesion.Clear();
            sesion[ClaveUsuario] = usuario;
            sesion["GeneracionVitaPet"] = EstadoAplicacion.Generacion;
        }

        public bool HaySesionContingencia
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
        public string CuentaContingencia => HaySesionContingencia ? (string)ObtenerSesion()["CuentaContingencia"] : null;
        public void IniciarContingencia(string cuenta)
        {
            if (string.IsNullOrWhiteSpace(cuenta)) throw new ArgumentException("Cuenta requerida.");
            var sesion = ObtenerSesion();
            sesion.Clear();
            sesion["CuentaContingencia"] = cuenta;
            sesion["VenceContingencia"] = DateTime.UtcNow.AddMinutes(15);
            sesion["GeneracionVitaPet"] = EstadoAplicacion.Generacion;
        }

        public void ActualizarUsuarioActual(UsuarioBE usuario)
        {
            var actual = UsuarioActual;
            if (usuario != null && actual != null && actual.IdUsuario == usuario.IdUsuario)
                ObtenerSesion()[ClaveUsuario] = usuario;
        }

        public string ObtenerIdiomaUsuario()
        {
            string idioma = UsuarioActual?.Idioma;
            return idioma == "en" ? "en" : "es";
        }

        public void ActualizarIdiomaUsuario(string nuevoIdioma)
        {
            if (nuevoIdioma != "es" && nuevoIdioma != "en")
                throw new ArgumentException("Idioma no admitido.", nameof(nuevoIdioma));
            var usuario = UsuarioActual;
            if (usuario == null)
                throw new InvalidOperationException("No hay un usuario autenticado.");
            usuario.Idioma = nuevoIdioma;
            // Reasignar también para proveedores de sesión fuera de proceso.
            ObtenerSesion()[ClaveUsuario] = usuario;
        }

        public void AutorizarRecuperacionContrasena(
            int idUsuario)
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

        public void FinalizarRecuperacionContrasena()
        {
            HttpSessionState sesion = ObtenerSesion();

            sesion.Remove(ClaveUsuarioRecuperacion);
            sesion.Remove(ClaveRecuperacionAutorizada);
        }

        public void CerrarSesion()
        {
            HttpSessionState sesion = ObtenerSesion();
            sesion.Clear();
            sesion.Abandon();
        }

        private HttpSessionState ObtenerSesion()
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
