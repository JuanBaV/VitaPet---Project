using System;
using System.Globalization;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class GestionPagoPage : PaginaClientePlanes
{
    private int IdSuscripcion { get {int id;return int.TryParse(Request.QueryString["contratacion"],out id)&&id>0?id:0;} }
    protected override void EjecutarFuncionalidad()
    {
        pnlDummy.Visible=GestionPagoBLL.EsPruebaLocal && pnlPendiente.Visible;
        if(!IsPostBack)EjecutarPlan(()=>{
            if(IdSuscripcion==0)throw new GestionPagoException("ER054");
            ViewState["Suscripcion"]=IdSuscripcion;
            if(Request.QueryString["comprobante"]=="1")MostrarComprobante();
            else CargarResumen();
        });
    }
    private void CargarResumen()
    {
        ResumenPagoBE c;
        try {c=new GestionPagoBLL().Consultar(IdSuscripcion);}
        catch(GestionPagoException ex) when(ex.Codigo=="ER054")
        {
            // Si el cliente regresa desde planes tras haber pagado, puede continuar
            // desde su comprobante, sin volver a presentar datos de tarjeta.
            MostrarErrorPlan("ER054");MostrarComprobante();return;
        }
        pnlPendiente.Visible=true;pnlComprobante.Visible=false;
        lblNombrePlan.Text=Server.HtmlEncode(c.NombrePlan);lblPrecioMascota.Text=USD(c.PrecioMascota);
        lblCantidadMascotas.Text=c.CantidadMascotas.ToString();lblImporte.Text=USD(c.Importe);
        ViewState["ImporteConfirmado"]=c.Importe;
        pnlDummy.Visible=GestionPagoBLL.EsPruebaLocal;
    }
    private void MostrarComprobante()
    {
        var p=new GestionPagoBLL().ObtenerComprobante(IdSuscripcion);
        pnlPendiente.Visible=false;pnlComprobante.Visible=true;pnlDummy.Visible=false;
        lblNumeroPago.Text=p.IdPago.ToString();lblPlanPagado.Text=Server.HtmlEncode(p.NombrePlan);
        lblImportePagado.Text=USD(p.Importe);
        lblMedioPagado.Text=TextosUI.Obtener(p.MedioPago==GestionPagoBLL.Credito?"Pago_Credito":"Pago_Debito");
        lblFechaPago.Text=p.FechaPago.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture);
    }
    private void ValidarContexto()
    {
        if(IdSuscripcion==0||(ViewState["Suscripcion"] as int? ?? 0)!=IdSuscripcion)
            throw new GestionPagoException("ER054");
    }
    protected void Pagar_Click(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            ValidarContexto();
            if(!(ViewState["ImporteConfirmado"] is decimal))throw new GestionPagoException("ER054");
            new GestionPagoBLL().Pagar(IdSuscripcion,(decimal)ViewState["ImporteConfirmado"],new DatosPagoBE {
                MedioPago=ddlMedioPago.SelectedValue,Titular=txtTitular.Text,NumeroTarjeta=txtNumeroTarjeta.Text,
                Vencimiento=txtVencimiento.Text,CodigoSeguridad=txtCodigoSeguridad.Text});
            Response.Redirect(ResolveUrl("~/GestionPago.aspx")+"?contratacion="+IdSuscripcion+"&comprobante=1",false);
            Context.ApplicationInstance.CompleteRequest();
        });
        txtTitular.Text=txtNumeroTarjeta.Text=txtVencimiento.Text=txtCodigoSeguridad.Text="";
    }
    protected void Continuar_Click(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            ValidarContexto();new GestionPagoBLL().ObtenerComprobante(IdSuscripcion);
            new ContratacionPlanBLL().ConfirmarPago(IdSuscripcion);
            Session["PlanContratado"]=true;
            Response.Redirect(ResolveUrl("~/Planes.aspx"),false);Context.ApplicationInstance.CompleteRequest();
        });
    }
    private static string USD(decimal value){return "USD "+value.ToString("N2",CultureInfo.InvariantCulture);}
}
