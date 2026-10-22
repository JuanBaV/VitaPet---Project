using System;
using System.Globalization;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class GestionPagoPage : PaginaClientePlanes
{
    private bool descarga;
    private int IdSuscripcion { get {int id;return int.TryParse(Request.QueryString["contratacion"],out id)&&id>0?id:0;} }
    protected override void EjecutarFuncionalidad()
    {
        if (!IsPostBack && Request.QueryString["origen"] == "perfil")
            lnkVolver.NavigateUrl = "~/Cliente.aspx";
        if(!IsPostBack && Request.QueryString["descargar"]=="1"){DescargarFactura_Click(this,EventArgs.Empty);return;}
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
        lnkVolver.NavigateUrl="~/ContratarPlan.aspx?plan="+c.IdPlan.ToString(CultureInfo.InvariantCulture);
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
        pnlMensaje.Visible=false;
        lblNumeroFactura.Text=p.Factura==null?TextosUI.Obtener("Factura_Pendiente"):Server.HtmlEncode(p.Factura.Numero);
        btnDescargarFactura.Visible=p.Factura!=null;btnReintentarFactura.Visible=p.Factura==null;
        pnlCertificado.Visible=false;
        if(GestionPagoBLL.EsPruebaLocal)MostrarCertificado();
        MostrarCorreo(p.Factura);
        if(p.CodigoErrorFactura!=null)MostrarErrorPlan(p.CodigoErrorFactura);
    }
    private void MostrarCorreo(FacturaBE factura)
    {
        pnlCorreo.Visible=factura!=null&&!string.IsNullOrWhiteSpace(factura.CorreoFactura);
        if(!pnlCorreo.Visible)return;
        bool enviado=factura.EstadoCorreo=="Enviado";
        lblEstadoCorreo.Text=Server.HtmlEncode(enviado?TextosUI.Formato("Factura_CorreoEnviado",factura.DestinatarioCorreo):
            TextosUI.Obtener(factura.EstadoCorreo=="SinConfigurar"?"Factura_CorreoSinConfigurar":"Factura_CorreoPendiente"));
        btnReintentarCorreo.Visible=!enviado;
    }
    protected void ReintentarCorreo_Click(object sender,EventArgs e)
    {EjecutarPlan(()=>{ValidarContexto();MostrarCorreo(new FacturaBLL().ReintentarCorreo(IdSuscripcion));});}
    private void MostrarCertificado()
    {
        pnlCertificado.Visible=false;
        var certificado=new GestionPagoBLL().ObtenerCertificado(IdSuscripcion);
        lblEmisorCertificado.Text=Server.HtmlEncode(certificado.Emisor);
        lblSerieCertificado.Text=Server.HtmlEncode(certificado.Serie);
        lblHuellaCertificado.Text=Server.HtmlEncode(certificado.Huella);
        lblVigenciaCertificado.Text=certificado.Desde.ToString("dd/MM/yyyy")+" - "+certificado.Hasta.ToString("dd/MM/yyyy");
        pnlCertificado.Visible=true;
    }
    protected void VerificarCertificado_Click(object sender,EventArgs e)
    {EjecutarPlan(()=>{ValidarContexto();MostrarCertificado();});}
    protected void DescargarCertificado_Click(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            ValidarContexto();var certificado=new GestionPagoBLL().ObtenerCertificado(IdSuscripcion);
            bool autorizacion=ReferenceEquals(sender,btnDescargarAutorizacion);
            byte[] datos=autorizacion?certificado.Autorizacion:certificado.Certificado;
            Response.Clear();Response.ContentType=autorizacion?"application/pkcs7-mime":"application/pkix-cert";
            Response.AddHeader("Content-Disposition","attachment; filename=VitaPet-"+IdSuscripcion+(autorizacion?"-autorizacion.p7m":"-certificado.cer"));
            Response.AddHeader("Content-Length",datos.Length.ToString(CultureInfo.InvariantCulture));
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);Response.Cache.SetNoStore();
            Response.BinaryWrite(datos);descarga=true;Context.ApplicationInstance.CompleteRequest();
        });
    }
    private void ValidarContexto()
    {
        if(IdSuscripcion==0||(ViewState["Suscripcion"] as int? ?? 0)!=IdSuscripcion)
            throw new GestionPagoException("ER054");
    }
    protected void Pagar_Click(object sender,EventArgs e)
    {
        Page.Validate("Pago");
        if(!Page.IsValid)return;
        EjecutarPlan(()=>{
            ValidarContexto();
            if(!(ViewState["ImporteConfirmado"] is decimal))throw new GestionPagoException("ER054");
            new GestionPagoBLL().Pagar(IdSuscripcion,(decimal)ViewState["ImporteConfirmado"],new DatosPagoBE {
                MedioPago=ddlMedioPago.SelectedValue,Titular=txtTitular.Text,NumeroTarjeta=txtNumeroTarjeta.Text,
                Vencimiento=txtVencimiento.Text,CodigoSeguridad=txtCodigoSeguridad.Text,CorreoFactura=ucCorreoFactura.Correo});
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
    protected void ReintentarFactura_Click(object sender,EventArgs e)
    {EjecutarPlan(()=>{ValidarContexto();new FacturaBLL().Emitir(IdSuscripcion);MostrarComprobante();});}
    protected void DescargarFactura_Click(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            byte[] pdf=new FacturaBLL().Descargar(IdSuscripcion);Response.Clear();Response.ContentType="application/pdf";
            Response.AddHeader("Content-Disposition","attachment; filename=Factura-VitaPet-"+IdSuscripcion+".pdf");
            Response.AddHeader("Content-Length",pdf.Length.ToString(CultureInfo.InvariantCulture));
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);Response.Cache.SetNoStore();
            Response.BinaryWrite(pdf);descarga=true;Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void Render(System.Web.UI.HtmlTextWriter writer){if(!descarga)base.Render(writer);}
    private static string USD(decimal value){return "USD "+value.ToString("N2",CultureInfo.InvariantCulture);}
}
