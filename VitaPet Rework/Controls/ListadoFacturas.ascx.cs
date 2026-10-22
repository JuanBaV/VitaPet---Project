using System.Collections.Generic;
using System.Linq;
using VitaPet.BE;
public partial class Controls_ListadoFacturas : System.Web.UI.UserControl
{
    public void Vincular(IEnumerable<ConsultaFacturaBE> facturas)
    {
        var filas=facturas.ToList();pnlSinFacturas.Visible=filas.Count==0;
        rptFacturas.DataSource=filas;rptFacturas.DataBind();
    }
}
