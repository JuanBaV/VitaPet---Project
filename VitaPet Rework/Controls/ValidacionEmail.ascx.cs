using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Controls_ValidacionEmail : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public string Correo
    {
        get
        {
            return txtCorreo.Text.Trim();
        }
        set
        {
            txtCorreo.Text = value;
        }
    }


    public int MaxLength
    {
        get { return txtCorreo.MaxLength; }
        set { txtCorreo.MaxLength = value; revLongitudCorreo.ValidationExpression=value>0?@"^[\s\S]{1,"+value+@"}$":@"^[\s\S]*$"; }
    }

    public bool EnableClientValidation
    {
        get { return rfvCorreo.EnableClientScript; }
        set { rfvCorreo.EnableClientScript=value;revCorreo.EnableClientScript=value;revLongitudCorreo.EnableClientScript=value; }
    }

    public string ValidationGroup
    {
        get
        {
            return rfvCorreo.ValidationGroup;
        }
        set
        {
            rfvCorreo.ValidationGroup = value;
            revCorreo.ValidationGroup = value;
            revLongitudCorreo.ValidationGroup = value;
        }
    }



}
