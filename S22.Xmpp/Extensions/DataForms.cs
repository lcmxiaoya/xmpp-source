namespace S22.Xmpp.Extensions
{
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;

    internal class DataForms : XmppExtension
    {
        public DataForms(XmppIm im) : base(im)
        {
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "jabber:x:data" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.DataForms;
            }
        }
    }
}

