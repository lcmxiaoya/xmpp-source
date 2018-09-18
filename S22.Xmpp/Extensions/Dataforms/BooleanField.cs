namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class BooleanField : DataField
    {
        internal BooleanField(XmlElement element) : base(element)
        {
            base.AssertType(DataFieldType.Boolean);
        }

        public BooleanField(string name, bool value) : this(name, false, null, null, new bool?(value))
        {
        }

        public BooleanField(string name, bool required = false, string label = null, string description = null, bool? value = new bool?()) : base(DataFieldType.Boolean, name, required, label, description)
        {
            name.ThrowIfNull<string>("name");
            this.Value = value;
        }

        private bool ParseValue(string value)
        {
            value.ThrowIfNull<string>("value");
            if ((value == "0") || (value == "false"))
            {
                return false;
            }
            return true;
        }

        public bool? Value
        {
            get
            {
                XmlElement element = base.element["value"];
                if (element == null)
                {
                    return null;
                }
                return new bool?(this.ParseValue(element.InnerText));
            }
            private set
            {
                if (base.element["value"] != null)
                {
                    if (!value.HasValue)
                    {
                        base.element.RemoveChild(base.element["value"]);
                    }
                    else
                    {
                        base.element["value"].InnerText = value.ToString().ToLower();
                    }
                }
                else if (value.HasValue)
                {
                    base.element.Child(Xml.Element("value", null).Text(value.ToString().ToLower()));
                }
            }
        }
    }
}

