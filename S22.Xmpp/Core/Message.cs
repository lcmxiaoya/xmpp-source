namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class Message : Stanza
    {
        public Message(XmlElement element) : base(element)
        {
        }

        public Message(Jid to = null, Jid from = null, XmlElement data = null, string id = null, CultureInfo language = null) : base(null, to, from, id, language, new XmlElement[] { data })
        {
        }
    }
}

