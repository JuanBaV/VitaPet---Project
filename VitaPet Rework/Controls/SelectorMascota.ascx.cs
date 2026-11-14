using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using VitaPet.BE;
public partial class Controls_SelectorMascota : System.Web.UI.UserControl
{
  //OBSERVER: permite seleccionar una mascota de un listado
  //y notifica a la pagina que lo contiene mediante el evento MascotaSeleccionada
 public event EventHandler MascotaSeleccionada; //evento que se dispara cuando se selecciona una mascota
 public int IdMascotaSeleccionada {get{return gvMascotas.SelectedDataKey==null?0:Convert.ToInt32(gvMascotas.SelectedDataKey.Value);} } //consultar ID de la mascota seleccionada
 public void Vincular(IEnumerable<MascotaBE> mascotas){gvMascotas.SelectedIndex=-1;gvMascotas.DataSource=mascotas;gvMascotas.DataBind(); } //notificar al control que se vinculen las mascotas al gridview
 public void LimpiarSeleccion(){gvMascotas.SelectedIndex=-1;} 
 protected void SeleccionarMascota(object sender,EventArgs e){var h=MascotaSeleccionada;if(h!=null)h(this,EventArgs.Empty);} 
 protected override void OnPreRender(EventArgs e){if(gvMascotas.HeaderRow!=null)gvMascotas.HeaderRow.TableSection=TableRowSection.TableHeader;base.OnPreRender(e);}
}
