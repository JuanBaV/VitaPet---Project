using System;
using System.Globalization;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class GestionPlanesPage:PaginaPlan
{
    private int Seleccionado {get{return gvPlanes.SelectedDataKey==null?0:(int)gvPlanes.SelectedDataKey.Value;}}
    protected override void EjecutarFuncionalidad()
    {pnlMensaje.Visible=false;if(!IsPostBack)EjecutarPlan(()=>{gvPlanes.DataSource=new PlanBLL().Listar();gvPlanes.DataBind();});}
    protected void Seleccionar(object sender,EventArgs e)
    {btnConsultarPlan.Enabled=btnModificarPlan.Enabled=false;EjecutarPlan(()=>{new PlanBLL().Seleccionar(Seleccionado);btnConsultarPlan.Enabled=btnModificarPlan.Enabled=true;});}
    private void Abrir(string pagina)
    {EjecutarPlan(()=>{var p=new PlanBLL().Seleccionar(Seleccionado);Response.Redirect(ResolveUrl("~/"+pagina)+"?id="+p.IdPlan.ToString(CultureInfo.InvariantCulture),false);Context.ApplicationInstance.CompleteRequest();});}
    protected void Consultar(object sender,EventArgs e){Abrir("ConsultaPlan.aspx");}
    protected void Modificar(object sender,EventArgs e){Abrir("ModificarPlan.aspx");}
    protected override void AlFallarPlan(string codigo){gvPlanes.SelectedIndex=-1;btnConsultarPlan.Enabled=btnModificarPlan.Enabled=false;}
}
