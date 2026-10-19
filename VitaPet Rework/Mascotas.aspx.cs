using System;
using System.Globalization;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
public partial class MascotasPage : PaginaClientePlanes
{
 protected override void EjecutarFuncionalidad(){pnlMensaje.Visible=false;if(!IsPostBack)EjecutarPlan(()=>ucSelectorMascota.Vincular(new MascotaBLL().Listar()));}
 private void Habilitar(bool valor){btnActualizarDatos.Enabled=btnConsultarDatos.Enabled=btnHistorialClinico.Enabled=btnDatosClinicos.Enabled=valor;}
 protected void Seleccionar(object sender,EventArgs e){Habilitar(false);EjecutarPlan(()=>{new MascotaBLL().Seleccionar(ucSelectorMascota.IdMascotaSeleccionada);Habilitar(true);});}
 private void Abrir(string pagina)
 {
  EjecutarPlan(()=>{var mascota=new MascotaBLL().Seleccionar(ucSelectorMascota.IdMascotaSeleccionada);
   Response.Redirect(ResolveUrl("~/"+pagina)+"?id="+mascota.IdMascota.ToString(CultureInfo.InvariantCulture),false);Context.ApplicationInstance.CompleteRequest();});
 }
 protected void Actualizar(object sender,EventArgs e){Abrir("ActualizarMascota.aspx");}
 protected void Consultar(object sender,EventArgs e){Abrir("ConsultaMascota.aspx");}
 protected void Historial(object sender,EventArgs e){Abrir("HistorialClinico.aspx");}
 protected void Clinicos(object sender,EventArgs e){Abrir("DatosClinicos.aspx");}
 protected override void OnPreRender(EventArgs e){if(pnlMensaje.Visible){ucSelectorMascota.LimpiarSeleccion();Habilitar(false);}base.OnPreRender(e);}
}
