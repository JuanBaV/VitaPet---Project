using System;
using System.Net;
using System.Web;
using System.Web.Services.Protocols;
using System.Web.UI.WebControls;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.GUI.WebServices;
public partial class DatosClinicosPage : PaginaMascota
{
 protected override void EjecutarFuncionalidad()
 {
  pnlMensaje.Visible=false;pnlDatos.Visible=false;
  EjecutarPlan(()=>{try{
   var r=new HistorialClinicoService().ObtenerDatosClinicos(IdMascota);
   if(!string.IsNullOrEmpty(r.CodigoError))throw new GestionMascotaException(r.CodigoError);
   if(r.Mascota==null||r.Datos==null)throw new GestionMascotaException("ER063");
   lblMascota.Text=Server.HtmlEncode(r.Mascota.Nombre+" · "+r.Mascota.Especie+" · "+r.Mascota.Raza);
   gvDatosClinicos.DataSource=r.Datos;gvDatosClinicos.DataBind();
   if(gvDatosClinicos.HeaderRow!=null)gvDatosClinicos.HeaderRow.TableSection=TableRowSection.TableHeader;
   pnlDatos.Visible=true;
  }catch(SoapException){throw new UnauthorizedAccessException("Acceso denegado al servicio clínico.");}
   catch(HttpException ex){throw new GestionMascotaException("ER063",ex);}
   catch(WebException ex){throw new GestionMascotaException("ER063",ex);}
  });
 }
}
