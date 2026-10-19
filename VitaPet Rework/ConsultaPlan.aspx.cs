using System;
using System.Globalization;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class ConsultaPlanPage:PaginaPlan
{
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if(!IsPostBack){pnlDatos.Visible=false;EjecutarPlan(()=>{
            var p=new PlanBLL().Consultar(IdPlan);lblIdPlan.Text=Server.HtmlEncode(p.IdPlan.ToString(CultureInfo.InvariantCulture));
            lblNombrePlan.Text=Server.HtmlEncode(p.Nombre);lblDescripcionPlan.Text=Server.HtmlEncode(p.Descripcion);
            lblPrecioPlan.Text=p.Precio.HasValue?Server.HtmlEncode("USD "+p.Precio.Value.ToString("0.00",CultureInfo.CurrentCulture)):"—";
            lblEstadoPlan.Text=TextosUI.Obtener(p.Estado?"Empresa_Activo":"Empresa_Inactivo");gvCoberturas.DataSource=p.Coberturas;gvCoberturas.DataBind();pnlDatos.Visible=true;
            if(p.Coberturas.Count==0)throw new PlanException("ER008");
        });}
    }
}
