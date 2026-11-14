using System;
using System.Globalization;
using System.Net;
using System.Web;
using System.Web.Services.Protocols;
using System.Web.UI;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.GUI.WebServices;

public partial class ConsultaCoberturaPage : PaginaClientePlanes
{
    private bool descargando;
    protected override void EjecutarFuncionalidad()
    {
        pnlCobertura.Visible=false;pnlMensaje.Visible=false;
        EjecutarPlan(CargarCobertura);
    }
    private void CargarCobertura()
    {
        try
        {
            CoberturaService servicio=new CoberturaService();
            var resultado=servicio.ObtenerCobertura();
            if(resultado==null)throw new ConsultaCoberturaException("ER058");
            if(!string.IsNullOrEmpty(resultado.CodigoError))throw new ConsultaCoberturaException(resultado.CodigoError);
            var c=resultado.Consulta;
            if(c==null||c.Coberturas==null)throw new ConsultaCoberturaException("ER058");
            lblNombrePlan.Text=Server.HtmlEncode(c.NombrePlan);
            lblDescripcionPlan.Text=Server.HtmlEncode(c.DescripcionPlan);
            lblEstadoSuscripcion.Text=TextosUI.Obtener("Cobertura_Activa");
            lblFechaAlta.Text=c.FechaAlta.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture);
            lblFechaVencimiento.Text=c.FechaVencimiento.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture);
            gvCoberturas.DataSource=c.Coberturas;gvCoberturas.DataBind();
            if(gvCoberturas.HeaderRow!=null)gvCoberturas.HeaderRow.TableSection=System.Web.UI.WebControls.TableRowSection.TableHeader;
            pnlCobertura.Visible=true;
        }
        catch(SoapException){throw new UnauthorizedAccessException("Acceso denegado al servicio de cobertura.");}
        catch(HttpException ex){throw new ConsultaCoberturaException("ER058",ex);}
        catch(WebException ex){throw new ConsultaCoberturaException("ER058",ex);}
    }
    protected void ExportarXml(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            // salida xml Cobertura.xml
            byte[] contenido=new ConsultaCoberturaBLL().ExportarXml();
            Response.Clear();Response.ContentType="application/xml";Response.ContentEncoding=System.Text.Encoding.UTF8;
            Response.Charset="utf-8";Response.AddHeader("Content-Disposition","attachment; filename=Cobertura.xml");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);Response.Cache.SetNoStore();
            Response.AddHeader("X-Content-Type-Options","nosniff");
            Response.BinaryWrite(contenido);descargando=true;Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void Render(HtmlTextWriter writer){if(!descargando)base.Render(writer);}
}
