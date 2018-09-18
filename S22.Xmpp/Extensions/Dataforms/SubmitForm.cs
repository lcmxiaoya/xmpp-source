namespace S22.Xmpp.Extensions.Dataforms
{
    using System;
    using System.Xml;

    public class SubmitForm : DataForm
    {
        public SubmitForm(params DataField[] fields) : base(null, null, false, fields)
        {
            base.Type = DataFormType.Submit;
        }

        internal SubmitForm(XmlElement element) : base(element, false)
        {
            base.AssertType(DataFormType.Submit);
        }
    }
}

