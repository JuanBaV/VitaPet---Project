using System;
using System.Globalization;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;
public partial class ActualizarMascotaPage : PaginaMascota
{
 private string Version {get{return ViewState["VersionMascota"] as string;}set{ViewState["VersionMascota"]=value;}}
 private string Resultado(string token){return "ActualizarMascota:"+SessionManager.Instancia.UsuarioActual.IdUsuario+":"+token;}
 protected override void OnInit(EventArgs e){base.OnInit(e);Form.Attributes["novalidate"]="novalidate";dtpFechaNacimiento.Attributes["max"]=DateTime.Today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
 protected override void EjecutarFuncionalidad()
 {
  pnlMensaje.Visible=false;lblConfirmacion.Visible=false;
  if(!IsPostBack){pnlDatos.Visible=false;EjecutarPlan(()=>{
   var mascota=new MascotaBLL().Seleccionar(IdMascota);Version=mascota.DVH;pnlDatos.Visible=true;
   txtNombre.Text=mascota.Nombre;txtEspecie.Text=mascota.Especie;txtRaza.Text=mascota.Raza;dtpFechaNacimiento.Value=mascota.FechaNacimiento.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
   string token=Request.QueryString["resultado"];Guid guid;
   if(Guid.TryParseExact(token,"N",out guid)&&Session[Resultado(token)] is int&&(int)Session[Resultado(token)]==IdMascota)
   {Session.Remove(Resultado(token));lblConfirmacion.Visible=true;pnlDatos.Enabled=false;dtpFechaNacimiento.Enabled=false;}
  });}
 }
 protected void Guardar(object sender,EventArgs e)
 {
  EjecutarPlan(()=>{
   if(!pnlDatos.Enabled||string.IsNullOrEmpty(Version))throw new GestionMascotaException("ER064");
   DateTime fecha;DateTime? nacimiento=null;
   if(!string.IsNullOrWhiteSpace(dtpFechaNacimiento.Value))
   {if(!DateTime.TryParseExact(dtpFechaNacimiento.Value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out fecha))throw new GestionMascotaException("ER025");nacimiento=fecha;}
   new MascotaBLL().Actualizar(IdMascota,Version,new RegistroMascotaBE {Nombre=txtNombre.Text,Especie=txtEspecie.Text,Raza=txtRaza.Text,FechaNacimiento=nacimiento});
   string token=Guid.NewGuid().ToString("N");Session[Resultado(token)]=IdMascota;
   Response.Redirect(ResolveUrl("~/ActualizarMascota.aspx")+"?id="+IdMascota.ToString(CultureInfo.InvariantCulture)+"&resultado="+token,false);Context.ApplicationInstance.CompleteRequest();
  });
 }
}
