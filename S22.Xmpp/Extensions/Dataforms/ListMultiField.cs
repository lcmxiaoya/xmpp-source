namespace S22.Xmpp.Extensions.Dataforms
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class ListMultiField : DataField
    {
        private XmlCollection<Option> options;
        private XmlCollection<string> values;

        internal ListMultiField(XmlElement e) : base(e)
        {
            base.AssertType(DataFieldType.ListMulti);
            this.values = new XmlCollection<string>(base.element, "value", elem => elem.InnerText);
            this.options = new XmlCollection<Option>(base.element, "option", new Func<XmlElement, Option>(this.OptionFromElement));
        }

        public ListMultiField(string name, params string[] values) : this(name, false, null, null, null, values)
        {
        }

        public ListMultiField(string name, bool required = false, string label = null, string description = null, IEnumerable<Option> options = null, params string[] values) : base(DataFieldType.ListMulti, name, required, label, description)
        {
            this.values = new XmlCollection<string>(base.element, "value", elem => elem.InnerText);
            this.options = new XmlCollection<Option>(base.element, "option", new Func<XmlElement, Option>(this.OptionFromElement));
            if (options != null)
            {
                foreach (Option option in options)
                {
                    this.Options.Add(option);
                }
            }
            if (values != null)
            {
                foreach (string str in values)
                {
                    if (str != null)
                    {
                        this.values.Add(str);
                    }
                }
            }
        }

        private Option OptionFromElement(XmlElement element)
        {
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

        public ICollection<string> Values
        {
            get
            {
                return this.values;
            }
        }
    }
}

