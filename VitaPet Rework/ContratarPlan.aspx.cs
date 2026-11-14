using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Services.Protocols;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.GUI.WebServices;

public partial class ContratarPlanPage : PaginaClientePlanes
{
    protected int IdPlan {get{int id;return int.TryParse(Request.QueryString["plan"],out id)?id:0;}}
    protected override void EjecutarFuncionalidad(){if(!IsPostBack)EjecutarPlan(Cargar);}
    private void Cargar()
    {
        var c=new ContratacionPlanBLL().ConsultarDetalle(IdPlan);pnlDetalle.Visible=true;
        lblNombrePlan.Text=Server.HtmlEncode(c.Plan.Nombre);lblDescripcionPlan.Text=Server.HtmlEncode(c.Plan.Descripcion);
        lblPrecioPlan.Text="USD "+c.Plan.Precio.ToString("N2");lblCantidadMascotas.Text=c.CantidadMascotas.ToString();lblTotalMensual.Text="USD "+c.ImporteUSD.ToString("N2");
        ViewState["VersionPlan"]=c.Plan.Version;ViewState["CantidadMascotas"]=c.CantidadMascotas;
        CargarCobertura();
        if(c.CantidadMascotas==0){btnContratar.Enabled=false;MostrarErrorPlan("ER052");}
    }
    private void CargarCobertura()
    {
        gvCoberturas.DataSource = new List<CoberturaBE>();
        try
        {
            CoberturaService service = new CoberturaService(); //crear instancia de CoberturaService
            ResultadoCoberturaBE resultado = service.ObtenerCobertura(IdPlan); //obtener cobertura del plan mediante CoberturaService
            if (string.IsNullOrEmpty(resultado.CodigoError))
                gvCoberturas.DataSource = resultado.Coberturas;
            else
                MostrarErrorCobertura(resultado.CodigoError);
        }
        catch (SoapException)
        {
            throw new UnauthorizedAccessException("Acceso denegado al servicio de coberturas.");
        }
        catch (HttpException)
        {
            MostrarErrorCobertura("ER003");
        }
        gvCoberturas.DataBind();
    }
    private void MostrarErrorCobertura(string codigo)
    {
        lblErrorCobertura.Text = Server.HtmlEncode(codigo + " - " +
            TextosUI.Error(codigo, TextoErrorPlan(codigo)));
    }
    protected void Contratar_Click(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            var c=new ContratacionPlanBLL().PrepararContratacion(IdPlan,ViewState["VersionPlan"] as string,ViewState["CantidadMascotas"] as int? ?? -1);
            Response.Redirect(ResolveUrl("~/GestionPago.aspx")+"?contratacion="+c.IdSuscripcion,false);Context.ApplicationInstance.CompleteRequest();
        });
    }
}
