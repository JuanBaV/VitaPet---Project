using System;
using System.Linq;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
public partial class ConsultarPerfilesPage : PaginaBase
{
    protected override string[] RolesPermitidos { get {return new[]{"Administrador","Webmaster"};} }
    protected override void EjecutarFuncionalidad()
    {
        try {arbol.Nodes.Clear();foreach(var p in new PerfilBLL().Consultar().OfType<PerfilBE>())arbol.Nodes.Add(Nodo(p));arbol.ExpandAll();}
        catch(System.Data.SqlClient.SqlException) {pnlMensaje.Visible=true;litMensaje.Text=Server.HtmlEncode(TextosUI.Traducir("Fallo de conexión."));}
    }
    private TreeNode Nodo(PermisoComponenteBE p)
    {
        var hoja=p as PermisoPantallaBE;
        var n=new TreeNode(Server.HtmlEncode(TextosUI.Traducir(hoja==null ? p.Nombre : PerfilBLL.NombrePantalla(hoja.Pantalla)))) {SelectAction=TreeNodeSelectAction.None};
        var f=p as PerfilBE;if(f!=null)foreach(var h in f.Hijos)n.ChildNodes.Add(Nodo(h));return n;
    }
}
