using VitaPet.GUI.Presentacion;
public partial class MascotasPage : PaginaBase
{
    protected override string[] RolesPermitidos {get{return new[]{"Cliente"};}}
    protected override void EjecutarFuncionalidad(){}
}
