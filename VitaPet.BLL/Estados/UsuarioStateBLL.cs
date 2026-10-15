using System;
using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    public class UsuarioStateBLL
    {
        private readonly UsuarioBE _usuario;
        private IUsuarioStateBLL _estadoActual;

        public UsuarioStateBLL(UsuarioBE usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }

            _usuario = usuario;
            ActualizarEstado();
        }

        public UsuarioBE Usuario
        {
            get
            {
                return _usuario;
            }
        }

        public bool PuedeIniciarSesion()
        {
            return _estadoActual.PuedeIniciarSesion(_usuario);
        }

        public void RegistrarIntentoFallido()
        {
            _estadoActual.RegistrarIntentoFallido(_usuario);
            ActualizarEstado();
        }

        public void RegistrarIngresoExitoso()
        {
            _estadoActual.RegistrarIngresoExitoso(_usuario);
            ActualizarEstado();
        }

        public void Desbloquear()
        {
            _estadoActual.Desbloquear(_usuario);
            ActualizarEstado();
        }

        private void ActualizarEstado()
        {
            if (_usuario.Bloqueado)
            {
                _estadoActual = new UsuarioBloqueadoStateBLL();
                return;
            }

            _estadoActual = new UsuarioActivoStateBLL();
        }
    }
}
