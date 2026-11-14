using System;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class AltaPlanPage:PaginaPlan
{
    protected override void EjecutarFuncionalidad() //metodo traido de PaginaBase
    {
        pnlMensaje.Visible=false;
        if(!IsPostBack)EjecutarPlan(()=>{
            var b=new PlanBLL();b.PrepararAlta();int id;
            if(int.TryParse(Convert.ToString(Session["AltaPlanRegistrado"]),out id)&&Request.QueryString["resultado"]==Convert.ToString(Session["AltaPlanToken"]))
            {editorPlan.Cargar(b.Seleccionar(id));pnlDatos.Enabled=false;}
            else editorPlan.Cargar();
        });
    }
    protected void Registrar(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            if(!pnlDatos.Enabled)throw new PlanException("ER025");
            if(!ValidacionAlta.Validar(this,"AltaPlan"))return;
            int id=new PlanBLL().Registrar(editorPlan.Datos());string token=Guid.NewGuid().ToString("N");Session["AltaPlanRegistrado"]=id;Session["AltaPlanToken"]=token;
            Response.Redirect(ResolveUrl("~/AltaPlan.aspx")+"?resultado="+token,false);Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected void ErrorEditor(object sender,ErrorPlanEventArgs e){EjecutarPlan(()=>{throw new PlanException(e.Codigo);});}
    protected override void AlFallarPlan(string codigo){if(codigo!="ER002"&&codigo!="ER025"&&codigo!="ER078")pnlDatos.Enabled=false;}
}
