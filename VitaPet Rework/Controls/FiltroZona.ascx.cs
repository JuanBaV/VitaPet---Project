using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using VitaPet.GUI.Presentacion;
public partial class Controls_FiltroZona : System.Web.UI.UserControl
{
    public event EventHandler ZonaCambiada;
    public string ZonaSeleccionada {get{return ddlZona.SelectedValue;}}
    public bool Habilitado {get{return ddlZona.Enabled;}set{ddlZona.Enabled=value;}}
    public void Vincular(IEnumerable<string> zonas)
    {
        ddlZona.Items.Clear();ddlZona.Items.Add(new ListItem(TextosUI.Obtener("Turno_SeleccionarZona"),""));
        foreach(string zona in zonas)ddlZona.Items.Add(new ListItem(zona,zona));
    }
    public void LimpiarSeleccion(){ddlZona.SelectedIndex=0;}
    protected void CambiarZona(object sender,EventArgs e){var h=ZonaCambiada;if(h!=null)h(this,EventArgs.Empty);}
}
