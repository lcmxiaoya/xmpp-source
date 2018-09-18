namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    internal class Attention : XmppExtension
    {
        private EntityCapabilities ecapa;

        public Attention(XmppIm im) : base(im)
        {
        }

        public void GetAttention(Jid jid, string message = null)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (!this.ecapa.Supports(jid, new Extension[] { Extension.Attention }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Attention' extension.");
            }
            Message message2 = new Message(jid, message, null, null, MessageType.Normal, null);
            message2.Data.Child(Xml.Element("attention", "urn:xmpp:attention:0"));
            base.im.SendMessage(message2);
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:attention:0" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.Attention;
            }
        }
    }
}

