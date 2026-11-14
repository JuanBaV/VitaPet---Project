using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.GUI.Presentacion;

public partial class ConsultaBitacoraPage : PaginaBase
{
    private const string PrefijoConsulta = "ConsultaBitacora:";
    private bool accesoAutorizado;
    private bool salidaXml;
    private string errorInicial;
    private readonly SecurityManager seguridad = new SecurityManager();
    private readonly ConsultaBitacoraBLL bitacora = new ConsultaBitacoraBLL();
    protected override string[] RolesPermitidos { get { return new[] { "Webmaster" }; } }

    protected override void OnPreInit(EventArgs e)
    {
        // Before loading posted values or dispatching any event, validate the actual account.
        try { seguridad.ExigirWebmaster("ConsultaBitacora"); accesoAutorizado = true; }
        catch (UnauthorizedAccessException) { DenegarAcceso(); }
        catch (SqlException) { errorInicial = "ER003"; }
        catch (ConfigurationErrorsException) { errorInicial = "ER003"; }
        catch (InvalidOperationException) { errorInicial = "ER023"; }
        ViewStateUserKey = Session.SessionID;
        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        base.OnPreInit(e);
    }

    protected override void MostrarMensajes()
    {
        pnlMensaje.Visible = false;
        litMensaje.Text = string.Empty;
    }

    protected override void EjecutarFuncionalidad()
    {
        if (!accesoAutorizado)
        {
            btnConsultar.Enabled = btnExportarXml.Enabled = false;
            MostrarError(errorInicial ?? "ER003");
            return;
        }
        if (!IsPostBack) Ejecutar(() => Consultar(LeerFiltros()));
    }

    protected void Consultar_Click(object sender, EventArgs e)
    {
        Ejecutar(() => Consultar(LeerFiltros()));
    }

