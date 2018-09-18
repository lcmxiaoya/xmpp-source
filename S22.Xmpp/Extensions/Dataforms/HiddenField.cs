namespace S22.Xmpp.Extensions.Dataforms
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class HiddenField : DataField
    {
        private XmlCollection<string> values;

        internal HiddenField(XmlElement element) : base(element)
        {
            base.AssertType(DataFieldType.Hidden);
        }

        public HiddenField(string name, params string[] values) : this(name, false, null, null, values)
        {
        }

        public HiddenField(string name, bool required = false, string label = null, string description = null, params string[] values) : base(DataFieldType.Hidden, name, required, label, description)
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

