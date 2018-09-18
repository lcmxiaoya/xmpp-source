namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class Presence : S22.Xmpp.Core.Presence
    {
        private PresenceType type;

        public Presence(S22.Xmpp.Core.Presence presence) : base(null, null, null, null, new XmlElement[0])
        {
            presence.ThrowIfNull<S22.Xmpp.Core.Presence>("presence");
            this.type = this.ParseType(presence.Data.GetAttribute("type"));
            base.element = presence.Data;
        }

        public Presence(Jid to = null, Jid from = null, PresenceType type = 0, string id = null, CultureInfo language = null, params XmlElement[] data) : base(to, from, id, language, data)
        {
            this.Type = type;
        }

        private PresenceType ParseType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return PresenceType.Available;
            }
            return (PresenceType) Enum.Parse(typeof(PresenceType), value.Capitalize());
        }

        public PresenceType Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
                if (value == PresenceType.Available)
                {
                    base.element.RemoveAttribute("type");
                }
                else
                {
                    string str = value.ToString().ToLowerInvariant();
                    base.element.SetAttribute("type", str);
                }
            }
        }
    }
}

