namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class PasswordField : DataField
    {
        internal PasswordField(XmlElement element) : base(element)
        {
            base.AssertType(DataFieldType.TextPrivate);
        }

        public PasswordField(string name, string value) : this(name, false, null, null, value)
        {
        }

        public PasswordField(string name, bool required = false, string label = null, string description = null, string value = null) : base(DataFieldType.TextPrivate, name, required, label, description)
        {
            name.ThrowIfNull<string>("name");
            this.Value = value;
        }

        public string Value
        {
            get
            {
                XmlElement element = base.element["value"];
                return ((element != null) ? element.InnerText : null);
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
                        base.element["value"].InnerText = value;
                    }
                }
                else if (value != null)
                {
                    base.element.Child(Xml.Element("value", null).Text(value));
                }
            }
        }
    }
}

