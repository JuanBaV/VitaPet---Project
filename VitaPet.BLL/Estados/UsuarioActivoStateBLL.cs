using System;
using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    public class UsuarioActivoStateBLL : IUsuarioStateBLL
    {
        private const byte LimiteIntentosFallidos = 3;

        public bool PuedeIniciarSesion(UsuarioBE usuario) //verifica si el usuario puede iniciar sesión según su estado actual
        {
            ValidarUsuario(usuario);

            return usuario.Estado && !usuario.Bloqueado && !usuario.RequiereRestablecerContrasena;
        }

        public void RegistrarIntentoFallido(UsuarioBE usuario) //registra un intento de login fallido y actualiza el estado en caso de superar el limite
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

        public void RegistrarIngresoExitoso(UsuarioBE usuario) //registra un ingreso exitoso y reinicia el contador de intentos fallidos
        {
            ValidarUsuario(usuario);
            usuario.IntentosFallidos = 0;
        }
       
        public void Desbloquear(UsuarioBE usuario) //desbloquea al usuario y reinicia el contador de intentos fallidos
        {
            ValidarUsuario(usuario);
            usuario.Bloqueado = false;
            usuario.IntentosFallidos = 0;
        }
        private void ValidarUsuario(UsuarioBE usuario) //metodo privado para validar que el objeto UsuarioBE no sea nulo antes de realizar operaciones sobre él
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }
        }
    }
}

        
