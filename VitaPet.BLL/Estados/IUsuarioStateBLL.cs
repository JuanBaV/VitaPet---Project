using VitaPet.BE;

namespace VitaPet.BLL.Estados
{
    public interface IUsuarioStateBLL
    {
        bool PuedeIniciarSesion(UsuarioBE usuario);

        void RegistrarIntentoFallido(UsuarioBE usuario);

        void RegistrarIngresoExitoso(UsuarioBE usuario);

        void Desbloquear(UsuarioBE usuario);
    }
}
