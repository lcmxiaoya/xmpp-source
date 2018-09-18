namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    internal class Ping : XmppExtension, IInputFilter<Iq>
    {
        private EntityCapabilities ecapa;

        public Ping(XmppIm im) : base(im)
        {
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Get)
            {
                return false;
            }
            XmlElement element = stanza.Data["ping"];
            if ((element == null) || (element.NamespaceURI != "urn:xmpp:ping"))
            {
                return false;
            }
            base.im.IqResult(stanza, null);
            return true;
        }

        public TimeSpan PingEntity(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (!this.ecapa.Supports(jid, new Extension[] { Extension.Ping }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Ping' extension.");
            }
            DateTime now = DateTime.Now;
            Iq errorIq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("ping", "urn:xmpp:ping"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "Could not ping XMPP entity.");
            }
            return DateTime.Now.Subtract(now);
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:ping" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.Ping;
            }
        }
    }
}

