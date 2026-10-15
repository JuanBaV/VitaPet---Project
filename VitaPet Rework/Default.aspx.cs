using VitaPet.GUI.Presentacion;

public partial class _Default : PaginaBase
{
    protected override string[] RolesPermitidos
    {
        get
        {
            return new[]
            {
                "Cliente",
                "Administrador",
                "Webmaster"
            };
        }
    }

    protected override void EjecutarFuncionalidad()
    {
    }
}
