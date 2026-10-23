using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;

public partial class Controls_ValidacionNumerica : UserControl
{
    public string Valor
    {
        get { return txtNumero.Text.Trim(); }
        set { txtNumero.Text = value; }
    }
    public string TextoEtiqueta
    {
        get { return lblNumero.Text; }
        set { lblNumero.Text = value; }
    }
    public string ValidationGroup
    {
        get { return rfvNumero.ValidationGroup; }
        set { rfvNumero.ValidationGroup = revNumero.ValidationGroup = value; }
    }
    public bool IntentarObtenerValor(out decimal numero)
    {
        numero = 0;
        return Regex.IsMatch(Valor, @"\A[0-9]+(?:[.,][0-9]{1,2})?\z")
            && decimal.TryParse(Valor.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out numero);
    }
}
