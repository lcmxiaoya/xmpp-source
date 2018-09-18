namespace S22.Xmpp.Extensions.Dataforms
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class TextMultiField : DataField
    {
        private XmlCollection<string> values;

        internal TextMultiField(XmlElement element) : base(element)
        {
            base.AssertType(DataFieldType.TextMulti);
        }

        public TextMultiField(string name, params string[] values) : this(name, false, null, null, values)
        {
        }

        public TextMultiField(string name, bool required = false, string label = null, string description = null, params string[] values) : base(DataFieldType.TextMulti, name, required, label, description)
        {
            this.values = new XmlCollection<string>(base.element, "value", elem => elem.InnerText);
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

        public ICollection<string> Values
        {
            get
            {
                return this.values;
            }
        }
    }
}

