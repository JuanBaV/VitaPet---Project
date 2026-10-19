using System;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;
using VitaPet.SECURITY;

public partial class AltaEmpresaPage : PaginaEmpresa
{
    private string Clave(string token) { return "AltaEmpresa:"+SessionManager.Instancia.UsuarioActual.IdUsuario+":"+token; }
    protected override void EjecutarFuncionalidad()
    {
        pnlMensaje.Visible=false;lblConfirmacion.Visible=false;
        if (!IsPostBack) EjecutarEmpresa(()=>{
            Guid guid;string token=Request.QueryString["resultado"];
            if (Guid.TryParseExact(token,"N",out guid))
            {
                var empresa=Session[Clave(token)] as EmpresaBE;
                if (empresa!=null)
                {
                    Session.Remove(Clave(token));
                    txtRazonSocial.Text=empresa.RazonSocial;txtCuit.Text=empresa.Cuit;
                    txtDireccion.Text=empresa.Direccion;txtTelefono.Text=empresa.Telefono;
                    pnlDatos.Enabled=false;lblConfirmacion.Visible=true;
                    lblConfirmacion.Text=Server.HtmlEncode(TextosUI.Formato("Empresa_ConfirmacionAlta",empresa.IdEmpresa,empresa.RazonSocial));
                }
            }
        });
    }
    protected void Registrar(object sender,EventArgs e)
    {
        EjecutarEmpresa(()=>{
            if (!pnlDatos.Enabled) throw new EmpresaException("ER068");
            var empresa=new EmpresaBLL().Registrar(new DatosEmpresaBE { RazonSocial=txtRazonSocial.Text,
                Cuit=txtCuit.Text, Direccion=txtDireccion.Text, Telefono=txtTelefono.Text });
            string token=Guid.NewGuid().ToString("N");Session[Clave(token)]=empresa;
            Response.Redirect(ResolveUrl("~/AltaEmpresa.aspx")+"?resultado="+token,false);Context.ApplicationInstance.CompleteRequest();
        });
    }
    protected override void AlFallarEmpresa(string codigo)
    { if (codigo!="ER002"&&codigo!="ER025"&&codigo!="ER068") pnlDatos.Enabled=false; }
}
