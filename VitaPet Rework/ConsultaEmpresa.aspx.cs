using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class ConsultaEmpresaPage : PaginaEmpresa
{
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if (!IsPostBack)
        {
            pnlDatos.Visible=false;
            EjecutarEmpresa(()=>{
                var empresa=new EmpresaBLL().Consultar(IdEmpresa);
                lblIdEmpresa.Text=Server.HtmlEncode(empresa.IdEmpresa.ToString(System.Globalization.CultureInfo.InvariantCulture));
                lblRazonSocial.Text=Server.HtmlEncode(empresa.RazonSocial);lblCuit.Text=Server.HtmlEncode(empresa.Cuit);
                lblDireccion.Text=Server.HtmlEncode(empresa.Direccion);lblTelefono.Text=Server.HtmlEncode(empresa.Telefono);
                lblEstado.Text=TextosUI.Obtener(empresa.Estado ? "Empresa_Activo" : "Empresa_Inactivo");pnlDatos.Visible=true;
            });
        }
    }
    protected override void AlFallarEmpresa(string codigo) { pnlDatos.Visible=false; }
}
