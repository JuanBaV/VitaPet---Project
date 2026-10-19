using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using VitaPet.BE;
public partial class Controls_SelectorMascota : System.Web.UI.UserControl
{
 public event EventHandler MascotaSeleccionada;
 public int IdMascotaSeleccionada {get{return gvMascotas.SelectedDataKey==null?0:Convert.ToInt32(gvMascotas.SelectedDataKey.Value);}}
 public void Vincular(IEnumerable<MascotaBE> mascotas){gvMascotas.SelectedIndex=-1;gvMascotas.DataSource=mascotas;gvMascotas.DataBind();}
 public void LimpiarSeleccion(){gvMascotas.SelectedIndex=-1;}
 protected void SeleccionarMascota(object sender,EventArgs e){var h=MascotaSeleccionada;if(h!=null)h(this,EventArgs.Empty);}
 protected override void OnPreRender(EventArgs e){if(gvMascotas.HeaderRow!=null)gvMascotas.HeaderRow.TableSection=TableRowSection.TableHeader;base.OnPreRender(e);}
}
