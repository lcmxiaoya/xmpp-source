namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class Option
    {
        private XmlElement element;

        public Option(string value, string label = null)
        {
            value.ThrowIfNull<string>("value");
            this.element = Xml.Element("option", null);
            this.Value = value;
            this.Label = label;
        }

        public override string ToString()
        {
            return this.element.InnerXml;
        }

        public string Label
        {
            get
            {
                string attribute = this.element.GetAttribute("label");
                return (string.IsNullOrEmpty(attribute) ? null : attribute);
            }
            private set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("label");
                }
                else
                {
                    this.element.SetAttribute("label", value);
                }
            }
        }

        public string Value
        {
            get
            {
                XmlElement element = this.element["value"];
                return ((element != null) ? element.InnerText : null);
            }
            private set
            {
                if (this.element["value"] != null)
                {
                    if (value == null)
                    {
                        this.element.RemoveChild(this.element["value"]);
                    }
                    else
                    {
                        this.element["value"].InnerText = value;
                    }
                }
                else if (value != null)
                {
                    this.element.Child(Xml.Element("value", null).Text(value));
                }
            }
        }
    }
}

