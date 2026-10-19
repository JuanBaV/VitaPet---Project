using System;
using System.Globalization;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class ModificarEmpresaPage : PaginaEmpresa
{
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if (!IsPostBack)
        {
            pnlDatos.Visible=false;
            EjecutarEmpresa(()=>{
                var empresa=new EmpresaBLL().Seleccionar(IdEmpresa);
                lblIdEmpresa.Text=Server.HtmlEncode(empresa.IdEmpresa.ToString(CultureInfo.InvariantCulture));
                txtRazonSocial.Text=empresa.RazonSocial;txtCuit.Text=empresa.Cuit;
                txtDireccion.Text=empresa.Direccion;txtTelefono.Text=empresa.Telefono;
                lblEstado.Text=TextosUI.Obtener(empresa.Estado ? "Empresa_Activo" : "Empresa_Inactivo");pnlDatos.Visible=true;
            });
        }
    }
    protected void Guardar(object sender,EventArgs e)
    {
        EjecutarEmpresa(()=>{
            if (!pnlDatos.Visible||!pnlDatos.Enabled) throw new EmpresaException("ER011");
            new EmpresaBLL().Actualizar(IdEmpresa,new DatosEmpresaBE { RazonSocial=txtRazonSocial.Text,
                Cuit=txtCuit.Text, Direccion=txtDireccion.Text, Telefono=txtTelefono.Text });
            Response.Redirect(ResolveUrl("~/ModificarEmpresa.aspx")+"?id="+IdEmpresa.ToString(CultureInfo.InvariantCulture),false);
            Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void AlFallarEmpresa(string codigo)
    { if (codigo!="ER002"&&codigo!="ER025"&&codigo!="ER068") pnlDatos.Enabled=false; }
}
