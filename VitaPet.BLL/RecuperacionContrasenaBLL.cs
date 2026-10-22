using System;
using System.Collections.Generic;
using System.Linq;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public class RecuperacionContrasenaBLL
    {
        private const string RespuestaCorrecta = "UAI";
        private const string TablaUsuario = "Usuario";

        private readonly UsuarioBLL _usuarioBLL;
        private readonly UsuarioDAL _usuarioDAL;
        private readonly SecurityManager _seguridad;

        public RecuperacionContrasenaBLL()
        {
            _usuarioBLL = new UsuarioBLL();
            _usuarioDAL = new UsuarioDAL();
            _seguridad = new SecurityManager();
        }

        public UsuarioBE ValidarIdentidad(
            string correoElectronico,
            string respuesta)
        {
            UsuarioBE usuario =
                _usuarioBLL.ObtenerPorCorreo(
                    correoElectronico);

            if (usuario == null)
            {
                return null;
            }

            if (!string.Equals(
                respuesta,
                RespuestaCorrecta,
                StringComparison.Ordinal))
            {
                return null;
            }

            return usuario;
        }

        public void ActualizarContrasena(
            int idUsuario,
            string nuevaContrasena)
        {
            using(var operacion = new OperacionIntegridadDAL())
            {
                _seguridad.ExigirIntegridad();

            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                throw new ArgumentException(
                    "La contraseña es obligatoria.",
                    nameof(nuevaContrasena));
            }

            if (!_seguridad.CumpleRequisitosContrasena(
                nuevaContrasena))
            {
                throw new ArgumentException(
                    "La contraseña no cumple los requisitos de seguridad.",
                    nameof(nuevaContrasena));
            }

            List<UsuarioBE> usuarios =
                _usuarioDAL.ObtenerParaIntegridad();

            UsuarioBE usuario =
                usuarios.SingleOrDefault(item =>
                    item.IdUsuario == idUsuario);

            if (usuario == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el usuario a actualizar.");
            }

            usuario.Contrasena =
                _seguridad.CalcularHash(nuevaContrasena);

            usuario.RequiereRestablecerContrasena = false;

            usuario.DVH =
                _seguridad.CalcularDVH(usuario);

            IEnumerable<string> dvhsActualizados =
                usuarios.Select(item =>
                    item.IdUsuario == usuario.IdUsuario
                        ? usuario.DVH
                        : item.DVH);

            DVVBE dvv = new DVVBE
            {
                NombreTabla = TablaUsuario,
                ValorDVV = _seguridad.CalcularDVV(
                    dvhsActualizados)
            };

            _usuarioDAL.ActualizarContrasena(
                usuario.IdUsuario,
                usuario.Contrasena,
                usuario.DVH,
                dvv);

            // Elimina también la referencia de la marca verdadera; falso vuelve
            // a su representación canónica compatible con las cuentas anteriores.
            new ReferenciaIntegridadDAL().EliminarFila("Usuario",idUsuario.ToString(System.Globalization.CultureInfo.InvariantCulture));
            new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
            _usuarioBLL.DesbloquearUsuario(idUsuario);

                _seguridad.RegistrarBitacora(new BitacoraBE { IdUsuario=idUsuario, Fecha=DateTime.Now,
                    Accion="Contraseña restablecida", Modulo="Autenticación", Criticidad="Informativa" });
                operacion.Complete();
            }
        }
    }
}
