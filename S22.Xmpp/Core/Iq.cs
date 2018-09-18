namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class Iq : Stanza
    {
        public Iq(XmlElement element) : base(element)
        {
        }

        public Iq(IqType type, string id, Jid to = null, Jid from = null, XmlElement data = null, CultureInfo language = null) : base(null, to, from, id, language, new XmlElement[] { data })
        {
            this.Type = type;
        }

        private IqType ParseType(string value)
        {
            value.ThrowIfNull<string>("value");
            Dictionary<string, IqType> dictionary2 = new Dictionary<string, IqType>();
            dictionary2.Add("set", IqType.Set);
            dictionary2.Add("get", IqType.Get);
            dictionary2.Add("result", IqType.Result);
            dictionary2.Add("error", IqType.Error);
            Dictionary<string, IqType> dictionary = dictionary2;
            return dictionary[value];
        }

        public bool IsRequest
        {
            get
            {
                IqType type = this.Type;
                return ((type == IqType.Set) || (type == IqType.Get));
            }
        }

        public bool IsResponse
        {
            get
            {
                return !this.IsRequest;
            }
        }

        public IqType Type
        {
            get
            {
                return this.ParseType(base.element.GetAttribute("type"));
            }
            set
            {
                string str = value.ToString().ToLowerInvariant();
                base.element.SetAttribute("type", str);
            }
        }
    }
}

