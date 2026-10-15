using VitaPet.BE;
using VitaPet.GUI.Presentacion;
public partial class DesbloquearUsuarioPage : OperacionUsuarioPage
{
    protected override OperacionUsuario Operacion { get { return OperacionUsuario.Desbloquear; } }
}
