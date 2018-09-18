namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Xml;

    public abstract class Stanza
    {
        protected XmlElement element;

        protected Stanza(XmlElement element)
        {
            element.ThrowIfNull<XmlElement>("element");
            this.element = element;
        }

        public Stanza(string @namespace = null, Jid to = null, Jid from = null, string id = null, CultureInfo language = null, params XmlElement[] data)
        {
            string name = base.GetType().Name.ToLowerInvariant();
            this.element = Xml.Element(name, @namespace);
            this.To = to;
            this.From = from;
            this.Id = id;
            this.Language = language;
            foreach (XmlElement element in data)
            {
                if (element != null)
                {
                    this.element.Child(element);
                }
            }
        }

        public override string ToString()
        {
            return this.element.ToXmlString(false, false);
        }

        public XmlElement Data
        {
            get
            {
                return this.element;
            }
        }

        public Jid From
        {
            get
            {
                string attribute = this.element.GetAttribute("from");
                return (string.IsNullOrEmpty(attribute) ? null : new Jid(attribute));
            }
            set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("from");
                }
                else
                {
                    this.element.SetAttribute("from", value.ToString());
                }
            }
        }

        public string Id
        {
            get
            {
                string attribute = this.element.GetAttribute("id");
                return (string.IsNullOrEmpty(attribute) ? null : attribute);
            }
            set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("id");
                }
                else
                {
                    this.element.SetAttribute("id", value);
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this.Data.IsEmpty;
            }
        }

        public CultureInfo Language
        {
            get
            {
                string attribute = this.element.GetAttribute("xml:lang");
                return (string.IsNullOrEmpty(attribute) ? null : new CultureInfo(attribute));
            }
            set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("xml:lang");
                }
                else
                {
                    this.element.SetAttribute("xml:lang", value.Name);
                }
            }
        }

        public Jid To
        {
            get
            {
                string attribute = this.element.GetAttribute("to");
                return (string.IsNullOrEmpty(attribute) ? null : new Jid(attribute));
            }
            set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("to");
                }
                else
                {
                    this.element.SetAttribute("to", value.ToString());
                }
            }
        }
    }
}

