using VitaPet.BE;
using VitaPet.GUI.Presentacion;
public partial class BloquearUsuarioPage : OperacionUsuarioPage
{
    protected override OperacionUsuario Operacion { get { return OperacionUsuario.Bloquear; } }
}
