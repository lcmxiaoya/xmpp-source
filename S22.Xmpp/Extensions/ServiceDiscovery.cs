namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Xml;

    internal class ServiceDiscovery : XmppExtension, IInputFilter<Iq>
    {
        private IDictionary<Jid, IEnumerable<Extension>> cache;

        public ServiceDiscovery(XmppIm im) : base(im)
        {
            this.cache = new Dictionary<Jid, IEnumerable<Extension>>();
            Attribute attribute = (Attribute) Assembly.GetExecutingAssembly().GetCustomAttributes(true)[0];
            string name = "S22.Xmpp";
            this.Identity = new S22.Xmpp.Extensions.Identity("client", "pc", name);
        }

        private IEnumerable<string> CompileFeatureSet()
        {
            ISet<string> set = new HashSet<string>();
            foreach (XmppExtension extension in base.im.Extensions)
            {
                foreach (string str in extension.Namespaces)
                {
                    set.Add(str);
                }
            }
            return set;
        }

        public IEnumerable<Extension> GetExtensions(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (!this.cache.ContainsKey(jid))
            {
                this.cache.Add(jid, this.QueryFeatures(jid));
            }
            return this.cache[jid];
        }

        public IEnumerable<S22.Xmpp.Extensions.Identity> GetIdentities(Jid jid)
        {
            return this.QueryIdentities(jid);
        }

        public IEnumerable<S22.Xmpp.Extensions.Item> GetItems(Jid jid)
        {
            return this.QueryItems(jid);
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Get)
            {
                return false;
            }
            XmlElement element = stanza.Data["query"];
            if (element == null)
            {
                return false;
            }
            if (element.NamespaceURI == "http://jabber.org/protocol/disco#items")
            {
                base.im.IqResult(stanza, Xml.Element("query", "http://jabber.org/protocol/disco#items"));
                return true;
            }
            if (element.NamespaceURI != "http://jabber.org/protocol/disco#info")
            {
                return false;
            }
            XmlElement child = Xml.Element("identity", null).Attr("category", this.Identity.Category).Attr("type", this.Identity.Type).Attr("name", this.Identity.Name);
            XmlElement e = Xml.Element("query", "http://jabber.org/protocol/disco#info").Child(child);
            foreach (string str in this.CompileFeatureSet())
            {
                e.Child(Xml.Element("feature", null).Attr("var", str));
            }
            base.im.IqResult(stanza, e);
            return true;
        }

        private IEnumerable<Extension> QueryFeatures(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            Iq iq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("query", "http://jabber.org/protocol/disco#info"), null, -1, "");
            if (iq.Type != IqType.Result)
            {
                throw new NotSupportedException("Could not query features: " + iq);
            }
            XmlElement element = iq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "http://jabber.org/protocol/disco#info"))
            {
                throw new NotSupportedException("Erroneous response: " + iq);
            }
            ISet<string> set = new HashSet<string>();
            foreach (XmlElement element2 in element.GetElementsByTagName("feature"))
            {
                set.Add(element2.GetAttribute("var"));
            }
            ISet<Extension> set2 = new HashSet<Extension>();
            foreach (XmppExtension extension in base.im.Extensions)
            {
                if (set.IsSupersetOf(extension.Namespaces))
                {
                    set2.Add(extension.Xep);
                }
            }
            return set2;
        }

        private IEnumerable<S22.Xmpp.Extensions.Identity> QueryIdentities(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            Iq iq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("query", "http://jabber.org/protocol/disco#info"), null, -1, "");
            if (iq.Type != IqType.Result)
            {
                throw new NotSupportedException("Could not query features: " + iq);
            }
            XmlElement element = iq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "http://jabber.org/protocol/disco#info"))
            {
                throw new NotSupportedException("Erroneous response: " + iq);
            }
            ISet<S22.Xmpp.Extensions.Identity> set = new HashSet<S22.Xmpp.Extensions.Identity>();
            foreach (XmlElement element2 in element.GetElementsByTagName("identity"))
            {
                string attribute = element2.GetAttribute("category");
                string str2 = element2.GetAttribute("type");
                string str3 = element2.GetAttribute("name");
                if (!string.IsNullOrEmpty(attribute) && !string.IsNullOrEmpty(str2))
                {
                    set.Add(new S22.Xmpp.Extensions.Identity(attribute, str2, string.IsNullOrEmpty(str3) ? null : str3));
                }
            }
            return set;
        }

        private IEnumerable<S22.Xmpp.Extensions.Item> QueryItems(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            Iq iq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("query", "http://jabber.org/protocol/disco#items"), null, -1, "");
            if (iq.Type != IqType.Result)
            {
                throw new NotSupportedException("Could not query items: " + iq);
            }
            XmlElement element = iq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "http://jabber.org/protocol/disco#items"))
            {
                throw new NotSupportedException("Erroneous response: " + iq);
            }
            ISet<S22.Xmpp.Extensions.Item> set = new HashSet<S22.Xmpp.Extensions.Item>();
            foreach (XmlElement element2 in element.GetElementsByTagName("item"))
            {
                string attribute = element2.GetAttribute("jid");
                string str2 = element2.GetAttribute("node");
                string str3 = element2.GetAttribute("name");
                if (!string.IsNullOrEmpty(attribute))
                {
                    try
                    {
                        Jid jid2 = new Jid(attribute);
                        set.Add(new S22.Xmpp.Extensions.Item(jid2, string.IsNullOrEmpty(str2) ? null : str2, string.IsNullOrEmpty(str3) ? null : str3));
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            return set;
        }

        public bool Supports<T>(Jid jid) where T: XmppExtension
        {
            jid.ThrowIfNull<Jid>("jid");
            T extension = base.im.GetExtension<T>();
            return this.Supports(jid, new Extension[] { extension.Xep });
        }

        public bool Supports(Jid jid, params Extension[] extensions)
        {
            jid.ThrowIfNull<Jid>("jid");
            extensions.ThrowIfNull<Extension[]>("extensions");
            if (!this.cache.ContainsKey(jid))
            {
                this.cache.Add(jid, this.QueryFeatures(jid));
            }
            IEnumerable<Extension> source = this.cache[jid];
            foreach (Extension extension in extensions)
            {
                if (!source.Contains<Extension>(extension))
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<string> Features
        {
            get
            {
                return this.CompileFeatureSet();
            }
        }

        public S22.Xmpp.Extensions.Identity Identity { get; private set; }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/disco#info", "http://jabber.org/protocol/disco#items" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.ServiceDiscovery;
            }
        }
    }
}

