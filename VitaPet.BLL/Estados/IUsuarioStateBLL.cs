using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    //contrato del patron State para la gestion de estados de usuario en la BLL
    public interface IUsuarioStateBLL
    {
        bool PuedeIniciarSesion(UsuarioBE usuario); //verifica si el usuario puede iniciar sesión según su estado actual

        void RegistrarIntentoFallido(UsuarioBE usuario); //registra un intento fallido de inicio de sesión y actualiza el estado del usuario según las reglas de negocio

        void RegistrarIngresoExitoso(UsuarioBE usuario); //registra un ingreso exitoso y actualiza el estado del usuario según las reglas de negocio

        void Desbloquear(UsuarioBE usuario); //desbloquea al usuario y actualiza su estado según las reglas de negocio
    }
}
