using VitaPet.BLL;
namespace VitaPet.GUI.Presentacion
{
 public abstract class PaginaMascota : PaginaClientePlanes
 {
  protected int IdMascota {get{int id;if(!int.TryParse(Request.QueryString["id"],out id)||id<=0)throw new GestionMascotaException("ER011");return id;}}
 }
}
