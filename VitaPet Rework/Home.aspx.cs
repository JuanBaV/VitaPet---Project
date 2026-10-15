using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class HomePage : PaginaBase
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

    protected override void CargarEncabezado()
    {
        litSaludo.Text = Server.HtmlEncode(
            VitaPet.GUI.Resources.Textos.Saludo +
            SessionManager.Instancia.UsuarioActual.Nombre);
    }

    protected override void EjecutarFuncionalidad()
    {
    }
}
