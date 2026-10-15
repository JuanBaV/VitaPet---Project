using System;
using System.Collections.Generic;
using VitaPet.BE;

public partial class Controls_ListadoPlanes : System.Web.UI.UserControl
{
    public event EventHandler PlanSeleccionado;
    public int IdPlanSeleccionado { get { return gvPlanes.SelectedDataKey==null?0:Convert.ToInt32(gvPlanes.SelectedDataKey.Value); } }
    public void Vincular(IEnumerable<PlanBE> planes){gvPlanes.DataSource=planes;gvPlanes.DataBind();}
    protected void SeleccionarPlan(object sender,EventArgs e){var handler=PlanSeleccionado;if(handler!=null)handler(this,EventArgs.Empty);}
}
