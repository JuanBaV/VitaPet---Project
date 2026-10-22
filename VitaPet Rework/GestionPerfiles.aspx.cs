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
    private bool Edita { get { return AppRelativeVirtualPath.EndsWith("/GestionPerfiles.aspx",StringComparison.OrdinalIgnoreCase) && SessionManager.Instancia.UsuarioActual.Rol=="Webmaster"; } }
    private int Id { get { return (int?)ViewState["PerfilId"] ?? 0; } set { ViewState["PerfilId"]=value; } }
    private string Version { get { return ViewState["Version"] as string; } set { ViewState["Version"]=value; } }
    protected override void OnInit(EventArgs e) { ViewStateUserKey=Session.SessionID; base.OnInit(e); }
    protected override void EjecutarFuncionalidad()
    {
        pnlEdicion.Visible=Edita;
        if(!IsPostBack) Ejecutar(() => Cargar());
    }
    private void Cargar()
    {
        var lista=new PerfilBLL().Consultar();
        gvPerfiles.DataSource=lista.OfType<PerfilBE>().OrderBy(p => p.Id).Select(p => new {p.Id,p.Nombre,Permisos=string.Join(", ",p.Pantallas().Select(PerfilBLL.NombrePantalla).OrderBy(x => x))}).ToList(); gvPerfiles.DataBind();
        chkComponentes.Items.Clear();
        foreach(var c in lista.OrderBy(c => c is PerfilBE ? 0 : 1).ThenBy(c => c.Nombre))
            if(c.Id!=Id) { var hoja=c as PermisoPantallaBE;chkComponentes.Items.Add(new ListItem(TextosUI.Traducir(hoja==null ? c.Nombre : PerfilBLL.NombrePantalla(hoja.Pantalla)),c.Id.ToString())); }
        btnEliminar.Enabled=Id!=0;
    }
    protected void Seleccionar(object sender,EventArgs e)
    {
        Ejecutar(() => {
            pnlConfirmar.Visible=false;Id=(int)gvPerfiles.SelectedDataKey.Value; var p=new PerfilBLL().Consultar().OfType<PerfilBE>().Single(f => f.Id==Id);Version=p.Version;
            Cargar();txtNombre.Text=p.Nombre; txtNombre.ReadOnly=p.Sistema;
            foreach(ListItem item in chkComponentes.Items) item.Selected=p.Hijos.Any(h => h.Id.ToString()==item.Value);
            litPerfil.Text=Server.HtmlEncode(p.Nombre); arbol.Nodes.Clear(); arbol.Nodes.Add(Nodo(p));arbol.ExpandAll();pnlDetalle.Visible=true;btnEliminar.Enabled=!p.Sistema;
        });
    }
    private TreeNode Nodo(PermisoComponenteBE p)
    {
        var hoja=p as PermisoPantallaBE;
        var n=new TreeNode(Server.HtmlEncode(TextosUI.Traducir(hoja==null ? p.Nombre : PerfilBLL.NombrePantalla(hoja.Pantalla)))) {SelectAction=TreeNodeSelectAction.None};
        var f=p as PerfilBE;if(f!=null)foreach(var h in f.Hijos)n.ChildNodes.Add(Nodo(h));return n;
    }
    protected void Nuevo(object sender,EventArgs e) { Ejecutar(() => {ExigirEdicion();pnlConfirmar.Visible=false;Id=0;Version=null;txtNombre.Text="";txtNombre.ReadOnly=false;pnlDetalle.Visible=false;Cargar();}); }
    protected void Guardar(object sender,EventArgs e)
    {
        Ejecutar(() => {ExigirEdicion();new PerfilBLL().Guardar(Id,Version,txtNombre.Text,chkComponentes.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)));Id=0;Version=null;txtNombre.Text="";txtNombre.ReadOnly=false;pnlDetalle.Visible=false;Cargar();Mensaje("El perfil se guardó correctamente.");});
    }
    protected void Eliminar(object sender,EventArgs e)
    {
        Ejecutar(() => {ExigirEdicion();var p=new PerfilBLL().Consultar().OfType<PerfilBE>().SingleOrDefault(f => f.Id==Id);if(p==null)throw new ArgumentException("Seleccioná un perfil.");litConfirmar.Text=Server.HtmlEncode("¿Confirmás la eliminación del perfil "+p.Nombre+"?");pnlConfirmar.Visible=true;});
    }
    protected void CancelarEliminacion(object sender,EventArgs e) {pnlConfirmar.Visible=false;}
    protected void ConfirmarEliminacion(object sender,EventArgs e)
    {
        Ejecutar(() => {ExigirEdicion();if(!pnlConfirmar.Visible)return;new PerfilBLL().Eliminar(Id,Version);pnlConfirmar.Visible=false;Id=0;Version=null;txtNombre.Text="";txtNombre.ReadOnly=false;pnlDetalle.Visible=false;Cargar();Mensaje("El perfil se eliminó correctamente.");});
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
