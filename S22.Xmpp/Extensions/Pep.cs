namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class Pep : XmppExtension, IInputFilter<S22.Xmpp.Im.Message>
    {
        private IDictionary<string, Action<Jid, XmlElement>> callbacks;
        private EntityCapabilities ecapa;
        private bool initialized;
        private bool supported;

        public Pep(XmppIm im) : base(im)
        {
            this.callbacks = new Dictionary<string, Action<Jid, XmlElement>>();
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public bool Input(S22.Xmpp.Im.Message stanza)
        {
            XmlElement element = stanza.Data["event"];
            if ((element != null) && (element.NamespaceURI == "http://jabber.org/protocol/pubsub#event"))
            {
                XmlElement element2 = element["items"];
                if (element2 == null)
                {
                    return false;
                }
                string attribute = element2.GetAttribute("node");
                if (string.IsNullOrEmpty(attribute))
                {
                    return false;
                }
                if (this.callbacks.ContainsKey(attribute))
                {
                    this.callbacks[attribute](stanza.From, element2["item"]);
                    return true;
                }
            }
            return false;
        }

        public void Publish(string node, string itemId = null, params XmlElement[] data)
        {
            node.ThrowIfNull<string>("node");
            if (!this.Supported)
            {
                throw new NotSupportedException("The server does not support publishing of information.");
            }
            XmlElement element = Xml.Element("pubsub", "http://jabber.org/protocol/pubsub").Child(Xml.Element("publish", null).Attr("node", node));
            if (data != null)
            {
                XmlElement e = Xml.Element("item", null);
                if (itemId != null)
                {
                    e.Attr("id", itemId);
                }
                foreach (XmlElement element3 in data)
                {
                    if (element3 != null)
                    {
                        e.Child(element3);
                    }
                }
                if (!e.IsEmpty)
                {
                    element["publish"].Child(e);
                }
            }
            Iq errorIq = base.im.IqRequest(IqType.Set, null, base.im.Jid, element, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The data could not be published.");
            }
        }

        private bool QueryServer()
        {
            foreach (Identity identity in this.ecapa.GetIdentities(base.im.Jid.GetBareJid()))
            {
                if ((identity.Type == "pep") && (identity.Category == "pubsub"))
                {
                    this.supported = true;
                }
            }
            this.initialized = true;
            return this.supported;
        }

        public XmlElement RetrieveItem(Jid jid, string node, string itemId)
        {
            jid.ThrowIfNull<Jid>("jid");
            node.ThrowIfNull<string>("node");
            itemId.ThrowIfNull<string>("itemId");
            XmlElement data = Xml.Element("pubsub", "http://jabber.org/protocol/pubsub").Child(Xml.Element("items", null).Attr("node", node).Child(Xml.Element("item", null).Attr("id", itemId)));
            Iq errorIq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The item could not be retrieved.");
            }
            XmlElement element2 = errorIq.Data["pubsub"];
            if ((element2 == null) || (element2.NamespaceURI != "http://jabber.org/protocol/pubsub"))
            {
                throw new XmppException("Expected 'pubsub' element: " + errorIq);
            }
            XmlElement e = element2["items"];
            if ((e == null) || (e.GetAttribute("node") != node))
            {
                throw new XmppException("Expected 'items' element: " + errorIq);
            }
            if ((e["item"] == null) || (e["item"].GetAttribute("id") != itemId))
            {
                throw new XmppException("Expected 'item' element: " + e.ToXmlString(false, false));
            }
            return e["item"];
        }

        public IEnumerable<XmlElement> RetrieveItems(Jid jid, string node)
        {
            jid.ThrowIfNull<Jid>("jid");
            node.ThrowIfNull<string>("node");
            XmlElement data = Xml.Element("pubsub", "http://jabber.org/protocol/pubsub").Child(Xml.Element("items", null).Attr("node", node));
            Iq errorIq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The items could not be retrieved.");
            }
            XmlElement element2 = errorIq.Data["pubsub"];
            if ((element2 == null) || (element2.NamespaceURI != "http://jabber.org/protocol/pubsub"))
            {
                throw new XmppException("Expected 'pubsub' element: " + errorIq);
            }
            XmlElement element3 = element2["items"];
            if ((element3 == null) || (element3.GetAttribute("node") != node))
            {
                throw new XmppException("Expected 'items' element: " + errorIq);
            }
            ISet<XmlElement> set = new HashSet<XmlElement>();
            foreach (XmlElement element4 in element3.GetElementsByTagName("item"))
            {
                set.Add(element4);
            }
            return set;
        }

        public void Subscribe(string node, Action<Jid, XmlElement> cb)
        {
            node.ThrowIfNull<string>("node");
            cb.ThrowIfNull<Action<Jid, XmlElement>>("cb");
            this.callbacks.Add(node, cb);
        }

        public void Unsubscribe(string node)
        {
            node.ThrowIfNull<string>("node");
            if (this.callbacks.ContainsKey(node))
            {
                this.callbacks.Remove(node);
            }
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/pubsub" };
            }
        }

        public bool Supported
        {
            get
            {
                if (!this.initialized)
                {
                    return this.QueryServer();
                }
                return this.supported;
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.PersonalEventingProcotol;
            }
        }
    }
}

