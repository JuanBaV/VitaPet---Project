using System;
using VitaPet.BE;
using VitaPet.DAL;
using VitaPet.SECURITY;

namespace VitaPet.BLL
{
    public partial class UsuarioBLL
    {
        public void ActualizarIdioma(int idUsuario, string nuevoIdioma)
        {
            if (idUsuario <= 0) throw new ArgumentOutOfRangeException(nameof(idUsuario));
            if (nuevoIdioma != "es" && nuevoIdioma != "en")
                throw new ArgumentException("Idioma no admitido.", nameof(nuevoIdioma));
            var actual = SessionManager.Instancia.UsuarioActual;
            if (actual == null || actual.IdUsuario != idUsuario)
                throw new UnauthorizedAccessException("Solo puede cambiar su propio idioma.");

            UsuarioBE actualizado;
            using (var operacion = new OperacionIntegridadDAL())
            {
                _seguridad.ExigirIntegridad();
                actualizado = _usuarioDAL.ObtenerPorId(idUsuario);
                if (actualizado == null || !actualizado.Estado || actualizado.Bloqueado)
                    throw new UnauthorizedAccessException("El usuario no puede cambiar el idioma.");
                actualizado.Idioma = nuevoIdioma;
                actualizado.DVH = _seguridad.CalcularDVH(actualizado);
                _usuarioDAL.ActualizarIdioma(idUsuario, nuevoIdioma, actualizado.DVH);
                new DVVDAL().Guardar(new DVVBE {
                    NombreTabla = TablaUsuario,
                    ValorDVV = _seguridad.CalcularDVV(_usuarioDAL.ObtenerDVHs())
                });
                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                operacion.Complete();
            }
            // Publicar en sesión solo después de que Dispose confirme la transacción.
            SessionManager.Instancia.ActualizarUsuarioActual(actualizado);
        }
    }
}
