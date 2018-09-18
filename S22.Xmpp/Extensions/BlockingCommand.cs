namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    internal class BlockingCommand : XmppExtension, IInputFilter<Iq>
    {
        private EntityCapabilities ecapa;

        public BlockingCommand(XmppIm im) : base(im)
        {
        }

        public void Block(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (!this.ecapa.Supports(base.im.Jid.Domain, new Extension[] { Extension.BlockingCommand }))
            {
                throw new NotSupportedException("The server does not support the 'Blocking Command' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Set, null, base.im.Jid, Xml.Element("block", "urn:xmpp:blocking").Child(Xml.Element("item", null).Attr("jid", jid.ToString())), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The XMPP entity could not be blocked.");
            }
        }

        public IEnumerable<Jid> GetBlocklist()
        {
            if (!this.ecapa.Supports(base.im.Jid.Domain, new Extension[] { Extension.BlockingCommand }))
            {
                throw new NotSupportedException("The server does not support the 'Blocking Command' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Get, null, base.im.Jid, Xml.Element("blocklist", "urn:xmpp:blocking"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The blocklist could not be retrieved.");
            }
            ISet<Jid> set = new HashSet<Jid>();
            XmlElement element = errorIq.Data["blocklist"];
            if ((element == null) || (element.NamespaceURI != "urn:xmpp:blocking"))
            {
                throw new XmppException("Erroneous server response.");
            }
            foreach (XmlElement element2 in element.GetElementsByTagName("item"))
            {
                try
                {
                    string attribute = element2.GetAttribute("jid");
                    set.Add(attribute);
                }
                catch (FormatException exception)
                {
                    throw new XmppException("Encountered an invalid JID.", exception);
                }
            }
            return set;
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Set)
            {
                return false;
            }
            if ((stanza.Data["block"] == null) && (stanza.Data["unblock"] == null))
            {
                return false;
            }
            bool flag = stanza.Data["block"] != null;
            XmlElement element = flag ? stanza.Data["block"] : stanza.Data["unblock"];
            if (element.NamespaceURI != "urn:xmpp:blocking")
            {
                return false;
            }
            base.im.IqResult(stanza, null);
            foreach (XmlElement element2 in element.GetElementsByTagName("item"))
            {
                try
                {
                    Jid attribute = element2.GetAttribute("jid");
                    if (flag)
                    {
                    }
                }
                catch (FormatException)
                {
                }
            }
            return true;
        }

        public void Unblock(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (!this.ecapa.Supports(base.im.Jid.Domain, new Extension[] { Extension.BlockingCommand }))
            {
                throw new NotSupportedException("The server does not support the 'Blocking Command' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Set, null, base.im.Jid, Xml.Element("unblock", "urn:xmpp:blocking").Child(Xml.Element("item", null).Attr("jid", jid.ToString())), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The XMPP entity could not be unblocked.");
            }
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:blocking" };
            }
        }

        public bool Supported
        {
            get
            {
                return this.ecapa.Supports(base.im.Jid.Domain, new Extension[] { Extension.BlockingCommand });
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.BlockingCommand;
            }
        }
    }
}

