namespace S22.Xmpp.Extensions.Dataforms
{
    using System;
    using System.Xml;

    internal class CancelForm : DataForm
    {
        public CancelForm() : base(null, null, true, new DataField[0])
        {
            base.Type = DataFormType.Cancel;
        }

        public CancelForm(XmlElement element) : base(element, false)
        {
            base.AssertType(DataFormType.Cancel);
        }
    }
}

