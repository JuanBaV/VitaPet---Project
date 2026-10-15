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
            rptErrores.DataSource = errores;
            rptErrores.DataBind();
            if (errores.Count == 0) MostrarEstado("El catálogo de errores está vacío.");
        }
        catch (SqlException) { MostrarEstado("ER003 - Fallo de conexión. No se pudo cargar el catálogo."); }
        catch (ConfigurationErrorsException) { MostrarEstado("ER003 - Fallo de conexión. No se pudo cargar el catálogo."); }
    }

    private void MostrarEstado(string mensaje)
    {
        lblEstado.Text = Server.HtmlEncode(TextosUI.Traducir(mensaje));
        lblEstado.Visible = true;
    }
}
