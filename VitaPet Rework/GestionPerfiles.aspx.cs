using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class GestionPerfilesPage : PaginaBase
{
    protected override string[] RolesPermitidos { get { return new[] {"Webmaster","Administrador"}; } }
    protected bool Edita { get { return AppRelativeVirtualPath.EndsWith("/GestionPerfiles.aspx",StringComparison.OrdinalIgnoreCase) && SessionManager.Instancia.UsuarioActual.Rol=="Webmaster"; } }
    private int Id { get { return (int?)ViewState["PerfilId"] ?? 0; } set { ViewState["PerfilId"]=value; } }
    private string Version { get { return ViewState["Version"] as string; } set { ViewState["Version"]=value; } }
    private int IdEliminar { get { return (int?)ViewState["EliminarId"] ?? 0; } set { ViewState["EliminarId"]=value; } }
    private string VersionEliminar { get { return ViewState["EliminarVersion"] as string; } set { ViewState["EliminarVersion"]=value; } }
    protected override void OnInit(EventArgs e) { ViewStateUserKey=Session.SessionID; base.OnInit(e); }
    protected override void EjecutarFuncionalidad()
    {
        pnlEdicion.Visible=Edita;
        btnNuevo.Visible=Edita;
        if(!IsPostBack) Ejecutar(() => Cargar());
    }
    private void Cargar()
    {
        var lista=new PerfilBLL().Consultar();
        gvPerfiles.DataSource=lista.OfType<PerfilBE>().OrderBy(p => p.Id).Select(p => new {p.Id,Nombre=p.Sistema?TextosUI.Traducir(p.Nombre):p.Nombre,p.Sistema,Permisos=string.Join(", ",p.Pantallas().Select(PerfilBLL.NombrePantalla).Select(TextosUI.Traducir).OrderBy(x => x))}).ToList(); gvPerfiles.DataBind();
        chkComponentes.Items.Clear();
        foreach(var c in lista.OrderBy(c => c is PerfilBE ? 0 : 1).ThenBy(c => c.Nombre))
            if(c.Id!=Id) { var hoja=c as PermisoPantallaBE;chkComponentes.Items.Add(new ListItem(TextosUI.Traducir(hoja==null ? c.Nombre : PerfilBLL.NombrePantalla(hoja.Pantalla)),c.Id.ToString())); }
    }
    protected void Seleccionar(object sender,EventArgs e)
    {
        Ejecutar(() => {
            CerrarConfirmacion();Id=(int)gvPerfiles.SelectedDataKey.Value; var p=new PerfilBLL().Consultar().OfType<PerfilBE>().Single(f => f.Id==Id);Version=p.Version;
            Cargar();txtNombre.Text=p.Nombre; txtNombre.ReadOnly=p.Sistema;
            foreach(ListItem item in chkComponentes.Items) item.Selected=p.Hijos.Any(h => h.Id.ToString()==item.Value);
            litPerfil.Text=Server.HtmlEncode(p.Sistema?TextosUI.Traducir(p.Nombre):p.Nombre); arbol.Nodes.Clear(); arbol.Nodes.Add(Nodo(p));arbol.ExpandAll();pnlDetalle.Visible=true;
        });
    }
    private TreeNode Nodo(PermisoComponenteBE p)
    {
        var hoja=p as PermisoPantallaBE;
        var n=new TreeNode(Server.HtmlEncode(TextosUI.Traducir(hoja==null ? p.Nombre : PerfilBLL.NombrePantalla(hoja.Pantalla)))) {SelectAction=TreeNodeSelectAction.None};
        var f=p as PerfilBE;if(f!=null)foreach(var h in f.Hijos)n.ChildNodes.Add(Nodo(h));return n;
    }
    protected void Nuevo(object sender,EventArgs e) { Ejecutar(() => {ExigirEdicion();CerrarConfirmacion();Id=0;Version=null;txtNombre.Text="";txtNombre.ReadOnly=false;pnlDetalle.Visible=false;Cargar();}); }
    protected void Guardar(object sender,EventArgs e)
    {
        Ejecutar(() => {ExigirEdicion();if(!ValidacionAlta.Validar(this,"Perfil"))return;CerrarConfirmacion();new PerfilBLL().Guardar(Id,Version,txtNombre.Text,chkComponentes.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)));Id=0;Version=null;txtNombre.Text="";txtNombre.ReadOnly=false;pnlDetalle.Visible=false;Cargar();Mensaje("El perfil se guardó correctamente.");});
    }
    protected void ValidarComponentes(object sender,ServerValidateEventArgs e)
    {e.IsValid=chkComponentes.Items.Cast<ListItem>().Any(i=>i.Selected);}
    protected void Eliminar(object sender,CommandEventArgs e)
    {
        Ejecutar(() => {
            ExigirEdicion();CerrarConfirmacion();pnlMensaje.Visible=false;
            int id;if(!int.TryParse(Convert.ToString(e.CommandArgument),out id))throw new ArgumentException("Seleccioná un perfil.");
            var p=new PerfilBLL().Consultar().OfType<PerfilBE>().SingleOrDefault(f => f.Id==id);
            if(p==null)throw new ArgumentException("Seleccioná un perfil.");
            if(p.Sistema)throw new ArgumentException("Los tres perfiles iniciales deben conservarse.");
            IdEliminar=p.Id;VersionEliminar=p.Version;
            litConfirmar.Text=Server.HtmlEncode(TextosUI.Formato("Perfil_ConfirmarFormato",p.Nombre));pnlConfirmar.Visible=true;pnlConfirmar.Focus();
        });
    }
    private void CerrarConfirmacion() {pnlConfirmar.Visible=false;ViewState.Remove("EliminarId");ViewState.Remove("EliminarVersion");}
    protected void CancelarEliminacion(object sender,EventArgs e) {CerrarConfirmacion();}
    protected void ConfirmarEliminacion(object sender,EventArgs e)
    {
        Ejecutar(() => {ExigirEdicion();if(!pnlConfirmar.Visible || IdEliminar==0)return;new PerfilBLL().Eliminar(IdEliminar,VersionEliminar);CerrarConfirmacion();Id=0;Version=null;txtNombre.Text="";txtNombre.ReadOnly=false;pnlDetalle.Visible=false;Cargar();Mensaje("El perfil se eliminó correctamente.");});
    }
    private void ExigirEdicion() { if(!Edita)throw new UnauthorizedAccessException("Solo el Webmaster puede modificar perfiles."); }
    private void Mensaje(string m) { pnlMensaje.Visible=true;litMensaje.Text=Server.HtmlEncode(TextosUI.Traducir(m)); }
    private void Ejecutar(Action accion)
    {
        try {accion();}
        catch(UnauthorizedAccessException) {Response.Redirect(ResolveUrl("~/AccesoDenegado.aspx"),true);}
        catch(ArgumentException ex) {Mensaje(ex.Message);}
        catch(System.Data.SqlClient.SqlException) {Mensaje("No se pudo completar la operación. Revisá la conexión e intentá nuevamente.");}
        catch(InvalidOperationException) {Mensaje("No se pudo completar la operación. Verificá la integridad o recargá los perfiles.");}
    }
}
