using VitaPet.GUI.Presentacion;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using VitaPet.BLL;

public partial class CatalogoErroresPage : VitaPet.GUI.Presentacion.PaginaConIdioma
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;
        Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        try
        {
            var errores = new ErrorSistemaBLL().ObtenerTodos();
            AgregarErroresConsultaVirtual(errores);
            rptErrores.DataSource = errores;
            rptErrores.DataBind();
            if (errores.Count == 0) MostrarEstado("El catálogo de errores está vacío.");
        }
        catch (SqlException) { MostrarEstado("ER003 - Fallo de conexión. No se pudo cargar el catálogo."); }
        catch (ConfigurationErrorsException) { MostrarEstado("ER003 - Fallo de conexión. No se pudo cargar el catálogo."); }
    }

    // Mensajes de consulta virtual ya definidos en los recursos del chat.
    // Se incluyen en el catálogo para mantener sus referencias documentales,
    // sin modificar los mensajes ni el comportamiento de la consulta.
    private void AgregarErroresConsultaVirtual(System.Collections.Generic.List<VitaPet.BE.ErrorSistemaBE> errores)
    {
        var mensajes = new[]
        {
            new { Codigo = "ER096", Recurso = "SinPlan", Criticidad = "Advertencia" },
            new { Codigo = "ER097", Recurso = "Sesion", Criticidad = "Advertencia" },
            new { Codigo = "ER098", Recurso = "Acceso", Criticidad = "Advertencia" },
            new { Codigo = "ER099", Recurso = "Verificacion", Criticidad = "Grave" },
            new { Codigo = "ER100", Recurso = "SinConfigurar", Criticidad = "Grave" },
            new { Codigo = "ER101", Recurso = "TextoLargo", Criticidad = "Advertencia" },
            new { Codigo = "ER102", Recurso = "Vacio", Criticidad = "Advertencia" },
            new { Codigo = "ER103", Recurso = "ImagenGrande", Criticidad = "Advertencia" },
            new { Codigo = "ER104", Recurso = "ImagenInvalida", Criticidad = "Advertencia" },
            new { Codigo = "ER105", Recurso = "Espera", Criticidad = "Advertencia" },
            new { Codigo = "ER106", Recurso = "LimiteGemini", Criticidad = "Advertencia" },
            new { Codigo = "ER107", Recurso = "NoDisponible", Criticidad = "Grave" },
            new { Codigo = "ER108", Recurso = "SinRespuesta", Criticidad = "Advertencia" },
            new { Codigo = "ER109", Recurso = "TiempoAgotado", Criticidad = "Grave" }
        };
        foreach (var mensaje in mensajes)
        {
            if (errores.Exists(error => error.Codigo == mensaje.Codigo)) continue;
            errores.Add(new VitaPet.BE.ErrorSistemaBE
            {
                Codigo = mensaje.Codigo,
                Descripcion = Convert.ToString(GetGlobalResourceObject("Textos", "ConsultaVirtual_Error_" + mensaje.Recurso)),
                Criticidad = mensaje.Criticidad
            });
        }
    }

    private void MostrarEstado(string mensaje)
    {
        lblEstado.Text = Server.HtmlEncode(TextosUI.Traducir(mensaje));
        lblEstado.Visible = true;
    }
}
