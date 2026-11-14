using System;
using System.Net;
using System.Web;
using System.Web.Services.Protocols;
using System.Web.UI.WebControls;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.GUI.WebServices;
public partial class HistorialClinicoPage : PaginaMascota
{
 protected override void EjecutarFuncionalidad()
 {
  pnlMensaje.Visible=false;pnlDatos.Visible=false;
  EjecutarPlan(()=>{try{
   var r=new HistorialClinicoService().ObtenerHistorialClinico(IdMascota); //obtener historial clínico de la mascota mediante HistorialClinicoService
          if (!string.IsNullOrEmpty(r.CodigoError))throw new GestionMascotaException(r.CodigoError);
   if(r.Mascota==null||r.Historial==null)throw new GestionMascotaException("ER063");
   lblMascota.Text=Server.HtmlEncode(r.Mascota.Nombre+" · "+r.Mascota.Especie+" · "+r.Mascota.Raza);
   gvHistorialClinico.DataSource=r.Historial;gvHistorialClinico.DataBind();
   if(gvHistorialClinico.HeaderRow!=null)gvHistorialClinico.HeaderRow.TableSection=TableRowSection.TableHeader;
   pnlDatos.Visible=true;
  }catch(SoapException){throw new UnauthorizedAccessException("Acceso denegado al servicio clínico.");}
   catch(HttpException ex){throw new GestionMascotaException("ER063",ex);}
   catch(WebException ex){throw new GestionMascotaException("ER063",ex);}
  });
 }
}
