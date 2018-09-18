namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;

    internal abstract class XmppExtension
    {
        protected XmppIm im;

        public XmppExtension(XmppIm im)
        {
            im.ThrowIfNull<XmppIm>("im");
            this.im = im;
        }

        public virtual void Initialize()
        {
        }

        public abstract IEnumerable<string> Namespaces { get; }

        public abstract Extension Xep { get; }
    }
}

