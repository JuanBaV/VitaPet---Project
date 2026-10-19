using System;
using System.Globalization;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class EmpresasPage : PaginaEmpresa
{
    private int Seleccionada { get { return gvEmpresas.SelectedDataKey==null ? 0 : (int)gvEmpresas.SelectedDataKey.Value; } }
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if (!IsPostBack) EjecutarEmpresa(()=>{gvEmpresas.DataSource=new EmpresaBLL().Listar();gvEmpresas.DataBind();});
    }
    protected void Seleccionar(object sender,EventArgs e)
    {
        btnConsultar.Enabled=btnModificar.Enabled=false;
        EjecutarEmpresa(()=>{new EmpresaBLL().Seleccionar(Seleccionada);btnConsultar.Enabled=btnModificar.Enabled=true;});
    }
    private void Abrir(string pagina)
    {
        EjecutarEmpresa(()=>{
            var empresa=new EmpresaBLL().Seleccionar(Seleccionada);
            Response.Redirect(ResolveUrl("~/"+pagina)+"?id="+empresa.IdEmpresa.ToString(CultureInfo.InvariantCulture),false);
            Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected void Consultar(object sender,EventArgs e) { Abrir("ConsultaEmpresa.aspx"); }
    protected void Modificar(object sender,EventArgs e) { Abrir("ModificarEmpresa.aspx"); }
    protected override void AlFallarEmpresa(string codigo)
    { gvEmpresas.SelectedIndex=-1;btnConsultar.Enabled=btnModificar.Enabled=false; }
}
