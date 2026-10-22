using System;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class PlanesPage : PaginaClientePlanes
{
    protected override void EjecutarFuncionalidad(){if(!IsPostBack)EjecutarPlan(()=>{
        ucFacturas.Vincular(new FacturaBLL().ListarPropias());
        ucPlanes.Vincular(new ContratacionPlanBLL().ConsultarPlanes());
        if(Session["PlanContratado"] as bool? == true){
            Session.Remove("PlanContratado");pnlMensaje.Visible=true;
            pnlMensaje.CssClass="login-message registro-confirmacion";
            litMensaje.Text=Server.HtmlEncode(TextosUI.Obtener("Pago_ContratacionConfirmada"));
        }
    });}
    protected void PlanSeleccionado(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            int id=ucPlanes.IdPlanSeleccionado;
            new ContratacionPlanBLL().ConsultarDetalle(id);
            Response.Redirect(ResolveUrl("~/ContratarPlan.aspx")+"?plan="+id,false);Context.ApplicationInstance.CompleteRequest();
        });
    }
}
