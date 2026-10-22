using System;
using System.Linq;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
public partial class AltaUsuarioPage : PaginaBase
{
    protected override string[] RolesPermitidos {get {return new[]{"Webmaster"};}}
    protected override void OnInit(EventArgs e) {ViewStateUserKey=Session.SessionID;base.OnInit(e);}
    protected override void EjecutarFuncionalidad()
    {
        if(!IsPostBack) Ejecutar(() => {ddlPerfil.Items.Clear();foreach(var p in new PerfilBLL().Consultar().OfType<PerfilBE>().OrderBy(p => p.Id))ddlPerfil.Items.Add(new ListItem(p.Nombre,p.Nombre));MostrarPermisos();});
    }
    private void MostrarPermisos()
    {
        var p=new PerfilBLL().Consultar().OfType<PerfilBE>().SingleOrDefault(f => f.Nombre==ddlPerfil.SelectedValue);
        listaPermisos.Items.Clear();if(p!=null)foreach(string pantalla in p.Pantallas().Select(PerfilBLL.NombrePantalla).OrderBy(x => x))listaPermisos.Items.Add(TextosUI.Traducir(pantalla));
    }
    protected void VerPermisos(object sender,EventArgs e) {Ejecutar(MostrarPermisos);}
    protected void Crear(object sender,EventArgs e)
    {
        Ejecutar(() => {Page.Validate("AltaUsuario");if(!Page.IsValid)return;
            int id=new AltaUsuarioBLL().Registrar(new RegistroClienteBE {Nombre=txtNombre.Text,Apellido=txtApellido.Text,CorreoElectronico=ucCorreo.Correo,Contrasena=txtContrasena.Text,Telefono=txtTelefono.Text},ddlPerfil.SelectedValue);
            txtNombre.Text=txtApellido.Text=txtContrasena.Text=txtTelefono.Text="";ucCorreo.Correo="";
            Mensaje("El usuario se creó correctamente. ID: "+id);
        });
        txtContrasena.Text="";
    }
    private void Mensaje(string texto) {pnlMensaje.Visible=true;litMensaje.Text=Server.HtmlEncode(TextosUI.Traducir(texto));}
    private void Ejecutar(Action accion)
    {
        try {accion();}
        catch(RegistroClienteException ex) {Mensaje(ex.Codigo+" - "+TextosUI.Error(ex.Codigo,"Revisá los datos ingresados, la contraseña y el correo."));}
        catch(ArgumentException ex) {Mensaje(ex.Message);}
        catch(UnauthorizedAccessException) {Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true);}
        catch(System.Data.SqlClient.SqlException) {Mensaje("No se pudo crear el usuario. Revisá la conexión y el correo ingresado.");}
        catch(InvalidOperationException) {Mensaje("No se pudo completar la operación. Verificá la integridad.");}
    }
}
