using System;
using System.Collections.Generic;
using System.Linq;
using VitaPet.BE;
using VitaPet.BLL.Estados;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public partial class UsuarioBLL
    {
        private const string TablaUsuario = "Usuario";

        private readonly UsuarioDAL _usuarioDAL;
        private readonly SecurityManager _seguridad;

        public UsuarioBLL() //
        {
            _usuarioDAL = new UsuarioDAL();
            _seguridad = new SecurityManager();
        }

        public ResultadoConsultaUsuariosBE ConsultarUsuarios(FiltroUsuariosBE filtro)
        {
            filtro = NormalizarFiltro(filtro);
            using (var operacion = new OperacionIntegridadDAL())
            {
                var webmaster = _seguridad.ExigirPermiso("GestionUsuarios");
                ExigirIntegridadConsulta();
                var resultado = _usuarioDAL.ListarUsuarios(filtro);
                // Audit even an empty result; publish data only after the audit commits.
                try
                {
                    _seguridad.RegistrarBitacora(new BitacoraBE {
                        IdUsuario = webmaster.IdUsuario, Fecha = DateTime.Now,
                        Accion = "Consulta de usuarios", Modulo = "Gestión de usuarios", Criticidad = "Informativa"
                    });
                }
                catch (Exception ex) when (ex is System.IO.IOException || ex is System.Xml.XmlException || ex is UnauthorizedAccessException)
                {
                    throw new ConsultaUsuariosException("ER033", "No se pudo registrar la consulta en bitácora.", ex);
                }
                operacion.Complete();
                return resultado;
            }
        }

        public UsuarioListadoBE ObtenerUsuarioSeleccionado(int idUsuario, FiltroUsuariosBE filtro)
        {
            filtro = NormalizarFiltro(filtro);
            using (var operacion = new OperacionIntegridadDAL())
            {
                _seguridad.ExigirPermiso("GestionUsuarios");
                ExigirIntegridadConsulta();
                // Revalidate the current state before offering actions; no mutation or new query event.
                var usuario = _usuarioDAL.ListarUsuarios(filtro).Usuarios.SingleOrDefault(u => u.IdUsuario == idUsuario);
                operacion.Complete();
                return usuario;
            }
        }

        public static FiltroUsuariosBE NormalizarFiltro(FiltroUsuariosBE filtro)
        {
            if (filtro == null) throw new ArgumentNullException(nameof(filtro));
            var nombre = (filtro.Nombre ?? string.Empty).Trim();
            if (nombre.Length > 100 || !Enum.IsDefined(typeof(EstadoFiltroUsuario), filtro.Estado))
                throw new ConsultaUsuariosException("ER025", "Revisá el nombre y el estado de la búsqueda.");
            return new FiltroUsuariosBE { Nombre = nombre, Estado = filtro.Estado };
        }

        private void ExigirIntegridadConsulta()
        {
            if (!_seguridad.VerificarIntegridad())
                throw new ConsultaUsuariosException("ER023", "Inconsistencia en la verificación de integridad.");
        }

        public UsuarioBE ObtenerPorCorreo(
            string correoElectronico)
        {
            return _usuarioDAL.ObtenerPorCorreo(
                correoElectronico);
        }

        public void RegistrarIntentoFallido(
            UsuarioBE usuario)
        {
            RegistrarEstadoIngreso(usuario, false);
        }

        public void RegistrarIngresoExitoso(
            UsuarioBE usuario)
        {
            RegistrarEstadoIngreso(usuario, true);
        }

        private void RegistrarEstadoIngreso(UsuarioBE usuario, bool exitoso)
        {
            using(var operacion = new OperacionIntegridadDAL())
            {
                _seguridad.ExigirIntegridad();
                ValidarUsuario(usuario);
                var usuarios = _usuarioDAL.ObtenerParaIntegridad();
                var vigente = usuarios.Single(item => item.IdUsuario == usuario.IdUsuario);
                usuario.IntentosFallidos = vigente.IntentosFallidos;
                usuario.Bloqueado = vigente.Bloqueado;
                usuario.Estado = vigente.Estado;
                usuario.RequiereRestablecerContrasena = vigente.RequiereRestablecerContrasena;

                var estadoUsuario = new UsuarioStateBLL(usuario);
                if (exitoso) estadoUsuario.RegistrarIngresoExitoso();
                else estadoUsuario.RegistrarIntentoFallido();

                ActualizarEstadoSeguridad(usuario, usuarios);
                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                operacion.Complete();
            }
        }

        public void DesbloquearUsuario(
            int idUsuario)
        {
            using(var operacion = new OperacionIntegridadDAL())
            {
                _seguridad.ExigirIntegridad();

            if (idUsuario <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idUsuario));
            }

            List<UsuarioBE> usuarios =
                _usuarioDAL.ObtenerParaIntegridad();

            UsuarioBE usuario = usuarios.SingleOrDefault(
                item => item.IdUsuario == idUsuario);

            if (usuario == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el usuario a actualizar.");
            }

            UsuarioStateBLL estadoUsuario =
                new UsuarioStateBLL(usuario);

            estadoUsuario.Desbloquear();

            ActualizarEstadoSeguridad(usuario, usuarios);

                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                operacion.Complete();
            }
        }

        private void ActualizarEstadoSeguridad(
            UsuarioBE usuario, List<UsuarioBE> usuarios)
        {
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

            _usuarioDAL.ActualizarEstadoSeguridad(
                usuario.IdUsuario,
                usuario.IntentosFallidos,
                usuario.Bloqueado,
                usuario.Estado,
                usuario.DVH,
                dvv);
        }

        private static void ValidarUsuario(
            UsuarioBE usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(
                    nameof(usuario));
            }
        }
    }
}
