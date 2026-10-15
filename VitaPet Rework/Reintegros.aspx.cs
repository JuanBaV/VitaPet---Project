using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class ReintegrosPage : PaginaClientePlanes
{
    private bool descargando;
    private int? Seleccionado
    {
        get { return ViewState["ReintegroConsultado"] as int?; }
        set { ViewState["ReintegroConsultado"]=value; }
    }
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if(!IsPostBack)Ejecutar(()=>{
            gvReintegros.DataSource=new ConsultaReintegroBLL().Listar();gvReintegros.DataBind();
            if(gvReintegros.HeaderRow!=null)gvReintegros.HeaderRow.TableSection=TableRowSection.TableHeader;
        });
    }
    private void LimpiarDetalle()
    {
        Seleccionado=null;pnlDetalle.Visible=false;btnExportarXml.Enabled=false;
        lblNumeroReintegro.Text=lblFechaSolicitud.Text=lblImporte.Text=lblEstado.Text=lblDetalle.Text=string.Empty;
    }
    private void Ejecutar(Action accion)
    {
        EjecutarPlan(()=>{
            try{accion();}
            catch(ConsultaReintegroException ex)
            {
                if(ex.Codigo!="ER015")LimpiarDetalle();
                if(ex.Codigo=="ER023"||ex.Codigo=="ER003"||ex.Codigo=="ER009"||ex.Codigo=="ER059")
                {gvReintegros.DataSource=null;gvReintegros.DataBind();}
                throw;
            }
        });
    }
    protected void ConsultarReintegro(object sender,GridViewCommandEventArgs e)
    {
        if(e.CommandName!="Consultar")return;
        Ejecutar(()=>{
            LimpiarDetalle();int fila;
            if(!int.TryParse(Convert.ToString(e.CommandArgument,CultureInfo.InvariantCulture),out fila)||fila<0||fila>=gvReintegros.DataKeys.Count)
                throw new ConsultaReintegroException("ER011");
            int id=(int)gvReintegros.DataKeys[fila].Value;
            var dato=new ConsultaReintegroBLL().Consultar(id);
            lblNumeroReintegro.Text=dato.IdReintegro.ToString(CultureInfo.InvariantCulture);
            lblFechaSolicitud.Text=dato.FechaSolicitud.ToString("dd/MM/yyyy HH:mm",CultureInfo.InvariantCulture);
            lblImporte.Text=dato.Importe.ToString("N2",CultureInfo.CurrentCulture);
            lblEstado.Text=Server.HtmlEncode(TextosUI.Traducir(dato.Estado));lblDetalle.Text=Server.HtmlEncode(dato.Detalle??string.Empty);
            Seleccionado=id;pnlDetalle.Visible=true;btnExportarXml.Enabled=true;
        });
    }
    protected void ExportarXml(object sender,EventArgs e)
    {
        Ejecutar(()=>{
            if(!Seleccionado.HasValue)throw new ConsultaReintegroException("ER011");
            byte[] contenido=new ConsultaReintegroBLL().ExportarXml(Seleccionado.Value);
            Response.Clear();Response.ContentType="application/xml";Response.ContentEncoding=System.Text.Encoding.UTF8;
            Response.Charset="utf-8";Response.AddHeader("Content-Disposition","attachment; filename=Reintegros.xml");
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);Response.Cache.SetNoStore();
            Response.BinaryWrite(contenido);descargando=true;Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void OnPreRender(EventArgs e)
    {
        if(pnlMensaje.Visible&&!litMensaje.Text.StartsWith("ER015",StringComparison.Ordinal))LimpiarDetalle();
        if(gvReintegros.HeaderRow!=null)gvReintegros.HeaderRow.TableSection=TableRowSection.TableHeader;
        base.OnPreRender(e);
    }
    protected override void Render(HtmlTextWriter writer){if(!descargando)base.Render(writer);}
}
