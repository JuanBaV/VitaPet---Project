using System;
using System.Collections.Specialized;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace VitaPet.GUI.Presentacion
{
    // Native browser calendar selector; unlike TextBox, its value is always handled as an optional date.
    [ValidationProperty("Value")]
    public sealed class DateTimePicker : WebControl, IPostBackDataHandler
    {
        public string Value
        {
            get { return (string)ViewState["Value"] ?? string.Empty; }
            set { ViewState["Value"] = value; }
        }
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            Page.RegisterRequiresPostBack(this);
        }
        public bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            if (Enabled) Value = postCollection[postDataKey] ?? string.Empty;
            return false;
        }
        public void RaisePostDataChangedEvent() { }
        protected override void Render(HtmlTextWriter writer)
        {
            AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "date");
            writer.AddAttribute(HtmlTextWriterAttribute.Name, UniqueID);
            writer.AddAttribute(HtmlTextWriterAttribute.Value, Value);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag();
        }
    }
}
