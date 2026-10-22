using VitaPet.GUI.Presentacion;

public partial class ClientePage : PaginaClientePlanes
{
    protected override string[] RolesPermitidos { get { return new[] { "Cliente" }; } }
    protected override void EjecutarFuncionalidad() {if(!IsPostBack)EjecutarPlan(()=>ucFacturas.Vincular(new VitaPet.BLL.FacturaBLL().ListarPropias()));}
}
