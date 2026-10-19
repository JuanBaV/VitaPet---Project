using System;
using System.Globalization;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class ModificarPlanPage:PaginaPlan
{
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if(!IsPostBack){pnlDatos.Visible=false;EjecutarPlan(()=>{var p=new PlanBLL().Seleccionar(IdPlan);lblIdPlan.Text=Server.HtmlEncode(p.IdPlan.ToString(CultureInfo.InvariantCulture));editorPlan.Cargar(p);pnlDatos.Visible=true;});}
    }
    protected void Guardar(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            if(!pnlDatos.Visible||!pnlDatos.Enabled)throw new PlanException("ER011");new PlanBLL().Actualizar(IdPlan,editorPlan.Datos());
            Response.Redirect(ResolveUrl("~/ModificarPlan.aspx")+"?id="+IdPlan.ToString(CultureInfo.InvariantCulture),false);Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected void ErrorEditor(object sender,ErrorPlanEventArgs e){EjecutarPlan(()=>{throw new PlanException(e.Codigo);});}
    protected override void AlFallarPlan(string codigo){if(codigo!="ER002"&&codigo!="ER025"&&codigo!="ER078")pnlDatos.Enabled=false;}
}
