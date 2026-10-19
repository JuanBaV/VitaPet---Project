using System;
using System.Globalization;
using System.Web.UI;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class RegistroClienteCorporativoPage:PaginaEmpresa
{
    private string Clave(string token){return "AltaCorporativo:"+SessionManager.Instancia.UsuarioActual.IdUsuario+":"+token;}
    private bool Ejecutar(Action action)
    {
        return EjecutarEmpresa(()=>{try{action();}catch(ClienteCorporativoException ex){throw new EmpresaException(ex.Codigo,ex);}});
    }
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;
        if(!IsPostBack)
        {
            Ejecutar(()=>{
                Guid token;string resultado=Request.QueryString["resultado"];
                if(Guid.TryParseExact(resultado,"N",out token)&&Session[Clave(resultado)] is AltaClienteCorporativoBE)
                {
                    var r=(AltaClienteCorporativoBE)Session[Clave(resultado)];Session.Remove(Clave(resultado));
                    pnlEmpresas.Visible=false;pnlFormulario.Visible=true;
                    lblPlanCorporativo.Text=Server.HtmlEncode(r.Plan);lblCondicionesPlan.Visible=false;lblEmpresa.Text=Server.HtmlEncode(r.Empresa);ucCorreo.Correo=r.Correo;ucCorreo.Visible=false;btnValidarCorreo.Visible=false;
                    pnlDatos.Visible=false;btnRegistrarCliente.Enabled=false;lblConfirmacion.Visible=true;
                    lblConfirmacion.Text=Server.HtmlEncode(TextosUI.Formato("Corporativo_Confirmacion",r.IdCliente,r.Correo,r.Empresa,r.Plan,r.Vencimiento.ToString("d",CultureInfo.CurrentCulture)));return;
                }
                if(Request.QueryString["id"]==null)
                {pnlEmpresas.Visible=true;pnlFormulario.Visible=false;gvEmpresas.DataSource=new ClienteCorporativoBLL().ListarEmpresas();gvEmpresas.DataBind();}
                else
                {
                    pnlEmpresas.Visible=false;pnlFormulario.Visible=false;
                    var p=new ClienteCorporativoBLL().SeleccionarEmpresa(IdEmpresa);
                    lblPlanCorporativo.Text=Server.HtmlEncode(p.Plan);lblCondicionesPlan.Text=Server.HtmlEncode(TextosUI.Formato("PlanAdmin_CondicionesCorporativo",p.Precio.ToString("0.00",CultureInfo.CurrentCulture)));
                    ViewState["EmpresaActual"]=IdEmpresa;lblEmpresa.Text=Server.HtmlEncode(p.Empresa.RazonSocial+" · "+p.Empresa.Cuit);
                    pnlFormulario.Visible=true;
                }
            });
        }
        else if(pnlFormulario.Visible&&!string.Equals(ucCorreo.Correo,ViewState["CorreoValidado"] as string,StringComparison.Ordinal))
        {ViewState.Remove("CorreoValidado");pnlDatos.Enabled=false;btnRegistrarCliente.Enabled=false;}
    }
    protected void Seleccionar(object sender,EventArgs e)
    {
        Ejecutar(()=>{
            int id=gvEmpresas.SelectedDataKey==null?0:(int)gvEmpresas.SelectedDataKey.Value;
            new ClienteCorporativoBLL().SeleccionarEmpresa(id);
            Response.Redirect(ResolveUrl("~/RegistroClienteCorporativo.aspx")+"?id="+id.ToString(CultureInfo.InvariantCulture),false);Context.ApplicationInstance.CompleteRequest();
        });
    }
    private void EmpresaActual()
    {
        if(!(ViewState["EmpresaActual"] is int)||(int)ViewState["EmpresaActual"]!=IdEmpresa)throw new ClienteCorporativoException("ER011");
    }
    protected void ValidarCorreo(object sender,EventArgs e)
    {
        ViewState.Remove("CorreoValidado");pnlDatos.Enabled=false;btnRegistrarCliente.Enabled=false;
        Ejecutar(()=>{EmpresaActual();ViewState["CorreoValidado"]=new ClienteCorporativoBLL().ValidarCorreo(IdEmpresa,ucCorreo.Correo);pnlDatos.Enabled=true;btnRegistrarCliente.Enabled=true;});
    }
    protected void Registrar(object sender,EventArgs e)
    {
        Ejecutar(()=>{
            EmpresaActual();
            if(string.IsNullOrEmpty(ViewState["CorreoValidado"] as string)||!string.Equals(ucCorreo.Correo,ViewState["CorreoValidado"] as string,StringComparison.Ordinal))throw new ClienteCorporativoException("ER025");
            var r=new ClienteCorporativoBLL().Registrar(new DatosClienteCorporativoBE{IdEmpresa=IdEmpresa,Correo=ucCorreo.Correo,Nombre=txtNombre.Text,Apellido=txtApellido.Text,Telefono=txtTelefono.Text});
            string token=Guid.NewGuid().ToString("N");Session[Clave(token)]=r;
            Response.Redirect(ResolveUrl("~/RegistroClienteCorporativo.aspx")+"?resultado="+token,false);Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void OnPreRender(EventArgs e)
    {
        pnlFormulario.Attributes["data-correo"]=ViewState["CorreoValidado"] as string??string.Empty;
        pnlFormulario.Attributes["data-habilitado"]=btnRegistrarCliente.Enabled?"1":"0";
        base.OnPreRender(e);
    }
    protected override void AlFallarEmpresa(string code)
    {
        if(code!="ER002"&&code!="ER025"&&code!="ER001")
        {pnlDatos.Enabled=false;btnRegistrarCliente.Enabled=false;btnValidarCorreo.Enabled=false;ViewState.Remove("CorreoValidado");}
    }
}
