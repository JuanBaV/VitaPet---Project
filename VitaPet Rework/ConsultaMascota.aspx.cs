using System.Globalization;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
public partial class ConsultaMascotaPage : PaginaMascota
{
 protected override void EjecutarFuncionalidad()
 {
  pnlMensaje.Visible=false;pnlDatos.Visible=false;
  EjecutarPlan(()=>{var m=new MascotaBLL().Consultar(IdMascota);
   lblNombre.Text=Server.HtmlEncode(m.Nombre);lblEspecie.Text=Server.HtmlEncode(m.Especie);lblRaza.Text=Server.HtmlEncode(m.Raza);
   lblFechaNacimiento.Text=m.FechaNacimiento.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture);
   lblEstado.Text=TextosUI.Obtener(m.Estado?"Mascota_Activa":"Mascota_Inactiva");pnlDatos.Visible=true;});
 }
}
