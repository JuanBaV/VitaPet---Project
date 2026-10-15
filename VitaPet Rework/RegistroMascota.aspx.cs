using System;
using System.Globalization;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;
public partial class RegistroMascotaPage : PaginaClientePlanes
{
    private string ClaveResultado {get{return "RegistroMascota:Confirmado:"+SessionManager.Instancia.UsuarioActual.IdUsuario;}}
    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);Form.Attributes["novalidate"]="novalidate";
        dtpFechaNacimiento.Attributes["max"]=DateTime.Today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    }
    protected override void EjecutarFuncionalidad()
    {
        lblConfirmacion.Visible=false;pnlMensaje.Visible=false;
        if(!IsPostBack&&Session[ClaveResultado] is RegistroMascotaBE)
        {
            var d=(RegistroMascotaBE)Session[ClaveResultado];Session.Remove(ClaveResultado);
            txtNombre.Text=d.Nombre;txtEspecie.Text=d.Especie;txtRaza.Text=d.Raza;
            dtpFechaNacimiento.Value=d.FechaNacimiento.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            pnlDatos.Enabled=false;dtpFechaNacimiento.Attributes["disabled"]="disabled";lblConfirmacion.Visible=true;
        }
    }
    protected void Registrar(object sender,EventArgs e)
    {
        EjecutarPlan(()=>{
            DateTime fecha;DateTime? nacimiento=null;
            if(!string.IsNullOrWhiteSpace(dtpFechaNacimiento.Value))
            {
                if(!DateTime.TryParseExact(dtpFechaNacimiento.Value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out fecha))
                    throw new RegistroMascotaException("ER025");
                nacimiento=fecha;
            }
            var d=new RegistroMascotaBE {Nombre=txtNombre.Text,Especie=txtEspecie.Text,Raza=txtRaza.Text,FechaNacimiento=nacimiento};
            new RegistroMascotaBLL().Registrar(d);Session[ClaveResultado]=RegistroMascotaBLL.Validar(d);
            // PRG: recargar la confirmación no repite el alta.
            Response.Redirect(ResolveUrl("~/RegistroMascota.aspx"),false);Context.ApplicationInstance.CompleteRequest();
        });
    }
}
