namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class JidField : DataField
    {
        internal JidField(XmlElement element) : base(element)
        {
            base.AssertType(DataFieldType.JidSingle);
            try
            {
                this.GetJid();
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The value is not a valid JID.", exception);
            }
        }

        public JidField(string name, S22.Xmpp.Jid jid) : this(name, false, null, null, jid)
        {
        }

        public JidField(string name, bool required = false, string label = null, string description = null, S22.Xmpp.Jid jid = null) : base(DataFieldType.JidSingle, name, required, label, description)
        {
            name.ThrowIfNull<string>("name");
            this.Jid = jid;
        }

        private S22.Xmpp.Jid GetJid()
        {
            S22.Xmpp.Jid jid;
            XmlElement element = base.element["value"];
            try
            {
                jid = (element != null) ? new S22.Xmpp.Jid(element.InnerText) : null;
            }
            catch (Exception exception)
            {
                throw new XmlException("Invalid value for JidField.", exception);
            }
            return jid;
        }

        public S22.Xmpp.Jid Jid
        {
            get
            {
                return this.GetJid();
            }
            private set
            {
                if (base.element["value"] != null)
                {
                    if (value == null)
                    {
                        base.element.RemoveChild(base.element["value"]);
                    }
                    else
                    {
                        base.element["value"].InnerText = value.ToString();
                    }
                }
                else if (value != null)
                {
                    base.element.Child(Xml.Element("value", null).Text(value.ToString()));
                }
            }
        }
    }
}

