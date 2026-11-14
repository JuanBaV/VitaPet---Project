using System;
using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    public class UsuarioStateBLL
    {
        private readonly UsuarioBE _usuario;
        private IUsuarioStateBLL _estadoActual; //referencia al estado actual del usuario, que implementa la interfaz IUsuarioStateBLL

        public UsuarioStateBLL(UsuarioBE usuario) //constructor que recibe un objeto UsuarioBE y establece el estado inicial del usuario
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

        //metodos traidos de IUsuarioStateBLL para manejar la logica de estado del usuario,
        //delegando la responsabilidad al estado actual
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

        private void ActualizarEstado() //metodo que actualiza el estado actual del usuario
        {
            if (_usuario.Bloqueado)
            {
                _estadoActual = new UsuarioBloqueadoStateBLL(); //si el usuario esta bloqueado, se establece el estado como UsuarioBloqueadoStateBLL
                return;
            }

            _estadoActual = new UsuarioActivoStateBLL(); //si el usuario no esta bloqueado, se establece el estado como UsuarioActivoStateBLL
        }
    }
}
