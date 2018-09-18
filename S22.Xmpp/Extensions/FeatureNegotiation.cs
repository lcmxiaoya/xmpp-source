namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Extensions.Dataforms;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    internal class FeatureNegotiation : XmppExtension
    {
        public FeatureNegotiation(XmppIm im) : base(im)
        {
        }

        public static XmlElement Create(DataForm form)
        {
            form.ThrowIfNull<DataForm>("form");
            return Xml.Element("feature", "http://jabber.org/protocol/feature-neg").Child(form.ToXmlElement());
        }

        public static DataForm Parse(XmlElement feature)
        {
            feature.ThrowIfNull<XmlElement>("feature");
            if (((feature.Name != "feature") || (feature.NamespaceURI != "http://jabber.org/protocol/feature-neg")) || (feature["x"] == null))
            {
                throw new ArgumentException("Invalid XML 'feature' element.");
            }
            return DataFormFactory.Create(feature["x"]);
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/feature-neg" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.FeatureNegotiation;
            }
        }
    }
}

