using System;
using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    public class UsuarioActivoStateBLL : IUsuarioStateBLL
    {
        private const byte LimiteIntentosFallidos = 3;

        public bool PuedeIniciarSesion(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            return usuario.Estado && !usuario.Bloqueado && !usuario.RequiereRestablecerContrasena;
        }

        public void RegistrarIntentoFallido(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            if (usuario.IntentosFallidos < LimiteIntentosFallidos)
            {
                usuario.IntentosFallidos++;
            }

            if (usuario.IntentosFallidos >= LimiteIntentosFallidos)
            {
                usuario.IntentosFallidos = LimiteIntentosFallidos;
                usuario.Bloqueado = true;
            }
        }

        public void RegistrarIngresoExitoso(UsuarioBE usuario)
        {
            ValidarUsuario(usuario);

            usuario.IntentosFallidos = 0;
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
