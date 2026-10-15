using VitaPet.BE;
using VitaPet.GUI.Presentacion;
public partial class ModificarUsuarioPage : OperacionUsuarioPage
{
    protected override OperacionUsuario Operacion { get { return OperacionUsuario.Modificar; } }
}
