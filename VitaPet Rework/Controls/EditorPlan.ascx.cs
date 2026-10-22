using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.GUI.Presentacion;

public partial class Controls_EditorPlan:UserControl
{
    public event EventHandler<ErrorPlanEventArgs> ErrorPlan;
    public string ValidationGroup
    { get { return (string)ViewState["ValidationGroup"] ?? string.Empty; } set { ViewState["ValidationGroup"]=value; } }
    protected override void OnLoad(EventArgs e)
    { base.OnLoad(e); ConfigurarValidaciones(this); }
    private void ConfigurarValidaciones(Control raiz)
    {
        foreach(Control control in raiz.Controls)
        {
            var validador=control as BaseValidator;
            if(validador!=null)validador.ValidationGroup=ValidationGroup;
            var resumen=control as ValidationSummary;
            if(resumen!=null)resumen.ValidationGroup=ValidationGroup;
            ConfigurarValidaciones(control);
        }
    }
    protected void ValidarDatos(object sender, ServerValidateEventArgs e)
    { ValidacionAlta.Comprobar((CustomValidator)sender, e, () => PlanBLL.Validar(Datos())); }
    private static DataTable Tabla()
    {var t=new DataTable();t.Columns.Add("idCobertura",typeof(int));t.Columns.Add("prestacion");t.Columns.Add("detalle");t.Columns.Add("estado");return t;}
    private void Vincular(DataTable t){gvCoberturas.DataSource=t;gvCoberturas.DataBind();ConfigurarValidaciones(this);}
    public void Cargar(PlanAdministracionBE plan=null)
    {
        txtNombrePlan.Text=plan==null?"":plan.Nombre;txtDescripcionPlan.Text=plan==null?"":plan.Descripcion;
        txtPrecioPlan.Text=plan==null||!plan.Precio.HasValue?"":plan.Precio.Value.ToString("0.00",CultureInfo.CurrentCulture);
        ddlEstadoPlan.SelectedValue=plan==null||plan.Estado?"1":"0";var t=Tabla();
        if(plan!=null)foreach(var c in plan.Coberturas)t.Rows.Add(c.IdCobertura,c.Prestacion,c.Detalle,c.Estado==true?"1":"0");
        if(t.Rows.Count==0)t.Rows.Add(0,"","","1");Vincular(t);
    }
    private DataTable Recoger()
    {
        var t=Tabla();foreach(GridViewRow row in gvCoberturas.Rows)t.Rows.Add((int)gvCoberturas.DataKeys[row.RowIndex].Value,
            ((TextBox)row.FindControl("txtPrestacion")).Text,((TextBox)row.FindControl("txtDetalle")).Text,((DropDownList)row.FindControl("ddlEstadoCobertura")).SelectedValue);
        return t;
    }
    private static bool Estado(string value){if(value!="1"&&value!="0")throw new PlanException("ER025");return value=="1";}
    public DatosPlanBE Datos()
    {
        string text=txtPrecioPlan.Text.Trim();decimal precio;
        if(text.Length==0)throw new PlanException("ER002");
        if(!Regex.IsMatch(text,@"\A[0-9]+(?:[.,][0-9]{1,2})?\z")||!decimal.TryParse(text.Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out precio))throw new PlanException("ER025");
        var d=new DatosPlanBE {Nombre=txtNombrePlan.Text,Descripcion=txtDescripcionPlan.Text,Precio=precio,Estado=Estado(ddlEstadoPlan.SelectedValue)};
        foreach(DataRow r in Recoger().Rows)d.Coberturas.Add(new DatosCoberturaPlanBE {IdCobertura=(int)r["idCobertura"],Prestacion=(string)r["prestacion"],Detalle=(string)r["detalle"],Estado=Estado((string)r["estado"])});
        return d;
    }
    protected void Agregar(object sender,EventArgs e)
    {var t=Recoger();t.Rows.Add(0,"","","1");Vincular(t);}
    protected void FilaComando(object sender,GridViewCommandEventArgs e)
    {
        if(e.CommandName!="QuitarNueva")return;
        int i;var t=Recoger();
        if(!int.TryParse(Convert.ToString(e.CommandArgument,CultureInfo.InvariantCulture),out i)||i<0||i>=t.Rows.Count||(int)t.Rows[i]["idCobertura"]!=0)
        {if(ErrorPlan!=null)ErrorPlan(this,new ErrorPlanEventArgs("ER025"));return;}
        t.Rows.RemoveAt(i);Vincular(t);
    }
}
public sealed class ErrorPlanEventArgs:EventArgs
{public string Codigo {get;private set;}public ErrorPlanEventArgs(string codigo){Codigo=codigo;}}
