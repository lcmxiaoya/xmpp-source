namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Xml;

    internal class SoftwareVersion : XmppExtension, IInputFilter<Iq>
    {
        private EntityCapabilities ecapa;

        public SoftwareVersion(XmppIm im) : base(im)
        {
            Attribute attribute = (Attribute) Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), true)[0];
            string name = (attribute != null) ? ((AssemblyProductAttribute) attribute).Product : "S22.Xmpp";
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Version = new VersionInformation(name, version, Environment.OSVersion.ToString());
        }

        public VersionInformation GetVersion(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (!this.ecapa.Supports(jid, new Extension[] { Extension.SoftwareVersion }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Software Version' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("query", "jabber:iq:version"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The version could not be retrieved.");
            }
            XmlElement element = errorIq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "jabber:iq:version"))
            {
                throw new XmppException("Erroneous server response: " + errorIq);
            }
            if ((element["name"] == null) || (element["version"] == null))
            {
                throw new XmppException("Missing name or version element: " + errorIq);
            }
            return new VersionInformation(element["name"].InnerText, element["version"].InnerText, (element["os"] != null) ? element["os"].InnerText : null);
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
            XmlElement element = stanza.Data["query"];
            if ((element == null) || (element.NamespaceURI != "jabber:iq:version"))
            {
                return false;
            }
            XmlElement data = Xml.Element("query", "jabber:iq:version").Child(Xml.Element("name", null).Text(this.Version.Name)).Child(Xml.Element("version", null).Text(this.Version.Version)).Child(Xml.Element("os", null).Text(this.Version.Os));
            base.im.IqResult(stanza, data);
            return true;
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "jabber:iq:version" };
            }
        }

        public VersionInformation Version { get; private set; }

        public override Extension Xep
        {
            get
            {
                return Extension.SoftwareVersion;
            }
        }
    }
}

