namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class Presence : Stanza
    {
        public Presence(XmlElement element) : base(element)
        {
        }

        public Presence(Jid to = null, Jid from = null, string id = null, CultureInfo language = null, params XmlElement[] data) : base(null, to, from, id, language, data)
        {
        }
    }
}

