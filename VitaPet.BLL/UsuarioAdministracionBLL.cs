using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public partial class UsuarioBLL
    {
        public EdicionUsuarioBE PrepararOperacionUsuario(int id, OperacionUsuario operacion)
        {
            if (!Enum.IsDefined(typeof(OperacionUsuario), operacion))
                throw ErrorUsuario("ER025");
            return EjecutarOperacionUsuario(() => {
                using (var tx = new OperacionIntegridadDAL())
                {
                    var actor = _seguridad.ExigirPermiso("ConsultarUsuario", "ModificarUsuario", "BloquearUsuario", "DesbloquearUsuario");
                    ExigirIntegridadConsulta();
                    var usuario = ExigirUsuario(id);
                    ValidarEstadoOperacion(usuario, operacion);
                    if (operacion == OperacionUsuario.Consultar)
                        AuditarUsuario(actor.IdUsuario, id, "Consulta de usuario", "ER033");
                    var resultado = new EdicionUsuarioBE {
                        Version = usuario.DVH,
                        Usuario = new UsuarioListadoBE {
                            IdUsuario = usuario.IdUsuario, Nombre = usuario.Nombre,
                            CorreoElectronico = usuario.CorreoElectronico, Rol = usuario.Rol,
                            Estado = usuario.Estado, Bloqueado = usuario.Bloqueado
                        }
                    };
                    tx.Complete();
                    return resultado;
                }
            }, operacion == OperacionUsuario.Consultar ? "ER033" : "ER039");
        }

        public void ModificarUsuario(int id, string version, CambiosUsuarioBE cambios)
        {
            GuardarOperacionUsuario(id, OperacionUsuario.Modificar, version, cambios);
        }

        public void BloquearUsuarioAdministrado(int id)
        {
            GuardarOperacionUsuario(id, OperacionUsuario.Bloquear, null, null);
        }

        public void DesbloquearUsuarioAdministrado(int id)
        {
            GuardarOperacionUsuario(id, OperacionUsuario.Desbloquear, null, null);
        }

        private void GuardarOperacionUsuario(int id, OperacionUsuario operacion, string version, CambiosUsuarioBE cambios)
        {
            UsuarioBE actualizado = null;
            EjecutarOperacionUsuario(() => {
                using (var tx = new OperacionIntegridadDAL())
                {
                    var actor = _seguridad.ExigirPermiso(operacion == OperacionUsuario.Modificar ? "ModificarUsuario" : operacion == OperacionUsuario.Bloquear ? "BloquearUsuario" : "DesbloquearUsuario");
                    if (operacion == OperacionUsuario.Modificar) cambios = ValidarCambiosUsuario(cambios);
                    ExigirIntegridadConsulta();
                    var u = ExigirUsuario(id);
                    string versionActual = u.DVH;
                    ValidarEstadoOperacion(u, operacion);
                    if (operacion == OperacionUsuario.Modificar)
                    {
                        if (string.IsNullOrEmpty(version) || !string.Equals(version, u.DVH, StringComparison.Ordinal))
                            throw ErrorUsuario("ER038");
                        var duplicado = _usuarioDAL.ObtenerPorCorreo(cambios.CorreoElectronico);
                        if (duplicado != null && duplicado.IdUsuario != id) throw ErrorUsuario("ER001");
                        u.Nombre = cambios.Nombre; u.CorreoElectronico = cambios.CorreoElectronico;
                        if (u.Rol != cambios.Rol && actor.Rol != "Webmaster") throw new UnauthorizedAccessException("Solo el Webmaster puede asignar perfiles.");
                        if (!new PerfilBLL().Leer().OfType<PerfilBE>().Any(p => p.Nombre==cambios.Rol)) throw ErrorUsuario("ER025");
                        u.Rol = cambios.Rol; u.Estado = cambios.Estado.Value;
                    }
                    else if (operacion == OperacionUsuario.Bloquear) u.Bloqueado = true;
                    else { u.Bloqueado = false; u.IntentosFallidos = 0; }
                    u.DVH = _seguridad.CalcularDVH(u);
                    _usuarioDAL.GuardarAdministracion(u, versionActual);
                    new DVVDAL().Guardar(new DVVBE {
                        NombreTabla = "Usuario", ValorDVV = _seguridad.CalcularDVV(_usuarioDAL.ObtenerDVHs())
                    });
                    // Actualiza la referencia protegida dentro de la misma transacción antes
                    // de que RegistrarBitacora vuelva a verificar la integridad.
                    new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                    string accion = operacion == OperacionUsuario.Modificar ? "Modificación de usuario" :
                        operacion == OperacionUsuario.Bloquear ? "Bloqueo de usuario" : "Desbloqueo de usuario";
                    AuditarUsuario(actor.IdUsuario, id, accion, "ER040");
                    actualizado = u;
                    tx.Complete();
                }
                return true;
            }, "ER039");
            // Solo después del commit; conserva los demás datos y tickets de la sesión.
            VitaPet.SECURITY.SessionManager.Instancia.ActualizarUsuarioActual(actualizado);
        }

        public static CambiosUsuarioBE ValidarCambiosUsuario(CambiosUsuarioBE c)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.Nombre) || string.IsNullOrWhiteSpace(c.CorreoElectronico)
                || string.IsNullOrWhiteSpace(c.Rol) || !c.Estado.HasValue) throw ErrorUsuario("ER002");
            var nombre = c.Nombre.Trim(); var correo = c.CorreoElectronico.Trim();
            if (nombre.Length > 100 || correo.Length > 100 || nombre.Any(char.IsControl) || correo.Any(char.IsWhiteSpace)
                || c.Rol.Length>30 || c.Rol.Any(char.IsControl)) throw ErrorUsuario("ER025");
            try
            {
                if (new MailAddress(correo).Address != correo || !correo.Contains("@")) throw ErrorUsuario("ER025");
                // El esquema existente usa VARCHAR; evitar pérdida silenciosa de caracteres.
                var encoding = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                encoding.GetBytes(nombre); encoding.GetBytes(correo);
            }
            catch (FormatException) { throw ErrorUsuario("ER025"); }
            catch (EncoderFallbackException) { throw ErrorUsuario("ER025"); }
            return new CambiosUsuarioBE { Nombre=nombre, CorreoElectronico=correo, Rol=c.Rol, Estado=c.Estado };
        }

        private UsuarioBE ExigirUsuario(int id)
        {
            var u = id > 0 ? _usuarioDAL.ObtenerPorId(id) : null;
            if (u == null) throw ErrorUsuario("ER036");
            return u;
        }

        private static void ValidarEstadoOperacion(UsuarioBE u, OperacionUsuario operacion)
        {
            if ((operacion == OperacionUsuario.Bloquear && (!u.Estado || u.Bloqueado)) ||
                (operacion == OperacionUsuario.Desbloquear && !u.Bloqueado)) throw ErrorUsuario("ER037");
        }

        private void AuditarUsuario(int actor, int id, string accion, string codigoError)
        {
            try
            {
                _seguridad.RegistrarBitacora(new BitacoraBE {
                    IdUsuario=actor, Fecha=DateTime.Now, Accion=accion + " (idUsuario=" + id + ")",
                    Modulo="Gestión de usuarios", Criticidad="Informativa"
                });
            }
            catch (SqlException ex) { throw ErrorUsuario(EsFalloConexion(ex) ? "ER003" : codigoError, ex); }
            catch (ConfigurationErrorsException ex) { throw ErrorUsuario("ER003", ex); }
            catch (Exception ex) when (ex is IOException || ex is XmlException || ex is UnauthorizedAccessException || ex is TransactionException)
            { throw ErrorUsuario(codigoError, ex); }
            catch (InvalidOperationException ex)
            { throw ErrorUsuario(ex.Message.StartsWith("ER023", StringComparison.Ordinal) ? "ER023" : codigoError, ex); }
        }

        private static T EjecutarOperacionUsuario<T>(Func<T> action, string error)
        {
            try { return action(); }
            catch (SqlException ex)
            {
                string codigo = EsFalloConexion(ex) ? "ER003" :
                    ex.Number == 2601 || ex.Number == 2627 || ex.Number == 50001 ? "ER001" :
                    ex.Number == 50036 ? "ER036" : ex.Number == 50038 ? "ER038" : error;
                throw ErrorUsuario(codigo, ex);
            }
            catch (ConfigurationErrorsException ex) { throw ErrorUsuario("ER003", ex); }
            catch (TransactionException ex) { throw ErrorUsuario(error, ex); }
            catch (InvalidOperationException ex)
            { throw ErrorUsuario(ex.Message.StartsWith("ER023", StringComparison.Ordinal) ? "ER023" : error, ex); }
        }

        private static bool EsFalloConexion(SqlException ex)
        {
            return ex.Class >= 20 || new[] { -2, 2, 20, 53, 64, 233, 4060, 18456, 10053, 10054, 10060 }.Contains(ex.Number);
        }

        private static ConsultaUsuariosException ErrorUsuario(string codigo, Exception inner = null)
        {
            return new ConsultaUsuariosException(codigo, "No se pudo completar la operación de usuario (" + codigo + ").", inner);
        }
    }
}
