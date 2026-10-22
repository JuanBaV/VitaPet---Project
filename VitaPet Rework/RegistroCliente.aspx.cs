using System;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class RegistroClientePage : PaginaConIdioma
{
    private const string ClaveResultado = "RegistroCliente:Confirmado";

    protected override void OnInit(EventArgs e)
    {
        // Una sesión anónima vacía puede recibir otro SessionID en el POST.
        // Persistir una clave vincula el ViewState al mismo navegador sin
        // autenticar al cliente ni deshabilitar la validación MAC.
        const string claveEstado = "RegistroCliente:ClaveEstado";
        if (Session[claveEstado] == null) Session[claveEstado] = Guid.NewGuid().ToString("N");
        ViewStateUserKey = (string)Session[claveEstado];
        base.OnInit(e);
        // El servidor centraliza ER002/ER025 sin depender de JavaScript o HTML5.
        Form.Attributes["novalidate"] = "novalidate";
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        if (!IsPostBack && Session[ClaveResultado] != null)
        {
            Session.Remove(ClaveResultado);
            pnlDatos.Visible = false;
            pnlMensaje.Visible = true;
            pnlMensaje.CssClass = "login-message registro-confirmacion";
            litMensaje.Text = Server.HtmlEncode(VitaPet.GUI.Resources.Textos.Registro_CuentaCreada);
        }
    }

    protected void btnRegistrar_Click(object sender, EventArgs e)
    {
        try
        {
            if (!ValidacionAlta.Validar(this, "RegistroCliente")) return;
            new ClienteBLL().RegistrarCliente(Datos());
            // PRG: recargar el resultado no vuelve a enviar el alta. No autentica.
            Session[ClaveResultado] = true;
            Response.Redirect(ResolveUrl("~/RegistroCliente.aspx"), false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (RegistroClienteException ex)
        {
            pnlMensaje.Visible = true;
            pnlMensaje.CssClass = "login-message registro-error";
            litMensaje.Text = Server.HtmlEncode(ex.Codigo + " - " + TextosUI.Error(ex.Codigo, Mensaje(ex.Codigo)));
        }
        finally { txtContrasena.Text = string.Empty; }
    }

    private RegistroClienteBE Datos()
    {
        return new RegistroClienteBE { Nombre=txtNombre.Text, Apellido=txtApellido.Text,
            CorreoElectronico=ucCorreo.Correo, Contrasena=txtContrasena.Text, Telefono=txtTelefono.Text };
    }
    protected void ValidarDatos(object sender, ServerValidateEventArgs e)
    {
        ValidacionAlta.Comprobar((CustomValidator)sender, e, () => ClienteBLL.ValidarDatosRegistro(Datos()));
    }
    private static string Mensaje(string codigo)
    {
        switch (codigo)
        {
            case "ER001": return "Correo ya registrado";
            case "ER002": return "Datos incompletos";
            case "ER003": return "Fallo de conexión";
            case "ER023": return "Inconsistencia en la verificación de integridad";
            case "ER025": return "Datos inválidos";
            case "ER040": return "No se pudo registrar la operación en bitácora";
            default: return "No se pudo registrar el cliente";
        }
    }
}
