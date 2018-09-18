namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class ListField : DataField
    {
        private XmlCollection<Option> options;

        internal ListField(XmlElement e) : base(e)
        {
            base.AssertType(DataFieldType.ListSingle);
            this.options = new XmlCollection<Option>(base.element, "option", new Func<XmlElement, Option>(this.OptionFromElement));
        }

        public ListField(string name, string value) : this(name, false, null, null, null, value)
        {
        }

        public ListField(string name, bool required = false, string label = null, string description = null, IEnumerable<Option> options = null, string value = null) : base(DataFieldType.ListSingle, name, required, label, description)
        {
            this.options = new XmlCollection<Option>(base.element, "option", new Func<XmlElement, Option>(this.OptionFromElement));
            if (options != null)
            {
                foreach (Option option in options)
                {
                    this.Options.Add(option);
                }
            }
            if (value != null)
            {
                this.Value = value;
            }
        }

        private Option OptionFromElement(XmlElement element)
        {
            element.ThrowIfNull<XmlElement>("element");
            string attribute = element.GetAttribute("label");
            if (attribute == string.Empty)
            {
                attribute = null;
            }
            if (element["value"] == null)
            {
                throw new ArgumentException("Missing 'value' child.");
            }
            return new Option(element["value"].InnerText, attribute);
        }

        public ICollection<Option> Options
        {
            get
            {
                return this.options;
            }
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

