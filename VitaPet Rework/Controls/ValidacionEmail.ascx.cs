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
        set { txtCorreo.MaxLength = value; }
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
        }
    }



}
