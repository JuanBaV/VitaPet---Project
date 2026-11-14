using System;
using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    //Esto funciona igual que el estado activo pero al reves
    public class UsuarioBloqueadoStateBLL : IUsuarioStateBLL
    {
        private const byte LimiteIntentosFallidos = 3;

        public bool PuedeIniciarSesion(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            return false;
        }

        public void RegistrarIntentoFallido(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            usuario.IntentosFallidos = LimiteIntentosFallidos;
            usuario.Bloqueado = true;
        }

        public void RegistrarIngresoExitoso(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            usuario.IntentosFallidos = LimiteIntentosFallidos;
            usuario.Bloqueado = true;
        }

        public void Desbloquear(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            usuario.Bloqueado = false;
            usuario.IntentosFallidos = 0;
        }

        private void ValidarUsuario(UsuarioBE usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }
        }
    }
}