    protected void Bitacora_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        Ejecutar(() =>
        {
            var filtro = LeerFiltros();
            var consulta = ObtenerConsulta();
            if (consulta == null || !Iguales(consulta.Filtro, filtro))
            {
                Consultar(filtro);
                return;
            }
            Vincular(consulta, e.NewPageIndex);
        });
    }

    protected void ExportarXml_Click(object sender, EventArgs e)
    {
        Ejecutar(() =>
        {
            var filtro = LeerFiltros();
            var consulta = ObtenerConsulta();
            // Apply edited filters even if the user clicks Export without first clicking Consultar.
            if (consulta == null || !Iguales(consulta.Filtro, filtro))
                consulta = Consultar(filtro);
            if (consulta.Registros.Count == 0) { MostrarError("ER019"); return; }
            // salida xml bitacora-{yyyyMMdd-HHmmss}.xml
            byte[] xml = bitacora.ExportarXml(consulta.Registros);
            Response.Clear();
            Response.ContentType = "application/xml";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            Response.AddHeader("Content-Disposition", "attachment; filename=bitacora-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".xml");
            Response.AddHeader("X-Content-Type-Options", "nosniff");
            Response.BinaryWrite(xml);
            salidaXml = true;
            Context.ApplicationInstance.CompleteRequest();
        }, true);
    }

    protected override void Render(HtmlTextWriter writer)
    {
        if (!salidaXml) base.Render(writer);
    }

    private ResultadoConsulta Consultar(FiltroBitacoraBE filtro)
    {
        var consulta = new ResultadoConsulta { Filtro = filtro, Registros = bitacora.Consultar(filtro) };
        consulta.SoloHistoricoIntegridad = bitacora.SoloHistoricoIntegridad;
        string clave = ViewState["ConsultaId"] as string;
        if (clave == null)
        {
            // Bound server-side snapshots; independent tabs never share their selected filters.
            var anteriores = Session.Keys.Cast<string>().Where(k => k.StartsWith(PrefijoConsulta, StringComparison.Ordinal)).ToList();
            foreach (string anterior in anteriores.Take(Math.Max(0, anteriores.Count - 4))) Session.Remove(anterior);
            clave = PrefijoConsulta + Guid.NewGuid().ToString("N");
            ViewState["ConsultaId"] = clave;
        }
        Session[clave] = consulta;
        Vincular(consulta, 0);
        if (consulta.Registros.Count == 0) MostrarError("ER019");
        return consulta;
    }

    private ResultadoConsulta ObtenerConsulta()
    {
        string clave = ViewState["ConsultaId"] as string;
        return clave == null ? null : Session[clave] as ResultadoConsulta;
    }

    private void Vincular(ResultadoConsulta consulta, int pagina)
    {
        int paginas = Math.Max(1, (consulta.Registros.Count + 19) / 20);
        gvBitacora.PageIndex = Math.Max(0, Math.Min(pagina, paginas - 1));
        gvBitacora.DataSource = consulta.Registros;
        gvBitacora.DataBind();
        btnExportarXml.Enabled = !consulta.SoloHistoricoIntegridad && consulta.Registros.Count > 0;
        if (consulta.SoloHistoricoIntegridad)
        {
            litMensaje.Text = Server.HtmlEncode(TextosUI.Obtener("Bitacora_Recuperacion"));
            pnlMensaje.Visible = true;
        }
        litResultados.Text = TextosUI.Formato("ResumenEventos", consulta.Registros.Count, gvBitacora.PageIndex + 1, paginas, gvBitacora.PageSize);
    }

    private FiltroBitacoraBE LeerFiltros()
    {
        var filtro = new FiltroBitacoraBE {
            FechaDesde = LeerFecha(dtpFechaDesde.Value), FechaHasta = LeerFecha(dtpFechaHasta.Value),
            Criticidad = ddlCriticidad.SelectedValue
        };
        ConsultaBitacoraBLL.ValidarFiltro(filtro);
        return filtro;
    }

    private static DateTime? LeerFecha(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        DateTime fecha;
        if (!DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            throw new BitacoraException("ER025", "Datos inválidos.");
        return fecha;
    }

    private static bool Iguales(FiltroBitacoraBE a, FiltroBitacoraBE b)
    {
        return a.FechaDesde == b.FechaDesde && a.FechaHasta == b.FechaHasta && a.Criticidad == b.Criticidad;
    }

    private void Ejecutar(Action accion, bool exportando = false)
    {
        if (!accesoAutorizado) return;
        try { accion(); }
        catch (BitacoraException ex) { MostrarError(ex.Codigo); }
        catch (UnauthorizedAccessException) { DenegarAcceso(); }
        catch (SqlException) { MostrarError("ER003"); }
        catch (ConfigurationErrorsException) { MostrarError("ER003"); }
        catch (IOException) { MostrarError(exportando ? "ER015" : "ER031"); }
        catch (System.Xml.XmlException) { MostrarError(exportando ? "ER015" : "ER031"); }
        catch (TransactionException) { MostrarError("ER032"); }
        catch (InvalidOperationException) { MostrarError("ER032"); }
    }

    private void MostrarError(string codigo)
    {
        string mensaje;
        switch (codigo)
        {
            case "ER003": mensaje = "Fallo de conexión o configuración. Reintentá la operación."; break;
            case "ER015": mensaje = "No se pudo generar la salida XML."; break;
            case "ER019": mensaje = "No se encontraron eventos para los filtros indicados."; break;
            case "ER023": mensaje = "Inconsistencia en la verificación de integridad."; break;
            case "ER025": mensaje = "Datos inválidos. Revisá las fechas y la criticidad."; break;
            case "ER027": mensaje = "La fecha desde es posterior a la fecha hasta."; break;
            case "ER031": mensaje = "No se pudo leer el histórico XML de bitácora."; break;
            case "ER033": mensaje = "No se pudo registrar la consulta en bitácora."; break;
            default: mensaje = "No se pudo completar la consulta de bitácora."; break;
        }
        try
        {
            var error = seguridad.ObtenerError(codigo);
            if (error != null) mensaje = error.Descripcion;
        }
        catch (SqlException) { }
        catch (ConfigurationErrorsException) { }
        // Never continue exporting a stale result after an integrity or source-read error.
        if (codigo != "ER025" && codigo != "ER027" && codigo != "ER015")
        {
            string clave = ViewState["ConsultaId"] as string;
            if (clave != null) Session.Remove(clave);
            gvBitacora.DataSource = new BitacoraBE[0]; gvBitacora.DataBind();
            btnExportarXml.Enabled = false; litResultados.Text = string.Empty;
        }
        litMensaje.Text = Server.HtmlEncode(codigo + " - " + TextosUI.Error(codigo, mensaje));
        pnlMensaje.Visible = true;
    }

    private void DenegarAcceso()
    {
        // Redirect(true) terminates the handler; CompleteRequest alone would still dispatch postbacks.
        Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"), true);
    }

    private sealed class ResultadoConsulta
    {
        public FiltroBitacoraBE Filtro { get; set; }
        public List<BitacoraBE> Registros { get; set; }
        public bool SoloHistoricoIntegridad { get; set; }
    }
}
