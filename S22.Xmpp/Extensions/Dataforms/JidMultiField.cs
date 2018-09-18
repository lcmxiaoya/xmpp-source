namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class JidMultiField : DataField
    {
        private XmlCollection<Jid> values;

        internal JidMultiField(XmlElement element) : base(element)
        {
            base.AssertType(DataFieldType.JidMulti);
        }

        public JidMultiField(string name, params Jid[] values) : this(name, false, null, null, values)
        {
        }

        public JidMultiField(string name, bool required = false, string label = null, string description = null, params Jid[] values) : base(DataFieldType.TextMulti, name, required, label, description)
        {
            Func<XmlElement, Jid> conversion = null;
            if (conversion == null)
            {
                conversion = e => new Jid(base.element.InnerText);
            }
            this.values = new XmlCollection<Jid>(base.element, "value", conversion);
            if (values != null)
            {
                foreach (Jid jid in values)
                {
                    if (jid != null)
                    {
                        this.values.Add(jid);
                    }
                }
            }
        }

        public ICollection<Jid> Values
        {
            get
            {
                return this.values;
            }
        }
    }
}

