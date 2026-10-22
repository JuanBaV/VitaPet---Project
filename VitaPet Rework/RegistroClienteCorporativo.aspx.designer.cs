public partial class RegistroClienteCorporativoPage
{
    protected global::System.Web.UI.WebControls.Panel pnlMensaje;
    protected global::System.Web.UI.WebControls.Literal litMensaje;
    protected global::System.Web.UI.WebControls.Panel pnlEmpresas;
    protected global::System.Web.UI.WebControls.GridView gvEmpresas;
    protected global::System.Web.UI.WebControls.Panel pnlFormulario;
    protected global::System.Web.UI.WebControls.Label lblEmpresa;
    protected global::System.Web.UI.WebControls.Label lblPlanCorporativo;
    protected global::System.Web.UI.WebControls.Label lblCondicionesPlan;
    protected global::System.Web.UI.WebControls.Button btnValidarCorreo;
    protected global::System.Web.UI.WebControls.Panel pnlDatos;
    protected global::System.Web.UI.WebControls.TextBox txtNombre;
    protected global::System.Web.UI.WebControls.TextBox txtApellido;
    protected global::System.Web.UI.WebControls.TextBox txtTelefono;
    protected global::System.Web.UI.WebControls.Button btnRegistrarCliente;
    protected global::System.Web.UI.WebControls.Label lblConfirmacion;
    protected global::Controls_ValidacionEmail ucCorreo;
    protected global::System.Web.UI.WebControls.ValidationSummary vsCorreoCorporativo;
    protected global::System.Web.UI.WebControls.ValidationSummary vsAltaCorporativo;
    protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvNombre;
    protected global::System.Web.UI.WebControls.RegularExpressionValidator revNombre;
    protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvApellido;
    protected global::System.Web.UI.WebControls.RegularExpressionValidator revApellido;
    protected global::System.Web.UI.WebControls.RequiredFieldValidator rfvTelefono;
    protected global::System.Web.UI.WebControls.RegularExpressionValidator revTelefono;
    protected global::System.Web.UI.WebControls.CustomValidator cvDatosCorporativo;
}
