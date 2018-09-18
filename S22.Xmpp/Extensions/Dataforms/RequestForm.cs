namespace S22.Xmpp.Extensions.Dataforms
{
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class RequestForm : DataForm
    {
        public RequestForm(XmlElement element) : base(element, false)
        {
            base.AssertType(DataFormType.Form);
        }

        public RequestForm(string title = null, string instructions = null, params DataField[] fields) : base(title, instructions, false, fields)
        {
            base.Type = DataFormType.Form;
        }
    }
}

