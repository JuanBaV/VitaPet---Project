using VitaPet.BE;
using VitaPet.GUI.Presentacion;
public partial class ConsultarUsuarioPage : OperacionUsuarioPage
{
    protected override OperacionUsuario Operacion { get { return OperacionUsuario.Consultar; } }
}
