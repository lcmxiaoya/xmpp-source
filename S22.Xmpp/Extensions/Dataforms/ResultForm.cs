namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class ResultForm : DataForm
    {
        public ResultForm(XmlElement element) : base(element, false)
        {
            base.AssertType(DataFormType.Result);
        }

        public ResultForm(string title = null, string instructions = null, IEnumerable<DataField> header = null, IList<IEnumerable<DataField>> cells = null) : base(title, instructions, false, new DataField[0])
        {
            base.Type = DataFormType.Result;
            if (header != null)
            {
                XmlElement e = Xml.Element("reported", null);
                foreach (DataField field in header)
                {
                    e.Child(field.ToXmlElement());
                }
                base.element.Child(e);
            }
            if (cells != null)
            {
                foreach (IEnumerable<DataField> enumerable in cells)
                {
                    XmlElement element2 = Xml.Element("item", null);
                    foreach (DataField field in enumerable)
                    {
                        element2.Child(field.ToXmlElement());
                    }
                    base.element.Child(element2);
                }
            }
        }

        public IList<IEnumerable<DataField>> Cells
        {
            get
            {
                List<IEnumerable<DataField>> list = new List<IEnumerable<DataField>>();
                foreach (XmlElement element in base.element.GetElementsByTagName("item"))
                {
                    list.Add(new FieldList(element, false));
                }
                return list;
            }
        }

        public IEnumerable<DataField> Header
        {
            get
            {
                XmlElement element = base.element["reported"];
                if (element == null)
                {
                    return new List<DataField>();
                }
                return new FieldList(element, false);
            }
        }
    }
}

